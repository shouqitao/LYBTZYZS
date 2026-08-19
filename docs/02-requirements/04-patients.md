# 患者管理 (Patient Management)

> 患者管理模块负责维护诊所所有患者的个人信息与医疗档案基础数据，是医案（MedicalCase）的前置依赖，也是挂号、处方、打印等业务流程的起点。模块通过 `[SensitiveData]` 特性对敏感字段实施差异化脱敏，保障医疗数据合规。
>
> **权限权威**：完整权限矩阵（含删除/禁用/恢复等操作级细分）以 [04-permissions.md](../01-product/04-permissions.md) 为准，本模块 US 中策略名为摘要。
>
> | 我是… | 我能… |
> | ------- | ------- |
> | 前台 | 登记新患者、修改信息、按身份证查人 |
> | 医生 | 查看患者列表、查看详情 |
> | 管理员 | 全部操作：增删改、批量导入、启用/禁用 |

---

## US-PAT-001: 分页查询患者列表

**角色**: 前台/医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 分页查询患者列表并支持关键字与拼音搜索，**以便** 快速定位患者档案。

**验收标准**:

- [ ] 支持分页参数（pageIndex、pageSize）
- [ ] 支持按姓名、电话、拼音首字母筛选
- [ ] 返回总数与分页数据
- [ ] 非管理员仅返回 `IsEnabled=true` 的患者
- [ ] 所有 DoctorOrReceptionist 角色均可访问（含 Receptionist）

**业务规则**:

1. 端点受 `DoctorOrReceptionist` 策略保护（Receptionist/Doctor/Admin/SuperAdmin）
2. 拼音搜索基于 `PinyinAbbreviation`（如 "dg" 匹配 "张三" 等拼音首字母为 ZS 的患者——注：实际为姓名拼音首字母）
3. 非管理员可见性由全局查询过滤器 + 角色判断联合实现
4. **非管理员可见性（模块规则）**：Doctor/Receptionist 仅可见 `IsEnabled=true` 的患者；Admin/SuperAdmin 可见全部（含禁用）

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:38` (HttpGet list), `IPatientService`

---

## US-PAT-002: 查看患者详情

**角色**: 前台/医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 查看指定患者的完整档案，**以便** 接诊前了解患者基本信息与病史。

**验收标准**:

- [ ] 通过患者 ID 查询
- [ ] 返回完整患者资料（敏感字段按脱敏规则返回）
- [ ] 不存在的 ID 返回 404
- [ ] 非管理员查询禁用患者返回 404

**业务规则**:

1. 敏感字段（IdCardNumber/PhoneNumber/Address/AllergyHistory/MedicalHistory）按 `[SensitiveData]` 规则脱敏后返回
2. 非管理员访问禁用患者视为不存在（404）

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:65` (HttpGet `{id:guid}`), `IPatientService`

---

## US-PAT-003: 创建患者

**角色**: 前台/医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现（电话唯一查重 409 + 拼音自动生成兜底）

**作为** 诊所工作人员，**我想要** 创建新的患者档案，**以便** 为新就诊患者建立基础记录。

**验收标准**:

- [ ] 接受姓名、性别、出生日期、电话等字段
- [ ] 电话号码唯一，重复返回 409
- [ ] 自动生成拼音首字母（PinyinAbbreviation）
- [ ] 由 BirthDate 自动计算 Age（控制器手动赋值）
- [ ] 创建成功返回新患者资料（含 ID）

**业务规则**:

1. 电话唯一约束由业务级查重强制（**修正 2026-08-13**：原「数据库索引强制」不实——Patient 无 DB 唯一索引，7caa27e41 确认软删实体不设 DB 唯一索引，业务级查重已友好）
2. 拼音由拼音服务自动生成，无需客户端提供
3. 年龄字段为计算字段，Mapperly 忽略，由控制器手动赋值
4. 新患者默认 `IsEnabled=true`、`IsDeleted=false`

