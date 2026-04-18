# LYBTZYZS PRD 优化方案

> **生成日期**: 2026-04-18
> **基于**: LYBTZYZS PRD 深度审查报告（总评 8.5/10）
> **目标**: 修复文档矛盾、补齐业务缺失、建立长期维护机制

---

## 一、修复总览

| 优先级 | 数量 | 预估工作量 | 执行策略 |
|--------|------|-----------|---------|
| 🔴 P0 必须修复 | 3 项 | 1.5 天 | 直接修改原文档 |
| 🟡 P1 应当修复 | 4 项 | 2 天 | 修改原文档 + 新增文档 |
| 🟢 P2 改进建议 | 4 项 | 3 天 | 新增文档 + 功能规划 |

---

## 二、P0 — 必须修复（文档矛盾会导致实现 bug）

### P0-01: 统一 `Prescription.ReferencedFormulas` 定义

#### 问题描述
两个文档对该字段的存储格式和长度定义不一致：

| 属性 | medical-cases.md | data-model.md |
|------|-----------------|---------------|
| 类型 | `string(1000)?` | `string(500)` |
| 格式 | JSON 数组 | 逗号分隔 |
| 示例 | `[{"type":"formula","id":"uuid","name":"四君子汤","importedAt":"..."}]` | `"uuid1,uuid2"` |

#### 修改方案
**以 medical-cases.md 为权威来源**（JSON 格式支持更多元数据，扩展性更好）。

##### 修改文件 1: `03-architecture/data-model.md`
定位到 Prescription 表的 `ReferencedFormulas` 行：

**修改前**:
```
ReferencedFormulas | string(500) | 引用验方 (逗号分隔)
```

**修改后**:
```
ReferencedFormulas | string(1000)? | 引用验方 JSON 数组。格式: [{"type":"formula","id":"uuid","name":"验方名","importedAt":"ISO8601"}]
```

##### 修改文件 2: `01-product/glossary.md`
如果 glossary.md 中有 ReferencedFormulas 条目，同步更新格式描述。

##### 涉及代码检查
如果后端代码中已实现该字段，需同步检查：
- Entity 类中字段长度是否为 1000
- DTO 序列化/反序列化是否使用 JSON 格式
- 数据库列定义是否需要 ALTER

---

### P0-02: 统一 `DecocteMethod`（煎法）枚举

#### 问题描述
三个文档的煎法枚举定义完全不一致：

| 枚举值 | medical-cases.md | data-model.md | glossary.md |
|--------|-----------------|---------------|-------------|
| 0 | Normal | Default | Default |
| 1 | DecocteFirst | PreDecoct | PreDecoct |
| 2 | DecocteLater | PostAdd | PostDecoct |
| 3 | WrapDecoction | MeltIn | — |
| 4 | SeparateDecoction | TakeWithWater | — |
| 5 | MeltIn | WrapDecoct | — |
| 6 | TakeWithDecoction | SeparateDecoct | — |
| **总数** | **7** | **7（名称不同）** | **仅 3** |

#### 修改方案
**以 medical-cases.md 为权威来源**（业务描述最完整，与中医术语最贴切）。

##### 标准枚举定义（将作为全局权威）:
```
Normal(0)          - 常规煎法
DecocteFirst(1)    - 先煎
DecocteLater(2)    - 后下
WrapDecoction(3)   - 包煎
SeparateDecoction(4) - 另煎/单煎
MeltIn(5)          - 烊化（溶化）
TakeWithDecoction(6) - 冲服（兑服）
```

##### 修改文件 1: `03-architecture/data-model.md`
定位到 DecocteMethod 枚举定义，替换为上述标准定义。同时更新所有引用该枚举的字段描述。

##### 修改文件 2: `01-product/glossary.md`
定位到 DecocteMethod 条目，从 3 个值扩展为 7 个值，完整对齐标准定义。

##### 涉及代码检查
- 枚举类定义是否与标准一致
- 如果使用了 data-model.md 的旧名称（PostAdd/MeltIn 等），需要重命名
- 数据库已有数据是否需要迁移（如果值 2-6 的映射不同，需要写迁移脚本）

**⚠️ 风险提示**: 如果代码已部署且数据库有数据，枚举值 2-6 的映射变更需要写数据迁移脚本，否则会导致煎法显示错误。

---

### P0-03: 统一 `MedicalCaseAuditLog.OperationType` 类型

