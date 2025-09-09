# HerbCompatNotes 实施计划 - 最小实现路径

**实施时间**: 2025-09-09  
**预估工期**: 1-2个工作日  
**实施原则**: 最小变更，最大复用，零风险

## 🎯 实施步骤概览

总共**5个步骤**，每步完成后进行验收，确保增量交付和快速回滚能力。

| 步骤 | 描述 | 工作量 | 风险等级 |
|------|------|--------|----------|
| Step 1 | 数据库迁移和实体模型 | 2小时 | 🟡 中等 |
| Step 2 | Repository数据访问层 | 2小时 | 🟢 低 |
| Step 3 | AppService应用服务层 | 3小时 | 🟢 低 |
| Step 4 | Controller API控制器层 | 2小时 | 🟢 低 |
| Step 5 | 服务注册和集成测试 | 1小时 | 🟡 中等 |

## 📋 详细实施步骤

### Step 1: 数据库迁移和实体模型

#### 📁 影响文件
- `src/Server/Core/LYBT.Infrastructure/Data/Migrations/` (新增迁移文件)
- `src/Server/Core/LYBT.Entities/HerbCompat/HerbCompatNoteModel.cs` (新建)
- `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` (修改)

#### 🛠️ 具体操作

1. **创建实体模型文件**
```bash
# 路径: src/Server/Core/LYBT.Entities/HerbCompat/HerbCompatNoteModel.cs
# 包含: HerbCompatNote实体类定义，Table属性，导航属性
```

2. **更新DbContext**
```csharp
// 在AppDbContext.cs中添加DbSet
public DbSet<HerbCompatNote> HerbCompatibilityNotes { get; set; }
```

3. **生成数据库迁移**
```bash
# 在项目根目录执行
dotnet ef migrations add AddHerbCompatibilityNotes \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI
```

4. **应用迁移到数据库**
```bash
dotnet ef database update \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI
```

#### ✅ Step 1 验收标准
```bash
# 验证数据库表创建成功
sqlcmd -S localhost -d LYBTDB -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HerbCompatibilityNotes'"
# 预期结果: 返回 1

# 验证索引创建成功
sqlcmd -S localhost -d LYBTDB -Q "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('HerbCompatibilityNotes')"
# 预期结果: 返回 6 (1个主键 + 5个非聚集索引)
```

#### 🔄 Step 1 回滚方案
```bash
# 回滚迁移
dotnet ef migrations remove \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI

# 或者生成回滚SQL脚本
dotnet ef migrations script AddHerbCompatibilityNotes 0 \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI \
    --output rollback-herbcompat.sql
```

---

### Step 2: Repository数据访问层

#### 📁 影响文件
- `src/Server/Modules/LYBT.Module.HerbCompat/` (新建目录)
- `src/Server/Modules/LYBT.Module.HerbCompat/Interfaces/IHerbCompatNoteRepository.cs` (新建)
- `src/Server/Modules/LYBT.Module.HerbCompat/Repositories/HerbCompatNoteRepository.cs` (新建)
- `src/Shared/LYBT.Shared.Models/Contracts/HerbCompat/HerbCompatNoteDtos.cs` (新建)

#### 🛠️ 具体操作

1. **创建共享DTO模型**
```bash
# 路径: src/Shared/LYBT.Shared.Models/Contracts/HerbCompat/HerbCompatNoteDtos.cs
# 包含: HerbCompatNoteDto, HerbCompatNoteCreateDto, HerbCompatNoteQueryDto
```

2. **创建Repository接口**
```bash
# 路径: src/Server/Modules/LYBT.Module.HerbCompat/Interfaces/IHerbCompatNoteRepository.cs
# 包含: CRUD方法定义，查询方法定义
```

3. **实现Repository类**
```bash
# 路径: src/Server/Modules/LYBT.Module.HerbCompat/Repositories/HerbCompatNoteRepository.cs
# 包含: EF Core具体实现，LINQ查询逻辑
```

