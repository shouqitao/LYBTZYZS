# Desktop层综合重构优化 - 任务分解

**Change ID**: refactor-desktop-comprehensive
**Created**: 2025-12-30
**Total Phases**: 6
**Estimated Effort**: 60-80小时

---

## Phase 0: 代码清理与规范化 (进行中)

**目标**: 清理历史积累的无用代码，统一目录结构和命名规范
**预估工时**: 8小时
**依赖**: 无

### Task 0.1: 废弃模块清理 [已完成]

**优先级**: P0
**工时**: 0.5小时
**状态**: 已完成

**已完成项**:
- [x] 删除 `LYBT.Desktop.Consultation/` 残留目录
- [x] 清理obj缓存文件

---

### Task 0.2: 目录结构标准化 [已完成]

**优先级**: P0
**工时**: 1小时
**状态**: 已完成

**已完成项**:
- [x] Herbs模块: `Contracts/` -> `Interfaces/` 迁移
- [x] 更新5个文件的命名空间引用
- [x] 编译验证通过

---

### Task 0.3: 全模块目录结构审查

**优先级**: P1
**工时**: 2小时
**状态**: 待执行

**文件**:
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Patients/` 审查
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Formula/` 审查
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Users/` 审查
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Auth/` 审查
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/` 审查
- [ ] `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/` 审查

**检查项**:
1. 是否存在 `Contracts/` vs `Interfaces/` 不一致
2. 是否存在废弃的空目录
3. 是否存在未使用的文件
4. 目录结构是否符合规范

**验收**:
- [ ] 所有模块目录结构统一
- [ ] 无空目录残留
- [ ] 无未使用文件

---

### Task 0.4: 废弃代码/注释清理

**优先级**: P1
**工时**: 2小时
**状态**: 待执行

**检查项**:
1. 注释掉的代码块 (// TODO: remove, /* deprecated */)
2. 标记为 [Obsolete] 但未删除的代码
3. 空实现或占位符方法
4. 未使用的 using 声明

**工具**:
```bash
# 查找注释掉的代码
grep -rn "// TODO" --include="*.cs" src/Client/Desktop/
grep -rn "\[Obsolete\]" --include="*.cs" src/Client/Desktop/
```

**验收**:
- [ ] 无废弃代码块
- [ ] 无过期注释
- [ ] 编译0警告

---

### Task 0.5: 命名规范统一

**优先级**: P2
**工时**: 2小时
**状态**: 待执行

**规范**:
| 类型 | 命名规范 | 示例 |
|------|----------|------|
| 接口目录 | `Interfaces/` | 非 `Contracts/` |
| 服务接口 | `I{Name}Service` | `IHerbService` |
| 仓库接口 | `I{Name}Repository` | `IHerbRepository` |
| 服务实现 | `{Name}Service` | `HerbService` |
| 仓库实现 | `{Name}Repository` | `HerbRepository` |
| 处理器 | `{Name}Handler` | `PrescriptionItemHandler` |
| 视图模型 | `{Name}ViewModel` | `HerbDetailViewModel` |

**验收**:
- [ ] 100%符合命名规范
- [ ] 无混用情况

---

## Phase 1: 基础设施层建设

**目标**: 建立统一的基础设施和服务接口
**预估工时**: 10小时
**依赖**: Phase 0完成

### Task 1.1: 添加CommunityToolkit.Mvvm依赖

**优先级**: P0
**工时**: 0.5小时

**文件**:
- [ ] `Directory.Packages.props` 或各模块csproj

**验收**:
- [ ] 编译通过
- [ ] 无版本冲突

---

### Task 1.2: 创建标准服务接口

**优先级**: P0
**工时**: 3小时

**新建文件**:
- [ ] `Core/LYBT.Desktop.Contracts/Services/IMasterDetailServices.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/ILoadingStateManager.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/IPaginationService.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/ISearchService.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/ISelectionService.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/IDetailEditorService.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/IDialogManager.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/IViewNavigationService.cs`
- [ ] `Core/LYBT.Desktop.Contracts/Services/IErrorHandler.cs`

**验收**:
- [ ] 接口定义完整
- [ ] XML文档齐全

---

### Task 1.3: 实现标准服务

**优先级**: P0
**工时**: 4小时

**新建文件**:
- [ ] `Core/LYBT.Desktop.Infrastructure/Services/MasterDetailServices.cs`
- [ ] 各服务实现类

**验收**:
- [ ] 所有接口实现
- [ ] DI注册正确
- [ ] 单元测试通过

---

### Task 1.4: 创建CommandResult标准返回类型

**优先级**: P0
**工时**: 1小时

**新建文件**:
- [ ] `Core/LYBT.Desktop.Contracts/Results/CommandResult.cs`

```csharp
public record CommandResult<T>(
    bool Success,
    T? Data = default,
    string? Error = null
);
```

**验收**:
- [ ] 支持泛型
- [ ] 包含静态工厂方法

---

### Task 1.5: DTO命名规范化

**优先级**: P1
**工时**: 1.5小时

**规范**:
| 用途 | 后缀 | 示例 |
|------|------|------|
| 列表展示 | `ListDto` | `HerbListDto` |
| 详情展示 | `DetailDto` | `HerbDetailDto` |
| 创建/更新输入 | `InputDto` | `HerbInputDto` |
| 保存请求 | `SaveDto` | `MedicalCaseSaveDto` |

**验收**:
- [ ] 所有DTO符合规范
- [ ] 编译通过

---

## Phase 2: 数据层统一

**目标**: 所有模块使用统一的Service+Repository模式
**预估工时**: 12小时
**依赖**: Phase 1完成

### Task 2.1: Herbs模块数据层 [已完成]

**状态**: 已完成 (参考实现)

**结构**:
```
Interfaces/
  ├── IHerbService.cs
  └── IHerbRepository.cs
