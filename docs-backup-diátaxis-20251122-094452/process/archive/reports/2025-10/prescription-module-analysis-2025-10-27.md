# Prescription模块功能分析报告

**生成时间**: 2025-10-27
**分析范围**: Prescription模块查询功能实现状态
**目标**: 识别MedicalCase中已实现的处方功能，迁移到Prescription模块

---

## 📋 用户需求清单

根据用户描述，处方模块必须支持以下功能：

| 功能编号 | 功能描述 | 业务场景 |
|---------|---------|---------|
| **REQ-1** | 按照患者查询处方 | 查看患者历史用药记录 |
| **REQ-2** | 按照病症查询处方 | 参考相似病症的治疗方案 |
| **REQ-3** | 历史处方复制到当前处方 | 复诊时沿用有效方剂 |
| **REQ-4** | 历史处方转存成验方 | 将有效处方保存为经验方 |

---

## ✅ PrescriptionService中已实现的功能

### 1. 按患者查询处方（REQ-1）✅

**方法**: `GetPatientRecentPrescriptionsAsync`
**位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs:513-599`

```csharp
public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
    Guid patientId,
    int count = 5)
```

**功能特性**:
- ✅ 支持按患者ID查询所有历史处方
- ✅ 按创建日期倒序排列
- ✅ 支持限制返回数量（默认5条）
- ✅ 包含处方详情：主诉、中医诊断、剂数、嘱托、处方来源
- ✅ **包含药材明细**（Issue #1370新增）：药材数量、处方项列表

**实现方式**: MVP内存过滤（适用于<1000条处方）

**暴露状态**: ❌ **未在PrescriptionsController中暴露**

---

### 2. 按病症查询处方（REQ-2）✅

**方法**: `SearchPrescriptionsAsync`
**位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs:406-504`

```csharp
public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
    string? patientName = null,
    string? symptomKeyword = null)
```

**功能特性**:
- ✅ 支持按**患者姓名**模糊搜索
- ✅ 支持按**病症关键词**搜索（匹配中医诊断和主诉）
- ✅ 同时搜索`Consultation.TCMDiagnosis`和`Prescription.Indication`
- ✅ 支持组合搜索（患者+病症）
- ✅ 返回完整处方摘要信息

**搜索范围**:
- `Consultation.TCMDiagnosis` - 中医诊断
- `Prescription.Indication` - 处方主诉

**实现方式**: MVP内存过滤（跨表关联：Prescription → MedicalCase → Patient + Consultation）

**暴露状态**: ❌ **未在PrescriptionsController中暴露**

---

### 3. 其他已实现的查询方法

| 方法名 | 功能 | 位置 | 暴露状态 |
|-------|------|------|---------|
| `GetPagedAsync` | 分页查询处方（支持关键词、日期范围） | Line 55-98 | ❌ 未暴露 |
| `GetByIdAsync` | 按ID查询处方详情（含药材明细） | Line 100-117 | ❌ 未暴露 |
| `GetByMedicalCaseIdAsync` | 按病案ID查询处方列表 | Line 123-140 | ❌ 未暴露 |
| `RecalculatePriceAsync` | 重新计算处方价格 | Line 169-194 | ❌ 未暴露 |
| `GeneratePrintFormatAsync` | 生成打印格式 | Line 201-217 | ❌ 未暴露 |
| `GetStatisticsAsync` | 获取处方统计数据 | Line 308-347 | ❌ 未暴露 |
| `GetRangeStatisticsAsync` | 获取时间范围统计 | Line 352-397 | ❌ 未暴露 |

---

## ⚠️ MedicalCaseController中的处方功能

### 有限的处方查询端点

**端点**: `GET /api/v1/medicalcases/{medicalCaseId}/prescriptions`
**位置**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs:407-423`

```csharp
[HttpGet("{medicalCaseId}/prescriptions")]
public async Task<ActionResult<ApiResponse<List<PrescriptionDetailDto>>>> GetPrescriptionList(
    Guid medicalCaseId)
