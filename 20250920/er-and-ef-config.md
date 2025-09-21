# ER 关系示意与 EF Core 配置样例

## ER 关系（Mermaid）
```mermaid
erDiagram
  User ||--o{ MedicalCase : DoctorId
  Patient ||--o{ MedicalCase : PatientId
  MedicalCase ||--|| Consultation : MedicalCaseId
  MedicalCase ||--o| Prescription : MedicalCaseId
  Prescription ||--o{ PrescriptionItem : PrescriptionId
  Herb ||--o{ PrescriptionItem : HerbId
  Formula ||--o{ FormulaHerbItem : FormulaId

  User {
    guid Id PK
    string Username
  }
  Patient {
    guid Id PK
    string Name
  }
  MedicalCase {
    guid Id PK
    guid PatientId FK
    guid DoctorId FK
    datetime CreatedAt
    int Status
  }
  Consultation {
    guid Id PK
    guid MedicalCaseId UK/FK
    int Status
  }
  Prescription {
    guid Id PK
    guid MedicalCaseId UK/FK
    int DosageCount
    decimal Discount
    int Status
  }
  PrescriptionItem {
    guid Id PK
    guid PrescriptionId FK
    guid HerbId FK
    int Quantity
    decimal UnitPrice
  }
  Herb {
    guid Id PK
    string Name
    string Unit
    decimal Price
  }
  Formula {
    guid Id PK
    string Name
  }
  FormulaHerbItem {
    guid Id PK
    guid FormulaId FK
    string HerbName
    int Quantity
  }
```

说明：
- 每个病案（MedicalCase）恰好 1 个诊断（Consultation），至多 1 张处方（Prescription）。
- 处方项（PrescriptionItem）保存价格快照 `UnitPrice` 与整型剂量 `Quantity`。
- 病案创建时间 `CreatedAt` 用于“同日可编辑”判定（服务器本地时区）。

## EF Core Fluent API 配置样例
> 下述为核心关系与索引示例，具体命名可按项目实际命名空间与枚举值调整。

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    // 1) 一病案一诊断（唯一约束）
    builder.Entity<Consultation>()
        .HasIndex(c => c.MedicalCaseId)
        .IsUnique();

    builder.Entity<MedicalCase>()
        .HasOne<Consultation>()
        .WithOne()
        .HasForeignKey<Consultation>(c => c.MedicalCaseId)
        .OnDelete(DeleteBehavior.Cascade);

    // 2) 一病案至多一处方（唯一约束）
    builder.Entity<Prescription>()
        .HasIndex(p => p.MedicalCaseId)
        .IsUnique();

    builder.Entity<MedicalCase>()
        .HasOne<Prescription>()
        .WithOne()
        .HasForeignKey<Prescription>(p => p.MedicalCaseId)
        .OnDelete(DeleteBehavior.Cascade);

    // 3) 单患者仅一条“未完成病案” —— 过滤唯一索引（SQL Server）
    // 注意：Status 为枚举时落库为 int，请用实际 Completed/Cancelled 的数值替换 2,3。
    builder.Entity<MedicalCase>()
        .HasIndex(mc => mc.PatientId)
        .HasDatabaseName("UX_MedicalCases_Patient_ActiveOnly")
        .IsUnique()
        .HasFilter("[Status] NOT IN (2, 3)");

    // 4) 列类型与精度
    builder.Entity<Prescription>()
        .Property(p => p.Discount)
        .HasColumnType("decimal(3,2)"); // 0.80 表示八折

    builder.Entity<PrescriptionItem>()
        .Property(i => i.UnitPrice)
        .HasColumnType("decimal(18,2)");

    builder.Entity<PrescriptionItem>()
        .Property(i => i.Quantity)
        .HasColumnType("int"); // 剂量不需要小数

    // 5) 并发表与审计（可选）
    builder.Entity<MedicalCase>()
        .Property<byte[]>("RowVersion")
        .IsRowVersion();

    // 建议给三类对象添加 CreatedBy（医生UserId）
    builder.Entity<MedicalCase>().Property<Guid>("CreatedBy");
    builder.Entity<Consultation>().Property<Guid>("CreatedBy");
    builder.Entity<Prescription>().Property<Guid>("CreatedBy");
}
```

## 计价与打印示例（伪代码）
```csharp
// 单副价：所有处方项的单价×剂量之和（不四舍五入、不截断）
decimal singlePack = items.Sum(i => i.UnitPrice * i.Quantity);

// 总价：应用副数与折扣后，在最终总价处“直接舍去”到 2 位
decimal rawTotal = singlePack * prescription.DosageCount * prescription.Discount;
decimal finalTotal = Math.Truncate(rawTotal * 100m) / 100m;

// 打印
var printPerPack = singlePack;        // 每副价格（未折扣）
var printTotal  = finalTotal;         // 折扣后总价（2位小数，直接舍去）
```

## 迁移与数据清理建议（摘要）
- 若现库存在“多诊断/多处方/多未完成病案”脏数据：
  - 诊断：按创建时间保留最新一条，余者合并备注后软删或标记 Cancelled。
  - 处方：保留最新处方为有效，其余归档或 Cancelled；并补齐处方项 `UnitPrice` 快照。
  - 未完成病案：同患者仅保留一条为进行中，其余设为 Cancelled。
- 建索引前先脚本校验并清理，确保唯一约束能建立成功。
```
