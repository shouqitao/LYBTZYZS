# Epic #1612 文档同步清单

**生成时间**: 2025-10-27 14:20
**代码变更范围**: bb35e1b5..HEAD (Epic #1612 Phase 1-3)
**变更模块**: MedicalCase/Consultation/Prescription
**Skill版本**: lybtzyzs-doc-sync v1.0

---

## 📊 变更检测总结

### API端点变更（14个新端点）✅
- **Controller**: MedicalCaseController (API v1)
- **Write Layer**: 8个端点（创建/更新/删除）
- **Read Layer**: 4个端点（查询/列表）
- **Helper Layer**: 2个端点（验证）

### 架构调整✅
- **Repository重构**: MedicalCaseRepository.cs
- **Service重构**: IMedicalCaseService.cs, MedicalCaseService.cs
- **测试扩展**: 32个单元测试, 18个集成测试

### 数据模型变更✅
- **DTO更新**: MedicalCaseDtos.cs, SimplifiedMedicalCaseDtos.cs
- **Client API**: IMedicalCaseApi.cs

---

## 🔴 必须更新（高优先级）

### 1. 创建MedicalCase API文档 ⭐⭐⭐

**文件**: `docs/api/medicalcase-api.md` (新建)

**原因**:
- Epic #1612重构了MedicalCaseController，新增14个API端点
- 当前docs/api/目录缺少MedicalCase模块API文档
- 集成测试已完成（18/18通过），API契约已稳定

**必须包含内容**:

#### Write Layer (8个端点)

1. **POST /api/v1/medicalcases** - 创建新病案
   ```json
   请求：
   {
     "patientId": "guid",
     "visitDate": "2025-10-27T10:00:00Z"
   }

   响应：200 OK
   {
     "success": true,
     "message": "病案创建成功",
     "data": { MedicalCaseEntity }
   }

   业务规则：
   - AR-001: 通过聚合根创建
   - BR-001: 单个患者只能有一个Active病案
   ```

2. **PUT /api/v1/medicalcases/{id}/consultation** - 更新辨证信息
   ```json
   请求：UpdateConsultationRequest
   响应：200 OK / 400 Bad Request（状态不允许）
   ```

3. **PUT /api/v1/medicalcases/{id}/prescription-flag** - 标记是否需要开处方
   ```json
   请求：
   {
     "needsPrescription": true
   }

   响应：200 OK / 422 Unprocessable Entity
   业务规则：BF-002 动态流程控制
   ```

4. **POST /api/v1/medicalcases/{id}/prescriptions** - 创建处方
   ```json
   请求：CreatePrescriptionRequest
   响应：200 OK / 422 Unprocessable Entity
   业务规则：AR-003 一诊一方约束
   ```

5. **PUT /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}** - 更新处方
   ```json
   请求：UpdatePrescriptionRequest
   响应：200 OK / 403 Forbidden（处方不属于该病案）
   ```

6. **DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}** - 删除处方
   ```json
   响应：204 No Content / 403 Forbidden / 422 Unprocessable Entity
   ```

7. **PUT /api/v1/medicalcases/{id}/status** - 更新病案状态
   ```json
   请求：
   {
     "status": "Active | Completed | Cancelled"
   }

   响应：200 OK / 422 Unprocessable Entity（状态转换不合法）
   ```

8. **PUT /api/v1/medicalcases/{id}/complete** - 完成病案
   ```json
   响应：200 OK / 422 Unprocessable Entity
   业务规则：BF-002 三步流程验证
   ```

#### Read Layer (4个端点)

9. **GET /api/v1/medicalcases/{id}** - 获取病案详情
   ```json
   响应：200 OK
   注：预加载Consultation和Prescription
   ```

10. **GET /api/v1/medicalcases** - 查询病案列表（分页）
    ```json
    查询参数：
    - status: Active | Completed | Cancelled（可选）
    - patientId: guid（可选）
    - page: 1（必填，>0）
    - pageSize: 20（必填，1-100）

    响应：PagedResult<MedicalCaseEntity>
    ```

