# 5分钟快速开始

**目标**：让完全新手在5分钟内启动系统并完成首次操作

**创建日期**：2025-10-29
**状态**：🚧 占位文档（待补充详细内容）

---

## 📋 你将学到

完成本教程后，你将能够：
- ✅ 配置.NET 8和SQL Server开发环境
- ✅ 启动Server端（WebAPI后端）
- ✅ 启动Client端（Desktop WPF桌面应用）
- ✅ 完成首次登录和基本操作

**预计时间**：5分钟
**难度**：⭐（入门）

---

## 🎯 先决条件

在开始之前，请确保你的电脑满足以下条件：

### 硬件要求
- ⚠️ **TODO**：补充最低硬件配置（CPU、内存、磁盘）

### 软件要求
- ⚠️ **TODO**：列出必需软件清单
  - .NET 8 SDK
  - Visual Studio 2022或JetBrains Rider
  - SQL Server 2022 Express
  - Git

---

## 📝 步骤1：克隆仓库

⚠️ **TODO**：补充详细步骤

```bash
# 克隆仓库
git clone https://github.com/shouqitao/LYBTZYZS.git

# 进入项目目录
cd LYBTZYZS
```

**验证**：检查目录结构是否完整

---

## 📝 步骤2：配置数据库

⚠️ **TODO**：补充详细步骤

### 2.1 创建数据库

```sql
-- TODO: 补充数据库创建脚本
```

### 2.2 运行迁移

```bash
# TODO: 补充EF Core迁移命令
```

**验证**：确认数据库表正确创建

---

## 📝 步骤3：启动Server端

⚠️ **TODO**：补充详细步骤

### 3.1 配置连接字符串

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "TODO: 补充连接字符串示例"
  }
}
```

### 3.2 启动WebAPI

```bash
# TODO: 补充启动命令
dotnet run --project src/Server/Services/LYBT.WebAPI
```

**验证**：浏览器访问 https://localhost:5001/swagger，看到API文档页面

---

## 📝 步骤4：启动Client端

⚠️ **TODO**：补充详细步骤

### 4.1 配置API地址

```json
// TODO: 补充Client端配置
```

### 4.2 启动Desktop应用

```bash
# TODO: 补充启动命令
```

**验证**：看到登录界面

---

## 📝 步骤5：首次登录

⚠️ **TODO**：补充详细步骤

### 5.1 使用默认账号登录

```
用户名：TODO
密码：TODO
```

### 5.2 创建测试病案

⚠️ **TODO**：补充操作截图和步骤

**验证**：成功创建病案并在列表中看到

---

## ✅ 成功标志

完成以上步骤后，你应该能够：
- [x] Server端正常运行（Swagger页面可访问）
- [x] Client端正常运行（登录界面正常显示）
- [x] 成功登录系统
- [x] 创建一个测试病案

---

## 🐛 常见问题

### 问题1：数据库连接失败

⚠️ **TODO**：补充常见错误和解决方案

**错误信息**：
```
A network-related or instance-specific error occurred...
```

**解决方案**：
1. TODO: 检查SQL Server服务是否启动
2. TODO: 验证连接字符串配置
3. TODO: 检查防火墙设置

### 问题2：端口被占用

⚠️ **TODO**：补充端口冲突解决方案

### 问题3：编译错误

⚠️ **TODO**：补充常见编译错误

---

## 📚 下一步

恭喜完成快速开始！接下来推荐：

1. **理解架构**：阅读[架构总览](../architecture/README.md)了解三层对齐架构
2. **实战开发**：尝试[开发第一个功能](first-feature.md)教程
3. **深入学习**：查阅[Server端开发指南](../development/server/README.md)和[Client端开发指南](../development/client/README.md)

---

## 🔗 相关资源

- [项目README](../../README.md) - 项目总览
- [文档导航中心](../index.md) - 完整文档索引
- [常见问题解决](../quick-reference/troubleshooting.md) - 问题排查

---

⚠️ **编辑者注意**：本文档为占位版本，需要补充详细步骤、截图和验证命令。请参考Issue #1715完成内容填充。

**最后更新**：2025-10-29
**状态**：占位文档（待补充）