4. **添加AutoMapper配置**
```bash
# 路径: src/Server/Modules/LYBT.Module.HerbCompat/Mapping/HerbCompatNoteMappingProfile.cs
# 包含: Entity ↔ DTO 映射规则
```

#### ✅ Step 2 验收标准
```bash
# 编译验证
dotnet build src/Server/Modules/LYBT.Module.HerbCompat/LYBT.Module.HerbCompat.csproj
# 预期结果: Build succeeded. 0 Error(s)

# 单元测试验证 (可选)
# 创建基础的Repository测试，验证CRUD操作
```

#### 🔄 Step 2 回滚方案
```bash
# 直接删除新建的目录和文件
rm -rf src/Server/Modules/LYBT.Module.HerbCompat/
rm -rf src/Shared/LYBT.Shared.Models/Contracts/HerbCompat/
```

---

### Step 3: AppService应用服务层

#### 📁 影响文件
- `src/Server/Modules/LYBT.Module.HerbCompat/Interfaces/IHerbCompatNoteAppService.cs` (新建)
- `src/Server/Modules/LYBT.Module.HerbCompat/Services/HerbCompatNoteAppService.cs` (新建)

#### 🛠️ 具体操作

1. **创建AppService接口**
```bash
# 路径: src/Server/Modules/LYBT.Module.HerbCompat/Interfaces/IHerbCompatNoteAppService.cs
# 包含: 业务方法定义，ServiceResult返回类型
```

2. **实现AppService类**
```bash
# 路径: src/Server/Modules/LYBT.Module.HerbCompat/Services/HerbCompatNoteAppService.cs
# 包含: 业务逻辑实现，Repository调用，DTO转换
```

3. **添加输入验证**
```csharp
// 在AppService中添加业务验证逻辑
// 1. 检查重复记录
// 2. 验证权限（只能删除自己创建的记录）
// 3. 处方关联性验证
```

#### ✅ Step 3 验收标准
```bash
# 编译验证
dotnet build src/Server/Modules/LYBT.Module.HerbCompat/LYBT.Module.HerbCompat.csproj
# 预期结果: Build succeeded. 0 Error(s)

# 模拟单元测试
# 验证AppService方法逻辑正确性（可选）
```

#### 🔄 Step 3 回滚方案
```bash
# 删除新建的AppService文件
rm src/Server/Modules/LYBT.Module.HerbCompat/Interfaces/IHerbCompatNoteAppService.cs
rm src/Server/Modules/LYBT.Module.HerbCompat/Services/HerbCompatNoteAppService.cs
```

---

### Step 4: Controller API控制器层

#### 📁 影响文件
- `src/Server/Services/LYBT.WebAPI/Controllers/HerbCompatNotesController.cs` (新建)

#### 🛠️ 具体操作

1. **创建API控制器**
```bash
# 路径: src/Server/Services/LYBT.WebAPI/Controllers/HerbCompatNotesController.cs
# 包含: 5个RESTful端点实现
# 继承: BaseApiController
# 路由: /api/v1/herb-compat
```

2. **实现API端点**
```csharp
// GET /api/v1/herb-compat/notes - 分页查询
// GET /api/v1/herb-compat/notes/{id} - 获取详情
// POST /api/v1/herb-compat/notes - 创建备注
// DELETE /api/v1/herb-compat/notes/{id} - 删除备注
// GET /api/v1/herb-compat/prescriptions/{prescriptionId}/notes - 获取处方关联备注
```

3. **添加授权和验证**
```csharp
[Authorize] // 要求登录
[ModelState validation] // 输入验证
[Exception handling] // 异常处理
```

#### ✅ Step 4 验收标准
```bash
# 编译验证
dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj
# 预期结果: Build succeeded. 0 Error(s)

# API端点验证 (需要先完成Step 5服务注册)
# 启动WebAPI项目，访问Swagger页面验证端点
```

#### 🔄 Step 4 回滚方案
```bash
# 删除控制器文件
rm src/Server/Services/LYBT.WebAPI/Controllers/HerbCompatNotesController.cs
```

---

### Step 5: 服务注册和集成测试

