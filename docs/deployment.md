# デプロイメントガイド

## 前提条件

以下のツールがインストールされていること：
- Azure CLI
- Azure Developer CLI (azd)
- Terraform >= 1.12
- Docker
- kubectl

> **注意**: このプロジェクトではazd のkustomize機能を使用するため、初期設定時に機能を有効化する必要があります。

## デプロイ手順

### 1. 初期設定

```bash
# リポジトリのクローン
git clone https://github.com/torumakabe/wi-sample.git
cd wi-sample

# Azure CLIでログイン
az login

# Azure Developer CLI初期化
azd init

# Azure Developer CLI のkustomize機能を有効化（AKS環境で必要）
azd config set alpha.aks.kustomize on
```

### 2. Terraform変数設定（tfvars サンプルの利用）

```bash
cd infra
# サンプルを配置して編集
cp main.tfvars.sample.json main.tfvars.json
# main.tfvars.json を編集して実際の値を設定
# - environment_name, location
# - azure_subscription_id, azure_tenant_id
# - 既存名を固定したい場合: resource_group_name, aks_cluster_name, acr_name
```

### 3. インフラストラクチャのプロビジョニング

```bash
# Terraformでインフラを作成（手動手順の例）
terraform init
terraform plan
terraform apply

# 出力値を確認（AZURE_* / UPPER_SNAKE_CASE を使用）
terraform output -json > ../outputs.json
```

### 4. 環境変数の設定（azd 推奨）

```bash
# azd の環境から値を取得（KEY=VALUE 形式）
azd env get-values

# 例: 必要に応じて個別に取り出す
export AZURE_TENANT_ID=$(azd env get-values | awk -F= '/^AZURE_TENANT_ID=/{print $2}')
export API_CLIENT_ID=$(azd env get-values | awk -F= '/^API_CLIENT_ID=/{print $2}')
export FRONTEND_CLIENT_ID=$(azd env get-values | awk -F= '/^FRONTEND_CLIENT_ID=/{print $2}')
export API_SCOPE=$(azd env get-values | awk -F= '/^API_SCOPE=/{print $2}')
```

### 5. Azure SQL のユーザー作成（貼り付け実行）

- `azd provision` の後、貼り付け用 SQL が `tmp/sql/create-user-<DB名>.sql` に生成されます（postprovision フック）。
- Azure Portal > 対象 SQL Database > Query editor (preview) を開き、該当 SQL を貼り付け、Entra ID 管理者で実行してください。
- なぜ貼り付け実行か: 多要素認証(MFA)や組織のセキュリティ ポリシーにより、CLI・非対話での DB ユーザー作成が失敗/複雑化するケースがあるためです。Query editor はブラウザで AAD 管理者として安全に実行でき、ローカルの IP 許可や追加ツール導入も不要なため、サンプルとしてシンプルかつ確実な手順にしています。

### 6. アプリケーションのビルドとデプロイ

```bash
# プロジェクトルートに戻る
cd ..

# Azure Developer CLIでデプロイ
azd deploy
```

## ランタイム設定とヘルスチェック

- 設定キーの統一（Kubernetes 環境変数は `__` 区切り）
  - sampleapi: `AzureAd__Instance`, `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__Roles__0..`
  - samplefe: `Api__Endpoint`, `Api__Scope`, `Sql__Server`, `Sql__Database`
- ヘルスチェック
  - sampleapi: `/healthz`（Service 経由、Pod 内部は 8080、Service は 80 → 8080）
  - samplefe: `/healthz`（liveness）と `/readyz`（readiness: 初回 API 成功後に Ready）を公開（Pod 内部 8080）

## 命名の統一

- アセンブリ/名前空間
  - sampleapi: `SampleApi`（AssemblyName/RootNamespace を設定）
  - samplefe: `SampleFe`（AssemblyName/RootNamespace を設定）
- Docker ENTRYPOINT
  - sampleapi: `SampleApi.dll`
  - samplefe: `SampleFe.dll`

## 動作確認

### AKS接続（azd 環境から取得）

```bash
# AKSクラスターへの認証情報取得（azd 環境からRG/AKS名を取得）
RG=$(azd env get-values | awk -F= '/^AZURE_RESOURCE_GROUP=/{print $2}')
AKS=$(azd env get-values | awk -F= '/^AZURE_AKS_CLUSTER_NAME=/{print $2}')
az aks get-credentials --resource-group "$RG" --name "$AKS"

# Podの状態確認
kubectl get pods -n default

# ログ確認
kubectl logs -l app=samplefe -n default
kubectl logs -l app=sampleapi -n default
```

### Azure SQL 接続の確認

```bash
kubectl logs -l app=samplefe -n default | rg "SQL Database connected successfully"
```

### トラブルシューティング

#### azd deploy でkustomize関連エラー

azd がkustomize機能を使用できない場合、以下のエラーが発生することがあります：
```
ERROR: kustomize not supported
```

対処法：
```bash
# kustomize機能が有効化されているか確認
azd config get alpha.aks.kustomize

# 有効化されていない場合は設定
azd config set alpha.aks.kustomize on
```

#### Pod起動失敗

```bash
# Pod詳細確認
kubectl describe pod <pod-name> -n default

# イベント確認
kubectl get events -n default --sort-by='.lastTimestamp'
```

#### 認証エラー

1. Workload Identity設定確認：
```bash
kubectl get sa workload-identity-sa -n default -o yaml
```

2. フェデレーション設定確認：
```bash
az ad app federated-credential list --id <frontend-app-object-id>
```

3. Azure SQL ログイン失敗（Error 18456）

- 症状: `Login failed for user '<token-identified principal>'`
- 主因: 対象 DB にサービス プリンシパルの外部ユーザーが未作成 or 権限不足
- 対処: 本手順の「Azure SQL のユーザー作成（貼り付け実行）」を実施し、再度 `samplefe` のログで接続成功を確認

## クリーンアップ

```bash
# リソースの削除
cd infra
terraform destroy

# Azure Developer CLI環境削除
azd down
```
