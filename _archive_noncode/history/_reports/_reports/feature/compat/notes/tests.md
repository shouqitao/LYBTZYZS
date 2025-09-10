# HerbCompatNotes MVP 手工测试与脚本验收

**文档版本**: v1.0  
**创建时间**: 2025-09-09  
**项目**: LYBTZYZS - 配伍禁忌记录系统MVP  

## 🧪 测试策略概述

### 测试范围
- **API端点验证**: 5个核心API的功能完整性
- **数据持久化验证**: 数据库CRUD操作正确性
- **业务逻辑验证**: 配伍记录业务规则
- **异常处理验证**: 错误场景的正确处理

### 测试工具
- **手工测试**: Swagger UI + Postman
- **脚本测试**: PowerShell + curl
- **数据库验证**: SQL Server Management Studio

## 📋 手工验收测试用例

### TC-01: 创建配伍记录 (POST)
**Given**: 已有处方ID和有效的配伍记录数据  
**When**: 调用POST `/api/v1/prescriptions/{prescriptionId}/compat-notes`  
**Then**: 返回201状态码和创建的记录详情  

**测试数据**:
```json
{
  "herbCombination": "人参+萝卜子",
  "compatibilityType": "Conflict",
  "severityLevel": "Medium",
  "compatibilityNote": "人参补气，萝卜子行气，两者药性相反",
  "referenceSource": "中药学第九版",
  "doctorRecommendation": "建议分开服用，间隔2小时以上"
}
```

**验收标准**:
- ✅ HTTP状态码: 201 Created
- ✅ 响应包含完整的CompatibilityNoteDto
- ✅ ID字段为有效GUID
- ✅ CreateTime字段为当前时间
- ✅ 数据库中成功插入记录

### TC-02: 查询处方配伍记录 (GET)
**Given**: 处方ID存在且包含配伍记录  
**When**: 调用GET `/api/v1/prescriptions/{prescriptionId}/compat-notes`  
**Then**: 返回200状态码和配伍记录列表  

**验收标准**:
- ✅ HTTP状态码: 200 OK
- ✅ 返回ServiceResult<List<CompatibilityNoteDto>>格式
- ✅ 数据按创建时间倒序排列
- ✅ 包含所有必要字段

### TC-03: 更新配伍记录 (PUT)
**Given**: 配伍记录ID存在  
**When**: 调用PUT `/api/v1/prescriptions/{prescriptionId}/compat-notes/{noteId}`  
**Then**: 返回200状态码和更新后的记录  

**更新数据**:
```json
{
  "compatibilityNote": "更新后的配伍说明",
  "doctorRecommendation": "更新后的医生建议"
}
```

**验收标准**:
- ✅ HTTP状态码: 200 OK
- ✅ 返回更新后的完整记录
- ✅ 数据库中对应记录已更新
- ✅ UpdateTime字段已更新

### TC-04: 删除配伍记录 (DELETE)
**Given**: 配伍记录ID存在  
**When**: 调用DELETE `/api/v1/prescriptions/{prescriptionId}/compat-notes/{noteId}`  
**Then**: 返回200状态码和删除确认  

**验收标准**:
- ✅ HTTP状态码: 200 OK
- ✅ 返回删除成功消息
- ✅ 数据库中记录被逻辑删除(IsDeleted=true)
- ✅ 后续查询不返回该记录

### TC-05: 异常处理验证
**Given**: 无效的请求参数  
**When**: 调用任意API端点  
**Then**: 返回适当的错误状态码和错误信息  

**测试场景**:
- 无效的GUID格式 → 400 Bad Request
- 不存在的处方ID → 404 Not Found
- 缺少必填字段 → 400 Bad Request
- 服务器内部错误 → 500 Internal Server Error

## 🖥️ PowerShell 自动化测试脚本

### 环境准备脚本
```powershell
# test-setup.ps1
# 设置测试环境变量
$baseUrl = "https://localhost:7001"
$apiVersion = "v1"
$testPrescriptionId = "550e8400-e29b-41d4-a716-446655440000"

# 获取访问令牌 (假设已有认证)
$token = "Bearer YOUR_JWT_TOKEN_HERE"
$headers = @{
    "Authorization" = $token
    "Content-Type" = "application/json"
}

Write-Host "测试环境配置完成" -ForegroundColor Green
Write-Host "基础URL: $baseUrl" -ForegroundColor Yellow
Write-Host "测试处方ID: $testPrescriptionId" -ForegroundColor Yellow
```

