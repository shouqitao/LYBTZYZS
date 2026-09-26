# 支持流程（Support Process）

> 版本: v1.0 | 日期: 2026-09-24

对应需求：US-SHELL-023（三级支持 + 远程协助 / 工单 / 版本回退渠道）。

## 一、三级支持结构

```
一线：使用者自助          → 二线：诊所管理员          → 三线：厂商 / 开发
   FAQ + F1 帮助 +           账号/配置/备份/诊断            远程协助 + 工单 + 版本回退
   troubleshooting
```

### 一线 — 使用者自助（先走完再升级）

1. [faq.md](faq.md) 对应章节
2. 系统内 **F1 帮助**
3. [../06-operations/03-troubleshooting.md](../06-operations/03-troubleshooting.md)（管理员也可用）

**升级条件**：自助 10 分钟未解决，或属账号 / 网络 / 服务器类问题。

### 二线 — 诊所管理员

处理范围：
- 账号：建号、重置密码、角色分配（一级管一级、不可自管）
- 配置：诊所信息、连接模式、配置校验（`POST /api/v1/configuration/validate`）
- 备份恢复：执行与恢复（[../06-operations/06-backup-recovery.md](../06-operations/06-backup-recovery.md)）
- 读卡器诊断入口（sysadmin 主页）
- 本地模式 LocalDB 连接问题

**升级条件（任一）**：数据不一致 / 打印批量失败 / 服务端健康检查非 200 / 二线 30 分钟未解决。

### 三线 — 厂商 / 开发

| 渠道 | 用于 | 入口 |
|------|------|------|
| **远程协助** | 需要看现场界面 / 复现操作的问题 | 由二线向厂商预约，远程会话内操作全程可见 |
| **工单** | 缺陷、需求、批量问题 | GitHub Issues（`shouqitao/LYBTZYZS`）；无法访问仓库时由二线邮件代提 |
| **版本回退** | 新版本引入阻断性故障 | [../06-operations/08-deployment-rollback.md](../06-operations/08-deployment-rollback.md) §回滚决策矩阵 + §客户端部署（Velopack 回退），**禁止手改安装目录** |

## 二、报障必须带的信息（二线收集后升级）

1. 版本号（帮助 / 关于）
2. 本地 or 远程模式 + 连接是否正常
3. 问题时间点、操作路径（从哪一步开始错）
4. 截图 / 报错原文（一字不差，不要转述）
5. 复现频率（必现 / 偶发）
6. 影响面（几台机 / 几个人 / 数据是否已错）
7. **若涉及数据错乱：先备份再动任何东西**

## 三、版本回退渠道（升级即触发条件）

- **触发**：新版本上线后出现阻断性故障（登录失败 / 数据错乱 / 性能不可用）
- **决策与执行**：[../06-operations/13-go-live-checklist.md](../06-operations/13-go-live-checklist.md) §四 + [../06-operations/08-deployment-rollback.md](../06-operations/08-deployment-rollback.md)
- **权限**：仅管理员执行；执行后在工单记录回退时间与原因

## 四、问题闭环

- 三线修复的缺陷 → 回填 [faq.md](faq.md) 对应问题（同类问题不再二次升级）
- 反馈集中在试运行期：见 [../06-operations/13-go-live-checklist.md](../06-operations/13-go-live-checklist.md) §三

## 变更记录

| 日期 | 变更 | 版本 |
|------|------|------|
| 2026-09-24 | 首版（US-SHELL-023 交付，三级支持 + 三渠道 + 报障信息清单） | v1.0 |
