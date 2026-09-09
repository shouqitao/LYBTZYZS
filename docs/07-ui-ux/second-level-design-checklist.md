# Desktop 二级界面设计清单

> **日期**: 2026-08-22 | **基准**: desktop-design-tokens.md
> **范围**: 一级页面（19 个已完成）+ 二级界面 + 需求驱动新增页面

---

## A. 缺失的一级页面（待 delegate 确认后生成）

| # | 页面 | 代码状态 | 需求来源 | 设计稿 |
|---|------|---------|---------|--------|
| A1 | 患者选择 PatientSelectionView | ✅ 已注册使用 | US-REG-001/002 | ❌ 待生成 |
| A2 | 待诊队列 PendingQueueView | ❌ **死代码已确认并删除（2026-08-29）**——队列 UI 内嵌 PatientSelectionView，`PendingQueueViewModel` 保留 | US-REG-004/005 | 不需要独立设计稿 |
| A3 | 临床首页 ClinicalHomeView | ⚠️ **半死已确认（2026-08-29）**——仅 `RoleRegistry.DefaultHomeView` fallback；Doctor 真实 Home=`ClinicalWorkspaceView` | US-SHELL-003 | 低优先级（fallback） |
| A4 | 前台首页 ReceptionistHomeView | ✅ 存在 | US-SHELL-003 | ❌ 待生成 |
| A5 | 审计日志 AuditLogView | ✅ 存在（医案域，非安全审计） | US-MC-017 | ❌ 待生成 |

> 审计报告：`docs/compose/reports/desktop-view-design-audit-2026-08-29.md`

## B. 对话框二级界面（代码已存在，必须设计）

| # | 界面 | 尺寸建议 | 内容 |
|---|------|---------|------|
| B1 | 新建挂号 RegistrationCreateDialog | 560×640 | 患者搜索(autocomplete)+医生选择+挂号类型+费用 |
| B2 | 验方导入 FormulaImportDialog | 560×480 | 选择验方+药材预览+剂量调整 |
| B3 | 历史复制 HistoryCopyDialog | 640×560 | 历史医案列表+预览+复制选项 |
| B4 | 未保存更改 UnsavedChangesDialog | 420×240 | 警告图标+保存/不保存/取消 |
| B5 | 通用对话框（确认/输入/消息合并设计） | 420×280 | 三种形态展示于一图 |

## C. 编辑表单二级界面（代码已存在，必须设计）

| # | 界面 | 形态 | 字段 |
|---|------|------|------|
| C1 | 用户编辑 UserEditControl | 表单页 | 用户名/姓名/角色/邮箱/电话/密码 |
| C2 | 患者编辑 PatientEditControl | 表单页 | 姓名/性别/年龄/电话/身份证/地址/过敏史 |
| C3 | 药材编辑 HerbEditControl | 表单页 | 名称/拼音/性味/归经/功效/价格/库存 |
| C4 | 验方编辑 FormulaEditControl | 复合表单 | 名称/适应症+药材组成列表(可增删行)+用法用量 |
| C5 | 医案编辑 MedicalCaseEditControl | 工作台子页 | 诊断信息+处方编辑 |

## D. 需求驱动但代码未创建的页面

| # | 页面 | 需求来源 | 状态 |
|---|------|---------|------|
| D1 | 安全审计日志查看页 | US-SHELL-014 | 代码缺失，需先补代码再设计？→ 直接出设计稿指导开发 |
| D2 | 数据导入导出页（JSON） | US-SHELL-016 / 已定案 JSON 格式 | 同上 |
| D3 | 读卡器诊断面板 | US-CARD 系列（CardReaderDiagnosticsViewModel 已存在） | VM 有、View 无 → 出设计稿 |

## E. 详情查看类（并入主从布局详情面板，不单独出稿）

PatientViewControl / HerbViewControl / FormulaViewControl / UserViewControl
→ 这些渲染在 MasterDetail 右侧详情面板中，已包含在列表页设计稿内。
→ 如需独立展示形态，后续补充。

---

## 执行顺序

1. B 类对话框 ×5（立即开始——代码已存在，优先级最高）
2. C 类编辑表单 ×5（紧随其后）
3. A 类一级页面 ×3~5（等 delegate 确认结果）
4. D 类需求页面 ×3（直接出设计稿作为开发依据）

预计总量：16 个新设计稿，串行约 3 小时
