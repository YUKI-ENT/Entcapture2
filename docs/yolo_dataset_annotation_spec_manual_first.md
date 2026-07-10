# YOLO教師データ作成機能 仕様（手動アノテーション優先版）

ENTcapture2 の細菌解析フォームに、自院グラム染色画像から YOLO 学習用データを作成するための **手動アノテーションフォーム** を追加する。

現在の Clinical Bacteria DataSet 由来の ONNX モデルは、公開データセット画像では検出できるが、自院の通常の 1000倍油浸グラム染色画像ではドメイン差が大きく、検出 box がほとんど出ない。そのため、今回の主目的は **AI候補の修正** ではなく、**人間が画像上で菌を一つずつ囲み、4分類ラベルとメタ情報を付けて保存すること** とする。

---

## 目的

グラム染色画像から細菌を検出・分類する YOLO モデルを fine tuning するため、ENTcapture2 上で以下を行えるようにする。

1. 現在表示中の画像を学習用画像として保存する。
2. 画像上で菌を手動で box 指定できる。
3. box ごとに 4分類ラベルを選べる。
4. box の追加、削除、移動、サイズ変更ができる。
5. YOLO形式の `labels/*.txt` を保存する。
6. 画像単位の追加情報を `meta/*.json` に保存する。
7. box が 0 件でも、菌なし画像・誤検出しやすい背景画像として保存できる。
8. 既存 ONNX 推論結果がある場合は、参考候補として読み込めるが、必須ではない。

---

## 重要方針

### 1. 手動アノテーションを主機能にする

現状の ONNX モデルは自院画像ではほとんど検出しないため、以下のような運用を想定する。

```text
画像を開く
↓
人間が菌を一つずつ左ドラッグで囲む
↓
boxごとに 1〜4キーまたは右クリックでクラス指定
↓
必要なら画像全体の所見メモを入力
↓
YOLO label txt + meta json + 画像を保存
```

AI検出結果の編集機能は便利だが、今回の Must ではなく Should とする。

### 2. YOLO用ラベルは4分類だけに固定する

肺炎球菌様、モラクセラ様、インフルエンザ菌様などに見えても、YOLO の box ラベルは以下の4分類だけにする。

```text
0: G-cocci
1: G+cocci
2: G-bacilli
3: G+bacilli
```

菌名候補、配列、分布、莢膜様所見などは `meta.json` に保存する。

### 3. 自由テキストだけにしない

表記ゆれを避けるため、box のクラス指定は必ず固定選択式にする。自由テキストは画像単位の補足メモとしてのみ使う。

---

## 優先順位

### Must

- 現在画像を保存できる。
- 手動で box を追加できる。
- box を選択できる。
- box を削除できる。
- box のクラスを 4分類から選べる。
- YOLO形式の label txt を保存できる。
- box が 0 件でも空の label txt を保存できる。
- `meta.json` を保存できる。
- `data.yaml` を生成できる。
- train / val の保存先を選べる。

### Should

- box の移動、サイズ変更ができる。
- 右クリックメニューでクラス変更・削除ができる。
- キーボードショートカットで高速にクラス変更できる。
- 画像単位のメタ情報を選択式UIで入力できる。
- 既存 AI 検出 box がある場合、それを編集対象として取り込める。

### Could

- クラスごとに box 色を変える。
- 複数の dominant_findings を登録できる。
- 前画像/次画像移動。
- 保存済みアノテーションの再読み込み。
- アノテーション済み/未保存の状態表示。

---

## YOLOクラス定義

クラス順は必ず以下で固定する。

```text
0: G-cocci
1: G+cocci
2: G-bacilli
3: G+bacilli
```

日本語表示は以下。

```text
0: G- 球菌 / グラム陰性球菌
1: G+ 球菌 / グラム陽性球菌
2: G- 桿菌 / グラム陰性桿菌
3: G+ 桿菌 / グラム陽性桿菌
```

UI表示用の短縮名は以下でもよい。

