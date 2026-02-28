# Sprint 5 Batch 5 Progress

## Session: 2026-02-28

### Actions
1. 读取 8 个目标文件 + 4 个参考文件
2. Task 1+2+3 并行执行（HerbService 拼音码 + Validator 修复）
3. Task 4 执行（FormulaRepository + FormulaImportExportService 导出增强）
4. Task 5+6 并行执行（IFormulaDataSource + Local/Remote 实现）
5. 编译验证: 0 errors, 4 warnings (已有的 UserMapper RMG012)
6. 测试验证: 592 + 638 + 74 = 1304 tests passed

### Modified Files
| File | Change |
|------|--------|
| `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` | CreateAsync + UpdateAsync 拼音码逻辑 |
| `src/Shared/LYBT.Shared.Validators/Formula/FormulaInputDtoValidator.cs` | Herbs 强制非空校验 |
| `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaRepository.cs` | +GetAllWithHerbsAsync |
| `src/Server/Modules/LYBT.Module.Formula/Repositories/FormulaRepository.cs` | +GetAllWithHerbsAsync 实现 |
| `src/Server/Modules/LYBT.Module.Formula/Services/FormulaImportExportService.cs` | ExportAsync 药材组成 Sheet |
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/DataSources/IFormulaDataSource.cs` | +BatchToggleStatusAsync, +GetImportTemplate*Columns |
| `src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalFormulaDataSource.cs` | +BatchToggleStatusAsync, +GetImportTemplate*Columns |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteFormulaDataSource.cs` | +BatchToggleStatusAsync, +GetImportTemplate*Columns |

### Test Results
- LYBT.Tests.Unit: 592 passed
- LYBT.Tests.Desktop.Unit: 638 passed
- LYBT.Tests.Architecture: 74 passed
- Total: 1304 passed, 0 failed