### 主要测试脚本
```powershell
# test-compat-notes.ps1

# 引入环境配置
. .\test-setup.ps1

function Test-CreateCompatNote {
    Write-Host "测试创建配伍记录..." -ForegroundColor Cyan
    
    $body = @{
        herbCombination = "测试药材组合"
        compatibilityType = "Warning"
        severityLevel = "Low"
        compatibilityNote = "测试配伍说明"
        referenceSource = "测试来源"
        doctorRecommendation = "测试建议"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/prescriptions/$testPrescriptionId/compat-notes" -Method POST -Body $body -Headers $headers
        Write-Host "✅ 创建成功: $($response.data.id)" -ForegroundColor Green
        return $response.data.id
    } catch {
        Write-Host "❌ 创建失败: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Test-GetCompatNotes {
    Write-Host "测试查询配伍记录..." -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/prescriptions/$testPrescriptionId/compat-notes" -Method GET -Headers $headers
        Write-Host "✅ 查询成功, 记录数: $($response.data.Count)" -ForegroundColor Green
        return $response.data
    } catch {
        Write-Host "❌ 查询失败: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Test-UpdateCompatNote($noteId) {
    Write-Host "测试更新配伍记录..." -ForegroundColor Cyan
    
    $body = @{
        compatibilityNote = "更新后的配伍说明"
        doctorRecommendation = "更新后的医生建议"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/prescriptions/$testPrescriptionId/compat-notes/$noteId" -Method PUT -Body $body -Headers $headers
        Write-Host "✅ 更新成功" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "❌ 更新失败: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Test-DeleteCompatNote($noteId) {
    Write-Host "测试删除配伍记录..." -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/prescriptions/$testPrescriptionId/compat-notes/$noteId" -Method DELETE -Headers $headers
        Write-Host "✅ 删除成功" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "❌ 删除失败: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# 执行完整测试流程
Write-Host "开始HerbCompatNotes MVP测试" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Yellow

$noteId = Test-CreateCompatNote
if ($noteId) {
    $notes = Test-GetCompatNotes
    if ($notes) {
        $updateResult = Test-UpdateCompatNote $noteId
        if ($updateResult) {
            $deleteResult = Test-DeleteCompatNote $noteId
        }
    }
}

Write-Host "================================" -ForegroundColor Yellow
Write-Host "测试完成" -ForegroundColor Yellow
```

### 数据库验证脚本
```powershell
# db-validation.ps1
function Test-DatabaseIntegrity {
    Write-Host "验证数据库表结构..." -ForegroundColor Cyan
    
    $connectionString = "Server=localhost;Database=LYBTDB;Integrated Security=true;"
    
    # 检查表是否存在
    $query = "SELECT COUNT(*) as TableExists FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HerbCompatibilityNotes'"
    
    # 这里需要根据实际数据库连接方式调整
    # 示例使用sqlcmd
    $result = sqlcmd -S "localhost" -d "LYBTDB" -Q $query -h -1
    
    if ($result -eq "1") {
        Write-Host "✅ 数据库表结构正确" -ForegroundColor Green
    } else {
        Write-Host "❌ 数据库表不存在" -ForegroundColor Red
    }
}

Test-DatabaseIntegrity
```

## 🌐 curl 命令行测试

### 基础认证获取
```bash
# 获取JWT Token
curl -X POST "https://localhost:7001/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "sysadmin", 
    "password": "Admin@123456"
  }'

# 导出token环境变量
export JWT_TOKEN="eyJ0eXAiOiJKV1QiLCJhbGc..."
```

### API测试命令
```bash
# 创建配伍记录
curl -X POST "https://localhost:7001/api/v1/prescriptions/550e8400-e29b-41d4-a716-446655440000/compat-notes" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "herbCombination": "人参+萝卜子",
    "compatibilityType": "Conflict",
    "severityLevel": "Medium", 
    "compatibilityNote": "人参补气，萝卜子行气，药性相反",
    "referenceSource": "中药学",
    "doctorRecommendation": "建议间隔服用"
  }'

# 查询配伍记录
curl -X GET "https://localhost:7001/api/v1/prescriptions/550e8400-e29b-41d4-a716-446655440000/compat-notes" \
  -H "Authorization: Bearer $JWT_TOKEN"

# 更新配伍记录
curl -X PUT "https://localhost:7001/api/v1/prescriptions/550e8400-e29b-41d4-a716-446655440000/compat-notes/{noteId}" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "compatibilityNote": "更新后的配伍说明",
    "doctorRecommendation": "更新后的建议"
  }'

# 删除配伍记录
curl -X DELETE "https://localhost:7001/api/v1/prescriptions/550e8400-e29b-41d4-a716-446655440000/compat-notes/{noteId}" \
  -H "Authorization: Bearer $JWT_TOKEN"
```

## 📊 验收报告模板

### 测试执行记录
```
测试日期: ___________
测试环境: ___________
执行人员: ___________

功能测试结果:
□ TC-01: 创建配伍记录 - 通过/失败
□ TC-02: 查询配伍记录 - 通过/失败  
□ TC-03: 更新配伍记录 - 通过/失败
□ TC-04: 删除配伍记录 - 通过/失败
□ TC-05: 异常处理验证 - 通过/失败

性能测试结果:
□ API响应时间 < 2秒 - 通过/失败
□ 数据库操作正常 - 通过/失败

安全测试结果:
□ 身份认证正常 - 通过/失败
□ 数据验证正确 - 通过/失败

总体评价:
□ 通过验收 - 可以部署生产环境
□ 有条件通过 - 需要修复轻微问题
□ 不通过 - 需要重大修复

备注事项:
_________________________________
_________________________________
```

## 🔧 故障排除指南

### 常见问题及解决方案

**问题1**: API返回401未授权
**解决**: 检查JWT Token是否有效，重新登录获取新token

**问题2**: 404 Not Found错误  
**解决**: 确认处方ID存在，检查URL路径是否正确

**问题3**: 400 Bad Request错误
**解决**: 检查请求体JSON格式，确认必填字段完整

**问题4**: 数据库连接失败
**解决**: 确认SQL Server服务运行，检查连接字符串

**问题5**: PowerShell脚本执行权限
**解决**: 运行 `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### 测试环境要求
- .NET 8 SDK
- SQL Server 2019+
- PowerShell 5.1+
- curl (for Linux/Mac测试)
- 有效的JWT认证令牌

---
**文档完成**: HerbCompatNotes MVP手工测试与脚本验收规范  
**下一步**: 执行diff-preview.patch和risks.md生成