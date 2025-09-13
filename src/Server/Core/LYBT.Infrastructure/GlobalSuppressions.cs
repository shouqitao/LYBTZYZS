// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// 抑制过时成员的警告 - 这些是系统演进过程中的正常现象
[assembly: SuppressMessage("Compiler", "CS0618", Justification = "过时成员警告 - 系统演进过程中的向后兼容性保证", Scope = "assembly")]

// 抑制StyleCop XML文档分析禁用警告 - 项目配置决定
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA0001", Justification = "XML注释分析根据项目配置禁用", Scope = "assembly")]

// 抑制可空引用类型的警告 - 遗留代码兼容性
[assembly: SuppressMessage("Compiler", "CS8601", Justification = "可空引用赋值 - 遗留代码向可空引用类型迁移过程中的兼容性问题", Scope = "assembly")]
