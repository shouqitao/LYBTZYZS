using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using LYBT.Tests.Core;

namespace LYBT.Module.Users.Tests.Base
{
    /// <summary>
    /// Repository测试基类 - Phase D1 统一SQL Server测试基座
    /// 继承SqlServerTestBase以使用真实SQL Server数据库进行测试
    /// </summary>
    public abstract class RepositoryTestBase : SqlServerTestBase
    {
        protected RepositoryTestBase() : base()
        {
            // 基类SqlServerTestBase已经处理了数据库初始化
            // DbContext已经可以通过基类的DbContext属性访问
        }

        /// <summary>
        /// 为了向后兼容，提供Context属性
        /// </summary>
        protected AppDbContext Context => DbContext;

        /// <summary>
        /// 重写种子数据方法，专门为用户测试添加必要的数据
        /// </summary>
        protected override async Task SeedTestDataAsync()
        {
            await base.SeedTestDataAsync();
            // 可以在这里添加用户模块特定的测试数据
        }
    }
}