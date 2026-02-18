# Findings

## 分析范围
- 4 个产品层文档: vision.md, glossary.md, user-roles.md, README.md
- 16 个需求层文档: auth/users/patients/herbs/formulas/medical-cases/sync/printing/card-reader/health-diagnostics/error-handling/logging/desktop-shell/configuration/ui-patterns/nfr

## 已确认并修复的问题

### 第一段: 功能缺失 -- 业务流程断链 (2 个问题, 已修复)

| # | 问题 | 修复内容 | 版本 |
|---|------|---------|------|
| 1 | "复制历史处方"功能缺失 FR | 新增 FR-MC-018 (10 条业务规则 + 6 条验收标准) | v1.7 |
| 2 | 处方总价计算公式未定义 | FR-MC-004 补充规则 7-9 (SingleDosePrice/TotalPrice/Discount) | v1.7 |

### 第二段: 数据模型缺陷 (6 个问题, 已修复)

| # | 问题 | 修复内容 | 版本 |
|---|------|---------|------|
| 3 | Prescription.LastPrintedAt 缺失 | 数据模型补充 LastPrintedAt 字段 | v1.8 |
| 4 | DecocteMethod 枚举未定义 | 新增完整枚举表 (Normal/先煎/后下/包煎/另炖/烊化/冲服) + 打印规则 | v1.8 |
| 5 | Dosage/UnitPrice 单位不明确 | 修正为"数值部分，单位由 Unit 指定"; UnitPrice 标注"元/单位"; Amount 标注 decimal(18,2) | v1.8 |
| 6 | ReferencedFormulas 格式模糊 | 改为 JSON 数组 + 格式示例 (type/id/name/importedAt) | v1.8 |
| 7 | 错误消息"病案"术语混用 | 全文替换为"医案" | v1.8 |
| 8 | DoctorName/PatientName 更新策略未定义 | 标注"创建时快照，后续改名不影响历史医案" | v1.8 |

### 第三段: 错误码体系 + 双模式覆盖 (3 个问题, 2 修复 + 1 非问题)

| # | 问题 | 处理结果 |
|---|------|---------|
| 9 | 4 个模块约 85 个错误场景缺数字编号 | 已修复: 全量分配 90 个编号 (MCCEE 体系) 到 6 个文件 |
| 10 | FR-AUTH-007 本地模式"保持登录"行为不明确 | 已修复: 明确为重置计时器 (无 Token 刷新), 验收标准拆分远程/本地 |
| 11 | sync.md "不适用" vs "已支持" 看似矛盾 | 降级: 两个不同层面 (API 端点 vs 实体类型支持), 逻辑自洽 |

**问题 9 详情** -- 错误码分配汇总:

| 文件 | 范围 | 子类别 | 编号数 | 版本 |
|------|------|--------|--------|------|
| patients.md | 2xxxx | 207xx 业务 / 208xx 导入 | +9 (补全) | v1.7 |
| medical-cases.md | 3xxxx | 301xx~306xx (6 组) | 29 | v1.9 |
| herbs.md | 5xxxx | 501xx~503xx (3 组) | 15 | v1.4 |
| formulas.md | 6xxxx | 601xx~603xx (3 组) | 17 | v1.5 |
| sync.md | 7xxxx (新) | 701xx~705xx (5 组) | 20 | v3.1 |
| error-handling.md | - | 范围表更新 + "90+ 场景" | - | v2.2 |

排除项: 导入行级验证消息不分配编号 (与 patients.md 已有模式一致)。

---

## 已确认并修复的问题 (第四段)

### 第四段: 边界条件 + 其他

| # | 问题 | 处理结果 |
|---|------|---------|
| 12 | 打印与编辑并发策略缺失 | 已修复: IsPrinted 提升到 MedicalCase 聚合根; 打印后修改任何内容需 EditReason; 修改后 IsPrinted=false + PrintVersion++ (MC-D15) |
| 13 | 患者禁用规则缺失 | 已修复: 新增 FR-PAT-013 患者状态管理; FR-MC-001 增加患者状态检查 (ERR-30105); 禁用场景=患者已故 (PAT-D05); Receptionist 查询过滤禁用患者; 历史医案按角色脱敏 (MC-D16); v2.0 规划关系转移 (PAT-D06) |

| 14 | 缓存失效策略不完整 | 已修复: nfr.md 缓存章节完整重写 -- OutputCache 失效映射表 (12 类写操作); Desktop 写后失效规则; 删除 PrescriptionsCache; 新增客户端配置 (NFR-PERF-003); 内存占用估算 |

| 15 | 分页参数统一性 | 已修复: nfr.md 新增 NFR-API-001 全局分页规范; patients.md 补 ERR-20705; herbs.md 补 ERR-50106; formulas.md/medical-cases.md 加交叉引用 |

| 16 | 身份证脱敏示例错误 | 降级: nfr.md 示例 `320***********1234` 共 18 位，格式正确。原分析误判 |

---

---

## 排除项 (非真实问题)
- MC-D13 与 vision.md "价格快照" 不矛盾 (都是操作时取实时价存快照)
- FR-MC-018 禁用药材处理已明确复用 MC-D09
- CaseNumber 重号风险已在 MC-D02 决策接受
- sync.md FR-SYNC "不适用" vs 决策 3 "已支持": 不同层面，自洽
- auth.md 错误码 ERR- 前缀: 已在 v1.2 修复
- FR-MC-012 本地模式审计: 已在 v1.8 明确
- nfr.md 身份证脱敏示例: `320***********1234` 共 18 位，格式正确，原分析误判
