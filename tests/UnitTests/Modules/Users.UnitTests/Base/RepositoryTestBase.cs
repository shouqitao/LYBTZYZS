using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using Bogus;

namespace LYBT.Module.Users.Tests.Base
{
    /// <summary>
    /// Repository 测试基类
    /// </summary>
    public abstract class RepositoryTestBase : IDisposable
    {
        protected readonly AppDbContext Context;
        private readonly string _databaseName;

        protected RepositoryTestBase()
        {
            _databaseName = $"TestDb_{Guid.NewGuid()}";
            
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .EnableSensitiveDataLogging()
                .Options;

            Context = new AppDbContext(options);
            
            // 确保数据库已创建
            Context.Database.EnsureCreated();
        }

        /// <summary>
        /// 初始化测试数据
        /// </summary>
        protected virtual async Task SeedTestDataAsync()
        {
            // 子类可以重写此方法来添加特定的测试数据
            await Task.CompletedTask;
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        protected virtual async Task ClearTestDataAsync()
        {
            // 清理所有数据
            Context.Users.RemoveRange(Context.Users);
            Context.Patients.RemoveRange(Context.Patients);
            Context.Herbs.RemoveRange(Context.Herbs);
            Context.Consultations.RemoveRange(Context.Consultations);
            Context.Prescriptions.RemoveRange(Context.Prescriptions);
            Context.MedicalCases.RemoveRange(Context.MedicalCases);
            
            await Context.SaveChangesAsync();
        }

        public virtual void Dispose()
        {
            Context?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}