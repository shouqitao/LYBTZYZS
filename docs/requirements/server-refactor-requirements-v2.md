# Server端重构需求文档 v2.0

**创建时间**: 2025-10-27
**版本**: v2.0 (MVP范围界定版)
**原则**: 有需求才有代码，不过度设计，不反复定义

---

## 📋 核心原则

### 1. MVP范围界定
- ✅ **有用户需求才保留代码**
- ❌ **删除所有超前设计的功能**
- ❌ **删除没有需求支撑的API**
- ✅ **够用即好，需要时再开发**

### 2. 聚合根约束加强
- ✅ **Repository改为internal**（防止绕过聚合根）
- ✅ **只读API可独立暴露**（符合AR-001）
- ✅ **写操作必须通过MedicalCase聚合根**

---

## 🎯 用户明确的功能需求

基于用户反馈，当前明确的处方相关需求：

| 需求编号 | 功能描述 | 业务价值 | 优先级 |
|---------|---------|---------|--------|
| **REQ-1** | 按照患者查询处方 | 查看患者历史用药记录 | 🔴 P0 |
| **REQ-2** | 按照病症查询处方 | 参考相似病症的治疗方案 | 🔴 P0 |
| **REQ-3** | 历史处方复制到当前处方 | 复诊时沿用有效方剂 | ✅ **前端实现** |
| **REQ-4** | 历史处方转存成验方 | 将有效处方保存为经验方（基于历史处方编辑） | 🟢 P2 |

**说明**:
- P0需求：当前MVP必须实现（后端API）
- REQ-3：通过前端ViewModel实现，无需新增后端API
- P2需求：依赖Formula模块，MVP阶段延后

---

## 📊 现状分析：代码 vs 需求映射

### PrescriptionService - 方法清单与需求映射

| 方法名 | 代码行 | 对应需求 | MVP判定 |
|-------|--------|---------|---------|
| `GetPagedAsync` | 55-98 | ❌ 无需求 | 🗑️ **删除**（超前设计） |
| `GetByIdAsync` | 100-117 | ✅ 查看处方详情（隐含需求） | ✅ **保留** |
| `GetByMedicalCaseIdAsync` | 123-140 | ✅ 查看病案的处方（隐含需求） | ✅ **保留** |
| `SearchPrescriptionsAsync` | 406-504 | ✅ **REQ-2** | ✅ **保留** |
| `GetPatientRecentPrescriptionsAsync` | 513-599 | ✅ **REQ-1** | ✅ **保留** |
| `RecalculatePriceAsync` | 169-194 | ❌ 无需求 | 🗑️ **删除**（超前设计） |
| `GeneratePrintFormatAsync` | 201-217 | ❌ 无需求 | 🗑️ **删除**（超前设计） |
| `GeneratePrescriptionNoAsync` | 278-303 | ❌ 无需求 | 🗑️ **删除**（超前设计） |
| `GetStatisticsAsync` | 308-347 | ❌ 无需求 | 🗑️ **删除**（超前设计） |
| `GetRangeStatisticsAsync` | 352-397 | ❌ 无需求 | 🗑️ **删除**（超前设计） |

**保留方法**: 4个（GetByIdAsync、GetByMedicalCaseIdAsync、SearchPrescriptionsAsync、GetPatientRecentPrescriptionsAsync）
**删除方法**: 6个（超前设计）

---

### ConsultationService - 方法清单与需求映射

| 方法名 | 代码行 | 对应需求 | MVP判定 |
|-------|--------|---------|---------|
| `GetPagedAsync` | 32-62 | ❌ 无需求 | 🗑️ **删除**（超前设计） |
| `GetByIdAsync` | 64-85 | ✅ 查看诊疗详情（隐含需求） | ✅ **保留** |
| `GetByMedicalCaseIdAsync` | 91-114 | ✅ 查看病案的诊疗记录（隐含需求） | ✅ **保留** |
| `SearchAsync` | 116-132 | ❌ 无需求 | 🗑️ **删除**（超前设计） |

**保留方法**: 2个（GetByIdAsync、GetByMedicalCaseIdAsync）
**删除方法**: 2个（超前设计）

---

### ConsultationController - 端点清单与需求映射

| 端点路径 | 对应Service方法 | 对应需求 | MVP判定 |
|---------|----------------|---------|---------|
| `GET /consultations` | GetPagedAsync | ❌ 无需求 | 🗑️ **删除** |
| `GET /consultations/{id}` | GetByIdAsync | ✅ 查看详情 | ✅ **保留** |
| `GET /consultations/medicalcase/{id}` | GetByMedicalCaseIdAsync | ✅ 查看病案诊疗 | ✅ **保留** |
| `GET /consultations/search` | SearchAsync | ❌ 无需求 | 🗑️ **删除** |

**保留端点**: 2个
**删除端点**: 2个

---

### PrescriptionsController - 需补充的端点

