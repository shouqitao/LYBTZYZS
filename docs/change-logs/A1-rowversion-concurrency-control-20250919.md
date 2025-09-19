# Phase A1: RowVersion 并发控制实施简报

**变更时间**: 2025-09-19  
**变更类型**: 数据库架构 + 业务逻辑优化  
**风险等级**: 中等（需要数据库迁移）

## 变更概览

为Patient、User、Prescription三个关键实体添加乐观并发控制，实施最小改动的事务安全优化策略。

## 具体变更

### 1. 实体模型增强
**文件修改**:
- `src/Server/Core/LYBT.Entities/Patients/PatientModel.cs`
- `src/Server/Core/LYBT.Entities/Users/UserModel.cs` 
- `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs`

**变更内容**:
```csharp
/// <summary>并发控制字段 - 乐观并发控制</summary>
[Timestamp]
[DisplayName("版本")]
public byte[] RowVersion { get; set; } = new byte[8];
```

### 2. EF Core 配置更新
**文件修改**: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`

**变更内容**:
```csharp
// 配置并发控制字段
entity.Property(u => u.RowVersion).IsRowVersion().IsConcurrencyToken();
entity.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken(); 
prescriptionEntity.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();
```

### 3. 数据库迁移
**文件新增**: `src/Server/Core/LYBT.Infrastructure/Migrations/20250919095302_AddRowVersionConcurrencyControl.cs`

**SQL 变更摘要**:
```sql
-- 为三个关键表添加 RowVersion 字段
ALTER TABLE [Users] ADD [RowVersion] rowversion NOT NULL;
ALTER TABLE [Patients] ADD [RowVersion] rowversion NOT NULL;  
ALTER TABLE [Prescriptions] ADD [RowVersion] rowversion NOT NULL;
```

### 4. 业务服务并发异常处理
**文件修改**:
- `src/Server/Modules/LYBT.Module.Users/Services/UserBusinessService.cs`
- `src/Server/Modules/LYBT.Module.Patients/Services/PatientBusinessService.cs`
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs`

**变更内容**:
```csharp
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "数据并发冲突: {Id}", id);
    return ServiceResult<T>.Failure("数据已被其他用户修改，请刷新后重试");
}
```

## 技术依据

**基于审计发现**:
- TX_AUDIT_REPORT.md 识别的高并发冲突风险实体
- TX_DECISION_CHECKLIST.md 的并发控制判据C2

**风险实体评估**:
- 🔴 Patient: 多前台同时修改患者信息
- 🔴 User: 管理员重置vs用户自己修改密码
- 🔴 Prescription: 医生修改处方时护士查看

## 预期效果

### 正面影响
1. **并发安全**: 防止多用户同时修改同一记录导致的数据覆盖
2. **用户体验**: 清晰的并发冲突提示，引导用户正确操作
3. **数据完整性**: SQL Server rowversion 提供可靠的版本控制

### 兼容性保证
1. **API兼容**: 现有API接口无变化，客户端无需修改
2. **数据兼容**: 新增字段非空但自动填充，无遗留数据问题
3. **性能影响**: rowversion开销极小，对查询性能影响可忽略

## 风险评估

### 潜在风险
1. **迁移风险**: 
   - 大表迁移可能耗时较长
   - 需要在业务低峰期执行
2. **并发冲突频率**:
   - 正常业务场景冲突概率较低（≤5%）
   - 批量导入等操作需要重构优化

### 缓解措施
1. **数据库迁移**:
   ```sql
   -- 先备份数据库
   BACKUP DATABASE [LYBT] TO DISK = 'LYBT_Backup_20250919.bak'
   
   -- 执行迁移（预估耗时：<5分钟）
   dotnet ef database update --project src/Server/Core/LYBT.Infrastructure
   ```

2. **回滚预案**:
   ```sql
   -- 紧急回滚：删除RowVersion列
   ALTER TABLE [Users] DROP COLUMN [RowVersion];
   ALTER TABLE [Patients] DROP COLUMN [RowVersion];  
   ALTER TABLE [Prescriptions] DROP COLUMN [RowVersion];
   ```

## 验证计划

### 单元测试
1. **并发冲突模拟**: 
   - 同时修改同一用户/患者/处方
   - 验证异常捕获和错误消息
2. **正常更新流程**: 
   - 确保无并发时更新正常
   - RowVersion自动更新

### 集成测试  
1. **多用户场景**: 
   - 2个管理员同时修改用户信息
   - 医生和护士同时操作处方
2. **性能基准**: 
   - 对比添加RowVersion前后的响应时间
   - 确保<100ms延迟增加

## 部署建议

### 执行时机
- **推荐**: 周末或业务低峰期（如晚上22:00-06:00）
- **估计停机时间**: 5-10分钟（迁移+验证）

### 部署步骤
1. 备份生产数据库
2. 部署代码到测试环境验证
3. 执行数据库迁移
4. 验证关键功能正常
5. 监控并发冲突日志

### 监控要点
```bash
# 监控并发冲突频率
grep "数据并发冲突" /logs/app.log | wc -l

# 检查RowVersion字段创建
SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
WHERE COLUMN_NAME = 'RowVersion'
```

## 下一步

**Phase B1**: 处方复制短事务外壳 - 基于当前并发控制基础，优化关键业务流程的事务边界。

---

**实施负责人**: 系统架构师  
**审核状态**: 待技术审查  
**文档版本**: v1.0