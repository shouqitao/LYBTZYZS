# 代码分析配置说明

本文档说明凌隐宝堂中医诊所管理系统的代码分析规则配置。

## 概述

项目使用 `.editorconfig` 文件来统一代码风格和质量检查，确保代码库的一致性和质量。配置涵盖了以下几个方面：

- 代码风格规则（IDE0xxx）
- 代码质量规则（CAxxxx）  
- 命名约定规则
- 现代C#语法推荐
- 异步模式规则
- 未使用代码检测
- 项目特定抑制规则

## 配置结构

### 基础编辑器配置

```ini
# 全局文件配置
[*]
charset = utf-8-bom
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

# C# 代码文件
[*.{cs,vb}]
indent_style = space
indent_size = 4
```

### 代码风格规则 (IDE0xxx)

#### 核心风格规则

| 规则ID | 严重性 | 说明 | 配置 |
|--------|--------|------|------|
| IDE0005 | warning | 移除不必要的 using 指令 | `dotnet_diagnostic.IDE0005.severity = warning` |
| IDE0007 | suggestion | 使用 var 关键字的偏好 | `csharp_style_var_when_type_is_apparent = true:suggestion` |
| IDE0017 | suggestion | 使用对象初始化器 | `dotnet_style_object_initializer = true:suggestion` |
| IDE0028 | suggestion | 使用集合初始化器 | `dotnet_style_collection_initializer = true:suggestion` |
| IDE0040 | suggestion | 使用 nameof 表达式 | 启用自动检测 |
| IDE0059 | suggestion | 移除不必要的值赋值 | 自动检测未使用变量 |
| IDE0060 | suggestion | 移除未使用的参数 | 检测方法中未使用的参数 |
| IDE0161 | suggestion | 使用文件作用域命名空间 | `csharp_style_namespace_declarations = file_scoped:suggestion` |

#### 表达式和模式匹配

```ini
# 使用模式匹配
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion

# 使用表达式主体
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = when_on_single_line:suggestion
```

### 代码质量规则 (CAxxxx)

#### 性能规则

| 规则ID | 严重性 | 说明 |
|--------|--------|------|
| CA1822 | suggestion | 标记成员为静态（当适用时） |
| CA1825 | warning | 避免零长度数组分配 |
| CA1826 | warning | 使用属性而不是 Linq Enumerable 方法 |
| CA1827 | warning | 不要使用 Count()/LongCount() |
| CA1829 | warning | 使用 Length/Count 属性而不是 Count() 方法 |
| CA1834 | suggestion | 对单字符字符串使用 StringBuilder.Append(char) |

#### 设计规则

| 规则ID | 严重性 | 说明 |
|--------|--------|------|
| CA1001 | warning | 具有可释放字段的类型应该是可释放的 |
| CA1031 | suggestion | 不要捕获一般异常类型 |
| CA1062 | suggestion | 验证公共方法的参数 |
| CA1063 | suggestion | 正确实现 IDisposable |
| CA1068 | suggestion | CancellationToken 参数必须最后出现 |

#### 安全规则

| 规则ID | 严重性 | 说明 |
|--------|--------|------|
| CA2100 | suggestion | 检查 SQL 查询是否存在安全漏洞 |
| CA5394 | warning | 不要使用不安全的随机性 |
| CA5397 | warning | 不要使用已弃用的 SslProtocols 值 |

### 命名约定

#### 接口命名
```ini
# 接口必须以 'I' 开头
dotnet_naming_rule.interface_should_be_prefixed_with_i.severity = suggestion
dotnet_naming_rule.interface_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_prefixed_with_i.style = prefixed_with_i
```

#### 私有字段命名
```ini
# 私有字段使用下划线前缀
dotnet_naming_rule.private_fields_with_underscore.severity = suggestion
dotnet_naming_rule.private_fields_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_with_underscore.style = underscore_camel_case
```

#### 类型和成员命名
```ini
# 类型和公共成员使用 PascalCase
dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.non_field_members_should_be_pascal_case.severity = suggestion
```

### 异步模式规则

#### Visual Studio Thread Helper 规则

| 规则ID | 严重性 | 说明 |
|--------|--------|------|
| VSTHRD100 | error | 避免异步 void 方法（医疗系统要求严格） |
| VSTHRD002 | warning | 避免有问题的同步阻塞 |
| VSTHRD104 | warning | 提供异步选项 |
| VSTHRD200 | suggestion | 异步方法名称应以 "Async" 结尾 |

#### ConfigureAwait 规则
```ini
# 库代码中考虑 ConfigureAwait(false)
dotnet_diagnostic.CA2007.severity = none   # 在大多数情况下放宽此要求
```

### 现代 C# 语法推荐

#### C# 新特性
- 文件作用域命名空间 (IDE0161)
- Switch 表达式 (IDE0066) 
- 模式匹配 (IDE0078)
- is not 模式 (IDE0084)
- 简化的 new 表达式 (IDE0090)
- 索引和范围运算符 (IDE0056, IDE0057)
- 复合赋值 (IDE0054, IDE0074)

### 项目特定配置

#### 文件类型特定规则

##### 测试文件 (`*Test*.cs`, `*Tests.cs`, `*.Test.cs`)
```ini
dotnet_diagnostic.CA1062.severity = none    # 测试中的参数验证可以放松
dotnet_diagnostic.CA1707.severity = none    # 测试方法名可以包含下划线
dotnet_diagnostic.CA1822.severity = none    # 测试方法不需要是静态的
```

##### ViewModel 文件 (`*ViewModel.cs`, `*ViewModels.cs`)
```ini
dotnet_diagnostic.CA1822.severity = none    # ViewModel 方法通常需要实例绑定
dotnet_diagnostic.CA1062.severity = none    # Prism 注入的参数由框架保证非空
```