```text
G- 球菌
G+ 球菌
G- 桿菌
G+ 桿菌
```

---

## 手動アノテーションUI

細菌解析フォームに「教師データ作成」または「アノテーション編集」モードを追加する。

### 基本操作

```text
左ドラッグ:
  新規 box 追加

box クリック:
  box 選択

選択 box の内部ドラッグ:
  box 移動

選択 box の四隅または辺ドラッグ:
  box サイズ変更

Delete キー:
  選択 box 削除

1 キー:
  G- 球菌に変更

2 キー:
  G+ 球菌に変更

3 キー:
  G- 桿菌に変更

4 キー:
  G+ 桿菌に変更

Esc キー:
  選択解除
```

### 右クリックメニュー

box 上で右クリックした場合、以下のメニューを表示する。

```text
G- 球菌に変更
G+ 球菌に変更
G- 桿菌に変更
G+ 桿菌に変更
削除
```

画像上の box でない場所を右クリックした場合は、直前に作成した box または選択中の box に対する操作でもよい。

---

## box追加時のクラス指定

高速に作業できるよう、以下のどちらかを実装する。

### 推奨方式A

現在選択中のクラスをツールバーに持つ。

```text
現在のラベル:
  G- 球菌 / G+ 球菌 / G- 桿菌 / G+ 桿菌
```

ユーザーが 1〜4 キーで現在ラベルを切り替える。新規 box は現在ラベルで作成される。

### 方式B

box 作成直後に右クリックメニューまたは小さな選択メニューでクラスを選ぶ。

ただし、1画像あたり10〜100個程度の菌を囲う可能性があるため、方式Aの方が作業効率がよい。

---

## 画面表示

各 box には以下を表示する。

```text
クラス名
必要なら confidence
```

手動作成 box の confidence は空または `manual` 扱いでよい。

例:

```text
G+ 球菌
G- 桿菌
```

AI由来の box を取り込む場合は、以下のように表示してもよい。

```text
G- 桿菌 0.82
```

---

## クラス表示色

可能ならクラスごとに色分けする。

```text
G- 球菌: cyan
G+ 球菌: magenta
G- 桿菌: lime
G+ 桿菌: orange
```

難しければ、まずは全 box 同じ色でも構わない。

---

## 保存先フォルダ構成

ユーザーが保存先データセットフォルダを選べるようにする。

初期値はアプリフォルダ直下または設定値から以下とする。

```text
ENTcapture2_YOLO_Dataset/
```

保存構造は以下。

```text
ENTcapture2_YOLO_Dataset/
  images/
    train/
    val/
  labels/
    train/
    val/
  meta/
    train/
    val/
  data.yaml
```

UIに train / val を選ぶラジオボタンまたはコンボボックスを追加する。

```text
保存先:
  train
  val
```

初期値は `train` とする。

---

## ファイル名

保存ファイル名は重複しにくい形式にする。

例:

```text
20260710_091530_001.jpg
20260710_091530_001.txt
20260710_091530_001.json
```

保存先:

```text
images/train/20260710_091530_001.jpg
labels/train/20260710_091530_001.txt
meta/train/20260710_091530_001.json
```

同名ファイルが存在する場合は `_002`, `_003` のように連番を付ける。

---

## YOLO label txt仕様

`labels/train/*.txt` または `labels/val/*.txt` は YOLO形式で保存する。

1行1box。

```text
class_id x_center y_center width height
```

座標は画像全体に対する 0〜1 正規化座標。

例:

```text
1 0.512300 0.443100 0.018000 0.020000
1 0.531000 0.447200 0.017500 0.019500
2 0.411000 0.601200 0.035000 0.012000
```

box が 0 個の場合も、空の `.txt` ファイルを作成する。

これは菌なし画像、または細胞片・粘液・染色ムラなどが多い negative/background 画像として学習に使うため。

---

## 座標変換の注意

画面上の表示画像は、元画像を PictureBox 等に縮小・拡大して表示している可能性がある。

