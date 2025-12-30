# Tasks: optimize-patient-selection-ui

## Phase 1: PatientViewControl布局重构

- [x] 1.1 将PatientViewControl从StackPanel改为2x3 Grid布局
- [x] 1.2 移除PatientViewControl内部的ScrollViewer
- [x] 1.3 InfoCard按两列分布：左列(基本、健康、紧急联系人) 右列(身份、联系、就诊统计)
- [x] 1.4 调整InfoCard间距确保紧凑布局

## Phase 2: PatientSelectionView Detail区域重构

- [x] 2.1 移除Detail区域的ScrollViewer包装
- [x] 2.2 添加IsEditMode属性支持
- [x] 2.3 添加PatientEditControl用于编辑模式
- [x] 2.4 更新DetailToolbar替换自定义Header（复用DetailToolbar）
- [x] 2.5 添加View/Edit模式切换逻辑

## Phase 3: ViewModel重构

- [x] 3.1 PatientSelectionViewModel添加IsEditMode属性
- [x] 3.2 添加CurrentDetail属性（可编辑的患者数据副本）
- [x] 3.3 NewPatientCommand改为进入编辑模式（ExecuteNewPatient）
- [x] 3.4 添加SaveCommand/CancelCommand
- [x] 3.5 添加EditCommand（从查看切换到编辑）

## Phase 4: 清理与验证

- [x] 4.1 移除QuickCreatePatientDialog相关代码（3个文件已删除）
- [x] 4.2 更新PatientsModule注册（移除Dialog注册）
- [x] 4.3 编译验证（0警告0错误）
- [x] 4.4 运行测试（10/10通过）

## Phase 5: UI样式统一（补充）

- [x] 5.1 创建FormComboBoxStyle统一下拉框样式
- [x] 5.2 创建FormDatePickerStyle统一日期选择器样式
- [x] 5.3 PatientEditControl使用统一表单样式
