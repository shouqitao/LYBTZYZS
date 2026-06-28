# ADR-0013: SignalR 实时推送（v1.0 范围决策）

## 状态
Accepted（范围决策）— v1.0 落地「挂号→医生工作台实时推送」这一范围已锁定；**实现细节待后续 SignalR 专项 spec 承载**。

> **范围收敛（2026-06-28，R10 spec S7）**：SignalR 推送**仅远程模式**。本地模式**不部署 SignalR**——本地模式默认医生独立（无前台用户时无待诊队列）；即使 Admin 建了前台用户产生队列，本地也不部署推送（SignalR 仅远程）。

## 上下文
R10 用户访谈反馈：医生在工作台接诊时，需要**实时看到**新挂号进入待诊列表，而非手动刷新或定时轮询。市面医院信息系统（HIS）普遍将「挂号→医生端实时刷新」作为标配体验，用户对此有明确预期。

当前 v1.0 候诊队列依赖 [US-REG-004](../../02-requirements/08-registration.md) 的分页查询接口，医生需主动刷新才能看到新挂号，体验落后于行业基准。

> 注：「实时推送」诉求**仅存在于远程模式**（有前台挂号→有待诊队列→需推送）。本地模式默认医生独立（无前台用户时无队列）；即使建前台用户有队列，本地也不部署 SignalR（见 [R10 spec S3](../../compose/specs/2026-06-28-registration-workflow-redesign.md)）。

## 决策
v1.0 采用 **SignalR** 实现**远程模式**下挂号变更到医生工作台的实时推送：
- 新挂号创建（Source=Receptionist，Status=Waiting）→ 推送给指派医生
- 挂号状态变更（开始就诊 / 取消）→ 推送同步
- 医生工作台待诊列表据此实时刷新（见 [US-REG-008](../../02-requirements/08-registration.md)）
- **本地模式排除**：无队列，不部署 Hub，不做推送

## 理由
- SignalR 是 ASP.NET Core 一等公民，与现有 WebAPI 技术栈无缝集成
- 支持 WebSocket 自动降级（Server-Sent Events / Long Polling），适应诊所网络环境
- 推送通道与 REST API 共享认证、依赖注入管道，复用现有 `ApiResponse<T>` 契约与权限策略
- 参考市面 HIS 标配体验，满足 R10 访谈中医生对实时性的明确诉求

## 后果

### 待专项设计的架构问题（本 ADR 仅锚定范围决策，细节由后续 SignalR 专项 spec 承载）

> **双模式推送机制已收敛（R10 S7）**：原「本地模式如何推送」待决项已关闭——本地模式无队列，**排除在 SignalR 范围外**。专项 spec 仅需设计远程 Hub + 推送粒度 + 降级轮询。

| 议题 | 结论 / 待决项 | 候选方向（非定案） |
|------|--------|-------------------|
| ~~双模式推送机制~~ | ✅ **已收敛**：本地排除，仅远程 | ~~LocalWebAPI 内嵌 Hub~~ 不再考虑；本地无队列无推送 |
| 推送内容粒度 | 待 spec 定案 | 新挂号、挂号状态变更（开始就诊 / 取消）；待 spec 细化 payload schema |
| 降级策略 | 推送失败时医生端回退轮询 | 复用 [US-REG-004](../../02-requirements/08-registration.md) 候诊队列接口 |
| 与 SwitchingApiClient 的关系 | 待 spec 定义 | 切换到本地时关闭 Hub 连接（本地无推送）；切回远程时重建 |

### 即时影响
- 新增 [US-REG-008: 医生工作台待诊列表实时更新](../../02-requirements/08-registration.md)，优先级 Must
- [系统架构总览](../01-system-overview.md) 架构图 WebAPI 侧新增 SignalR Hub 组件
- v1.0 US 总数 141（含 REG-008 SignalR 推送），REG 模块 8
- 本地模式（[ADR-0009](0009-url-driven-dual-mode.md)）不涉及推送通道——Registration 在本地按需激活（无前台用户时不显现）；无论队列有无，本地都不部署 SignalR

### 风险
- ~~双模式推送机制是深层架构问题~~ **已收敛**（R10 S7）：本地排除，无需设计 LocalWebAPI Hub 宿主方案，排期风险解除
- 后续 SignalR 专项 spec 聚焦：远程 Hub 部署 + 推送粒度 + 降级轮询 + Hub 连接随模式切换的生命周期

## 交叉引用
- [US-REG-008: 医生工作台待诊列表实时更新](../../02-requirements/08-registration.md)
- [US-REG-004: 分页查询挂号 + 查看排队](../../02-requirements/08-registration.md)（降级轮询复用）
- [ADR-0002: 双模式架构](0002-dual-mode-architecture.md)
- [ADR-0009: URL 驱动双模式](0009-url-driven-dual-mode.md)
- [系统架构总览](../01-system-overview.md)

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | 范围收敛：本地模式不部署 SignalR（无论队列有无），仅远程；关闭「双模式推送机制」待决项 | R10 spec S7：本地默认无前台用户时无队列；有队列也不推送 |
| 2026-06-28 | 新建 ADR-0013，锁定 v1.0 SignalR 推送范围决策 | R10 访谈反馈 + 对齐市面 HIS 标配 |

## 关联 US

- **US-REG-008**（医生工作台待诊列表实时更新：本 ADR 的直接产物）
- US-REG-004（分页查询挂号 + 候诊队列：推送失败时的降级轮询复用此接口）
