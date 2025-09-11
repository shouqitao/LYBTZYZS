# Record-Only 精简安全防护措施

## 概述

在执行 Pass 6 Record-Only 功能精简过程中，确保系统安全防护措施，防止误删关键功能、保护间接依赖、维护系统完整性。

## 🛡️ 核心功能保护清单

### 绝对不可删除的核心功能

**基础CRUD操作** (48个核心方法):
```csharp
// 8个模块 × 6个基础方法 = 48个保护方法
- CreateAsync()          // 新建记录
- GetByIdAsync()         // 获取详情  
- UpdateAsync()          // 更新记录
- DeleteAsync()          // 删除记录
- GetPagedAsync()        // 分页查询
- SearchAsync()          // 搜索筛选
```

**历史查询功能** (24个核心方法):
```csharp
// 核心历史查询保护
- GetPatientHistoryAsync()           # 患者就诊历史
- GetPrescriptionsByPatientAsync()   # 患者处方历史
- GetMedicalCasesByPatientAsync()    # 患者医案历史
- GetConsultationHistoryAsync()      # 诊断历史记录
- ... (共24个历史查询方法)
```

**认证授权功能** (8个核心方法):
```csharp  
- LoginAsync()                       # 用户登录
- LogoutAsync()                      # 用户登出
- ValidateTokenAsync()               # 令牌验证
- RefreshTokenAsync()                # 令牌刷新
- GetCurrentUserAsync()              # 当前用户
- ChangePasswordAsync()              # 修改密码
- [Authorize] 属性保护              # API权限控制
- BaseApiController 异常处理         # 统一异常处理
```

## 🔍 间接依赖风险识别

### Reflection 反射依赖保护

**受反射影响的功能区域**:
```csharp
// AutoMapper 配置文件依赖
src/Shared/LYBT.Shared.Mapping/Profiles/
├── UserMappingProfile.cs            # 保护 - DTO映射依赖
├── PatientMappingProfile.cs         # 保护 - 患者数据映射  
├── PrescriptionMappingProfile.cs    # 保护 - 处方映射
├── ... (其他MappingProfile)         # 全部保护

// EF Core DbContext 反射扫描
src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs
- DbSet<> 属性定义                   # 保护 - 实体集合定义
- OnModelCreating() 配置             # 保护 - 数据模型配置
- 实体关系映射配置                    # 保护 - 外键关系定义
```

**反射安全检查清单**:
1. **删除前检查**: 使用Grep搜索类名在整个解决方案中的引用
2. **动态类型检查**: 搜索 `typeof()` 和字符串类型引用  
3. **配置文件检查**: 检查appsettings.json中的类型配置
4. **依赖注入检查**: 检查ServiceCollection注册中的类型引用

### 序列化依赖保护

**JSON序列化影响评估**:
```csharp
// API响应DTO保护 (绝对不能删除)
src/Shared/LYBT.Shared.Models/Contracts/
├── Patients/PatientDto.cs           # 保护 - API响应依赖
├── MedicalCase/MedicalCaseDto.cs    # 保护 - 医案数据传输
├── Prescriptions/PrescriptionDto.cs # 保护 - 处方数据传输
├── ... (所有基础DTO)                 # 全部保护

// 枚举类型保护 (JSON序列化依赖)
src/Shared/LYBT.Shared.Models/Enums/SystemEnums.cs
- CompatibilityType 枚举             # 可删除 - 配伍相关
- CompatibilitySeverity 枚举         # 可删除 - 配伍相关
- CommonStatus 枚举                  # 保护 - 通用状态
- MedicalCaseStatus 枚举            # 简化 - 7种状态→2种状态
```

**序列化安全策略**:
1. **API契约保护**: 保留所有前后端通信使用的DTO
2. **枚举简化**: 只简化枚举值，不删除整个枚举类型
3. **向后兼容**: 保留已发布API的响应格式
4. **测试验证**: API序列化/反序列化完整性测试

### 依赖注入风险防护

**DI容器注册保护**:
```csharp
// 服务注册保护 (src/Server/Modules/*/ModuleServiceRegistration.cs)
services.AddScoped<IPatientService, PatientService>();        # 保护
services.AddScoped<IPrescriptionService, PrescriptionService>(); # 保护  
services.AddScoped<ICompatibilityNoteService, CompatibilityNoteService>(); # 删除

// Repository注册保护
services.AddScoped<IPatientRepository, PatientRepository>();  # 保护
services.AddScoped<ICompatibilityNoteRepository, CompatibilityNoteRepository>(); # 删除
```

**DI安全检查流程**:
1. **构造函数扫描**: 检查删除服务的构造函数注入使用情况
2. **接口引用检查**: 确保删除接口没有其他地方使用
3. **循环依赖验证**: 避免删除后形成循环依赖
4. **启动测试**: 每次删除后验证应用程序正常启动

### XAML绑定依赖保护

