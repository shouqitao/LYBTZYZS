# ADR-0006: ViewModel 组件化分解模式

**状态**: 已采纳
**日期**: 2025-12-04
**来源**: ADR-004, ADR-009

## 背景

Desktop 端 ViewModel 随业务增长容易膨胀超过 500 行，导致可维护性和可测试性下降。

## 决策

超过 500 行的 ViewModel 必须拆分为 Coordinator + Components 模式:

```
ViewModels/
  {Feature}ViewModel.cs           # Coordinator (绑定+导航)
  Components/
    {Feature}DataManager.cs       # 数据加载、缓存
    {Feature}CommandHandler.cs    # CRUD 命令
    {Feature}Validator.cs         # 业务验证
```

### 标准 Component 类型

| 类型 | 职责 |
|------|------|
| DataManager | 数据加载、缓存、导入导出 |
| CommandHandler | CRUD 和批量操作 |
| Validator | 业务规则验证 |
| Calculator | 计算逻辑 (可选) |

### 注册方式
- Component 注册为 Transient
- 通过构造函数注入到 ViewModel

## 理由

- 单一职责: 每个 Component 专注一项能力
- 可测试性: Component 可独立单元测试
- 可复用性: DataManager/Validator 可跨 ViewModel 共享

## 变更记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 初始决策 |
