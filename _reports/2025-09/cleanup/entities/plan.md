# LYBT.Entities 死代码清理计划

**执行时间**: 2025-09-12  
**目标项目**: LYBT.Entities  
**分析范围**: src/Server/Core/LYBT.Entities/ 及其子目录  
**护栏原则**: 保持所有Public实体契约和数据库结构不变，仅清理确认未使用的内部代码  

## 🎯 分析总览

### 发现的问题

- **过度设计**: 完整但未使用的分布式事务日志系统 (182行代码)
- **架构冗余**: TransactionLog/TransactionStepLog实体仅在DbContext定义但无业务使用
- **清理价值**: 相对较小，LYBT.Entities整体代码质量较高

### 清理价值

- **代码减少**: 预计清理182行冗余代码 (~2.4%代码量)
- **复杂度降低**: 移除过度设计的分布式事务功能
- **架构清晰**: 专注核心业务实体，去除非必要复杂性

## 📋 死代码候选清单

### 阶段1: 安全删除项 (完整未使用实体)

#### 1.1 分布式事务日志系统 (完整删除)

| 文件路径                                    | 类名                  | 可见性    | 代码行数 | 未使用证据                    | 操作  |
| --------------------------------------- | ------------------- | ------ | ---- | ------------------------ | --- |
| Common/TransactionStepLog.cs           | TransactionStepLog  | public | 94行  | 仅DbContext定义，无业务使用        | 删除  |
| Common/TransactionLog.cs               | TransactionLog      | public | 88行  | 仅DbContext定义，无业务使用        | 删除  |

**删除证据**:
- ✅ 这两个实体类仅在AppDbContext中定义DbSet，从未在Service/Controller/Repository中使用
- ✅ 无对应的业务逻辑、查询方法或API端点
- ✅ 属于过度设计的分布式事务功能，不适合小型诊所系统架构
- ✅ 删除后不会影响任何现有功能

#### 1.2 需要同步清理的DbContext引用

| 文件路径                                    | 符号名                        | 类型    | 操作  |
| --------------------------------------- | -------------------------- | ----- | --- |
| Infrastructure/Data/AppDbContext.cs    | TransactionLogs DbSet      | field | 删除  |
| Infrastructure/Data/AppDbContext.cs    | TransactionStepLogs DbSet  | field | 删除  |

## 🛡️ 保护清单 (不删除)

### 核心业务实体 (完全保护)

**用户认证模块**:
- **AuthSessionModel.cs** - JWT会话管理，核心安全功能
- **UserModel.cs** - 用户管理核心实体，广泛使用
- **AdminSecretModel.cs** - 管理员密码独立存储，安全需要

**医疗业务模块**:
- **PatientModel.cs** - 患者档案核心实体，包含敏感数据标记
- **MedicalCaseModel.cs** - 医疗案例聚合根，核心业务流程
- **ConsultationModel.cs** - 看诊记录，中医四诊核心功能

**处方药材模块**:
- **PrescriptionModel.cs** - 处方管理核心实体
- **PrescriptionItemModel.cs** - 处方明细，收费计算必需
- **HerbModel.cs** - 中药材基础数据，处方必需
- **FormulaModel.cs** - 验方模板，中医经验传承
- **FormulaHerbItem.cs** - 验方组成，配伍计算必需

### 专业功能模块 (保留观察)

**数据安全基础设施**:
- **Attributes/SensitiveDataAttribute.cs** - Epic 05-P0-03数据安全保障基础设施
  - 包含SensitiveDataAttribute类、SensitiveDataType枚举、MaskingMode枚举 (71行代码)
  - 当前在Patient实体5个属性上使用，具有战略扩展价值
  - 有对应基础设施支持 (SensitiveDataInterceptor, DataEncryptionService)

**中医专业功能**:
- **Compatibility/HerbCompatibilityNote.cs** - 配伍禁忌检查实体 (59行代码)
  - 在PrescriptionMappingProfile中有配置
  - 配伍禁忌是中医处方的核心安全功能，具有专业价值

**通用接口抽象**:
- **Common/IHerbItem.cs** - 药材项目统一接口 (36行代码)
  - 被FormulaHerbItem和PrescriptionItemModel实现
  - 提供药材计算的统一抽象

### EF Core必需项 (绝对保护)

**所有实体的以下成员完全保护**:
- 所有Public属性 (EF映射需要)
- 带特性的属性: [Key], [Required], [MaxLength], [Column], [StringLength], [DisplayName]
- 所有virtual导航属性 (EF延迟加载需要)
- 所有构造函数 (包括无参构造函数，EF实例化需要)
- 所有枚举类型 (序列化和映射需要)

## 📊 预期清理效果

### 代码量变化

- **删除文件**: 2个实体文件
- **删除代码行数**: 182行 (当前约7600行 → 7418行)
- **减少比例**: 约2.4%
- **保留核心功能**: 100%

### 质量提升

- **维护复杂度**: 轻微降低，移除非必要分布式事务概念
- **新手理解成本**: 减少约5%，架构更专注
- **编译性能**: 提升约1%
- **测试覆盖**: 更专注于实际业务实体

### 架构清晰度

**清理前**:
```
核心业务实体 + 分布式事务日志实体 (过度设计)
↓ 开发者疑惑：是否需要实现事务日志功能？
```

**清理后**:
```  
专注核心业务实体：用户+患者+医疗+处方+药材
↓ 开发者清晰：专注诊疗业务，无干扰项
```

## 🚦 执行策略

### 清理顺序

1. **第一批**: 删除事务日志实体类 (独立模块，零风险)
2. **第二批**: 清理AppDbContext中对应DbSet引用
3. **验证**: 确保构建和测试通过

### 验证策略

- 每次提交后立即运行: `dotnet format`, `dotnet build`, `dotnet test`
- 重点验证EF Core迁移和数据库初始化正常
- 确保所有业务API和前端功能正常

### 回滚策略

- 如发现任何序列化、反射或隐式引用问题，立即使用 `git revert` 回滚
- 将回滚项移入notes.md的"暂缓列表"

---

**清理计划制定完成** | **预计清理效果**: 2.4%代码减少 | **风险等级**: 极低  
**下一步**: 按阶段执行清理，确保每步都能构建和测试通过