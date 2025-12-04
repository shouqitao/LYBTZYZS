# LYBT.Module.Formula

> 经验方/验方管理 | 传统三层 | 处方模板支撑

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 被Prescriptions模块引用作为模板

## 目录结构

```
LYBT.Module.Formula/
├── FormulaModule.cs
├── Interfaces/
│   └── IFormulaRepository.cs
├── Services/
│   └── FormulaService.cs
├── Repositories/
│   └── FormulaRepository.cs
├── Validators/
│   ├── FormulaCreateDtoValidator.cs
│   └── FormulaUpdateDtoValidator.cs
└── Mapping/
    └── FormulaMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IFormulaService | 19 | CRUD/搜索/克隆/导入导出/智能匹配 |
| IFormulaRepository | 8 | 模板/共享/分类/用户/详情查询 |

## 主要功能

| 功能 | 说明 |
|------|------|
| 验方克隆 | 复制现有验方快速创建新验方 |
| Excel导入 | 批量创建验方，支持智能药材匹配 |
| 药材验证 | 检查验方中药材有效性 |
| 共享机制 | IsShared标志控制验方共享 |

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (FormulaModel, FormulaHerbItem)
- LYBT.Shared.Models (FormulaDto等)
- LYBT.Module.Herbs (药材查询和匹配)

### 被依赖
- LYBT.Module.Prescriptions (从验方创建处方)
- LYBT.WebAPI (FormulasController)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/formulas | GET | 分页查询验方 |
| /api/formulas/{id} | GET | 按ID查询验方详情 |
| /api/formulas | POST | 创建验方 |
| /api/formulas/{id} | PUT | 更新验方 |
| /api/formulas/{id} | DELETE | 删除验方 |
| /api/formulas/{id}/clone | POST | 克隆验方 |
| /api/formulas/search | GET | 搜索验方(按名称/分类) |
| /api/formulas/import | POST | Excel导入验方 |
| /api/formulas/export | GET | 导出验方到Excel |
| /api/formulas/batch-delete | POST | 批量删除验方 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
