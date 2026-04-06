# LYBT.Desktop.Utilities

> 桌面端通用工具库，提供 Excel 导入导出能力

## 项目定位

- **层级**: Desktop Core (工具层)
- **职责**: 提供桌面端通用工具方法，当前包含基于 NPOI 的 Excel 导入导出功能，支持泛型类型映射与 Display 特性识别
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Utilities/
└── Excel/
    └── ExcelHelper.cs     # Excel 导入导出静态工具类
```

## 核心组件

| 名称 | 说明 |
|------|------|
| ExcelHelper | 静态工具类，提供泛型 ExportToExcel/ImportFromExcel 方法 |

## 功能说明

ExcelHelper 提供以下能力：
- **导出**: 将 IEnumerable\<T\> 数据导出为 .xlsx 文件，支持自定义列定义、标题行样式、列宽自适应
- **导入**: 从 .xlsx 文件读取数据并映射为强类型对象，支持 Display 特性自动识别列名
- **类型转换**: 自动处理常见数据类型 (string, int, decimal, DateTime 等) 的单元格读写转换

基于 NPOI XSSFWorkbook 实现，仅支持 .xlsx 格式 (Office 2007+)。

## 设计依据

Excel 导入导出是中医诊所管理系统的常见需求，用于患者数据批量导入、用户列表导出等场景。选择 NPOI 而非 EPPlus 等方案，因为 NPOI 开源免费无商业许可限制。

作为独立工具库，不依赖任何业务层项目，便于多模块复用。

## 依赖关系

### 依赖
- NPOI - Excel 文件读写引擎
- System.ComponentModel.Annotations - Display 特性支持

### 被依赖
- LYBT.Desktop.Users - 用户管理模块 (用户列表导出)
- LYBT.Desktop.Patients - 患者管理模块 (患者数据导入导出)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始 README 创建 |

## 开发笔记

# LYBT.Desktop.Utilities 模块说明

## 代码文件结构

```
LYBT.Desktop.Utilities/
└── Excel/
    └── ExcelHelper.cs     # Excel 导入导出静态工具类 (542行)
```

### Excel/ExcelHelper.cs

- **类型**: `ExcelHelper` (static class)
- **命名空间**: `LYBT.Desktop.Utilities.Excel`
- **职责**: 基于 NPOI 的 Excel 导入导出工具，支持泛型类型映射和 Display 特性识别
- **依赖**: NPOI (XSSFWorkbook), System.ComponentModel.Annotations

**导出方法**:

| 方法 | 签名 | 说明 |
|------|------|------|
| `ExportToExcel<T>` | `(IEnumerable<T>, Dictionary<string,string>, string, string)` | 按指定列定义导出，同步 |
| `ExportAsync<T>` | `(IEnumerable<T>, string, string)` | 自动识别 Display 特性导出，异步 |

**导入方法**:

| 方法 | 签名 | 说明 |
|------|------|------|
| `ImportFromExcel` | `(string, bool) -> DataTable` | 导入为 DataTable，同步 |
| `ParseAsync<T>` | `(Stream, bool) -> Task<List<T>>` | 解析为泛型列表，支持 Display 特性映射，异步 |

**模板方法**:

| 方法 | 签名 | 说明 |
|------|------|------|
| `CreateTemplate` | `(string[], string, string, List<string[]>?)` | 创建导入模板，支持示例数据 |
| `GenerateTemplateAsync<T>` | `(string, string, IEnumerable<T>?)` | 泛型模板生成，自动识别 Display 特性 |

**私有辅助方法**:

| 类别 | 方法 | 说明 |
|------|------|------|
| 样式 | `CreateHeaderStyle()` | 标题行样式 (加粗, 灰色背景, 居中) |
| 样式 | `CreateSampleStyle()` | 示例数据样式 (斜体, 灰色) |
| 样式 | `AutoResizeColumns()` | 列宽自适应 (最小宽度 3000) |
| 单元格 | `SetCellValue(ICell, object?)` | 类型分支设置 (string/DateTime/bool/numeric/other) |
| 单元格 | `GetCellValue(ICell)` | 类型分支读取 (Numeric/String/Boolean/Formula) |
| 反射 | `GetReadableProperties<T>()` | 获取可读公共属性列表 |
| 反射 | `GetWritableProperties<T>()` | 获取可写公共属性列表 |
| 反射 | `GetColumnHeader(PropertyInfo)` | 优先级: DisplayAttribute.Name > DisplayNameAttribute.DisplayName > PropertyInfo.Name |
| 反射 | `BuildColumnPropertyMap()` | 建立列索引到属性的映射 (支持 Display 特性匹配) |
| 类型转换 | `ConvertToPropertyType()` | 统一类型转换 (Nullable, Enum, DateTime, bool, ChangeType) |
| 类型转换 | `ConvertToEnum()` | 名称匹配或数值匹配 |
| 类型转换 | `ConvertToDateTime()` | OADate 或 TryParse |
| 类型转换 | `ConvertToBoolean()` | 支持 "是"/"否", "true"/"false", "1"/"0" |

---

## 死代码与废弃标记

(无)

所有公共方法均有明确调用:
- `ExcelHelper` 被 `PatientMasterDetailViewModel` (患者数据导入导出) 和 `UserImportExportHandler` (用户列表导出) 使用

---

## 设计分析

### 纯静态工具类设计

ExcelHelper 作为无状态的静态工具类，不需要 DI 注册。调用方直接 `ExcelHelper.ExportAsync<T>(...)` 即可使用。

### 仅支持 .xlsx 格式

基于 `XSSFWorkbook` 实现，仅支持 Office 2007+ 的 .xlsx 格式。不支持旧版 .xls 格式。

### Display 特性自动识别

导入导出均支持通过 `[Display(Name = "列标题")]` 或 `[DisplayName("列标题")]` 特性自动映射列名与属性。优先级: DisplayAttribute > DisplayNameAttribute > 属性名。

---

## 已知陷阱

1. **DateTime 中文格式化**: `SetCellValue` 将 DateTime 格式化为 `yyyy-MM-dd HH:mm:ss`，无法自定义格式。如需不同格式需在调用前转为字符串
2. **导入忽略类型转换错误**: `ParseAsync<T>` 中 `ConvertToPropertyType` 失败时静默忽略 (catch 空块)，不会报错也不会设置默认值
3. **反射性能**: 每次调用 `ExportAsync<T>` 或 `ParseAsync<T>` 都通过反射获取属性信息，大数据量时可能有性能影响
4. **bool 类型中文映射**: 导出时 bool 转为 "是"/"否"，导入时支持 "是"/"否"/"true"/"false"/"1"/"0"，但不支持其他中文表达 (如 "有"/"无")
5. **最小列宽硬编码**: `MinColumnWidth = 3000` 为硬编码常量，无法通过参数调整

---

最后更新: 2026-03-01
