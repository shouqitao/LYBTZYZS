# Findings - 偏差分类确认清单

## 分类标准

### CODE (代码侧修复) -- 193 项 (74.5%)
- 安全漏洞/数据完整性风险 (X3 Token撤销, X7 引用检查, 密码哈希Bug等)
- PRD 要求合理且已被设计文档确认的核心功能 (X8 打印重构等)
- 验证值/配置值与 PRD 不一致且 PRD 值更合理 (X5 部分项)
- Bug 和技术实现问题 (X6 分页内存过滤, Mapper不映射等)
- 错误码体系脱节 (X1, X4)
- 本地模式功能缺口 (X2)

### PRD (PRD侧修订) -- 38 项 (14.7%)
- simplify-auth 简化决策已接受 (登出前警告、超时前警告)
- PRD 过度设计 (AuthSession独立表、触摸事件追踪、内网限流区分)
- 代码行为更合理 (Price验证、状态名称、OperationType存储)
- P3 细节差异中代码格式/命名更合理的项

### DEFER (延期处理) -- 25 项 (9.7%)
- MedicalCase 同步体系 (7项) -- 独立 Epic
- 本地模式 Excel/JSON 导入导出 (7项) -- 独立 Sprint
- 运行时模式切换+回退 (3项) -- 合并入同步 Epic
- EditModeStateMachine (2项) -- 独立 Sprint
- SQLite 字段级加密 (1项) -- 独立 Epic
- 其他低优先级功能 (5项)

### BOTH (双方调整) -- 3 项 (1.2%)
- MC 初始状态 Active vs Draft
- Checksum 字段规格
- 超时前警告 (simplify-auth)

## 关键发现

### 1. CODE 占比 74.5% -- 代码工作量大
绝大多数偏差需要代码侧修复。users(29/30)和patients(28/28)模块几乎全部是CODE。

### 2. X8 打印层级重构是最大单一修复任务
独占 16 条偏差，是 printing 模块 12 个 P1 的根因。C6 确认后必须执行。

### 3. X3 Token Family 撤销是最严重的安全问题
6 处安全漏洞分布在 auth 和 users 模块，已删除/禁用/密码变更的用户 Token 仍有效。

### 4. sync 模块 DEFER 最多 (7/19)
MedicalCase 同步完全未实现(PRD 220行规格)，复杂度极高需独立 Epic。

### 5. P3-PRD 占比高 (31/66 = 47%)
P3 级别偏差中近半属于 PRD 过度规范，说明 PRD 在细节层面需要简化。

## 横切面命中统计

| 横切面 | 命中偏差数 | 分布最广模块 |
|--------|-----------|-------------|
| X1 错误码体系 | ~15 | auth, patients, herbs, formulas, sync, error-handling |
| X2 本地模式缺口 | ~22 | users(7), herbs(5), formulas(5), patients(2), printing(2), desktop-shell(2), nfr(1) |
| X3 Token 撤销 | ~6 | users(5), auth(1) |
| X4 硬编码字符串 | ~6 | users(1), herbs(1), formulas(1), logging(2), nfr(1) |
| X5 字段验证值 | ~14 | auth(1), users(1), patients(1), herbs(6), formulas(3), mc(2), shell(1), config(2), nfr(2) |
| X6 分页内存过滤 | ~5 | users(1), herbs(1), formulas(2), medical-cases(1) |
| X7 引用检查 | ~10 | patients(6), herbs(4) |
| X8 打印重构 | ~16 | medical-cases(5), printing(12) |
