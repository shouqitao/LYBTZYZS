# medicalcase-api Specification Delta

## Purpose
优化医案API设计，实现聚合保存、端点合并、统一查询和设计决策(Q1-Q5)。

---

## ADDED Requirements

### Requirement: API-001 CloseCaseAsync返回完整数据
系统 **SHALL** 在关闭医案时返回完整的MedicalCaseDetailDto，而非空响应。

#### Scenario: 关闭医案返回详情
- **GIVEN** 医案状态为Active或Draft
- **WHEN** 调用 `PUT /api/v1/medicalcases/{id}/close`
- **THEN** 返回200 OK
- **AND** 响应体包含完整的MedicalCaseDetailDto
- **AND** 包含更新后的Status、CompletedAt等字段
- **AND** Client可直接使用返回数据更新UI

#### Scenario: 避免额外查询
- **GIVEN** Client调用CloseCaseAsync
- **WHEN** API返回成功
- **THEN** Client不需要再次调用GetById
- **AND** 减少一次网络往返

---

### Requirement: API-002 GetById端点支持includeDetails参数
系统 **SHALL** 将GetById和GetByIdWithDetails合并，通过查询参数控制返回详细程度。

#### Scenario: 默认包含详情
- **GIVEN** 医案ID存在
- **WHEN** 调用 `GET /api/v1/medicalcases/{id}`
- **THEN** 返回完整的MedicalCaseDetailDto
- **AND** 包含Consultation和Prescription详情

#### Scenario: 显式排除详情
- **GIVEN** 医案ID存在
- **WHEN** 调用 `GET /api/v1/medicalcases/{id}?includeDetails=false`
- **THEN** 返回基础的MedicalCaseDto
- **AND** 不包含Consultation和Prescription详情
- **AND** 响应体更小，查询更快

#### Scenario: 旧端点标记废弃
- **GIVEN** 存在GetByIdWithDetails端点
- **WHEN** 代码编译
- **THEN** 该端点标记[Obsolete]
- **AND** 提示使用GetById with includeDetails=true

---

### Requirement: API-003 统一查询端点
系统 **SHALL** 提供统一的医案列表查询端点，通过QueryType参数分发不同查询逻辑。

#### Scenario: 按QueryType分发
- **GIVEN** 调用 `GET /api/v1/medicalcases`
- **WHEN** 指定QueryType参数
- **THEN** 系统根据QueryType执行对应查询：
  - All: 分页列表
  - ByPatient: 按患者ID
  - Pending: 待看诊队列
  - Unfinished: 未完成医案
  - Recent: 最近处方参考

#### Scenario: 默认查询类型
- **GIVEN** 调用 `GET /api/v1/medicalcases`
- **WHEN** 未指定QueryType
- **THEN** 默认使用QueryType.All
- **AND** 返回分页医案列表

#### Scenario: 旧端点标记废弃
- **GIVEN** 存在GetByPatientId、GetPending等独立端点
- **WHEN** 代码编译
- **THEN** 这些端点标记[Obsolete]
- **AND** 提示使用统一的GetMedicalCases端点

---

### Requirement: API-004 处方Items全量替换策略
系统 **SHALL** 在更新处方Items时采用全量替换策略，而非增量更新。

#### Scenario: 全量替换执行
- **GIVEN** 处方存在若干Items
- **WHEN** 调用保存API提交新的Items列表
- **THEN** 系统删除该处方所有现有Items
- **AND** 添加提交的新Items
- **AND** 操作在单一事务中完成

#### Scenario: 清空Items
- **GIVEN** 处方存在若干Items
- **WHEN** 调用保存API提交空Items列表
- **THEN** 系统删除所有现有Items
- **AND** 处方变为无药材状态

#### Scenario: 事务原子性
- **GIVEN** 正在执行Items替换
- **WHEN** 添加新Items时发生错误
- **THEN** 回滚删除操作
- **AND** 原有Items保持不变
- **AND** 返回错误响应

---

### Requirement: API-005 处方生命周期管理
系统 **SHALL** 根据Items是否存在自动管理Prescription的创建与删除。

#### Scenario: 首次添加Item时创建处方
- **GIVEN** 医案不存在Prescription记录
- **WHEN** 用户添加第一个药材Item并保存
- **THEN** 系统自动创建Prescription记录
- **AND** 将Item关联到新创建的Prescription
- **AND** 返回包含Prescription的MedicalCaseDetailDto

