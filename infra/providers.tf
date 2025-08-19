provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }

  subscription_id = var.azure_subscription_id
}

provider "azuread" {
  tenant_id = var.azure_tenant_id
}

