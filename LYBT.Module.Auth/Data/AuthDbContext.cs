using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Data {

    /// <summary>
    /// 认证模块数据库上下文
    /// </summary>
    public class AuthDbContext : DbContext {

        /// <summary>
        /// 构造函数
        /// </summary>
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) {
        }

        /// <summary>
        /// 配置数据库模型
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // Auth module primarily handles authentication logic
            // No dedicated tables needed beyond what's in Users/Infrastructure
        }
    }
}