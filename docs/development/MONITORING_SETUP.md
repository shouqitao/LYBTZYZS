# 凌隐宝堂监控告警系统部署指南 - UltraThink重构监控架构

## 概述

完整的企业级监控告警系统，包含指标收集、日志聚合、可视化仪表板、多渠道告警通知等功能。

## 架构组件

### 核心监控服务
- **Prometheus** (端口: 9090) - 指标收集和存储
- **Grafana** (端口: 3000) - 可视化仪表板
- **AlertManager** (端口: 9093) - 告警管理和通知

### 系统监控组件
- **Node Exporter** (端口: 9100) - 系统资源指标
- **cAdvisor** (端口: 8080) - 容器监控

### 日志处理栈 (ELK)
- **Elasticsearch** (端口: 9200) - 日志存储和搜索
- **Logstash** (端口: 5044) - 日志处理和转换
- **Kibana** (端口: 5601) - 日志分析界面
- **Filebeat** - 日志收集器

### 应用性能监控 (APM)
- **Jaeger** (端口: 16686) - 分布式追踪
- **Consul** (端口: 8500) - 服务发现

## 快速部署

### 1. 环境准备

确保已安装 Docker 和 Docker Compose：

```bash
docker --version
docker-compose --version
```

### 2. 配置文件检查

确保以下配置文件存在：
- `docker-compose.monitoring.yml` - 主配置文件
- `Docker/prometheus/rules/lybt-alerts.yml` - 告警规则
- `Docker/alertmanager/alertmanager.yml` - 告警配置
- `Docker/grafana/provisioning/dashboards/lybt-overview.json` - 仪表板
- `Docker/elk/logstash/pipeline/lybt.conf` - 日志处理配置

### 3. 启动监控栈

```bash
# 启动完整监控系统
docker-compose -f docker-compose.monitoring.yml up -d

# 检查服务状态
docker-compose -f docker-compose.monitoring.yml ps

# 查看服务日志
docker-compose -f docker-compose.monitoring.yml logs -f prometheus
```

### 4. 验证部署

访问以下URL验证各服务：

- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/LYBT@Grafana2024)
- **AlertManager**: http://localhost:9093
- **Kibana**: http://localhost:5601
- **Jaeger**: http://localhost:16686
- **Consul**: http://localhost:8500

## 配置说明

### Prometheus配置

创建 `Docker/prometheus/prometheus.yml`：

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "/etc/prometheus/rules/*.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093

scrape_configs:
  - job_name: 'lybt-webapi'
    static_configs:
      - targets: ['host.docker.internal:7001']
    metrics_path: '/api/v1/monitoring/metrics'
    scrape_interval: 30s

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']

  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']
```

### 邮件通知配置

编辑 `Docker/alertmanager/alertmanager.yml`：

```yaml
global:
  smtp_smarthost: 'your-smtp-server:587'
  smtp_from: 'your-email@domain.com'
  smtp_auth_username: 'your-email@domain.com'
  smtp_auth_password: 'your-email-password'
```

## 使用指南

### 1. Grafana仪表板

首次登录Grafana：
- 用户名: `admin`
- 密码: `LYBT@Grafana2024`

导入预配置的仪表板：
1. 导航到 Dashboards → Browse
2. 查看 "凌隐宝堂系统总览" 仪表板
3. 配置数据源指向 Prometheus (http://prometheus:9090)

### 2. 告警规则

告警规则已预配置，包括：

- **系统资源告警**: CPU、内存、磁盘使用率
- **API性能告警**: 响应时间、错误率、请求量
- **数据库告警**: 连接数、慢查询、死锁
- **安全告警**: 暴力破解、异常流量、未授权访问

### 3. 日志查看

在Kibana中查看和分析日志：

1. 访问 http://localhost:5601
2. 创建索引模式:
   - `lybt-webapi-*` - Web API日志
   - `lybt-security-*` - 安全审计日志
   - `lybt-database-*` - 数据库日志
   - `lybt-performance-*` - 性能日志

3. 使用Discover功能搜索和过滤日志

### 4. 分布式追踪

在Jaeger中查看请求追踪：
1. 访问 http://localhost:16686
2. 选择服务 `lybt-webapi`
3. 查看请求调用链和性能分析

## 告警通知

### 邮件通知

系统支持多级邮件告警：
- **严重告警**: 立即发送给管理员和运维团队
- **警告告警**: 发送给运维团队
- **信息告警**: 仅记录到系统日志

### 企业微信通知

配置企业微信机器人：
1. 获取企业微信API密钥
2. 更新 `alertmanager.yml` 中的微信配置
3. 测试通知是否正常

### Webhook通知

系统会将告警发送到Web API的webhook端点：
- `/api/v1/monitoring/webhooks/alertmanager` - 一般告警
- `/api/v1/monitoring/webhooks/critical` - 严重告警
- `/api/v1/monitoring/webhooks/security` - 安全告警

## 性能调优

### Elasticsearch优化

```yaml
# 调整JVM堆内存
environment:
  - "ES_JAVA_OPTS=-Xms2g -Xmx2g"
