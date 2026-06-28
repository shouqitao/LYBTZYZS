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

## 关联 US

- US-SHELL-001 / US-SHELL-003 / US-SHELL-005（启动管线、模块加载、菜单导航：Coordinator + Components 拆分）
- US-SHELL-010 / US-SHELL-011 / US-SHELL-018（安装、初始化向导、sysadmin 配置中心：ViewModel 分解）

## 代码验证对照（2026-06-28 审计）

**已实现的 Component 清单：**

| 模块 | Coordinator | Components | 行数 |
|------|-------------|------------|------|
| MedicalCase | MedicalCaseViewModel | MedicalCaseDataManager, MedicalCaseCommandHandler | ~480 |
| Patients | PatientViewModel | PatientDataManager, PatientCommandHandler | ~420 |
| Herbs | HerbViewModel | HerbDataManager, HerbCommandHandler | ~350 |
| Formulas | FormulaViewModel | FormulaDataManager, FormulaCommandHandler | ~380 |
| Users | UserViewModel | UserDataManager, UserCommandHandler | ~400 |

**合规检查**: 5 个 MasterDetail ViewModel 均已拆分，最大文件 480 行（< 500 行阈值）。

**新 ViewModel 入口检查**: PR review 时需确认新 ViewModel 是否超过 300 行（预警线），超过 500 行必须拆分。
