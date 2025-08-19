resource "azurerm_container_registry" "main" {
  name                = local.acr_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = false

  tags = local.tags
}

locals {
  # Naming: environment suffix at the end; ACR requires alphanumeric and starts with letter
  acr_name = var.acr_name != "" ? var.acr_name : "acrwisample${replace(var.environment_name, "-", "")}"
}

# AKSがACRからイメージをプルできるようにロール割り当て
resource "azurerm_role_assignment" "aks_acr" {
  principal_id                     = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.main.id
  skip_service_principal_aad_check = true
}
