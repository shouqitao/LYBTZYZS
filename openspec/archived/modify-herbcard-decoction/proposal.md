# Change: HerbCardControl UI优化与煎法字段添加

## Why

当前HerbCardControl存在以下问题：
1. UI上显示的"单位"字段对用户操作无实际意义，因为单位是从药材库自动获取的固定值
2. 缺少煎法标注功能，而中医处方中常需标注先煎、后下等特殊煎法
3. **Bug**: 输入完整正确的药材名称后按回车，焦点不移动
4. **Bug**: 输入不存在的药材名称（如只输入"当"）后按回车，系统接受了无效输入

## What Changes

1. **UI隐藏单位显示**
   - HerbCardControl中移除单位的可视化显示
   - 后台数据模型保留Unit字段（打印时仍需显示）
   - 选择药材时自动从药材库同步Unit值

2. **新增煎法字段**
   - 在PrescriptionItem实体添加`DecocteMethod`字段
   - 可选值：默认、先煎、后下、烊化、冲服、包煎、另煎
   - HerbCardControl UI添加煎法下拉选择器
   - 回车跳转逻辑：药材名称 → 剂量 → 下一行药材（跳过煎法）
   - 打印处方时显示非默认的煎法标注

3. **修复回车键焦点跳转Bug**
   - 输入完整正确药材名称后，回车应跳转到剂量输入框
   - 输入不存在的药材名称后，回车应提示"药材不存在"而非接受无效输入

## Impact

- **Affected specs**: prescription
- **Affected code**:
  - `HerbCardControl.xaml` - UI布局调整
  - `HerbCardControl.xaml.cs` - 事件处理
  - `PrescriptionItem.cs` - 添加DecocteMethod字段
  - `PrescriptionItemViewModel.cs` - 添加DecocteMethod属性
  - `HerbItemViewModelBase.cs` - 添加DecocteMethod属性
  - `PrescriptionPrintTemplate.xaml` - 煎法显示
  - `PrescriptionPrintDto.cs` - 打印数据传输
  - EF Core迁移 - 数据库Schema变更
