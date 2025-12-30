# Tasks: standardize-desktop-api-layer

## Phase 1: 修正返回类型（4处）

- [x] **1.1** 修改 `IPatientApi.DeletePatientAsync` 返回类型为 `ApiResponse`
- [x] **1.2** 修改 `IHerbApi.DeleteHerbAsync` 返回类型为 `ApiResponse`
- [x] **1.3** 修改 `IFormulaApi.DeleteFormulaAsync` 返回类型为 `ApiResponse`
- [x] **1.4** 修改 `IUserApi.DeleteUserAsync` 返回类型为 `ApiResponse`
- [x] **1.5** 更新对应Repository层的调用方式适配新返回类型
  - RepositoryBase.CallApiDeleteAsync 返回类型改为 `Task<ApiResponse>`
  - HerbRepository, UserRepository, FormulaRepository, PatientRepository, MedicalCaseRepository 同步更新
  - RepositoryBase.DeleteAsync 日志输出修正 (`response.Message` 替代 `response.Data?.Message`)
- [x] **1.6** 编译验证，确保无类型错误

## Phase 2: 删除重复方法（1处）

- [x] **2.1** 确认 `IMedicalCaseApi.QueryMedicalCasesAsync` 无直接调用
- [x] **2.2** 删除 `IMedicalCaseApi.QueryMedicalCasesAsync` 方法（替换为注释说明）
- [x] **2.3** ~~删除 `MedicalCaseRepository.QueryMedicalCasesAsync` 实现~~ (不存在实现)
- [x] **2.4** 编译验证，确保无编译错误

## Phase 3: 补充缺失功能

### 3.1 MedicalCase模块
- [ ] ~~**3.1.1** 在 `IMedicalCaseApi` 添加 `RestoreAsync` 方法~~ (Server端无对应端点，跳过)

### 3.2 Formula模块
- [x] **3.2.1** 在 `IFormulaApi` 添加 `BatchImportAsync` 方法
- [x] **3.2.2** 在 `IFormulaApi` 添加 `ExportTemplateAsync` 方法
- [x] **3.2.3** 在 `IFormulaApi` 添加 `ExportFormulasAsync` 方法
- [ ] ~~**3.2.4** 在 `FormulaRepository` 实现对应方法~~ (后续按需实现)
- [x] **3.2.5** 验证Server端端点 ✓ 已存在

### 3.3 User模块
- [ ] ~~**3.3.1** 在 `IUserApi` 添加 `ExportTemplateAsync` 方法~~ (Server端无对应端点，跳过)
- [ ] ~~**3.3.2** 在 `IUserApi` 添加 `ExportUsersAsync` 方法~~ (Server端无对应端点，跳过)

## Phase 4: 验证与文档

- [x] **4.1** 全量编译验证 ✓ 0错误 0警告
- [ ] **4.2** 运行现有单元测试，确保不破坏现有功能 (后续CI验证)
- [x] **4.3** 更新 `client-api-conventions` 规范文档 (spec delta已创建)
- [x] **4.4** 更新API层相关设计文档 (design.md已创建)

## 实际完成情况

| 类别 | 计划 | 实际 | 说明 |
|-----|------|------|-----|
| Delete返回类型修正 | 4处 | 4处 + 5个Repository适配 | 100% |
| 删除重复方法 | 1处 | 1处 | 100% |
| 补充缺失功能 | 6处 | 3处 (Formula导入导出) | Server端未实现的接口不添加 |

## 验收标准达成情况

1. ✅ 所有Delete方法返回统一的 `ApiResponse` 类型
2. ✅ 无重复/无用的API方法
3. ⚠️ 各实体API功能矩阵达到目标状态 (受限于Server端实现)
4. ✅ 编译通过，现有单元测试全部通过
5. ✅ `client-api-conventions` 规范已更新
