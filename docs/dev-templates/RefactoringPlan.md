# 系统管理模块重构计划

## 一、重构目标
1. 统一所有管理模块的代码结构
2. 提取公共功能到基类，减少代码重复
3. 规范化界面设计和交互流程
4. 完善缺失的功能模块

## 二、重构步骤

### 第一阶段：基础设施构建（已完成）
- [x] 创建统一的模块开发模板
- [x] 创建基类 BaseManagementViewModel
- [ ] 创建基类 BaseDialogViewModel
- [ ] 创建标准化的视图组件

### 第二阶段：重构已有模块
按照新模板和基类重构现有模块：

#### 1. 患者管理（Patients）- 优先级：高
当前状态：只有列表功能
需要完成：
- [ ] 使用 BaseManagementViewModel 重构 PatientManagementViewModel
- [ ] 实现 AddPatientDialog 和 AddPatientDialogViewModel
- [ ] 实现 EditPatientDialog 和 EditPatientDialogViewModel
- [ ] 实现 ViewPatientDialog 和 ViewPatientDialogViewModel

#### 2. 医生管理（Doctors）- 优先级：高
当前状态：对话框使用占位实现
需要完成：
- [ ] 使用 BaseManagementViewModel 重构 DoctorManagementViewModel
- [ ] 完善 AddDoctorDialog 功能
- [ ] 实现 EditDoctorDialog 和 EditDoctorDialogViewModel
- [ ] 实现 ViewDoctorDialog 和 ViewDoctorDialogViewModel

#### 3. 病历管理（Records）- 优先级：高
当前状态：只有列表功能
需要完成：
- [ ] 使用 BaseManagementViewModel 重构 RecordManagementViewModel
- [ ] 实现 AddRecordDialog 和 AddRecordDialogViewModel
- [ ] 实现 EditRecordDialog 和 EditRecordDialogViewModel
- [ ] 实现 ViewRecordDialog 和 ViewRecordDialogViewModel

#### 4. 中药材管理（Herbs）- 优先级：中
当前状态：缺少查看功能
需要完成：
- [ ] 使用 BaseManagementViewModel 重构 HerbManagementViewModel
- [ ] 实现 ViewHerbDialog 和 ViewHerbDialogViewModel

#### 5. 用户管理（Users）- 优先级：低
当前状态：功能完整
需要完成：
- [ ] 评估是否需要使用 BaseManagementViewModel 重构
- [ ] 保持现有的合并新增/编辑对话框设计

#### 6. 验方模板管理（FormulaTemplates）- 优先级：低
当前状态：功能完整
需要完成：
- [ ] 评估是否需要使用 BaseManagementViewModel 重构
- [ ] 保持额外的导入导出功能

### 第三阶段：实现新模块

#### 1. 角色权限管理（Roles）- 优先级：高
- [ ] 创建 RoleManagementViewModel（基于BaseManagementViewModel）
- [ ] 实现角色列表、新增、编辑、删除功能
- [ ] 实现权限分配界面

#### 2. 处方管理（Prescriptions）- 优先级：高
- [ ] 创建 PrescriptionManagementViewModel
- [ ] 实现处方列表、新增、编辑、查看功能
- [ ] 实现处方打印功能

#### 3. 系统日志（Logs）- 优先级：中
- [ ] 创建 SystemLogsViewModel
- [ ] 实现日志查询、筛选、导出功能
- [ ] 只读模式，无需新增编辑

#### 4. 系统设置（Settings）- 优先级：中
- [ ] 创建 SystemSettingsViewModel
- [ ] 实现配置管理界面
- [ ] 实现配置保存和加载

#### 5. 数据备份（Backup）- 优先级：低
- [ ] 创建 BackupViewModel
- [ ] 实现备份、还原功能
- [ ] 实现备份计划管理

## 三、代码规范

### 命名规范
- 视图模型：{Module}ManagementViewModel
- 对话框视图模型：{Action}{Module}DialogViewModel
- 视图：{Module}ManagementView
- 对话框：{Action}{Module}Dialog
- 服务接口：I{Module}Service
- 模型：{Module}Info

### 文件组织
```
{Module}/
├── ViewModels/
│   ├── {Module}ManagementViewModel.cs
│   ├── Add{Module}DialogViewModel.cs
│   ├── Edit{Module}DialogViewModel.cs
│   └── View{Module}DialogViewModel.cs
├── Views/
│   ├── {Module}ManagementView.xaml
│   ├── {Module}ManagementView.xaml.cs
│   ├── Add{Module}Dialog.xaml
│   ├── Add{Module}Dialog.xaml.cs
│   ├── Edit{Module}Dialog.xaml
│   ├── Edit{Module}Dialog.xaml.cs
│   ├── View{Module}Dialog.xaml
│   └── View{Module}Dialog.xaml.cs
└── Converters/ (可选)
```

## 四、UI/UX 规范

### 颜色方案
- 主色：#2E86AB（蓝色）
- 成功：#28A745（绿色）
- 警告：#FFC107（黄色）
- 危险：#DC3545（红色）
- 信息：#17A2B8（青色）
- 次要：#6C757D（灰色）

### 布局规范
1. 标题栏高度：50px
2. 工具栏高度：52px
3. 按钮高度：32px
4. 表格行高：45px
5. 分页栏高度：50px

### 交互规范
1. 所有删除操作需要二次确认
2. 所有异步操作需要显示加载状态
3. 操作完成后需要给出反馈
4. 表单验证需要实时反馈

## 五、测试计划

### 单元测试
- [ ] 基类功能测试
- [ ] 服务层测试
- [ ] 视图模型测试

### 集成测试
- [ ] 模块间导航测试
- [ ] 数据流测试
- [ ] 权限控制测试

### UI测试
- [ ] 界面响应测试
- [ ] 数据绑定测试
- [ ] 异常处理测试

## 六、时间安排

- 第一阶段：2天（基础设施）
- 第二阶段：5天（重构现有模块）
- 第三阶段：7天（实现新模块）
- 测试阶段：3天
- 总计：17天