保存するYOLO座標は、表示座標ではなく **元画像のピクセル座標** を基準にする。

必要な変換:

```text
表示上の box 座標
↓
元画像上の box 座標
↓
YOLO 正規化座標
```

元画像サイズを `imageWidth`, `imageHeight` とする。

```csharp
xCenter = (x1 + x2) / 2.0 / imageWidth;
yCenter = (y1 + y2) / 2.0 / imageHeight;
width   = (x2 - x1) / imageWidth;
height  = (y2 - y1) / imageHeight;
```

保存前に、座標は必ず `0.0〜1.0` に clamp する。

幅または高さが0以下の box は保存しない。

---

## meta.json仕様

YOLOの4分類とは別に、画像全体の所見メモを `meta/*.json` に保存する。

目的は、将来の菌名候補推定、データ管理、症例背景の整理に使うため。YOLO学習には直接使わなくてもよい。

### 最低限の構造

```json
{
  "image_id": "20260710_091530_001",
  "image_file": "images/train/20260710_091530_001.jpg",
  "label_file": "labels/train/20260710_091530_001.txt",
  "class_order": ["G-cocci", "G+cocci", "G-bacilli", "G+bacilli"],
  "source": "ENTcapture2",
  "created_at": "2026-07-10T09:15:30+09:00",
  "split": "train",
  "specimen": "",
  "magnification": "",
  "gram_quality": "",
  "dominant_findings": [],
  "free_note": ""
}
```

---

## meta入力UI

フォーム上に、画像単位のメタ情報入力欄を追加する。

自由入力だけでなく、できれば選択式にする。

### specimen / 検体種別

表示候補:

```text
鼻汁
咽頭
喀痰
耳漏
膿汁
BAL/下気道
血液培養
その他
不明
```

保存値候補:

```text
nasal_discharge
pharyngeal
sputum
otorrhea
pus
lower_respiratory
blood_culture
other
unknown
```

### magnification / 倍率

```text
1000x_oil
400x
unknown
```

初期値は `1000x_oil` とする。

### gram_quality / 染色品質

保存値:

```text
good
pale
overstained
blurred
debris_many
unknown
```

日本語表示:

```text
良好
淡染
過染
ピンぼけ
debris多い
不明
```

---

## dominant_findings

画像全体としての主要所見を複数登録できるようにする。

最低限、UIが難しければ1件だけでも可。

各所見は以下の構造にする。

```json
{
  "morphology": "G+cocci",
  "arrangement": "diplococci",
  "distribution": "scattered",
  "species_candidate": "Streptococcus pneumoniae-like",
  "capsule_appearance": "unclear",
  "confidence_note": "morphology only, not definitive"
}
```

### morphology候補

```text
G-cocci
G+cocci
G-bacilli
G+bacilli
mixed
unknown
```

### arrangement候補

```text
single
diplococci
chains
clusters
palisade
scattered
intracellular
extracellular
unknown
```

日本語表示:

```text
単在
双球菌
連鎖
集簇
柵状
散在
細胞内
細胞外
不明
```

### distribution候補

```text
scattered
localized
around_cells
inside_neutrophils
outside_cells
mixed
unknown
```

日本語表示:

```text
散在
局在
細胞周囲
好中球内
細胞外
混在
不明
```

### species_candidate候補

```text
none
Streptococcus pneumoniae-like
Staphylococcus-like
Streptococcus-like
Moraxella-like
Neisseria-like
Haemophilus-like
Enterobacterales-like
Pseudomonas-like
Corynebacterium-like
mixed flora
unknown
```

日本語表示例:

```text
なし
肺炎球菌様
ブドウ球菌様
レンサ球菌様
モラクセラ様
ナイセリア様
インフルエンザ菌様
腸内細菌科様
緑膿菌様
コリネ様
混合菌叢
不明
```

### capsule_appearance候補

```text
visible
suspected
not_apparent
unclear
not_evaluable
```

