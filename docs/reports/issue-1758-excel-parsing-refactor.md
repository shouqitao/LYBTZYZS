# Issue #1758: Excel解析逻辑迁移至Client端

**完成时间**: 2025-11-01
**类型**: 架构重构
**范围**: Formula模块（Server + Client）

## 🎯 重构目标

将Excel文件解析逻辑从Server端迁移至Client端，使Server端不再依赖特定文件格式，符合**架构分层原则**：
- **Server端**：处理结构化DTO，专注业务逻辑
- **Client端**：负责文件格式解析和转换

## 📋 实施清单

### ✅ Server端改动

1. **Service层** (`src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`)
   - ❌ 移除：`ImportFromExcelAsync(Stream stream, string? fileName = null)`
   - ✅ 新增：`ImportFromDataAsync(List<FormulaImportDto> formulas, string? fileName = null)`
   - ❌ 移除：`ParseHerbItems(ExcelWorksheet herbSheet)` 方法
   - ❌ 移除：`HerbItemData` 私有类
   - ❌ 移除：EPPlus依赖

2. **Interface层** (`src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs`)
   - 更新方法签名为 `ImportFromDataAsync`

3. **Controller层** (`src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`)
   - ❌ 移除：`POST /api/formulas/import` 接收 `IFormFile`
   - ✅ 更新：接收 `ImportFormulasDataRequest` (包含 `List<FormulaImportDto>`)
   - ❌ 移除：文件格式验证（.xlsx检查、大小限制）

4. **DTO层** (`src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDtos.cs`)
   - ✅ 新增：`ImportFormulasDataRequest` DTO

### ✅ Client端改动

1. **Utilities** (`src/Client/Desktop/Modules/LYBT.Desktop.Formula/Utilities/ExcelParseHelper.cs`)
   - ✅ 新增：`ParseFormulasFromExcel(Stream stream)` 静态方法
   - ✅ 新增：`ParseHerbItems(ExcelWorksheet herbSheet)` 私有方法（从Server迁移）
   - ✅ 新增：`HerbItemData` 私有类（从Server迁移）

2. **项目引用** (`src/Client/Desktop/Modules/LYBT.Desktop.Formula/LYBT.Desktop.Formula.csproj`)
   - ✅ 新增：EPPlus包引用

## 🔄 API变更

### 旧API（已废弃）
```http
POST /api/formulas/import
Content-Type: multipart/form-data

file: [Excel文件]
```

### 新API
```http
POST /api/formulas/import
Content-Type: application/json

{
  "formulas": [
    {
      "name": "验方名称",
      "effect": "功效",
      "usage": "用法",
      // ... 其他字段
      "herbs": [
        {
          "herbName": "药材名称",
          "quantity": 10,
          "unit": "g",
          // ...
        }
      ]
    }
  ],
  "fileName": "original_file.xlsx"
}
```

## 📊 数据流变化

### 之前（Server端解析）
```
Desktop Client → 上传Excel文件 → Server API
                                    ↓
                           Server解析Excel (EPPlus)
                                    ↓
                           Service处理DTO列表
                                    ↓
                           保存到数据库
```

### 现在（Client端解析）
```
Desktop Client → 读取Excel文件
       ↓
    解析Excel (ExcelParseHelper)
       ↓
    生成List<FormulaImportDto>
       ↓
    发送JSON到Server API
                ↓
         Service处理DTO列表
                ↓
         保存到数据库
```

## 🎓 架构原则

1. **职责分离**
   - Server端：数据验证、业务规则、持久化
   - Client端：格式转换、用户交互、工作流编排

2. **格式无关**
   - Server端不依赖任何特定文件格式（Excel/CSV/XML）
   - 未来支持其他格式时，只需Client端实现新的Parser

3. **可扩展性**
   - Web前端可用JavaScript库（如SheetJS）在浏览器中解析Excel
   - 移动端可用各自平台的Excel库

## ⚠️ Breaking Changes

**影响范围**：仅API签名变更，目前Desktop Client的导入功能尚未实现（TODO状态），无实际调用。

**迁移指南**：
- Desktop Client实现导入功能时，需调用 `ExcelParseHelper.ParseFormulasFromExcel(stream)` 解析Excel
- 将解析结果通过新API发送到Server

## ✅ 验证结果

- ✅ 编译通过（0 errors, 0 warnings）
- ✅ Server端不再依赖EPPlus
- ✅ Client端成功集成EPPlus
- ⏭️ 功能测试：待Desktop Client实现导入功能后验证

## 📚 相关文档

- Epic #1753: Server端代码优化（已完成）
- Constitution约束：MVP阶段禁止过度抽象
- 三层架构指南：`docs/explanation/architecture/server/README.md`

---

**变更记录**：
- 2025-11-01: Issue #1758 完成，Excel解析逻辑成功迁移至Client端
