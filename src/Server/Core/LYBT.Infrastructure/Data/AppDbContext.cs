using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Security;
using LYBT.Entities.Auth;
using LYBT.Entities.Consultation;
using LYBT.Entities.Formula;
using LYBT.Entities.Herbs;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;


namespace LYBT.Infrastructure.Data
{

    /// <summary>
    /// 统一应用数据库上下文 - 整个项目使用单一数据库LYBTDB
    /// </summary>
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 用户管理
        public DbSet<User> Users { get; set; }

        // 管理员密钥
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }

        // 认证管理
        public DbSet<AuthSession> AuthSessions { get; set; }
        // public DbSet<LoginAttempt> LoginAttempts { get; set; } // UltraThink简化：暂时移除
        // public DbSet<SecurityLog> SecurityLogs { get; set; } // UltraThink简化：暂时移除

        // 安全审计 - UltraThink重构安全架构 (已移除过度设计的SecurityAuditLog)

        // 患者管理
        public DbSet<Patient> Patients { get; set; }

        // 医疗案例
        public DbSet<MedicalCase> MedicalCases { get; set; }

        // 看诊
        public DbSet<Consultation> Consultations { get; set; }

        // 处方管理
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }

        // 药材管理
        public DbSet<Herb> Herbs { get; set; }

        // 验方管理
        public DbSet<Formula> Formulas { get; set; }


        // ==================== 日志相关实体 - UltraThink重构：简化 ====================



        // ==================== 配置相关实体 ====================

        // UltraThink v2.0简化：配置相关实体已移除，使用简化的配置管理

        /// <summary>
        /// 治疗目录
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置各个实体的映射关系
            ConfigureUsers(modelBuilder);
            ConfigureAdminSecrets(modelBuilder);
            ConfigureAuth(modelBuilder);
            ConfigurePatients(modelBuilder);
            // ConfigureRegistrations(modelBuilder); // 模块已删除
            ConfigureMedicalCases(modelBuilder);
            ConfigureConsultations(modelBuilder);
            ConfigurePrescriptions(modelBuilder);
            ConfigureHerbs(modelBuilder);
            ConfigureFormulas(modelBuilder);
            // ConfigurePharmacies(modelBuilder); // 模块已删除
            // ConfigurePharmacyHerbs(modelBuilder); // 模块已删除
            // ConfigureCashiers(modelBuilder); // 模块已删除
            // ConfigureTreatmentTasks(modelBuilder); // 模块已删除
            // ConfigureSyncs(modelBuilder); // MVP阶段暂不需要

            // ConfigureConfigurationModels(modelBuilder); // UltraThink v2.0简化：配置实体已移除
            ConfigureSecurityAudit(modelBuilder);
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<User>();
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            // 明确配置字段映射以解决命名冲突
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).HasColumnName("UserName");
            entity.Property(u => u.RealName).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).HasMaxLength(255);
            // CreateTime字段已删除（UltraThink v2.0简化）
            entity.Property(u => u.PinYinCode).HasMaxLength(50);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            // UltraThink v2.0: Remark字段已删除（简化用户管理）
            // 配置枚举字段
            entity.Property(u => u.Status).HasConversion<int>();
            entity.Property(u => u.Role).HasConversion<int>();

        }

        private static void ConfigureAdminSecrets(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<AdminSecretModel>();
            entity.ToTable("AdminSecrets");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Username).IsUnique();
            entity.Property(a => a.Username).HasMaxLength(50).IsRequired();
            entity.Property(a => a.PasswordHash).HasMaxLength(500).IsRequired();

            // 添加默认的 sysadmin 种子数据
            // 密码: Admin@123456
            entity.HasData(new AdminSecretModel {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "sysadmin",
                PasswordHash = "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ=="
            });
        }

        /// <summary>
        /// 配置认证相关实体
        /// </summary>
        private static void ConfigureAuth(ModelBuilder modelBuilder)
        {
            // 认证会话配置
            modelBuilder.Entity<AuthSession>(entity =>
            {
                entity.ToTable("AuthSessions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TokenHash).HasMaxLength(256);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<int>();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.LoginTime);
                entity.HasIndex(e => e.Status);
            });




        }

        private static void ConfigurePatients(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Patient>();
            entity.ToTable("Patients");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(100);

            entity.Property(p => p.PinYinCode).HasMaxLength(20);
            // CreateTime字段已删除（UltraThink v2.0简化）
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.Address).HasMaxLength(256);
            entity.Property(p => p.IdType).HasMaxLength(20);
            entity.Property(p => p.IdNumber).HasMaxLength(50);
            // Occupation、MaritalStatus、Ethnicity、Education字段已删除
            entity.Property(p => p.AllergyHistory).HasMaxLength(500);

            // 配置Status枚举字段
            entity.Property(p => p.Status).HasConversion<int>();
        }

        // 医生功能已整合到Users
        /*

        */


        // 挂号模块已删除
        /*        private static void ConfigureRegistrations(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<RegistrationModel>();
            entity.ToTable("Registrations");
            entity.HasKey(r => r.Id);
        }*/

        // 排队模块已删除


        private static void ConfigureMedicalCases(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<MedicalCase>();
            entity.ToTable("MedicalCases");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Status).HasConversion<string>();
            entity.Property(m => m.Remark).HasMaxLength(500);
            entity.HasIndex(m => m.PatientId);
            entity.HasIndex(m => m.DoctorId);
            // CreateTime字段已删除（UltraThink v2.0简化）
            entity.HasIndex(m => m.Status);

            // 配置关联关系 - 修复：一对多关系
            // entity.HasOne(m => m.Consultation).WithOne().HasForeignKey<MedicalCase>(m => m.ConsultationId).IsRequired(false); // 删除：循环引用错误
            entity.HasOne(m => m.Prescription).WithOne().HasForeignKey<MedicalCase>(m => m.PrescriptionId).IsRequired(false);
        }

        private static void ConfigureConsultations(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Consultation>();
            entity.ToTable("Consultations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ChiefComplaint).HasMaxLength(500);
            entity.Property(c => c.PresentIllness).HasMaxLength(1000);

            entity.Property(c => c.Inspection).HasMaxLength(500);
            entity.Property(c => c.AuscultationOlfaction).HasMaxLength(500);
            entity.Property(c => c.Inquiry).HasMaxLength(1000);
            entity.Property(c => c.Palpation).HasMaxLength(500);

            entity.Property(c => c.TCMDiagnosis).HasMaxLength(500);

            entity.Property(c => c.TreatmentPrinciple).HasMaxLength(500);
            entity.Property(c => c.MedicalAdvice).HasMaxLength(500);
            entity.Property(c => c.Remark).HasMaxLength(1000);
            entity.HasIndex(c => c.MedicalCaseId);
            entity.HasIndex(c => c.PatientId);
            entity.HasIndex(c => c.UserId);
            
            // 明确配置外键属性和导航属性关系
            entity.Property(c => c.MedicalCaseId).HasColumnName("MedicalCaseId").IsRequired();
            
            // UltraThink Phase 7: 配置与MedicalCase的一对一关系
            entity.HasOne(c => c.MedicalCase)
                  .WithOne(m => m.Consultation)
                  .HasForeignKey<Consultation>(c => c.MedicalCaseId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict); // 防止级联删除

        }

        private static void ConfigurePrescriptions(ModelBuilder modelBuilder)
        {
            var prescriptionEntity = modelBuilder.Entity<Prescription>();
            prescriptionEntity.ToTable("Prescriptions");
            prescriptionEntity.HasKey(p => p.Id);

            var itemEntity = modelBuilder.Entity<PrescriptionItemModel>();
            itemEntity.ToTable("PrescriptionItems");
            itemEntity.HasKey(i => i.Id);
        }

        private static void ConfigureHerbs(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Herb>();
            entity.ToTable("Herbs");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).HasMaxLength(100);
            entity.Property(h => h.PinYinCode).HasMaxLength(20);
            entity.Property(h => h.Origin).HasMaxLength(50);
            entity.Property(h => h.Spec).HasMaxLength(50);
            entity.Property(h => h.Unit).HasMaxLength(10);
            entity.Property(h => h.Effect).HasMaxLength(256);
            entity.Property(h => h.Usage).HasMaxLength(256);
            entity.Property(h => h.Price).HasColumnType("decimal(18,2)");
            // 配置Status枚举字段
            entity.Property(h => h.Status).HasConversion<int>();
            entity.HasIndex(h => h.Name);
            entity.HasIndex(h => h.PinYinCode);
        }

        private static void ConfigureFormulas(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Formula>();
            entity.ToTable("Formulas");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).HasMaxLength(200);
            entity.Property(f => f.Effect).HasMaxLength(500);
            entity.Property(f => f.Usage).HasMaxLength(500);
            entity.Property(f => f.Property).HasMaxLength(300);
            entity.Property(f => f.Remark).HasMaxLength(500);
            // 配置Status枚举字段
            entity.Property(f => f.Status).HasConversion<int>();
            entity.Property(f => f.IsShared).HasDefaultValue(false);

            // 简化配置，忽略子实体以避免复杂的配置问题
            entity.Ignore(f => f.Herbs);
        }

        // 药房模块已删除
        /*        private static void ConfigurePharmacies(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PharmacyModel>();
            entity.ToTable("Pharmacies");
            entity.HasKey(p => p.Id);

            // 配置药房与药材的一对多关系
            entity.HasMany(p => p.Herbs)
                  .WithOne(ph => ph.Pharmacy)
                  .HasForeignKey(ph => ph.PharmacyId);
        }*/

        // 药房模块已删除
        /*
        private static void ConfigurePharmacyHerbs(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<PharmacyHerbModel>();
            entity.ToTable("PharmacyHerbs");
            entity.HasKey(ph => new { ph.PharmacyId, ph.HerbId });
            
            entity.HasOne(ph => ph.Pharmacy)
                  .WithMany(p => p.Herbs)
                  .HasForeignKey(ph => ph.PharmacyId);
                  
            entity.HasOne(ph => ph.Herb)
                  .WithMany()
                  .HasForeignKey(ph => ph.HerbId);
        }
        */

        // 收银模块已删除
        /*
        private static void ConfigureCashiers(ModelBuilder modelBuilder) {
            // CashierRecord 配置
            var cashierEntity = modelBuilder.Entity<CashierRecord>();
            cashierEntity.HasKey(c => c.Id);
            cashierEntity.Property(c => c.TotalAmount).HasColumnType("decimal(18,2)");
            cashierEntity.Property(c => c.PaidAmount).HasColumnType("decimal(18,2)");
            cashierEntity.Property(c => c.RefundAmount).HasColumnType("decimal(18,2)");
            
            // CashierItem 配置
            var itemEntity = modelBuilder.Entity<CashierItem>();
            itemEntity.ToTable("CashierItems");
            itemEntity.HasKey(ci => ci.Id);
            itemEntity.Property(ci => ci.UnitPrice).HasColumnType("decimal(18,2)");
            itemEntity.Property(ci => ci.Amount).HasColumnType("decimal(18,2)");
            
            // CashierPayment 配置
            var paymentEntity = modelBuilder.Entity<CashierPayment>();
            paymentEntity.ToTable("CashierPayments");
            paymentEntity.HasKey(cp => cp.Id);
            paymentEntity.Property(cp => cp.Amount).HasColumnType("decimal(18,2)");
            
            // DailySettlement 配置
            var settlementEntity = modelBuilder.Entity<DailySettlement>();
            settlementEntity.ToTable("DailySettlements");
            settlementEntity.HasKey(ds => ds.Id);
            settlementEntity.Property(ds => ds.TotalAmount).HasColumnType("decimal(18,2)");
            settlementEntity.Property(ds => ds.RefundAmount).HasColumnType("decimal(18,2)");
            settlementEntity.Property(ds => ds.NetAmount).HasColumnType("decimal(18,2)");
            
            // Invoice 配置
            var invoiceEntity = modelBuilder.Entity<Invoice>();
            invoiceEntity.ToTable("Invoices");
            invoiceEntity.HasKey(i => i.Id);
            invoiceEntity.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
        }
        */


        // 治疗室模块已删除
        /*        private static void ConfigureTreatmentTasks(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<TreatmentTaskModel>();
            entity.ToTable("TreatmentTasks");
            entity.HasKey(t => t.Id);
        }*/

        // Sync模块MVP阶段暂不需要





        /// <summary>
        /// 配置安全审计实体 - UltraThink重构安全审计架构
        /// 注意：SecurityAuditLog已在深度清理中移除，简化为日志记录
        /// </summary>
        private static void ConfigureSecurityAudit(ModelBuilder modelBuilder)
        {
            // SecurityAuditLog实体已被移除，转为简化的日志记录方式
        }
    }
}
