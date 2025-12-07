# Design: DetailView容器化架构

## Context

当前5个DetailView页面（Patient, Herb, User, Formula, MedicalCase）使用IsReadOnly属性在同一组控件上切换查看/编辑模式。这种方式虽然代码量少，但限制了UI表现力：
- 查看模式下TextBox仍显示输入框边框
- 无法为查看模式设计卡片式纯展示布局
- 编辑模式无法添加必填标记、验证提示等交互元素

## Goals / Non-Goals

**Goals:**
- 查看模式使用纯展示控件（TextBlock），无边框无输入框外观
- 编辑模式使用完整表单控件，支持验证和必填标记
- 提供可复用的BaseDetailContainer容器组件
- 保持ViewModel层不变或最小改动

**Non-Goals:**
- 不改变现有的导航和路由机制
- 不改变数据加载和保存逻辑
- 不引入新的第三方UI库

## Decisions

### Decision 1: 容器+面板组合模式

采用ContentPresenter切换ViewContent/EditContent的方案。

**结构设计：**
```
BaseDetailContainer (UserControl)
├── Header区域
│   ├── 标题 (Title属性)
│   ├── 模式指示器 (查看/编辑)
│   └── 操作按钮区域 (ActionButtons ContentPresenter)
├── Content区域
│   ├── ViewPanel (ViewContent ContentPresenter, IsEditMode=False时可见)
│   └── EditPanel (EditContent ContentPresenter, IsEditMode=True时可见)
└── Footer区域
    └── 保存/取消按钮 (仅编辑模式显示)
```

**Alternatives considered:**
1. DataTemplateSelector - 需要为每个实体写两套DataTemplate，代码量翻倍
2. Visibility切换 - 两组控件都存在于可视树，性能略差
3. 自定义FormField控件 - 需要开发多种类型控件（Text, ComboBox等）

**选择理由：** 容器+面板模式完全分离View/Edit，设计自由度最高，且符合用户"容器"概念。

### Decision 2: 辅助控件设计

**InfoCard控件（查看模式）：**
- 卡片式布局，带标题和内容区域
- 支持多列网格布局
- 用于展示只读信息组

**FormField控件（编辑模式，可选）：**
- 标签+输入控件组合
- 支持IsRequired标记
- 支持验证错误提示

### Decision 3: 渐进式迁移

选择HerbDetailView作为试点（字段最简单），验证后再迁移其他页面。

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|---------|
| 初期工作量较大 | 渐进式迁移，先完成一个验证效果 |
| 复杂字段处理 | 药材列表、处方组成等保持现有控件 |
| 数据绑定重复 | ViewContent/EditContent绑定同一ViewModel |

## Migration Plan

1. **Phase 1**: 开发BaseDetailContainer基础组件
2. **Phase 2**: 开发InfoCard辅助控件
3. **Phase 3**: 迁移HerbDetailView（试点）
4. **Phase 4**: 验证效果，收集反馈
5. **Phase 5**: 迁移剩余4个DetailView
6. **Phase 6**: 优化统一样式

**Rollback:** 如果效果不理想，可保留现有IsReadOnly方案作为备选。

## Open Questions

1. 是否需要FormField控件，还是直接在EditContent中手动布局？
2. 复杂字段（药材列表DataGrid）是否需要特殊处理？
3. 是否需要支持第三种模式（如打印预览模式）？
