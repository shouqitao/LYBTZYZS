# 桌面端角色流程需求补充

> 更新时间：2025-09-24

## 角色与默认入口
- 系统角色字段：`Role`，当前定义包括 `sysadmin`（超级管理员）、`admin`（预留）、`doctor`（默认业务角色）。
- 登录行为：
  - 系统检测 `Username == sysadmin` 或 `Role == Admin` 时，进入管理员工作台 `SystemWorkbenchMainView`。
  - 其他用户进入诊疗工作台 `MedicalWorkbenchMainView`。
- 未来当诊所规模扩大，可新增前台/药房等角色；需在 `WorkbenchRouter` 中维护角色→工作台映射。

## 登录与导航流程
1. 应用启动 -> `LoginRegion` 显示 `LoginView`。
2. 登录成功 -> `MainWindowViewModel` 根据角色选择工作台视图；在 `ContentRegion` 加载对应 Workbench。
3. 工作台内部导航：
   - 管理员：提供系统管理、用户管理、药材管理、经验方管理等模块导航。
   - 医生：默认打开诊疗流程视图；可在工作台内切换到管理 / 历史查询等模块，无需返回主界面。

## 诊疗（Consultation）页面术语调整
- “看诊”与“诊断”含义不匹配，应统一为“诊疗流程”或拆分为“诊断”、“治疗”等环节。
- `Consultation` 术语用于表示诊疗流程中的一个环节（诊断/处方），不要扩大为整个工作台。
- `MedicalWorkbenchMainView` 建议后续重命名为“诊疗工作台”，内部再划分子视图：
  1. 患者选择
  2. 四诊录入
  3. 诊断结果
  4. 处方编辑
  5. 历史/管理入口

## 事件与状态同步
- 统一使用单一事件定义（参考 `Core/Events/UnifiedEvents.cs` 重构后版本）。
- `SessionManager` 负责发布登录变化、患者变化、诊疗状态变化等事件；UI 层订阅后更新视图。
- Status / Message 事件统一使用 `StatusMessageType` 枚举，避免 `MessageType` 与 `StatusMessageType` 并存。

## 下一步优化摘要
1. 合并事件定义，解决当前编译冲突，并减少多源事件带来的维护成本。
2. 调整命名，将“MedicalWorkbench”定位为“诊疗工作台”，并确保 `Consultation` 指向诊疗流程本身。
3. 收敛对话框与导航服务的实现，充分利用 Prism 原生机制。
4. 补齐桌面端测试计划，覆盖 Session 管理、导航流程、关键 ViewModel 命令。
