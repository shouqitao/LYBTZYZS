using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Data {

    /// <summary>
    /// 基础设施数据库上下文
    /// </summary>
    public class InfrastructureDbContext : DbContext {

        public InfrastructureDbContext(DbContextOptions<InfrastructureDbContext> options) : base(options) {
        }

        // ==================== 日志相关实体 ====================

        /// <summary>
        /// 统一日志
        /// </summary>
        public DbSet<LogModel> Logs { get; set; }

        /// <summary>
        /// 系统日志
        /// </summary>
        public DbSet<SystemLogModel> SystemLogs { get; set; }

        /// <summary>
        /// 用户操作日志
        /// </summary>
        public DbSet<UserActionLogModel> UserActionLogs { get; set; }

        /// <summary>
        /// 错误日志
        /// </summary>
        public DbSet<ErrorLogModel> ErrorLogs { get; set; }

        /// <summary>
        /// 审计日志
        /// </summary>
        public DbSet<AuditLogModel> AuditLogs { get; set; }

        /// <summary>
        /// 性能日志
        /// </summary>
        public DbSet<PerformanceLogModel> PerformanceLogs { get; set; }

        // ==================== 配置相关实体 ====================

        /// <summary>
        /// 全局设置
        /// </summary>
        public DbSet<GlobalSettingsModel> GlobalSettings { get; set; }

        /// <summary>
        /// 系统设置
        /// </summary>
        public DbSet<SettingsModel> Settings { get; set; }

        /// <summary>
        /// 诊断目录
        /// </summary>
        public DbSet<DiagnosisCatalogModel> DiagnosisCatalogs { get; set; }

        /// <summary>
        /// 治疗目录
        /// </summary>
        public DbSet<TreatmentCatalogModel> TreatmentCatalogs { get; set; }

        /// <summary>
        /// 治疗室
        /// </summary>
        public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            ConfigureLogModels(modelBuilder);
            ConfigureConfigurationModels(modelBuilder);
        }

        /// <summary>
        /// 配置日志相关实体
        /// </summary>
        private static void ConfigureLogModels(ModelBuilder modelBuilder) {
            // 统一日志配置
            modelBuilder.Entity<LogModel>(entity => {
                entity.ToTable("InfrastructureLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LogType).HasConversion<string>();
                entity.Property(e => e.ObjectType).HasConversion<string>();
                entity.Property(e => e.ActionType).HasConversion<string>();
                entity.Property(e => e.OperatorName).HasMaxLength(50);
                entity.Property(e => e.Content).HasMaxLength(500);
                entity.Property(e => e.IP).HasMaxLength(45);
                entity.Property(e => e.Remark).HasMaxLength(1000);
                entity.HasIndex(e => e.LogTime);
                entity.HasIndex(e => e.OperatorId);
                entity.HasIndex(e => e.LogType);
            });

            // 系统日志配置
            modelBuilder.Entity<SystemLogModel>(entity => {
                entity.ToTable("SystemLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Level).HasConversion<string>();
                entity.Property(e => e.Source).HasMaxLength(100);
                entity.Property(e => e.Message).HasMaxLength(2000);
                entity.Property(e => e.ServerInfo).HasMaxLength(100);
                entity.Property(e => e.RequestId).HasMaxLength(50);
                entity.HasIndex(e => e.LogTime);
                entity.HasIndex(e => e.Level);
                entity.HasIndex(e => e.Source);
            });

            // 用户操作日志配置
            modelBuilder.Entity<UserActionLogModel>(entity => {
                entity.ToTable("UserActionLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ActionType).HasConversion<string>();
                entity.Property(e => e.UserName).HasMaxLength(50);
                entity.Property(e => e.Module).HasMaxLength(50);
                entity.Property(e => e.Function).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.RequestPath).HasMaxLength(500);
                entity.Property(e => e.HttpMethod).HasMaxLength(10);
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.HasIndex(e => e.ActionTime);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ActionType);
            });

            // 错误日志配置
            modelBuilder.Entity<ErrorLogModel>(entity => {
                entity.ToTable("ErrorLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
                entity.Property(e => e.ExceptionType).HasMaxLength(200);
                entity.Property(e => e.RequestPath).HasMaxLength(500);
                entity.Property(e => e.HttpMethod).HasMaxLength(10);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Environment).HasMaxLength(50);
                entity.Property(e => e.Severity).HasMaxLength(20);
                entity.Property(e => e.ResolutionNotes).HasMaxLength(1000);
                entity.HasIndex(e => e.OccurredAt);
                entity.HasIndex(e => e.IsResolved);
                entity.HasIndex(e => e.Severity);
            });

            // 审计日志配置
            modelBuilder.Entity<AuditLogModel>(entity => {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).HasMaxLength(50);
                entity.Property(e => e.ResourceType).HasMaxLength(50);
                entity.Property(e => e.ResourceId).HasMaxLength(50);
                entity.Property(e => e.UserName).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.SessionId).HasMaxLength(50);
                entity.Property(e => e.RequestId).HasMaxLength(50);
                entity.Property(e => e.Result).HasMaxLength(50);
                entity.Property(e => e.RiskLevel).HasMaxLength(20);
                entity.Property(e => e.ComplianceFlags).HasMaxLength(200);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.ResourceType);
            });

            // 性能日志配置
            modelBuilder.Entity<PerformanceLogModel>(entity => {
                entity.ToTable("PerformanceLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperationName).HasMaxLength(100);
                entity.Property(e => e.ModuleName).HasMaxLength(50);
                entity.Property(e => e.MethodName).HasMaxLength(100);
                entity.Property(e => e.ClientIP).HasMaxLength(45);
                entity.Property(e => e.RequestPath).HasMaxLength(500);
                entity.Property(e => e.PerformanceLevel).HasMaxLength(20);
                entity.Property(e => e.AdditionalData).HasColumnType("text");
                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.Duration);
                entity.HasIndex(e => e.PerformanceLevel);
            });
        }

        /// <summary>
        /// 配置配置相关实体
        /// </summary>
        private static void ConfigureConfigurationModels(ModelBuilder modelBuilder) {
            // 全局设置配置
            modelBuilder.Entity<GlobalSettingsModel>(entity => {
                entity.ToTable("GlobalSettings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SystemName).HasMaxLength(100);
                entity.Property(e => e.SystemVersion).HasMaxLength(20);
                entity.Property(e => e.SystemLogo).HasMaxLength(255);
                entity.Property(e => e.DefaultRecordSharing).HasMaxLength(20);
                entity.Property(e => e.SyncMode).HasMaxLength(20);
                entity.Property(e => e.UpdatedByName).HasMaxLength(50);
            });

            // 系统设置配置
            modelBuilder.Entity<SettingsModel>(entity => {
                entity.ToTable("Settings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).HasMaxLength(128).IsRequired();
                entity.Property(e => e.Value).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.ValueType).HasMaxLength(20);
                entity.Property(e => e.Group).HasMaxLength(50);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasIndex(e => e.Group);
                entity.HasIndex(e => e.IsEnabled);
            });

            // 诊断目录配置
            modelBuilder.Entity<DiagnosisCatalogModel>(entity => {
                entity.ToTable("DiagnosisCatalogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(20);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IcdCode).HasMaxLength(20);
                entity.Property(e => e.TcmSyndrome).HasMaxLength(100);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.Code);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.ParentId);
                entity.HasIndex(e => e.IsEnabled);
                entity.HasIndex(e => e.IsCommon);
                
                // 自引用关系
                entity.HasOne<DiagnosisCatalogModel>()
                      .WithMany()
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 治疗目录配置
            modelBuilder.Entity<TreatmentCatalogModel>(entity => {
                entity.ToTable("TreatmentCatalogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(20);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Indications).HasMaxLength(500);
                entity.Property(e => e.Contraindications).HasMaxLength(500);
                entity.Property(e => e.Precautions).HasMaxLength(1000);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.Code);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.ParentId);
                entity.HasIndex(e => e.IsEnabled);
                entity.HasIndex(e => e.IsCommon);
                
                // 自引用关系
                entity.HasOne<TreatmentCatalogModel>()
                      .WithMany()
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 治疗室配置
            modelBuilder.Entity<TreatmentRoomModel>(entity => {
                entity.ToTable("TreatmentRooms");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RoomNumber).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
                entity.Property(e => e.RoomType).HasMaxLength(50);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Equipment).HasMaxLength(1000);
                entity.Property(e => e.ResponsibleDoctorName).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.Remark).HasMaxLength(500);
                entity.HasIndex(e => e.RoomNumber).IsUnique();
                entity.HasIndex(e => e.ResponsibleDoctorId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsEnabled);
            });
        }
    }
}