**WPF XAML绑定安全**:
```xml
<!-- 数据绑定保护检查 -->
<!-- 保护的ViewModel属性 -->
<DataGrid ItemsSource="{Binding Patients}"/>           <!-- 保护 -->
<TextBox Text="{Binding SelectedPatient.Name}"/>       <!-- 保护 -->

<!-- 删除的绑定检查 -->
<Button Command="{Binding CheckCompatibilityCommand}"/> <!-- 删除前检查 -->
<Chart DataSource="{Binding UsageStatistics}"/>        <!-- 删除前检查 -->
```

**XAML安全验证步骤**:
1. **绑定扫描**: 搜索删除属性在XAML中的Binding引用
2. **Command检查**: 搜索删除Command在XAML中的绑定
3. **资源引用**: 检查StaticResource和DynamicResource引用
4. **编译验证**: XAML编译错误检查

## 🧪 安全验证检查清单

### 删除前安全检查 (每个功能删除前必执行)

```bash
# 1. 全文搜索引用检查
grep -r "ClassName" src/                    # 搜索类名引用
grep -r "MethodName" src/                   # 搜索方法名引用
grep -r "PropertyName" src/                 # 搜索属性名引用

# 2. 配置文件检查
grep -r "删除的类型" appsettings*.json      # 检查配置引用
grep -r "删除的类型" *.config               # 检查配置文件

# 3. 反射引用检查
grep -r "typeof.*删除的类" src/             # 检查typeof引用
grep -r "\"删除的类名\"" src/               # 检查字符串类型引用

# 4. 依赖注入检查
grep -r "Add.*<.*删除的接口" src/           # 检查DI注册
grep -r "删除的接口" src/*/构造函数         # 检查构造函数注入

# 5. XAML绑定检查 (WPF项目)
grep -r "删除的属性" src/**/*.xaml          # 检查数据绑定
grep -r "删除的Command" src/**/*.xaml       # 检查命令绑定
```

### 删除后验证检查 (每个功能删除后必执行)

```bash
# 1. 编译完整性验证
dotnet build LYBT.All.sln --verbosity minimal   # 确保零错误零警告

# 2. 启动完整性验证  
dotnet run --project src/Server/Services/LYBT.WebAPI  # 确保应用启动成功

# 3. DI容器验证
# 启动应用时检查依赖注入容器是否正常构建

# 4. API端点验证
curl -X GET http://localhost:5001/api/v1/health  # 健康检查端点

# 5. 数据库连接验证
# 确保EF Core DbContext正常初始化和迁移
```

## 📋 分阶段安全防护策略

### Phase 6-A 安全防护 (低风险)

**统计分析功能删除防护**:
- ✅ 统计相关类可以安全删除 (无关键依赖)
- ⚠️ 注意检查Controller中的依赖注入引用
- ✅ 前端统计图表可以直接移除

**智能推荐功能删除防护**:
- ⚠️ 检查推荐接口是否被其他模块调用
- ✅ 推荐算法逻辑可以安全删除
- ⚠️ 前端推荐面板删除前检查XAML绑定

**价格计算功能删除防护**:
- ✅ 自动计算逻辑可以安全删除
- ⚠️ 保留手动价格输入功能
- ✅ 价格相关DTO保持完整 (API兼容性)

### Phase 6-B 安全防护 (中风险)

**配伍检查系统删除防护**:
- 🔴 **高风险**: 涉及数据库表删除，需要完整备份
- ⚠️ 检查HerbCompatibilityNote实体的FK关系
- ✅ 配伍相关枚举可以安全删除
- 🔴 **迁移风险**: EF Core迁移需要谨慎测试

**验方套用系统防护**:
- ✅ 套用逻辑可以安全删除
- ⚠️ 保留Formula实体和基础CRUD功能
- ✅ 前端套用界面可以简化移除

**患者状态管理防护**:
- ⚠️ 保留患者基础信息不受影响
- ⚠️ 状态枚举简化而不是删除
- ✅ 状态管理UI可以安全移除

### Phase 6-C 安全防护 (高风险)

**医案状态流转简化防护**:
- 🔴 **极高风险**: 涉及核心业务流程
- 🔴 **数据风险**: 现有医案状态数据迁移
- ⚠️ 保留基础的2种状态 (进行中/已完成)  
- 🔴 **测试要求**: 完整的业务流程测试

**用户权限系统简化防护**:
- 🔴 **安全风险**: 权限系统是安全基础
- ⚠️ 保留基础认证功能不受影响
- ⚠️ 简化权限检查逻辑，保留核心检查
- 🔴 **测试要求**: 完整的安全测试

**JWT会话管理简化防护**:
- ⚠️ 保留基础JWT认证功能
- ✅ 会话管理表可以安全删除
- ⚠️ 保留token验证和刷新功能
- ✅ 会话监控功能可以安全移除

## 🔒 数据安全防护

### 数据库备份策略

