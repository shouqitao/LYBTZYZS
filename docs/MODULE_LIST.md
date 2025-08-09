# 核心模块清单 - 凌隐宝堂中医诊所系统

**更新时间**: 2025年8月9日  
**版本**: 最终版（基于实际项目状态）

## 📋 8个核心模块总览

本系统采用模块化架构，共有8个核心业务模块，每个模块职责明确，遵循单一职责原则。

| # | 模块名 | 英文名 | 主要职责 | 数据表 | API路径 |
|---|-------|--------|---------|--------|---------|
| 1 | 身份认证 | Auth | JWT认证、权限管理 | AdminSecrets | `/api/v1/auth` |
| 2 | 用户管理 | Users | 用户CRUD、医生信息 | Users | `/api/v1/users` |
| 3 | 患者管理 | Patients | 患者档案、接待 | Patients | `/api/v1/patients` |
| 4 | 中药材 | Herbs | 药材信息、价格 | Herbs | `/api/v1/herbs` |
| 5 | 验方管理 | Formula | 经典验方、模板 | Formulas, FormulaItems | `/api/v1/formula` |
| 6 | 看诊管理 | Consultation | 中医四诊、诊断 | Consultations | `/api/v1/consultation` |
| 7 | 医疗案例 | MedicalCase | 病历、诊疗记录 | MedicalCases | `/api/v1/medicalcase` |
| 8 | 处方管理 | Prescriptions | 处方开具、打印 | Prescriptions, PrescriptionItems | `/api/v1/prescriptions` |

## 🏗️ 模块架构标准

每个模块都遵循统一的架构模式：

```
LYBT.Module.{ModuleName}/
├── {ModuleName}Module.cs           # 模块注册和DI配置
├── Interfaces/
│   ├── I{ModuleName}Repository.cs  # 数据访问接口
│   └── I{ModuleName}Service.cs     # 业务服务接口
├── Repositories/
│   └── {ModuleName}Repository.cs   # 数据访问实现
├── Services/
│   └── {ModuleName}Service.cs      # 业务服务实现
├── Mapping/
│   └── {ModuleName}MappingProfile.cs # AutoMapper配置
└── README.md                       # 模块文档
```

## 📖 模块详细说明

### 1. Auth模块 - 身份认证和授权

**主要功能**：
- JWT Token生成和验证
- 用户登录认证
- 系统管理员密码管理
- 登录会话管理

**核心类**：
- `AuthService` - 认证服务
- `AdminSecretModel` - 管理员密钥模型

**特点**：
- 支持普通用户和系统管理员双重认证
- JWT过期时间可配置（默认8小时）
- Remember Me功能（30天有效期）

### 2. Users模块 - 用户管理

**主要功能**：
- 用户CRUD操作
- 医生信息管理（合并设计）
- 用户角色和权限
- 用户状态管理

**核心类**：
- `UserService` - 用户业务服务
- `UserRepository` - 用户数据访问
- `BaseUserModel` - 统一用户模型（包含医生字段）

**特点**：
- 医生功能合并到用户模型
- 支持专长、挂号费、执业证书等医生字段
- 统一的用户状态管理

### 3. Patients模块 - 患者管理

**主要功能**：
- 患者档案管理
- 患者基础信息CRUD
- 快速接待功能（简化挂号）
- 患者搜索和筛选

**核心类**：
- `PatientService` - 患者业务服务
- `PatientRepository` - 患者数据访问
- `BasePatientModel` - 患者基础模型

**特点**：
- 整合了原Registration（挂号）模块的基础功能
- 支持患者快速登记和接待
- 与MedicalCase模块紧密关联

### 4. Herbs模块 - 中药材管理

**主要功能**：
- 中药材基础信息管理
- 药材价格管理
- 批量导入导出
- 处方用药支持

**核心类**：
- `HerbService` - 药材业务服务
- `HerbRepository` - 药材数据访问
- `BaseHerbModel` - 药材基础模型

**特点**：
- 仅管理药材信息，不涉及库存
- 支持批量操作和价格历史
- 专为处方开具优化

### 5. Formula模块 - 验方管理

**主要功能**：
- 经典验方模板管理
- 个人验方创建和维护
- 验方组合和应用
- 验方分类和标签

**核心类**：
- `FormulaService` - 验方业务服务
- `FormulaRepository` - 验方数据访问
- `FormulaModel` - 验方主模型
- `FormulaHerbItem` - 验方药材项

**特点**：
- 支持经典验方库和个人验方
- 可直接应用到处方
- 支持验方的组合和修改

