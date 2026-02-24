# Findings: Sprint 2 - Core Feature Fixes

## Batch 1 Findings

### MedicalCase 实体结构
- BaseEntity 包含 Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, RowVersion, IsDeleted
- MedicalCase 是聚合根, 已有 Consultation(1:1) + Prescription(1:0..1) 导航
- Prescription 已有 PrintVersion/LastPrintedAt/PrintCount/IsPrinted/PrintLogs 字段

### Mapper 体系
- Server 端: `MedicalCaseMapper` (Riok.Mapperly), RequiredMappingStrategy=Target
  - 3 个手动映射方法: MapToMedicalCaseDto/MapToMedicalCaseDetailDto/MapToPrescriptionDetailDto
  - 多个 Mapperly 生成方法: ToListDto/ToDetailDto/ToEntity/UpdateEntity
- Desktop 端: `LocalMedicalCaseMapper` (Riok.Mapperly), 独立映射链
  - ToDetailDtoCore (Mapperly) + ToDetailDto (手动补充嵌套)

### PrescriptionPrintLog 现状
- FK: PrescriptionId -> Prescription (Cascade Delete)
- 表名: PrescriptionPrintLogs
- 配置: PrescriptionPrintLogConfiguration (独立 IEntityTypeConfiguration)
- Batch 2 将迁移此 FK 到 MedicalCaseId

### EF 配置
- MedicalCaseConfiguration 使用 BaseEntityConfiguration<T> 基类
- PrescriptionPrintLogConfiguration 未使用基类 (直接 IEntityTypeConfiguration)
- MedicalCasePrintLogConfiguration 遵循相同模式

## Sprint 1 Key Learnings (Carried Forward)

### CanManageUser 权限模型
- SuperAdmin -> 可管理所有角色
- Admin -> 仅可管理 Doctor/Receptionist
- Doctor/Receptionist -> 无管理权限

### EF Core 注意事项
- FindAsync 应用全局查询过滤器 (IsDeleted)
- 需要 IgnoreQueryFilters() 查询软删除记录
