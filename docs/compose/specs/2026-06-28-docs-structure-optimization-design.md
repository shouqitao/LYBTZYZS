# 文档结构优化与内容提炼 spec

> **日期**：2026-06-28
> **状态**：📝 待审
> **范围**：A 内容提炼 + B 结构重组 + D 导航优化（C v2.0 归档用户已排除）
> **依据**：3 路诊断（API 冗余 / 需求+架构提炼 / 导航）
> **目标**：稳定文档 ~22,700 行 → ~17,000 行（降 ~25%），同时提升可导航性与可维护性

---

## [S1] 诊断结论

| 域 | 现状 | 主要问题 | 目标 |
|---|---|---|---|
| 04-api-reference | 6184 行 | JSON 外壳/curl/错误块重复铺张；09-sync v2.0 占 430 行 | ~3850 行（-38%） |
| 02-requirements + 03-architecture | ~9800 行 | 跨文档重复；v2.0 同步协议混入 v1.0；历史残留堆积 | ~8700 行（-11%） |
| 11-platform | 1377 行 | 6 域全塞一文件 | 拆 5 文件（✅ 已完成） |
| 导航 | — | ADR/US 计数滞后；缺运维/PM 路径；compose 不可见 | 计数统一+路径补全 |

---

## [S2] 批次 1：API 参考提炼（A）

**策略**：README 集中通用内容 + 各端点去冗余。

**README 集中化**（+67 行，吸收散落内容）：
1. TOKEN 获取脚本（标准 + sysadmin 变体）— 散落 7 处合一
2. 失败响应信封模板 + `errors.code` 形态
3. 通用 HTTP 状态码表（已有，强化为各端点只保留特有码）
4. `ApiResponse<T>` 字段说明表

**各端点提炼规则**：
- JSON 示例：去 `success/message/errors/timestamp/requestId` 外壳，**只留 data 部分**
- 错误响应：删每端点 8-12 行 JSON 块，合并到文件尾「错误码表」
- HTTP 状态码小表：删通用码（401/403/404），只留端点特有（422/409 等）
- curl：删 TOKEN 获取脚本（引用 README），只留端点行

**重点文件**：
- `06-medical-cases.md`（996→580）：3 处 MedicalCaseDetailDto 完整重复合一 + 引用
- `09-sync.md`（430→120）：v2.0 虚构示例删，留骨架表 + v2.0 spec 指向
- 其余 11 文件：按规则去外壳

**预计**：6184 → ~3850（-2330）

---

## [S3] 批次 2：架构/需求提炼（B 内容）

**跨文档重复权威表**（权威保留全文，他处改链接）：

| 内容 | 权威文档 | 他处处理 |
|---|---|---|
| 权限矩阵 | `12-permissions-matrix.md` | 07-mc 留 MC 铁律；02-desktop 留菜单可见性；11a-shell 删 |
| MC 状态机 | `07-medical-cases.md` | 04-data-model 留枚举 |
| BR-003 校验 | `07-medical-cases.md` | US-MC-011 改引用 |
| 缓存策略 | `12-nfr.md` | 03-server 留链接 |
| SensitiveData | `08-shared.md` | 03-server/11d-observability 留链接 |
| 同步协议(v2.0) | **新 sync-protocol.md**（v2.0） | 05-dual-mode 留概述 |

**逐文件提炼**：
- `02-desktop.md`（896→600）：删事件目录(无信息) + 同步 UI v2.0 外移 + US 章节改链接
- `05-dual-mode.md`（627→380）：**同步协议整段(290行)外移为 v2.0 独立文件**
- `03-server.md`（634→480）：BaseRepository 全表改"见源码" + 重复段链接
- `08-shared.md`（590→450）：Mapperly 规范外移为 mapperly.md
- `04-data-model.md`（513→400）：辅助实体重复删 + 幻影实体字段表压成 spec 引用
- `07-medical-cases.md`（604→520）：BR-003/权限矩阵重复改链接

**预计**：降 ~1100 行

---

## [S4] 批次 3：11-platform 拆分（B 结构）（✅ 已完成）

**拆分方案**（1377 行 → 5 文件）：

| 新文件 | 行数 | 归属 |
|---|---:|---|
| `11a-shell.md` | ~480 | US-SHELL-001~019 |
| `11b-configuration.md` | ~140 | US-CFG-001~004 |
| `11c-error-handling.md` | ~250 | US-ERR-001~008 |
| `11d-observability.md` | ~500 | US-LOG×7 + US-SYS×9（LoggingLevelManager 跨域强关联） |
| `11e-cardreader.md` | ~80 | US-CARD-001~002 |

**不拆 6 文件**：LOG+SYS 合并（强关联）；CARD 2 US 独立即 80 行。

**同步成本**：README 索引 1→5 行；13-traceability-matrix 按 US-ID 索引零改动；跨文档引用 ~6 处链接更新；AGENTS.md WHERE TO LOOK 1 行。

---

## [S5] 批次 4：导航优化（D）

1. **计数统一**：ADR 12→14（docs/README:9,49 + 03-arch/README:50）；02-req 12→13（docs/README:8）；01-product README 9模块/136US→10/141
2. **补链**：04-api/README「系统配置」段补 `10-configuration.md` 链接；补「打印模块」一级索引
3. **阅读路径**：docs/README 快速导航补「运维部署」「PM 需求」两条路径；「开发者」补 04-patterns/06-security/15-migration
4. **compose 可见度**：docs/README 加 compose 入口 + 「compose 产物使用说明」段
5. **追溯矩阵可见**：docs/README 快速导航补 13-traceability-matrix 一行
6. **编号**：评估填补 05-development/02 空洞（或注明跳号）

---

## [S6] 执行序与验证

**执行序**（依赖最小化）：
1. 批次 4 导航（独立，先做，计数统一）
2. 批次 1 API 提炼（README 先集中化，再各文件提炼）
3. 批次 2 架构/需求提炼（权威表先确立，再他处链接）
4. 批次 3 11-platform 拆分（最后，因引用多）✅ 已完成

**验证**：
- 行数：04-api ~3850、02+03 ~8700、总计 ~17,000
- grep 无残留：完整 ApiResponse 外壳在分文档零命中（README 除外）；TOKEN 脚本散落零命中
- 导航：ADR/US 计数全仓一致；断链零；阅读路径覆盖 5 类读者
- 拆分：11-platform 拆 5 文件后原文件删，引用更新 ✅ 已完成

---

## [S7] 范围外

- C v2.0/历史归档（用户已排除）——09-sync 精简但保留主文档；同步协议外移为 v2.0 独立文件属提炼（非归档）
- 不改代码
- 不引入新需求
