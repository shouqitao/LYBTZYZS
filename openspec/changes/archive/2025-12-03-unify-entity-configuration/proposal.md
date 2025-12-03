# OpenSpec Proposal: 统一实体配置方法论研究

## 变更概述

**变更名称**: unify-entity-configuration
**提案日期**: 2025-12-03
**类型**: 研究分析 / 潜在重构
**影响范围**: LYBT.Entities, LYBT.Infrastructure.Data.Configurations

## 背景与动机

### 当前现状

项目当前同时使用两种EF Core实体配置方法：

1. **Data Annotations** (实体类内)
   - `[Required]`, `[StringLength]` - 验证和长度限制
   - `[Key]`, `[Timestamp]` - 主键和并发控制
   - `[DisplayName]` - UI显示名称
   - `[Table]`, `[Column]`, `[NotMapped]` - Schema映射
   - 自定义属性如 `[SensitiveData]`

2. **Fluent API** (Configuration类)
   - `HasMaxLength()`, `IsRequired()` - 数据库约束
   - `HasConversion<int>()` - 枚举转换
   - `HasIndex()`, `IsUnique()` - 索引配置
   - `HasDefaultValueSql()` - 默认值
   - `IsConcurrencyToken()` - 并发控制
   - `HasQueryFilter()` - 全局过滤器

### 问题识别

**冗余配置示例** (PatientModel vs PatientConfiguration):

| 字段 | Data Annotations | Fluent API | 冗余? |
|------|------------------|------------|-------|
| Name | `[StringLength(100)]` | `HasMaxLength(100)` | **是** |
| PinYinCode | `[StringLength(20)]` | `HasMaxLength(50)` | **不一致** |
| PhoneNumber | `[StringLength(20)]` | `HasMaxLength(20)` | **是** |
| Address | `[StringLength(256)]` | `HasMaxLength(256)` | **是** |
| AllergyHistory | `[StringLength(500)]` | `HasMaxLength(500)` | **是** |

**统计数据**:
- Data Annotations使用: 18个文件, 140处
- Fluent API使用: 16个配置文件, 95处

## 分析问题

**需要回答的核心问题**:

1. **能否只用Fluent API?** 完全移除Data Annotations
2. **能否只用Data Annotations?** 完全移除Fluent API Configuration类
3. **当前混合方案是否合理?** 继续保持现状

## 提议的分析范围

### Phase 1: 可行性分析
- 评估Fluent API能否完全替代所有Data Annotations功能
- 评估Data Annotations能否完全替代所有Fluent API功能
- 识别各方案的能力边界

### Phase 2: 权衡分析
- 代码可读性影响
- 维护成本对比
- 团队技能要求
- 迁移复杂度评估

### Phase 3: 建议输出
- 推荐方案及理由
- 迁移路径（如适用）
- 最佳实践文档更新

## 成功标准

1. 完成三种方案的详细分析
2. 提供明确的推荐方案及理由
3. 如推荐变更，提供可执行的迁移计划

## 风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 分析结论为保持现状 | 低 | 至少产出最佳实践文档 |
| 迁移范围过大 | 中 | 可分阶段实施 |
| 破坏现有功能 | 高 | 完善测试覆盖后再迁移 |

## 参考资料

- 现有规范: `openspec/specs/data-layer-conventions/spec.md`
- 项目文档: `src/Server/Core/LYBT.Entities/README.md`
- EF Core官方文档: Fluent API vs Data Annotations
