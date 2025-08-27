# Workload Identity フェデレーション設定（フロントエンド用・UAMIに付与）
resource "azurerm_federated_identity_credential" "frontend" {
  name                = "kubernetes-federated-credential"
  resource_group_name = azurerm_resource_group.main.name
  issuer              = azurerm_kubernetes_cluster.main.oidc_issuer_url
  subject             = "system:serviceaccount:default:workload-identity-sa"
  audience            = ["api://AzureADTokenExchange"]
  parent_id           = azurerm_user_assigned_identity.frontend.id
}
