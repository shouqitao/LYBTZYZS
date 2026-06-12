# Postman CLI 测试指南

## 1. 安装 Postman CLI

### 方式一：npm 安装（推荐）
```bash
npm install -g @postman/cli
```

### 方式二：Windows PowerShell 安装
```powershell
# 使用 winget
winget install Postman.Postman

# 或者下载安装程序
# 访问 https://www.postman.com/downloads/ 下载安装
```

### 验证安装
```bash
postman --version
```

---

## 2. 登录 Postman CLI

```bash
postman login
# 会打开浏览器进行 OAuth 登录
```

---

## 3. 导入 Collection 到 Postman Cloud（可选）

如果不想登录，可以使用 Newman（Postman CLI 的开源替代品）：

```bash
# 安装 Newman
npm install -g newman

# 运行测试
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --env-var "baseUrl=https://localhost:5001" \
  --insecure  # 忽略 SSL 证书验证（开发环境）
```

---

## 4. 使用 Newman 进行自动化测试（推荐离线使用）

### 安装 Newman
```bash
npm install -g newman newman-reporter-htmlextra
```

### 基础测试
```bash
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --env-var "baseUrl=https://localhost:5001" \
  --insecure \
  --delay-request 100
```

### 带环境文件的测试
创建 `postman-environment.json`:
```json
{
  "name": "LYBT Local",
  "values": [
    { "key": "baseUrl", "value": "https://localhost:5001", "enabled": true },
    { "key": "authToken", "value": "", "enabled": true },
    { "key": "refreshToken", "value": "", "enabled": true },
    { "key": "currentUsername", "value": "sysadmin", "enabled": true }
  ]
}
```

运行：
```bash
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  -e postman-environment.json \
  --insecure \
  --reporters cli,htmlextra \
  --reporter-htmlextra-export test-report.html
```

---

## 5. 批量测试脚本

### Windows PowerShell
```powershell
# 创建环境变量文件
$envContent = @'
{
  "name": "LYBT-Local",
  "values": [
    { "key": "baseUrl", "value": "https://localhost:5001", "enabled": true },
    { "key": "currentUsername", "value": "sysadmin", "enabled": true },
    { "key": "currentUserId", "value": "", "enabled": true },
    { "key": "authToken", "value": "", "enabled": true },
    { "key": "refreshToken", "value": "", "enabled": true }
  ]
}
'@
$envContent | Out-File -FilePath "lybt-env.json" -Encoding UTF8

# 运行测试
newman run docs/06-operations/LYBTZYZS_API_Collection.json `
  -e lybt-env.json `
  --insecure `
  --delay-request 100 `
  --reporters cli,json,htmlextra `
  --reporter-json-export results.json `
  --reporter-htmlextra-export report.html
```

### Bash
```bash
# 创建环境变量文件
cat > lybt-env.json << 'EOF'
{
  "name": "LYBT-Local",
  "values": [
    { "key": "baseUrl", "value": "https://localhost:5001", "enabled": true },
    { "key": "currentUsername", "value": "sysadmin", "enabled": true },
    { "key": "currentUserId", "value": "", "enabled": true },
    { "key": "authToken", "value": "", "enabled": true },
    { "key": "refreshToken", "value": "", "enabled": true }
  ]
}
EOF

# 运行测试
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  -e lybt-env.json \
  --insecure \
  --delay-request 100 \
  --reporters cli,json,htmlextra \
  --reporter-json-export results.json \
  --reporter-htmlextra-export report.html
```

---

## 6. 分阶段测试

### 阶段 1：Health + Auth
```bash
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --folder "Health" \
  --env-var "baseUrl=https://localhost:5001" \
  --insecure

newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --folder "Auth" \
  --env-var "baseUrl=https://localhost:5001" \
  --insecure
```

### 阶段 2：核心业务
```bash
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --folder "Patients" \
  --env-var "baseUrl=https://localhost:5001" \
  --insecure

newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --folder "Medical Cases" \
  --env-var "baseUrl=https://localhost:5001" \
  --insecure
```

---

## 7. CI/CD 集成示例

### GitHub Actions
```yaml
name: API Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'
      
      - name: Start API
        run: |
          dotnet run --project src/Server/Services/LYBT.WebAPI &
          sleep 10
      
      - name: Install Newman
        run: npm install -g newman newman-reporter-htmlextra
      
      - name: Run API Tests
        run: |
          newman run docs/06-operations/LYBTZYZS_API_Collection.json \
            --env-var "baseUrl=http://localhost:5000" \
            --reporters cli,htmlextra \
            --reporter-htmlextra-export test-report.html
      
      - name: Upload Report
        uses: actions/upload-artifact@v4
        with:
          name: test-report
          path: test-report.html
```

---

## 8. 常见问题

### SSL 证书错误
```bash
# 添加 --insecure 参数跳过证书验证
newman run collection.json --insecure
```

### 超时问题
```bash
# 增加超时时间
newman run collection.json --timeout-request 30000
```

### 依赖请求（需先登录）
由于 Collection 中有预请求脚本处理登录，Newman 会自动执行。但如果需要手动处理：
```bash
# 先获取 token
curl -k -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"sysadmin","password":"DevPass123"}'

# 然后使用 token 运行特定请求
newman run collection.json --env-var "authToken=YOUR_TOKEN"
```

---

## 9. 快速开始命令

```bash
# 一键安装 Newman
npm install -g newman newman-reporter-htmlextra

# 一键测试（需要先确保 WebAPI 已启动）
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --env-var "baseUrl=https://localhost:5001" \
  --insecure \
  --reporters cli,htmlextra \
  --reporter-htmlextra-export report.html
```

查看报告：`report.html`