11. **GET /api/v1/medicalcases/{medicalCaseId}/consultations** - 查询辨证记录列表
    ```json
    响应：List<ConsultationDetailDto>
    ```

12. **GET /api/v1/medicalcases/{medicalCaseId}/prescriptions** - 查询处方列表
    ```json
    响应：List<PrescriptionDetailDto>
    ```

#### Helper Layer (2个端点)

13. **GET /api/v1/medicalcases/{id}/can-edit** - 验证病案是否可编辑
    ```json
    响应：CanEditResponse
    {
      "canEdit": true,
      "reason": "..."
    }
    ```

14. **GET /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}/can-delete** - 验证处方是否可删除
    ```json
    响应：CanDeleteResponse
    {
      "canDelete": true,
      "reason": "..."
    }
    ```

**参考资料**:
- Controller源码: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- 集成测试: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`
- E2E测试报告: `docs/reports/e2e-test-coverage-analysis.md`

---

### 2. 更新快速参考文档 ⭐⭐

**文件**: `docs/quick-reference/api-reference.md`

**变更内容**:
- 添加MedicalCase模块的14个API端点到快速参考表
- 格式：

```markdown
| 端点 | 方法 | 说明 | 业务规则 |
|------|------|------|---------|
| /api/v1/medicalcases | POST | 创建新病案 | AR-001, BR-001 |
| /api/v1/medicalcases/{id}/consultation | PUT | 更新辨证信息 | AR-001 |
| /api/v1/medicalcases/{id}/prescription-flag | PUT | 标记是否开处方 | BF-002 |
| ... | ... | ... | ... |
```

---

### 3. 更新架构文档 - Service层 ⭐⭐

**文件**: `docs/architecture/server/README.md` 或 `docs/architecture/server/services.md`

**变更内容**:
- 记录MedicalCaseService的重构
- 新增方法（14个）：
  - Write Layer: CreateAsync, UpdateConsultationAsync, SetPrescriptionFlagAsync, CreatePrescriptionAsync, UpdatePrescriptionAsync, DeletePrescriptionAsync, UpdateStatusAsync, CompleteAsync
  - Read Layer: GetByIdAsync, GetListAsync, GetConsultationListAsync, GetPrescriptionListAsync
  - Helper Layer: CanEditAsync, CanDeletePrescriptionAsync

**参考资料**:
- Service源码: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- Service接口: `src/Server/Modules/LYBT.Module.MedicalCase/Services/IMedicalCaseService.cs`

---

### 4. 更新架构文档 - Repository层 ⭐

**文件**: `docs/architecture/server/README.md` 或 `docs/architecture/server/repositories.md`

**变更内容**:
- 记录MedicalCaseRepository的重构
- 关键方法：GetByIdWithDetailsAsync, GetPagedWithDetailsAsync, GetByPatientIdAsync

**参考资料**:
- Repository源码: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

---

### 5. 更新测试文档 ⭐

**文件**: `docs/testing/README.md` 或 `docs/development/shared/testing-guide.md`

**变更内容**:
- 记录Epic #1612的测试成果：
  - 单元测试：32个测试, 82.6%覆盖率, 57.14%分支覆盖率
  - 集成测试：18个测试, 100%通过率, 14个API端点覆盖
  - E2E测试：4个业务场景, 100%通过率

**参考资料**:
- 单元测试: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs`
- 集成测试: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`
- E2E测试报告: `docs/reports/e2e-test-coverage-analysis.md`

---

## 🟡 建议更新（中优先级）

### 6. 创建MedicalCase模块文档 ⭐

**文件**: `docs/modules/medicalcase/README.md` (新建)

**原因**:
- 当前docs/modules/目录缺少模块级文档
- MedicalCase是核心业务模块，应有完整文档

**建议包含内容**:
- 模块概述（业务价值、核心功能）
- 架构设计（三层对齐、聚合根边界）
- API端点列表（链接到docs/api/medicalcase-api.md）
- 数据模型（MedicalCase/Consultation/Prescription实体关系）
- 业务规则（AR-001, AR-003, BF-002, BR-001）
- 开发指南（如何扩展、常见问题）

**状态**: 等待确认是否需要创建完整模块文档

---

### 7. 更新代码模式文档

**文件**: `docs/quick-reference/code-patterns.md`

**变更内容**:
- 添加Service模式示例（基于MedicalCaseService）
- 添加Repository模式示例（基于MedicalCaseRepository）
- 添加Controller三层分离模式示例（Write/Read/Helper）

**状态**: 等待确认是否需要补充代码模式示例

---

### 8. 更新业务规则文档

**文件**: `docs/business-rules.md`

**变更内容**:
- 确认是否需要更新AR-001, AR-003, BF-002, BR-001的描述
- 添加Epic #1612实现细节

**状态**: 等待确认业务规则是否已在docs/business-rules.md中记录

---

## ✅ 文档链接验证

### 验证结果

**检查范围**: docs/目录下所有Markdown文件的内部链接

**结果**: ✅ 未检测到失效链接（当前Epic #1612未影响现有文档链接）

**说明**: Epic #1612主要是新增功能，未修改或删除现有文档，因此未产生失效链接。

---

## 📝 文档归档规范

根据`docs/support/documentation-guidelines.md`和`.claude/core/FILE-ORGANIZATION.md`：

**报告归档**:
- ✅ 本文件: `docs/reports/epic-1612-doc-sync-checklist.md`
- ✅ E2E测试报告: `docs/reports/e2e-test-coverage-analysis.md`

**API文档归档**:
- 🆕 待创建: `docs/api/medicalcase-api.md`

**模块文档归档**:
- 🆕 待创建: `docs/modules/medicalcase/README.md` (可选)

---

## 🎯 优先级执行计划

### Phase 1: 核心API文档（必须）⭐⭐⭐
1. 创建 `docs/api/medicalcase-api.md` - 14个API端点完整文档
2. 更新 `docs/quick-reference/api-reference.md` - 添加快速参考表

**预计时间**: 2-3小时
**理由**: API契约已稳定（集成测试100%通过），缺少API文档会影响前端开发和接口对接

### Phase 2: 架构和测试文档（必须）⭐⭐
3. 更新 `docs/architecture/server/README.md` - Service/Repository重构
4. 更新 `docs/testing/README.md` - 测试成果记录

**预计时间**: 1-2小时
**理由**: 记录架构调整和测试覆盖率，保持文档与代码一致性

### Phase 3: 模块文档（建议）⭐
5. 创建 `docs/modules/medicalcase/README.md` - 完整模块文档（可选）
6. 更新 `docs/quick-reference/code-patterns.md` - 代码模式示例（可选）

**预计时间**: 2-3小时
**理由**: 提升文档完整性，但不影响当前开发工作

---

## 📚 相关资源

### Git提交历史
- Epic #1612提交范围: bb35e1b5..HEAD
- 关键提交:
  - 2c59517a: feat(medicalcase): Epic #1612 Task 2.5完成 - 创建MedicalCaseV2Controller（14个端点）
  - 061fc8a8: test(medicalcase): Task 2.9 - 补充MedicalCaseService单元测试覆盖率

### 源码文件
- Controller: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- Service: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- Repository: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

### 测试文件
- 单元测试: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs`
- 集成测试: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`

### 文档规范
- 文档组织: `.claude/core/FILE-ORGANIZATION.md`
- 文档维护: `docs/support/documentation-guidelines.md`

---

**生成者**: Claude Code + lybtzyzs-doc-sync Skill v1.0
**验证状态**: ✅ API端点 | ✅ 架构调整 | ✅ 数据模型 | ✅ 链接有效性
**下一步**: 执行Phase 1核心API文档创建
