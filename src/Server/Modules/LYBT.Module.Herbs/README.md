# LYBT.Module.Herbs

> 中药材档案管理 | 传统三层 | Record-Only模式(无库存)

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 被Formula/Prescriptions模块引用

## 目录结构

```
LYBT.Module.Herbs/
├── HerbsModule.cs
├── Interfaces/
│   └── IHerbRepository.cs
├── Services/
│   └── HerbService.cs
├── Repositories/
│   └── HerbRepository.cs
├── Validators/
│   ├── HerbCreateDtoValidator.cs
│   └── HerbUpdateDtoValidator.cs
└── Mapping/
    └── HerbMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IHerbService | 13 | CRUD/搜索/批量导入导出 |
| IHerbRepository | 2 | 按名称精确查询/拼音模糊查询 |

## 药材字段

| 字段 | 说明 |
|------|------|
| Name | 药材名称 |
| Category | 分类(补益药、清热药等) |
| Effects | 功效(补气养血等) |
| UnitPrice | 单价(元/克) |
| DefaultUnit | 默认计量单位(克、两) |
| DefaultDosage | 常用剂量(3-9g) |
| PinyinAbbreviation | 拼音首字母(快速检索) |

## 设计特点

| 特点 | 说明 |
|------|------|
| Record-Only模式 | 只管理药材档案，不涉及库存 |
| 拼音检索 | 输入"dg"可匹配"当归" |
| 批量导入 | Excel导入，自动去重验证 |

## 设计依据

- Record-Only 模式只管理药材档案信息，不涉及库存管理，符合小型诊所 MVP 定位
- 拼音首字母检索 (PinyinAbbreviation) 适配中医处方开具时的快速药材选择场景
- FluentValidation 在 Service 层做输入验证，BaseService 泛型基类复用错误处理逻辑
- 作为被引用最多的基础数据模块，通过 IHerbCrossModuleService 向 Formula/MedicalCase 暴露只读查询

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (HerbModel实体)
- LYBT.Shared.Models (HerbDto等)

### 被依赖
- LYBT.Module.Formula (验方药材组成)
- LYBT.Module.Prescriptions (处方药材配伍)
- LYBT.WebAPI (HerbsController)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/herbs | GET | 分页查询药材 |
| /api/herbs/{id} | GET | 按ID查询药材详情 |
| /api/herbs | POST | 创建药材 |
| /api/herbs/{id} | PUT | 更新药材 |
| /api/herbs/{id} | DELETE | 删除药材 |
| /api/herbs/search | GET | 搜索药材(名称/拼音/功效) |
| /api/herbs/import | POST | Excel导入药材 |
| /api/herbs/export | GET | 导出药材到Excel |
| /api/herbs/template | GET | 下载导入模板 |
| /api/herbs/batch-delete | POST | 批量删除药材 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
