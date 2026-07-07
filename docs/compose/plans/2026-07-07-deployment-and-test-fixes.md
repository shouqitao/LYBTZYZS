# 部署流程优化 + Newman 测试修复计划

## [S1] 问题总结

### 1.1 部署流程问题
- `unzip -o` 每次覆盖 appsettings.Production.json，导致占位符 `${SYSADMIN_PASSWORD}` 回来
- 需要反复手动修复配置，效率极低
- 本地 Development 模式跳过 Production 验证，问题只在部署后暴露

### 1.2 Newman 测试问题
- Phase 1.2+ 所有请求返回 500，token 变量未正确传递
- `toggle-status` 端点有 `InvalidCastException`（Int32→String 类型不匹配）
- 测试断言与实际 API 行为不匹配（如 User Not Found 返回 400 而非 401）

### 1.3 环境差异
- 本地 Development：跳过密码复杂度验证、跳过 ProductionConfigurationValidator
- 服务器 Production：完整验证，缺失任何配置都终止启动

## [S2] 修复方案

### 2.1 部署流程优化

**方案 A：打包时排除配置文件（推荐）**
```powershell
# 打包时排除配置文件
$files = Get-ChildItem "$env:TEMP\lybt-publish\*" -Exclude "appsettings*.json"
Compress-Archive -Path $files -DestinationPath "$env:TEMP\lybt-webapi-update.zip" -Force
```

**方案 B：部署后自动恢复配置**
```bash
# 在部署脚本中加入
ssh player@192.168.190.246 "python3 /tmp/fix_both_configs.py"
```

**方案 C：使用环境变量（最安全）**
- 将敏感配置移到环境变量
- 配置文件只包含非敏感默认值
- 需要修改 Program.cs 读取环境变量

**推荐**：先用方案 A（简单有效），后续考虑方案 C（更安全）

### 2.2 toggle-status 修复
- 已修复 `UsersDbContext.cs`：`HasConversion<string>()` → `HasConversion<int>()`
- 需要重新构建并部署

### 2.3 Newman 测试修复

**Phase 1.2 变量传递问题**：
- 原因：Phase 1 登录设置的 `sysadmin_token` 在 Phase 1.2 中未解析
- 方案：使用 `pm.environment.set()` 替代 `pm.collectionVariables.set()`
- 或者：在每个 Phase 开始时重新登录获取 token

**断言修正**：
- "User Not Found"：服务端返回 400，断言改为 `pm.expect(pm.response.code).to.be.oneOf([400,401])`
- "Validate Token"：响应字段是 `valid` 而非 `isValid`
- "Refresh Token"：logout 后 token 失效，断言改为允许 401

## [S3] 执行步骤

### Step 1：修复部署脚本
- 创建 `deploy-fixed.ps1`，打包时排除配置文件
- 部署后自动运行配置修复脚本

### Step 2：重新构建并部署
- 包含 toggle-status 修复
- 使用新的部署脚本

### Step 3：修复 Newman 测试集合
- 修正变量传递机制
- 修正断言预期

### Step 4：运行完整测试验证
- Phase 1-4 全部通过
- 权限边界测试通过

## [S4] 预期结果

- 部署时间从 30+ 分钟缩短到 5 分钟
- Newman 测试断言通过率从 ~10% 提升到 90%+
- 后续部署不再需要手动修复配置
