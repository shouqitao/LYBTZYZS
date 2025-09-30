# Desktop 架构优化 - Issue #820 执行指南

## 📋 概述

本目录包含用于执行 **Issue #820: Desktop 架构优化 - 统一文件夹命名规范与路径结构** 的自动化脚本。

## 🎯 目标

1. 重命名 `Core_New` → `Core`
2. 为所有 Modules 子文件夹添加 `LYBT.Desktop.` 前缀
3. 更新所有项目文件、解决方案文件和代码引用
4. 编译验证
5. 提交到 Git

## 📂 脚本文件

### 1. `execute-issue-820.ps1` ⭐ 主执行脚本

**推荐使用此脚本**，它会自动调用其他脚本并完成整个流程。

```powershell
# 完整执行（包括编译和提交）
.\scripts\execute-issue-820.ps1

# 仅预览（不实际执行）
.\scripts\execute-issue-820.ps1 -DryRun

# 跳过编译验证
.\scripts\execute-issue-820.ps1 -SkipBuild

# 跳过 Git 提交
.\scripts\execute-issue-820.ps1 -SkipGit
```

### 2. `rename-desktop-folders.ps1`

重命名 9 个文件夹（Core_New + 8个Modules）。

```powershell
.\scripts\rename-desktop-folders.ps1
```

### 3. `update-desktop-references.ps1`

更新所有项目文件、解决方案文件和代码引用。

```powershell
.\scripts\update-desktop-references.ps1
```

## 🚀 快速开始

### 方法 1: 一键执行（推荐）

```powershell
# 1. 关闭 Visual Studio（重要！）

# 2. 打开 PowerShell（以管理员身份）

# 3. 切换到项目根目录
cd D:\source\repos\LYBTZYZS

# 4. 先预览将要执行的操作
.\scripts\execute-issue-820.ps1 -DryRun

# 5. 确认无误后执行
.\scripts\execute-issue-820.ps1
```

### 方法 2: 分步执行

```powershell
# 1. 关闭 Visual Studio

# 2. 重命名文件夹
.\scripts\rename-desktop-folders.ps1

# 3. 更新引用
.\scripts\update-desktop-references.ps1

# 4. 编译验证
dotnet build LYBT.Desktop.sln -c Release
dotnet build LYBT.All.sln -c Release

# 5. Git 提交
git add .
git commit -m "refactor(desktop): 统一文件夹命名规范 - Issue #820"
```

## ⚠️ 重要注意事项

### 执行前

1. **关闭 Visual Studio** - 必须关闭，否则文件重命名会失败
2. **关闭相关进程** - 停止所有 dotnet 进程
3. **备份当前状态** - 建议先提交当前所有更改或创建 Git 分支

### 执行中

- 脚本会使用 `git mv` 保留文件历史
- 如果遇到权限问题，请以管理员身份运行 PowerShell
- 脚本会自动清理 bin/obj 缓存

### 执行后

1. 检查 `git status` 确认所有变更符合预期
2. 重新打开 Visual Studio 并加载解决方案
3. 验证编译无错误
4. 推送到远程分支

## 📊 执行流程

```
[1/5] 清理构建缓存
  └─ 删除所有 bin 和 obj 文件夹

[2/5] 重命名文件夹
  ├─ Core_New → Core
  └─ 8 个 Modules 子文件夹添加 LYBT.Desktop. 前缀

[3/5] 更新项目引用
  ├─ 更新 LYBT.Desktop.sln
  ├─ 更新 LYBT.All.sln
  ├─ 更新所有 .csproj 文件
  └─ 更新 .cs 文件的 using 语句

[4/5] 编译验证
  ├─ dotnet build LYBT.Desktop.sln
  └─ dotnet build LYBT.All.sln

[5/5] Git 提交
  └─ git commit -m "..."
```

## 🔍 故障排查

### 问题 1: 权限被拒绝

```
错误: Move-Item: 访问被拒绝
```

**解决方案:**
1. 关闭 Visual Studio
2. 以管理员身份运行 PowerShell
3. 检查文件是否被其他程序占用

### 问题 2: 文件夹已存在

```
警告: 新路径已存在，跳过
```

**解决方案:**
- 文件夹可能已经重命名过
- 检查是否之前部分执行过脚本
- 手动删除冲突的文件夹或使用 `git status` 检查状态

### 问题 3: 编译失败

```
错误: LYBT.Desktop.sln 编译失败
```

**解决方案:**
1. 检查错误日志确定具体问题
2. 确认所有文件已正确重命名
3. 清理并重新构建: `dotnet clean && dotnet build`

## 📝 变更清单

### 文件夹重命名（9个）

- ✅ `src/Client/Desktop/Core_New` → `src/Client/Desktop/Core`
- ✅ `Modules/Auth` → `Modules/LYBT.Desktop.Auth`
- ✅ `Modules/Consultation` → `Modules/LYBT.Desktop.Consultation`
- ✅ `Modules/Formula` → `Modules/LYBT.Desktop.Formula`
- ✅ `Modules/Herbs` → `Modules/LYBT.Desktop.Herbs`
- ✅ `Modules/MedicalCase` → `Modules/LYBT.Desktop.MedicalCase`
- ✅ `Modules/Patients` → `Modules/LYBT.Desktop.Patients`
- ✅ `Modules/Prescriptions` → `Modules/LYBT.Desktop.Prescriptions`
- ✅ `Modules/Users` → `Modules/LYBT.Desktop.Users`

### 文件更新

- ✅ LYBT.Desktop.sln - 项目路径引用
- ✅ LYBT.All.sln - 项目路径引用
- ✅ 所有 .csproj - ProjectReference 路径
- ✅ 所有 .cs - using 语句（如需要）

## 🎉 完成后

执行成功后，您应该看到：

```
========================================
🎉 Issue #820 执行完成！
========================================

执行摘要:
  ✅ 清理构建缓存
  ✅ 重命名文件夹 (9个)
  ✅ 更新项目引用
  ✅ 编译验证通过
  ✅ Git 提交完成

下一步:
  1. 查看 git status 确认所有变更
  2. 推送到远程分支: git push
  3. 在 GitHub 上更新 Issue #820 状态
```

## 📞 支持

如遇到问题，请在 GitHub Issue #820 中反馈。

---

生成时间: 2025-09-30
维护者: Claude Code
相关 Issue: #820