# LYBT.Desktop.Admin

> 管理员角色模块 | 6功能导航工作台 | Prism模块化

## 项目定位

- **层级**: Desktop Roles
- **职责**: 提供管理员角色专属工作台主页,集成用户管理、中药管理、患者管理、方剂管理、病案管理、系统设置6个功能模块的快速导航,支持基于角色的权限控制
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Admin/
├── AdminModule.cs           # Prism模块注册
├── ViewModels/
│   └── AdminHomeViewModel.cs   # 管理员主页ViewModel(6个导航命令)
└── Views/
    ├── AdminHomeView.xaml       # 管理员主页视图
    └── AdminHomeView.xaml.cs    # 视图后置代码
```

## 核心组件

| 名称 | 说明 |
|------|------|
| AdminModule | Prism模块注册,自动发现Views和ViewModels |
| AdminHomeViewModel | 6个导航命令 + INavigationAware实现 + 权限检查 |
| AdminHomeView | 管理员工作台主页UI,包含6个功能卡片 |

## 设计依据

管理员工作台采用"导航枢纽"模式:单一主页通过6个DelegateCommand导航到各功能模块,每个命令绑定权限检查(SessionManager.HasPermission)。这种设计将角色入口与功能模块解耦,新增管理功能只需添加导航命令,无需修改现有模块。

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (Desktop端基础类型和接口)
- LYBT.Desktop.Infrastructure (区域管理、导航服务)
- LYBT.Desktop.Models (ViewModelBase基类)
- LYBT.Desktop.Contracts (区域名称常量)
- LYBT.Shared.Models (共享DTO模型)

### 被依赖
- LYBT.Desktop.Shell (Prism模块注册,加载管理员模块)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 按精简规范重写README,代码示例迁移至CLAUDE.md |
| 2025-10-29 | 初始版本 |
