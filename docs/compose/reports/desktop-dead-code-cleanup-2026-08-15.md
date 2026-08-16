# Desktop 死代码/无效代码清理清单

**扫描日期**: 2026-08-16  
**扫描范围**: `src/Client/Desktop/` (479 .cs + 78 .xaml)  
**扫描方法**: 静态分析（grep 引用计数 + Prism DI 注册检查 + XAML 绑定检查）

---

## 扫描摘要

| 类别 | 总数 | 问题项 | 状态 |
|------|------|--------|------|
| ViewModels | 50 | 0 | ✅ 全部有引用 |
| XAML Views | 37 | 0 | ✅ 全部已注册 |
| Controls | 33 | 0 | ✅ 全部有引用 |
| Converters | 12 | 0 | ✅ 通过 Cvt 静态类使用 |
| ViewNames 常量 | 22 | 0 | ✅ 全部有引用 |
| NuGet 包 | 34 | **2** | ⚠️ 需清理 |
| 空类/空方法 | - | 0 | ✅ 无问题 |
| 注释代码块 | - | 0 | ✅ 无问题 |
| 重复定义 | - | 0 | ✅ 无问题 |
| `#if false` 块 | - | 0 | ✅ 无问题 |
| 备份/临时文件 | - | 0 | ✅ 无问题 |
| 未使用的 using | - | - | ℹ️ GlobalUsings.cs 已统一处理 |

---

## 🔴 确认的死代码（建议删除）

### 1. 未使用的 NuGet 包

| 文件 | 包名 | 建议操作 | 说明 |
|------|------|----------|------|
| `Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj` | `Polly` | **删除** | 无任何 .cs 文件引用 Polly 命名空间，仅在 csproj 中声明 |
| `Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj` | `Microsoft.Extensions.Configuration.EnvironmentVariables` | **删除** | 无任何 .cs 文件调用 `AddEnvironmentVariables()`，仅在 csproj 中声明 |

**验证方法**:
```bash
# Polly - 确认无使用
grep -rn 'Polly' src/Client/Desktop --include='*.cs' | grep -v obj/ | grep -v bin/ | grep -v '.csproj'
# 结果: 空

# EnvironmentVariables - 确认无使用
grep -rn 'EnvironmentVariables' src/Client/Desktop --include='*.cs' | grep -v obj/ | grep -v bin/ | grep -v '.csproj'
# 结果: 空
```

---

## 🟡 需验证的包（可能为传递依赖需要）

| 文件 | 包名 | 当前状态 | 建议 |
|------|------|----------|------|
| `Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj` | `SixLabors.Fonts` | 仅在 Printing.csproj 引用 | 保留（QuestPDF 传递依赖覆盖） |
| `Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj` | `SixLabors.ImageSharp` | 仅在 Printing.csproj 引用 | 保留（QuestPDF 传递依赖覆盖） |
| 多个 csproj | `Microsoft.Extensions.Configuration.Binder` | 通过 `IConfiguration.Get<T>()` 间接使用 | 保留 |
| 多个 csproj | `Microsoft.Extensions.Configuration.Json` | 通过 `AddJsonFile()` 间接使用 | 保留 |
| 多个 csproj | `Microsoft.Extensions.Logging.Abstractions` | 通过 `ILogger<T>` 间接使用 | 保留 |

> **注意**: `SixLabors.Fonts` 和 `SixLabors.ImageSharp` 在 Printing.csproj 中有注释说明：
> `<!-- Security: 显式引用 Fonts 和 ImageSharp 覆盖 QuestPDF 的传递依赖 -->`

---

## ✅ 已验证无问题的类别

### ViewModels（50 个）
所有 ViewModel 类均通过以下方式之一被引用：
- Prism DI 注册（`containerRegistry.Register<T>()`）
- XAML DataContext 绑定
- LocationProvider 控件映射
- 其他 ViewModel 的组合/聚合

### XAML Views（37 个）
所有 View 均通过以下方式注册：
- `RegisterForNavigation<T>()`
- `RegisterDialog<T>()`
- `RegisterViewWithRegion()`

### Controls/Converters（45 个）
所有控件和转换器均被引用：
- XAML 使用（通过 `x:Static converters:Cvt.*` 静态引用）
- 其他 XAML 文件中的 `<Controls:XxxControl />` 引用

### ViewNames 常量（22 个）
所有常量均在代码中被引用，用于导航和模块加载。

---

## 📊 代码质量指标

| 指标 | 值 |
|------|-----|
| .cs 文件总数 | 479 |
| .xaml 文件总数 | 78 |
| ViewModel 类 | 50 |
| 自定义控件 | 33 |
| 转换器 | 12 |
| NuGet 包 | 34 |
| 未使用的包 | **2** |
| 注释代码块 | 0 |
| `NotImplementedException` | 0 |
| `#if false` 块 | 0 |
| 备份/临时文件 | 0 |
| GlobalUsings.cs | ✅ 已统一管理 |

---

## 清理操作清单

### 立即执行
1. [ ] 从 `LYBT.Desktop.Foundation.csproj` 移除 `Polly` 包引用
2. [ ] 从 `LYBT.Desktop.Foundation.csproj` 移除 `Microsoft.Extensions.Configuration.EnvironmentVariables` 包引用
3. [ ] 运行 `dotnet restore` 验证无编译错误

### 可选优化
4. [ ] 考虑将 `SixLabors.Fonts` 和 `SixLabors.ImageSharp` 的版本锁定到与 QuestPDF 兼容的版本
5. [ ] 定期运行 `dotnet list package --outdated` 检查包更新

---

## 扫描排除说明

以下内容按设计规则排除：
- **Prism 模块注册的 View/ViewModel** — 通过 DI 容器间接使用
- **主题/样式文件** (`Themes/*.xaml`) — 框架自动加载
- **接口定义** (`I*.cs`) — 可能被外部项目使用
- **Base/Abstract 类** — 作为继承基类使用
- **[ObservableProperty] 字段** — CommunityToolkit.Mvvm 源生成器生成公共属性

---

*报告生成: Hermes Agent (2026-08-16)*
