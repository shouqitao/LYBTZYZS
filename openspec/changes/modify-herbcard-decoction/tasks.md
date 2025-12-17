# Tasks: HerbCardControl UI优化与煎法字段添加

## Phase 1: 数据模型变更

- [x] 1.1 创建DecocteMethod枚举类型
  - 文件: `src/Shared/LYBT.Shared.Models/Enums/DecocteMethod.cs`
  - 值: Default(默认), PreDecoct(先煎), PostAdd(后下), MeltIn(烊化), TakeWithWater(冲服), WrapDecoct(包煎), SeparateDecoct(另煎)

- [x] 1.2 更新PrescriptionItem实体
  - 文件: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionItem.cs`
  - 添加DecocteMethod属性，默认值Default

- [x] 1.3 创建EF Core迁移
  - 文件: `src/Server/Core/LYBT.Infrastructure/Migrations/20251215085856_AddDecocteMethodColumn.cs`
  - 添加DecocteMethod列到PrescriptionItems表

## Phase 2: ViewModel层变更

- [x] 2.1 更新HerbItemViewModelBase
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/HerbItemViewModelBase.cs`
  - 添加DecocteMethod属性

- [x] 2.2 更新PrescriptionItemViewModel
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionItemViewModel.cs`
  - 添加DecocteMethod属性
  - 添加AvailableDecocteMethods静态列表

## Phase 3: UI层变更与Bug修复

- [x] 3.1 修改HerbCardControl.xaml
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Components/HerbCardControl.xaml`
  - 移除Unit显示（保留数据绑定）
  - 添加DecocteMethod ComboBox
  - **UI优化**: 移除删除按钮，改为右键菜单删除（节省空间+防误删）
  - 调整布局：药材名称 | 剂量 | 煎法（3列）

- [x] 3.2 更新HerbCardControl.xaml.cs
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Components/HerbCardControl.xaml.cs`
  - 添加OnContextMenuOpening事件处理（非编辑模式禁用右键菜单）
  - 添加OnDeleteMenuItemClick事件处理（带确认对话框，防止误删）

- [x] 3.3 **Bug修复**: 完整药材名称回车焦点跳转
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Components/HerbCardControl.xaml.cs`
  - 当输入完整正确药材名称（如"当归"）且建议框未打开时，回车应跳转到剂量输入框

- [x] 3.4 **Bug修复**: 无效药材名称校验
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Presentation/Components/HerbCardControl.xaml.cs`
  - 当输入的药材名称在药材库中不存在时，回车应提示"药材不存在"
  - 不允许提交无效的药材名称

## Phase 4: 打印功能适配

- [x] 4.1 更新PrescriptionPrintDto
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Models/PrescriptionPrintDto.cs`
  - PrescriptionItemPrintDto添加DecocteMethod属性
  - 添加DisplayText计算属性用于格式化输出

- [x] 4.2 更新打印模板显示逻辑
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionFlowDocumentBuilder.cs`
  - 药材项显示格式使用DisplayText: "药材名剂量单位(煎法)" - 仅非默认煎法显示括号标注

- [x] 4.3 更新PrescriptionDtos共享模型
  - 文件: `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDtos.cs`
  - PrescriptionItemDto添加DecocteMethod属性

- [x] 4.4 更新打印服务映射
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`
  - MapPrescriptionItems方法添加DecocteMethod映射

## Phase 5: 验证与测试

- [x] 5.1 编译验证
  - 确保所有项目编译通过（0警告0错误）

- [ ] 5.2 功能验证（手动测试）
  - HerbCardControl不显示单位
  - 煎法下拉可选择
  - 打印输出正确显示单位和煎法
  - 回车焦点跳转正确
  - 无效药材名称提示正确
