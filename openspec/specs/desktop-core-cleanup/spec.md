# desktop-core-cleanup Specification

## Purpose
TBD - created by archiving change optimize-desktop-core. Update Purpose after archive.
## Requirements
### Requirement: DCC-001 异常处理统一

Desktop层 SHALL 使用Shared.ExceptionHandling进行统一异常处理。

**变更内容**:
- 删除Infrastructure/Services/ErrorHandling/目录
- 删除Presentation/Notifications/UnifiedErrorHandlingService.cs
- 添加Desktop.Infrastructure对Shared.ExceptionHandling的引用

#### Scenario: 处理ViewModel中的异常
- **WHEN** ViewModel执行异步操作抛出异常
- **THEN** SHALL 使用IDesktopExceptionHandler处理
- **AND** SHALL 显示用户友好的错误消息

#### Scenario: 注册全局异常处理器
- **WHEN** 应用启动
- **THEN** Shell SHALL 注册IDesktopExceptionHandler
- **AND** SHALL 配置AppDomain.UnhandledException处理

---

### Requirement: DCC-002 Token管理统一

Desktop层 SHALL 使用Foundation.ITokenLifecycleService作为唯一Token管理接口。

**变更内容**:
- 删除Infrastructure/Interfaces/ITokenManager.cs
- 所有Token操作通过ITokenLifecycleService

#### Scenario: 检查Token有效性
- **WHEN** 需要验证Token状态
- **THEN** SHALL 调用ITokenLifecycleService.IsTokenValid
- **AND** SHALL NOT 使用ITokenManager

#### Scenario: 刷新Token
- **WHEN** Token即将过期
- **THEN** SHALL 调用ITokenLifecycleService.RefreshTokenAsync()
- **AND** SHALL 自动更新存储

---

### Requirement: DCC-003 会话管理职责分离

ISessionManager SHALL 仅负责内存会话状态，不包含Token管理。

**变更内容**:
- 删除ISessionManager中的CurrentToken/AccessToken/RefreshToken属性
- 删除IUserSessionManager接口（合并到ISessionManager）

#### Scenario: 设置当前用户
- **WHEN** 登录成功
- **THEN** IAuthenticationService完成API调用
- **AND** ITokenLifecycleService保存Token
- **AND** ISessionManager.SetCurrentUser设置用户信息

#### Scenario: 检查认证状态
- **WHEN** 需要检查用户是否已登录
- **THEN** SHALL 调用ISessionManager.IsAuthenticated
- **AND** 内部依赖ITokenLifecycleService.IsTokenValid

---

### Requirement: DCC-004 映射器统一

Desktop层 SHALL 使用SimpleMapper作为主要映射工具。

**变更内容**:
- 删除Models/Mapping/MappingService.cs
- 删除Models/Mapping/IMappingService.cs

#### Scenario: 简单DTO转换
- **WHEN** 需要转换DTO到Item模型
- **THEN** SHALL 使用SimpleMapper.Map<TSource, TTarget>()
- **AND** SHALL NOT 使用MappingService

#### Scenario: 复杂映射规则
- **WHEN** 需要复杂映射逻辑（条件、嵌套）
- **THEN** MAY 使用AutoMapper Profile
- **AND** SHALL 在Presentation层配置

---

### Requirement: DCC-005 ViewModel基类层次

ViewModel SHALL 遵循简化的两层继承结构。

**变更内容**:
- 简化ViewModelBase为~150行核心功能
- 创建ListViewModelBase<T>和DetailViewModelBase
- 移除HTTP状态码处理到独立服务

#### Scenario: 创建列表ViewModel
- **WHEN** 实现列表功能
- **THEN** SHALL 继承ListViewModelBase<T>
- **AND** SHALL 使用Items属性绑定数据

#### Scenario: 创建详情ViewModel
- **WHEN** 实现详情/编辑功能
- **THEN** SHALL 继承DetailViewModelBase
- **AND** SHALL 使用SaveCommand/CancelCommand

#### Scenario: 处理API异常
- **WHEN** API调用返回错误状态码
- **THEN** SHALL 使用ApiExceptionHandler处理
- **AND** ViewModelBase SHALL NOT 包含HTTP状态码逻辑

---

### Requirement: DCC-006 接口位置规范

接口 SHALL 定义在职责对应的项目中。

**变更内容**:
- IUserNotificationService从Infrastructure移至Presentation
- ILoginCoordinator从Infrastructure移至Foundation

#### Scenario: 定义UI相关接口
- **WHEN** 接口涉及UI呈现（通知、对话框）
- **THEN** SHALL 定义在Presentation项目

#### Scenario: 定义业务基础接口
- **WHEN** 接口涉及业务基础（认证、缓存）
- **THEN** SHALL 定义在Foundation项目

---

### Requirement: DCC-007 控件组织规范

控件 SHALL 按通用/业务分离组织。

**变更内容**:
- 创建Infrastructure/Controls/Common/目录
- 业务控件移至对应业务模块

#### Scenario: 使用通用控件
- **WHEN** 需要LoadingOverlay/SearchBox等通用控件
- **THEN** SHALL 从Infrastructure.Controls.Common引用

#### Scenario: 使用业务控件
- **WHEN** 需要PrescriptionEditor等业务控件
- **THEN** SHALL 从对应业务模块引用
- **AND** SHALL NOT 在Infrastructure定义业务控件

---

### Requirement: DCC-008 Item模型命名规范

模型 SHALL 遵循统一命名规范。

**命名规则**:
- `{Entity}Dto` = API传输对象（Shared层）
- `{Entity}Item` = UI列表项模型（Desktop.Models）
- `{Entity}ItemViewModel` = 带行为的列表项（业务模块）

#### Scenario: 定义列表项模型
- **WHEN** 需要在列表中显示实体
- **THEN** SHALL 创建{Entity}Item类
- **AND** SHALL 定义在Desktop.Models/Items/

#### Scenario: 列表项需要行为
- **WHEN** 列表项需要命令（编辑、删除）
- **THEN** SHALL 创建{Entity}ItemViewModel
- **AND** SHALL 继承ViewModelBase

---

