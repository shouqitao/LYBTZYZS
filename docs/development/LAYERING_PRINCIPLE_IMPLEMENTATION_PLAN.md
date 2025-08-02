# 分层原则实施计划

## 概述

当前系统中有大量DTO错误地放置在`LYBT.Models`项目中，需要按照三层架构原则迁移到`LYBT.Shared.Models`项目。

## 现状分析

### 统计信息
- 总计发现：76个DTO文件
- 影响模块：15个业务模块

### 模块清单及DTO数量
1. **Auth模块** (5个DTOs)
   - ChangeSysAdminPasswordDto.cs
   - LoginRequestDto.cs
   - LoginResponseDto.cs
   - LogoutRequestDto.cs
   - ChangePasswordRequestDto.cs

2. **Users模块** (8个DTOs)
   - ChangePasswordDto.cs
   - ChangeProfileDto.cs
   - ResetPasswordDto.cs
   - UserDetailDto.cs
   - UserQueryDto.cs
   - UserCreateDto.cs
   - BatchIdsDto.cs
   - UserDto.cs

3. **Patients模块** (6个DTOs)
   - AssignDoctorDto.cs
   - BatchIdsDto.cs
   - PatientDetailDto.cs
   - PatientDto.cs
   - PatientPagedQueryDto.cs
   - QuickPatientCreateDto.cs

4. **Doctors模块** (4个DTOs)
   - BatchIdsDto.cs
   - DoctorQueryDto.cs
   - DoctorDto.cs
   - DoctorDetailDto.cs

5. **Herbs模块** (7个DTOs)
   - HerbCreateDto.cs
   - HerbDetailDto.cs
   - HerbEditDto.cs
   - HerbImportDto.cs
   - HerbPagedQueryDto.cs
   - HerbStatusUpdateDto.cs
   - HerbDto.cs

6. **其他模块** (46个DTOs)
   - Billing (5个)
   - DiagnosisTreatment (6个)
   - FormulaTemplates (5个)
   - Pharmacy (4个)
   - Prescriptions (4个)
   - Queueing (4个)
   - Records (4个)
   - Registration (4个)
   - Sync (6个)
   - TreatmentRoom (4个)

## 实施策略

### 第一阶段：核心模块迁移（高优先级）
1. **Auth模块** - 认证相关，影响所有功能
2. **Users模块** - 用户管理，基础功能
3. **Patients模块** - 患者管理，核心业务
4. **Herbs模块** - 药材管理，核心业务

### 第二阶段：业务模块迁移（中优先级）
5. **Doctors模块** - 医生管理
6. **Prescriptions模块** - 处方管理
7. **DiagnosisTreatment模块** - 诊断治疗
8. **Billing模块** - 费用管理

### 第三阶段：辅助模块迁移（低优先级）
9. **Registration模块** - 挂号管理
10. **Queueing模块** - 排队管理
11. **Pharmacy模块** - 药房管理
12. **Records模块** - 病历管理
13. **FormulaTemplates模块** - 方剂模板
14. **TreatmentRoom模块** - 诊室管理
15. **Sync模块** - 数据同步

## 每个模块的迁移步骤

### 1. 创建目标文件夹
```
LYBT.Shared.Models/Contracts/[ModuleName]/
```

### 2. 迁移DTO文件
- 复制DTO文件到新位置
- 更新命名空间
- 检查并修复依赖关系

### 3. 更新引用
- 更新Service层引用
- 更新Controller层引用
- 更新AutoMapper配置

### 4. 测试验证
- 编译通过
- API测试通过

### 5. 删除旧文件
- 删除LYBT.Models中的旧DTO文件
- 删除空的Dtos文件夹

## 注意事项

### 1. 命名空间变更
```csharp
// 旧命名空间
namespace LYBT.Models.Users.Dtos

// 新命名空间
namespace LYBT.Shared.Models.Contracts.Users
```

### 2. 重复DTO处理
- 多个模块都有BatchIdsDto，考虑创建通用版本
- 检查是否有其他可以共享的DTO

### 3. 依赖关系
- 某些DTO可能依赖枚举或其他类型
- 确保依赖项也在正确的位置

### 4. 向后兼容
- 可以暂时保留旧DTO并标记为Obsolete
- 给前端足够的时间适配

## 预期收益

1. **代码组织更清晰**
   - Entity和DTO职责分离
   - 符合三层架构原则

2. **减少耦合**
   - 前后端通过DTO契约通信
   - 易于版本管理

3. **提高可维护性**
   - DTO集中管理
   - 便于文档生成

## 风险评估

- **风险等级**：中等
- **影响范围**：所有API和前端调用
- **缓解措施**：
  - 分阶段实施
  - 充分测试
  - 保留兼容层

## 时间估算

- 第一阶段：2-3天
- 第二阶段：2-3天
- 第三阶段：3-4天
- 总计：7-10天

## 当前进度

- [ ] Auth模块
- [ ] Users模块（部分完成）
- [ ] Patients模块（部分完成）
- [ ] Doctors模块（部分完成）
- [ ] Herbs模块（部分完成）
- [ ] 其他模块（待开始）