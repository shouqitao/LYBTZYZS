# 状态字段统一迁移文档

**迁移名称**: UnifyStatusFieldsToInt
**创建日期**: 2025-09-21
**Issue**: #624

## 概述

将所有实体的状态字段从 string 存储统一为 int 存储，使用 EF Core 的 HasConversion<int>() 配置。

## 涉及的实体和枚举

### 1. 已配置的实体
- **User**: Status (CommonStatus) - ✅ 已配置
- **AuthSession**: Status (CommonStatus) - ✅ 已配置
- **Patient**: Status (CommonStatus) - ✅ 已配置
- **Herb**: Status (CommonStatus) - ✅ 已配置
- **Formula**: Status (CommonStatus) - ✅ 已配置

### 2. 本次修改的实体
- **MedicalCase**: Status (MedicalCaseStatus) - 从 string 改为 int
- **Prescription**: Status (PrescriptionStatus) - 新增配置

## 枚举定义

### CommonStatus (SystemEnums.cs)
```csharp
public enum CommonStatus
{
    Disabled = 0,  // 禁用
    Enabled = 1    // 启用
}
```

### MedicalCaseStatus (MedicalCaseEnums.cs)
```csharp
public enum MedicalCaseStatus
{
    Active = 10,   // 活跃状态
    Closed = 20    // 已关闭
}
```

### PrescriptionStatus (PrescriptionStatus.cs)
```csharp
public enum PrescriptionStatus
{
    Draft = 0,      // 草稿
    Completed = 1   // 已完成
}
```

## 数据库变更

### MedicalCases 表
- **Status 字段**: nvarchar(450) → int
- **索引更新**: UX_MedicalCases_Patient_ActiveOnly
  - 旧过滤: `[Status] = 'Active' OR [Status] = 'Draft'`
  - 新过滤: `[Status] = 10` (只允许一个活跃状态)

## 迁移脚本

### 应用迁移
```bash
# 在 worktree 中应用迁移
cd .worktrees/issue-624
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

### 回滚迁移
```bash
# 回滚到上一个迁移
cd .worktrees/issue-624
dotnet ef database update [PreviousMigrationName] --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# 或者移除迁移（如果还未应用到数据库）
dotnet ef migrations remove --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

## 数据迁移注意事项

### MedicalCase 状态映射
需要在迁移前执行数据更新脚本：
```sql
-- 备份原始数据
SELECT Id, Status INTO #MedicalCasesBackup FROM MedicalCases;

-- 数据映射（迁移前执行）
UPDATE MedicalCases
SET Status = CASE
    WHEN Status IN ('Active', 'Registered', 'InConsultation', 'Suspended') THEN '10'
    WHEN Status IN ('Closed', 'Completed', 'Cancelled', 'Archived') THEN '20'
    ELSE '10' -- 默认为活跃
END;
```

### 回滚时的数据恢复
```sql
-- 从备份恢复（如需回滚）
UPDATE MedicalCases
SET Status = b.Status
FROM MedicalCases m
INNER JOIN #MedicalCasesBackup b ON m.Id = b.Id;
```

## 验证检查

### 1. 编译验证
```bash
dotnet build LYBT.Server.sln
```

### 2. 数据库架构验证
```sql
-- 检查 MedicalCases 表的 Status 字段类型
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'MedicalCases'
AND COLUMN_NAME = 'Status';

-- 检查索引
SELECT
    i.name AS index_name,
    i.filter_definition
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('MedicalCases')
AND i.name = 'UX_MedicalCases_Patient_ActiveOnly';
```

### 3. 数据完整性验证
```sql
-- 验证状态值范围
SELECT DISTINCT Status FROM MedicalCases;
SELECT DISTINCT Status FROM Prescriptions;

-- 验证唯一约束
SELECT PatientId, COUNT(*) as ActiveCount
FROM MedicalCases
WHERE Status = 10
GROUP BY PatientId
HAVING COUNT(*) > 1;
```

## 风险和缓解措施

### 风险
1. **数据丢失风险**: string 到 int 转换可能导致数据丢失
2. **业务中断风险**: 状态值变化可能影响业务逻辑
3. **索引重建风险**: 大表索引重建可能耗时

### 缓解措施
1. **数据备份**: 迁移前完整备份数据库
2. **分阶段部署**: 先在测试环境验证
3. **回滚计划**: 保留回滚脚本和数据映射表
4. **监控告警**: 迁移后监控错误日志

## 后续任务
- [ ] 更新所有使用状态字段的查询和业务逻辑
- [ ] 更新前端对应的状态显示逻辑
- [ ] 更新 API 文档中的状态值说明
- [ ] 性能测试验证索引效果