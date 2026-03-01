using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Tests.Services
{
    /// <summary>
    /// 方剂服务单元测试
    /// 测试方剂的创建、查询、更新、删除以及药材配比管理等核心业务逻辑
    /// OpenSpec: decouple-server-modules - 使用IHerbCrossModuleService替代IHerbRepository
    /// </summary>
    public class FormulaServiceTests : TestBase
    {
        private readonly FormulaService _formulaService;
        private readonly IFormulaRepository _repositoryMock;
        private readonly IHerbCrossModuleService _crossModuleQueryMock;
        private readonly ILogger<FormulaService> _loggerMock;
        private readonly ICacheInvalidationService _cacheInvalidationMock;

        public FormulaServiceTests()
        {
            _repositoryMock = CreateMock<IFormulaRepository>();
            _crossModuleQueryMock = CreateMock<IHerbCrossModuleService>();
            _loggerMock = CreateLoggerMock<FormulaService>();
            _cacheInvalidationMock = CreateMock<ICacheInvalidationService>();

            _formulaService = new FormulaService(
                _repositoryMock,
                _crossModuleQueryMock,
                _loggerMock,
                _cacheInvalidationMock);
        }

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_WithExistingFormula_ShouldReturnFormula()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = CreateTestFormula(formulaId);

            _repositoryMock.GetByIdAsync(formulaId).Returns(formula);

            // Act
            var result = await _formulaService.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(formulaId);
            result.Data!.Name.Should().Be(formula.Name);

            await _repositoryMock.Received(1).GetByIdAsync(formulaId);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingFormula_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.GetByIdAsync(formulaId).Returns((FormulaEntity?)null);

            // Act
            var result = await _formulaService.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("方剂不存在");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var exception = new Exception("数据库错误");

            _repositoryMock.GetByIdAsync(formulaId).ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _formulaService.GetByIdAsync(formulaId));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var formulas = CreateTestFormulas(5);
            var pagedResult = new PagedResult<FormulaEntity>
            {
                Items = formulas,
                TotalCount = 5,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock.GetPagedAsync(1, 20, Arg.Any<string?>()).Returns(pagedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(5);
            result.Data!.TotalCount.Should().Be(5);
            result.Data!.CurrentPage.Should().Be(1);
            result.Data!.PageSize.Should().Be(20);

            await _repositoryMock.Received(1).GetPagedAsync(1, 20, Arg.Any<string?>());
        }

        [Fact]
        public async Task GetPagedAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var exception = new Exception("数据库错误");
            _repositoryMock.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _formulaService.GetPagedAsync(1, 20, null));

            thrownException.Message.Should().Be("数据库错误");
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var pagedResult = new PagedResult<FormulaEntity>
            {
                Items = new List<FormulaEntity>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock.GetPagedAsync(1, 20, Arg.Any<string?>()).Returns(pagedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().BeEmpty();
            result.Data!.TotalCount.Should().Be(0);
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateFormula()
        {
            // Arrange
            var createDto = new FormulaInputDto
            {
                Name = "测试方剂",
                Category = "补益剂",
                Effect = "补气养血",
                Herbs = new List<FormulaHerbItemInputDto>()
            };

            var createdFormula = new FormulaEntity
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Category = createDto.Category,
                Effect = createDto.Effect,
                CreatedAt = DateTime.UtcNow
            };

            _repositoryMock.AddAsync(Arg.Any<FormulaEntity>()).Returns(createdFormula);

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(createDto.Name);

            await _repositoryMock.Received(1).AddAsync(Arg.Any<FormulaEntity>());
        }

        [Fact]
        public async Task CreateAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var createDto = new FormulaInputDto
            {
                Name = "测试方剂",
                Effect = "测试功效",
                Herbs = new List<FormulaHerbItemInputDto>()
            };

            var exception = new Exception("数据库错误");
            _repositoryMock.AddAsync(Arg.Any<FormulaEntity>()).ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _formulaService.CreateAsync(createDto));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_WithExistingFormula_ShouldUpdateFormula()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var existingFormula = CreateTestFormula(formulaId);

            var updateDto = new FormulaInputDto
            {
                Name = "更新的方剂名称",
                Category = "更新的分类",
                Effect = "更新的功效",
                Herbs = new List<FormulaHerbItemInputDto>()
            };

            var updatedFormula = new FormulaEntity
            {
                Id = formulaId,
                Name = updateDto.Name,
                Category = updateDto.Category,
                Effect = updateDto.Effect,
                UpdatedAt = DateTime.UtcNow
            };

            _repositoryMock.GetByIdAsync(formulaId).Returns(existingFormula);
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>()).Returns(updatedFormula);

            // Act
            var result = await _formulaService.UpdateAsync(formulaId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(formulaId);
            result.Data!.Name.Should().Be(updateDto.Name);

            await _repositoryMock.Received(1).GetByIdAsync(formulaId);
            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<FormulaEntity>());
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingFormula_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var updateDto = new FormulaInputDto
            {
                Name = "更新的方剂名称",
                Herbs = new List<FormulaHerbItemInputDto>()
            };

            _repositoryMock.GetByIdAsync(formulaId).Returns((FormulaEntity?)null);

            // Act
            var result = await _formulaService.UpdateAsync(formulaId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("方剂不存在");

            await _repositoryMock.Received(1).GetByIdAsync(formulaId);
            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<FormulaEntity>());
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var existingFormula = CreateTestFormula(formulaId);
            var updateDto = new FormulaInputDto
            {
                Name = "更新的方剂名称",
                Herbs = new List<FormulaHerbItemInputDto>()
            };

            var exception = new Exception("数据库错误");
            _repositoryMock.GetByIdAsync(formulaId).Returns(existingFormula);
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>()).ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _formulaService.UpdateAsync(formulaId, updateDto));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_WithExistingFormula_ShouldDeleteSuccessfully()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.DeleteAsync(formulaId).Returns(true);

            // Act
            var result = await _formulaService.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            await _repositoryMock.Received(1).DeleteAsync(formulaId);
        }

        [Fact]
        public async Task DeleteAsync_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.DeleteAsync(formulaId).Returns(false);

            // Act
            var result = await _formulaService.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("删除失败");
        }

        [Fact]
        public async Task DeleteAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var exception = new Exception("数据库错误");

            _repositoryMock.DeleteAsync(formulaId).ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _formulaService.DeleteAsync(formulaId));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_WithMatchingKeyword_ShouldReturnMatchingFormulas()
        {
            // Arrange
            var keyword = "补气";
            var matchingFormulas = new List<FormulaEntity>
            {
                CreateTestFormula(),
                CreateTestFormula()
            };
            matchingFormulas[0].Name = "补气方";
            matchingFormulas[1].Name = "补气养血方";

            _repositoryMock.GetPagedAsync(1, 100, keyword)
                .Returns(new PagedResult<FormulaEntity>
                {
                    Items = matchingFormulas,
                    TotalCount = 2,
                    CurrentPage = 1,
                    PageSize = 100
                });

            // Act
            var result = await _formulaService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyKeyword_ShouldReturnEmptyList()
        {
            // Arrange
            var keyword = "";

            // Act
            var result = await _formulaService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();

            await _repositoryMock.DidNotReceive().GetAllAsync();
        }

        [Fact]
        public async Task SearchAsync_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var keyword = "不存在的方剂";

            _repositoryMock.GetPagedAsync(1, 100, keyword)
                .Returns(new PagedResult<FormulaEntity>
                {
                    Items = new List<FormulaEntity>(),
                    TotalCount = 0,
                    CurrentPage = 1,
                    PageSize = 100
                });

            // Act
            var result = await _formulaService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var keyword = "补气";
            var exception = new Exception("数据库错误");

            _repositoryMock.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _formulaService.SearchAsync(keyword));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region RestoreAsync 测试

        [Fact]
        public async Task RestoreAsync_WithDeletedFormula_ShouldRestore()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = CreateTestFormula(formulaId);
            formula.IsDeleted = true;

            _repositoryMock.GetByIdIncludingDeletedAsync(formulaId).Returns(formula);
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>()).Returns(formula);

            // Act
            var result = await _formulaService.RestoreAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            formula.IsDeleted.Should().BeFalse();

            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<FormulaEntity>());
        }

        [Fact]
        public async Task RestoreAsync_WithNonDeletedFormula_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = CreateTestFormula(formulaId);
            formula.IsDeleted = false;

            _repositoryMock.GetByIdIncludingDeletedAsync(formulaId).Returns(formula);

            // Act
            var result = await _formulaService.RestoreAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("未被删除");

            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<FormulaEntity>());
        }

        [Fact]
        public async Task RestoreAsync_WithNonExistingFormula_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.GetByIdIncludingDeletedAsync(formulaId).Returns((FormulaEntity?)null);

            // Act
            var result = await _formulaService.RestoreAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("方剂不存在");
        }

        #endregion

        #region 批量删除测试

        [Fact]
        public async Task BatchDeleteAsync_WithValidIds_ShouldSoftDeleteAll()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var formula1 = new FormulaEntity { Id = id1, Name = "方剂1", Effect = "功效1" };
            var formula2 = new FormulaEntity { Id = id2, Name = "方剂2", Effect = "功效2" };

            _repositoryMock.GetByIdAsync(id1).Returns(formula1);
            _repositoryMock.GetByIdAsync(id2).Returns(formula2);
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>())
                .Returns(callInfo => callInfo.Arg<FormulaEntity>());

            // Act
            var result = await _formulaService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(2);
            result.Data.FailureCount.Should().Be(0);

            await _repositoryMock.Received(2).UpdateAsync(Arg.Any<FormulaEntity>());
        }

        [Fact]
        public async Task BatchDeleteAsync_WithSomeNonExistent_ShouldReportPartial()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var formula1 = new FormulaEntity { Id = id1, Name = "方剂1", Effect = "功效1" };

            _repositoryMock.GetByIdAsync(id1).Returns(formula1);
            _repositoryMock.GetByIdAsync(id2).Returns((FormulaEntity?)null);
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>())
                .Returns(callInfo => callInfo.Arg<FormulaEntity>());

            // Act
            var result = await _formulaService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data.FailureCount.Should().Be(1);
        }

        [Fact]
        public async Task BatchDeleteAsync_WithEmptyList_ShouldReturnEmptyResult()
        {
            // Arrange
            var ids = new List<Guid>();

            // Act
            var result = await _formulaService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalCount.Should().Be(0);
            result.Data.SuccessCount.Should().Be(0);
        }

        [Fact]
        public async Task BatchDeleteAsync_WithException_ShouldIsolateErrors()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var formula1 = new FormulaEntity { Id = id1, Name = "方剂1", Effect = "功效1" };
            var formula2 = new FormulaEntity { Id = id2, Name = "方剂2", Effect = "功效2" };

            _repositoryMock.GetByIdAsync(id1).Returns(formula1);
            _repositoryMock.GetByIdAsync(id2).Returns(formula2);

            // 第一个成功，第二个抛异常
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>())
                .Returns(
                    callInfo => formula1,
                    callInfo => throw new Exception("Database error")
                );

            // Act
            var result = await _formulaService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data.FailureCount.Should().Be(1);
        }

        #endregion

        #region 批量更新状态测试

        [Fact]
        public async Task BatchUpdateStatusAsync_WithValidIds_ShouldUpdateAll()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };
            var targetStatus = LYBT.Shared.Models.Enums.CommonStatus.Disabled;

            var formula1 = new FormulaEntity { Id = id1, Name = "方剂1", Effect = "功效1", Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled };
            var formula2 = new FormulaEntity { Id = id2, Name = "方剂2", Effect = "功效2", Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled };

            _repositoryMock.GetByIdAsync(id1).Returns(formula1);
            _repositoryMock.GetByIdAsync(id2).Returns(formula2);
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>())
                .Returns(callInfo => callInfo.Arg<FormulaEntity>());
            _repositoryMock.SaveChangesAsync().Returns(0);

            // Act
            var result = await _formulaService.BatchUpdateStatusAsync(ids, targetStatus);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(2);
            result.Data.FailureCount.Should().Be(0);

            await _repositoryMock.Received(2).UpdateAsync(Arg.Any<FormulaEntity>());
        }

        [Fact]
        public async Task BatchUpdateStatusAsync_WithMixedResults_ShouldReportPartial()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };
            var targetStatus = LYBT.Shared.Models.Enums.CommonStatus.Disabled;

            var formula1 = new FormulaEntity { Id = id1, Name = "方剂1", Effect = "功效1", Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled };

            _repositoryMock.GetByIdAsync(id1).Returns(formula1);
            _repositoryMock.GetByIdAsync(id2).Returns((FormulaEntity?)null);
            _repositoryMock.UpdateAsync(Arg.Any<FormulaEntity>())
                .Returns(callInfo => callInfo.Arg<FormulaEntity>());
            _repositoryMock.SaveChangesAsync().Returns(0);

            // Act
            var result = await _formulaService.BatchUpdateStatusAsync(ids, targetStatus);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data.FailureCount.Should().Be(1);
        }

        [Fact]
        public async Task BatchUpdateStatusAsync_WithEmptyList_ShouldReturnEmptyResult()
        {
            // Arrange
            var ids = new List<Guid>();
            var targetStatus = LYBT.Shared.Models.Enums.CommonStatus.Disabled;

            _repositoryMock.SaveChangesAsync().Returns(0);

            // Act
            var result = await _formulaService.BatchUpdateStatusAsync(ids, targetStatus);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalCount.Should().Be(0);
            result.Data.SuccessCount.Should().Be(0);
        }

        #endregion

        #region 辅助方法

        private FormulaEntity CreateTestFormula(Guid? id = null)
        {
            var formulaId = id ?? Guid.NewGuid();
            return new FormulaEntity
            {
                Id = formulaId,
                Name = $"方剂_{formulaId.ToString().Substring(0, 8)}",
                Category = "补益剂",
                Effect = "补气养血",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private List<FormulaEntity> CreateTestFormulas(int count)
        {
            var formulas = new List<FormulaEntity>();
            for (int i = 0; i < count; i++)
            {
                formulas.Add(CreateTestFormula());
            }
            return formulas;
        }

        #endregion
    }
}
