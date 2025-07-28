using LYBT.Models.TreatmentRoom;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.TreatmentRoom.Data {

    /// <summary>
    /// 治疗室模块数据库上下文
    /// </summary>
    public class TreatmentRoomDbContext : DbContext {

        /// <summary>
        /// 构造函数
        /// </summary>
        public TreatmentRoomDbContext(DbContextOptions<TreatmentRoomDbContext> options) : base(options) {
        }

        /// <summary>
        /// 治疗室数据集
        /// </summary>
        public DbSet<TreatmentRoomModel> TreatmentRooms { get; set; }

        /// <summary>
        /// 配置数据库模型
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            ConfigureTreatmentRooms(modelBuilder);
        }

        /// <summary>
        /// 配置治疗室表
        /// </summary>
        private static void ConfigureTreatmentRooms(ModelBuilder modelBuilder) {
            var entity = modelBuilder.Entity<TreatmentRoomModel>();
            entity.ToTable("TreatmentRooms");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Status).HasDatabaseName("IX_TreatmentRooms_Status");
            entity.HasIndex(t => t.PatientId).HasDatabaseName("IX_TreatmentRooms_PatientId");
            entity.HasIndex(t => t.DoctorId).HasDatabaseName("IX_TreatmentRooms_DoctorId");
            entity.HasIndex(t => t.TreatmentType).HasDatabaseName("IX_TreatmentRooms_TreatmentType");
        }
    }
}