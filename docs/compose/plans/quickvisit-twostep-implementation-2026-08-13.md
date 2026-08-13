# 任务：QuickVisit 两步改造实施（删除 quick-visit 端点）

## 背景（2026-08-13 产品决策定案）
QuickVisit 从「一步原子（InProgress+医案）」改为「两步」：
1. POST /Registrations（建 Waiting 挂号，Source=Doctor，doctorId=当前医生）
2. PUT /Registrations/{id}/start-visit（接诊：Waiting→InProgress + 原子建医案）

**决策要点**（用户确认）：
- API 单一职能（组合在前端 VM）
- 医生本来就可挂号（无需权限扩展）
- 「快速看诊」= 医生高效建号（doctorId 强制=当前医生）
- 「挂号」= 正常挂号单逻辑（可指定医生，UI 设计时深入）
- InProgress 后置（断网残留 Waiting 可被待诊列表捕捉 → 自愈）
- **旧 quick-visit 端点删除**（两步收敛后不再需要）

## 实施要求

### 1. 删除 quick-visit 端点（Server）
- 删除 `QuickVisitCommand` / `QuickVisitCommandHandler` / `QuickVisitCommandValidator`（src/Server/Modules/LYBT.Module.Registration/Application/Commands/ + Validators/）
- 删除 Controller 的 quick-visit 端点（RegistrationsController.cs）
- 删除 QuickVisitInputDto / QuickVisitResultDto（如存在且无其他引用）
- 检查 README 模块依赖说明（src/Server/Modules/LYBT.Module.MedicalCases/README.md:121 提到 QuickVisit）

### 2. 删除 Desktop QuickVisit 接线
- 删除 QuickVisitDialog + QuickVisit 相关 VM 方法（RegistrationListViewModel.cs 等）
- 改为前端 VM 两步编排（建挂号 → start-visit）——**先建一个 QuickVisitTwoStepFlow 占位或说明**（UI 设计深入阶段再实现，本次删除死代码即可）

### 3. 确认 POST /Registrations 支持 Source=Doctor
- CreateRegistrationCommandHandler 是否已支持医生建号（Source=Doctor）？
- 若否：补 Source 参数支持（doctorId=当前医生校验：Doctor 角色强制当前医生）
- 权限：POST /Registrations 应含 Doctor（医生本来就可挂号——用户确认）

### 4. 文档同步（代码-文档一致性红线）
- 08-registration.md（已更新 US-REG-002——已由 Hermes 完成）
- 07-medical-cases.md BR-000（已更新——Hermes 完成）
- 13-traceability-matrix.md：US-REG-002 实现参考从 `POST /Registrations/quick-visit` 改为两步（POST /Registrations + PUT /start-visit）
- 03-users.md 权限决策段若提到 quick-visit 需同步（确认）
- README 模块依赖说明

## 验证
- build 0 错误 0 警告 + Server 全量 + 架构 87/87
- grep 确认无 quick-visit 残留（代码 + 文档——除历史变更记录）
- 测试：Doctor 建 Waiting 挂号（Source=Doctor）+ start-visit 接诊成功
- commit + push

## 注意
- 删除是「外科手术式」——只删 quick-visit 相关，不动其他
- 历史变更记录保留（不可改历史）
- 完成后删除任务书
