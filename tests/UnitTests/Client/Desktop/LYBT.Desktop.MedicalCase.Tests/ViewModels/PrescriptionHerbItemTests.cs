using FluentAssertions;
using LYBT.Desktop.Prescriptions.Models.Items;
using LYBT.Shared.Models.Contracts.Herbs;
using System.Collections.ObjectModel;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.ViewModels
{
    /// <summary>
    /// PrescriptionHerbItem单元测试
    /// Epic #2175 BF-002 Phase 4 Task 4.1: 拼音过滤算法单元测试
    /// OpenSpec: unify-frontend-backend-types Phase 8.4 - 类型重命名
    ///
    /// 测试覆盖范围：
    /// 1. GetMatchScore - 7级智能评分算法测试
    /// 2. IsPinyinFuzzyMatch - 拼音模糊匹配测试
    /// 3. FilterHerbs - 集成过滤测试
    /// 4. 边界条件和异常处理测试
    ///
    /// 注意：精确匹配时返回空列表（Bug修复：防止用户选择后Popup一直显示）
    /// </summary>
    public class PrescriptionHerbItemTests
    {
        private static PrescriptionHerbItem CreateViewModel()
        {
            return new PrescriptionHerbItem();
        }

        #region GetMatchScore测试 - Level 1: 名称完全匹配 (100分)

        [Fact]
        public void FilterHerbs_WhenExactNameMatch_ShouldReturnEmpty()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归尾", PinYinCode = "dangguiwei", Price = 18.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入完全匹配的名称
            viewModel.HerbName = "当归";

            // Assert - Bug修复：精确匹配时不显示建议列表（防止用户选择后Popup一直显示）
            viewModel.FilteredHerbs.Should().BeEmpty("精确匹配药材名称时应返回空列表，避免Popup一直显示");
        }

        [Fact]
        public void FilterHerbs_WhenExactNameMatchCaseInsensitive_ShouldReturnEmpty()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 大小写不敏感的精确匹配
            viewModel.HerbName = "当归";

            // Assert
            viewModel.FilteredHerbs.Should().BeEmpty("大小写不敏感的精确匹配也应返回空列表");
        }

        #endregion

        #region GetMatchScore测试 - Level 2: 拼音码完全匹配 (90分)

        [Fact]
        public void FilterHerbs_WhenExactPinyinMatch_ShouldScore90Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Price = 20.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = "danggui";

            // Assert
            viewModel.FilteredHerbs.Should().HaveCount(1);
            viewModel.FilteredHerbs.First().Name.Should().Be("当归", "拼音码完全匹配应该得90分");
        }

        [Fact]
        public void FilterHerbs_WhenExactPinyinMatchUpperCase_ShouldScore90Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 大写输入
            viewModel.HerbName = "DANGGUI";

            // Assert
            viewModel.FilteredHerbs.Should().HaveCount(1);
            viewModel.FilteredHerbs.First().Name.Should().Be("当归");
        }

        #endregion

        #region GetMatchScore测试 - Level 3: 名称前缀匹配 (80分)

        [Fact]
        public void FilterHerbs_WhenNamePrefixMatch_ShouldScore80Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归尾", PinYinCode = "dangguiwei", Price = 18.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "归脾丸", PinYinCode = "guipiwan", Price = 25.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"当"应该匹配"当归"和"当归尾"
            viewModel.HerbName = "当";

            // Assert
            viewModel.FilteredHerbs.Should().HaveCount(2);
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归尾");
            viewModel.FilteredHerbs.Should().NotContain(h => h.Name == "归脾丸", "归脾丸不是前缀匹配");
        }

        [Theory]
        [InlineData("当", 2)] // 前缀匹配：当归、当归尾
        [InlineData("当归", 0)] // 精确匹配 → 返回空列表（Bug修复）
        [InlineData("当归尾", 0)] // 精确匹配 → 返回空列表（Bug修复）
        public void FilterHerbs_WhenNamePrefixMatchTheory_ShouldReturnCorrectCount(string searchText, int expectedCount)
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归尾", PinYinCode = "dangguiwei", Price = 18.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = searchText;

            // Assert
            viewModel.FilteredHerbs.Should().HaveCount(expectedCount);
        }

        #endregion

        #region GetMatchScore测试 - Level 4: 拼音码前缀匹配 (70分)

        [Fact]
        public void FilterHerbs_WhenPinyinPrefixMatch_ShouldScore70Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Price = 20.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "大黄", PinYinCode = "dahuang", Price = 10.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"dang"应该匹配当归、党参、大黄（都是da开头）
            viewModel.HerbName = "dang";

            // Assert
            viewModel.FilteredHerbs.Should().HaveCountGreaterThanOrEqualTo(2);
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "党参");
        }

        [Theory]
        [InlineData("dang", 3)] // 当归danggui、党参dangshen、大黄dahuang (模糊匹配)
        [InlineData("dangg", 1)] // 只有当归danggui
        [InlineData("da", 3)] // 所有都是da开头
        public void FilterHerbs_WhenPinyinPrefixMatchTheory_ShouldReturnCorrectCount(string searchText, int expectedCount)
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Price = 20.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "大黄", PinYinCode = "dahuang", Price = 10.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = searchText;

            // Assert
            viewModel.FilteredHerbs.Should().HaveCount(expectedCount);
        }

        #endregion

        #region GetMatchScore测试 - Level 5: 名称包含匹配 (50分)

        [Fact]
        public void FilterHerbs_WhenNameContainsMatch_ShouldScore50Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归尾", PinYinCode = "dangguiwei", Price = 18.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "归脾丸", PinYinCode = "guipiwan", Price = 25.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"归"应该匹配所有包含"归"的药材
            viewModel.HerbName = "归";

            // Assert
            viewModel.FilteredHerbs.Should().HaveCount(3);
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归尾");
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "归脾丸");
        }

        [Fact]
        public void FilterHerbs_WhenNameContainsMatchMiddle_ShouldScore50Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "生黄芪", PinYinCode = "shenghuangqi", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "黄连", PinYinCode = "huanglian", Price = 20.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"黄"应该匹配两个药材（都包含"黄"）
            viewModel.HerbName = "黄";

            // Assert
            viewModel.FilteredHerbs.Should().HaveCount(2);
        }

        #endregion

        #region GetMatchScore测试 - Level 6: 拼音码包含匹配 (40分)

        [Fact]
        public void FilterHerbs_WhenPinyinContainsMatch_ShouldScore40Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "huangqi", Price = 20.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "川芎", PinYinCode = "chuanxiong", Price = 18.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"gg"应该匹配danggui（包含gg）
            viewModel.HerbName = "gg";

            // Assert
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归", "danggui包含gg");
        }

        [Theory]
        [InlineData("gg")] // danggui包含gg
        [InlineData("gui")] // danggui包含gui
        [InlineData("angg")] // danggui包含angg
        public void FilterHerbs_WhenPinyinContainsMatchTheory_ShouldFindDanggui(string searchText)
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = searchText;

            // Assert
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
        }

        #endregion

        #region GetMatchScore测试 - Level 7: 拼音码模糊匹配 (30分)

        [Fact]
        public void FilterHerbs_WhenPinyinFuzzyMatch_ShouldScore30Points()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Price = 20.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"dg"应该通过模糊匹配找到danggui（d_angg_ui → dg）
            viewModel.HerbName = "dg";

            // Assert
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归", "dg应该模糊匹配danggui");
        }

        [Theory]
        [InlineData("dg")] // danggui → dg (跳跃式匹配)
        [InlineData("dgi")] // danggui → dgi
        [InlineData("dgui")] // danggui → dgui
        public void FilterHerbs_WhenPinyinFuzzyMatchTheory_ShouldFindDanggui(string searchText)
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = searchText;

            // Assert
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
        }

        #endregion

        #region IsPinyinFuzzyMatch测试

        [Fact]
        public void FilterHerbs_WhenFuzzyMatchWithMultipleJumps_ShouldMatch()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "生黄芪", PinYinCode = "shenghuangqi", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"shq"应该模糊匹配shenghuangqi (s_h_eng_h_uang_q_i)
            viewModel.HerbName = "shq";

            // Assert
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "生黄芪");
        }

        [Theory]
        [InlineData("shq")] // shenghuangqi → shq
        [InlineData("shhq")] // shenghuangqi → shhq
        [InlineData("shgq")] // shenghuangqi → shgq
        [InlineData("shqi")] // shenghuangqi → shqi
        public void FilterHerbs_WhenComplexFuzzyMatch_ShouldFindShenghuangqi(string searchText)
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "生黄芪", PinYinCode = "shenghuangqi", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = searchText;

            // Assert
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "生黄芪");
        }

        [Fact]
        public void FilterHerbs_WhenFuzzyMatchOutOfOrder_ShouldNotMatch()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"gd"（顺序颠倒）不应该匹配danggui
            viewModel.HerbName = "gd";

            // Assert
            viewModel.FilteredHerbs.Should().NotContain(h => h.Name == "当归", "模糊匹配必须保持字符顺序");
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public void FilterHerbs_WhenEmptySearchText_ShouldReturnEmpty()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "huangqi", Price = 20.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = "";

            // Assert - 空字符串触发IsNullOrWhiteSpace，返回空列表
            viewModel.FilteredHerbs.Should().BeEmpty("空字符串不进行过滤，返回空列表");
        }

        [Fact]
        public void FilterHerbs_WhenNullSearchText_ShouldReturnEmpty()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = null;

            // Assert - null触发IsNullOrWhiteSpace，返回空列表
            viewModel.FilteredHerbs.Should().BeEmpty("null不进行过滤，返回空列表");
        }

        [Fact]
        public void FilterHerbs_WhenWhitespaceSearchText_ShouldReturnEmpty()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = "   ";

            // Assert - 空白字符串触发IsNullOrWhiteSpace，返回空列表
            viewModel.FilteredHerbs.Should().BeEmpty("空白字符串不进行过滤，返回空列表");
        }

        [Fact]
        public void FilterHerbs_WhenNoMatch_ShouldReturnEmptyList()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = "xyz";

            // Assert
            viewModel.FilteredHerbs.Should().BeEmpty("没有匹配项应返回空列表");
        }

        [Fact]
        public void FilterHerbs_WhenHerbNameIsNull_ShouldNotThrowException()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = null, PinYinCode = "test", Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            Action act = () => viewModel.HerbName = "test";

            // Assert
            act.Should().NotThrow("药材名称为null不应抛出异常");
        }

        [Fact]
        public void FilterHerbs_WhenPinyinCodeIsNull_ShouldNotThrowException()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = null, Price = 15.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 使用非精确匹配的输入
            Action act = () => viewModel.HerbName = "当";

            // Assert
            act.Should().NotThrow("拼音码为null不应抛出异常");
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归", "仍然可以通过名称前缀匹配");
        }

        [Fact]
        public void FilterHerbs_WhenAllHerbsIsNull_ShouldNotThrowException()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.AllHerbs = null;

            // Act
            Action act = () => viewModel.HerbName = "test";

            // Assert
            act.Should().NotThrow("AllHerbs为null不应抛出异常");
        }

        [Fact]
        public void FilterHerbs_WhenAllHerbsIsEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.AllHerbs = new ObservableCollection<HerbDetailDto>();

            // Act
            viewModel.HerbName = "test";

            // Assert
            viewModel.FilteredHerbs.Should().BeEmpty();
        }

        #endregion

        #region 排序测试

        [Fact]
        public void FilterHerbs_ShouldSortByScoreDescending()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归尾", PinYinCode = "dangguiwei", Price = 18.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "归脾丸", PinYinCode = "guipiwan", Price = 25.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 使用非精确匹配的输入来测试排序
            viewModel.HerbName = "当";

            // Assert - 前缀匹配排序：当归、当归尾
            viewModel.FilteredHerbs.Should().HaveCount(2);
            viewModel.FilteredHerbs[0].Name.Should().Be("当归", "前缀匹配：当归 排第一");
            viewModel.FilteredHerbs[1].Name.Should().Be("当归尾", "前缀匹配：当归尾 排第二");
        }

        [Fact]
        public void FilterHerbs_WhenMultipleLevelsMatch_ShouldSortCorrectly()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Price = 20.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "大黄", PinYinCode = "dahuang", Price = 10.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "丹参", PinYinCode = "danshen", Price = 12.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act - 输入"dang"
            viewModel.HerbName = "dang";

            // Assert
            viewModel.FilteredHerbs.Should().HaveCountGreaterThanOrEqualTo(2);
            // 拼音码前缀匹配的应该排在前面（当归danggui、党参dangshen）
            var topTwo = viewModel.FilteredHerbs.Take(2).ToList();
            topTwo.Should().Contain(h => h.Name == "当归");
            topTwo.Should().Contain(h => h.Name == "党参");
        }

        #endregion

        #region 集成测试

        [Fact]
        public void FilterHerbs_RealWorldScenario_ShouldWorkCorrectly()
        {
            // Arrange - 模拟真实场景：医生输入"dg"查找当归
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Price = 20.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "大黄", PinYinCode = "dahuang", Price = 10.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "丹参", PinYinCode = "danshen", Price = 12.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "杜仲", PinYinCode = "duzhong", Price = 18.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act
            viewModel.HerbName = "dg";

            // Assert
            viewModel.FilteredHerbs.Should().NotBeEmpty();
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归", "dg应该通过模糊匹配找到danggui");
        }

        [Fact]
        public void FilterHerbs_ProgressiveTyping_ShouldNarrowResults()
        {
            // Arrange - 模拟用户逐字输入的场景
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>
            {
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "党参", PinYinCode = "dangshen", Price = 20.0m },
                new HerbDetailDto { Id = Guid.NewGuid(), Name = "大黄", PinYinCode = "dahuang", Price = 10.0m }
            };
            viewModel.AllHerbs = testHerbs;

            // Act & Assert - 逐步输入
            viewModel.HerbName = "d";
            var step1Count = viewModel.FilteredHerbs.Count;

            viewModel.HerbName = "da";
            var step2Count = viewModel.FilteredHerbs.Count;

            viewModel.HerbName = "dang";
            var step3Count = viewModel.FilteredHerbs.Count;

            viewModel.HerbName = "dangg";
            var step4Count = viewModel.FilteredHerbs.Count;

            // Assert - 结果应该逐步收窄
            step1Count.Should().BeGreaterThanOrEqualTo(step2Count);
            step2Count.Should().BeGreaterThanOrEqualTo(step3Count);
            step3Count.Should().BeGreaterThanOrEqualTo(step4Count);
            step4Count.Should().Be(1, "dangg应该只匹配danggui");
        }

        #endregion

        #region 性能基线测试 (为Task 4.3做准备)

        [Fact]
        public void FilterHerbs_WithLargeDataset_ShouldCompleteQuickly()
        {
            // Arrange - 创建100个药材的数据集
            var viewModel = CreateViewModel();
            var testHerbs = new ObservableCollection<HerbDetailDto>();
            for (int i = 0; i < 100; i++)
            {
                testHerbs.Add(new HerbDetailDto
                {
                    Id = Guid.NewGuid(),
                    Name = $"药材{i}",
                    PinYinCode = $"yaocai{i}",
                    Price = 10.0m + i
                });
            }
            // 添加目标药材
            testHerbs.Add(new HerbDetailDto { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "danggui", Price = 15.0m });
            viewModel.AllHerbs = testHerbs;

            // Act & Measure
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            viewModel.HerbName = "dg";
            stopwatch.Stop();

            // Assert - 基线性能要求：100个药材过滤应在100ms内完成
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "100个药材的过滤应在100ms内完成");
            viewModel.FilteredHerbs.Should().Contain(h => h.Name == "当归");
        }

        #endregion
    }
}
