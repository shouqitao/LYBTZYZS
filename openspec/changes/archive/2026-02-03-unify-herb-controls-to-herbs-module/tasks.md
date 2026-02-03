# 任务清单: unify-herb-controls-to-herbs-module

## Phase 1: 迁移MedicalCaseEditControl

### 1.1 添加模块依赖
- [x] 在 `LYBT.Desktop.MedicalCase.csproj` 中添加对 `LYBT.Desktop.Herbs` 的引用

### 1.2 更新XAML
- [x] `MedicalCaseEditControl.xaml`: 添加 xmlns:herbList 引用
- [x] `MedicalCaseEditControl.xaml`: 替换 `sharedControls:HerbListEditor` 为 `herbList:HerbListControl`
- [x] `MedicalCaseEditControl.xaml`: 更新属性绑定 (添加AllHerbs, 移除命令绑定)

### 1.3 更新代码后置
- [x] `MedicalCaseEditControl.xaml.cs`: 添加 `AllHerbs` 依赖属性
- [x] `MedicalCaseEditControl.xaml.cs`: 移除 `DeleteHerbCommand` 属性
- [x] `MedicalCaseEditControl.xaml.cs`: 移除 `DosageCompletedCommand` 属性
- [x] `MedicalCaseEditControl.xaml.cs`: 移除 `AddNewRowCommand` 属性

### 1.4 更新调用方
- [x] `MedicalCaseMasterDetailControl.xaml`: 更新绑定，添加AllHerbs，移除命令绑定

### 1.5 验证
- [x] 编译验证通过

## Phase 2: 迁移FormulaEditControl

### 2.1 添加模块依赖
- [x] 在 `LYBT.Desktop.Formula.csproj` 中添加对 `LYBT.Desktop.Herbs` 的引用

### 2.2 更新XAML
- [x] `FormulaEditControl.xaml`: 添加 xmlns:herbList 引用
- [x] `FormulaEditControl.xaml`: 替换 `sharedControls:HerbListEditor` 为 `herbList:HerbListControl`
- [x] `FormulaEditControl.xaml`: 更新属性绑定 (添加AllHerbs, 移除命令绑定)

### 2.3 更新代码后置
- [x] `FormulaEditControl.xaml.cs`: 添加 `AllHerbs` 依赖属性
- [x] `FormulaEditControl.xaml.cs`: 移除 `DeleteHerbCommand` 属性
- [x] `FormulaEditControl.xaml.cs`: 移除 `DosageCompletedCommand` 属性
- [x] `FormulaEditControl.xaml.cs`: 移除 `AddNewRowCommand` 属性

### 2.4 更新调用方
- [x] `FormulaMasterDetailControl.xaml`: 更新绑定，添加AllHerbs，移除命令绑定

### 2.5 验证
- [x] 编译验证通过

## Phase 3: 删除老控件

### 3.1 删除Infrastructure中的老控件
- [x] 删除 `Infrastructure/Controls/HerbListEditor.xaml`
- [x] 删除 `Infrastructure/Controls/HerbListEditor.xaml.cs`
- [x] 删除 `Infrastructure/Controls/HerbCardControl.xaml`
- [x] 删除 `Infrastructure/Controls/HerbCardControl.xaml.cs`
- [x] 删除 `Infrastructure/Controls/HerbListView.xaml`
- [x] 删除 `Infrastructure/Controls/HerbListView.xaml.cs`
- [x] 删除 `Infrastructure/Controls/HerbItem/*`
- [x] 删除 `Infrastructure/Controls/HerbList/*`
- [x] 删除 `Infrastructure/Models/HerbItemDto.cs`

### 3.2 验证
- [x] 编译验证通过 (0错误0警告)
- [x] 无遗留引用

## Phase 4: 最终验证

### 4.1 全量验证
- [x] 全量编译通过 (`dotnet build LYBT.All.sln`)
- [ ] 运行时测试：医案管理中药材编辑功能
- [ ] 运行时测试：经验方管理中药材编辑功能