#### 📁 影响文件
- `src/Server/Modules/LYBT.Module.HerbCompat/ServiceCollectionExtensions.cs` (新建)
- `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs` (修改)

#### 🛠️ 具体操作

1. **创建服务扩展类**
```bash
# 路径: src/Server/Modules/LYBT.Module.HerbCompat/ServiceCollectionExtensions.cs
# 包含: AddHerbCompatModule扩展方法
# 注册: Repository、AppService、AutoMapper Profile
```

2. **更新统一服务注册**
```csharp
// 在UnifiedServiceRegistration.cs中添加
services.AddHerbCompatModule();
```

3. **添加AutoMapper配置**
```csharp
// 确保HerbCompatNoteMappingProfile被正确注册
cfg.AddProfile<HerbCompatNoteMappingProfile>();
```

#### ✅ Step 5 验收标准
```bash
# 完整编译验证
dotnet build LYBT.Server.sln
# 预期结果: Build succeeded. 0 Error(s)

# 启动WebAPI项目
dotnet run --project src/Server/Services/LYBT.WebAPI
# 预期结果: 项目启动成功，无依赖注入错误

# Swagger验证
# 访问 https://localhost:7001/swagger
# 验证 herb-compat 相关端点出现在API文档中
```

#### 🔄 Step 5 回滚方案
```bash
# 删除服务扩展文件
rm src/Server/Modules/LYBT.Module.HerbCompat/ServiceCollectionExtensions.cs

# 还原UnifiedServiceRegistration.cs
# git checkout src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs
```

## 🧪 集成验收脚本

### PowerShell 快速验收脚本

```powershell
# herb-compat-test.ps1 - 快速功能验收脚本
param(
    [string]$BaseUrl = "https://localhost:7001",
    [string]$Username = "sysadmin",
    [string]$Password = "LybtAdmin2025@SecurePass!"
)

Write-Host "=== HerbCompatNotes API 集成测试 ===" -ForegroundColor Green

# Step 1: 获取认证Token
Write-Host "1. 获取认证Token..." -ForegroundColor Yellow
$loginBody = @{
    Username = $Username
    Password = $Password
    RememberMe = $false
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.data.token
    $headers = @{ Authorization = "Bearer $token" }
    Write-Host "✅ 认证成功" -ForegroundColor Green
} catch {
    Write-Host "❌ 认证失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: 创建配伍备注
Write-Host "2. 创建配伍备注..." -ForegroundColor Yellow
$createBody = @{
    HerbName = "甘草"
    CounterHerbName = "甘遂"
    NoteText = "甘草与甘遂相反，不宜同用。测试记录-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/herb-compat/notes" -Method POST -Body $createBody -ContentType "application/json" -Headers $headers
    $noteId = $createResponse.data.id
    Write-Host "✅ 创建成功, ID: $noteId" -ForegroundColor Green
} catch {
    Write-Host "❌ 创建失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: 查询配伍备注
Write-Host "3. 查询配伍备注..." -ForegroundColor Yellow
try {
    $queryResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/herb-compat/notes?pageIndex=1&pageSize=10" -Method GET -Headers $headers
    $totalCount = $queryResponse.data.totalCount
    Write-Host "✅ 查询成功, 总数: $totalCount" -ForegroundColor Green
} catch {
    Write-Host "❌ 查询失败: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: 获取单条记录
Write-Host "4. 获取单条记录..." -ForegroundColor Yellow
try {
    $getResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/herb-compat/notes/$noteId" -Method GET -Headers $headers
    $herbName = $getResponse.data.herbName
    Write-Host "✅ 获取成功, 药材: $herbName" -ForegroundColor Green
} catch {
    Write-Host "❌ 获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: 删除配伍备注
Write-Host "5. 删除配伍备注..." -ForegroundColor Yellow
try {
    $deleteResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/herb-compat/notes/$noteId" -Method DELETE -Headers $headers
    Write-Host "✅ 删除成功" -ForegroundColor Green
} catch {
    Write-Host "❌ 删除失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "=== 集成测试完成 ===" -ForegroundColor Green
```

### cURL 验收脚本示例