```

### Prometheus数据保留

```yaml
command:
  - '--storage.tsdb.retention.time=30d'
  - '--storage.tsdb.retention.size=10GB'
```

### Grafana性能优化

```yaml
environment:
  - GF_DATABASE_MAX_OPEN_CONN=300
  - GF_DATABASE_MAX_IDLE_CONN=2
```

## 故障排除

### 常见问题

1. **Prometheus无法抓取指标**
   - 检查防火墙设置
   - 验证Web API是否正常运行
   - 确认 `/api/v1/monitoring/metrics` 端点可访问

2. **Grafana无法连接Prometheus**
   - 检查数据源配置
   - 确认容器网络连通性

3. **告警不工作**
   - 检查告警规则语法
   - 验证AlertManager配置
   - 查看AlertManager日志

4. **日志未收集**
   - 检查Filebeat配置
   - 确认日志文件路径正确
   - 验证Logstash处理管道

### 日志调试

```bash
# 查看容器日志
docker logs lybt-prometheus
docker logs lybt-grafana
docker logs lybt-alertmanager
docker logs lybt-elasticsearch
docker logs lybt-kibana

# 进入容器调试
docker exec -it lybt-prometheus sh
docker exec -it lybt-grafana bash
```

## 维护任务

### 定期维护

1. **清理旧数据** (每周)
   ```bash
   # 清理Elasticsearch索引
   curl -X DELETE "localhost:9200/lybt-*-$(date -d '7 days ago' '+%Y.%m.%d')"
   ```

2. **备份配置** (每月)
   ```bash
   # 备份Grafana仪表板
   docker exec lybt-grafana grafana-cli admin export-dashboard
   
   # 备份Prometheus配置
   cp -r Docker/prometheus/ backup/
   ```

3. **更新版本** (按需)
   ```bash
   docker-compose -f docker-compose.monitoring.yml pull
   docker-compose -f docker-compose.monitoring.yml up -d
   ```

## 扩展功能

### 添加新的监控指标

1. 在Web API中添加自定义指标
2. 更新Prometheus配置
3. 创建Grafana面板
4. 配置相应的告警规则

### 集成其他服务

- **Redis监控**: 添加Redis Exporter
- **SQL Server监控**: 配置MSSQL Exporter
- **业务指标**: 自定义业务指标采集

### 多环境部署

复制配置文件并修改：
- `docker-compose.monitoring.dev.yml` - 开发环境
- `docker-compose.monitoring.staging.yml` - 测试环境
- `docker-compose.monitoring.prod.yml` - 生产环境

## 安全考虑

1. **网络隔离**: 使用独立的监控网络
2. **访问控制**: 配置防火墙规则
3. **数据加密**: 启用HTTPS和TLS
4. **密码管理**: 使用环境变量存储敏感信息

## 最佳实践

1. **监控监控系统**: 配置监控组件自身的健康检查
2. **分层告警**: 避免告警风暴，设置告警抑制规则
3. **文档更新**: 及时更新告警手册和故障处理流程
4. **容量规划**: 定期评估存储和计算资源需求

## 联系支持

如遇问题，请查看：
1. 本文档的故障排除部分
2. 各组件官方文档
3. 项目GitHub Issues

---

*此文档是UltraThink重构项目的一部分，最后更新于 $(date '+%Y-%m-%d')*