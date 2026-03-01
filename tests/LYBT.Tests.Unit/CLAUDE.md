# LYBT.Tests.Unit

服务端核心单元测试项目。覆盖实体模型、基础设施服务、共享日志、安全工具。

## 项目基本信息

- **目标框架**: net8.0
- **测试框架**: xunit + FluentAssertions + NSubstitute
- **项目引用**: LYBT.Entities, LYBT.Infrastructure, LYBT.Shared.Utilities, LYBT.Shared.Models, LYBT.Shared.Logging
- **总测试方法数**: 约 260 个 ([Fact] + [Theory])

## 目录结构

```
tests/LYBT.Tests.Unit/
├── GlobalUsings.cs
├── Entities/
│   ├── Auth/AuthSessionModelTests.cs                 # AuthSession 实体 (19)
│   ├── Common/BaseEntityTests.cs                     # BaseEntity 基类 (12)
│   ├── Common/SystemLogTests.cs                      # SystemLog 日志实体 (14)
│   ├── Consultation/ConsultationModelTests.cs        # Consultation 诊断实体 (10)
│   ├── Formula/FormulaHerbItemTests.cs               # FormulaHerbItem 明细 (21)
│   ├── Formula/FormulaModelTests.cs                  # Formula 验方实体 (19)
│   ├── Herbs/HerbModelTests.cs                       # Herb 中药材实体 (21)
│   ├── MedicalCase/MedicalCaseModelTests.cs          # MedicalCase 聚合根 (17)
│   ├── Patients/PatientModelTests.cs                 # Patient 患者实体 (31)
│   ├── Prescriptions/PrescriptionModelTests.cs       # Prescription 处方 (8)
│   └── Users/UserModelTests.cs                       # User 用户实体 (19)
├── Infrastructure/
│   ├── Serialization/SensitiveDataJsonConverterTests.cs  # 脱敏序列化 (4)
│   └── Services/BaseServiceTests.cs                      # BaseService 权限验证 (12)
├── Shared/Logging/
│   ├── CorrelationIdEnricherTests.cs                 # 日志关联ID (6)
│   ├── LoggingLevelManagerTests.cs                   # 日志级别动态切换 (10)
│   └── SensitiveDataMaskerTests.cs                   # 敏感数据脱敏 (16)
└── Utilities/Security/
    ├── PasswordHelperTests.cs                        # 密码哈希和验证 (52)
    └── PasswordPolicyValidatorTests.cs               # 密码策略校验 (26)
```

## 测试覆盖映射

| 测试类 | 被测目标 | 测试数 |
|--------|----------|--------|
| AuthSessionModelTests | LYBT.Entities.Auth.AuthSession | 19 |
| BaseEntityTests | LYBT.Entities.Common.BaseEntity | 12 |
| SystemLogTests | LYBT.Entities.Common.SystemLog | 14 |
| ConsultationModelTests | LYBT.Entities.Consultations.Consultation | 10 |
| FormulaHerbItemTests | LYBT.Entities.Formulas.FormulaHerbItem | 21 |
| FormulaModelTests | LYBT.Entities.Formulas.Formula | 19 |
| HerbModelTests | LYBT.Entities.Herbs.Herb | 21 |
| MedicalCaseModelTests | LYBT.Entities.MedicalCases.MedicalCase | 17 |
| PatientModelTests | LYBT.Entities.Patients.Patient | 31 |
| PrescriptionModelTests | LYBT.Entities.Prescriptions.Prescription | 8 |
| UserModelTests | LYBT.Entities.Users.User | 19 |
| SensitiveDataJsonConverterTests | Infrastructure.Serialization.SensitiveDataJsonConverterFactory | 4 |
| BaseServiceTests | Infrastructure.Services.BaseService | 12 |
| CorrelationIdEnricherTests | Shared.Logging.Enrichers.CorrelationIdEnricher | 6 |
| LoggingLevelManagerTests | Shared.Logging.Management.LoggingLevelManager | 10 |
| SensitiveDataMaskerTests | Shared.Logging.Masking.SensitiveDataMasker | 16 |
| PasswordHelperTests | Shared.Utilities.Security.PasswordHelper | 52 |
| PasswordPolicyValidatorTests | Shared.Utilities.Security.PasswordPolicyValidator | 26 |

## 测试模式

- **AAA 模式**: 所有测试遵循 Arrange/Act/Assert，注释标记三段
- **实体测试**: 构造函数默认值验证 + 业务方法行为 (如 MedicalCase 计算属性)
- **参数化测试**: `[Theory]+[InlineData]` 用于密码策略、脱敏模式等多输入场景
- **Mock**: NSubstitute 用于 Logger/ICorrelationIdProvider 等接口

## 覆盖空白

- Formula.Herbs 集合操作 (添加/删除药材明细) 无测试
- MedicalCase 状态转换方法 (Complete/Lock) 未覆盖
- BaseService 权限验证仅覆盖 ValidateEditPermission
- SensitiveDataJsonConverterTests 仅 4 个测试，嵌套对象/反序列化未覆盖

## 已删除文件

以下测试随对应源代码删除，无覆盖缺口:
- ConfigurationHelperTests, EnvironmentHelperTests
- ApplicationInitializationExtensionsTests
- ClaimsHelperTests, RoleHelperTests

## 与 UnitTests 的关系

`tests/UnitTests/` 下存在另一组服务端模块级单元测试 (LYBT.Infrastructure.Tests, LYBT.Module.Auth.Tests 等)，本项目聚焦实体和共享工具，UnitTests 聚焦模块 Service/Repository 层。

---
最后更新: 2026-03-01
