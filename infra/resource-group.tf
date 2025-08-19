resource "azurerm_resource_group" "main" {
  name     = local.resource_group_name
  location = var.location
  tags     = local.tags
}

locals {
  # Naming: put environment suffix at the end per Azure guidelines
  resource_group_name = var.resource_group_name != "" ? var.resource_group_name : "rg-wi-sample-${var.environment_name}"
}
