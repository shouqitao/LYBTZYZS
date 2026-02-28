# Sprint 5 Batch 5 Findings

## T5-P2-33/34: Herb PinYin
- PinYinHelper.GetPinYinCode 已在 HerbService 中使用 (BatchImport/ExcelImport)，using 已导入
- CreateAsync 验证后、ToEntity 前插入拼音码逻辑
- UpdateAsync 比较 entity.Name != dto.Name 判断名称变更

## T5-P2-35: Formula Validator
- 原 `.When(x => x.Herbs != null)` 导致 Herbs=null 时跳过校验
- 改为 `.NotNull()` + `.NotEmpty()` 链式调用，确保 null 和空集合都被拦截
- RuleForEach 的 `.When()` 保留不动（防止空集合枚举报错）

## T5-P2-36: Formula Export
- `_repository.GetAllAsync()` 来自 BaseRepository，不 Include Herbs
- FormulaRepository.GetBaseQuery() 已有 `.Include(f => f.Herbs)`
- 新增 GetAllWithHerbsAsync 复用 GetBaseQuery，简洁直接

## T5-P2-37: BatchToggleStatus
- IFormulaApi 已有 BatchEnableAsync/BatchDisableAsync 端点
- 参照 LocalHerbDataSource/RemoteHerbDataSource 的同名方法实现

## T5-P2-38: ImportTemplateColumns
- 参照 IHerbDataSource.GetImportTemplateColumns() 模式
- 验方需要两组列（主表+药材明细），拆为两个方法
- 列名与 GenerateImportTemplate 中的 Sheet 表头对齐
