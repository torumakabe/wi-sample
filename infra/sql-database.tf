# Get current user information
data "azurerm_client_config" "current" {}

data "azuread_user" "current" {
  object_id = data.azurerm_client_config.current.object_id
}

# Azure SQL Database Server
resource "azurerm_mssql_server" "main" {
  name                         = "sql-wi-sample-${var.environment_name}"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  minimum_tls_version          = "1.2"

  azuread_administrator {
    login_username              = data.azuread_user.current.user_principal_name
    object_id                   = data.azuread_user.current.object_id
    tenant_id                   = data.azuread_client_config.current.tenant_id
    azuread_authentication_only = true
  }

  public_network_access_enabled = true
}

# Azure SQL Database
resource "azurerm_mssql_database" "main" {
  name           = "sqldb-wi-sample-${var.environment_name}"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  max_size_gb    = 2
  sku_name       = "Basic"
  zone_redundant = false
}

# Allow Azure services
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAllWindowsAzureIps"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

