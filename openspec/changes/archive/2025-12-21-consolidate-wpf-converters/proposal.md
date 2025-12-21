# Proposal: consolidate-wpf-converters

## Summary

统一管理Desktop层WPF转换器(IValueConverter)，消除重复定义，建立统一的转换器资源字典。

## Problem Statement

当前Desktop层存在以下问题:

### 1. 转换器分散在多个位置

| 位置 | 数量 | 说明 |
|------|------|------|
| LYBT.Desktop.Infrastructure/Converters/ | 15个 | 核心转换器 |
| LYBT.Desktop.Shell/Converters/ | 5个 | Shell专用，部分重复 |
| LYBT.Desktop.MedicalCase/Converters/ | 1个 | 模块专用，与核心重复 |

### 2. 重复的转换器实现

| Shell/模块版本 | Infrastructure版本 | 差异 |
|---------------|-------------------|------|
| `FirstCharConverter` | `FirstCharacterConverter` | 功能相同，仅命名不同 |
| `ApiHealthStatusToColorConverter` | 同名 | 颜色值略有差异 |
| `ApiHealthStatusToTextConverter` | 同名 | 完全相同 |
| `InvertedBoolConverter` | `InverseBooleanConverter` | 功能相同，默认值略有差异 |

### 3. XAML资源注册混乱

- 部分View在本地Resources重复定义已全局注册的转换器
- 使用WPF内置`BooleanToVisibilityConverter`与自定义版本混用
- 无统一的转换器资源字典

## Proposed Solution

### Phase 1: 统一转换器到Infrastructure

1. 保留Infrastructure中的转换器作为唯一实现
2. 删除Shell和模块中的重复转换器
3. 统一颜色值差异(使用Fluent Design标准色)

### Phase 2: 创建转换器资源字典

1. 在Infrastructure中创建`Converters.xaml`资源字典
2. 预注册所有转换器实例
3. 在App.xaml中合并该资源字典

### Phase 3: 清理View中的重复定义

1. 移除View本地Resources中的转换器定义
2. 统一使用全局注册的转换器
3. 更新XAML命名空间引用

## Success Criteria

- [ ] 所有转换器统一定义在Infrastructure/Converters目录
- [ ] 无重复的转换器实现
- [ ] 所有View使用全局注册的转换器StaticResource
- [ ] 编译通过，UI功能正常

## Affected Areas

- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/`
- `src/Client/Desktop/Shell/Converters/` (删除)
- `src/Client/Desktop/Shell/App.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Converters/` (删除)
- 各模块View中的转换器引用

## Risks & Mitigations

| 风险 | 缓解措施 |
|------|----------|
| 颜色值统一可能影响UI一致性 | 使用Fluent Design标准色，确保UI测试 |
| 删除转换器可能导致编译错误 | 分阶段执行，每阶段验证编译 |
| View引用更新遗漏 | 使用Grep全面搜索确认 |

## References

- Spec: `ui-style-conventions` - UI样式规范
- Spec: `desktop-code-patterns` - Desktop代码模式
- OpenSpec: `consolidate-shared-utilities` - 类似的工具类统一模式
