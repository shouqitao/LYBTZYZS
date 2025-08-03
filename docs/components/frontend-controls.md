# 凌隐宝堂中医诊所诊疗系统 - 控件使用文档

## 概述

本文档介绍了系统中自定义控件的使用方法和最佳实践。

## 控件目录

### 1. 列表项控件

#### UserListItemControl（用户列表项控件）
- **位置**: `Core/Controls/Users/UserListItemControl.xaml`
- **用途**: 显示用户列表中的单个用户信息
- **数据类型**: `UserDto`
- **主要功能**:
  - 显示用户头像、姓名、角色、状态等信息
  - 支持编辑和启用/禁用操作
  - 根据性别自动设置头像背景色

**使用示例**:
```xml
<controls:UserListItemControl Data="{Binding UserData}"/>
```

#### HerbListItemControl（草药列表项控件）
- **位置**: `Core/Controls/Herbs/HerbListItemControl.xaml`
- **用途**: 显示草药列表中的单个草药信息
- **数据类型**: `HerbDto`
- **主要功能**:
  - 显示草药名称、产地、规格、价格、库存等信息
  - 库存状态自动识别（正常、库存不足、缺货）
  - 支持编辑和启用/禁用操作

**使用示例**:
```xml
<controls:HerbListItemControl Data="{Binding HerbData}"/>
```

#### PatientListItemControl（患者列表项控件）
- **位置**: `Core/Controls/Patients/PatientListItemControl.xaml`
- **用途**: 显示患者列表中的单个患者信息
- **数据类型**: `PatientDto`
- **主要功能**:
  - 显示患者姓名、性别、年龄、联系方式等信息
  - 过敏史特别标注显示
  - 支持查看详情、编辑和挂号操作

**使用示例**:
```xml
<controls:PatientListItemControl Data="{Binding PatientData}"/>
```

#### FormulaTemplateListItemControl（验方模板列表项控件）
- **位置**: `Core/Controls/FormulaTemplates/FormulaTemplateListItemControl.xaml`
- **用途**: 显示验方模板列表中的单个模板信息
- **数据类型**: `FormulaTemplateDto`
- **主要功能**:
  - 显示模板名称
  - 支持查看详情、编辑、应用和删除操作

**使用示例**:
```xml
<controls:FormulaTemplateListItemControl Data="{Binding TemplateData}"/>
```

#### DoctorListItemControl（医生列表项控件）
- **位置**: `Core/Controls/Doctors/DoctorListItemControl.xaml`
- **用途**: 显示医生列表中的单个医生信息
- **数据类型**: `DoctorDto`
- **主要功能**:
  - 显示医生姓名、性别、职称、专长等信息
  - 工作状态显示（门诊、急诊、会议、请假、下班）
  - 支持查看详情、编辑和排班操作

**使用示例**:
```xml
<controls:DoctorListItemControl Data="{Binding DoctorData}"/>
```

### 2. 认证控件

#### LoginStatusControl（登录状态控件）
- **位置**: `Core/Controls/Auth/LoginStatusControl.xaml`
- **用途**: 显示当前登录用户状态
- **数据类型**: `LoginResponseDto`
- **主要功能**:
  - 显示用户头像和名称
  - 显示用户角色
  - 提供退出登录按钮

**使用示例**:
```xml
<controls:LoginStatusControl User="{Binding CurrentUser}"/>
```

## 值转换器

系统提供了多个值转换器用于数据展示：

### 基础转换器
- **BooleanToVisibilityConverter**: 布尔值转可见性（WPF内置）
- **InvertBooleanConverter**: 反转布尔值
- **StringToVisibilityConverter**: 字符串空值判断转可见性

### 数据格式化转换器
- **FirstCharacterConverter**: 提取字符串首字符（用于头像显示）
- **GenderToTextConverter**: 性别枚举转文本（Male→男，Female→女）
- **IDNumberMaskConverter**: 身份证号码脱敏显示
- **StockStatusConverter**: 库存数量转状态（正常/库存不足/缺货）

### UI相关转换器
- **BooleanToBackgroundConverter**: 布尔值转背景色
- **ItemIndexConverter**: 获取列表项索引

## 动画效果

系统提供了多种动画效果附加属性：

### 使用方式
```xml
xmlns:behaviors="clr-namespace:LYBT.WPF.Client.Core.Behaviors"

<!-- 淡入效果 -->
<Control behaviors:AnimationBehaviors.EnableFadeIn="True"/>

<!-- 滑入效果 -->
<Control behaviors:AnimationBehaviors.EnableSlideIn="True"/>

<!-- 鼠标悬停缩放 -->
<Control behaviors:AnimationBehaviors.EnableHoverScale="True"/>

<!-- 点击波纹效果 -->
<Button behaviors:AnimationBehaviors.EnableRippleEffect="True"/>

<!-- 延迟加载动画 -->
<Control behaviors:AnimationBehaviors.EnableStaggeredLoad="True"
         behaviors:AnimationBehaviors.LoadDelay="100"/>
```

## 基础视图模板

### BaseListView（通用列表页面）
- **位置**: `Core/Views/Base/BaseListView.xaml`
- **用途**: 提供统一的列表页面布局
- **功能**:
  - 页面标题
  - 搜索框
  - 筛选条件区域
  - 列表内容区域
  - 分页控件
  - 加载动画
  - 空数据提示

### BaseListViewModel（列表视图模型基类）
- **位置**: `Core/ViewModels/Base/BaseListViewModel.cs`
- **用途**: 提供列表页面的通用功能
- **功能**:
  - 数据加载
  - 搜索功能
  - 分页管理
  - 批量操作
  - 错误处理

## 最佳实践

### 1. 控件使用
- 始终通过Data属性传递数据
- 使用数据绑定而非直接赋值
- 命令通过RelativeSource绑定到父容器的DataContext

### 2. 列表实现
- 继承BaseListViewModel实现具体的列表功能
- 重写GetDataAsync方法获取数据
- 重写ExecuteAddAsync方法处理新增
- 使用ObservableCollection管理列表数据

### 3. 动画使用
- 适度使用动画，避免过度
- 列表项使用延迟加载创建流畅的进入效果
- 交互元素使用悬停和点击反馈

### 4. 性能优化
- 使用虚拟化技术处理大量数据
- 避免在列表项中使用复杂的数据绑定
- 合理使用异步加载

## 示例项目

控件使用示例可以在以下位置查看：
- **模块**: Examples
- **视图**: ControlExamplesView
- **路径**: `Modules/Examples/Controls/Views/ControlExamplesView.xaml`

在主界面点击"控件示例"按钮即可查看所有控件的实际效果。