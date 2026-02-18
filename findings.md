# Findings

## PRD 变更清单 (需要设计文档承接)

| # | PRD 变更 | 来源 | 设计影响 |
|---|----------|------|---------|
| 1 | FR-MC-018 复制历史处方 | medical-cases.md v1.7 | API + DTO + Service |
| 2 | FR-PAT-013 患者状态管理 | patients.md v1.8 | API + 实体 + 查询 |
| 3 | MC-D15 IsPrinted 提升到聚合根 | medical-cases.md v2.0 | 数据模型迁移 |
| 4 | MC-D16 角色脱敏 | medical-cases.md v2.1 | 查询权限过滤 |
| 5 | 90 错误码 (MCCEE体系) | 6个PRD文件 | 错误码常量设计 |
| 6 | 缓存失效策略重写 | nfr.md v1.2 | 架构层缓存设计 |
| 7 | NFR-API-001 全局分页规范 | nfr.md v1.3 | API基类设计 |

---

## 现有设计文档调研

### 03-architecture/ (7个文档 + 6个ADR)

| 文档 | 版本 | 核心内容 | 与PRD变更的差距 |
|------|------|---------|----------------|
| data-model.md | v1.0 | 12实体字段定义、8枚举、EF Core约定 | **高**: IsPrinted 在 Prescription 上，PRD要求提升到 MedicalCase; Patient 用 Status:CommonStatus 非 IsDisabled |
| server.md | v1.0 | 三层架构、CQRS、错误码(5位前缀制)、DI规范 | **中**: 错误码前缀分配与PRD的MCCEE体系需对齐; 无缓存策略 |
| desktop.md | v1.1 | MVVM+Prism、ViewModel体系、Components模式 | 低 |
| shared.md | v1.0 | DTO层次、验证规则、敏感字段脱敏 | **中**: 角色脱敏(MC-D16)需补充 |
| dual-mode.md | v1.0 | 双模式策略、同步架构 | 低 |
| system-overview.md | v1.0 | 整体架构、33项目、依赖方向 | 低 |
| ADR-0001~0006 | - | 聚合根、双模式、测试、用户上下文、SuperAdmin、组件化 | 低 |

### 04-api-reference/ (9个模块文档 + README)

| 文档 | 端点数 | 与PRD差距 |
|------|--------|----------|
| patients.md | 10 | **高**: 缺 FR-PAT-013 状态管理端点 (PUT /patients/{id}/status) |
| medical-cases.md | 18 | **中**: 缺 FR-MC-018 专用端点，但可通过现有API组合实现 |
| README.md | 索引 | 需更新端点总数、错误码引用 |
| 其他7个模块 | 64 | 100% 覆盖，无差距 |

**总计**: 92 个 API 端点

---

## 差距分析结论

### 必须更新的设计文档 (4项)

| 优先级 | 目标文档 | 更新内容 | 原因 |
|--------|---------|---------|------|
| P0 | data-model.md | IsPrinted 迁移 + Patient Status 语义 + LastPrintedAt/PrintVersion | 数据模型变更是其他设计的基础 |
| P0 | 04-api-reference/patients.md | FR-PAT-013 状态管理API设计 | 完全缺失，无法实现 |
| P1 | 04-api-reference/medical-cases.md | FR-MC-018 复制历史处方实现路径 | 需明确组合API模式 |
| P1 | server.md | 缓存失效策略 + 错误码MCCEE对齐 | 架构层缺失 |

### 可选更新 (2项)

| 优先级 | 目标文档 | 更新内容 | 原因 |
|--------|---------|---------|------|
| P2 | shared.md | MC-D16 角色脱敏DTO设计 | 涉及查询权限过滤 |
| P2 | 04-api-reference/README.md | 端点总数、版本更新 | 索引同步 |
