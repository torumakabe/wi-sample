variable "environment_name" {
  description = "環境名（例: dev, staging, prod）"
  type        = string
}

variable "location" {
  description = "Azureリージョン"
  type        = string
  default     = "japaneast"
}

variable "azure_subscription_id" {
  description = "Azure サブスクリプションID"
  type        = string
  sensitive   = true
}

variable "azure_tenant_id" {
  description = "Azure テナントID"
  type        = string
  sensitive   = true
}

variable "resource_group_name" {
  description = "リソースグループ名"
  type        = string
  default     = ""
}

variable "aks_cluster_name" {
  description = "AKSクラスター名"
  type        = string
  default     = ""
}

variable "acr_name" {
  description = "Azure Container Registry名"
  type        = string
  default     = ""
}

variable "api_app_display_name" {
  description = "API用Entra IDアプリケーション表示名"
  type        = string
  default     = "wi-sample-api"
}

variable "frontend_app_display_name" {
  description = "フロントエンド用Entra IDアプリケーション表示名"
  type        = string
  default     = "wi-sample-frontend"
}

variable "kubernetes_version" {
  description = "Kubernetesバージョン"
  type        = string
  default     = "1.33"
}

variable "node_count" {
  description = "AKSノード数"
  type        = number
  default     = 2
}

variable "node_vm_size" {
  description = "AKSノードVMサイズ"
  type        = string
  default     = "Standard_B2ms"
}