#### 问题描述
medical-cases.md 描述为"使用 int 枚举存储（非 string）"，但 data-model.md 中字段类型为 `string` + `MaxLength(20)`。

#### 修改方案
**统一为 `int` 枚举存储**（性能更好，索引更高效，枚举值有限且稳定）。

##### 修改文件: `03-architecture/data-model.md`
定位到 MedicalCaseAuditLog 表的 OperationType 行：

**修改前**:
```
OperationType | string | MaxLength(20) | 操作类型
```

**修改后**:
```
OperationType | int | - | 操作类型枚举（见 medical-cases.md 审计日志枚举定义）
```

##### 涉及代码检查
- 确认代码中是否已定义为 int 或 string
- 如果是 string，需要评估迁移影响

---

## 三、P1 — 应当修复（本周完成）

### P1-01: 修正 user-roles.md Receptionist 患者权限

#### 问题描述
user-roles.md 的 API 权限矩阵中，Receptionist 对 `/api/v1/patients` 显示"禁止"，但 patients.md 明确定义 Receptionist 有 CRU 权限。

#### 修改方案
##### 修改文件: `01-product/user-roles.md`
定位到模块权限矩阵表，Receptionist 行 + Patients 列：

**修改前**: 禁止

**修改后**: CRU（无删除权限）

同时在单元格中添加说明：
```
CRU（无删除权限）— 创建患者、查看列表/详情、更新信息
```

##### 交叉验证
确认 role-permission-matrix.md 中 Receptionist 对 Patients 的描述已正确（应为 CRU），如不一致则同步修正。

---

### P1-02: role-permission-matrix.md 增加枚举值 vs 权限值对照

#### 问题描述
UserRole 枚举值（0/1/10/100）与 PermissionLevel 权限值（40/60/80/100）容易混淆，代码中混用会导致权限漏洞。

#### 修改方案
##### 修改文件: `02-requirements/role-permission-matrix.md`
在 Section 1.1 角色定义之前，插入醒目的对照表：

```markdown
### ⚠️ 重要区分：UserRole 枚举值 vs PermissionLevel 权限值

本文档涉及两套不同的数值体系，**切勿混用**：

| 角色 | UserRole 枚举值 | 用途 | PermissionLevel 权限值 | 用途 |
|------|-----------------|------|----------------------|------|
| Receptionist | 0 | 数据库存储、JWT Token | 40 | API 权限层级判断 |
| Doctor | 1 | 数据库存储、JWT Token | 60 | API 权限层级判断 |
| Admin | 10 | 数据库存储、JWT Token | 80 | API 权限层级判断 |
| SuperAdmin | 100 | 数据库存储、JWT Token | 100 | API 权限层级判断 |

**UserRole**：存储在数据库 User 表和 JWT Token 中，用于身份识别。
**PermissionLevel**：用于 API 端点权限检查，值越大权限越高。
```

---

### P1-03: 补充中药配伍禁忌检查设计

#### 问题描述
中医"十八反、十九畏"是开方安全底线，当前 PRD 完全未涉及。虽然 v1.0 scope 未包含，但应在文档中规划。

#### 修改方案
##### 新增文件: `02-requirements/contraindications.md`