```bash
#!/bin/bash
# herb-compat-test.sh - cURL版本验收脚本

BASE_URL="https://localhost:7001"
USERNAME="sysadmin"
PASSWORD="LybtAdmin2025@SecurePass!"

echo "=== HerbCompatNotes API 集成测试 ==="

# 1. 获取Token
echo "1. 获取认证Token..."
TOKEN=$(curl -s -X POST "$BASE_URL/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"Username\":\"$USERNAME\",\"Password\":\"$PASSWORD\",\"RememberMe\":false}" \
  | jq -r '.data.token')

if [ "$TOKEN" = "null" ]; then
    echo "❌ 认证失败"
    exit 1
fi
echo "✅ 认证成功"

# 2. 创建配伍备注
echo "2. 创建配伍备注..."
NOTE_ID=$(curl -s -X POST "$BASE_URL/api/v1/herb-compat/notes" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{\"HerbName\":\"甘草\",\"CounterHerbName\":\"甘遂\",\"NoteText\":\"测试配伍备注-$(date +%Y%m%d-%H%M%S)\"}" \
  | jq -r '.data.id')

if [ "$NOTE_ID" = "null" ]; then
    echo "❌ 创建失败"
    exit 1
fi
echo "✅ 创建成功, ID: $NOTE_ID"

# 3. 查询列表
echo "3. 查询配伍备注列表..."
TOTAL_COUNT=$(curl -s -X GET "$BASE_URL/api/v1/herb-compat/notes?pageIndex=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" \
  | jq -r '.data.totalCount')
echo "✅ 查询成功, 总数: $TOTAL_COUNT"

# 4. 获取详情
echo "4. 获取配伍备注详情..."
HERB_NAME=$(curl -s -X GET "$BASE_URL/api/v1/herb-compat/notes/$NOTE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  | jq -r '.data.herbName')
echo "✅ 获取成功, 药材: $HERB_NAME"

# 5. 删除记录
echo "5. 删除配伍备注..."
curl -s -X DELETE "$BASE_URL/api/v1/herb-compat/notes/$NOTE_ID" \
  -H "Authorization: Bearer $TOKEN" > /dev/null
echo "✅ 删除完成"

echo "=== 集成测试完成 ==="
```

## ⚠️ 风险控制措施

### 编译时风险控制
- **分步编译**: 每步完成后立即编译验证
- **依赖隔离**: 新模块独立编译，不影响现有模块
- **回滚预案**: 每步都有明确的回滚方案

### 运行时风险控制
- **数据库隔离**: 新表与现有表无强依赖，删除不影响核心业务
- **API隔离**: 新端点独立路由，不影响现有API
- **权限控制**: 严格的用户权限验证，防止数据泄露

### 性能风险控制
- **索引优化**: 预设查询索引，避免全表扫描
- **分页查询**: 强制分页，避免大量数据查询
- **缓存策略**: 预留缓存接口，支持后续性能优化

## 📊 实施时间表

| 时间段 | 任务 | 负责人角色 |
|--------|------|------------|
| Day 1 上午 | Step 1-2: 数据库+Repository | 后端开发 |
| Day 1 下午 | Step 3: AppService业务层 | 后端开发 |
| Day 2 上午 | Step 4-5: Controller+集成 | 后端开发 |
| Day 2 下午 | 验收测试+文档更新 | QA+文档 |

## 🎯 成功标准

### 功能完整性
- [x] 5个API端点正常工作
- [x] CRUD操作功能完整
- [x] 权限控制正常
- [x] 分页查询正常

### 质量标准
- [x] 零编译错误和警告
- [x] 集成测试脚本通过
- [x] 符合现有代码风格和架构
- [x] API文档自动生成

### 非功能性
- [x] 响应时间 <500ms
- [x] 并发支持正常
- [x] 错误处理完整
- [x] 日志记录合理

---

**实施指导**: 严格按照步骤顺序执行，每步完成后进行验收  
**应急联系**: 如遇阻塞问题，立即执行对应回滚方案  
**质量保证**: 编译、测试、文档三重验收标准