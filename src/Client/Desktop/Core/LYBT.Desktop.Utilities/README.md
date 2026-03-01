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
