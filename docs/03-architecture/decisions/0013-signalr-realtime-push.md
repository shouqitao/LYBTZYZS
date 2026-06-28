# ADR-0013: SignalR 实时推送（v1.0 范围决策）

## 状态
Accepted（范围决策）— v1.0 落地「挂号→医生工作台实时推送」这一范围已锁定；**实现细节待后续 SignalR 专项 spec 承载**。

## 上下文
R10 用户访谈反馈：医生在工作台接诊时，需要**实时看到**新挂号进入待诊列表，而非手动刷新或定时轮询。市面医院信息系统（HIS）普遍将「挂号→医生端实时刷新」作为标配体验，用户对此有明确预期。

当前 v1.0 候诊队列依赖 [US-REG-004](../../02-requirements/08-registration.md) 的分页查询接口，医生需主动刷新才能看到新挂号，体验落后于行业基准。

## 决策
v1.0 采用 **SignalR** 实现挂号变更到医生工作台的实时推送：
- 新挂号创建（Source=Receptionist，Status=Waiting）→ 推送给指派医生
- 挂号状态变更（开始就诊 / 取消）→ 推送同步
- 医生工作台待诊列表据此实时刷新（见 [US-REG-008](../../02-requirements/08-registration.md)）

## 理由
- SignalR 是 ASP.NET Core 一等公民，与现有 WebAPI 技术栈无缝集成
- 支持 WebSocket 自动降级（Server-Sent Events / Long Polling），适应诊所网络环境
- 推送通道与 REST API 共享认证、依赖注入管道，复用现有 `ApiResponse<T>` 契约与权限策略
- 参考市面 HIS 标配体验，满足 R10 访谈中医生对实时性的明确诉求

## 后果

### 待专项设计的架构问题（本 ADR 仅锚定范围决策，细节由后续 SignalR 专项 spec 承载）

| 议题 | 待决项 | 候选方向（非定案） |
|------|--------|-------------------|
| 双模式推送机制 | 远程模式 Hub 部署于 WebAPI（公网）；本地模式如何推送 | LocalWebAPI 内嵌 Hub？或本地模式仅前端轮询降级？ |
| 推送内容粒度 | 新挂号、挂号状态变更（开始就诊 / 取消） | 待 spec 细化 payload schema |
| 降级策略 | 推送失败时医生端回退轮询 | 复用 [US-REG-004](../../02-requirements/08-registration.md) 候诊队列接口 |
| 与 SwitchingApiClient 的关系 | 推送通道是否随 URL 模式切换 | 待 spec 定义 Hub 连接的路由策略 |

### 即时影响
- 新增 [US-REG-008: 医生工作台待诊列表实时更新](../../02-requirements/08-registration.md)，优先级 Must
- [系统架构总览](../01-system-overview.md) 架构图 WebAPI 侧新增 SignalR Hub 组件
- v1.0 US 总数 136 → 137，REG 模块 7 → 8
- 双模式架构（[ADR-0009](0009-url-driven-dual-mode.md)）需在专项 spec 中明确推送通道的本地/远程行为

### 风险
- 双模式推送机制是深层架构问题，本地模式（内嵌 LocalWebAPI）的 Hub 宿主方案尚未定案，可能影响 v1.0 排期
- 后续 SignalR 专项 spec 应作为独立 brainstorm 主题，重点解决双模式推送机制

## 交叉引用
- [US-REG-008: 医生工作台待诊列表实时更新](../../02-requirements/08-registration.md)
- [US-REG-004: 分页查询挂号 + 查看排队](../../02-requirements/08-registration.md)（降级轮询复用）
- [ADR-0002: 双模式架构](0002-dual-mode-architecture.md)
- [ADR-0009: URL 驱动双模式](0009-url-driven-dual-mode.md)
- [系统架构总览](../01-system-overview.md)

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | 新建 ADR-0013，锁定 v1.0 SignalR 推送范围决策 | R10 访谈反馈 + 对齐市面 HIS 标配 |

## 关联 US

- **US-REG-008**（医生工作台待诊列表实时更新：本 ADR 的直接产物）
- US-REG-004（分页查询挂号 + 候诊队列：推送失败时的降级轮询复用此接口）
