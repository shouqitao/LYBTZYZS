# 上线检查清单（Go-Live Checklist）

> 版本: v1.0 | 日期: 2026-09-24（US-SHELL-022 交付物）

对应需求：[`../02-requirements/11a-shell.md` US-SHELL-022 系统上线与回滚](../02-requirements/11a-shell.md)。回滚细则是独立 SSOT，本文只引用不复制。

## 一、上线前 T-7 ~ T-1 天

- [ ] **备份基线**：按 [06-backup-recovery.md](06-backup-recovery.md) §服务端备份 做一次全量备份并**实际验证可恢复**（恢复到临时库抽查 3 张核心表：患者 / 医案 / 用户）
- [ ] **版本冻结**：确认本次上线版本号（`Directory.Build.props` VersionPrefix），发布包已按 [12-desktop-release.md](12-desktop-release.md) §1–§2 出包并通过 `release-preflight` 五项校验（版本号标准 §4.3）
- [ ] **配置校验**：`POST /api/v1/configuration/validate` 通过，无 Error 级问题（见 [11-server-config-reference.md](11-server-config-reference.md)）
- [ ] **环境变量与密钥核对**：[09-variables-secrets.md](09-variables-secrets.md) 所列项逐条就位；生产 `.env` 权限 600（NTFS ACL 收紧）
- [ ] **角色与账号就绪**：sysadmin 可登录；按权限矩阵建齐前台 / 医生 / 管理员账号（种子逻辑见 IdentitySeedData，权限 SSOT 见 PRD 权限矩阵）
- [ ] **连通性**：Desktop（远程模式）→ WebAPI `GET /api/v1/health` 200；本地模式 LocalDB 连接测试通过（初始化向导第 2 步）
- [ ] **培训与材料到位**：培训材料 / 快速上手 / FAQ 已发到各角色（见 [../training/README.md](../training/README.md)）
- [ ] **双轨方案确认**：试运行期纸质/旧流程保留方式、核对频率与责任人（见本文 §三）

## 二、上线日 T-0

1. [ ] 停写窗口：通知业务暂停录入（若为增量切换）
2. [ ] 最终全量备份（再次走 [06-backup-recovery.md](06-backup-recovery.md)）
3. [ ] 服务端部署：按 [01-deployment.md](01-deployment.md) §服务端部署，部署后 `systemctl status lybt-webapi` active（服务名以服务器实测为准）
4. [ ] 迁移执行：EF 迁移随启动自动执行；对照迁移清单确认无未挂起模型差异
5. [ ] 冒烟（服务端）：
   - `GET /api/v1/health` → 200
   - `GET /api/v1/health/details` → 各项依赖（DB / 缓存）Healthy
   - sysadmin 登录 → 200，取 Token 成功
6. [ ] Desktop 客户端发布：按 [12-desktop-release.md](12-desktop-release.md) 推 release 渠道（GitHubReleaseSource 为主），安装包 SHA256 与清单一致
7. [ ] 冒烟（客户端，四条主链路）：
   - 登录 → 首登初始化向导（若新环境）
   - 挂号 → 接诊 → 医案完成
   - 患者建档 → Excel 导入 1 行样例
   - 打印（处方笺）→ 打印机出纸
8. [ ] 上线健康检查留痕：把 `/health/details` 输出与冒烟结果记录进上线记录（日期 / 版本 / 执行人）

## 三、试运行期（双轨运行）

- [ ] 旧流程（纸质 / 旧表）与新系统并行，**逐日核对**：挂号数、收款笔数（如线下）、医案数
- [ ] 核对口径与差异登记表（差异 > 0 必须当日归因）
- [ ] 每日收工后跑一次增量备份（[06-backup-recovery.md](06-backup-recovery.md)）
- [ ] 每日健康检查：`GET /api/v1/health/ping`
- [ ] 反馈渠道宣贯：一线问题走 [../training/support-process.md](../training/support-process.md)，高频问题回填 [../training/faq.md](../training/faq.md)
- [ ] 试运行退出条件：连续 5 个工作日核对差异 = 0，且无阻断性故障 → 转正式运行，旧流程归档

## 四、回滚（触发即执行，不讨论）

**触发条件**（任一即回）：登录失败 / 数据错乱 / 性能不可用（US-SHELL-022 业务规则 3）。

1. [ ] 决策：查 [08-deployment-rollback.md](08-deployment-rollback.md) §回滚决策矩阵
2. [ ] 执行：
   - 数据回退 → [06-backup-recovery.md](06-backup-recovery.md) §恢复流程
   - 版本回退 → [08-deployment-rollback.md](08-deployment-rollback.md) §回滚流程（服务端）+ §客户端部署（Velopack 版本回退）
3. [ ] 验证：走完 [08-deployment-rollback.md](08-deployment-rollback.md) §回滚后验证清单 全项
4. [ ] 记录：回滚时间、触发原因、影响面、恢复时长，登记进上线记录

## 五、上线后 T+7

- [ ] 差异趋势复盘（核对表归档）
- [ ] 备份策略转入常规巡检（[07-monitoring-alerting.md](07-monitoring-alerting.md)）
- [ ] 未解决问题清点：移交 [../training/support-process.md](../training/support-process.md) 三线流程
- [ ] 更新上线记录：版本、日期、执行人、回滚与否

## 变更记录

| 日期 | 变更 | 版本 |
|------|------|------|
| 2026-09-24 | 首版（US-SHELL-022 交付，补齐原缺失的上线检查清单） | v1.0 |
