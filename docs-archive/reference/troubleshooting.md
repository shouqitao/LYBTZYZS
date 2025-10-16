# 快速问题解决

**更新时间**: 2025-10-15 18:11:07
**条目数量**: 25 个
**使用说明**: 快速查找常用解决方案，点击目录直接跳转

## 📋 快速目录

1. [## ## 4. 结论与建议](#1-##-##-4.-结论与建议)
2. [为杜绝“为了技术而技术”的问题，并确保所有技术决策都服务于项目的长远健康，本技术路线严格遵循以下核心...](#2-为杜绝“为了技术而技术”的问题，并确保所有技术决策都服务于项目的长远健康，本技术路线严格遵循以下核心...)
3. [## #### 内部文档](#3-##-####-内部文档)
4. [```markdown](#4-```markdown)
5. [## #### 3. WPF / Prism MVVM 规范（🟡 建议问题）](#5-##-####-3.-wpf-/-prism-mvvm-规范（🟡-建议问题）)
6. [foreach (var error in validationResult.Errors)](#6-foreach-(var-error-in-validationresult.errors))
7. [```powershell](#7-```powershell)
8. [```yaml](#8-```yaml)
9. [```yaml](#9-```yaml)
10. [```yaml](#10-```yaml)
11. [echo "错误: kubectl 未安装或不在 PATH 中"](#11-echo-"错误:-kubectl-未安装或不在-path-中")
12. [- alert: HighErrorRate](#12---alert:-higherrorrate)
13. [□ 异常处理完整，错误返回统一](#13-□-异常处理完整，错误返回统一)
14. [```csharp](#14-```csharp)
15. [_logger.LogError(ex, "数据库监控失败");](#15-_logger.logerror(ex,-"数据库监控失败");)
16. [- interface_error_rate](#16---interface_error_rate)
17. [return {'status': 'error', 'error': str(e)}](#17-return-{'status':-'error',-'error':-str(e)})
18. [_logger.LogError(ex, "患者创建失败，姓名: {Name}, 电话: {Phon...](#18-_logger.logerror(ex,-"患者创建失败，姓名:-{name},-电话:-{phon...)
19. [self.error_patterns = [](#19-self.error_patterns-=-[)
20. [| **客户端-服务端通信** | **锁定 Refit**。 | Refit 提供了类型安全的HT...](#20-|-**客户端-服务端通信**-|-**锁定-refit**。-|-refit-提供了类型安全的ht...)
21. [## ### Phase 2: 文档完善（进行中）](#21-##-###-phase-2:-文档完善（进行中）)
22. [4. **保持简单**：不要为了用Prism特性而用，要解决实际问题](#22-4.-**保持简单**：不要为了用prism特性而用，要解决实际问题)
23. [**问题**：历史设计偏差导致Consultation被误作为中心实体，违反了聚合根架构原则。](#23-**问题**：历史设计偏差导致consultation被误作为中心实体，违反了聚合根架构原则。)
24. [*   **问题描述：**](#24-*---**问题描述：**)
25. [## ## 📋 目录](#25-##-##-📋-目录)

---

## 1. ## ## 4. 结论与建议

**解决方案**:
在凌隐宝堂中医诊所管理系统的当前阶段，引入完整的CQRS模式和MediatR库**均没有必要**。这两种技术所解决的核心痛点（高并发性能、极端复杂的业务解耦）在本项目中并不突出。强行引入将导致过度设计，违背项目“稳定优先、最小变更”的核心原则。

**来源**: `ADR-001-cqrs-mediatr-rejection.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 2. 为杜绝“为了技术而技术”的问题，并确保所有技术决策都服务于项目的长远健康，本技术路线严格遵循以下核心原则：

**解决方案**:
*   **稳定性压倒一切 (Stability First)**：技术选型优先选择成熟、稳定、具有长期支持（LTS）的方案。避免追逐潮流，不把生产环境作为新技术的试验场。
*   **务实主义与简约化 (Pragmatism & Simplicity)**：始终选择能够解决当前问题的、最简单的方案。当简单的方案（如标准分层架构）能满足需求时，绝不引入不必要的复杂设计（如完整的CQRS/MediatR）。
*   **发挥存量优势 (Leverage Existing Strengths)**：项目的技术栈（.NET/WPF/EF Core）是成熟且强大的组合。我们应深化对现有技术的投资，而不是将精力分散到新的技术领域，以实现最高的投资回报率。

**来源**: `ADR-002-technology-roadmap-suggestion.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 3. ## #### 内部文档

**解决方案**:
## #### 内部文档

- [CLAUDE.md](../CLAUDE.md) - 主配置文件

**来源**: `ai-collaboration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 4. ```markdown

**解决方案**:
- Line 45: 考虑提取方法减少复杂度
- Line 156: 缺少异步方法的ConfigureAwait

**代码示例**:
```markdown
## 代码审查报告

### ✅ 通过项
- 命名规范: 100%符合
- 依赖注入: 正确使用构造函数注入
- 文件编码: UTF-8 with BOM

### ⚠️ 建议改进
- Line 45: 考虑提取方法减少复杂度
- Line 78: 可优化LINQ查询性能

### ❌ 必须修复
- Line 120: 使用了ServiceLocator反模式
- Line 156: 缺少异步方法的ConfigureAwait
```

**来源**: `ai-collaboration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 5. ## #### 3. WPF / Prism MVVM 规范（🟡 建议问题）

**解决方案**:
- 必须继承 `BindableBase` 或实现 `INotifyPropertyChanged`

**来源**: `code-review-guidelines.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 6. foreach (var error in validationResult.Errors)

**解决方案**:
```csharp
// Program.cs
services.AddLybtConfiguration(configuration);

**代码示例**:
```csharp
// Program.cs
services.AddLybtConfiguration(configuration);

// 在应用启动时验证配置
var validationResult = configuration.ValidateLybtConfiguration();
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"配置错误: {error}");
    }
    throw new InvalidOperationException("配置验证失败");
}
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 7. ```powershell

**解决方案**:
```powershell
# Windows PowerShell
# setup-project.ps1

**代码示例**:
```powershell
# Windows PowerShell
# setup-project.ps1

# 设置项目路径
$projectPath = "D:\source\repos\LYBTZYZS"

# 检查项目目录是否存在
if (-not (Test-Path $projectPath)) {
    Write-Host "项目目录不存在，请先克隆项目" -ForegroundColor Red
    Write-Host "执行: git clone <repository-url> $projectPath" -ForegroundColor Yellow
    exit 1
}

# 进入项目目录
Set-Location $projectPath

# 恢复 NuGet 包
Write-Host "恢复 NuGet 包..." -ForegroundColor Yellow
dotnet restore LYBT.All.sln

# 构建项目
Write-Host "构建项目..." -ForegroundColor Yellow
dotnet build LYBT.All.sln -c Release

# 运行数据库迁移
Write-Host "运行数据库迁移..." -ForegroundColor Yellow
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/LYBT.Server.API

# 初始化开发环境配置
Write-Host "初始化开发环境配置..." -ForegroundColor Yellow
dotnet user-secrets init

# 设置开发环境密钥
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=LYBT_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
dotnet user-secrets set "Authentication:Jwt:SecretKey" "dev-jwt-secret-key-256-bits-minimum-length-for-security"
dotnet user-secrets set "Authentication:Jwt:Issuer" "LYBT-Dev"
dotnet user-secrets set "Authentication:Jwt:Audience" "LYBT-Client-Dev"

Write-Host "开发环境设置完成！" -ForegroundColor Green
```

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 8. ```yaml

**解决方案**:
```yaml
# docker-compose.test.yml
version: '3.8'

**代码示例**:
```yaml
# docker-compose.test.yml
version: '3.8'

services:
  # SQL Server 数据库
  sql-server:
    image: mcr.microsoft.com/mssql/server:2019-latest
    container_name: lybt-sql-test
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=TestPassword123!
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sql_test_data:/var/opt/mssql/data
      - ./scripts/test/create-test-database.sql:/docker-entrypoint-initdb.d/create-test-database.sql
    networks:
      - lybt-test-network
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "TestPassword123!", "-Q", "SELECT 1"]
      interval: 30s
      timeout: 10s
      retries: 5

  # Redis 缓存
  redis:
    image: redis:7-alpine
    container_name: lybt-redis-test
    ports:
      - "6379:6379"
    volumes:
      - redis_test_data:/data
    networks:
      - lybt-test-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 5

  # 后端 API
  lybt-api:
    build:
      context: .
      dockerfile: src/Server/LYBT.Server.API/Dockerfile
      target: test
    container_name: lybt-api-test
    environment:
      - ASPNETCORE_ENVIRONMENT=Testing
      - ConnectionStrings__DefaultConnection=Server=sql-server,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;
      - ConnectionStrings__Redis=redis:6379
      - Authentication__Jwt__SecretKey=test-jwt-secret-key-256-bits-minimum
      - Authentication__Jwt__Issuer=LYBT-Test
      - Authentication__Jwt__Audience=LYBT-Client-Test
    ports:
      - "5001:80"
    depends_on:
      sql-server:
        condition: service_healthy
      redis:
        condition: service_healthy
    networks:
      - lybt-test-network
    volumes:
      - ./test-results:/app/test-results
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 5

  # 测试运行器
  test-runner:
    build:
      context: .
      dockerfile: Dockerfile.test
    container_name: lybt-test-runner
    environment:
      - ASPNETCORE_ENVIRONMENT=Testing
      - ConnectionStrings__DefaultConnection=Server=sql-server,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;
      - ConnectionStrings__Redis=redis:6379
      - Authentication__Jwt__SecretKey=test-jwt-secret-key-256-bits-minimum
    depends_on:
      lybt-api:
        condition: service_healthy
    networks:
      - lybt-test-network
    volumes:
      - ./test-results:/app/test-results
      - ./tests:/app/tests
    command: ["dotnet", "test", "--logger", "trx", "--results-directory", "/app/test-results"]

volumes:
  sql_test_data:
  redis_test_data:

networks:
  lybt-test-network:
    driver: bridge
```

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 9. ```yaml

**解决方案**:
```yaml
# .github/workflows/test.yml
name: Test Environment

**代码示例**:
```yaml
# .github/workflows/test.yml
name: Test Environment

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Unit Tests
      run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test_results.trx" --results-directory TestResults

    - name: Upload Test Results
      if: failure()
      uses: actions/upload-artifact@v3
      with:
        name: unit-test-results
        path: TestResults/

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests

    services:
      sql-server:
        image: mcr.microsoft.com/mssql/server:2019-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: TestPassword123!
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestPassword123! -Q 'SELECT 1'"
          --health-interval 30s
          --health-timeout 10s
          --health-retries 5

      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 30s
          --health-timeout 10s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Database Migrations
      run: dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/LYBT.Server.API
      env:
        ConnectionStrings__DefaultConnection: Server=localhost,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;

    - name: Run Integration Tests
      run: dotnet test tests/LYBT.Server.IntegrationTests --no-build --verbosity normal --logger "trx;LogFileName=test_results.trx" --results-directory TestResults
      env:
        ASPNETCORE_ENVIRONMENT: Testing
        ConnectionStrings__DefaultConnection: Server=localhost,1433;Database=LYBT_Test;User Id=sa;Password=TestPassword123!;TrustServerCertificate=true;
        ConnectionStrings__Redis: localhost:6379
        Authentication__Jwt__SecretKey: test-jwt-secret-key-256-bits-minimum
        Authentication__Jwt__Issuer: LYBT-Test
        Authentication__Jwt__Audience: LYBT-Client-Test

    - name: Upload Test Results
      if: failure()
      uses: actions/upload-artifact@v3
      with:
        name: integration-test-results
        path: TestResults/

  performance-tests:
    runs-on: ubuntu-latest
    needs: integration-tests
    if: github.ref == 'main'

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '18'

    - name: Install k6
      run: |
        sudo gpg -k
        sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
        echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
        sudo apt-get update
        sudo apt-get install k6

    - name: Restore and Build
      run: |
        dotnet restore
        dotnet build --no-restore

    - name: Start Application
      run: |
        dotnet run --project src/Server/LYBT.Server.API --urls http://localhost:5000 &
        sleep 30

    - name: Run Performance Tests
      run: |
        k6 run --out json=performance-results.json tests/performance/load-test.js

    - name: Upload Performance Results
      uses: actions/upload-artifact@v3
      with:
        name: performance-results
        path: performance-results.json
```

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 10. ```yaml

**解决方案**:
```yaml
# k8s/namespace.yaml
apiVersion: v1

**代码示例**:
```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: lybt-production
  labels:
    name: lybt-production
    environment: production

---
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: lybt-config
  namespace: lybt-production
  labels:
    app: lybt
    environment: production
data:
  appsettings.json: |
    {
      "Application": {
        "Name": "LYBT.Server",
        "Environment": "Production",
        "Logging": {
          "Level": "Warning",
          "EnableConsole": false,
          "EnableFile": true,
          "FilePath": "/app/logs/lybt.log",
          "RollingInterval": "Day",
          "RetainedFileCountLimit": 30
        }
      },
      "Server": {
        "Urls": "http://0.0.0.0:80",
        "Cors": {
          "AllowOrigins": ["https://lybt.example.com"],
          "AllowMethods": ["GET", "POST", "PUT", "DELETE"],
          "AllowHeaders": ["Authorization", "Content-Type"]
        }
      },
      "Database": {
        "Provider": "SqlServer",
        "ConnectionStringName": "DefaultConnection",
        "EnableRetryOnFailure": true,
        "MaxRetryCount": 5,
        "CommandTimeout": 60
      },
      "Cache": {
        "Provider": "Redis",
        "EnableDistributedCache": true,
        "DefaultExpirationMinutes": 60
      },
      "HealthCheck": {
        "Enabled": true,
        "IntervalSeconds": 30,
        "TimeoutSeconds": 10
      }
    }

---
# k8s/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: lybt-secrets
  namespace: lybt-production
  labels:
    app: lybt
    environment: production
type: Opaque
data:
  # Base64 编码的配置值
  connection-string: <base64-encoded-connection-string>
  jwt-secret: <base64-encoded-jwt-secret>
  redis-connection: <base64-encoded-redis-connection-string>

---
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: lybt-api
  namespace: lybt-production
  labels:
    app: lybt-api
    environment: production
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: lybt-api
  template:
    metadata:
      labels:
        app: lybt-api
        environment: production
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "80"
        prometheus.io/path: "/metrics"
    spec:
      containers:
      - name: lybt-api
        image: lybt-registry.example.com/lybt-api:latest
        imagePullPolicy: Always
        ports:
        - containerPort: 80
          name: http
        - containerPort: 443
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: connection-string
        - name: ConnectionStrings__Redis
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: redis-connection
        - name: Authentication__Jwt__SecretKey
          valueFrom:
            secretKeyRef:
              name: lybt-secrets
              key: jwt-secret
        volumeMounts:
        - name: config-volume
          mountPath: /app/config
        - name: logs-volume
          mountPath: /app/logs
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/startup
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 6
      volumes:
      - name: config-volume
        configMap:
          name: lybt-config
      - name: logs-volume
        emptyDir: {}
      imagePullSecrets:
      - name: lybt-registry-secret
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        fsGroup: 1000

---
# k8s/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: lybt-api-service
  namespace: lybt-production
  labels:
    app: lybt-api
    environment: production
spec:
  selector:
    app: lybt-api
  ports:
  - name: http
    port: 80
    targetPort: 80
    protocol: TCP
  - name: https
    port: 443
    targetPort: 443
    protocol: TCP
  type: ClusterIP

---
# k8s/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: lybt-api-ingress
  namespace: lybt-production
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
    nginx.ingress.kubernetes.io/limit-connections: "100"
    nginx.ingress.kubernetes.io/limit-rps: "50"
    nginx.ingress.kubernetes.io/rate-limit: "100"
spec:
  tls:
  - hosts:
    - lybt-api.example.com
    secretName: lybt-api-tls
  rules:
  - host: lybt-api.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: lybt-api-service
            port:
              number: 80

---
# k8s/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: lybt-api-hpa
  namespace: lybt-production
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: lybt-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 100
        periodSeconds: 15
```

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 11. echo "错误: kubectl 未安装或不在 PATH 中"

**解决方案**:
```bash
#!/bin/bash
# deploy-production.sh

**代码示例**:
```bash
#!/bin/bash
# deploy-production.sh

set -e

# 配置变量
NAMESPACE="lybt-production"
DOCKER_REGISTRY="lybt-registry.example.com"
IMAGE_TAG="latest"
ENVIRONMENT="production"

echo "开始部署 LYBT 生产环境..."

# 检查 kubectl 是否可用
if ! command -v kubectl &> /dev/null; then
    echo "错误: kubectl 未安装或不在 PATH 中"
    exit 1
fi

# 检查集群连接
if ! kubectl cluster-info &> /dev/null; then
    echo "错误: 无法连接到 Kubernetes 集群"
    exit 1
fi

# 创建命名空间
echo "创建命名空间: $NAMESPACE"
kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

# 应用配置
echo "应用配置文件..."
kubectl apply -f k8s/configmap.yaml -n $NAMESPACE
kubectl apply -f k8s/secret.yaml -n $NAMESPACE

# 构建和推送镜像
echo "构建和推送 Docker 镜像..."
docker build -t $DOCKER_REGISTRY/lybt-api:$IMAGE_TAG -f src/Server/LYBT.Server.API/Dockerfile .
docker push $DOCKER_REGISTRY/lybt-api:$IMAGE_TAG

# 部署应用
echo "部署应用..."
kubectl apply -f k8s/deployment.yaml -n $NAMESPACE
kubectl apply -f k8s/service.yaml -n $NAMESPACE
kubectl apply -f k8s/ingress.yaml -n $NAMESPACE
kubectl apply -f k8s/hpa.yaml -n $NAMESPACE

# 等待部署完成
echo "等待部署完成..."
kubectl rollout status deployment/lybt-api -n $NAMESPACE --timeout=600s

# 验证部署
echo "验证部署状态..."
kubectl get pods -n $NAMESPACE -l app=lybt-api
kubectl get services -n $NAMESPACE
kubectl get ingress -n $NAMESPACE

# 健康检查
echo "执行健康检查..."
sleep 30
HEALTH_URL="https://lybt-api.example.com/health"
if curl -f -s $HEALTH_URL > /dev/null; then
    echo "✅ 健康检查通过"
else
    echo "❌ 健康检查失败"
    exit 1
fi

echo "✅ 生产环境部署完成！"
echo "API 地址: https://lybt-api.example.com"
```

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 12. - alert: HighErrorRate

**解决方案**:
```yaml
# k8s/monitoring.yaml
apiVersion: v1

**代码示例**:
```yaml
# k8s/monitoring.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: monitoring
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
      evaluation_interval: 15s

    rule_files:
      - "lybt_rules.yml"

    scrape_configs:
      - job_name: 'lybt-api'
        static_configs:
          - targets: ['lybt-api-service.lybt-production.svc.cluster.local:80']
        metrics_path: /metrics
        scrape_interval: 15s

    alerting:
      alertmanagers:
        - static_configs:
            - targets:
              - alertmanager:9093

---
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-rules
  namespace: monitoring
data:
  lybt_rules.yml: |
    groups:
    - name: lybt-api
      rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.1
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value }} errors per second"

      - alert: HighResponseTime
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High response time detected"
          description: "95th percentile response time is {{ $value }} seconds"

      - alert: DatabaseConnectionFailure
        expr: up{job="lybt-api"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "API is down"
          description: "LYBT API has been down for more than 1 minute"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: prometheus
  namespace: monitoring
spec:
  replicas: 1
  selector:
    matchLabels:
      app: prometheus
  template:
    metadata:
      labels:
        app: prometheus
    spec:
      containers:
      - name: prometheus
        image: prom/prometheus:latest
        ports:
        - containerPort: 9090
        volumeMounts:
        - name: config-volume
          mountPath: /etc/prometheus
        - name: rules-volume
          mountPath: /etc/prometheus/rules
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "200m"
      volumes:
      - name: config-volume
        configMap:
          name: prometheus-config
      - name: rules-volume
        configMap:
          name: prometheus-rules
```

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 13. □ 异常处理完整，错误返回统一

**解决方案**:
## ### 开发过程检查

```bash

**代码示例**:
```bash
□ 代码结构符合三层架构标准
□ 接口命名统一，返回类型一致
□ 依赖注入正确，无 Service Locator
□ 异步编程规范，I/O 操作异步
□ 异常处理完整，错误返回统一
□ 单元测试覆盖，核心逻辑测试
□ 代码注释完整，关键逻辑说明
```

**来源**: `rapid-development-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 14. ```csharp

**解决方案**:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

**代码示例**:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 注册数据库上下文
builder.Services.AddDbContext<LybtDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册仓储层
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IConsultationRepository, ConsultationRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IHerbRepository, HerbRepository>();
builder.Services.AddScoped<IFormulaRepository, FormulaRepository>();

// 注册服务层
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHerbService, HerbService>();
builder.Services.AddScoped<IFormulaService, FormulaService>();

// 注册外部服务
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<ISmsService, AliyunSmsService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// 注册认证服务
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

var app = builder.Build();
```

**来源**: `module-integration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 15. _logger.LogError(ex, "数据库监控失败");

**解决方案**:
```csharp
public class DatabaseMonitoringService
{

**代码示例**:
```csharp
public class DatabaseMonitoringService
{
    private readonly LybtDbContext _context;
    private readonly ILogger<DatabaseMonitoringService> _logger;
    private readonly IMetrics _metrics;

    public async Task MonitorDatabasePerformanceAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // 测试数据库连接
            await _context.Database.CanConnectAsync();
            
            // 获取数据库统计信息
            var connectionCount = await GetActiveConnectionsAsync();
            var slowQueries = await GetSlowQueriesAsync();
            
            // 记录指标
            _metrics.Gauge("database_active_connections").Set(connectionCount);
            _metrics.Gauge("database_slow_queries").Set(slowQueries.Count);
            
            _logger.LogInformation("数据库监控完成，活跃连接数: {ConnectionCount}, 慢查询数: {SlowQueryCount}",
                connectionCount, slowQueries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库监控失败");
        }
        finally
        {
            stopwatch.Stop();
            _metrics.Histogram("database_monitoring_duration_seconds").Observe(stopwatch.Elapsed.TotalSeconds);
        }
    }
}
```

**来源**: `module-integration-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 16. - interface_error_rate

**解决方案**:
- dns_resolution_time_ms

**代码示例**:
```yaml
# 网络监控指标
network_metrics:
  bandwidth:
    - interface_bandwidth_utilization
    - interface_throughput_mbps
    - interface_packet_loss_rate
    - interface_error_rate
  
  connectivity:
    - ping_response_time_ms
    - tcp_connection_success_rate
    - dns_resolution_time_ms
    - http_response_time_ms
  
  security:
    - firewall_blocked_connections
    - intrusion_detection_events
    - ddos_attack_detected
    - ssl_certificate_expiry_days
```

**来源**: `monitoring-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 17. return {'status': 'error', 'error': str(e)}

**解决方案**:
```python
# 自动故障检测脚本
import requests

**代码示例**:
```python
# 自动故障检测脚本
import requests
import time
import smtplib
from email.mime.text import MIMEText
from datetime import datetime
import logging

class HealthChecker:
    def __init__(self):
        self.services = {
            'patient-api': 'https://api.lybt.com/patient/health',
            'consultation-api': 'https://api.lybt.com/consultation/health',
            'prescription-api': 'https://api.lybt.com/prescription/health',
            'web-frontend': 'https://www.lybt.com/health'
        }
        
        self.notification_threshold = 3  # 连续失败次数阈值
        self.check_interval = 60  # 检查间隔（秒）
        
    def check_service_health(self, service_name, service_url):
        """检查服务健康状态"""
        try:
            response = requests.get(service_url, timeout=10)
            if response.status_code == 200:
                return {'status': 'healthy', 'response_time': response.elapsed.total_seconds()}
            else:
                return {'status': 'unhealthy', 'response_time': response.elapsed.total_seconds(), 'status_code': response.status_code}
        except requests.exceptions.RequestException as e:
            return {'status': 'error', 'error': str(e)}
    
    def send_alert(self, service_name, health_status):
        """发送告警通知"""
        alert_message = f"""
        服务告警通知
        
        服务名称: {service_name}
        健康状态: {health_status['status']}
        检测时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}
        
        详细信息:
        {health_status}
        
        请立即检查服务状态并采取相应措施。
        """
        
        # 发送邮件通知
        self.send_email_alert(f"[{health_status['status'].upper()}] 服务告警: {service_name}", alert_message)
        
        # 发送短信通知（针对关键服务）
        if service_name in ['patient-api', 'consultation-api', 'prescription-api']:
            self.send_sms_alert(f"服务{service_name}状态异常: {health_status['status']}")
    
    def send_email_alert(self, subject, message):
        """发送邮件告警"""
        try:
            msg = MIMEText(message)
            msg['Subject'] = subject
            msg['From'] = 'monitoring@lybt.com'
            msg['To'] = 'ops@lybt.com'
            
            with smtplib.SMTP('smtp.lybt.com', 587) as server:
                server.starttls()
                server.login('monitoring@lybt.com', 'password')
                server.send_message(msg)
        except Exception as e:
            logging.error(f"发送邮件告警失败: {e}")
    
    def send_sms_alert(self, message):
        """发送短信告警"""
        # 这里可以集成短信服务提供商的API
        logging.info(f"短信告警: {message}")
    
    def run_health_check(self):
        """运行健康检查"""
        service_status = {}
        
        for service_name, service_url in self.services.items():
            health_status = self.check_service_health(service_name, service_url)
            service_status[service_name] = health_status
            
            # 检查是否需要发送告警
            if health_status['status'] != 'healthy':
                failure_count = self.get_failure_count(service_name)
                if failure_count >= self.notification_threshold:
                    self.send_alert(service_name, health_status)
                    self.reset_failure_count(service_name)
                else:
                    self.increment_failure_count(service_name)
            else:
                self.reset_failure_count(service_name)
        
        return service_status
    
    def start_monitoring(self):
        """开始监控"""
        logging.info("开始服务健康监控...")
        
        while True:
            try:
                status = self.run_health_check()
                self.log_status(status)
                time.sleep(self.check_interval)
            except Exception as e:
                logging.error(f"监控过程中发生错误: {e}")
                time.sleep(self.check_interval)
    
    def log_status(self, status):
        """记录服务状态"""
        for service_name, health_status in status.items():
            logging.info(f"服务 {service_name}: {health_status['status']}")
            if 'response_time' in health_status:
                logging.info(f"响应时间: {health_status['response_time']}秒")

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    checker = HealthChecker()
    checker.start_monitoring()
```

**来源**: `monitoring-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 18. _logger.LogError(ex, "患者创建失败，姓名: {Name}, 电话: {Phone}",

**解决方案**:
```csharp
// 日志配置 (appsettings.json)
{

**代码示例**:
```csharp
// 日志配置 (appsettings.json)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "LyBT": "Debug"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
    },
    "File": {
      "Path": "logs/lybt-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30,
      "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
    },
    "Seq": {
      "ServerUrl": "http://localhost:5341",
      "ApiKey": "your-api-key"
    }
  }
}

// 日志使用示例
public class PatientService
{
    private readonly ILogger<PatientService> _logger;
    
    public async Task<PatientDto> CreateAsync(CreatePatientDto createDto)
    {
        _logger.LogInformation("开始创建患者，姓名: {Name}, 电话: {Phone}", 
            createDto.Name, createDto.Phone);

        try
        {
            var patient = new Patient
            {
                Name = createDto.Name,
                Gender = createDto.Gender,
                BirthDate = createDto.BirthDate,
                Phone = createDto.Phone,
                Address = createDto.Address
            };

            var createdPatient = await _repository.AddAsync(patient);
            
            _logger.LogInformation("患者创建成功，ID: {PatientId}, 姓名: {Name}", 
                createdPatient.Id, createdPatient.Name);

            return _mapper.Map<PatientDto>(createdPatient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者创建失败，姓名: {Name}, 电话: {Phone}", 
                createDto.Name, createDto.Phone);
            throw;
        }
    }
}
```

**来源**: `monitoring-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 19. self.error_patterns = [

**解决方案**:
```python
# 日志监控脚本
import re

**代码示例**:
```python
# 日志监控脚本
import re
import json
from datetime import datetime, timedelta
from elasticsearch import Elasticsearch

class LogMonitor:
    def __init__(self):
        self.es = Elasticsearch(['http://localhost:9200'])
        self.error_patterns = [
            r'Exception',
            r'Error',
            r'Failed',
            r'Timeout',
            r'Connection refused'
        ]
        
    def search_error_logs(self, time_range_hours=1):
        """搜索错误日志"""
        query = {
            "query": {
                "bool": {
                    "must": [
                        {
                            "range": {
                                "@timestamp": {
                                    "gte": f"now-{time_range_hours}h",
                                    "lte": "now"
                                }
                            }
                        },
                        {
                            "regexp": {
                                "message": "Exception|Error|Failed|Timeout"
                            }
                        }
                    ]
                }
            },
            "sort": [
                {
                    "@timestamp": {
                        "order": "desc"
                    }
                }
            ],
            "size": 100
        }
        
        response = self.es.search(index="lybt-logs-*", body=query)
        return response['hits']['hits']
    
    def analyze_error_patterns(self, logs):
        """分析错误模式"""
        error_patterns = {}
        
        for log in logs:
            message = log['_source']['message']
            timestamp = log['_source']['@timestamp']
            
            for pattern in self.error_patterns:
                if re.search(pattern, message, re.IGNORECASE):
                    if pattern not in error_patterns:
                        error_patterns[pattern] = []
                    
                    error_patterns[pattern].append({
                        'timestamp': timestamp,
                        'message': message,
                        'service': log['_source'].get('service', 'unknown')
                    })
        
        return error_patterns
    
    def generate_error_report(self):
        """生成错误报告"""
        error_logs = self.search_error_logs()
        error_patterns = self.analyze_error_patterns(error_logs)
        
        report = {
            'timestamp': datetime.now().isoformat(),
            'total_errors': len(error_logs),
            'error_patterns': {},
            'top_errors': []
        }
        
        for pattern, errors in error_patterns.items():
            report['error_patterns'][pattern] = {
                'count': len(errors),
                'services': list(set([error['service'] for error in errors])),
                'latest_error': errors[0]['timestamp'] if errors else None
            }
            
            # 添加到top_errors
            if len(errors) > 5:
                report['top_errors'].extend(errors[:5])
        
        return report

if __name__ == "__main__":
    monitor = LogMonitor()
    report = monitor.generate_error_report()
    
    print("错误日志报告:")
    print(f"生成时间: {report['timestamp']}")
    print(f"总错误数: {report['total_errors']}")
    
    for pattern, info in report['error_patterns'].items():
        print(f"\n错误模式: {pattern}")
        print(f"  出现次数: {info['count']}")
        print(f"  影响服务: {', '.join(info['services'])}")
        print(f"  最新错误: {info['latest_error']}")
```

**来源**: `monitoring-guide.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 20. | **客户端-服务端通信** | **锁定 Refit**。 | Refit 提供了类型安全的HTTP客户端，能将API定义转化为C#接口，显著减少了手写HTTP请求的错误，提升了开发体验。 |

**解决方案**:
## ### 2.2. 前端 (Frontend)

| 领域 | 决策 | 理由 |

**来源**: `ADR-002-technology-roadmap-suggestion.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 21. ## ### Phase 2: 文档完善（进行中）

**解决方案**:
## ### Phase 2: 文档完善（进行中）

✅ **Issue #1216**: 创建 Desktop 架构标准文档

**来源**: `ADR-002-desktop-services-removal.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 22. 4. **保持简单**：不要为了用Prism特性而用，要解决实际问题

**解决方案**:
4. **保持简单**：不要为了用Prism特性而用，要解决实际问题

**来源**: `PRISM_OPTIMIZATION_ULTRATHINK.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 23. **问题**：历史设计偏差导致Consultation被误作为中心实体，违反了聚合根架构原则。

**解决方案**:
## ### 2025-01-09: Issue #1093 - 依赖关系正名

**问题**：历史设计偏差导致Consultation被误作为中心实体，违反了聚合根架构原则。

**来源**: `consultation-module.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 24. *   **问题描述：**

**解决方案**:
## ### 3.1. 自定义对话框服务的具体实现
虽然 `ICustomDialogService` 的设计思想是正确的，但其具体实现 `WpfDialogService.cs` 存在一些问题。`ShowDialogAsync(string dialogName, ...)` 方法内部包含了大量的 `if-else if` 分支和硬编码的 ViewModel 类型字符串，并通过反射来调用特定 ViewModel 的初始化方法。
*   **违反 MVVM 关注点分离：** 服务层（Service）不应该了解视图模型（ViewModel）的内部实现细节（如方法名 `InitializeWithContextAsync`）。

**来源**: `Prism_Implementation_Report.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 25. ## ## 📋 目录

**解决方案**:
## ## 📋 目录

1. [概述](#概述)

**来源**: `ai-collaboration-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.9/1.0)

---

## 💡 使用建议

- **快速查找**: 使用目录快速定位到具体问题
- **代码示例**: 所有代码示例都可以直接复制使用
- **相关问题**: 查看条目的来源文档获取更多详细信息
- **反馈建议**: 发现问题或有改进建议请及时反馈

