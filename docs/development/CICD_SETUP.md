# 凌隐宝堂CI/CD流水线配置指南 - UltraThink重构

## 概述

本项目实施了完整的CI/CD（持续集成/持续部署）流水线，支持自动化构建、测试、安全扫描和部署。

## 支持的CI/CD平台

### 1. GitHub Actions（推荐）
- **优势**: 与GitHub深度集成，免费额度充足
- **配置文件**: `.github/workflows/`
- **特性**: 并行作业、矩阵构建、秘密管理

### 2. GitLab CI
- **优势**: 支持私有化部署，功能完整
- **配置文件**: `.gitlab-ci.yml`
- **特性**: 内置Docker Registry、Kubernetes集成

### 3. Jenkins
- **优势**: 插件生态丰富，企业广泛使用
- **配置文件**: `Jenkinsfile`
- **特性**: 蓝海界面、共享库、分布式构建

## 流水线架构

```mermaid
graph LR
    A[代码提交] --> B[CI触发]
    B --> C[代码质量检查]
    C --> D[构建]
    D --> E[测试]
    E --> F[安全扫描]
    F --> G[打包]
    G --> H[部署]
    H --> I[验证]
    I --> J[通知]
```

## 流水线阶段详解

### 1. 代码质量检查
- **代码格式化检查**: dotnet-format
- **静态代码分析**: SonarCloud/SonarQube
- **代码规范检查**: StyleCop
- **复杂度分析**: Code Metrics

### 2. 构建阶段
- **多平台构建**: Windows/Linux/macOS
- **多配置构建**: Debug/Release
- **版本管理**: 语义化版本控制
- **依赖缓存**: NuGet包缓存优化

### 3. 测试阶段
- **单元测试**: xUnit + Moq
- **集成测试**: TestContainers
- **性能测试**: NBomber/K6
- **E2E测试**: Cypress/Selenium
- **代码覆盖率**: Codecov/Coveralls

### 4. 安全扫描
- **依赖漏洞扫描**: OWASP Dependency Check
- **容器镜像扫描**: Trivy/Snyk
- **SAST扫描**: Semgrep/CodeQL
- **密钥泄露检测**: GitLeaks
- **许可证合规**: License Finder

### 5. 打包发布
- **Docker镜像**: 多阶段构建优化
- **Helm Chart**: Kubernetes部署包
- **NuGet包**: 组件库发布
- **安装程序**: Inno Setup/WiX

### 6. 部署策略
- **开发环境**: 每次提交自动部署
- **测试环境**: 主分支自动部署
- **生产环境**: 手动审批 + 蓝绿部署

## 环境配置

### GitHub Actions配置

#### 1. 设置Secrets
在GitHub仓库设置中添加以下Secrets：

```yaml
# Docker Hub
DOCKER_USERNAME: your-docker-username
DOCKER_PASSWORD: your-docker-password

# SonarCloud
SONAR_TOKEN: your-sonar-token

# Codecov
CODECOV_TOKEN: your-codecov-token

# 部署凭证
KUBE_CONFIG_DEV: base64编码的kubeconfig
KUBE_CONFIG_STAGING: base64编码的kubeconfig
KUBE_CONFIG_PROD: base64编码的kubeconfig

# 通知
SLACK_WEBHOOK: your-slack-webhook-url
WECHAT_WEBHOOK: your-wechat-webhook-url
```

#### 2. 启用Actions
1. 进入仓库Settings
2. 选择Actions → General
3. 启用"Allow all actions and reusable workflows"

### GitLab CI配置

#### 1. 配置CI/CD变量
在项目设置中添加变量：

```bash
# Docker Registry
CI_REGISTRY_USER: your-registry-username
CI_REGISTRY_PASSWORD: your-registry-password

# Kubernetes
KUBE_CONFIG_DEV: base64编码的kubeconfig
KUBE_CONFIG_STAGING: base64编码的kubeconfig
KUBE_CONFIG_PROD: base64编码的kubeconfig

# 通知
WEBHOOK_URL: your-webhook-url
```

#### 2. 配置Runner
```bash
# 安装GitLab Runner
curl -L --output /usr/local/bin/gitlab-runner \
  "https://gitlab-runner-downloads.s3.amazonaws.com/latest/binaries/gitlab-runner-linux-amd64"

chmod +x /usr/local/bin/gitlab-runner

# 注册Runner
gitlab-runner register \
  --url https://gitlab.lybt.com \
  --registration-token YOUR_TOKEN \
  --executor docker \
  --docker-image mcr.microsoft.com/dotnet/sdk:8.0
```

### Jenkins配置

#### 1. 安装插件
必需插件列表：
- Pipeline
- Git
- Docker Pipeline
- Kubernetes
- SonarQube Scanner
- Slack Notification
- Email Extension

#### 2. 配置凭证
在Jenkins凭证管理中添加：
```groovy
// Docker凭证
docker-credentials: Username with password

// Kubernetes配置
DEV_KUBECONFIG: Secret file
STAGING_KUBECONFIG: Secret file
PROD_KUBECONFIG: Secret file

// SonarQube
SONAR_TOKEN: Secret text
```

