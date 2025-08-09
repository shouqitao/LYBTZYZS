using System;
using System.Collections.Generic;
using System.Linq;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Builders
{
    /// <summary>
    /// UltraThink测试数据构建器基类
    /// 职责单一：专注于测试数据的构建和生成
    /// 代码干净：流畅接口设计，链式调用
    /// 性能出色：延迟构建，按需生成
    /// </summary>
    public abstract class TestDataBuilder<TEntity, TBuilder> 
        where TEntity : class, new()
        where TBuilder : TestDataBuilder<TEntity, TBuilder>
    {
        protected TEntity _entity;
        protected readonly List<Action<TEntity>> _buildActions;
        protected readonly Random _random;

        protected TestDataBuilder()
        {
            _entity = new TEntity();
            _buildActions = new List<Action<TEntity>>();
            _random = new Random();
        }

        /// <summary>
        /// 构建实体
        /// </summary>
        public virtual TEntity Build()
        {
            // 应用所有构建动作
            foreach (var action in _buildActions)
            {
                action(_entity);
            }

            // 应用默认值
            ApplyDefaults();

            return _entity;
        }

        /// <summary>
        /// 构建多个实体
        /// </summary>
        public virtual List<TEntity> BuildMany(int count)
        {
            var entities = new List<TEntity>();
            for (int i = 0; i < count; i++)
            {
                // 每次构建都创建新实例
                _entity = new TEntity();
                entities.Add(Build());
            }
            return entities;
        }

        /// <summary>
        /// 使用自定义配置
        /// </summary>
        public TBuilder With(Action<TEntity> customization)
        {
            _buildActions.Add(customization);
            return (TBuilder)this;
        }

        /// <summary>
        /// 应用默认值（子类实现）
        /// </summary>
        protected abstract void ApplyDefaults();

        #region 通用辅助方法

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        protected string GenerateRandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// 生成随机中文名
        /// </summary>
        protected string GenerateChineseName()
        {
            string[] surnames = { "张", "李", "王", "刘", "陈", "杨", "赵", "黄", "周", "吴" };
            string[] names = { "伟", "芳", "娜", "敏", "静", "强", "磊", "洋", "艳", "军" };
            
            return surnames[_random.Next(surnames.Length)] + 
                   names[_random.Next(names.Length)] + 
                   names[_random.Next(names.Length)];
        }

        /// <summary>
        /// 生成随机手机号
        /// </summary>
        protected string GeneratePhoneNumber()
        {
            string[] prefixes = { "138", "139", "186", "187", "188", "150", "151", "152" };
            return prefixes[_random.Next(prefixes.Length)] + 
                   _random.Next(10000000, 99999999).ToString();
        }

        /// <summary>
        /// 生成随机日期
        /// </summary>
        protected DateTime GenerateRandomDate(DateTime? minDate = null, DateTime? maxDate = null)
        {
            var min = minDate ?? DateTime.Now.AddYears(-5);
            var max = maxDate ?? DateTime.Now;
            var range = (max - min).Days;
            return min.AddDays(_random.Next(range));
        }

        /// <summary>
        /// 生成随机价格
        /// </summary>
        protected decimal GenerateRandomPrice(decimal min = 1, decimal max = 1000)
        {
            return Math.Round((decimal)(_random.NextDouble() * (double)(max - min) + (double)min), 2);
        }

        /// <summary>
        /// 生成随机枚举值
        /// </summary>
        protected T GenerateRandomEnum<T>() where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(_random.Next(values.Length))!;
        }

        #endregion
    }
}