日本語表示:

```text
あり
疑い
明らかでない
不明
評価不能
```

重要:

通常のグラム染色だけで「莢膜なし」と断定しない。保存値は `not_apparent` または `unclear` 程度にする。

---

## 画像保存

現在表示中の元画像を JPEG または PNG で保存する。

設定可能なら JPEG品質 95 とする。

元画像がすでに JPEG で読み込まれている場合でも、まずは現在の元画像 Bitmap を保存すればよい。

将来的には元画像そのもののコピー保存も検討するが、今回は Bitmap 保存で構わない。

---

## data.yaml自動生成

保存先データセットフォルダに `data.yaml` が無い場合は自動生成する。

すでに存在する場合は上書きしないか、確認してから上書きする。

内容:

```yaml
path: <dataset root absolute path>
train: images/train
val: images/val
nc: 4
names:
  0: G-cocci
  1: G+cocci
  2: G-bacilli
  3: G+bacilli
```

Windows のパスでも Ultralytics で読めるよう、できれば `/` 区切りで保存する。

---

## 保存前チェック

「教師データとして保存」ボタンを押したとき、以下を確認する。

```text
保存先 dataset root が設定されているか
画像が読み込まれているか
box 座標が画像内にあるか
クラスIDが 0〜3 か
```

box が 0 件でも保存できるようにする。

その場合は確認メッセージを出す。

```text
box が0件です。菌なし/背景画像として空ラベルで保存しますか？
```

---

## 保存後の表示

保存後、以下を表示する。

```text
保存しました:
images/train/xxxx.jpg
labels/train/xxxx.txt
meta/train/xxxx.json
```

また、ステータスバーかログ欄にも出す。

---

## 実装上の希望

既存の細菌解析フォームのコード構造を大きく壊さず、以下のように分離する。

```text
YoloAnnotation
  ClassId
  Label
  RectangleF BoxOriginalPixel
  Confidence
  Source
    "manual"
    "ai"

YoloDatasetExporter
  EnsureDatasetStructure()
  EnsureDataYaml()
  SaveImage()
  SaveYoloLabel()
  SaveMetaJson()

YoloMetaInfo
  ImageId
  ImageFile
  LabelFile
  ClassOrder
  Source
  CreatedAt
  Split
  Specimen
  Magnification
  GramQuality
  DominantFindings
  FreeNote

YoloDominantFinding
  Morphology
  Arrangement
  Distribution
  SpeciesCandidate
  CapsuleAppearance
  ConfidenceNote
```

`System.Text.Json` を使って `meta.json` を保存する。

---

## 既存ONNX推論結果の扱い

既存ONNX推論で box が得られる場合は、アノテーション候補として編集対象に取り込めるとよい。

ただし、自院画像では検出できないことが多いため、今回の実装では AI 検出結果に依存しない。

```text
AI解析結果あり:
  既存 box を編集対象として表示

AI解析結果なし:
  空の状態から手動で box を追加
```

保存時には、AI由来 box も手動修正後の内容として YOLO label に保存する。

---

## 今回実装のゴール

今回のゴールは、機械学習用データを自院画像から作ること。

最低限必要なのは以下。

```text
手動box追加
box削除
4分類ラベル選択
画像保存
YOLO txt保存
meta json保存
data.yaml生成
train/val選択
```

肺炎球菌様などの菌名候補は、YOLOクラスにはせず、`meta.json` に保存する。

---

## 現在使っているONNXモデル情報

現在の ONNX モデルは YOLO11n ベース。

```text
input:
  images [1,3,640,640] float32 RGB 0-1

output:
  output0 [1,8,8400]

classes:
  0: G-cocci
  1: G+cocci
  2: G-bacilli
  3: G+bacilli
```

Clinical Bacteria DataSet のサンプル画像では検出できるが、自院の通常グラム染色画像ではドメイン差が大きい。そのため、自院画像を手動アノテーションして fine tuning する。
