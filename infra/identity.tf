# 現在のAzure ADテナント情報を取得
data "azuread_client_config" "current" {}

# API用アプリケーション
resource "azuread_application" "api" {
  display_name    = var.api_app_display_name
  identifier_uris = ["api://wi-sample-${var.environment_name}"]

  api {
    requested_access_token_version = 2
  }

  app_role {
    allowed_member_types = ["Application", "User"]
    description          = "Read weather forecast data"
    display_name         = "Forecast.Read"
    enabled              = true
    id                   = random_uuid.api_role_forecast_read.result
    value                = "Forecast.Read"
  }
}

resource "random_uuid" "api_role_forecast_read" {}

resource "azuread_service_principal" "api" {
  client_id                    = azuread_application.api.client_id
  app_role_assignment_required = false
}

# フロントエンド用 ユーザー割り当てマネージドID（UAMI）
resource "azurerm_user_assigned_identity" "frontend" {
  name                = "uami-wi-sample-${var.environment_name}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tags                = local.tags
}

# UAMI（Service Principal）にAPIのForecast.Readロールを割り当て
resource "azuread_app_role_assignment" "uami_to_api" {
  app_role_id         = random_uuid.api_role_forecast_read.result
  principal_object_id = azurerm_user_assigned_identity.frontend.principal_id
  resource_object_id  = azuread_service_principal.api.object_id
}
