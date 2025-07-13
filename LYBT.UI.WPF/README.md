# LYBT.UI.WPF

## 概述

本项目是 LYBT 系统的桌面客户端。它是一个基于 Windows Presentation Foundation (WPF) 的应用，使用 Prism 实现模块化 MVVM 模式，并通过 Refit 与 WebAPI 通讯。

## 前置条件

- .NET 8 SDK
- 运行于 Windows（WPF）

## 构建与运行

1. 还原并构建项目：
   ```bash
   dotnet build
   ```
2. 启动应用：
   ```bash
   dotnet run --project LYBT.UI.WPF
   ```

## 主要功能

- 支持基于角色的用户登录与导航
- 提供患者、收费、药房等模块的管理界面
- UI 主题基于 MaterialDesign
- 通过 Refit 与 LYBT WebAPI 进行 HTTP 通讯