#### 3. 配置共享库
```groovy
// 在Jenkins系统设置中配置
@Library('lybt-shared-library@main') _
```

## 本地开发流程

### 1. 提交前检查
```bash
# 运行本地测试
dotnet test

# 检查代码格式
dotnet format --verify-no-changes

# 安全扫描
dotnet list package --vulnerable
```

### 2. 使用pre-commit钩子
```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: dotnet-format
        name: dotnet format
        entry: dotnet format --verify-no-changes
        language: system
        files: \.(cs|csproj)$
      
      - id: dotnet-test
        name: dotnet test
        entry: dotnet test
        language: system
        pass_filenames: false
```

### 3. 分支策略
```
main        → 生产环境
develop     → 开发环境  
release/*   → 测试环境
feature/*   → 功能开发
hotfix/*    → 紧急修复
```

## 部署流程

### 开发环境部署
```bash
# 自动触发
git push origin develop

# 手动触发
gh workflow run cd-deploy.yml -f environment=development
```

### 测试环境部署
```bash
# 创建发布分支
git checkout -b release/v1.2.0
git push origin release/v1.2.0

# 手动触发
gh workflow run cd-deploy.yml -f environment=staging -f version=v1.2.0
```

### 生产环境部署
```bash
# 创建标签
git tag v1.2.0
git push origin v1.2.0

# 需要审批
gh workflow run cd-deploy.yml -f environment=production -f version=v1.2.0
```

## 监控和通知

### 构建状态徽章
在README.md中添加：
```markdown
![Backend CI](https://github.com/lybt/lybt/workflows/Backend%20CI%20Pipeline/badge.svg)
![Frontend CI](https://github.com/lybt/lybt/workflows/Frontend%20CI%20Pipeline/badge.svg)
![Security Scan](https://github.com/lybt/lybt/workflows/Security%20Scanning%20Pipeline/badge.svg)
```

### 通知配置

#### Slack通知
```javascript
// Slack Webhook配置
{
  "text": "构建通知",
  "attachments": [{
    "color": "good",
    "fields": [
      {"title": "项目", "value": "LYBT"},
      {"title": "分支", "value": "main"},
      {"title": "状态", "value": "成功"}
    ]
  }]
}
```

#### 企业微信通知
```javascript
// 企业微信机器人配置
{
  "msgtype": "markdown",
  "markdown": {
    "content": "## 构建通知\n> 项目: LYBT\n> 状态: 成功"
  }
}
```

## 故障排除

### 常见问题

#### 1. Docker构建失败
```bash
# 清理Docker缓存
docker system prune -a

# 检查Dockerfile
docker build -t test -f Dockerfile .
```

#### 2. 测试失败
```bash
# 本地运行测试
dotnet test --logger "console;verbosity=detailed"

# 检查测试数据库
docker-compose -f docker-compose.test.yml up
```

#### 3. 部署失败
```bash
# 检查Kubernetes连接
kubectl config view
kubectl get pods -n lybt-prod

# 查看部署日志
kubectl logs deployment/lybt-webapi -n lybt-prod
```

#### 4. 安全扫描问题
```bash
# 更新依赖
dotnet add package [PackageName] --version [LatestVersion]

# 忽略误报
# 在.trivyignore中添加
CVE-2021-12345
```

## 性能优化

### 1. 构建优化
- 使用构建缓存
- 并行执行任务
- 增量构建
- 多阶段Docker构建

### 2. 测试优化
- 并行测试执行
- 测试分片
- 只运行受影响的测试
- 使用测试容器

### 3. 部署优化
- 滚动更新
- 蓝绿部署
- 金丝雀发布
- 特性开关

## 最佳实践

### 1. 版本管理
- 使用语义化版本
- 自动生成更新日志
- 标记重要版本
- 保留构建产物

### 2. 安全实践
- 不在代码中硬编码密钥
- 使用Secret管理
- 定期更新依赖
- 执行安全扫描

### 3. 监控实践
- 监控构建时间
- 跟踪测试覆盖率
- 分析失败原因
- 优化瓶颈

### 4. 文档实践
- 记录部署流程
- 更新配置说明
- 维护故障手册
- 分享最佳实践

## 工具链

### 开发工具
- **IDE**: Visual Studio 2022 / VS Code
- **版本控制**: Git
- **包管理**: NuGet / npm

### CI/CD工具
- **构建**: MSBuild / dotnet CLI
- **测试**: xUnit / Moq / TestContainers
- **扫描**: SonarQube / Trivy / Semgrep

### 部署工具
- **容器**: Docker / Podman
- **编排**: Kubernetes / Docker Compose
- **配置**: Helm / Kustomize

### 监控工具
- **指标**: Prometheus / Grafana
- **日志**: ELK Stack
- **追踪**: Jaeger
- **告警**: AlertManager

## 联系支持

- **CI/CD问题**: devops@lybt.com
- **紧急支持**: 13800138000
- **文档**: https://docs.lybt.com/cicd

---

*此文档是UltraThink重构项目的一部分，最后更新于 2025-08-12*