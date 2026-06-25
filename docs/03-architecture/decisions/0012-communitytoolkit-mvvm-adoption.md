# ADR-0012: 采用 CommunityToolkit.Mvvm 替代 Prism MVVM 基础设施

## 状态
Accepted

## 上下文
项目使用 Prism 作为 MVVM 框架，但 Prism 的 BindableBase 和 DelegateCommand
在大型 ViewModel 中导致大量样板代码。

## 决策
业务模块的 ViewModel 使用 CommunityToolkit.Mvvm 的 `[ObservableProperty]`
和 `[RelayCommand]` 源生成器，替代 Prism 的 BindableBase/DelegateCommand。

## 理由
- 源生成器减少样板代码（无需手动 OnPropertyChanged）
- 编译时生成 INotifyPropertyChanged 实现
- Prism 的 DI 容器（DryIoc）和导航（IRegionManager）仍然保留
- 基类仍使用 Infrastructure 层的 CoreViewModelBase/NavigableViewModelBase

## 后果
- 新 ViewModel 必须使用 [ObservableProperty]/[RelayCommand]
- 禁止使用 Prism 的 BindableBase/DelegateCommand（仅限新代码）
- 旧代码逐步迁移，不一次性重构
