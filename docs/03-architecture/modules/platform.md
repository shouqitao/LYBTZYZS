# Platform 模块设计
> 版本: v1.0 | 日期: 2026-08-20

> 复杂度: 高 | 状态: 草稿

## 概述

Platform 是系统基础平台，包含 Shell、配置、错误处理、日志、健康检查、读卡器等基础设施。

**职责**: 应用启动/生命周期、导航/模块加载、配置管理、错误处理、日志、API 健康检查、读卡器集成、主题切换。

**依赖**: 上游无（最底层），下游所有业务模块。

## Shell 启动

**入口**: `App.xaml.cs`（PrismApplication）→ 单实例互斥锁 → Serilog → DI 注册 → 14 模块加载 → 登录界面

**启动管道**: ErrorHandling(10) → ModuleCoordinator(20)+CoreServices(30)并行 → ApiHealthCheck(40) → Warmup(50) → LocalWebApi(250)

**模块加载**: WhenAvailable(Auth,Clinical,Admin,Receptionist,Sysadmin) / OnDemand(Users,Patients,Herbs 等)

## 配置管理

| 服务 | 职责 |
|------|------|
| IConnectionSettingsService | 远程/本地 URL 管理 |
| IConnectionModeService | 连接模式（用户显式切换，不自动降级） |
| IApiRouter | 当前 URL 和模式 |

## 健康检查 + 熔断

```
Closed(正常) → 3次失败 → Open(熔断) → 30秒 → HalfOpen → 成功 → Closed
```

`IApiHealthMonitor` 10秒间隔监控 + `IHealthCheckCoordinator` Tick-based 调度。

## 读卡器（策略模式）

```
ICardReader → HuaDaHD100CardReader(P/Invoke) | MockCardReader(测试)
```

**去重链**: IdNumber 精确匹配 → Name+BirthDate 模糊 → 多候选提示 → 无匹配快速创建

## 业务规则

| 规则 | 描述 |
|------|------|
| 单实例 | 互斥锁防止多开 |
| 两阶段启动 | 立即显示登录 + 后台初始化 |
| 按需加载 | 业务模块按角色按需加载 |
| 熔断保护 | 3次失败后停止检查30秒 |
