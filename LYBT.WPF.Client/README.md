# 凌隐宝堂中医诊所WPF客户端

## 项目说明

- **项目名称**: 凌隐宝堂中医诊所管理系统WPF客户端
- **项目简称**: LYBT.WPF.Client
- **目标框架**: .NET 8.0 Windows
- **UI框架**: WPF + Prism 8.1+
- **架构模式**: MVVM + 模块化

## 项目结构

### 核心项目

- **LYBT.WPF.Client.Shell**: 主壳程序，负责应用程序启动和模块加载
- **LYBT.WPF.Client.Core**: 核心基础设施，包含模型、服务接口、常量等
- **LYBT.WPF.Client.Services**: 服务层，包含业务逻辑和API调用
- **LYBT.WPF.Client.Infrastructure**: 基础设施，包含通用控件、转换器、样式等

### 业务模块

- **Authentication**: 认证模块（登录、密码管理等）
- **SystemManagement**: 系统管理模块（超级管理员/管理员功能）
- **FrontDesk**: 前台模块（挂号、排队等）
- **Doctor**: 医生模块（诊疗、开方等）
- **Cashier**: 收银员模块（收费、结算等）
- **Pharmacist**: 药剂师模块（配药、发药等）
- **Common**: 通用模块（公共对话框、控件等）

## 技术栈

- **.NET 8.0**: 基础框架
- **WPF**: 用户界面框架
- **Prism 8.1+**: MVVM框架和模块化容器
- **Unity**: 依赖注入容器
- **Serilog**: 日志框架
- **Refit**: HTTP客户端
- **AutoMapper**: 对象映射
- **FluentValidation**: 数据验证
- **NPOI**: Excel操作
- **iTextSharp**: PDF生成

## 开发指南

### 环境要求

- Visual Studio 2022 或更高版本
- .NET 8.0 SDK
- Windows 10/11

### 项目约定

1. 所有类名和接口与后端WebAPI保持一致
2. 使用原生WPF控件，不依赖第三方UI库
3. 严格遵循MVVM模式
4. 模块间通过接口通信
5. 统一的错误处理和日志记录

### 构建说明

1. 运行 `create_project_structure.bat` 创建完整目录结构
2. 使用 Visual Studio 打开 `LYBT.WPF.Client.sln`
3. 执行 `dotnet restore` 恢复NuGet包
4. 执行 `dotnet build` 编译项目

## 与后端集成

- API基地址: https://localhost:5001 (开发环境)
- 认证方式: JWT Bearer Token
- 数据格式: JSON
- 错误处理: 统一的ApiResponse格式

## 角色和权限

- **超级管理员**: 完整系统管理权限
- **管理员**: 系统管理和配置权限
- **医生**: 诊疗和开方权限
- **前台**: 挂号和患者管理权限
- **收银员**: 收费和结算权限
- **药剂师**: 配药和药房管理权限

## 版本信息

- 版本: 1.0.0
- 最后更新: 2025-07-29
- 维护者: 凌隐宝堂技术团队