# Spec Delta: viewmodel-conventions

## ADDED Requirements

### Requirement: VM-002-A 标准Components类型

当ViewModel需要拆分时，SHALL 使用以下标准Component类型:

| Component类型 | 命名模式 | 职责 | 必需性 |
|---------------|----------|------|--------|
| CommandHandler | `{Entity}CommandHandler` | 处理用户命令(CRUD/批量操作) | 推荐 |
| DataManager | `{Entity}DataManager` | 数据加载、保存、导入导出 | 推荐 |
| Validator | `{Entity}Validator` | 业务验证逻辑 | 推荐 |
| Calculator | `{Entity}Calculator` | 计算逻辑 | 可选 |
| Coordinator | `{Entity}Coordinator` | 跨组件协调 | 可选 |
| StateMachine | `{Entity}StateMachine` | 状态管理 | 可选 |

#### Scenario: 选择Component类型
- **WHEN** ViewModel超过500行需要拆分
- **THEN** SHALL 优先提取CommandHandler、DataManager、Validator
- **AND** 根据业务需要选择性添加Calculator、Coordinator、StateMachine

#### Scenario: Component命名
- **WHEN** 创建新Component
- **THEN** SHALL 使用`{Entity}{ComponentType}`格式命名
- **AND** SHALL 放置在ViewModels/Components/目录下