| 端点路径 | 对应Service方法 | 对应需求 | MVP判定 |
|---------|----------------|---------|---------|
| `GET /prescriptions/{id}` | GetByIdAsync | ✅ 查看处方详情 | ✅ **新增** |
| `GET /prescriptions/medicalcase/{id}` | GetByMedicalCaseIdAsync | ✅ 查看病案处方 | ✅ **新增** |
| `GET /prescriptions/search` | SearchPrescriptionsAsync | ✅ **REQ-2** | ✅ **新增** |
| `GET /prescriptions/patient/{patientId}/recent` | GetPatientRecentPrescriptionsAsync | ✅ **REQ-1** | ✅ **新增** |

**新增端点**: 4个（全部基于现有Service方法）

---

## 🎯 重构范围清单

### Phase 1: 删除超前设计的代码（清理阶段）

#### 1.1 删除PrescriptionService中的超前设计方法

**删除清单** (6个方法，约350行代码):
- [ ] `GetPagedAsync` - 分页查询（Line 55-98）
- [ ] `RecalculatePriceAsync` - 价格计算（Line 169-194）
- [ ] `GeneratePrintFormatAsync` - 打印格式（Line 201-217）
- [ ] `GeneratePrescriptionNoAsync` - 生成处方号（Line 278-303）
- [ ] `GetStatisticsAsync` - 统计数据（Line 308-347）
- [ ] `GetRangeStatisticsAsync` - 范围统计（Line 352-397）

**验收标准**:
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 接口IPrescriptionService同步删除对应方法签名
- ✅ 如有调用方，一并删除或调整

#### 1.2 删除ConsultationService中的超前设计方法

**删除清单** (2个方法，约100行代码):
- [ ] `GetPagedAsync` - 分页查询（Line 32-62）
- [ ] `SearchAsync` - 搜索功能（Line 116-132）

**验收标准**:
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 接口IConsultationService同步删除对应方法签名

#### 1.3 删除ConsultationController中的超前设计端点

**删除清单** (2个端点):
- [ ] `GET /consultations` - GetConsultations方法（Line 38-54）
- [ ] `GET /consultations/search` - Search方法（Line 118-136）

**验收标准**:
- ✅ 编译通过
- ✅ 只保留2个端点：GetById、GetByMedicalCaseId

---

### Phase 2: 新增PrescriptionsController端点（实施阶段）

#### 2.1 实施PrescriptionsController的4个只读端点

**新增清单**:
- [ ] `GET /prescriptions/{id}` - 获取处方详情
- [ ] `GET /prescriptions/medicalcase/{medicalCaseId}` - 查看病案的处方
- [ ] `GET /prescriptions/search` - 按病症/患者搜索处方（**REQ-2**）
- [ ] `GET /prescriptions/patient/{patientId}/recent` - 患者最近处方（**REQ-1**）

**参考模板**: ConsultationController（删除后剩余的2个端点）

**验收标准**:
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 4个端点全部可访问
- ✅ 运行时验证：启动WebAPI，调用端点返回正确数据

---

### Phase 3: Repository改为internal（约束加强）

#### 3.1 调整Repository可见性

**修改清单**:
- [ ] `ConsultationRepository` - public → internal
- [ ] `PrescriptionRepository` - public → internal
- [ ] 其他7个Repository（如有public） - public → internal

**影响分析**:
- ✅ Controller通过Service访问（不受影响）
- ✅ Service通过DI注入Repository（不受影响）
- ❌ 禁止Controller直接访问Repository（强制约束）

**验收标准**:
- ✅ 编译通过
- ✅ 所有Repository类标记为`internal class`
- ✅ 所有Repository接口保持`public interface`（DI需要）

---

### REQ-3实现说明：前端ViewModel方案

**实现方式**: 通过前端ViewModel实现，无需新增后端API

**实现流程**:
1. 前端调用 `GET /prescriptions/patient/{patientId}/recent` 获取历史处方列表
2. 用户选择某个历史处方
3. ViewModel加载历史处方数据到表单（包括药材明细）
4. 用户在UI上调整（剂数、药材等）
5. 前端调用 `POST /medicalcases/{id}/prescriptions` 保存新处方

**优点**:
- ✅ 0个新API（完全复用现有API）
- ✅ 更好的用户体验（所见即所得）
- ✅ 符合MVP原则（够用即好）
- ✅ 前端控制权更大（可实现撤销、对比等功能）

**涉及的现有API**:
- `GET /prescriptions/patient/{patientId}/recent` - 获取历史处方列表
- `GET /prescriptions/{id}` - 获取处方详情（含药材明细）
- `POST /medicalcases/{id}/prescriptions` - 创建新处方

---

## 📉 代码删除统计

| 模块 | 删除方法数 | 删除端点数 | 删除代码行数（估算） |
|-----|-----------|-----------|-------------------|
| PrescriptionService | 6个 | - | ~350行 |
| ConsultationService | 2个 | - | ~100行 |
| ConsultationController | - | 2个 | ~50行 |
| **合计** | **8个方法** | **2个端点** | **~500行** |

