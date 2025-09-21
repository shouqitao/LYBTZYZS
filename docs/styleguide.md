# 代码风格与规范

- 缩进：C# 4 空格；XML/JSON/YAML 2 空格；UTF-8，CRLF，去除行尾空白
- using 顺序：`System.*` 优先；using 放在命名空间外；尽量单行
- 花括号/换行：左花括号换行；`else/catch/finally` 前换行
- 命名：类型与非字段成员 PascalCase；接口前缀 `I`；私有字段 `_camelCase`；异步方法以 `Async` 结尾
- 分析器：启用 StyleCop.Analyzers；修复警告或给出充分理由的抑制
- 文档：公共 API 适度 XML 注释；示例与命令配短中文说明

## 常见 StyleCop 抑制示例
- 局部抑制（建议仅限必要范围）：
  ```csharp
  #pragma warning disable SA1200 // Using directives should be placed correctly
  using System;
  #pragma warning restore SA1200
  ```
- 全局抑制（不推荐）：在 `GlobalSuppressions.cs` 增加特定规则抑制，并注明理由。