#### Scenario: 移除所有Items时删除处方
- **GIVEN** 医案存在Prescription且包含Items
- **WHEN** 用户移除所有Items并保存
- **THEN** 系统删除Prescription记录
- **AND** MedicalCase.Prescription变为null
- **AND** Prescription相关字段（Remark、服法等）同时清除

#### Scenario: 空Items不创建处方
- **GIVEN** 新创建的医案没有Prescription
- **WHEN** 保存时Items列表为空
- **THEN** 不创建Prescription记录
- **AND** MedicalCase.Prescription保持null

#### Scenario: 处方存在性判断
- **GIVEN** 查询医案详情
- **WHEN** 返回MedicalCaseDetailDto
- **THEN** 根据`Prescription != null`判断是否有处方
- **AND** 移除对已废弃的NeedsPrescription字段的依赖

#### Scenario: 字段迁移清理
- **GIVEN** 数据库存在NeedsPrescription列
- **WHEN** 执行迁移
- **THEN** 删除该列
- **AND** 现有数据根据Prescription != null确定显示状态

---

### Requirement: API-006 暂存医案机制
系统 **SHALL** 提供暂存医案功能，保存当前诊断和处方数据，状态变为Draft。

#### Scenario: 入口1 - 编辑界面暂存按钮
- **GIVEN** 医案状态为Active（编辑中）
- **WHEN** 用户点击"暂存医案"按钮
- **THEN** 系统保存当前Consultation数据
- **AND** 系统保存当前Prescription Items
- **AND** 医案状态变为Draft
- **AND** 返回更新后的MedicalCaseDetailDto
- **AND** UI从编辑状态切换为查看状态

#### Scenario: 入口2 - 待看诊列表双击Draft医案
- **GIVEN** 待看诊列表存在Draft状态的医案
- **WHEN** 用户双击该Draft医案
- **THEN** 医案状态从Draft变为Active
- **AND** 界面进入编辑模式
- **AND** 恢复之前保存的Consultation和Prescription数据

#### Scenario: 入口3 - 选择患者时发现Draft医案
- **GIVEN** 用户在患者列表选择患者
- **AND** 该患者存在Draft状态的医案
- **WHEN** 用户点击选择该患者
- **THEN** 系统弹出四选对话框

#### Scenario: 入口3四选对话框 - 继续暂存医案
- **GIVEN** 四选对话框显示
- **WHEN** 用户选择"继续暂存医案"
- **THEN** 系统打开该Draft医案
- **AND** 状态变为Active进入编辑模式
- **AND** 不创建新医案

#### Scenario: 入口3四选对话框 - 关闭暂存医案后新建
- **GIVEN** 四选对话框显示
- **WHEN** 用户选择"关闭暂存医案后新建"
- **THEN** 系统将Draft医案状态改为Cancelled
- **AND** 创建新的Active医案
- **AND** 进入新医案编辑界面

#### Scenario: 入口3四选对话框 - 仅关闭暂存医案
- **GIVEN** 四选对话框显示
- **WHEN** 用户选择"仅关闭暂存医案"
- **THEN** 系统将Draft医案状态改为Cancelled
- **AND** 不创建新医案
- **AND** 返回患者列表界面

#### Scenario: 入口3四选对话框 - 取消
- **GIVEN** 四选对话框显示
- **WHEN** 用户选择"取消"
- **THEN** 关闭对话框
- **AND** 不执行任何操作
- **AND** 保持当前界面状态

#### Scenario: 同一患者Draft唯一性
- **GIVEN** 某患者已存在Draft状态医案
- **WHEN** 尝试为该患者创建新医案
- **THEN** 必须先处理现有Draft（继续或关闭）
- **AND** 系统不允许同一患者同时存在多个Draft医案

#### Scenario: Draft永不过期
- **GIVEN** Draft状态的医案
- **WHEN** 经过任意时间
- **THEN** Draft状态保持不变
- **AND** 下次该患者挂号时触发入口3流程

#### Scenario: 查看状态不触发对话框
- **GIVEN** 医案状态为Draft（查看状态）
- **WHEN** 用户离开医案界面
- **THEN** 直接退出，不弹出对话框
- **AND** 不执行额外保存操作

---

## MODIFIED Requirements

### Requirement: LIFECYCLE-007 医案编辑权限控制 (MODIFIED)
系统 **SHALL** 基于用户角色、医案归属（UserId）、医案状态和**操作时间**控制编辑权限。