**新增代码行数（估算）**:
- PrescriptionsController: ~150行（4个端点）
- **合计**: ~150行

**净删除**: ~350行代码

---

## ✅ 验收标准（整体）

### 编译验证
- [ ] `dotnet build LYBT.All.sln -c Release --no-restore`
- [ ] 0 errors, 0 warnings

### 运行时验证
- [ ] 启动WebAPI + Desktop客户端
- [ ] 测试REQ-1：查询患者历史处方（`GET /prescriptions/patient/{id}/recent`）
- [ ] 测试REQ-2：按病症关键词搜索处方（`GET /prescriptions/search?symptomKeyword=xxx`）
- [ ] 验证Repository为internal（尝试直接访问应编译失败）

**注**: REQ-3和REQ-4在前端ViewModel中实现，后端只需验证现有API可用性

### 架构合规性
- [ ] 所有写操作通过MedicalCase聚合根
- [ ] 所有读操作可独立查询（符合AR-001）
- [ ] Repository不可被Controller直接访问

---

## 🚫 明确不做的事（MVP阶段）

### 延后功能（有需求时再开发）
- ❌ 处方分页列表查询（GetPagedAsync）
- ❌ 诊疗记录分页查询（GetPagedAsync）
- ❌ 处方统计功能（GetStatisticsAsync、GetRangeStatisticsAsync）
- ❌ 打印格式生成（GeneratePrintFormatAsync）
- ❌ 价格重算（RecalculatePriceAsync）
- ❌ 处方号生成（GeneratePrescriptionNoAsync）
- ❌ 诊疗记录搜索（SearchAsync）

### REQ-3和REQ-4（前端ViewModel实现）
- ✅ REQ-3（复制处方）：前端加载历史处方数据到表单，调整后保存
- ✅ REQ-4（转存验方）：前端加载历史处方数据，编辑后调用Formula API保存
- ⚠️ REQ-4前提：Formula模块需实现基础的"创建验方"API
- ❌ 不需要专门的"处方转验方"API（避免过度设计）

---

## 📋 实施顺序建议

### Step 1: 删除超前设计（1-2小时）
1. 删除PrescriptionService的6个方法 + 接口同步
2. 删除ConsultationService的2个方法 + 接口同步
3. 删除ConsultationController的2个端点
4. 编译验证

### Step 2: 新增PrescriptionsController（2-3小时）
1. 创建4个只读端点
2. 参考ConsultationController的代码模式
3. 编译验证 + 运行时验证

### Step 3: Repository改为internal（0.5小时）
1. 修改9个Repository类的可见性
2. 编译验证

**总工作量估算**: 4-6小时

---

## ✅ 已确认的实施方案（用户已批准）

### 决策点1: ConsultationController的删减范围
- ✅ **采纳方案A**: 删除2个端点（GetConsultations、Search），保留2个（GetById、GetByMedicalCaseId）
- 理由：严格遵循"有需求才有代码"原则

### 决策点2: PrescriptionService的删减范围
- ✅ **采纳方案A**: 删除6个方法（所有无需求支撑的方法）
- 理由：需要时再开发，避免超前设计

### 决策点3: Repository可见性调整的范围
- ✅ **采纳方案A**: 全部9个Repository改为internal
- 理由：统一约束，防止未来违规绕过聚合根

### REQ-3和REQ-4实施方案
- ✅ **采纳前端ViewModel方案**: 通过前端实现，无需新增后端API
- 理由：复用现有API，更好的用户体验，符合MVP原则

---

## 📝 后续文档更新

完成重构后需同步更新：
- [ ] `docs/architecture/server/README.md` - 更新Consultation/Prescription模块说明
- [ ] `docs/api/prescriptions-api.md` - 新增Prescription API文档
- [ ] `docs/api/consultation-api.md` - 更新Consultation API（删除部分端点）
- [ ] `docs/index.md` - 更新导航链接

---

## 💡 REQ-4（转存验方）补充说明

### 与REQ-3类似的前端实现逻辑

**REQ-4实现流程**:
1. 前端调用 `GET /prescriptions/patient/{patientId}/recent` 获取历史处方列表
2. 用户选择某个历史处方
3. ViewModel加载历史处方数据到"创建验方"表单
4. 用户编辑调整（修改验方名称、添加标签、调整说明等）
5. 前端调用 `POST /formulas` 保存为验方

**前提条件**:
- ⚠️ Formula模块需实现基础的"创建验方"API（`POST /formulas`）
- ⚠️ 如Formula模块尚未实现，需优先实现Formula的基础CRUD

**优点**:
- ✅ 0个新API（无需专门的"处方转验方"API）
- ✅ 符合MVP原则（够用即好）
- ✅ 前端可灵活控制转换过程

**结论**: REQ-3和REQ-4都通过前端ViewModel实现，后端只需提供基础的CRUD API

---

**生成者**: Claude Code
**版本**: v2.0（MVP范围界定版）
**下一步**: 等待用户确认3个决策点，然后生成设计文档
