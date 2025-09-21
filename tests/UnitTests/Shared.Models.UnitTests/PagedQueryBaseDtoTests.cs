using Xunit;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Tests
{
    public class PagedQueryBaseDtoTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var request = new PagedQueryBaseDto();

            Assert.Equal(1, request.PageIndex);
            Assert.Equal(20, request.PageSize);
            Assert.Null(request.Keyword);
            Assert.Null(request.SortField);
            Assert.False(request.IsDescending);
            Assert.Equal(0, request.Skip);
            Assert.NotNull(request.Extensions);
        }

        [Theory]
        [InlineData(1, 10, 0)]
        [InlineData(2, 10, 10)]
        [InlineData(3, 20, 40)]
        public void Skip_CalculatesCorrectly(int pageIndex, int pageSize, int expected)
        {
            var request = new PagedQueryBaseDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            Assert.Equal(expected, request.Skip);
        }

        [Fact]
        public void Properties_WorkCorrectly()
        {
            var request = new PagedQueryBaseDto();

            request.PageIndex = 5;
            request.Keyword = "test";

            Assert.Equal(5, request.PageIndex);
            Assert.Equal("test", request.Keyword);
        }

        [Fact]
        public void Extensions_WorkCorrectly()
        {
            var request = new PagedQueryBaseDto();

            request.Extensions["category"] = "books";
            request.Extensions["minPrice"] = 10.5m;

            Assert.Equal("books", request.Extensions["category"]);
            Assert.Equal(10.5m, request.Extensions["minPrice"]);
        }
    }
}