# Phase 5: Desktop UI 拆分 + 测试补全

## Goal
处理 Desktop 层代码质量问题: 拆分过大 XAML/C# 文件，清理占位符测试，修复架构测试断言。

## Phases

### Phase 5A: UnifiedComponents.xaml 拆分 [complete]
- 拆分为 5 个子文件: DesignTokens / ButtonStyles / InputStyles / DataGridStyles / PanelStyles
- UnifiedComponents.xaml 保留为纯聚合器 (~28 行)

### Phase 5B: ServiceCollectionExtensions 拆分 [complete]
- 抽取 LoggingRegistrationExtensions.cs (~185 行)
- 抽取 HttpServiceRegistrationExtensions.cs (~85 行)
- 删除死代码: ErrorHandlingServiceExtensions.cs, CommonStyles.xaml
- ServiceCollectionExtensions.cs 缩减至 ~210 行

### Phase 5C: 测试清理 [complete]
- 删除 3 个占位符测试文件
- 恢复 Batch2_ConfigurationDirectRead 实际检查逻辑
- 重写 Should_Use_Unified_Navigation_Service 为有效断言

### 验证 [complete]
- dotnet build: 0 errors, 0 warnings
- Architecture tests: 74 passed
- Desktop unit tests: 612 passed
- Server unit tests: 370 passed

## Decisions
- XAML 拆分: 利用 WPF MergedDictionaries 加载顺序保证 StaticResource 跨文件解析
- 测试减少 3 个 (占位符删除)，Architecture 测试从存根恢复为实际断言
