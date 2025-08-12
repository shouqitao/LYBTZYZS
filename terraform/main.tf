# 凌隐宝堂基础设施配置 - UltraThink重构生产环境自动化
terraform {
  required_version = ">= 1.5.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.70.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.23.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.11.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.5.0"
    }
  }
  
  backend "azurerm" {
    resource_group_name  = "lybt-terraform-state"
    storage_account_name = "lybtterraformstate"
    container_name       = "tfstate"
    key                  = "prod.terraform.tfstate"
  }
}

# Azure Provider配置
provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = false
    }
    resource_group {
      prevent_deletion_if_contains_resources = true
    }
  }
}

# 数据源
data "azurerm_client_config" "current" {}

# ==================== 资源组 ====================
resource "azurerm_resource_group" "main" {
  name     = "${var.project_name}-${var.environment}-rg"
  location = var.location
  
  tags = local.common_tags
}

# ==================== 网络配置 ====================
module "network" {
  source = "./modules/network"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  vnet_address_space = var.vnet_address_space
  subnet_configs     = var.subnet_configs
  
  tags = local.common_tags
}

# ==================== AKS集群 ====================
module "aks" {
  source = "./modules/aks"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  kubernetes_version     = var.kubernetes_version
  node_pool_configs      = var.node_pool_configs
  vnet_subnet_id        = module.network.aks_subnet_id
  
  enable_auto_scaling   = true
  min_node_count        = 3
  max_node_count        = 10
  
  tags = local.common_tags
}

# ==================== SQL数据库 ====================
module "sql_database" {
  source = "./modules/sql_database"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  sql_server_version    = "12.0"
  database_sku          = var.database_sku
  max_size_gb           = var.database_max_size_gb
  
  backup_retention_days = var.environment == "production" ? 35 : 7
  enable_geo_redundancy = var.environment == "production"
  
  vnet_subnet_id        = module.network.database_subnet_id
  
  tags = local.common_tags
}

# ==================== Redis缓存 ====================
module "redis" {
  source = "./modules/redis"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  cache_sku_name     = var.redis_sku_name
  cache_family       = var.redis_family
  cache_capacity     = var.redis_capacity
  
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  
  vnet_subnet_id     = module.network.redis_subnet_id
  
  tags = local.common_tags
}

# ==================== 存储账户 ====================
module "storage" {
  source = "./modules/storage"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  account_tier              = var.storage_account_tier
  account_replication_type  = var.storage_replication_type
  
  enable_blob_encryption    = true
  enable_file_encryption    = true
  enable_queue_encryption   = true
  enable_table_encryption   = true
  
  containers = [
    {
      name        = "backups"
      access_type = "private"
    },
    {
      name        = "logs"
      access_type = "private"
    },
    {
      name        = "documents"
      access_type = "private"
    }
  ]
  
  network_rules = {
    default_action             = "Deny"
    bypass                     = ["AzureServices"]
    ip_rules                   = var.allowed_ip_addresses
    virtual_network_subnet_ids = [module.network.app_subnet_id]
  }
  
  tags = local.common_tags
}

# ==================== Key Vault ====================
module "key_vault" {
  source = "./modules/key_vault"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  tenant_id = data.azurerm_client_config.current.tenant_id
  
  sku_name = "standard"
  
  enabled_for_deployment          = true
  enabled_for_disk_encryption     = true
  enabled_for_template_deployment = true
  
  purge_protection_enabled   = var.environment == "production"
  soft_delete_retention_days = var.environment == "production" ? 90 : 7
  
  network_acls = {
    default_action             = "Deny"
    bypass                     = "AzureServices"
    ip_rules                   = var.allowed_ip_addresses
    virtual_network_subnet_ids = [module.network.app_subnet_id]
  }
  
  access_policies = [
    {
      tenant_id = data.azurerm_client_config.current.tenant_id
      object_id = data.azurerm_client_config.current.object_id
      
      secret_permissions = [
        "Get", "List", "Set", "Delete", "Purge", "Recover"
      ]
      
      certificate_permissions = [
        "Get", "List", "Create", "Delete", "Import"
      ]
      
      key_permissions = [
        "Get", "List", "Create", "Delete", "Encrypt", "Decrypt"
      ]
    }
  ]
  
  secrets = {
    "db-connection-string" = module.sql_database.connection_string
    "redis-connection-string" = module.redis.connection_string
    "storage-connection-string" = module.storage.connection_string
    "jwt-secret" = random_password.jwt_secret.result
  }
  
  tags = local.common_tags
}

# ==================== Application Gateway ====================
module "app_gateway" {
  source = "./modules/app_gateway"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  subnet_id = module.network.gateway_subnet_id
  
  sku_name     = var.app_gateway_sku_name
  sku_tier     = var.app_gateway_sku_tier
  sku_capacity = var.app_gateway_capacity
  
  enable_waf            = var.environment == "production"
  waf_mode              = "Prevention"
  waf_rule_set_type     = "OWASP"
  waf_rule_set_version  = "3.2"
  
  backend_pools = [
    {
      name = "aks-backend"
      fqdns = [module.aks.ingress_fqdn]
    }
  ]
  
  ssl_certificates = var.ssl_certificates
  
  enable_autoscale = true
  min_capacity     = 2
  max_capacity     = 10
  
  tags = local.common_tags
}

# ==================== CDN配置 ====================
module "cdn" {
  source = "./modules/cdn"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = "global"
  resource_group_name = azurerm_resource_group.main.name
  
