"""
Train and export a YOLO model for Clinical Bacteria DataSet.

Expected class order for the app:
  0: G-cocci
  1: G+cocci
  2: G-bacilli
  3: G+bacilli

Usage:
  python build/train_clinical_bacteria_yolo.py --dataset D:\data\ClinicalBacteria\DetectionDataSet

The dataset directory should contain YOLO-style train/val image and label
folders. If your extracted dataset uses a different layout, create a data.yaml
manually with the same class order and pass it via --data-yaml.
"""

from __future__ import annotations

import argparse
from pathlib import Path
import subprocess
import sys
import textwrap


CLASS_NAMES = ["G-cocci", "G+cocci", "G-bacilli", "G+bacilli"]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--dataset", type=Path, required=False)
    parser.add_argument("--data-yaml", type=Path, required=False)
    parser.add_argument("--model", default="yolov8n.pt")
    parser.add_argument("--epochs", type=int, default=100)
    parser.add_argument("--imgsz", type=int, default=640)
    parser.add_argument("--batch", type=int, default=16)
    parser.add_argument("--project", default="runs/clinical_bacteria")
    parser.add_argument("--name", default="yolo_gram_stain")
    args = parser.parse_args()

    data_yaml = args.data_yaml
    if data_yaml is None:
        if args.dataset is None:
            parser.error("--dataset or --data-yaml is required")
        data_yaml = write_data_yaml(args.dataset)

    run_command(
        [
            sys.executable,
            "-m",
            "ultralytics",
            "train",
            f"model={args.model}",
            f"data={data_yaml}",
            f"epochs={args.epochs}",
            f"imgsz={args.imgsz}",
            f"batch={args.batch}",
            f"project={args.project}",
            f"name={args.name}",
        ]
    )

    best_model = Path(args.project) / args.name / "weights" / "best.pt"
    if not best_model.exists():
        print(f"best.pt not found: {best_model}", file=sys.stderr)
        return 1

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

    print(f"Exported ONNX next to: {best_model}")
    return 0


def write_data_yaml(dataset: Path) -> Path:
    dataset = dataset.resolve()
    if not dataset.exists():
        raise FileNotFoundError(dataset)

    yaml_path = dataset / "entcapture_clinical_bacteria.yaml"
    yaml_path.write_text(
        textwrap.dedent(
            f"""
            path: {dataset.as_posix()}
            train: train/images
            val: val/images
            nc: 4
            names:
              0: {CLASS_NAMES[0]}
              1: {CLASS_NAMES[1]}
              2: {CLASS_NAMES[2]}
              3: {CLASS_NAMES[3]}
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    return yaml_path


def run_command(command: list[str]) -> None:
    print(" ".join(command))
    subprocess.run(command, check=True)


if __name__ == "__main__":
    raise SystemExit(main())
