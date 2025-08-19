// Note: To avoid collisions when tools normalize keys, only AZURE_* and UPPER_SNAKE_CASE
// outputs are published. Duplicate/lowercase aliases have been removed.

# Azure Developer CLI標準出力
output "AZURE_CONTAINER_REGISTRY_ENDPOINT" {
  description = "Container Registry Endpoint for AZD"
  value       = azurerm_container_registry.main.login_server
}

output "AZURE_CONTAINER_REGISTRY_NAME" {
  description = "Container Registry Name for AZD"
  value       = azurerm_container_registry.main.name
}

output "AZURE_RESOURCE_GROUP" {
  description = "Resource Group Name for AZD"
  value       = azurerm_resource_group.main.name
}

output "AZURE_AKS_CLUSTER_NAME" {
  description = "AKS Cluster Name for AZD"
  value       = azurerm_kubernetes_cluster.main.name
}

# 環境変数として必要な出力
output "AZURE_TENANT_ID" {
  description = "Azure Tenant ID"
  # Auto-detect from the authenticated context (AzureAD).
  # This avoids relying on pre-set env and keeps azd env in sync automatically.
  value       = data.azuread_client_config.current.tenant_id
  sensitive   = true
}

output "API_CLIENT_ID" {
  description = "API Application Client ID"
  value       = azuread_application.api.client_id
  sensitive   = true
}

output "FRONTEND_CLIENT_ID" {
  description = "Frontend Application Client ID"
  value       = azuread_application.frontend.client_id
  sensitive   = true
}

output "API_SCOPE" {
  description = "API Scope"
  value       = "${tolist(azuread_application.api.identifier_uris)[0]}/.default"
}

output "SQL_SERVER_FQDN" {
  description = "SQL Server Fully Qualified Domain Name"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "SQL_DATABASE_NAME" {
  description = "SQL Database Name"
  value       = azurerm_mssql_database.main.name
}

output "FRONTEND_SERVICE_PRINCIPAL_NAME" {
  description = "Frontend Service Principal Display Name for SQL User"
  value       = azuread_service_principal.frontend.display_name
}

// Removed unnecessary SP object id output for simplicity
