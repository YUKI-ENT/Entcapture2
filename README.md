# ENTcapture2

ENTcaptureの次期版です。現行版を変更せず、.NET 10とWinFormsで段階的に再構築します。

## 現在の範囲

- 可変数プリセットのモデル
- JSON形式の設定保存
- 頻用プリセット5件と「その他」選択UI
- プリセットの追加、複製、削除、並べ替え
- カメラプレビューと録画機能を追加するための画面領域

## 開発環境

- .NET 10 SDK
- Visual Studio 2026または.NET 10対応Visual Studio
- Windows 10/11 x64

## ビルド

```powershell
dotnet build .\ENTcapture2.sln
```

## インストーラ作成

Inno Setup 6と、Windows証明書ストア内のコード署名証明書を使用します。

```powershell
.\build\build_installer.ps1
```

処理内容:

1. .NET 10 x64自己完結型で発行
2. ENTcapture2のEXE/DLLへ署名
3. Inno Setupインストーラ作成
4. インストーラとアンインストーラへ署名
5. 署名検証とSHA-256ファイル作成

署名を行わず動作確認する場合:

```powershell
.\build\build_installer.ps1 -SkipSign
```