Services/
  └── HerbService.cs
Repositories/
  └── HerbRepository.cs
```

---

### Task 2.2: Formula模块数据层对齐

**优先级**: P1
**工时**: 2小时

**文件**:
- [ ] 检查 `IFormulaService` 接口
- [ ] 检查 `IFormulaRepository` 接口
- [ ] 统一返回类型

---

### Task 2.3: Patients模块数据层对齐

**优先级**: P1
**工时**: 2小时

**文件**:
- [ ] 检查 `IPatientService` 接口
- [ ] 检查 `IPatientRepository` 接口
- [ ] 统一返回类型

---

### Task 2.4: Users模块数据层对齐

**优先级**: P2
**工时**: 1.5小时

---

### Task 2.5: MedicalCase模块数据层审查

**优先级**: P1
**工时**: 3小时

**特殊处理**:
- 聚合根特殊逻辑
- 多个子服务协调
- 状态机管理

---

### Task 2.6: 返回类型统一化

**优先级**: P1
**工时**: 2小时

**目标**: 所有Service方法返回 `CommandResult<T>` 或 `Task<CommandResult<T>>`

---

## Phase 3: ViewModel层重构

**目标**: ViewModel瘦身，职责分离
**预估工时**: 15小时
**依赖**: Phase 2完成

### Task 3.1: ViewModel基类优化

**优先级**: P0
**工时**: 2小时

**文件**:
- [ ] `Core/LYBT.Desktop.Models/ViewModels/Base/`

---

### Task 3.2: MedicalCase ViewModel拆分

**优先级**: P0
**工时**: 5小时

**目标**: MedicalCaseWorkspaceViewModel < 400行

**策略**:
1. 处方逻辑 -> PrescriptionPanelViewModel
2. 诊断逻辑 -> ConsultationPanelViewModel
3. 状态机 -> MedicalCaseStateMachine
4. 协调器 -> MedicalCaseWorkspaceCoordinator

---

### Task 3.3: Patients ViewModel优化

**优先级**: P1
**工时**: 3小时

---

### Task 3.4: Formula ViewModel优化

**优先级**: P2
**工时**: 2小时

---

### Task 3.5: 组合模式标准化

**优先级**: P1
**工时**: 3小时

**模式**:
```csharp
public class XxxMasterDetailViewModel : ViewModelBase
{
    private readonly IMasterDetailServices<TList, TDetail> _services;
    
    // 组合而非继承
}
```

---

## Phase 4: 控件提取与复用

**目标**: 提取可复用控件，减少重复实现
**预估工时**: 10小时
**依赖**: Phase 3完成

### Task 4.1: PatientInfoCardControl [已完成]

**状态**: 已完成

**位置**: `Core/LYBT.Desktop.Infrastructure/Controls/`

---

### Task 4.2: PendingQueueControl [已完成]

**状态**: 已完成

**位置**: `Core/LYBT.Desktop.Infrastructure/Controls/`

---

### Task 4.3: PatientSearchControl [已完成]

**状态**: 已完成

**位置**: `Core/LYBT.Desktop.Infrastructure/Controls/`

---

### Task 4.4: MasterDetailContainer通用化

**优先级**: P1
**工时**: 4小时

**目标**: 创建通用的Master-Detail布局容器

---

### Task 4.5: 控件文档编写

**优先级**: P2
**工时**: 2小时

---

## Phase 5: 导航与Shell优化

**目标**: 优化导航系统和Shell架构
**预估工时**: 8小时
**依赖**: Phase 4完成

### Task 5.1: 路由系统审查

**优先级**: P1
**工时**: 2小时

---

### Task 5.2: 菜单权限集成

**优先级**: P2
**工时**: 3小时

---

### Task 5.3: 导航状态管理

**优先级**: P2
**工时**: 2小时

---

### Task 5.4: Shell布局优化

**优先级**: P3
**工时**: 1小时

---

## 进度统计

| Phase | 任务数 | 已完成 | 进行中 | 待执行 |
|-------|--------|--------|--------|--------|
| Phase 0 | 5 | 2 | 0 | 3 |
| Phase 1 | 5 | 0 | 0 | 5 |
| Phase 2 | 6 | 1 | 0 | 5 |
| Phase 3 | 5 | 0 | 0 | 5 |
| Phase 4 | 5 | 3 | 0 | 2 |
| Phase 5 | 4 | 0 | 0 | 4 |
| **总计** | **30** | **6** | **0** | **24** |

---

## Changelog

| 日期 | 变更 |
|------|------|
| 2025-12-30 | 初始任务分解 |