#### Scenario: 医生只能操作自己的医案
- **GIVEN** 医生A登录系统
- **AND** 存在医生B创建的医案（MedicalCase.UserId != 医生A.Id）
- **WHEN** 医生A尝试编辑该医案
- **THEN** 系统拒绝编辑操作
- **AND** 返回403 Forbidden
- **AND** 提示"无权限操作其他医生的医案"

#### Scenario: 医生当天可修改自己的已完成医案
- **GIVEN** 医生登录系统
- **AND** 存在该医生今天完成(CompletedAt.Date == Today)的医案
- **AND** 医案的UserId等于当前医生Id
- **WHEN** 医生尝试编辑该医案
- **THEN** 系统允许编辑操作

#### Scenario: 医生隔天不可修改已完成医案
- **GIVEN** 医生登录系统
- **AND** 存在该医生昨天完成(CompletedAt.Date < Today)的医案
- **WHEN** 医生尝试编辑该医案
- **THEN** 系统拒绝编辑操作
- **AND** 界面显示只读模式
- **AND** 提示联系管理员修改

#### Scenario: 医生当天可修改自己的已取消医案
- **GIVEN** 医生登录系统
- **AND** 存在该医生今天取消(CancelledAt.Date == Today)的医案
- **AND** 医案的UserId等于当前医生Id
- **WHEN** 医生尝试编辑该医案
- **THEN** 系统允许编辑操作

#### Scenario: 医生可编辑自己的Active/Draft医案
- **GIVEN** 医生登录系统
- **AND** 存在该医生的Active或Draft状态医案
- **AND** 医案的UserId等于当前医生Id
- **WHEN** 医生尝试编辑该医案
- **THEN** 系统允许编辑操作

#### Scenario: 管理员可编辑所有医案（无归属和时间限制）
- **GIVEN** 管理员(Admin或SuperAdmin)登录系统
- **AND** 存在任意医生、任意状态、任意时间的医案
- **WHEN** 管理员尝试编辑该医案
- **THEN** 系统允许编辑操作
- **AND** 界面要求输入修改原因（用于后续审计集成）

#### Scenario: 授权服务权限检查
- **GIVEN** 调用MedicalCase写操作API
- **WHEN** 请求到达Server
- **THEN** 先调用IMedicalCaseAuthorizationService.CanModify
- **AND** 检查UserId归属（医生角色）
- **AND** 检查时间限制（Completed/Cancelled状态）
- **AND** 无权限返回403 Forbidden
- **AND** 有权限继续执行业务逻辑

---

---

### Requirement: API-007 废弃标注与清理策略
系统 **SHALL** 对不符合新设计的旧API和字段标注[Obsolete]，重构完成后统一清理。

#### Scenario: Controller端点标注废弃
- **GIVEN** 存在旧的查询端点（如GetPending, GetByPatientId等）
- **WHEN** 新的统一查询端点实现完成
- **THEN** 旧端点添加`[Obsolete("消息", false)]`特性
- **AND** 消息说明替代方案和移除版本
- **AND** 旧端点仍可调用但产生编译警告

#### Scenario: Service方法标注废弃
- **GIVEN** 存在旧的Service方法
- **WHEN** 新方法实现完成
- **THEN** 旧方法添加`[Obsolete]`特性
- **AND** 内部可选择委托到新方法实现

#### Scenario: DTO字段标注废弃
- **GIVEN** 存在需要移除的DTO字段（如NeedsPrescription）
- **WHEN** 新设计不再需要该字段
- **THEN** 字段添加`[Obsolete]`特性
- **AND** Server端逻辑不再依赖该字段
- **AND** Client端传值被忽略

#### Scenario: 清理阶段执行
- **GIVEN** 所有调用方已迁移到新API
- **AND** 全局搜索确认无废弃方法调用
- **WHEN** 执行Phase 6清理
- **THEN** 删除所有[Obsolete]标注的代码
- **AND** 创建数据库迁移删除废弃列
- **AND** 编译后无Obsolete警告

#### Scenario: 清理前验证
- **GIVEN** 准备执行清理
- **WHEN** 执行清理前验证
- **THEN** 运行全局搜索确认无调用
- **AND** 运行完整测试套件确认通过
- **AND** 验证失败则停止清理流程

---

## Related Capabilities
- `medicalcase-lifecycle` - 医案生命周期管理
- `api-authorization` - API授权控制

> **注**: 审计日志功能由独立模块`create-audit-module`提供，本需求仅实现权限控制