**边界条件**:

- 导入行电话号码与系统已有患者重复 → 该行跳过（DuplicateStrategy=Skip），其余行继续
- 导入行电话号码为空 → 返回 400 校验错误（PhoneNumber 为必填）

**实现注**:

- **2026-08-13 PATIENT-PHONE-UNIQUE-FIX**：真机发现同电话可重复创建——查重逻辑存在但错误码用 PatientNotFound（404 语义），且失败走 BusinessFail 恒 422，需求要求 409。修复：Handler 改 `PatientPhoneDuplicate`（ErrorCodeExtensions 400→409）；BatchImport 补电话查重（行内互查 + 与系统已有患者）；拼音码服务端自动生成兜底（对齐药材 B-03 先例——API 直调未传 PinYinCode 时按姓名生成）。
- **2026-08-13 PATIENT-PHONE-409-FIX**：ErrorCodeExtensions 已映射 409 但真机仍 422——根因：`HandleResult` 只认 `Result.ModuleErrorCode`，而 `Result.Failure(ErrorCode)` 不设 ModuleErrorCode → 落 BusinessFail 恒 422。修复：`HandleResult` 在 ModuleErrorCode 为空时**回退 ErrorCode 映射**（`code.ToHttpStatusCode()`）——电话唯一 → 真 409；双端 PatientsController 创建/更新/批量导入失败分支改用 `HandleResult(useAuthMapping: true)`。同类检查：IdentityController 已手写 `ErrorCode.ToHttpStatusCode()` 正确；409 错误码（MedicalCaseLocked 等）代码零消费无路径可测；`HandleResult` 回退使未来 403/404/409 全部按错误码正确映射。

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:89` (HttpPost create), `IPatientService`

---

## US-PAT-004: 更新患者

**角色**: 前台/医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现（更新电话唯一查重）

**作为** 诊所工作人员，**我想要** 更新患者档案信息，**以便** 修正错误或补充最新信息。

**验收标准**:

- [ ] 接受可修改字段
- [ ] 电话号码变更后仍需唯一
- [ ] 姓名变更触发拼音重新生成
- [ ] 出生日期变更触发年龄重新计算
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 更新时重新生成 PinyinAbbreviation（姓名可能变更）
2. 更新时重新计算 Age（BirthDate 可能变更）
3. 电话唯一约束同样适用于更新

**边界条件**:

- 两个管理员同时编辑同一患者 → 后提交者覆盖先提交者（Last Write Wins，乐观锁 RowVersion 检测冲突时返回 409）
- 编辑过程中患者被其他用户禁用 → 保存成功（禁用不影响编辑权限，仅影响可见性）
- 编辑过程中患者被软删除 → 返回 404

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:114` (HttpPut `{id:guid}`), `IPatientService`

---

## US-PAT-005: 删除患者（软删除，引用检查）

**角色**: 管理员（Admin 及以上）
**优先级**: Must
**状态**: ✅ 已实现（引用检查 CountMedicalCasesAsync）

**作为** 管理员，**我想要** 软删除患者档案（保留审计数据），**以便** 清理无效记录同时保留历史。

**验收标准**:

- [ ] 通过 ID 软删除（设置 IsDeleted=true）
- [ ] 删除前检查是否被 MedicalCase 引用
- [ ] 被引用时返回 422 Unprocessable Entity，拒绝删除
- [ ] 删除后列表查询自动排除
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 引用检查：查询 MedicalCase 表是否存在该患者的记录
2. 被引用的患者不可删除（保护医案完整性），返回 422
3. 软删除通过全局查询过滤器自动隐藏
4. **软删除（模块规则）**：通过 `IsDeleted` + 全局查询过滤器实现，默认隐藏已删除患者

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:146` (HttpDelete `{id:guid}`), `IPatientService`

---

## US-PAT-006: 启用/禁用患者

**角色**: 管理员（Admin 及以上）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 启用或禁用患者档案，**以便** 暂时屏蔽无效或敏感患者记录。

**验收标准**:

- [ ] 切换患者的 IsEnabled 状态
- [ ] 禁用后非管理员查询不可见
- [ ] 禁用不影响已存在的医案（医案仍可访问）
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 禁用与软删除语义不同：禁用保留记录但对非管理员隐藏；软删除则视为已移除
2. 禁用不影响历史医案的访问（医生仍需查看已接诊患者的历史）

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:175` (HttpPost `{id:guid}/toggle-status`), `IPatientService`

