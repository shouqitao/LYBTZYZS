# Desktop 重构第二步 — MedicalCase 编辑外壳重新设计（执行计划）

> **日期**: 2026-08-10
> **状态**: 待执行（3 批次派发 omp）
> **权威文档**: `docs/03-architecture/16-desktop-architecture-spec.md` §4.6（已更新 2026-08-10）
> **基线报告**: `docs/compose/reports/desktop-layer-review-2026-08-10.md`
> **执行 agent**: omp（psmux attach 实时观看）
> **硬性门禁**: `dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告；架构测试全绿

---

## 背景

Desktop 层 2026-08-09 重构第一步（commit `6c159d8f6`）完成 Registration/Formula/命名空间/DP-M1/M2/M3 后，MedicalCase 域成为 DTO 直编辑唯一残留重灾区：

1. **编辑真源是 DTO**：`Services/MedicalCaseEditContext`（DTO 快照）被 CommandService/LifecycleService/Service 四件套注入，`HasChanges` 靠三个方法逐字段比较 DTO
2. **取消恢复 = Clone 深拷贝**：`MedicalCaseCloneMapper.Clone()` 复制 DTO，笨重且无编辑会话语义
3. **处方药材行直编辑 DTO**：`PrescriptionItemViewModel.Items = ObservableCollection<PrescriptionItemDto>`，XAML 双向绑定到共享控件 HerbListControl（DP 类型 `IList<PrescriptionItemDto>`）
4. **`Models/Items/MedicalCaseEditContext` 死代码**：已具备正确形状（InitializeFromModel/ApplyToModel/Clone）但 0 引用
5. **共享控件契约混乱**：HerbListControl DP 强类型 DTO，但 Formula 侧实际传 `FormulaHerbItemViewModel` 集合

**2026-08-10 产品负责人定案**：
- 编辑外壳**重新设计**（不做兼容层），重建真正的 Model + EditContext 体系
- **前后端各自定义实例原则（必选）**：Server/Shared 定义 DTO（仅传输），Desktop 定义 Model（UI 可编辑），禁止 UI 层直接编辑对方实例
- T6（卫生清理）单独安排，不在本计划内

---

## 目标态（spec §4.6 已固化）

```
API DTO（Shared）
    ↓ Mapper/Service 边界
MedicalCaseDetailModel（Desktop Model，PrescriptionItems = ObservableCollection<PrescriptionItemModel>）
    ↓ CreateEditContext()
MedicalCaseEditContext（编辑会话：诊断字段 + 处方行集合 + 状态，支持 BeginEdit/Commit/Cancel）
    ↓ 用户编辑 → Save