  cdn_sku = var.environment == "production" ? "Standard_Microsoft" : "Standard_Verizon"
  
  origins = [
    {
      name      = "app-gateway"
      host_name = module.app_gateway.public_ip_address
    },
    {
      name      = "storage"
      host_name = module.storage.primary_blob_host
    }
  ]
  
  caching_rules = [
    {
      query_string_caching_behaviour = "IgnoreQueryString"
      cache_duration                 = "1.00:00:00"
      path_pattern                   = "/assets/*"
    },
    {
      query_string_caching_behaviour = "UseQueryString"
      cache_duration                 = "0.00:30:00"
      path_pattern                   = "/api/*"
    }
  ]
  
  enable_compression = true
  enable_https_only  = true
  
  tags = local.common_tags
}

# ==================== 监控和日志 ====================
module "monitoring" {
  source = "./modules/monitoring"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  log_analytics_sku           = "PerGB2018"
  log_analytics_retention_days = var.environment == "production" ? 90 : 30
  
  application_insights_type = "web"
  
  action_group_email_receivers = var.alert_email_addresses
  action_group_sms_receivers   = var.alert_sms_numbers
  
  metric_alerts = [
    {
      name        = "high-cpu-usage"
      description = "Alert when CPU usage is high"
      severity    = 2
      frequency   = "PT5M"
      window_size = "PT15M"
      
      criteria = {
        metric_namespace = "Microsoft.Compute/virtualMachineScaleSets"
        metric_name      = "Percentage CPU"
        aggregation      = "Average"
        operator         = "GreaterThan"
        threshold        = 80
      }
    },
    {
      name        = "database-connection-failed"
      description = "Alert when database connection fails"
      severity    = 1
      frequency   = "PT1M"
      window_size = "PT5M"
      
      criteria = {
        metric_namespace = "Microsoft.Sql/servers/databases"
        metric_name      = "connection_failed"
        aggregation      = "Total"
        operator         = "GreaterThan"
        threshold        = 5
      }
    }
  ]
  
  tags = local.common_tags
}

# ==================== 备份和灾难恢复 ====================
module "backup" {
  source = "./modules/backup"
  
  project_name        = var.project_name
  environment         = var.environment
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  
  recovery_vault_sku = "Standard"
  
  backup_policies = [
    {
      name                = "daily-backup"
      backup_frequency    = "Daily"
      backup_time         = "23:00"
      retention_daily     = 7
      retention_weekly    = 4
      retention_monthly   = 12
      retention_yearly    = 3
    }
  ]
  
  protected_items = [
    {
      type        = "SqlDatabase"
      resource_id = module.sql_database.database_id
      policy_name = "daily-backup"
    }
  ]
  
  enable_geo_redundancy = var.environment == "production"
  
  tags = local.common_tags
}

# ==================== 自动扩展配置 ====================
resource "azurerm_monitor_autoscale_setting" "aks" {
  name                = "${var.project_name}-${var.environment}-autoscale"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  target_resource_id  = module.aks.cluster_id
  
  profile {
    name = "default"
    
    capacity {
      default = 3
      minimum = 3
      maximum = 10
    }
    
    rule {
      metric_trigger {
        metric_name        = "node_cpu_usage_percentage"
        metric_resource_id = module.aks.cluster_id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT5M"
        time_aggregation   = "Average"
        operator           = "GreaterThan"
        threshold          = 70
      }
      
      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT5M"
      }
    }
    
    rule {
      metric_trigger {
        metric_name        = "node_cpu_usage_percentage"
        metric_resource_id = module.aks.cluster_id
        time_grain         = "PT1M"
        statistic          = "Average"
        time_window        = "PT5M"
        time_aggregation   = "Average"
        operator           = "LessThan"
        threshold          = 30
      }
      
      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT10M"
      }
    }
  }
  
  notification {
    email {
      send_to_subscription_administrator    = true
      send_to_subscription_co_administrators = true
      custom_emails                         = var.alert_email_addresses
    }
  }
  
  tags = local.common_tags
}

# ==================== 随机密码生成 ====================
resource "random_password" "jwt_secret" {
  length  = 64
  special = true
  upper   = true
  lower   = true
  numeric = true
}

resource "random_password" "admin_password" {
  length  = 16
  special = true
  upper   = true
  lower   = true
  numeric = true
}

# ==================== 本地变量 ====================
locals {
  common_tags = merge(
    var.common_tags,
    {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "Terraform"
      CreatedAt   = timestamp()
    }
  )
}

# ==================== 输出 ====================
output "resource_group_name" {
  value       = azurerm_resource_group.main.name
  description = "资源组名称"
}

output "aks_cluster_name" {
  value       = module.aks.cluster_name
  description = "AKS集群名称"
}

output "aks_kubeconfig" {
  value       = module.aks.kubeconfig
  sensitive   = true
  description = "AKS集群kubeconfig"
}

output "sql_server_fqdn" {
  value       = module.sql_database.server_fqdn
  description = "SQL Server FQDN"
}

output "redis_hostname" {
  value       = module.redis.hostname
  description = "Redis主机名"
}

output "key_vault_uri" {
  value       = module.key_vault.vault_uri
  description = "Key Vault URI"
}

output "app_gateway_public_ip" {
  value       = module.app_gateway.public_ip_address
  description = "Application Gateway公共IP"
}

output "cdn_endpoint" {
  value       = module.cdn.endpoint_hostname
  description = "CDN端点"
}

output "monitoring_workspace_id" {
  value       = module.monitoring.log_analytics_workspace_id
  description = "Log Analytics工作区ID"
}