---

## US-PAT-007: 恢复软删除患者

**角色**: 管理员（Admin，业务管理）
**优先级**: Should
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 恢复被软删除的患者档案，**以便** 在误删或患者复诊时还原记录。

**验收标准**:

- [ ] 通过 ID 恢复（设置 IsDeleted=false）
- [ ] 恢复后所有角色可见（依启用状态）
- [ ] 使用 `IgnoreQueryFilters` 查询已软删除记录
- [ ] 仅 Admin 可执行（sysadmin 系统运维不碰业务）

**业务规则**:

1. 恢复操作需 `IgnoreQueryFilters()` 绕过全局软删除过滤器定位记录
2. 恢复仅还原患者记录本身，不还原关联医案（医案删除独立）

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:197` (HttpPost `{id:guid}/restore`), `IPatientService`

---

## US-PAT-008: 批量删除患者

**角色**: 管理员（Admin 及以上）
**优先级**: Should
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 批量软删除多个患者，**以便** 高效清理大批量无效记录。

**验收标准**:

- [ ] 接受患者 ID 列表
- [ ] 逐项执行引用检查，被引用的跳过并记录
- [ ] 返回成功与失败（含原因）的明细
- [ ] 不存在的 ID 计入失败明细

**业务规则**:

1. 批量删除采用逐项处理（非原子），允许部分失败
2. 每项均执行引用检查（同 US-PAT-005 规则）
3. 端点受管理员权限保护

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:223` (HttpPost `batch-delete`), `IPatientService`

---

## US-PAT-009: 单个引用检查

**角色**: 前台/医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 在删除前检查患者是否被医案引用，**以便** 预判删除是否可行。

**验收标准**:

- [ ] 通过患者 ID 查询引用状态
- [ ] 返回是否被引用（HasReference）及引用计数
- [ ] 引用明细（可选）：关联的医案 ID 列表
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 引用检查查询 MedicalCase 表中该患者的记录数
2. 此端点允许所有 DoctorOrReceptionist 角色查询（前台/医生也需预判）

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:248` (HttpGet `{id:guid}/check-reference`), `IPatientService`

---

## US-PAT-010: 批量引用检查

**角色**: 管理员（Admin 及以上）
**优先级**: Should
**状态**: ✅ 已实现（批量计数一次查询）

**作为** 管理员，**我想要** 批量检查多个患者的引用状态，**以便** 批量删除前快速识别可删除项。

**验收标准**:

- [ ] 接受患者 ID 列表
- [ ] 返回每个患者的引用状态与计数
- [ ] 高效查询（避免 N+1）
- [ ] 不存在的 ID 标记为无效

**业务规则**:

1. 批量引用检查通过单次聚合查询实现（避免逐项 N+1）
2. 用于批量删除前的预检，帮助管理员筛选

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:267` (HttpPost `batch-check-reference`), `IPatientService`

---

## US-PAT-011: 下载导入模板

**角色**: 管理员（Admin 及以上）
**优先级**: Should
**状态**: ✅ 已实现（2026-08-13 起 JSON 格式；2026-08-19 Desktop 模板下载/导入 UI 接线完成）

**作为** 管理员，**我想要** 获取患者导入 JSON 模板说明，**以便** 按规范格式批量准备患者数据。

**验收标准**:

