# Sprint 5 Batch 5: Herb/Formula Enhancement

## Goal
完成药材拼音码自动生成、验方校验/导出/批量操作/导入模板 6 项增强。

## Phases

### Phase 1: Herb PinYin Auto-generation [complete]
- [x] T5-P2-33: HerbService.CreateAsync 拼音码自动生成
- [x] T5-P2-34: HerbService.UpdateAsync 名称变更时重新生成拼音码

### Phase 2: Formula Validator Fix [complete]
- [x] T5-P2-35: FormulaInputDtoValidator 强制 Herbs 非空校验

### Phase 3: Formula Export Enhancement [complete]
- [x] T5-P2-36: FormulaImportExportService 导出增加药材组成 Sheet
  - IFormulaRepository + GetAllWithHerbsAsync
  - FormulaRepository 实现
  - ExportAsync 添加 Sheet2

### Phase 4: Formula Desktop DataSource [complete]
- [x] T5-P2-37: IFormulaDataSource + Local/Remote BatchToggleStatusAsync
- [x] T5-P2-38: IFormulaDataSource + Local/Remote GetImportTemplate*Columns

### Phase 5: Verification [complete]
- [x] dotnet build LYBT.All.sln → 0 errors
- [x] Unit tests: 592 passed
- [x] Desktop Unit tests: 638 passed
- [x] Architecture tests: 74 passed

## Decisions
- T5-P2-36 采用 GetAllWithHerbsAsync 复用 GetBaseQuery (自带 Include Herbs)
- T5-P2-37 Local/Remote 实现分别参照 Herb 模块同名方法
- T5-P2-38 采用 string[] 返回列名数组模式，与 IHerbDataSource.GetImportTemplateColumns 一致
