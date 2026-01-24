# complete-management-create-logic

## Why

### 发现的问题

通过 `superpowers:brainstorm` 对管理界面新建逻辑的全面审查，发现以下问题：

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| `UserInputDtoValidator.cs` | 验证不足 | 仅验证UserName非空 | 完整验证Email/Phone/Role/RealName |
| `HerbMasterDetailViewModel.cs` | 功能未完成 | Import/Export有TODO | 完成文件对话框集成 |
| `FormulaMasterDetailViewModel.cs` | 功能未完成 | Export未实现 | 复用Herbs导出模式 |
| `UsersController.cs` | 格式不一致 | ApiVersion("1.0") | 统一为ApiVersion("1") |

### 影响分析

**模块影响范围**:
- Users模块：验证器增强，安全关键
- Herbs模块：导入导出功能完善
- Formula模块：导出功能完善
- 无数据库变更，无Breaking Change

**对比分析**:
- Patients模块：✅ 完整实现（8项验证，卡读写器集成，批量导入）
- Herbs模块：✅ 验证完整（10+规则），⚠️ UI导入导出未完成
- Formula模块：✅ 验证完整（嵌套验证），⚠️ 导出未完成
- Users模块：❌ 验证严重不足（仅1项）

## What Changes

### Phase 1: UserInputDtoValidator 验证完善 (Critical)

补全用户输入验证规则，对齐其他模块的验证标准：

- Email格式验证（可选字段，但填写时必须有效）
- Phone格式验证（中国手机号格式）
- Role枚举有效性验证
- RealName必填验证
- 密码复杂度验证（创建时）

**参考**: `HerbInputDtoValidator.cs` 的验证模式

### Phase 2: Herbs导入导出功能完成 (Medium)

完成 `HerbMasterDetailViewModel.cs` 中的TODO：

- 注入 `ICommonDialogService` 依赖
- 实现 `ImportHerbsAsync` 文件选择逻辑
- 实现 `ExportHerbsAsync` 文件保存逻辑
- 添加用户反馈（成功/失败提示）

### Phase 3: Formula导出功能完成 (Medium)

复用Herbs模块的导出模式：

- 注入 `ICommonDialogService` 依赖
- 实现 `ExportFormulasAsync` 完整逻辑
- 统一导出文件命名规范

### Phase 4: API版本格式统一 (Low)

- 修改 `UsersController.cs` 的 `ApiVersion("1.0")` 为 `ApiVersion("1")`
- 确保与其他Controller一致

## Architecture

### 变更影响范围

```
src/
├── Shared/
│   └── LYBT.Shared.Validators/
│       └── Users/
│           └── UserInputDtoValidator.cs  [修改] +30行
├── Server/
│   └── Services/LYBT.WebAPI/
│       └── Controllers/
│           └── UsersController.cs        [修改] 1行
└── Client/Desktop/Modules/
    ├── LYBT.Desktop.Herbs/
    │   └── ViewModels/
    │       └── HerbMasterDetailViewModel.cs  [修改] +20行
    └── LYBT.Desktop.Formula/
        └── ViewModels/
            └── FormulaMasterDetailViewModel.cs  [修改] +15行
```

## Impact

- **文件变更**: 4个文件
- **新增代码**: ~65行
- **风险等级**: Low
- **测试要求**: 手动测试各模块新建/导入导出功能

## Risks

| 风险 | 缓解措施 |
|------|----------|
| UserInputDto验证变严可能影响现有数据 | 验证规则使用When条件，仅对新建生效 |
| ICommonDialogService可能未注册 | 检查DI容器配置，确保服务已注册 |
| 导出文件格式不一致 | 统一使用Excel格式，复用现有导出逻辑 |

## References

- brainstorm分析: 管理界面新建逻辑审查
- 参考实现: `PatientInputDtoValidator.cs`, `HerbInputDtoValidator.cs`
- 相关模块CLAUDE.md: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/CLAUDE.md`

---

**创建时间**: 2026-01-23
**状态**: 待确认
