# Proposal: cleanup-desktop-core-unused

## Summary

清理Desktop Core层四个项目中的无用代码，删除从未被使用的接口、类和服务。

## Why

Desktop Core层(Foundation、Infrastructure、Models、Presentation)经过多次迭代，累积了大量未使用的代码：

### P0级 - 无用代码 (11个文件)

| 文件 | 项目 | 问题描述 |
|------|------|----------|
| CommandFactory.cs | Foundation | 命令工厂类，从未被实例化或引用 |
| DiagnosticService.cs | Foundation | 诊断服务，静态方法从未被调用 |
| SecurityService.cs | Foundation | 安全服务，从未被DI注册或注入 |
| IUnifiedApiClientManager.cs | Foundation | 接口定义，无实现且从未使用 |
| BaseApiRepository.cs | Foundation | 仓库基类，从未被继承(实际使用RepositoryBase) |
| CommandHandlerBase.cs | Infrastructure | 命令处理器基类，从未被继承 |
| ComponentValidatorBase.cs | Infrastructure | 验证器基类，从未被继承 |
| EnhancedNavigationService.cs | Infrastructure | 增强导航服务，仅定义从未使用 |
| CorrelationIdContext.cs | Infrastructure | Foundation版本的冗余包装器 |
| ThemeService.cs | Presentation | 主题服务，仅DI注册但从未注入 |
| INavigationService.cs | Presentation | 导航接口，定义但无消费者 |

### P1级 - 冗余代码 (2个问题)

| 问题 | 描述 |
|------|------|
| IStandardErrorHandler | 已注册但从未被注入使用 |
| CorrelationId双版本 | Foundation有实现，Infrastructure有包装器，应统一 |

## What Changes

### Phase 1: 删除无用文件

删除以下11个从未被使用的文件：

**Foundation项目 (5个)**:
- [ ] Commands/CommandFactory.cs
- [ ] Diagnostics/DiagnosticService.cs
- [ ] Security/SecurityService.cs (保留ISecurityService备用)
- [ ] Api/Managers/IUnifiedApiClientManager.cs
- [ ] Repositories/BaseApiRepository.cs

**Infrastructure项目 (4个)**:
- [ ] Components/CommandHandlerBase.cs
- [ ] Components/ComponentValidatorBase.cs
- [ ] Services/Navigation/EnhancedNavigationService.cs
- [ ] Logging/CorrelationIdContext.cs (包装器)

**Presentation项目 (2个)**:
- [ ] Theming/ThemeService.cs (含IThemeService)
- [ ] Navigation/INavigationService.cs

### Phase 2: 清理DI注册

更新ServiceCollection扩展方法，移除对已删除服务的注册：
- [ ] PresentationServiceCollectionExtensions.cs - 移除IThemeService注册
- [ ] 验证其他扩展方法无遗漏

### Phase 3: 清理冗余代码

- [ ] 移除IStandardErrorHandler及StandardErrorHandler（如确认未使用）
- [ ] 统一使用Foundation.Logging.CorrelationIdContext

## Scope

### In Scope
- Desktop Core层四个项目的代码清理
- DI注册代码更新
- 编译验证

### Out of Scope
- Modules层业务逻辑变更
- Shell项目变更（除DI注册更新外）
- 新功能开发
- 重构（仅删除无用代码）

## Impact

### 代码变更
- 删除：11-13个文件
- 修改：1-2个DI扩展文件
- 净删除：约800-1000行代码

### 依赖变更
- 无项目引用变更
- 无NuGet包变更

### Breaking Changes
- 无（删除的都是从未被使用的代码）

## Risks

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 遗漏隐藏引用 | 低 | 低 | Grep全量搜索确认无引用 |
| 编译失败 | 低 | 低 | 每删除一批验证编译 |
| 运行时错误 | 极低 | 低 | 现有测试覆盖 |

## Success Criteria

- [ ] 所有无用代码删除
- [ ] 编译通过无错误
- [ ] 现有测试全部通过
- [ ] 无新增警告

---

**Status**: Implemented
**Created**: 2025-12-20
**Implemented**: 2025-12-20
**Author**: Claude Code
