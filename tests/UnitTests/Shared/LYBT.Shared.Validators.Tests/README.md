# LYBT.Shared.Validators.Tests

> LYBT.Shared.Validators 业务输入验证器的单元测试

## 项目定位

- **层级**: UnitTests / Shared
- **被测模块**: LYBT.Shared.Validators
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| ChangePasswordRequestValidatorTests.cs | ChangePasswordRequestValidator | ~N |
| LoginRequestValidatorTests.cs | LoginRequestValidator | ~N |
| SuperAdminLoginRequestValidatorTests.cs | SuperAdminLoginRequestValidator | ~N |
| ConsultationInputDtoValidatorTests.cs | ConsultationInputDtoValidator | ~N |
| FormulaInputDtoValidatorTests.cs | FormulaInputDtoValidator | ~N |
| HerbInputDtoValidatorTests.cs | HerbInputDtoValidator | ~N |
| MedicalCaseInputDtoValidatorTests.cs | MedicalCaseInputDtoValidator | ~N |
| PatientInputDtoValidatorTests.cs | PatientInputDtoValidator | ~N |
| PrescriptionInputDtoValidatorTests.cs | PrescriptionInputDtoValidator | ~N |
| UserInputDtoValidatorTests.cs | UserInputDtoValidator | ~N |

## 运行方式

```bash
dotnet test tests/UnitTests/Shared/LYBT.Shared.Validators.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Shared.Validators
- LYBT.Shared.Models
- LYBT.Shared.Primitives
- 目标框架: net8.0

## 更新记录

- 2026-03-01: 创建 README
