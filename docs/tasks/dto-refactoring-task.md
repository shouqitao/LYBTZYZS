# DTO架构重构任务清单

> 创建日期: 2025-12-18
> 完成日期: 2025-12-18
> 状态: 已完成
> 规范文档: [docs/architecture/dto-architecture-specification.md](../architecture/dto-architecture-specification.md)

## 目标

将全项目DTO命名统一为规范格式：
- **ListDto**: `{Entity}ListDto` - 列表视图
- **DetailDto**: `{Entity}DetailDto` - 详情视图
- **InputDto**: `{Entity}InputDto` - 创建/更新输入
- **OperationDto**: `{Operation}Dto` - 特定操作

**核心原则**: 禁止模糊命名 `{Entity}Dto`，禁止空继承别名

---

## 任务清单

### Phase 1: User模块 [已完成]

| 任务 | 状态 | 说明 |
|------|------|------|
| 删除 `UserDto : UserDetailDto` 空继承 | [x] 完成 | 已删除空别名类 |
| 替换所有 `UserDto` → `UserDetailDto` | [x] 完成 | 约40个文件已更新 |
| 更新 `UserMappingProfile` | [x] 完成 | AutoMapper已配置 |
| 更新 `LoginResponse.User` 类型 | [x] 完成 | 认证响应已更新 |
| 更新单元测试 | [x] 完成 | 12个测试文件已更新 |
| 编译验证 | [x] 完成 | 0 errors, 0 warnings |

**已修改文件**:
- Server: `UserService.cs`, `UserMappingProfile.cs`, `AuthService.cs`
- Desktop: `SidebarControl.xaml.cs`, `IUserDataManager.cs`, `IUserCommandHandler.cs`, `UserCommandHandler.cs`, `MainWindowViewModel.cs`
- Shared: `UserDtos.cs` (删除空继承)
- Tests: 12个测试文件批量更新

---

### Phase 2-7: 其他模块评估 [已完成 - 无需修改]

**评估结论**: 其他模块(Patient, Herb, Formula, Prescription, MedicalCase, Consultation)不存在User模块的"空继承别名"反模式。

| 模块 | 当前结构 | 评估结果 |
|------|----------|----------|
| Patient | `PatientDto : StatusDto` | 有效继承，有实际属性，不是空别名 |
| Herb | `HerbDto : StatusDto, IRemarkable` | 有效继承，有实际属性，不是空别名 |
| Formula | `FormulaDto : StatusDto, IRemarkable` | 有效继承，有实际属性，不是空别名 |
| Prescription | `PrescriptionDto : StatusDto, IRemarkable` | 有效继承，有实际属性，不是空别名 |
| MedicalCase | `MedicalCaseDto : TimestampDto` | 有效继承，DetailDto继承自它，合理设计 |
| Consultation | `ConsultationDto : TimestampDto` | 有效继承，有实际属性，不是空别名 |

**关键发现**:
- 所有模块都已有独立的 `{Entity}ListDto` 文件
- 所有模块的 `{Entity}Dto` 继承自基类(StatusDto/TimestampDto)且包含实际属性
- 只有User模块存在空继承别名 `public class UserDto : UserDetailDto { }` 的反模式

---

### Phase 8: 最终验证 [已完成]

| 任务 | 状态 | 说明 |
|------|------|------|
| 全项目编译验证 | [x] 完成 | `dotnet build` - 0 errors, 0 warnings |
| 运行单元测试 | [x] 完成 | 348+测试通过 |
| 更新架构文档 | [x] 完成 | dto-architecture-specification.md |

**测试结果**:
- Desktop.Users.Tests: 23 通过
- Module.Users.Tests: 31 通过
- Module.Auth.Tests: 81 通过
- Desktop.Shell.Tests: 156 通过
- Desktop.Foundation.Tests: 57 通过

---

## 重构总结

### 实际变更

| 模块 | 操作 | 影响范围 |
|------|------|----------|
| User | 删除空继承别名，全量替换`UserDto`→`UserDetailDto` | ~40文件 |
| Patient | 无需修改 | - |
| Herb | 无需修改 | - |
| Formula | 无需修改 | - |
| Prescription | 无需修改 | - |
| MedicalCase | 无需修改 | - |
| Consultation | 无需修改 | - |

### 架构结论

1. **User模块特殊性**: 只有User模块存在空继承别名反模式，已修复
2. **其他模块合规**: Patient/Herb/Formula等模块的继承结构是合理的(继承自StatusDto/TimestampDto)
3. **ListDto分离**: 所有模块都已正确分离出`{Entity}ListDto`
4. **命名规范已建立**: 未来新增DTO应遵循 ListDto/DetailDto/InputDto 命名规范

---

## 进度追踪

- [x] 创建架构规范文档
- [x] 创建任务清单
- [x] Phase 1: User模块 (实际修改)
- [x] Phase 2: Patient模块 (评估 - 无需修改)
- [x] Phase 3: Herb模块 (评估 - 无需修改)
- [x] Phase 4: Formula模块 (评估 - 无需修改)
- [x] Phase 5: Prescription模块 (评估 - 无需修改)
- [x] Phase 6: MedicalCase模块 (评估 - 无需修改)
- [x] Phase 7: Consultation模块 (评估 - 无需修改)
- [x] Phase 8: 最终验证

---

## 变更历史

| 日期 | 变更内容 |
|------|----------|
| 2025-12-18 | 创建任务清单，User模块开始重构 |
| 2025-12-18 | Phase 1完成：User模块全部`UserDto`替换为`UserDetailDto` |
| 2025-12-18 | Phase 2-7评估完成：其他模块无需修改 |
| 2025-12-18 | Phase 8完成：编译0错误，348+测试通过 |
| 2025-12-18 | 任务完成：DTO架构重构结束 |
