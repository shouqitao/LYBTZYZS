# Delta: Desktop ViewModels

## MODIFIED Requirements

### Requirement: MasterDetail编辑模式

MasterDetail ViewModel基类 SHALL 提供统一的编辑模式管理，包括：
- 新建项时正确初始化CurrentDetail
- 编辑现有项时加载详情到CurrentDetail
- 取消编辑时恢复原始数据
- 保存时直接使用CurrentDetail数据

实现约束：
- 子类 SHALL NOT 定义独立的Edit属性（如EditName, EditUserName）
- 子类 SHALL 直接绑定CurrentDetail属性进行编辑
- XAML绑定 SHALL 使用`CurrentDetail.PropertyName`模式

#### Scenario: 新建项正确初始化

- **GIVEN** 用户在MasterDetail页面
- **WHEN** 用户点击新建按钮
- **THEN** CurrentDetail被初始化为新建对象
- **AND** IsEditMode设置为true
- **AND** SelectedItem设置为null但不影响CurrentDetail

#### Scenario: 编辑现有项

- **GIVEN** 用户选中列表中的某一项
- **WHEN** 用户点击编辑按钮
- **THEN** 系统保存CurrentDetail副本到_originalDetail
- **AND** IsEditMode设置为true
- **AND** 用户可以直接修改CurrentDetail属性

#### Scenario: 取消编辑恢复数据

- **GIVEN** 用户正在编辑模式
- **WHEN** 用户点击取消按钮
- **THEN** CurrentDetail恢复为_originalDetail的值
- **AND** IsEditMode设置为false

#### Scenario: 保存使用CurrentDetail数据

- **GIVEN** 用户正在编辑模式且已修改数据
- **WHEN** 用户点击保存按钮
- **THEN** 系统使用CurrentDetail构建DTO
- **AND** 调用后端API保存数据
- **AND** 保存成功后IsEditMode设置为false

### Requirement: 统一绑定模式

所有MasterDetail模块的XAML视图 SHALL 使用统一的绑定模式：

```xaml
<!-- 统一模式 -->
<TextBox Text="{Binding CurrentDetail.PropertyName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

<!-- 禁止模式 -->
<TextBox Text="{Binding EditPropertyName, ...}" />
```

#### Scenario: Users模块绑定

- **GIVEN** UserMasterDetailView.xaml
- **WHEN** 渲染用户编辑表单
- **THEN** 所有输入控件绑定到CurrentDetail.UserName、CurrentDetail.RealName等属性

#### Scenario: Patients模块绑定

- **GIVEN** PatientMasterDetailView.xaml
- **WHEN** 渲染患者编辑表单
- **THEN** 所有输入控件绑定到CurrentDetail.Name、CurrentDetail.Gender等属性

#### Scenario: Herbs模块绑定

- **GIVEN** HerbMasterDetailView.xaml
- **WHEN** 渲染药材编辑表单
- **THEN** 所有输入控件绑定到CurrentDetail.Name、CurrentDetail.Price等属性

## REMOVED Requirements

### Requirement: Edit属性模式

**Reason**: 造成代码冗余，每模块200-300行重复代码
**Migration**: 移除所有EditXxx属性，改用CurrentDetail直接绑定

以下模式不再使用：
- ViewModel中的EditPropertyName属性定义
- ClearEditProperties()方法
- 编辑时从CurrentDetail复制到Edit属性
- 保存时从Edit属性构建DTO