```markdown
# 配伍禁忌检查需求

> 版本规划: v1.1
> 优先级: 高（行业安全底线）

## 1. 概述

系统在处方保存前自动检查药材配伍禁忌，防止不安全处方开出。

## 2. 禁忌数据

### 2.1 十八反（绝对禁忌）
| 药材 | 禁忌药材 | 说明 |
|------|---------|------|
| 甘草 | 大戟、芫花、甘遂、海藻 | 甘草反大戟、芫花、甘遂、海藻 |
| 乌头 | 半夏、贝母、瓜蒌、白蔹、白及 | 乌头反半夏、贝母、瓜蒌、白蔹、白及 |
| 藜芦 | 人参、沙参、丹参、玄参、细辛、芍药 | 藜芦反人参等 |

### 2.2 十九畏（相对禁忌）
| 药材 | 畏惧药材 | 说明 |
|------|---------|------|
| 硫磺 | 朴硝 | 硫磺畏朴硝 |
| 水银 | 砒霜 | 水银畏砒霜 |
| 狼毒 | 密陀僧 | 狼毒畏密陀僧 |
| 巴豆 | 牵牛 | 巴豆畏牵牛 |
| 丁香 | 郁金 | 丁香畏郁金 |
| 牙硝 | 三棱 | 牙硝畏三棱 |
| 川乌/草乌 | 犀角 | 川乌草乌畏犀角 |
| 人参 | 五灵脂 | 人参畏五灵脂 |
| 肉桂 | 赤石脂 | 肉桂畏赤石脂 |

## 3. 检查规则

### 3.1 触发时机
- 处方添加药材时（实时检查）
- 处方保存前（最终校验）
- 验方导入处方时

### 3.2 严重度分级
| 级别 | 行为 | 适用范围 |
|------|------|---------|
| 🔴 绝对禁忌 | **阻断保存**，必须移除禁忌药材 | 十八反 |
| 🟡 相对禁忌 | **弹出警告**，医生确认原因后可继续 | 十九畏 |
| 🟢 提示 | **信息提示**，不影响操作 | 同类功效药材过多 |

### 3.3 审计日志
- 相对禁忌的确认操作必须记录：医生ID、禁忌组合、确认原因、时间戳
- 审计日志不可删除

## 4. 数据模型

### Contraindication 表
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | UUID | 主键 |
| HerbAId | UUID | 药材A ID |
| HerbBId | UUID | 药材B ID |
| Type | int | 0=十八反（绝对）/ 1=十九畏（相对） |
| Description | string(200) | 禁忌描述 |
| Source | string(50) | 出处（"十八反"/"十九畏"） |

## 5. API 设计

### GET /api/v1/contraindications/check?herbIds={id1,id2,...}
检查给定药材列表是否存在配伍禁忌。

**响应**:
```json
{
  "hasContraindication": true,
  "items": [
    {
      "herbA": { "id": "uuid", "name": "甘草" },
      "herbB": { "id": "uuid", "name": "大戟" },
      "type": "absolute",
      "description": "甘草反大戟",
      "source": "十八反"
    }
  ]
}
```

## 6. User Stories

- US-CT-001: 作为医生，我添加药材时如果与已有药材配伍禁忌，系统立即标红提示
- US-CT-002: 作为医生，处方含十八反禁忌时无法保存，必须移除禁忌药材
- US-CT-003: 作为医生，处方含十九畏禁忌时弹出警告，我输入确认原因后可继续保存
- US-CT-004: 作为管理员，我可以查看所有配伍禁忌确认记录
- US-CT-005: 作为管理员，我可以维护配伍禁忌数据表（增删改）
```

---

### P1-04: 补充过敏史开方提醒

#### 问题描述
Patient 有 `AllergyHistory` 字段，但处方开具流程中未定义自动提醒。

#### 修改方案
##### 修改文件: `02-requirements/medical-cases.md`
在 Business Rules 章节新增 BR-004：

```markdown
### BR-004: 过敏史检查（v1.1）

**触发时机**: 处方保存前
**检查逻辑**:
1. 获取当前患者 AllergyHistory
2. 如果 AllergyHistory 不为空，逐个匹配处方中的药材名称
3. 匹配规则：AllergyHistory 文本中包含药材名称即为命中

**行为**:
- 命中时：弹出警告弹窗，显示过敏药材和患者过敏史原文
- 医生必须点击"确认已知，继续保存"才能完成保存
- 确认操作写入 MedicalCaseAuditLog

**校验规则**:
- AllergyHistory 为空或 null → 跳过检查
- 命中多个药材 → 列表展示全部命中项
```

---

## 四、P2 — 改进建议（后续版本）

### P2-01: 创建术语权威来源表

##### 修改文件: `01-product/glossary.md`
在文件开头（标题之后）插入：

```markdown
## 权威来源声明

> 以下术语可能出现在多个文档中，本表定义每个术语的权威来源。
> 其他文档中的同名术语应与本表保持一致，如发现不一致以权威来源为准。

| 术语 | 权威来源 | 同步文档 | 最后同步日期 |
|------|---------|---------|------------|
| UserRole | user-roles.md | auth.md, role-permission-matrix.md | — |
| PermissionLevel | role-permission-matrix.md | user-roles.md | — |
| DecocteMethod | medical-cases.md | glossary.md, data-model.md | — |
| Prescription.ReferencedFormulas | medical-cases.md | data-model.md | — |
| MedicalCaseAuditLog.OperationType | medical-cases.md | data-model.md | — |
| Patient.BloodType | patients.md | data-model.md | — |
| Patient.MaritalStatus | patients.md | data-model.md | — |
```

