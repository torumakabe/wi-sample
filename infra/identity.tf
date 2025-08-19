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

# フロントエンド用アプリケーション
resource "azuread_application" "frontend" {
  display_name = var.frontend_app_display_name

  api {
    requested_access_token_version = 2
  }
}

resource "azuread_service_principal" "frontend" {
  client_id                    = azuread_application.frontend.client_id
  app_role_assignment_required = false
}

# フロントエンドアプリがAPIのForecast.Readロールを持つように設定
resource "azuread_app_role_assignment" "frontend_to_api" {
  app_role_id         = random_uuid.api_role_forecast_read.result
  principal_object_id = azuread_service_principal.frontend.object_id
  resource_object_id  = azuread_service_principal.api.object_id
}
