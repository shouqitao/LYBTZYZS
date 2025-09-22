# 阶段 B：通知与加载统一 PRD

## 目标
- 统一通知/对话服务的对外接口与体验；减少重复实现。
- 引入并推广统一的智能加载管理（ISmartLoadingManager）和 UI 指示器，替代分散的 IsLoading 手工逻辑。

## 范围
- In Scope：`IUserNotificationService`/`ICustomDialogService` 与实现；`ISmartLoadingManager`/`SmartLoadingIndicator`；关键 VM 的加载调用替换；资源中的图标/颜色/时长配置。
- Out of Scope：大规模 UI 视觉重构；后端接口与数据流程。

## 交付物
- 标准化通知服务：信息/成功/警告/错误/确认/输入等能力与统一配置（`NotificationConfiguration`）。
- 智能加载：容器注册、扩展方法封装（`SmartLoadingExtensions`）、关键 VM 改造样例（至少 3 处）。

## 验收标准
- 关键操作（登录、加载列表、保存/提交）均可见统一 Loading 行为（可取消/可展示进度的场景具备）。
- 通知/对话行为统一（标题、图标、自动关闭时长可配置）。
- 编译通过与基础回归用例通过。

## 里程碑
1. 容器注册：在 `Shell/Extensions/ServiceCollectionExtensions.cs` 注册 `ISmartLoadingManager -> SmartLoadingManager`（Singleton）；确保 `IUserNotificationService` 唯一对外实现。
2. VM 落地：挑选 3 个关键 VM（如 LoginViewModel、MedicalCaseManagementViewModel、PrescriptionsMainViewModel）改造为使用 `ExecuteWithLoadingAsync`。
3. 指示器接入：在主布局或模块页引入 `SmartLoadingIndicator`，绑定 LoadingManager 指示状态。
4. 通知收敛：统一走 `IUserNotificationService`，去除 MessageBox 直呼。

## 风险与缓解
- 风险：改造触及 VM 行为，可能影响交互节奏。缓解：先抽样 3 个场景，验证通过后逐步推广。
- 风险：与现有 IsLoading 属性并存导致显示冲突。缓解：改造阶段由 LoadingManager 驱动 IsLoading，同步状态，逐步移除直接赋值。

## 依赖
- 已存在的 `SmartLoadingExtensions` 与 `SmartLoadingIndicator`。
- 已补齐的 `NotificationConfiguration`。

## 回滚方案
- 保留原 IsLoading 逻辑开关（编译期 #if 或配置开关）；出现问题时关闭新 LoadingManager 路径。

## 度量
- 关键 VM 中对 `IsLoading = true/false` 的直接赋值下降 ≥ 70%。
- 直接 `MessageBox.Show` 调用下降至 0。

## 测试计划
- 构建/格式化：同阶段 A。
- 手动用例：
  - 登录失败/成功的通知是否一致、时长正确。
  - 大列表加载/批处理是否显示进度，取消是否生效。
  - 保存/提交后的成功/错误提示关闭策略正确。

## 受影响文件（示例）
- `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Client/Desktop/Core/Services/UserNotificationService.cs`
- `src/Client/Desktop/Core/Services/WpfDialogService.cs`
- `src/Client/Desktop/Core/Controls/SmartLoadingIndicator.xaml(.cs)`
- 各关键 VM（Login/MedicalCase/Prescriptions 等）

