# 模块对接分析报告：验方管理 (Formula)

| 统计项 | 数量 |
| :--- | :---: |
| WebApi 总点数 | 17 |
| Desktop 已对接点数 | 6 |
| **对接完成情况** | **35%** |

---

### WebApi 总点数列表 (17 个)

- `GetPagedFormulas` (分页列表)
- `GetFormulaById` (获取详情)
- `CreateFormula` (创建)
- `UpdateFormula` (更新)
- `DeleteFormula` (删除)
- `BatchDeleteFormulas` (批量删除)
- `CopyFormula` (复制)
- `ToggleFormulaStatus` (切换状态)
- `GetCategories` (获取分类)
- `ImportFormulas` (导入)
- `ValidateImportData` (验证导入)
- `ExportFormulas` (导出)
- `ExportAllFormulas` (导出全部)
- `ImportFromExcel` (从Excel导入)
- `ExportToExcel` (导出到Excel)
- `GetImportHistory` (导入历史)
- `GetImportTemplate` (下载模板)

---

### Desktop 已对接点数列表 (6 个)

- `GetFormulaByIdAsync`
- `CreateFormulaAsync`
- `UpdateFormulaAsync`
- `DeleteFormulaAsync`
- `CopyFormulaAsync`
- `ToggleFormulaStatusAsync`

---

### 未对接点分析

- **查询类**: `GetPagedFormulas`, `GetCategories`
- **批量操作**: `BatchDeleteFormulas`
- **所有导入导出功能**: 包括 `Import/Export`、`Excel` 操作、`Template` 下载、`History` 查看等共7个API。

---

### 状态总结

**对接程度最低，仅基础功能完成。**

这是目前对接比例最低的模块。前端仅实现了对单个验方的增、删、改、查、复制和状态切换。所有高级功能，特别是复杂的列表查询、批量操作和完整的导入导出流程，都还停留在后端 API 层面，前端完全没有涉及。
