# Desktop 批量导入/导出 UI 接线报告（2026-08-19）

> 任务书：`.hermes-task-batch-import-export-ui.md`（已删除）
> 范围：Patient/Herb/Formula/User 4 模块批量导入/导出 UI 接线（ViewModel + View），不改 Service/Repository 层
> 前置：desktop-doc-verification-2026-08-19.md 验证报告（发现导入/导出 API 完整但 ViewModel 零消费 + 2 处 XAML 死绑定）

---

## 一、实现总览

| 模块 | ViewModel | View | 新增命令 | 结果 |
|------|-----------|------|---------|------|
| 患者 | PatientMasterDetailViewModel | PatientMasterDetailControl.xaml | ImportPatients / ExportPatients / DownloadImportTemplate | ✅ 接线完成 |
| 药材 | HerbMasterDetailViewModel | HerbMasterDetailControl.xaml | ImportHerbs / ExportHerbs / DownloadImportTemplate | ✅ 接线完成 + 死绑定修复 |
| 验方 | FormulaMasterDetailViewModel | FormulaMasterDetailControl.xaml | ImportFormulas / ExportFormulas / DownloadImportTemplate | ✅ 接线完成 |
| 用户 | — | UserMasterDetailControl.xaml | —（无导出 API） | ⚠️ 删除死绑定，导出未接线 |

## 二、各模块实现详情

### 1. 患者模块（US-PAT-011/012）

**ViewModel**（PatientMasterDetailViewModel.cs）新增 3 命令：
- `DownloadImportTemplateCommand` → `_patientService.ExportTemplateAsync()` → SaveFileDialog 保存 JSON
- `ImportPatientsCommand` → OpenFileDialog 选 .json → ReadAllText → 反序列化 `PatientBatchImportInputDto`（camelCase+枚举字符串，ADR-0022 对齐）→ 确认框（显示条数+策略）→ `_patientService.BatchImportAsync` → 结果对话框（成功/失败/跳过）→ 刷新列表+缓存失效
- `ExportPatientsCommand` → `_patientService.ExportPatientsAsync(SearchText)`（带当前搜索条件）→ SaveFileDialog 保存

**View**：DataGridToolbar 加 `ExportCommand="{Binding ExportPatientsCommand}"`；AdditionalContent 加「模板」「导入」2 按钮（Outlined 风格，与现有编辑按钮一致）。

### 2. 药材模块（US-HERB-006/007/013）

**ViewModel**（HerbMasterDetailViewModel.cs）新增 3 命令（同患者模式，走 `_herbService`）：
- `DownloadImportTemplateCommand` → `ExportTemplateAsync`
- `ImportHerbsCommand` → `BatchImportAsync`（反序列化 `HerbBatchImportInputDto`）
- `ExportHerbsCommand` → `ExportHerbsAsync(SearchText)`

**View**：**修复死绑定**——`ImportHerbsCommand`/`ExportHerbsCommand` 原绑定不存在的命令（按钮点击无效），现命令已实现；AdditionalContent 加「模板」按钮。

### 3. 验方模块（US-FORM-006/013）

**ViewModel**（FormulaMasterDetailViewModel.cs）新增 3 命令（同患者模式，走 `_formulaService`）：
- `DownloadImportTemplateCommand` → `ExportTemplateAsync`
- `ImportFormulasCommand` → `BatchImportAsync`（反序列化 `FormulaBatchImportInputDto`；结果含 MatchedHerbsCount 匹配药材数）
- `ExportFormulasCommand` → `ExportFormulasAsync()`

**View**：DataGridToolbar 加 ExportCommand + AdditionalContent 加「模板」「导入」2 按钮。

### 4. 用户模块（US-USER-012）

**约束冲突处理**：任务书要求 `UserService.ExportUsersAsync`——但 **IUserService/UserRepository 无任何导出方法**（代码零命中），且约束「不改 Service/Repository 层 + 使用现有 API 接口」。**用户模块没有导出 API 可接线**。

**处理**：删除 `UserMasterDetailControl.xaml` 的 `ExportCommand="{Binding ExportCommand}"` 死绑定（该命令不存在，DataGridToolbar 的导出按钮因此隐藏——行为等价清理）。用户导出需新增 API（属后续任务，登记 backlog）。

## 三、通用实现模式

每个模块的导入流程（统一遵循任务书模式）：

```
1. OpenFileDialog（Filter: JSON 文件|*.json）→ 选择 .json 文件
2. ReadAllTextAsync → JsonSerializer.Deserialize<XxxBatchImportInputDto>
   （PropertyNameCaseInsensitive + JsonStringEnumConverter——ADR-0022 对齐）
3. 空数据校验（Patients/Herbs/Formulas 空 → 提示格式错误）
4. ShowConfirmAsync（显示条数 + 重复策略）→ 用户确认
5. Service.BatchImportAsync(dto)
6. ShowSuccessAsync（成功/失败/跳过计数）+ InvalidateCaches + RefreshAsync
```

每个模块的导出流程：

```
1. Service.ExportXxxAsync()（患者/药材带当前 SearchText 筛选）
2. 失败 → ShowErrorAsync（安全消息映射 GetSafeOperationFailureMessage）
3. SaveFileDialog（Filter: JSON 文件|*.json，默认名 实体导出_时间戳.json）
4. File.WriteAllBytesAsync → ShowSuccessAsync（保存路径 + 字节数）
```

**错误处理**：JsonException 单独捕获（提示「文件格式错误，请使用下载的 JSON 模板」）；通用异常用 `ClientErrorMessageMapper.GetSafeOperationFailureMessage`（安全消息，不泄露内部异常）。

**双模式兼容**：全部经 Service 接口调用（IPatientService/IHerbService/IFormulaService）→ 底层 SwitchingApiClient 自动路由 Remote/Local，无需额外处理。

## 四、验证

| 项 | 结果 |
|----|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | ✅ 0 错误 0 警告 |
| MasterDetail VM 相关测试（Patient/Herb/Formula/User） | ✅ 57/57 |
| 追溯矩阵更新 | ✅ v1.8 → v1.9（PAT-011/012、HERB-006/007/013、FORM-006/013 Desktop ⚠️→✅） |

## 五、遗留登记

| # | 项 | 说明 |
|---|----|------|
| 1 | **用户导出未接线** | IUserService/UserRepository 无导出 API（任务书假设不成立）——需新增 API 后接线，或按「用户数据导出非核心需求」评估是否跳过 |
| 2 | **验方导出不带筛选** | FormulaService.ExportFormulasAsync 支持 category 参数，当前 UI 传 null（导出全部）——如需按当前筛选导出可后续增强 |
| 3 | **导入文件格式依赖模板** | 用户需先下载模板了解 JSON 结构（模板字段说明 + DTO 形状一致）；已通过错误提示引导 |

---

*完成时间: 2026-08-19*
*约束遵守: 不改 Service/Repository ✅ / 使用现有 API ✅ / 双模式兼容 ✅ / 错误处理 ✅ / UI 风格一致（Outlined + PackIcon）✅*
