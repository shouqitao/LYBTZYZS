# LYBT.Desktop.Users

> 用户管理模块 | 用户 CRUD / 角色分配 / 密码重置 / 批量导入导出

## 项目定位

- **层级**: Client Modules 层
- **职责**: 提供系统用户管理界面，支持用户 CRUD、角色分配、密码重置、状态切换、批量导入导出。采用 Handler 组件拆分职责

## 目录结构

```
LYBT.Desktop.Users/
├── CommandHandlers/
│   ├── IUserCommandHandler.cs              # CommandHandler 接口
│   └── UserCommandHandler.cs               # CommandHandler 实现
├── Controls/
│   ├── UserEditControl.xaml/.xaml.cs        # 用户编辑控件 (双向绑定+验证)
│   ├── UserMasterDetailControl.xaml/.xaml.cs # Master-Detail 可复用控件
│   └── UserViewControl.xaml/.xaml.cs        # 用户只读预览控件
├── Interfaces/
│   ├── IUserRepository.cs                  # 用户仓储接口 (CRUD+搜索+密码+状态+批量)
│   └── IUserService.cs                     # 用户 Service 接口
├── Mappers/
│   └── UserMapper.cs                       # Mapperly 编译时映射器
├── Models/
│   ├── Items/
│   │   └── UserItem.cs                     # 列表项 UI 模型 (BindableBase)
│   └── UserDetailModel.cs                  # Detail 编辑模型 (ValidatableModelBase)
├── Repositories/
│   └── UserRepository.cs                   # 仓储实现 (DataSource + 可选 IUserApi)
├── ViewModels/
│   ├── Components/
│   │   └── UserService.cs                  # 用户命令操作 (CRUD+查询+状态+密码)
│   ├── Handlers/
│   │   ├── IUserImportExportHandler.cs     # 导入导出接口
│   │   ├── IUserPasswordHandler.cs         # 密码处理接口
│   │   ├── IUserStatusHandler.cs           # 状态处理接口
│   │   ├── UserImportExportHandler.cs      # Excel 导入/导出/模板下载
│   │   ├── UserPasswordHandler.cs          # 重置密码 (确认对话框)
│   │   └── UserStatusHandler.cs            # 切换状态/恢复用户
│   └── UserMasterDetailViewModel.cs        # 核心 ViewModel (组合模式)
└── UsersModule.cs                           # Prism 模块注册
```

## 核心接口

| 接口 | 职责 |
|------|------|
| IUserRepository | 用户仓储 (CRUD + 搜索 + 密码 + 状态 + 批量操作) |
| IUserPasswordHandler | 密码重置 (确认 + 调用 UserService) |
| IUserStatusHandler | 状态切换 / 恢复已删除用户 |
| IUserImportExportHandler | Excel 导入 / 导出 / 模板下载 |

## 关键功能

| 功能 | 实现 |
|------|------|
| Master-Detail 管理 | UserMasterDetailViewModel + MasterDetailControlBase |
| 密码重置 | UserPasswordHandler (管理员执行) |
| 状态管理 | UserStatusHandler (启用/禁用/恢复) |
| 批量操作 | UserImportExportHandler (Excel 导入导出) |
| 角色筛选 | SelectedRoleFilter / SelectedStatusFilter |

## 角色权限

| 角色 | 权限 |
|------|------|
| Admin | 用户管理全部权限 |
| Doctor | 无访问权限 |

## 设计依据

- Handler 组件拆分 (Password/Status/ImportExport) 实现 SRP，避免 ViewModel 职责膨胀
- MasterDetailControl 复用模式供 Admin 角色台 UserManagementView 嵌入使用
- Repository 通过 IUserDataSource 支持 Local/Remote，IUserApi 可选注入 (仅 Remote 高级功能)
- Mapperly 编译时映射替代 AutoMapper，零运行时开销

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (BaseApiRepository/Security)
- LYBT.Desktop.Infrastructure (MasterDetailControlBase/ViewModelBase/Services)
- LYBT.Desktop.Models (ValidatableModelBase)
- LYBT.Desktop.Contracts (IUserApi/IUserDataSource)
- LYBT.Desktop.Utilities (ExcelHelper)
- LYBT.Shared.Models (UserListDto/UserDetailDto/UserInputDto)
- Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Admin (UserManagementView 嵌入 UserMasterDetailControl)
- LYBT.Desktop.Shell (AccountSettingsViewModel 使用 IUserRepository)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 目录结构和接口表更新 |
| 2025-12-04 | 按 README 规范重写文档 |
| 2025-10-29 | 初始版本 |
