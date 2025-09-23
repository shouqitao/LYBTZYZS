# 下一阶段优化摘要（2025-09-23）

## 术语与界面一致性
- 将前端“看诊”相关文案统一更新为“诊疗”，避免与“诊断模块”混淆。
- 检查导航项、工作台标题、快捷命令等字符串资源，确保与 `docs/requirements/ui-workflow-spec.md` 一致。
- 若使用资源字典或常量（如 `NavigationItem`、`RegionNames`），同步调整命名避免旧词残留。
- 现有 `MedicalWorkbench*`、`Consultation*ViewModel` 等仅用于诊疗入口，需在命名重构时替换为 `Diagnosis`/`Treatment` 等更准确语义，防止与“诊断（ConsultationService）”概念冲突。

## 角色与权限
- 后端 `User.Role` 字段已经存在：保持 `sysadmin` 视为超级管理员，其余默认医生，后续扩展时再细化。
- 记录当前策略，并在权限服务 (`PermissionService` / `WorkbenchRouter`) 中留下注释，方便未来接入更多角色。

## 实施建议
- 在进行批量替换前先梳理受影响的视图、ViewModel 和文档，建立待改动清单；完成后运行 UI 冒烟测试确保导航正常。
- 对外沟通时使用“诊疗”描述医生主界面，以免用户误解“诊断=全流程”。
