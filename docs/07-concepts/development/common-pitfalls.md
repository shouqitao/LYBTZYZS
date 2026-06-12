---
type: development
title: 常见陷阱与解决方案
tags: [development, pitfalls, errors, debugging]
created: 2026-06-10
updated: 2026-06-10
source: .learnings/ERRORS.md, .learnings/LEARNINGS.md
---

## 概述

本文档汇总了 LYBTZYZS 项目开发过程中遇到的常见错误、已知陷阱和调试技巧，按类别组织，帮助开发者快速定位和解决问题。内容来源于项目 `.learnings` 目录中的 ERRORS.md 和 LEARNINGS.md。

## 核心内容

### PowerShell 语法相关

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| `export` 命令不识别 | Windows PowerShell 不支持 bash 的 `export` 语法 | 使用 `$env:VAR=value; git add ...` 或直接执行 git 命令 |
| Heredoc `$(cat <<'EOF')` 失败 | PowerShell 不支持 bash heredoc 语法 | 使用简单字符串：`git commit -m "feat: description"` |
| `grep` 工具不可用 | OpenCode 环境中没有 `grep` 工具 | 使用 `ast_grep_search`（代码搜索）或 `bash` + `Select-String`（文本搜索） |

**示例 - PowerShell 设置环境变量**：
```powershell
# 错误写法
export GIT_TERMINAL_PROMPT=0; git add .

# 正确写法
$env:GIT_TERMINAL_PROMPT=0; git add .
```

### 构建与编译相关

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| CS0759: 分部方法跨类边界 | `partial void OnXxxChanged()` 在基类生成，派生类尝试实现 | 改用 `PropertyChanged` 事件订阅 |
| NETSDK1100: 交叉编译失败 | Ubuntu 到 Windows 交叉编译缺少参数 | 添加 `-p:EnableWindowsTargeting=true` |
| XAML LSP diagnostics 未配置 | 没有 XAML 语言服务器 | 使用 build/design-time 编译验证，而非 LSP |
| Edit tool 空参数失败 | oldString 和 newString 相同或为空 | 确保提供不同的 oldString 和 newString |

**示例 - PropertyChanged 事件订阅（替代 partial 方法）**：
```csharp
// 错误写法 - 无法跨类继承
partial void OnErrorMessageChanged(string value) { ... }

// 正确写法 - 事件订阅
PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(ErrorMessage) || e.PropertyName == nameof(StatusMessage))
        OnPropertyChanged(nameof(HasMessage));
};
```

### 测试相关

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| 泛异常断言 | `Assert.ThrowsAsync<Exception>` 太宽泛 | 精确指定异常类型和状态码 |
| Mock 调用断言 | 只验证"调用了mock"，不验证业务结果 | 断言返回值的实际内容 |
| 测试实现细节 | 断命名字段 `_isDirty` 等内部字段 | 通过公共 API 验证行为 |
| Desktop 测试框架不匹配 | 混用 net8.0 和 net8.0-windows | Desktop 测试必须使用 `net8.0-windows` |

**示例 - 正确的断言方式**：
```csharp
// 错误 - 泛异常
await Assert.ThrowsAsync<Exception>(() => repo.CreateAsync(patient));

// 正确 - 精确断言
var ex = await Assert.ThrowsAsync<ApiException>(() => repo.CreateAsync(patient));
ex.StatusCode.Should().Be(HttpStatusCode.Conflict);
```

### 运行时相关

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| Server 2012 R2 `New-LocalUser` 不存在 | PowerShell 4.0 没有这些 cmdlet | 使用传统 `net user` 命令 |
| Server 2012 R2 密码复杂度 | 纯数字密码被拒绝 | 使用大小写+数字+特殊字符组合 |
| ping 不通但 SSH 正常 | ICMP 被防火墙禁用 | 直接测试目标端口（如 SSH 端口 22） |
| 中文乱码 | Server 2012 R2 cmd 编码问题 | 执行 `chcp 65001` 切换 UTF-8 |

**示例 - Server 2012 R2 创建用户**：
```powershell
# 错误 - PowerShell 4.0 不支持
New-LocalUser -Name "lybtapi" -Password $securePassword

# 正确 - 传统命令
net user lybtapi "P@ssw0rd!2026" /add
```

### 数据库与 Repository 相关

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| `FindAsync` 过滤软删除记录 | 全局查询过滤器 `IsDeleted` 生效 | 需要查询已删除记录时使用 `IgnoreQueryFilters()` |
| MedicalCase.HasPrescription 为 null | Mapper 未显式设置该计算属性 | 在 Mapper 中显式映射 `PrescriptionId.HasValue` |
| Service 直接注入 AppDbContext | 违反架构规则 | 必须通过 Repository 接口访问数据 |

### MVVM 与 WPF 相关

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| `[NotifyPropertyChangedFor]` 失效 | 目标属性不在同一个类中定义 | 使用 `PropertyChanged` 事件订阅 |
| CommunityToolkit.Mvvm partial 方法跨类 | partial 方法不能跨越类边界 | 在定义 partial 方法的类内实现，或用事件订阅 |
| XAML 资源文件 LSP 诊断缺失 | 没有配置 XAML 语言服务器 | 通过 build 验证，而非 LSP |

### 交叉编译与部署

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| Ubuntu 交叉编译失败 | 缺少 Windows 目标启用参数 | 添加 `-p:EnableWindowsTargeting=true` |
| SSH 自动化失败 | 密码包含特殊字符 | 使用 `sshpass` 并转义特殊字符 |

**示例 - Ubuntu 交叉编译到 Windows**：
```bash
# 错误 - 缺少参数
dotnet publish -c Release -r win-x64 --self-contained false

# 正确 - 添加 EnableWindowsTargeting
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj \
    -c Release -r win-x64 --self-contained false \
    -p:EnableWindowsTargeting=true
```

## 相关链接

- [构建和运行命令参考](build-and-run.md)
- [测试开发指南](../testing-strategy.md)
- overview - 项目概览
