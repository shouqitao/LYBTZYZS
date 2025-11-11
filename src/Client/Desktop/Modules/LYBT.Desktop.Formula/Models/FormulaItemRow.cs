using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Mvvm;

namespace LYBT.Desktop.Formula.Models
{
    /// <summary>
    /// 验方药材行数据模型 - 8列DataGrid布局
    /// Issue #2071: 支持4组药材（药材+用量）的快速录入
    /// </summary>
    public class FormulaItemRow : BindableBase
    {
        #region 私有字段

        private HerbDto? _herb1;
        private decimal _quantity1;
        private HerbDto? _herb2;
        private decimal _quantity2;
        private HerbDto? _herb3;
        private decimal _quantity3;
        private HerbDto? _herb4;
        private decimal _quantity4;

        #endregion

        #region 公共属性 - 第1组（药材1 + 用量1）

        /// <summary>
        /// 药材1
        /// </summary>
        public HerbDto? Herb1
        {
            get => _herb1;
            set => SetProperty(ref _herb1, value);
        }

        /// <summary>
        /// 用量1（克）
        /// </summary>
        public decimal Quantity1
        {
            get => _quantity1;
            set => SetProperty(ref _quantity1, value);
        }

        #endregion

        #region 公共属性 - 第2组（药材2 + 用量2）

        /// <summary>
        /// 药材2
        /// </summary>
        public HerbDto? Herb2
        {
            get => _herb2;
            set => SetProperty(ref _herb2, value);
        }

        /// <summary>
        /// 用量2（克）
        /// </summary>
        public decimal Quantity2
        {
            get => _quantity2;
            set => SetProperty(ref _quantity2, value);
        }

        #endregion

        #region 公共属性 - 第3组（药材3 + 用量3）

        /// <summary>
        /// 药材3
        /// </summary>
        public HerbDto? Herb3
        {
            get => _herb3;
            set => SetProperty(ref _herb3, value);
        }

        /// <summary>
        /// 用量3（克）
        /// </summary>
        public decimal Quantity3
        {
            get => _quantity3;
            set => SetProperty(ref _quantity3, value);
        }

        #endregion

        #region 公共属性 - 第4组（药材4 + 用量4）

        /// <summary>
        /// 药材4
        /// </summary>
        public HerbDto? Herb4
        {
            get => _herb4;
            set => SetProperty(ref _herb4, value);
        }

        /// <summary>
        /// 用量4（克）
        /// </summary>
        public decimal Quantity4
        {
            get => _quantity4;
            set => SetProperty(ref _quantity4, value);
        }

        #endregion

        #region 转换方法

        /// <summary>
        /// 将8列数据转换为FormulaHerbItemDto列表
        /// 自动过滤null的Herb项
        /// </summary>
        /// <returns>包含有效药材项的列表</returns>
        public List<FormulaHerbItemDto> ToHerbItems()
        {
            var items = new List<FormulaHerbItemDto>();
            int sortOrder = 0;

            // 第1组
            if (Herb1 != null && Quantity1 > 0)
            {
                items.Add(new FormulaHerbItemDto
                {
                    HerbId = Herb1.Id,
                    HerbName = Herb1.Name,
                    Quantity = Quantity1,
                    Unit = Herb1.Unit,
                    Price = Herb1.Price,
                    SortOrder = sortOrder++,
                    Herb = Herb1
                });
            }

            // 第2组
            if (Herb2 != null && Quantity2 > 0)
            {
                items.Add(new FormulaHerbItemDto
                {
                    HerbId = Herb2.Id,
                    HerbName = Herb2.Name,
                    Quantity = Quantity2,
                    Unit = Herb2.Unit,
                    Price = Herb2.Price,
                    SortOrder = sortOrder++,
                    Herb = Herb2
                });
            }

            // 第3组
            if (Herb3 != null && Quantity3 > 0)
            {
                items.Add(new FormulaHerbItemDto
                {
                    HerbId = Herb3.Id,
                    HerbName = Herb3.Name,
                    Quantity = Quantity3,
                    Unit = Herb3.Unit,
                    Price = Herb3.Price,
                    SortOrder = sortOrder++,
                    Herb = Herb3
                });
            }

            // 第4组
            if (Herb4 != null && Quantity4 > 0)
            {
                items.Add(new FormulaHerbItemDto
                {
                    HerbId = Herb4.Id,
                    HerbName = Herb4.Name,
                    Quantity = Quantity4,
                    Unit = Herb4.Unit,
                    Price = Herb4.Price,
                    SortOrder = sortOrder++,
                    Herb = Herb4
                });
            }

            return items;
        }

        #endregion
    }
}
