resource "azurerm_kubernetes_cluster" "main" {
  name                = local.aks_cluster_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  dns_prefix          = local.aks_cluster_name
  kubernetes_version  = var.kubernetes_version

  default_node_pool {
    name                 = "default"
    node_count           = var.node_count
    vm_size              = var.node_vm_size
    os_disk_size_gb      = 30
    os_disk_type         = "Managed"
    vnet_subnet_id       = azurerm_subnet.aks.id
    auto_scaling_enabled = false
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin    = "azure"
    network_policy    = "azure"
    load_balancer_sku = "standard"
    service_cidr      = "10.0.0.0/16"
    dns_service_ip    = "10.0.0.10"
  }

  workload_identity_enabled = true
  oidc_issuer_enabled       = true

  tags = local.tags

  lifecycle {
    ignore_changes = [
      default_node_pool[0].upgrade_settings,
      microsoft_defender
    ]
  }
}

locals {
  # Naming: environment suffix at the end
  aks_cluster_name = var.aks_cluster_name != "" ? var.aks_cluster_name : "aks-wi-sample-${var.environment_name}"
}