- [ ] 返回 JSON 模板（字段说明 + 示例 + 必填标注）——`application/json`
- [ ] 模板包含所有可导入字段
- [ ] 标注必填字段

**业务规则**:

1. 模板由 Service 层生成（JSON 结构，非 Excel）
2. 模板字段与导入端点期望的 DTO 一致（`batch-import` 收 JSON 数组）
3. **后端不涉及 Excel 格式**（2026-08-13 决策：保持通用性——Excel 处理由前端负责，如需）

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs` (HttpGet `import-template`), `IPatientService`

---

## US-PAT-012: 导出患者数据（JSON）

**角色**: 管理员（Admin 及以上）
**优先级**: Should
**状态**: ✅ 已实现（2026-08-13 起 JSON 格式；2026-08-19 Desktop 导出 UI 接线完成）

**作为** 管理员，**我想要** 将患者数据导出为 JSON，**以便** 数据备份、外部审计或迁移。

**验收标准**:

- [ ] 支持按筛选条件导出（非全量）
- [ ] 返回 JSON 数组（`application/json`）
- [ ] 敏感字段按脱敏规则导出
- [ ] 大数据量导出不影响主业务性能

**业务规则**:

1. 导出由 Service 层执行（返回 JSON 数据，非 Excel）
2. 敏感字段即使导出也按脱敏规则处理
3. 端点受管理员权限保护
4. **后端不涉及 Excel 格式**（2026-08-13 决策：保持通用性——Excel 转换由前端负责，如需）

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `PatientsController.cs:313` (HttpGet `export`), `IPatientService`

---

## US-PAT-013: 敏感数据脱敏（存储与传输）

**角色**: 系统安全
**优先级**: Must
**状态**: ✅ 已实现（DTO 加 [SensitiveData]，序列化管道掩码）

**作为** 系统安全机制，**我想要** 对患者敏感字段实施差异化脱敏，**以便** 满足医疗数据合规要求并降低泄露风险。

**验收标准**:

- [ ] `IdCardNumber`、`PhoneNumber` 采用 Partial 脱敏（保留首尾，中间掩码）
- [ ] `Address` 采用 Default 脱敏
- [ ] `AllergyHistory`、`MedicalHistory` 采用 Hash 脱敏（仅可比对，不可还原）
- [ ] 脱敏在序列化阶段统一应用（`[SensitiveData]` 特性）
- [ ] 存储层保留原始数据（仅传输层脱敏）

**业务规则**:

1. `[SensitiveData]` 特性声明脱敏策略，由统一序列化管道执行
2. Partial 脱敏：身份证保留前 4 后 4，电话保留前 3 后 4
3. Hash 脱敏：用于过敏史/病史比对（如确认两记录是否相同），不还原文
4. 数据库存储原始数据，脱敏仅发生在 API 响应阶段

**双模式差异**:

| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一脱敏管道） |

**实现参考**: `PatientsController.cs`（全端点），`[SensitiveData]` 特性，脱敏序列化管道

---

## US-PAT-014: 按身份证号查询患者

**角色**: 医生 / 前台 / Admin
**优先级**: Must
**状态**: ✅ 已实现（未文档化补记——R1 反向脱节）

**作为** 医生或前台，**我想要** 输入患者身份证号直接查询患者，**以便** 读卡建档时快速定位已有患者（US-CARD-001 读卡链路依赖）。

**验收标准**:

- [ ] GET `/api/v1/patients/by-id-number/{idNumber}` 返回匹配患者详情
- [ ] 未找到返回 404

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:296`（`by-id-number/{idNumber}`）、`PatientService.GetByIdNumberAsync`、Desktop `PatientCardReaderIntegration.FindPatientByIdNumberAsync`

---

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-08-11 | US-PAT-014 补记（身份证号查询——代码已实现未文档化） | R3-补 反向脱节收编 |
| 2026-06-25 | 补充导入电话唯一性、并发编辑边界条件验收标准 | 需求文档验收标准完善 |
