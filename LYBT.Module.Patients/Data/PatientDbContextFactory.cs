using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LYBT.Module.Patients.Data {

    /// <summary>
    /// 患者模块数据库上下文工厂
    /// </summary>
    public class PatientDbContextFactory : IDesignTimeDbContextFactory<PatientsDbContext> {

        public PatientsDbContext CreateDbContext(string[] args) {
            var optionsBuilder = new DbContextOptionsBuilder<PatientsDbContext>();
            
            // 使用默认连接字符串进行设计时迁移
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LYBTDB;Trusted_Connection=true;");

            return new PatientsDbContext(optionsBuilder.Options);
        }
    }
}
