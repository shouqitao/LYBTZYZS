# OpenSpec Proposal: 重构基础数据模块为Master-Detail布局

## Status: Approved
## Created: 2025-12-16
## Author: Claude Code

---

## 1. Why (问题与动机)

### 1.1 当前问题

1. **空间利用率低**: 详情页整个界面只显示少量信息，大量空白浪费屏幕空间
2. **操作路径长**: 查看详情需要跳转页面，返回列表又需要再次跳转
3. **上下文丢失**: 离开列表后无法同时看到其他记录，难以快速对比
4. **用户体验割裂**: 列表→详情→编辑的导航链路不够流畅

### 1.2 目标

- 采用 Master-Detail 布局模式，左侧列表 + 右侧详情/编辑
- 提升空间利用率和操作效率
- 保持上下文，减少页面跳转
- 参考 Microsoft Fluent Design 和行业优秀实践

### 1.3 范围

**包含的模块:**
- 患者管理 (Patients)
- 用户管理 (Users)
- 药材管理 (Herbs)
- 验方管理 (Formula)

**排除的模块:**
- 医案管理 (MedicalCase) - 已有工作区设计，内容复杂（诊疗+处方）

---

## 2. What Changes (变更内容)

### 2.1 架构变更

| 组件 | 当前实现 | 目标实现 |
|------|----------|----------|
| 列表视图 | 独立页面 (BaseMasterDataListView) | Master-Detail左侧面板 |
| 详情视图 | 独立页面 (BaseDetailContainer) | Master-Detail右侧面板 |
| 导航方式 | Prism Region跳转 | 同页面内容切换 |
| 编辑模式 | 详情页内切换 | 右侧面板内切换 |

### 2.2 新增组件

1. **MasterDetailLayout** - 通用Master-Detail容器控件
2. **IMasterDetailViewModel** - 统一的ViewModel接口
3. **MasterDetailNavigationService** - 列表-详情同步服务

### 2.3 复用组件

- PatientViewControl, PatientEditControl
- UserViewControl, UserEditControl
- HerbViewControl, HerbEditControl
- FormulaViewControl, FormulaEditControl

---

## 3. Impact (影响评估)

### 3.1 代码影响

| 类型 | 文件数 | 说明 |
|------|--------|------|
| 新增 | ~8 | MasterDetailLayout + 4模块适配 |
| 修改 | ~12 | 现有View/ViewModel重构 |
| 删除 | ~4 | 独立详情页面 |

### 3.2 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 导航逻辑变更 | 中 | 渐进式重构，逐模块迁移 |
| 现有功能回归 | 低 | 复用已测试的ViewControl |
| 响应式布局 | 低 | 参考Microsoft ListDetailsView模式 |

### 3.3 测试策略

- 单元测试: ViewModel逻辑
- 集成测试: 导航和数据同步
- 手动测试: UI布局和交互

---

## 4. References (参考资料)

### 4.1 Microsoft官方指南
- [List/Details Pattern](https://learn.microsoft.com/en-us/windows/apps/design/controls/list-details)
- [Fluent Design Navigation](https://learn.microsoft.com/en-us/windows/apps/design/basics/navigation-basics)

### 4.2 行业最佳实践
- Windows Community Toolkit ListDetailsView
- Healthcare UI设计原则：可扫描列表、清晰操作、上下文感知

### 4.3 设计原则
- 40:60比例分割（列表:详情）
- 响应式设计支持窄屏堆叠
- 选中状态高亮
- 平滑过渡动画