### 6. Consultation模块 - 看诊管理

**主要功能**：
- 中医四诊录入（望闻问切）
- 诊断结果记录
- 治疗方案制定
- 看诊流程管理

**核心类**：
- `ConsultationService` - 看诊业务服务
- `ConsultationRepository` - 看诊数据访问
- `BaseConsultationModel` - 看诊基础模型

**特点**：
- 专为中医诊疗优化
- 支持复杂的四诊信息录入
- 与MedicalCase和Prescriptions集成

### 7. MedicalCase模块 - 医疗案例

**主要功能**：
- 完整诊疗案例管理
- 病历记录（原Records功能）
- 诊疗时间线
- 案例统计分析

**核心类**：
- `MedicalCaseService` - 案例业务服务
- `MedicalCaseRepository` - 案例数据访问
- `BaseMedicalCaseModel` - 案例基础模型

**特点**：
- 作为诊疗流程的聚合根
- 整合了原Records模块功能
- 贯穿整个诊疗流程

### 8. Prescriptions模块 - 处方管理

**主要功能**：
- 处方开具和编辑
- 处方打印和导出
- 处方历史管理
- 智能处方建议

**核心类**：
- `PrescriptionService` - 处方业务服务
- `IntelligentPrescriptionService` - 智能处方服务
- `PrescriptionRepository` - 处方数据访问
- `BasePrescriptionModel` - 处方基础模型

**特点**：
- 与Formula和Herbs深度集成
- 支持智能处方建议
- 完整的处方生命周期管理

## 🔗 模块间关系

### 核心数据流
```
Patients → MedicalCase → Consultation → Prescriptions
    ↓          ↓            ↓             ↓
  (患者)    (病历案例)     (看诊)       (处方)
    ↓          ↓            ↓             ↓
  Users    Records整合    四诊信息     Formula+Herbs
```

### 依赖关系
- **MedicalCase** - 核心聚合根，依赖Patients
- **Consultation** - 依赖MedicalCase和Users
- **Prescriptions** - 依赖Consultation、Formula、Herbs
- **Formula** - 依赖Herbs（验方包含药材）
- **Auth** - 被所有模块依赖（认证）

## 🗑️ 已删除/合并的模块

为简化系统复杂度，以下模块已被删除或合并：

| 原模块名 | 状态 | 合并到/原因 |
|---------|-----|-----------|
| Doctors | 🗑️ 删除 | 合并到Users模块 |
| Records | 🗑️ 删除 | 合并到MedicalCase模块 |
| Registration | 🗑️ 删除 | 简化到Patients模块 |
| Queueing | 🗑️ 删除 | 功能简化，不需要复杂排队 |
| Billing | 🗑️ 删除 | 超出诊所系统范围 |
| Pharmacy | 🗑️ 删除 | 本系统不管理药房库存 |
| TreatmentRoom | 🗑️ 删除 | 诊所规模不需要 |
| Diagnostics | 🗑️ 删除 | 合并到Consultation |
| DiagnosisTreatment | 🗑️ 删除 | 合并到Consultation |
| Sync | 🗑️ 删除 | 单机系统不需要同步 |

## 🚀 模块标准化状态

### 已完成标准化 ✅
- [x] Auth - Module.cs + Mapping配置
- [x] Users - Module.cs + Mapping配置
- [x] Patients - Module.cs + Mapping配置
- [x] Herbs - Module.cs + Mapping配置
- [x] Formula - Module.cs + Mapping配置
- [x] Consultation - Module.cs + Mapping配置
- [x] MedicalCase - Module.cs + Mapping配置
- [x] Prescriptions - Module.cs + Mapping配置

### 统一基础架构 ✅
- [x] IBaseService - 统一服务接口
- [x] IBaseRepository - 统一仓储接口
- [x] BaseRepository - 统一仓储实现
- [x] IModule - 统一模块注册接口

## 📝 开发规范

### 命名约定
- **模块**: `LYBT.Module.{ModuleName}`
- **服务**: `{ModuleName}Service`
- **仓储**: `{ModuleName}Repository`
- **模型**: `Base{ModuleName}Model`

### 文件组织
- 每个模块独立目录
- 标准的Interfaces/Services/Repositories结构
- 统一的Mapping配置
- 完整的模块注册

### 依赖注入
```csharp
// 在Program.cs中注册
services.AddAuthModule();
services.AddUsersModule();
services.AddPatientsModule();
// ... 其他模块
```

---

**重要提醒**: 本文档基于实际项目状态编写，如有新的模块变更请及时更新此文档。