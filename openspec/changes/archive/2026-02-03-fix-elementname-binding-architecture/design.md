# fix-elementname-binding-architecture 设计文档

## 概述

基于 [proposal.md](./proposal.md) 的详细技术设计。

### 问题根因

**WPF NameScope 隔离机制**：`ContentPresenter` 会创建独立的 NameScope，导致其内部的 `ElementName` 绑定无法解析父级控件。

**失败场景**：
```xml
<UserControl x:Name="Root">  <!-- NameScope #1 -->
  <MasterDetailLayout>
    <MasterDetailLayout.DetailContent>
      <!-- ContentPresenter 创建 NameScope #2 -->
      <SomeControl Prop="{Binding X, ElementName=Root}"/>  <!-- 失败! Root 在 NameScope #1 中 -->
    </MasterDetailLayout.DetailContent>
  </MasterDetailLayout>
</UserControl>
```

## 架构决策

### ADR-1: 采用 ViewModel 绑定模式替代 ElementName 绑定

**状态**: 已采纳

**背景**:
- PatientSelectionControl 使用 31 个 `ElementName=Root` 绑定在 ContentPresenter 内部
- 这些绑定在运行时全部失败 (System.Windows.Data Error: 40)
- 同类控件 PatientMasterDetailControl 使用正确的 ViewModel 绑定模式

**决策**:
将 PatientSelectionControl 重构为直接绑定 DataContext (ViewModel)，移除所有 ElementName 绑定。

**具体方案**:

| 问题模式 | 正确模式 |
|----------|----------|
| `{Binding PatientDetail.Name, ElementName=Root}` | `{Binding PatientDetail.Name}` |
| `{Binding CreateNewCommand, ElementName=Root}` | `{Binding CreateNewCommand}` |

**后果**:
- 正面: 绑定正常工作，与其他 MasterDetail 控件一致
- 正面: 简化代码，移除 DependencyProperty 中间层
- 负面: PatientSelectionControl 不再是独立的可复用控件，依赖外部 DataContext

### ADR-2: DataContext 透传模式

**状态**: 已采纳

**背景**:
PatientSelectionControl 通过 DependencyProperty 接收数据，然后内部用 ElementName 绑定。这增加了不必要的间接层。

**决策**:
移除 PatientSelectionControl 的 DependencyProperty，让控件直接依赖继承的 DataContext。

**实现细节**:

1. **PatientSelectionView.xaml** (宿主视图):
   - DataContext 通过 Prism 自动注入 `PatientSelectionViewModel`
   - PatientSelectionControl 继承此 DataContext

2. **PatientSelectionControl.xaml** (内部):
   - 直接使用 `{Binding PropertyName}` 绑定到 ViewModel 属性
   - 无需 `ElementName=Root`

**属性映射**:

| 原 DependencyProperty | ViewModel 属性 |
|----------------------|----------------|
| Patients | Patients |
| SelectedPatient | SelectedPatient |
| PatientDetail | PatientDetail |
| SearchText | SearchKeyword |
| CreateNewCommand | NewPatientCommand |
| RefreshCommand | RefreshCommand |
| SearchCommand | SearchCommand |
| SelectCommand | StartMedicalCaseCommand |
| IsLoading | IsBusy |
| HasSelection | HasSelection |

### ADR-3: 保留 PatientSelectionControl 为独立 UserControl

**状态**: 已采纳

**背景**:
考虑过将 PatientSelectionControl 内联到 PatientSelectionView，但这会降低代码组织性。

**决策**:
- 保留 PatientSelectionControl 作为独立 UserControl
- 但移除 DependencyProperty，依赖 DataContext 透传
- 控件文档注释说明预期的 DataContext 类型

**后果**:
- 正面: 保持代码组织清晰
- 正面: 可以在其他地方复用（如果 DataContext 兼容）
- 负面: 控件不再完全独立，需要特定 ViewModel 类型

## 实现策略

### 策略选择

选择**渐进式重构**而非一次性替换，原因：
1. 变更集中在单个文件，影响范围可控
2. 每步可独立验证
3. 如有问题可快速回滚

### 关键实现点

1. **移除 DependencyProperty**
   - 删除 PatientSelectionControl.xaml.cs 中的所有 DependencyProperty
   - 保留 PatientDoubleClicked 事件（仍需要）

2. **更新 XAML 绑定**
   - 移除所有 `ElementName=Root`
   - 调整属性路径匹配 ViewModel 属性名

3. **更新宿主视图**
   - PatientSelectionView.xaml 移除显式属性赋值
   - 控件直接继承 DataContext

## 变更清单

### 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml` | 移除 31 个 ElementName 绑定，转为 DataContext 绑定 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml.cs` | 移除 10 个 DependencyProperty，保留事件 |
| `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml` | 移除控件属性赋值，保留 DataContext 透传 |

### 无需修改的文件

| 文件路径 | 原因 |
|----------|------|
| `PatientMasterDetailControl.xaml` | 已使用正确模式 |
| `MasterDetailLayout.xaml` | 架构控件，不变 |
| `PatientViewControl.xaml` | 内部使用 DataContext 包装，正确 |
| 其他 18 个 ElementName 文件 | 绑定在 UserControl 内部，不跨 ContentPresenter |

## 依赖关系

### 变更顺序

```
PatientSelectionControl.xaml.cs (移除 DependencyProperty)
    ↓
PatientSelectionControl.xaml (更新绑定)
    ↓
PatientSelectionView.xaml (简化控件使用)
    ↓
编译验证
```

三个文件变更存在依赖，必须一起修改：
1. 如果先改 XAML 绑定，会因为 DependencyProperty 不存在而报错
2. 如果先改 View，会因为控件属性不存在而报错
3. 三者需要同时修改，作为一个原子变更

## 测试策略

### 手动功能测试

1. **启动应用** → 登录 Doctor 角色
2. **导航到患者选择** → 验证页面正常加载
3. **选择患者** → 验证右侧详情正确显示（**核心验证**）
4. **搜索患者** → 验证搜索功能正常
5. **新建患者** → 验证按钮响应
6. **双击患者** → 验证跳转到诊疗

### 编译验证

```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

## 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| ViewModel 属性名不匹配 | 中 | 高 | 已完成属性映射分析，有明确对照表 |
| 遗漏绑定转换 | 低 | 中 | Grep 搜索确认所有 ElementName 已移除 |
| 编译错误 | 低 | 低 | 渐进式修改，每步验证 |
| 运行时绑定错误 | 中 | 中 | 完整功能测试覆盖 |

## 回滚计划

如果变更失败:
1. `git checkout -- src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/`
2. `git checkout -- src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml`
3. 编译验证回滚成功

## 参考实现

**正确模式参考**: `PatientMasterDetailControl.xaml:244-268`

```xml
<!-- 正确: 直接绑定 ViewModel -->
<patientControls:PatientViewControl
    PatientName="{Binding CurrentDetail.Name}"
    PinYinCode="{Binding CurrentDetail.PinYinCode}"
    Gender="{Binding CurrentDetail.Gender}"
    ...
```

**问题模式示例**: `PatientSelectionControl.xaml:77-99` (修复前)

```xml
<!-- 错误: ElementName 在 ContentPresenter 内部失效 -->
<local:PatientViewControl
    PatientName="{Binding PatientDetail.Name, ElementName=Root}"
    PinYinCode="{Binding PatientDetail.PinYinCode, ElementName=Root}"
    ...
```

---

**设计者**: Claude Code
**日期**: 2026-01-11
**状态**: 待审批