Mapper → InputDto → Repository → API
```

---

## 批次拆分（3 批，每批独立验证 + commit + push）

### B1: 共享控件契约解耦 + PrescriptionItemModel 新建 + 处方行绑定切换

**范围**：
- 新建 `PrescriptionItemModel : ObservableObject, IHerbItemEditable`（src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/）
  - 字段对齐 PrescriptionItemDto（Id/PrescriptionId/HerbId/HerbName/Unit/UnitPrice/Dosage/TotalPrice/TotalWeight/Subtotal/Usage/DecocteMethod/Role/Remark）
  - 实现 IHerbItemEditable（HerbId/HerbName/Unit/Dosage/UnitPrice + AllHerbs/FilteredHerbs/SelectedHerb）
  - 参考：`FormulaHerbItemModel`（Catalog/Models/Items/，7 字段）+ `HerbItemViewModelBase`（Infrastructure/ViewModels/Base/，IHerbItemEditable 实现模式）
- 共享控件契约解耦：`HerbListControl.HerbItems` DP 从 `IList<PrescriptionItemDto>` → `IEnumerable`（与 FormulaEditControl.HerbItems 同款宽松类型）
  - **先验证 Formula 侧实际绑定流**（FormulaMasterDetailControl.xaml:243 绑定 `FormulaEditor.EditHerbItems`（ObservableCollection<FormulaHerbItemViewModel>）→ FormulaEditControl.HerbItems（IEnumerable）→ 内部 HerbListControl）——确认 DP 类型变更不影响 Formula 侧
  - HerbListControl 内部 `OnHerbItemsChanged(IList<PrescriptionItemDto>?)` 改为 `OnHerbItemsChanged(IEnumerable?)`，`LoadFromDto/ToDto` 适配
- `PrescriptionItemViewModel.Items` → `ObservableCollection<PrescriptionItemModel>`（XAML 绑定链 MedicalCaseEditControl.xaml:472 `HerbItems="{Binding Prescription.Items}"` 不变）
- `MedicalCaseDetailModel.PrescriptionItems` → `ObservableCollection<PrescriptionItemModel>`（MedicalCaseDetailModelMapper 扩展 DTO↔Model 映射）
- 联动清理：所有 `ObservableCollection<PrescriptionItemDto>` 作为 UI 编辑类型的引用点（HistoryCopyDialog 等留 B3）

**不做**：EditContext 重建（B2）、对话框（B3）

**验收**：build 0/0 + 架构测试 + MedicalCase 相关单测；grep 确认 `PrescriptionItemViewModel.Items` 不再持 DTO

### B2: EditContext 重建 + 四件套改造 + CloneMapper 删除

**范围**：
- 重建 `Models/Items/MedicalCaseEditContext` 为完整编辑会话：
  - 诊断字段（PresentIllness/TongueDiagnosis/PulseDiagnosis/TcmDiagnosis/Remark）
  - 处方行集合（ObservableCollection<PrescriptionItemModel>）
  - 状态字段（Status）
  - `BeginEdit()`（从 Model 快照）/ `Commit()`（应用到 Model）/ `Cancel()`（恢复快照）/ `IsDirty`
  - 保留 CreateNew/InitializeFromModel/ApplyToModel/Clone 语义（已具备形状）
- 删除 `Services/MedicalCaseEditContext`（DTO 快照）+ `MedicalCaseCloneMapper`
- CommandService 改造：注入新 EditContext，`HasChanges => _context.IsDirty`，`SaveAsync` 从 EditContext → Mapper → InputDto → Repository
- LifecycleService 改造：`InitializeAsync` 加载 DTO → Mapper → Model → `_context.BeginEdit()`
- MedicalCaseService 聚合代理同步调整（Current/HasChanges 委托）

**不做**：对话框（B3）

**验收**：build 0/0 + 架构测试 + MedicalCase 相关单测（FSM/Workspace 行为等价）

### B3: 对话框治理 + DP-M1 测试扩展 + 文档校准

**范围**：
- HistoryCopyDialogViewModel：`MedicalCaseDetailDto` → `MedicalCaseDetailModel`（只读展示）+ `PrescriptionItemDto` → `PrescriptionItemModel`
- FormulaImportDialogViewModel：保持 DTO 只读（先例：Registration 患者/医生选择列表豁免 DP-M1）——仅确认不改，记录理由
- DP-M1 测试扩展：`ObservableCollection<T>` 泛型参数名以 Dto 结尾也判违规（当前只查直接声明 Dto 类型）
- 文档校准：
  - ADR-0010 引用清单 8→6 模块（Auth+Users→Identity、Herbs+Formulas→Catalog 合并后未同步）
  - LocalWebAPI README：端口 5290、6 模块、11 控制器（非「12」）、CatalogController（非 HerbsController+FormulasController）
  - 13c-current-status.md 已知问题表同步

**验收**：build 0/0 + 架构测试（含新 DP-M1 用例）+ 文档与代码一致

---

## 风险与注意

1. **共享控件 DP 改动影响 Formula 侧**——B1 第一步先验证 Formula 实际绑定流（FormulaMasterDetailControl → FormulaEditControl → HerbListControl 链路），确认 `IEnumerable` 化不影响 Formula 编辑
2. **编辑链路复杂**（FSM + 状态流转 + Workspace VM 595 行）——每批独立 build 0/0 验证；B2 强调行为等价（加载/保存/取消/挂起/完成全流程）
3. **Mapperly 生成代码**——PrescriptionMapper 是静态实例（PrescriptionItemViewModel 有 TODO 注释），扩展 DTO↔Model 映射时注意 Mapperly 属性名匹配
4. **死代码删除顺序**：B2 删除 `Services/MedicalCaseEditContext` 前，先 grep 确认所有引用点已迁移（MedicalCaseModule.cs 注册 + Command/Lifecycle/Service 注入）
5. **禁止顺手改**：每批只做本批任务，不"顺手"清理相邻代码（外科手术式修改）

---

## 派发清单

| 批次 | omp 任务名 | 依赖 |
|------|-----------|------|
| B1 | T-MC-B1-共享控件契约+处方行Model化 | 无 |
| B2 | T-MC-B2-EditContext重建+四件套改造 | B1 |
| B3 | T-MC-B3-对话框+DP-M1+文档校准 | B1+B2 |

每批完成 → 独立验证（build --no-incremental + 架构测试 + 相关单测）→ commit（英文，`refactor(desktop): ...`）→ push → 汇报，用户确认后再派下一批。
