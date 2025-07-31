# LYBT WebAPI 自动化部署系统说明文档

## 📋 系统概述

本自动化部署系统专为Windows Server 2016环境设计，实现本地WebAPI项目自动发布、上传、部署和重启的完整流程。

## 🎯 功能特性

- ✅ **一键部署**：本地执行一个脚本，完成整个部署流程
- ✅ **自动备份**：部署前自动备份当前版本
- ✅ **无缝重启**：服务器端自动停止、部署、启动服务
- ✅ **健康检查**：部署后自动验证服务状态
- ✅ **中文支持**：完整的UTF-8中文字符支持
- ✅ **错误处理**：完善的错误处理和日志记录

## 📁 脚本文件说明

### 本地端脚本
- `auto-deploy.bat` - 主部署脚本
- `upload-to-server.ps1` - 文件上传脚本
- `trigger-server-deploy.ps1` - 远程部署触发脚本
- `test-encoding.bat` - 中文编码测试脚本

### 服务器端脚本
- `server-deploy.bat` - 服务器端部署脚本
- `file-monitor.bat` - 文件监控脚本
- `install-service.bat` - Windows服务安装脚本
- `setup-server.bat` - 服务器环境初始化脚本

## 🚀 部署步骤

### 第一步：服务器端环境准备

1. **以管理员身份运行**服务器端初始化：
```cmd
setup-server.bat
```

2. **安装Windows服务**（可选）：
```cmd
install-service.bat
```

### 第二步：配置网络连接

确保以下网络连接方式之一可用：

#### 方式1：PowerShell Remoting（推荐）
```powershell
# 在服务器端启用PSRemoting
Enable-PSRemoting -Force
Set-Item WSMan:\localhost\Client\TrustedHosts * -Force
```

#### 方式2：网络共享
```cmd
# 确保C$共享可访问
net share C$
```

#### 方式3：使用WinSCP或PsExec工具

### 第三步：本地部署

1. **测试中文编码**：
```cmd
test-encoding.bat
```

2. **执行自动部署**：
```cmd
auto-deploy.bat
```

## ⚙️ 配置参数

### auto-deploy.bat 配置
```batch
set "SERVER_IP=192.168.190.243"          # 服务器IP地址
set "SERVER_USER=Administrator"          # 服务器用户名
set "SERVER_DEPLOY_PATH=C:\LYBT\WebAPI"  # 服务器部署路径
set "LOCAL_PROJECT_PATH=D:\source\repos\LYBTZYZS\src\Backend\Services\LYBT.WebAPI"
set "LOCAL_PUBLISH_PATH=D:\source\repos\LYBTZYZS\Release\WebAPI"
```

## 📂 目录结构

### 服务器端目录结构
```
C:\LYBT\
├── WebAPI\          # WebAPI程序文件
├── Backup\          # 自动备份目录
├── Logs\            # 部署日志
└── Scripts\         # 部署脚本
    ├── server-deploy.bat
    └── file-monitor.bat

C:\temp\             # 临时文件目录
└── WebAPI-Deploy.zip
```

### 本地端目录结构
```
D:\source\repos\LYBTZYZS\
├── scripts\         # 部署脚本目录
│   ├── auto-deploy.bat
│   ├── upload-to-server.ps1
│   ├── trigger-server-deploy.ps1
│   └── test-encoding.bat
├── Release\         # 发布输出目录
│   └── WebAPI\
└── src\Backend\Services\LYBT.WebAPI\  # 源码目录
```

## 🔧 技术细节

### 编码支持
所有脚本均配置UTF-8编码支持：
```batch
@echo off
chcp 65001 >nul 2>&1
setlocal enabledelayedexpansion
```

### PowerShell编码配置
```powershell
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
```

### 错误处理
- 每个步骤都有错误检查
- 失败时自动回滚
- 详细的错误日志记录

## 🛠 故障排除

### 常见问题

1. **中文字符显示乱码**
   - 运行 `test-encoding.bat` 测试
   - 确保控制台字体支持中文

2. **网络连接失败**
   - 检查防火墙设置
   - 验证PowerShell Remoting配置
   - 测试网络共享访问

3. **服务启动失败**
   - 检查端口占用情况
   - 验证数据库连接
   - 查看应用程序日志

4. **权限问题**
   - 确保以管理员身份运行
   - 检查文件夹权限设置

### 日志位置
- 部署日志：`C:\LYBT\Logs\deploy.log`
- 应用程序日志：查看Windows事件日志
- PowerShell错误：控制台输出

## 📞 技术支持

### 手动回滚步骤
1. 停止当前服务
2. 从备份目录恢复文件
3. 重启服务

### 健康检查
```
GET http://192.168.190.243:5297/health
```

### 服务管理
```cmd
# 查看服务状态
sc query LYBTWebAPI

# 手动启动服务
net start LYBTWebAPI

# 手动停止服务
net stop LYBTWebAPI
```

## 🎉 总结

本自动化部署系统提供了完整的WebAPI部署解决方案，支持：
- 快速一键部署
- 安全的备份机制
- 完善的错误处理
- 良好的中文显示支持

适用于Windows Server 2016及以上版本的生产环境。