##### 数据传输对象 (`*Dto.cs`, `*Request.cs`, `*Response.cs`)
```ini
dotnet_diagnostic.CA1051.severity = none    # DTO 可以有公共字段
dotnet_diagnostic.CA2227.severity = none    # DTO 集合属性可以是可设置的
```

##### 业务服务层 (`*BusinessService.cs`, `*Service.cs`) - 更严格
```ini
dotnet_diagnostic.CA1062.severity = warning  # 业务服务必须验证参数
dotnet_diagnostic.CA1031.severity = warning  # 不要捕获一般异常
dotnet_diagnostic.IDE0060.severity = warning # 移除未使用的参数
```

##### 数据访问层 (`*Repository.cs`, `*DbContext.cs`)
```ini
dotnet_diagnostic.CA2100.severity = warning  # 检查 SQL 注入漏洞
dotnet_diagnostic.CA1062.severity = warning  # 验证数据库参数
```

##### 控制器 (`*Controller.cs`)
```ini
dotnet_diagnostic.CA1062.severity = warning  # 验证 API 参数
dotnet_diagnostic.CA2100.severity = warning  # 防止 SQL 注入
dotnet_diagnostic.CA1054.severity = suggestion # URI 参数类型检查
```

#### 项目特定抑制

##### XML 文档注释（与 Directory.Build.props 一致）
```ini
dotnet_diagnostic.CS1591.severity = none    # 缺少公共可见类型或成员的XML注释
dotnet_diagnostic.CS1570.severity = none    # XML注释格式错误
dotnet_diagnostic.CS1572.severity = none    # XML注释参数不匹配
```

##### WPF 和 Prism 相关
```ini
dotnet_diagnostic.CA1822.severity = none    # WPF 事件处理程序不能是静态的
dotnet_diagnostic.CA1062.severity = none    # WPF 绑定参数可能为 null，但由框架保证
```

##### 中医诊所业务相关
```ini
dotnet_diagnostic.CA1303.severity = none    # 暂不考虑本地化，诊所内部使用
dotnet_diagnostic.CA1307.severity = none    # 中文字符串比较暂用默认规则
```

### 代码质量等级设置

```ini
# 默认质量等级（适中严格度）
dotnet_analyzer_diagnostic.category-design.severity = suggestion
dotnet_analyzer_diagnostic.category-performance.severity = suggestion  
dotnet_analyzer_diagnostic.category-reliability.severity = warning
dotnet_analyzer_diagnostic.category-security.severity = warning
dotnet_analyzer_diagnostic.category-usage.severity = suggestion
```

## IDE 集成

### Visual Studio / Visual Studio Code

配置会自动被 IDE 识别并应用：

1. **代码高亮**：违反规则的代码会被高亮显示
2. **快速修复**：大多数规则提供自动修复选项
3. **重构建议**：IDE 会根据规则提供重构建议
4. **格式化**：`Ctrl+K, Ctrl+D` 会应用配置的格式化规则

### 命令行工具

```powershell
# 检查代码格式
dotnet format --verify-no-changes

# 应用代码格式
dotnet format

# 分析特定项目
dotnet format LYBT.Server.sln --verbosity diagnostic
```

## 性能关键代码特殊处理

对于标记为性能关键的文件（`*Performance*.cs`, `*Cache*.cs`, `*Query*.cs`），启用更严格的性能相关警告：

```ini
dotnet_diagnostic.CA1822.severity = warning  # 性能关键代码建议静态方法
dotnet_diagnostic.CA1825.severity = warning  # 避免零长度数组
dotnet_diagnostic.CA1826.severity = warning  # 使用属性而不是 LINQ
```

## 医疗系统特殊要求

### 安全性增强
- SQL 注入检测（CA2100）设为 warning
- 不安全随机性检测（CA5394）设为 warning
- 弃用安全协议检测（CA5397）设为 warning

### 可靠性增强
- 异步 void 方法（VSTHRD100）设为 error
- 同步阻塞检测（VSTHRD002）设为 warning
- 资源释放检测（CA2000, CA1063）设为 warning

## 维护指南

### 添加新规则
1. 在 `.editorconfig` 文件中添加规则配置
2. 更新本文档
3. 测试规则在 IDE 中的表现
4. 考虑是否需要文件类型特定的配置

### 调整严重性
1. 根据团队反馈调整规则严重性
2. 考虑项目特定需求
3. 保持与 `Directory.Build.props` 中设置的一致性

### 性能考虑
- 过多的规则可能影响 IDE 性能
- 定期检查和清理不再需要的规则
- 优先配置对代码质量影响最大的规则

## 相关文件

- `.editorconfig` - 主配置文件
- `Directory.Build.props` - 构建级别的警告配置
- `Directory.Packages.props` - 代码分析工具包配置
- `docs/development/standards.md` - 开发标准文档

## 常见问题

### Q: IDE 中没有显示代码分析警告？
A: 
1. 确保安装了 StyleCop.Analyzers 包
2. 检查 IDE 的错误列表设置
3. 重启 IDE 并重新构建项目

### Q: 某些规则太严格，如何调整？
A:
1. 在 `.editorconfig` 中降低规则严重性
2. 或者为特定文件类型添加例外
3. 考虑添加到项目特定抑制区域

### Q: 如何为新的文件类型添加规则？
A:
1. 在 `.editorconfig` 中添加文件模式匹配 `[*.extension]`
2. 配置该文件类型的特定规则
3. 测试配置是否生效

---

最后更新：2025-09-28
版本：1.0.0