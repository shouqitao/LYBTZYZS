<!-- Parent: ../AGENTS.md -->
<!-- Updated: 2026-08-14 -->

# Resources (Desktop)

## Purpose

Desktop 资源说明（desktop-dead-code-audit DC-004：移除对不存在的 `Dictionaries/` 的引用，如实反映实际位置）。

## 实际资源位置

| 资源 | 实际位置 |
| ------ | --------- |
| XAML 主题字典（styles/colors/control templates） | `Core/LYBT.Desktop.Controls/Themes/`（Surfaces.xaml、TcmBrands.xaml 等，App.xaml 经 `MergedDictionaries` 引用） |
| 本地化字符串 (.resx) | `Shell/Resources/Strings/`（StringResources.resx + 生成的 StringResources.Designer.cs） |

> 历史说明：`Resources/Strings/` 下的重复 `StringResources.resx` 与零引用 `LoginStrings.resx` 已于 2026-08-14 删除（DC-001/DC-003——重复文件保留 Shell 版本，登录字符串已并入 StringResources）。

## For AI Agents

### Working In This Directory

- 本目录当前仅含本文档（无实际资源文件）。
- 新增 XAML 主题字典：放入 `Core/LYBT.Desktop.Controls/Themes/` 并在 `Shell/App.xaml` 的 `MergedDictionaries` 注册。
- 新增字符串资源：编辑 `Shell/Resources/Strings/StringResources.resx`（.Designer.cs 由 Visual Studio 生成器同步）。

### Common Patterns

- 资源字典使用 `ResourceDictionary` + `x:Key`
- 字符串资源通过 `{x:Static}` 绑定或 `StringResources.ClassName` 静态属性访问