---

### P2-02: 补充处方用法模板

##### 新增章节: 在 `02-requirements/formulas.md` 中增加

```markdown
## 处方用法模板（v1.2）

系统预置常用处方用法，医生可从下拉列表快速选择，减少手动输入。

### 预置模板
| ID | 模板内容 | 分类 |
|----|---------|------|
| TPL-001 | 水煎服，日一剂，分两次温服 | 内服 |
| TPL-002 | 水煎服，日一剂，分三次饭后服 | 内服 |
| TPL-003 | 水煎服，日一剂，早晚分服 | 内服 |
| TPL-004 | 开水冲服，日两次 | 颗粒/散剂 |
| TPL-005 | 研末吞服，每次3g，日两次 | 散剂 |
| TPL-006 | 外用，水煎熏洗患处 | 外用 |
| TPL-007 | 外用，研末调敷患处 | 外用 |
| TPL-008 | 黄酒送服 | 药酒 |

### User Stories
- US-FM-TPL01: 作为医生，我可以在开处方时从用法下拉框选择预置模板
- US-FM-TPL02: 作为医生，我可以选择预置模板后继续编辑微调
- US-FM-TPL03: 作为管理员，我可以维护用法模板列表
```

---

### P2-03: 补充药材分类体系

##### 修改文件: `02-requirements/herbs.md`
在 Category 字段相关位置增加预置分类：

```markdown
### 药材分类体系（Category 预置值）

| 分类编号 | 分类名称 | 示例药材 |
|---------|---------|---------|
| 01 | 解表药 | 麻黄、桂枝、荆芥、防风 |
| 02 | 清热药 | 石膏、知母、黄芩、黄连 |
| 03 | 泻下药 | 大黄、芒硝、火麻仁 |
| 04 | 祛风湿药 | 独活、威灵仙、秦艽 |
| 05 | 化湿药 | 苍术、厚朴、藿香 |
| 06 | 利水渗湿药 | 茯苓、泽泻、薏苡仁 |
| 07 | 温里药 | 附子、干姜、肉桂 |
| 08 | 理气药 | 陈皮、枳实、香附 |
| 09 | 消食药 | 山楂、神曲、麦芽 |
| 10 | 止血药 | 地榆、三七、白及 |
| 11 | 活血化瘀药 | 川芎、丹参、红花 |
| 12 | 止咳化痰平喘药 | 半夏、桔梗、杏仁 |
| 13 | 安神药 | 酸枣仁、远志、龙骨 |
| 14 | 平肝熄风药 | 天麻、钩藤、石决明 |
| 15 | 开窍药 | 麝香、冰片、苏合香 |
| 16 | 补益药 | 人参、黄芪、当归、枸杞 |
| 17 | 收涩药 | 五味子、乌梅、山茱萸 |
| 18 | 涌吐药 | 瓜蒂、常山 |
| 19 | 攻毒杀虫止痒药 | 硫磺、雄黄、蛇床子 |
| 20 | 拔毒化腐生肌药 | 升药、铅丹、炉甘石 |

> 参考：高等中医药院校教材《中药学》分类体系
```

---

### P2-04: 补充数据统计需求

##### 新增文件: `02-requirements/statistics.md`

