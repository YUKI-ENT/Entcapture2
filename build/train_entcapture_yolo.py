r"""
Train and export a YOLO model from an ENTcapture YOLO dataset.

Expected dataset layout:
  dataset/
    data.yaml
    images/train/*.jpg
    labels/train/*.txt
    images/val/*.jpg
    labels/val/*.txt

Example:
  python build/train_entcapture_yolo.py --dataset D:\work\ENTcapture2_YOLO_Dataset --name gram_stain_v1

The script trains with Ultralytics, exports best.pt to ONNX, and copies the ONNX
and yaml as a pair into dataset/export/<name>/.
"""

from __future__ import annotations

import argparse
from datetime import datetime
from pathlib import Path
import shutil
import subprocess
import sys


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dataset", type=Path, required=True, help="ENTcapture YOLO dataset root")
    parser.add_argument("--data-yaml", type=Path, help="Defaults to <dataset>/data.yaml")
    parser.add_argument("--model", default="yolo11n.pt", help="Base model, e.g. yolo11n.pt or a previous best.pt")
    parser.add_argument("--epochs", type=int, default=100)
    parser.add_argument("--imgsz", type=int, default=640)
    parser.add_argument("--batch", default="auto", help="Ultralytics batch value, e.g. auto, 8, 16")
    parser.add_argument("--device", default=None, help="Ultralytics device, e.g. 0, cpu")
    parser.add_argument("--workers", type=int, default=4)
    parser.add_argument("--patience", type=int, default=30)
    parser.add_argument("--project", type=Path, help="Defaults to <dataset>/runs")
    parser.add_argument("--name", default=None, help="Run/export name")
    parser.add_argument("--install-deps", action="store_true", help="Install ultralytics if missing")
    args = parser.parse_args()

    dataset = args.dataset.resolve()
    data_yaml = (args.data_yaml or dataset / "data.yaml").resolve()
    project = (args.project or dataset / "runs").resolve()
    name = args.name or f"entcapture_yolo_{datetime.now():%Y%m%d_%H%M%S}"

    validate_dataset(dataset, data_yaml)
    ensure_ultralytics(args.install_deps)

    train_command = [
        sys.executable,
        "-m",
        "ultralytics",
        "train",
        f"model={args.model}",
        f"data={data_yaml}",
        f"epochs={args.epochs}",
        f"imgsz={args.imgsz}",
        f"batch={args.batch}",
        f"workers={args.workers}",
        f"patience={args.patience}",
        f"project={project}",
        f"name={name}",
    ]
    if args.device:
        train_command.append(f"device={args.device}")
    run_command(train_command)

    best_model = project / name / "weights" / "best.pt"
    if not best_model.exists():
        raise FileNotFoundError(f"best.pt not found: {best_model}")

    run_command(
        [
            sys.executable,
            "-m",
            "ultralytics",
            "export",
            f"model={best_model}",
            "format=onnx",
            f"imgsz={args.imgsz}",
            "opset=12",
            "simplify=True",
        ]
    )

    onnx_path = best_model.with_suffix(".onnx")
    if not onnx_path.exists():
        raise FileNotFoundError(f"ONNX export not found: {onnx_path}")

    export_dir = dataset / "export" / name
    export_dir.mkdir(parents=True, exist_ok=True)
    paired_onnx = export_dir / f"{name}.onnx"
    paired_yaml = export_dir / f"{name}.yaml"
    shutil.copy2(onnx_path, paired_onnx)
    shutil.copy2(data_yaml, paired_yaml)

    print()
    print("Training complete.")
    print(f"ONNX: {paired_onnx}")
    print(f"YAML: {paired_yaml}")
    print("Load the ONNX file in ENTcapture; the paired YAML will be picked up automatically.")
    return 0


def validate_dataset(dataset: Path, data_yaml: Path) -> None:
    if not dataset.exists():
        raise FileNotFoundError(f"Dataset folder not found: {dataset}")
    if not data_yaml.exists():
        raise FileNotFoundError(f"data.yaml not found: {data_yaml}")

    required = [
        dataset / "images" / "train",
        dataset / "labels" / "train",
    ]
    missing = [path for path in required if not path.exists()]
    if missing:
        raise FileNotFoundError("Missing required dataset folders: " + ", ".join(map(str, missing)))

    train_images = list((dataset / "images" / "train").glob("*.*"))
    train_labels = list((dataset / "labels" / "train").glob("*.txt"))
    if not train_images:
        raise RuntimeError(f"No training images found: {dataset / 'images' / 'train'}")
    if not train_labels:
        raise RuntimeError(f"No training labels found: {dataset / 'labels' / 'train'}")

    val_images = dataset / "images" / "val"
    if not val_images.exists() or not any(val_images.glob("*.*")):
        print("WARNING: images/val is empty or missing. Add validation images for reliable training.")


def ensure_ultralytics(install_deps: bool) -> None:
    try:
        __import__("ultralytics")
        return
    except ImportError:
        if not install_deps:
            raise RuntimeError(
                "Ultralytics is not installed. Re-run with --install-deps or install it with: "
                f"{sys.executable} -m pip install ultralytics onnx onnxsim"
            )

    run_command([sys.executable, "-m", "pip", "install", "--upgrade", "pip"])
    run_command([sys.executable, "-m", "pip", "install", "ultralytics", "onnx", "onnxsim"])


def run_command(command: list[str]) -> None:
    print(" ".join(str(item) for item in command))
    subprocess.run(command, check=True)


if __name__ == "__main__":
    raise SystemExit(main())
