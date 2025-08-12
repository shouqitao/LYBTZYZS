# 凌隐宝堂Terraform变量定义 - UltraThink重构生产环境自动化

# ==================== 基础配置 ====================
variable "project_name" {
  description = "项目名称"
  type        = string
  default     = "lybt"
}

variable "environment" {
  description = "环境名称"
  type        = string
  validation {
    condition     = contains(["development", "staging", "production"], var.environment)
    error_message = "环境必须是 development, staging 或 production"
  }
}

variable "location" {
  description = "Azure区域"
  type        = string
  default     = "East Asia"
}

variable "common_tags" {
  description = "通用标签"
  type        = map(string)
  default = {
    Owner       = "LYBT Team"
    CostCenter  = "IT"
    Department  = "Development"
    Application = "LYBT TCM System"
  }
}

# ==================== 网络配置 ====================
variable "vnet_address_space" {
  description = "虚拟网络地址空间"
  type        = list(string)
  default     = ["10.0.0.0/16"]
}

variable "subnet_configs" {
  description = "子网配置"
  type = map(object({
    address_prefixes = list(string)
    service_endpoints = list(string)
    delegation = optional(object({
      name = string
      service_delegation = object({
        name    = string
        actions = list(string)
      })
    }))
  }))
  default = {
    aks = {
      address_prefixes  = ["10.0.1.0/24"]
      service_endpoints = ["Microsoft.Sql", "Microsoft.Storage", "Microsoft.KeyVault"]
    }
    database = {
      address_prefixes  = ["10.0.2.0/24"]
      service_endpoints = ["Microsoft.Sql"]
    }
    redis = {
      address_prefixes  = ["10.0.3.0/24"]
      service_endpoints = ["Microsoft.Cache"]
    }
    gateway = {
      address_prefixes  = ["10.0.4.0/24"]
      service_endpoints = []
    }
    app = {
      address_prefixes  = ["10.0.5.0/24"]
      service_endpoints = ["Microsoft.Web", "Microsoft.Storage", "Microsoft.KeyVault"]
    }
  }
}

# ==================== AKS配置 ====================
variable "kubernetes_version" {
  description = "Kubernetes版本"
  type        = string
  default     = "1.28.3"
}

variable "node_pool_configs" {
  description = "节点池配置"
  type = map(object({
    vm_size               = string
    node_count            = number
    enable_auto_scaling   = bool
    min_count            = number
    max_count            = number
    max_pods             = number
    node_labels          = map(string)
    node_taints          = list(string)
  }))
  default = {
    system = {
      vm_size             = "Standard_D2s_v3"
      node_count          = 3
      enable_auto_scaling = true
      min_count          = 3
      max_count          = 5
      max_pods           = 30
      node_labels = {
        "nodepool" = "system"
      }
      node_taints = []
    }
    application = {
      vm_size             = "Standard_D4s_v3"
      node_count          = 3
      enable_auto_scaling = true
      min_count          = 3
      max_count          = 10
      max_pods           = 50
      node_labels = {
        "nodepool" = "application"
      }
      node_taints = []
    }
  }
}

# ==================== 数据库配置 ====================
variable "database_sku" {
  description = "数据库SKU"
  type        = string
  default     = "S2"
}

variable "database_max_size_gb" {
  description = "数据库最大大小(GB)"
  type        = number
  default     = 250
}

# ==================== Redis配置 ====================
variable "redis_sku_name" {
  description = "Redis SKU名称"
  type        = string
  default     = "Premium"
}

variable "redis_family" {
  description = "Redis系列"
  type        = string
  default     = "P"
}

variable "redis_capacity" {
  description = "Redis容量"
  type        = number
  default     = 1
}

# ==================== 存储配置 ====================
variable "storage_account_tier" {
  description = "存储账户层级"
  type        = string
  default     = "Standard"
}

variable "storage_replication_type" {
  description = "存储复制类型"
  type        = string
  default     = "GRS"
}

# ==================== Application Gateway配置 ====================
variable "app_gateway_sku_name" {
  description = "Application Gateway SKU名称"
  type        = string
  default     = "WAF_v2"
}

variable "app_gateway_sku_tier" {
  description = "Application Gateway SKU层级"
  type        = string
  default     = "WAF_v2"
}

variable "app_gateway_capacity" {
  description = "Application Gateway容量"
  type        = number
  default     = 2
}

# ==================== SSL证书配置 ====================
variable "ssl_certificates" {
  description = "SSL证书配置"
  type = list(object({
    name     = string
    data     = string
    password = string
  }))
  default   = []
  sensitive = true
}

# ==================== 安全配置 ====================
variable "allowed_ip_addresses" {
  description = "允许的IP地址列表"
  type        = list(string)
  default     = []
}

variable "alert_email_addresses" {
  description = "告警邮件地址"
  type        = list(string)
  default     = ["devops@lybt.com"]
}

variable "alert_sms_numbers" {
  description = "告警短信号码"
  type = list(object({
    country_code = string
    phone_number = string
  }))
  default = []
}

# ==================== 备份配置 ====================
variable "backup_retention_days" {
  description = "备份保留天数"
  type        = number
  default     = 30
}

variable "geo_redundant_backup" {
  description = "是否启用地理冗余备份"
  type        = bool
  default     = true
}

# ==================== 自动扩展配置 ====================
variable "autoscale_min_capacity" {
  description = "自动扩展最小容量"
  type        = number
  default     = 2
}

variable "autoscale_max_capacity" {
  description = "自动扩展最大容量"
  type        = number
  default     = 10
}

variable "autoscale_cpu_threshold" {
  description = "CPU自动扩展阈值(%)"
  type        = number
  default     = 70
}

variable "autoscale_memory_threshold" {
  description = "内存自动扩展阈值(%)"
  type        = number
  default     = 80
}