```markdown
# 数据统计需求

> 版本规划: v1.1
> 优先级: 中

## 1. 概述

为基础数据统计面板提供需求定义，帮助诊所管理者了解运营状况。

## 2. 统计指标

### 2.1 门诊统计
| 指标 | 数据源 | 维度 |
|------|--------|------|
| 日门诊量 | MedicalCase | 日/周/月/年 |
| 医生接诊量排行 | MedicalCase.GroupBy(DoctorId) | 日/周/月 |
| 时段分布 | MedicalCase.CreateTime | 小时维度 |
| 初诊/复诊比例 | MedicalCase.IsFirstVisit | 百分比 |

### 2.2 处方统计
| 指标 | 数据源 | 维度 |
|------|--------|------|
| 常用药材 TOP20 | PrescriptionItem.GroupBy(HerbId) | 日/周/月 |
| 验方使用频率 | Prescription.ReferencedFormulas | 日/周/月 |
| 平均处方味数 | PrescriptionItem.Count | 均值 |
| 平均处方金额 | Prescription.TotalPrice | 均值/中位数 |

### 2.3 收入统计
| 指标 | 数据源 | 维度 |
|------|--------|------|
| 日/月收入 | Prescription.TotalPrice.Sum | 日/月 |
| 收入趋势 | Prescription.TotalPrice | 月度趋势图 |
| 收入构成 | 挂号费 + 处方费 | 百分比 |

### 2.4 患者统计
| 指标 | 数据源 | 维度 |
|------|--------|------|
| 新增患者数 | Patient.CreateTime | 日/周/月 |
| 活跃患者数 | MedicalCase.GroupBy(PatientId) | 月维度 |
| 性别/年龄分布 | Patient 表 | 饼图/柱状图 |

## 3. User Stories

- US-STAT-001: 作为管理员，我可以看到今日门诊量、收入、新增患者数
- US-STAT-002: 作为管理员，我可以查看门诊量趋势图（日/周/月/年）
- US-STAT-003: 作为管理员，我可以查看常用药材 TOP20 排行
- US-STAT-004: 作为管理员，我可以查看医生接诊量排行
- US-STAT-005: 作为管理员，我可以导出统计报表为 Excel
```

---

## 五、执行计划

### 阶段 1: 文档修正（P0，1.5 天）

| 步骤 | 操作 | 涉及文件 | 预估时间 |
|------|------|---------|---------|
| 1 | 统一 ReferencedFormulas 定义 | data-model.md, glossary.md | 0.5h |
| 2 | 统一 DecocteMethod 枚举 | data-model.md, glossary.md | 1h |
| 3 | 统一 OperationType 类型 | data-model.md | 0.5h |
| 4 | 交叉验证所有修改 | 全部相关文档 | 0.5h |
| 5 | 检查代码是否受影响 | 枚举类、Entity、数据库 | 剩余时间 |

### 阶段 2: 权限修正 + 业务补充（P1，2 天）

| 步骤 | 操作 | 涉及文件 | 预估时间 |
|------|------|---------|---------|
| 1 | 修正 Receptionist 权限 | user-roles.md | 0.5h |
| 2 | 增加枚举值对照表 | role-permission-matrix.md | 0.5h |
| 3 | 新建配伍禁忌需求文档 | contraindications.md（新） | 3h |
| 4 | 新增过敏史检查规则 | medical-cases.md | 1h |
| 5 | 审查报告对照验证 | 全部修改文件 | 1h |

### 阶段 3: 改进补充（P2，按需排期）

| 步骤 | 操作 | 涉及文件 |
|------|------|---------|
| 1 | 创建术语权威来源表 | glossary.md |
| 2 | 补充处方用法模板 | formulas.md |
| 3 | 补充药材分类体系 | herbs.md |
| 4 | 新建统计需求文档 | statistics.md（新） |

---

## 六、修改后验证清单

每项修改完成后，执行以下检查：

- [ ] 全文搜索修改的术语，确认无遗漏的旧定义
- [ ] 检查修改文件与其他文档的交叉引用
- [ ] 确认 glossary.md 的定义已同步
- [ ] 确认 data-model.md 的字段定义已同步
- [ ] 如果涉及枚举值变更，检查代码中的枚举类定义
- [ ] 如果涉及数据库字段类型变更，评估数据迁移影响
- [ ] 更新文档底部的变更记录（Change Log）

---

## 七、术语权威来源总表

以下为修改完成后应确立的权威来源体系：

| 术语/字段 | 权威来源文档 | 路径 | 说明 |
|----------|------------|------|------|
| UserRole 枚举 | user-roles.md | 01-product/ | 角色身份标识 |
| PermissionLevel | role-permission-matrix.md | 02-requirements/ | API 权限层级 |
| DecocteMethod | medical-cases.md | 02-requirements/ | 煎法枚举（7值） |
| ReferencedFormulas | medical-cases.md | 02-requirements/ | JSON 数组格式 |
| OperationType | medical-cases.md | 02-requirements/ | int 枚举 |
| Patient 字段 | patients.md | 02-requirements/ | 患者字段定义 |
| 所有数据模型 | data-model.md | 03-architecture/ | 数据库层定义（应与PRD同步） |

---

**文档结束**

> 本方案基于 2026-04-18 PRD 深度审查报告生成。
> 执行前建议与开发团队确认代码层面是否已实现相关功能，避免文档-代码再次脱节。
