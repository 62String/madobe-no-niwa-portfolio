# 窓辺の庭 | Unity実装ポートフォリオ

**育てた植物を収穫し、図鑑に記録する。その一連の体験をUI・ゲーム状態・保存処理につなぐ実装を担当しました。**

| 作品 | チーム・担当者 | 開発環境 |
| --- | --- | --- |
| 植物育成ゲーム『窓辺の庭』<br/>창가의 정원 / WindowGarden<br/>iOS / Android向け | Team.Garden・3人チーム<br/>ユ・ギヒョン / 62String | Unity 6000.3.11f1 / C#<br/>uGUI・TextMeshPro<br/>Input System・Coroutine・JSON |

## 担当機能のつながり

```mermaid
flowchart LR
    H["収穫<br/>次世代の種・収穫記録"] --> S["保存・復元<br/>SaveManager"]
    D["枯死・復活<br/>初回回生の使用状態"] --> S
    S --> C["植物図鑑<br/>履歴からカード・詳細表示"]
    S <-->|"SaveData / JSON"| F[("ローカルファイル")]
```

> 機能間のデータ連携を示した概要図です。クラス間の正確な呼び出し順は詳細資料に記載しています。

## 担当範囲とコード

| 機能 | 実装のポイント | ソース |
| --- | --- | --- |
| **収穫** | 収穫条件、次世代の種付与、履歴登録、ワールド空間からUIへ移動する演出 | [Controller](Scripts/Harvest/HarvestController.cs) / [Popup](Scripts/Harvest/HarvestPopupUI.cs) |
| **枯死・復活** | イベント購読、初回回生、アイテム・所持金による復活分岐 | [DeathController](Scripts/DeathRevive/DeathController.cs) |
| **植物図鑑** | 収穫履歴による抽出、ページ生成、最新記録を使った詳細表示 | [Collection](Scripts/Encyclopedia/CollectionUI.cs) / [Card](Scripts/Encyclopedia/PlantCardUI.cs) / [Detail](Scripts/Encyclopedia/DetailPanelUI.cs) |
| **保存・復元** | 各機能の状態集約、JSON保存、デモ／リリースの保存先分離 | [Manager](Scripts/SaveLoad/SaveManager.cs) / [Data](Scripts/SaveLoad/SaveData.cs) |

## 設計・改善を読む

**[実装解説：処理フロー・分岐・データ構造](docs/Implementation_ja.md)**

| 確認したい内容 | 詳細資料 |
| --- | --- |
| 収穫確定後、何をどの順番で処理するか | [収穫シーケンス](docs/Implementation_ja.md#harvest) |
| 回生・アイテム使用・購入をどう分岐するか | [枯死・復活フロー](docs/Implementation_ja.md#revive) |
| 植物の定義とプレイ履歴をどう組み合わせるか | [図鑑のデータフロー](docs/Implementation_ja.md#encyclopedia) |
| 保存・復元の責務と現在の制約は何か | [保存・復元](docs/Implementation_ja.md#save-load) |
| 不具合やテスト環境の混在をどう改善したか | [改善事例と検証範囲](docs/Implementation_ja.md#improvements) |

## 掲載範囲

- **製品コード8ファイルの抜粋**です。共通マネージャー・シーン・Prefab・画像は含まないため、単体ではコンパイル・実行できません。
- 初期実装後のチームメンバーによる修正を含みます。[担当・共同修正の内訳](docs/Implementation_ja.md#contributions)を明記しています。
- 掲載基準は元プロジェクトのコミット `a8bde20`。保存安定化の追加実装はまだ含めていません。
- 実機画面・動画と開発期間は確認後に追記します。公開前にチーム内でコード・素材の公開範囲を確認します。
- 再利用を許諾するライセンスは設定していません。
