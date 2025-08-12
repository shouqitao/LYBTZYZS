# 凌隐宝堂生产环境配置 - UltraThink重构生产环境自动化

# 基础配置
project_name = "lybt"
environment  = "production"
location     = "East Asia"

# 网络配置
vnet_address_space = ["10.0.0.0/16"]

subnet_configs = {
  aks = {
    address_prefixes  = ["10.0.1.0/23"]
    service_endpoints = ["Microsoft.Sql", "Microsoft.Storage", "Microsoft.KeyVault", "Microsoft.ContainerRegistry"]
  }
  database = {
    address_prefixes  = ["10.0.3.0/24"]
    service_endpoints = ["Microsoft.Sql"]
  }
  redis = {
    address_prefixes  = ["10.0.4.0/24"]
    service_endpoints = ["Microsoft.Cache"]
  }
  gateway = {
    address_prefixes  = ["10.0.5.0/24"]
    service_endpoints = []
  }
  app = {
    address_prefixes  = ["10.0.6.0/24"]
    service_endpoints = ["Microsoft.Web", "Microsoft.Storage", "Microsoft.KeyVault"]
  }
}

# AKS配置 - 生产环境
kubernetes_version = "1.28.3"

node_pool_configs = {
  system = {
    vm_size             = "Standard_D4s_v3"
    node_count          = 3
    enable_auto_scaling = true
    min_count          = 3
    max_count          = 5
    max_pods           = 50
    node_labels = {
      "nodepool" = "system"
      "environment" = "production"
    }
    node_taints = ["CriticalAddonsOnly=true:NoSchedule"]
  }
  application = {
    vm_size             = "Standard_D8s_v3"
    node_count          = 5
    enable_auto_scaling = true
    min_count          = 5
    max_count          = 20
    max_pods           = 100
    node_labels = {
      "nodepool" = "application"
      "environment" = "production"
      "workload" = "api"
    }
    node_taints = []
  }
  monitoring = {
    vm_size             = "Standard_D4s_v3"
    node_count          = 2
    enable_auto_scaling = true
    min_count          = 2
    max_count          = 4
    max_pods           = 30
    node_labels = {
      "nodepool" = "monitoring"
      "environment" = "production"
    }
    node_taints = ["monitoring=true:NoSchedule"]
  }
}

# 数据库配置 - 生产环境高性能
database_sku         = "P4"
database_max_size_gb = 1024

# Redis配置 - 生产环境高可用
redis_sku_name = "Premium"
redis_family   = "P"
redis_capacity = 3

# 存储配置 - 地理冗余
storage_account_tier     = "Premium"
storage_replication_type = "RAGRS"

# Application Gateway配置 - WAF保护
app_gateway_sku_name  = "WAF_v2"
app_gateway_sku_tier  = "WAF_v2"
app_gateway_capacity  = 3

# 安全配置
allowed_ip_addresses = [
  "203.0.113.0/24",  # 办公室IP范围
  "198.51.100.0/24", # VPN出口IP
  "192.0.2.0/24"     # 备用办公室
]

alert_email_addresses = [
  "devops@lybt.com",
  "oncall@lybt.com",
  "management@lybt.com"
]

alert_sms_numbers = [
  {
    country_code = "86"
    phone_number = "13800138000"
  },
  {
    country_code = "86"
    phone_number = "13900139000"
  }
]

# 备份配置 - 长期保留
backup_retention_days = 90
geo_redundant_backup  = true

# 自动扩展配置 - 激进扩展策略
autoscale_min_capacity     = 3
autoscale_max_capacity     = 30
autoscale_cpu_threshold    = 60
autoscale_memory_threshold = 70

# 标签
common_tags = {
  Owner       = "LYBT Production Team"
  CostCenter  = "Production"
  Department  = "Operations"
  Application = "LYBT TCM System"
  Environment = "Production"
  Criticality = "High"
  Compliance  = "HIPAA"
  DataClass   = "Confidential"
}