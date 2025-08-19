# Workload Identity フェデレーション設定（フロントエンド用）
resource "azuread_application_federated_identity_credential" "frontend" {
  application_id = azuread_application.frontend.id
  display_name   = "kubernetes-federated-credential"
  description    = "Kubernetes service account federated credential"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = azurerm_kubernetes_cluster.main.oidc_issuer_url
  subject        = "system:serviceaccount:default:workload-identity-sa"
}