**完整备份时机**:
- Pass 6-B 开始前 (配伍表删除前)
- Pass 6-C 开始前 (状态数据修改前)
- 每个高风险任务开始前

**备份内容**:
```sql
-- 完整数据库备份
BACKUP DATABASE LYBTZYZS TO DISK = 'backup/LYBTZYZS_Pass6_Before_{timestamp}.bak'

-- 关键表单独备份
SELECT * INTO HerbCompatibilityNotes_Backup FROM HerbCompatibilityNotes;
SELECT * INTO MedicalCases_Status_Backup FROM MedicalCases;
SELECT * INTO AuthSessions_Backup FROM AuthSessions;
```

### 数据迁移安全

**EF Core迁移防护**:
```csharp
// 安全的迁移模式 - 先添加新字段，再删除旧字段
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. 数据迁移 (如果需要)
    migrationBuilder.Sql("UPDATE MedicalCases SET NewStatus = CASE WHEN Status IN (1,2,3) THEN 1 ELSE 2 END");
    
    // 2. 删除约束
    migrationBuilder.DropForeignKey("FK_Prescriptions_CompatibilityNotes");
    
    // 3. 删除表
    migrationBuilder.DropTable("HerbCompatibilityNotes");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // 完整的回滚逻辑
    migrationBuilder.CreateTable("HerbCompatibilityNotes", /* 完整表定义 */);
    // ... 完整回滚步骤
}
```

## 📊 安全验证报告模板

### 功能删除安全报告

每个功能删除完成后，生成以下安全报告：

```markdown
# {功能名称} 删除安全报告

## 删除内容
- [ ] 删除的类数量: {数量}
- [ ] 删除的方法数量: {数量}  
- [ ] 删除的API端点数量: {数量}
- [ ] 删除的UI组件数量: {数量}

## 安全检查结果
- [ ] 反射依赖检查: 通过/发现问题
- [ ] 序列化依赖检查: 通过/发现问题
- [ ] DI容器检查: 通过/发现问题
- [ ] XAML绑定检查: 通过/发现问题

## 编译和测试结果
- [ ] 编译状态: 0警告 0错误
- [ ] 应用启动: 正常
- [ ] 核心功能测试: 全部通过
- [ ] API健康检查: 正常

## 性能影响
- [ ] 内存使用变化: {百分比}
- [ ] 启动时间变化: {毫秒}
- [ ] API响应时间变化: {毫秒}

## 回滚验证
- [ ] 回滚分支可用性: 已验证
- [ ] 数据备份完整性: 已验证
- [ ] 回滚步骤文档: 已完成
```

## ⚠️ 紧急回滚程序

### 快速回滚步骤

```bash
# 1. 立即停止当前操作
git stash  # 保存当前未提交的变更

# 2. 切换到备份分支
git checkout backup/{task-id}

# 3. 创建回滚分支
git checkout -b rollback/{task-id}-{timestamp}

# 4. 恢复数据库 (如果涉及数据库变更)
sqlcmd -S localhost -Q "RESTORE DATABASE LYBTZYZS FROM DISK='backup/LYBTZYZS_Pass6_Before_{timestamp}.bak' WITH REPLACE"

# 5. 验证系统状态
dotnet build LYBT.All.sln
dotnet run --project src/Server/Services/LYBT.WebAPI

# 6. 记录回滚原因
echo "回滚原因: {详细原因描述}" > rollback-report-{timestamp}.md
```

### 回滚决策标准

**立即回滚情况**:
- 🔴 编译错误超过5个无法快速解决
- 🔴 应用程序无法启动
- 🔴 核心CRUD功能受到影响
- 🔴 数据库迁移失败
- 🔴 关键API端点无响应

**延迟回滚情况** (先尝试修复):
- 🟡 少量警告信息
- 🟡 非关键功能异常
- 🟡 UI界面显示问题
- 🟡 性能轻微下降

## 🎯 最终安全验收标准

### Pass 6 完成后的系统完整性验证

```bash
# 1. 完整编译验证
dotnet build LYBT.All.sln --verbosity minimal
# 期望结果: 0 Warning(s), 0 Error(s)

# 2. 完整测试验证  
dotnet test --verbosity normal
# 期望结果: 所有保留的测试用例100%通过

# 3. 应用启动验证
dotnet run --project src/Server/Services/LYBT.WebAPI
# 期望结果: 应用正常启动，无依赖注入错误

# 4. 数据库连接验证
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure
# 期望结果: 所有迁移成功执行

# 5. API功能验证
curl -X GET http://localhost:5001/api/v1/patients
# 期望结果: 返回正常数据响应

# 6. 前端功能验证
# 手动测试: 患者管理、医案创建、处方录入、验方查询
# 期望结果: 所有Record-Only核心功能正常
```

**最终成功标准**: 系统精简45%代码量的同时，核心Record-Only功能100%保持完整，性能提升40%以上。