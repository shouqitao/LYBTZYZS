# 逐项目 class 验证交叉验证报告（技术总监 + Mimo）

> 日期：2026-08-08
> 独立报告：`structure-audit-perclass-mimo-2026-08-08.md`（Mimo，771 行）
> 技术总监抽查：类名后缀分布 + 蓝图核心声明核对 + 关键发现复证

---

## 一、验证规模

- **34 项目 1422 个顶层类型**逐类判定
- 结果：A=1009（71%）/ B=322（23%）/ C=62（4%）/ D=29（2%）
- **A/B 有设计依据占比 93.6%**——蓝图设计依据覆盖良好

## 二、交叉验证结论

### ✅ 双方一致（蓝图核心声明与代码吻合）

| # | 声明 | 验证 |
|---|------|------|
| 1 | Server 6 模块 CQRS 形态一致 | ✅ 技术总监类名后缀（Handler/Command 主导）+ Mimo 符号级一致 |
| 2 | MedicalCase Service 化（无 MediatR）| ✅ 10 Service + 7 Repository，无 Application |
| 3 | 7 个模块 DbContext（A-20）| ✅ 8 模块各自 DbContext 存在（Reports 复用 AppDbContext）|
| 4 | WebAPI 12 Controller | ✅ |
| 5 | Shared.Models 八目录零孤儿 | ✅ Mimo 144 类型全链有消费 |
| 6 | Server 分层职责 100% 可追溯 | ✅ 183 类对应蓝图/ADR/US |

### ⚠️ Mimo 抓到的新问题（技术总监未覆盖）

| # | 问题 | 详情 | 级别 |
|---|------|------|------|
| **P1-1** | **MVVM 越层还有 6 处遗漏** | A-21 修的 3 处生效，但 Shell/Roles 层仍有 6 个 VM 直接注入 IApiClient：Shell `AccountSettingsViewModel`（:71 注入 IApiClientUsers）、Clinical `PatientSelectionViewModel`（双接口）、Admin `SystemSettings/SysadminHome/LogLevelControl/DeploymentViewModel`——**A-21 只覆盖了 Modules 层，Shell/Roles 未覆盖**；架构守卫 P01c 只扫 Server 程序集，**Desktop 越层零守卫** | P1 |
| P1-2 | Infrastructure Http 目录仍滞留 | `LoggingHttpHandler` 存活应下沉 Foundation、`ApiResponseHelper` 死 | P1 |
| P1-3 | 孤儿类 D=29 | Patients 死组件链 12 类（~680 行）+ Infrastructure 9 + Foundation 2 + Formula 2 + Shared 1 | P1/P2 分批 |
| P2 | 文档滞后 4 处 | 蓝图 v1.0 基线不动，记录偏差 | P2 |

## 三、终版结论

**「每个 class 有设计依据」已达成 93.6%**（A/B 级）。剩余 6%（C 级 4% 存疑 + D 级 2% 孤儿）需处置：

1. **P1-1 六个 VM 越层** → 修复批次（补 A-21 未覆盖的 Shell/Roles）+ **架构守卫补 Desktop 越层规则**（当前 P01c 真空）
2. **P1-2/P1-3 孤儿类清理** → 删 D=29 + Infrastructure 滞留清理（~680+ 行）
3. **C 级 62 个** → 多为「类活但含死方法」，随清理批次处理

## 四、执行建议（A-23）

| 项 | 内容 | 预估 |
|----|------|------|
| A-23a | 6 个 VM 越层修复（Shell/Roles 层）| 0.5d |
| A-23b | 架构守卫新增「Desktop VM 禁止注入 IApiClient」| 0.25d |
| A-23c | 孤儿类 D=29 清理 + Infrastructure 滞留 | 0.5d |
| A-23d | 蓝图 v1.1（记录 4 处文档偏差 + 本轮成果）| 0.25d |
