terraform {
  required_version = ">= 1.12"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.40"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.5"
    }
  }

  # Define local backend with an explicit path to avoid relying on the deprecated
  # '-state' CLI flag. azd may still override state location, but this removes
  # the need for '-state' when running Terraform directly.
  backend "local" {
    path = "terraform.tfstate"
  }
}

locals {
  tags = {
    Environment = var.environment_name
    Project     = "wi-sample"
    ManagedBy   = "Terraform"
  }
}