```

**功能限制**:
- ❌ 只能查询**特定病案**的处方
- ❌ 无法跨病案查询患者的所有历史处方
- ❌ 无法按病症关键词搜索处方

**结论**: MedicalCase中**没有**实现跨病案的处方查询功能

---

## 🔍 ConsultationController参考模板

### 当前已暴露的Consultation端点（共4个）

| 端点 | 方法 | 功能 | 对应Service方法 |
|-----|------|------|----------------|
| `GET /api/v1/consultations` | GetConsultations | 分页查询诊疗记录 | `GetPagedAsync` |
| `GET /api/v1/consultations/{id}` | GetById | 获取诊疗详情 | `GetByIdAsync` |
| `GET /api/v1/consultations/medicalcase/{id}` | GetByMedicalCaseId | 按病案ID查询 | `GetByMedicalCaseIdAsync` |
| `GET /api/v1/consultations/search` | Search | 关键词搜索 | `SearchAsync` |

**ConsultationService已实现方法（共4个）**:
1. `GetPagedAsync` - 分页查询（Line 32-62）
2. `GetByIdAsync` - 按ID查询（Line 64-85）
3. `GetByMedicalCaseIdAsync` - 按病案ID查询（Line 91-114）
4. `SearchAsync` - 关键词搜索（Line 116-132）

**暴露率**: 4/4 = **100%**（所有方法都已暴露）

---

## 📊 Prescription vs Consultation 对比分析

### Service层方法对比

| 功能类型 | ConsultationService | PrescriptionService | 差异说明 |
|---------|-------------------|-------------------|---------|
| **基础查询** | 4个方法 | **10+个方法** | Prescription功能更丰富 |
| 分页查询 | ✅ GetPagedAsync | ✅ GetPagedAsync（**增强**：支持日期范围） | Prescription支持日期过滤 |
| ID查询 | ✅ GetByIdAsync | ✅ GetByIdAsync | 功能相同 |
| 病案ID查询 | ✅ GetByMedicalCaseIdAsync | ✅ GetByMedicalCaseIdAsync | 功能相同 |
| 关键词搜索 | ✅ SearchAsync | ✅ SearchPrescriptionsAsync（**增强**） | Prescription支持患者+病症组合搜索 |
| **高级查询** | ❌ 无 | ✅ GetPatientRecentPrescriptionsAsync | **Prescription独有** |
| **统计功能** | ❌ 无 | ✅ GetStatisticsAsync, GetRangeStatisticsAsync | **Prescription独有** |
| **辅助功能** | ❌ 无 | ✅ RecalculatePriceAsync, GeneratePrintFormatAsync | **Prescription独有** |

### Controller层端点对比

| 模块 | 已暴露端点 | 未暴露方法 | 暴露率 |
|-----|----------|----------|-------|
| **Consultation** | 4个 | 0个 | **100%** |
| **Prescription** | 0个 | 10+个 | **0%** |

**结论**:
- ✅ ConsultationController已完整暴露所有Service方法
- ❌ PrescriptionsController完全空白，所有Service方法未暴露

---

## 🎯 迁移建议

### 核心迁移任务

#### Phase 1: 迁移基础查询端点（对齐ConsultationController）

参考ConsultationController，在PrescriptionsController中添加4个基础端点：

| 端点路径 | HTTP方法 | 功能 | 对应Service方法 |
|---------|---------|------|----------------|
| `/api/v1/prescriptions` | GET | 分页查询处方 | `GetPagedAsync` |
| `/api/v1/prescriptions/{id}` | GET | 获取处方详情 | `GetByIdAsync` |
| `/api/v1/prescriptions/medicalcase/{id}` | GET | 按病案ID查询 | `GetByMedicalCaseIdAsync` |
| `/api/v1/prescriptions/search` | GET | 关键词搜索 | ~~SearchAsync~~ 改用 **SearchPrescriptionsAsync** |

**注意**:
- ✅ 使用`SearchPrescriptionsAsync`替代简单的`SearchAsync`，支持患者+病症组合搜索
- ✅ 这4个端点与用户需求**REQ-2**（按病症查询）**直接相关**

---

#### Phase 2: 添加Prescription独有的高级端点（满足用户需求）

| 端点路径 | HTTP方法 | 功能 | 对应Service方法 | 满足需求 |
|---------|---------|------|----------------|---------|
| `/api/v1/prescriptions/patient/{patientId}/recent` | GET | 获取患者最近处方 | `GetPatientRecentPrescriptionsAsync` | **REQ-1** ✅ |
| `/api/v1/prescriptions/statistics` | GET | 获取处方统计 | `GetStatisticsAsync` | 数据分析 |
| `/api/v1/prescriptions/statistics/range` | GET | 时间范围统计 | `GetRangeStatisticsAsync` | 数据分析 |
| `/api/v1/prescriptions/{id}/recalculate-price` | POST | 重新计算价格 | `RecalculatePriceAsync` | 辅助功能 |
| `/api/v1/prescriptions/{id}/print` | GET | 生成打印格式 | `GeneratePrintFormatAsync` | 辅助功能 |

**核心价值**:
- ✅ `/patient/{patientId}/recent` 端点直接满足**REQ-1**（按患者查询历史处方）
- ✅ `/search` 端点（Phase 1）配合`symptomKeyword`参数满足**REQ-2**（按病症查询）

---

#### Phase 3: 评估"复制"和"转存"功能（待实施）

| 功能 | 需求编号 | 实现状态 | 建议方案 |
|-----|---------|---------|---------|
| 历史处方复制到当前处方 | REQ-3 | ❌ 未实现 | 在MedicalCaseService中添加`CopyPrescriptionAsync`方法 |
| 历史处方转存成验方 | REQ-4 | ❌ 未实现 | 需要新增Formula模块（MVP阶段可延后） |

**实施建议**:
- **REQ-3（复制处方）**:
  - 端点：`POST /api/v1/medicalcases/{id}/prescriptions/copy-from/{sourcePrescriptionId}`
  - 归属：MedicalCaseController（写操作，通过聚合根）
  - 实现：MedicalCaseService.CopyPrescriptionAsync

- **REQ-4（转存验方）**:
  - 业务价值：将有效处方保存为可复用的经验方
  - MVP建议：延后到Formula模块实施（当前Formula功能尚未深化）
  - 临时方案：通过REQ-3"复制处方"功能部分满足需求

---

## ✅ 功能满足情况总结

| 用户需求 | 实现状态 | 满足方式 | 优先级 |
|---------|---------|---------|-------|
| **REQ-1**: 按患者查询处方 | ✅ 已实现 | `GetPatientRecentPrescriptionsAsync` → 需暴露端点 | 🔴 P0 |
| **REQ-2**: 按病症查询处方 | ✅ 已实现 | `SearchPrescriptionsAsync` → 需暴露端点 | 🔴 P0 |
| **REQ-3**: 历史处方复制 | ❌ 未实现 | 需在MedicalCaseService中新增方法 | 🟡 P1 |
| **REQ-4**: 转存验方 | ❌ 未实现 | 需Formula模块支持（MVP延后） | 🟢 P2 |

**当前优先级**:
1. **P0（紧急）**: 暴露PrescriptionService已有的查询方法（Phase 1 + Phase 2的第一个端点）
2. **P1（重要）**: 实现REQ-3"复制处方"功能
3. **P2（可延后）**: REQ-4"转存验方"等待Formula模块完善

---

## 🏗️ 架构合规性检查

### AR-001: 聚合根约束验证

| 操作类型 | 端点归属 | 是否符合AR-001 |
|---------|---------|---------------|
| **读操作** | PrescriptionsController | ✅ 合规（AR-001允许读操作绕过聚合根） |
| **写操作** | MedicalCaseController | ✅ 合规（通过聚合根执行） |

**结论**:
- ✅ PrescriptionsController添加只读端点**完全合规**
- ✅ REQ-3"复制处方"应归属MedicalCaseController（写操作）

---

## 📝 实施步骤建议

### Step 1: 修正需求文档（当前任务）
- 将"删除PrescriptionsController"改为"实施PrescriptionsController"
- 明确需要暴露的10+个端点清单
- 识别REQ-3/REQ-4需新增的功能

### Step 2: 创建设计文档
- 详细定义每个端点的路由、参数、响应格式
- 明确Phase拆分（Phase 1基础端点 → Phase 2高级端点 → Phase 3新功能）
- 评估是否需要DTO调整

### Step 3: 任务分解
- 使用`lybtzyzs-task-breakdown` Skill生成task文档
- 估算工作量（预计4-6小时完成Phase 1+2）

### Step 4: 批量创建Issues
- 使用`lybtzyzs-issue-template` Skill批量创建GitHub Issues
- 关联Epic #1600（Server端重构）

---

## 🎯 结论

### ✅ 核心发现
1. **PrescriptionService功能完备**：10+个查询方法已实现，覆盖用户需求REQ-1和REQ-2
2. **PrescriptionsController完全空白**：0%暴露率，急需补充
3. **MedicalCase无跨病案查询**：只能查询单个病案的处方，无法满足"按患者查询历史处方"需求

### ✅ 迁移价值
- **无需编写新Service代码**：PrescriptionService已实现所有查询逻辑
- **只需添加Controller端点**：工作量小，风险低
- **立即满足用户需求**：REQ-1和REQ-2可快速交付

### ⚠️ 后续工作
- REQ-3（复制处方）需新增MedicalCaseService方法
- REQ-4（转存验方）依赖Formula模块，MVP阶段可延后

---

**报告生成者**: Claude Code
**下一步**: 等待用户确认，然后修正需求文档并生成设计文档
