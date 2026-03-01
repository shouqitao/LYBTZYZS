# LYBT.Module.Patients.Tests

> 患者模块单元测试，覆盖患者控制器、仓储层及服务层的完整业务逻辑。

## 项目定位

- **层级**: UnitTests / Server / Modules
- **被测模块**: LYBT.Module.Patients
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Controllers/PatientsControllerTests.cs` | `PatientsController` | ~12 |
| `Repositories/PatientRepositoryTests.cs` | `PatientRepository` | ~5 |
| `Services/PatientServiceTests.cs` | `PatientService` | ~30 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Patients.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.Patients
- LYBT.Infrastructure
- LYBT.Entities
- Microsoft.AspNetCore.Mvc.Testing

## 更新记录

- 2026-03-01: 创建 README
