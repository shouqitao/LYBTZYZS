using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Dtos;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Common;
using LYBT.WPF.Client.Core.Interfaces.Services.System;
using LYBT.WPF.Client.Core.Mvvm;
using LYBT.WPF.Client.Modules.SystemManagement.Users.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Regions;
using Prism.Services.Dialogs;
using Xunit;

namespace LYBT.WPF.Client.Tests.ViewModels
{
    /// <summary>
    /// 用户管理ViewModel单元测试
    /// 测试业务逻辑和用户交互
    /// </summary>
    public class UserManagementViewModelTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<UserManagementViewModel>> _mockLogger;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly UserManagementViewModel _viewModel;

        public UserManagementViewModelTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UserManagementViewModel>>();
            _mockDialogService = new Mock<IDialogService>();
            _mockRegionManager = new Mock<IRegionManager>();
            
            _viewModel = new UserManagementViewModel(
                _mockUserService.Object,
                _mockLogger.Object,
                _mockDialogService.Object,
                _mockRegionManager.Object);
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            // Assert
            _viewModel.Users.Should().NotBeNull();
            _viewModel.Users.Should().BeEmpty();
            _viewModel.IsLoading.Should().BeFalse();
            _viewModel.CurrentPage.Should().Be(1);
            _viewModel.PageSize.Should().Be(10);
            _viewModel.TotalCount.Should().Be(0);
        }

        [Fact]
        public void Commands_AreInitializedCorrectly()
        {
            // Assert
            _viewModel.LoadDataCommand.Should().NotBeNull();
            _viewModel.SearchCommand.Should().NotBeNull();
            _viewModel.ResetCommand.Should().NotBeNull();
            _viewModel.AddCommand.Should().NotBeNull();
            _viewModel.EditCommand.Should().NotBeNull();
            _viewModel.DeleteCommand.Should().NotBeNull();
            _viewModel.ToggleStatusCommand.Should().NotBeNull();
            _viewModel.ResetPasswordCommand.Should().NotBeNull();
            _viewModel.PageChangedCommand.Should().NotBeNull();
        }

        #endregion

        #region LoadData Tests

        [Fact]
        public async Task LoadDataCommand_WithSuccessfulResponse_PopulatesUsersList()
        {
            // Arrange
            var pagedResult = new PagedResult<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Id = Guid.NewGuid(), Username = "user1", RealName = "用户1" },
                    new UserDto { Id = Guid.NewGuid(), Username = "user2", RealName = "用户2" }
                },
                Total = 2,
                CurrentPage = 1,
                TotalPages = 1
            };

            _mockUserService.Setup(x => x.GetPagedAsync(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(pagedResult);

            // Act
            await _viewModel.LoadDataCommand.ExecuteAsync();

            // Assert
            _viewModel.Users.Should().HaveCount(2);
            _viewModel.TotalCount.Should().Be(2);
            _viewModel.TotalPages.Should().Be(1);
            _viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public async Task LoadDataCommand_SetsIsLoadingCorrectly()
        {
            // Arrange
            var tcs = new TaskCompletionSource<PagedResult<UserDto>>();
            _mockUserService.Setup(x => x.GetPagedAsync(It.IsAny<PaginationRequest>()))
                .Returns(tcs.Task);

            // Act
            var loadTask = _viewModel.LoadDataCommand.ExecuteAsync();
            
            // Assert - loading should be true
            _viewModel.IsLoading.Should().BeTrue();
            
            // Complete the task
            tcs.SetResult(new PagedResult<UserDto> { Items = new List<UserDto>() });
            await loadTask;
            
            // Assert - loading should be false
            _viewModel.IsLoading.Should().BeFalse();
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchCommand_WithKeyword_CallsServiceWithCorrectParameters()
        {
            // Arrange
            _viewModel.SearchKeyword = "admin";
            _mockUserService.Setup(x => x.GetPagedAsync(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(new PagedResult<UserDto>());

            // Act
            await _viewModel.SearchCommand.ExecuteAsync();

            // Assert
            _mockUserService.Verify(x => x.GetPagedAsync(
                It.Is<PaginationRequest>(req => 
                    req.SearchKeyword == "admin" && 
                    req.CurrentPage == 1)), 
                Times.Once);
        }

        [Fact]
        public async Task ResetCommand_ClearsSearchAndReloadsData()
        {
            // Arrange
            _viewModel.SearchKeyword = "test";
            _viewModel.CurrentPage = 3;
            _mockUserService.Setup(x => x.GetPagedAsync(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(new PagedResult<UserDto>());

            // Act
            await _viewModel.ResetCommand.ExecuteAsync();

            // Assert
            _viewModel.SearchKeyword.Should().BeEmpty();
            _viewModel.CurrentPage.Should().Be(1);
            _mockUserService.Verify(x => x.GetPagedAsync(It.IsAny<PaginationRequest>()), Times.Once);
        }

        #endregion

        #region Add User Tests

        [Fact]
        public void AddCommand_ShowsAddUserDialog()
        {
            // Arrange
            _mockDialogService.Setup(x => x.ShowDialog(
                It.IsAny<string>(),
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, parameters, callback) =>
                {
                    // 模拟对话框关闭
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.OK);
                    callback(result.Object);
                });

            // Act
            _viewModel.AddCommand.Execute();

            // Assert
            _mockDialogService.Verify(x => x.ShowDialog(
                "AddUserDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()), 
                Times.Once);
        }

        #endregion

        #region Edit User Tests

        [Fact]
        public void EditCommand_WithSelectedUser_ShowsEditDialog()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };
            _viewModel.SelectedUser = user;

            _mockDialogService.Setup(x => x.ShowDialog(
                It.IsAny<string>(),
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()));

            // Act
            _viewModel.EditCommand.Execute(user);

            // Assert
            _mockDialogService.Verify(x => x.ShowDialog(
                "EditUserDialog",
                It.Is<IDialogParameters>(p => p.GetValue<Guid>("userId") == user.Id),
                It.IsAny<Action<IDialogResult>>()), 
                Times.Once);
        }

        [Fact]
        public void EditCommand_WithoutUser_DoesNotShowDialog()
        {
            // Arrange
            _viewModel.SelectedUser = null;

            // Act
            _viewModel.EditCommand.Execute(null);

            // Assert
            _mockDialogService.Verify(x => x.ShowDialog(
                It.IsAny<string>(),
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()), 
                Times.Never);
        }

        #endregion

        #region Delete User Tests

        [Fact]
        public async Task DeleteCommand_WithConfirmation_DeletesUser()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };
            _viewModel.Users.Add(user);
            _viewModel.TotalCount = 1;

            // 模拟确认对话框
            _mockDialogService.Setup(x => x.ShowDialog(
                "ConfirmDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, parameters, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.OK);
                    callback(result.Object);
                });

            _mockUserService.Setup(x => x.DeleteAsync(user.Id))
                .ReturnsAsync(new ServiceResult { IsSuccessStatusCode = true });

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(user);

            // Assert
            _viewModel.Users.Should().NotContain(user);
            _viewModel.TotalCount.Should().Be(0);
            _mockUserService.Verify(x => x.DeleteAsync(user.Id), Times.Once);
        }

        [Fact]
        public async Task DeleteCommand_WithCancellation_DoesNotDelete()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };

            // 模拟取消对话框
            _mockDialogService.Setup(x => x.ShowDialog(
                "ConfirmDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, parameters, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.Cancel);
                    callback(result.Object);
                });

            // Act
            await _viewModel.DeleteCommand.ExecuteAsync(user);

            // Assert
            _mockUserService.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Toggle Status Tests

        [Fact]
        public async Task ToggleStatusCommand_WithActiveUser_TogglesStatus()
        {
            // Arrange
            var user = new UserDto 
            { 
                Id = Guid.NewGuid(), 
                Username = "testuser", 
                IsActive = true 
            };
            _viewModel.Users.Add(user);

            var toggledUser = new UserDto 
            { 
                Id = user.Id, 
                Username = user.Username, 
                IsActive = false 
            };

            _mockUserService.Setup(x => x.ToggleStatusAsync(user.Id))
                .ReturnsAsync(new ServiceResult<UserDto> 
                { 
                    IsSuccessStatusCode = true, 
                    Content = toggledUser 
                });

            // Act
            await _viewModel.ToggleStatusCommand.ExecuteAsync(user);

            // Assert
            var updatedUser = _viewModel.Users.FirstOrDefault(u => u.Id == user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleStatusCommand_WithError_ShowsErrorMessage()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };
            
            _mockUserService.Setup(x => x.ToggleStatusAsync(user.Id))
                .ReturnsAsync(new ServiceResult<UserDto> 
                { 
                    IsSuccessStatusCode = false,
                    Error = new ProblemDetails { Detail = "状态切换失败" }
                });

            // Act
            await _viewModel.ToggleStatusCommand.ExecuteAsync(user);

            // Assert
            _mockDialogService.Verify(x => x.ShowDialog(
                "NotificationDialog",
                It.Is<IDialogParameters>(p => 
                    p.GetValue<string>("message").Contains("状态切换失败")),
                It.IsAny<Action<IDialogResult>>()), 
                Times.Once);
        }

        #endregion

        #region Reset Password Tests

        [Fact]
        public async Task ResetPasswordCommand_WithConfirmation_ResetsPassword()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };

            // 模拟确认对话框
            _mockDialogService.Setup(x => x.ShowDialog(
                "ConfirmDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, parameters, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.OK);
                    callback(result.Object);
                });

            _mockUserService.Setup(x => x.ResetPasswordAsync(user.Id))
                .ReturnsAsync(new ServiceResult { IsSuccessStatusCode = true });

            // Act
            await _viewModel.ResetPasswordCommand.ExecuteAsync(user);

            // Assert
            _mockUserService.Verify(x => x.ResetPasswordAsync(user.Id), Times.Once);
            
            // 验证显示成功消息
            _mockDialogService.Verify(x => x.ShowDialog(
                "NotificationDialog",
                It.Is<IDialogParameters>(p => 
                    p.GetValue<string>("message").Contains("密码重置成功")),
                It.IsAny<Action<IDialogResult>>()), 
                Times.Once);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task PageChangedCommand_WithNewPage_LoadsCorrectPage()
        {
            // Arrange
            _viewModel.CurrentPage = 1;
            _mockUserService.Setup(x => x.GetPagedAsync(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(new PagedResult<UserDto>());

            // Act
            await _viewModel.PageChangedCommand.ExecuteAsync(2);

            // Assert
            _viewModel.CurrentPage.Should().Be(2);
            _mockUserService.Verify(x => x.GetPagedAsync(
                It.Is<PaginationRequest>(req => req.CurrentPage == 2)), 
                Times.Once);
        }

        [Fact]
        public void HasPreviousPage_WhenOnFirstPage_ReturnsFalse()
        {
            // Arrange
            _viewModel.CurrentPage = 1;

            // Act & Assert
            _viewModel.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public void HasNextPage_WhenOnLastPage_ReturnsFalse()
        {
            // Arrange
            _viewModel.CurrentPage = 3;
            _viewModel.TotalPages = 3;

            // Act & Assert
            _viewModel.HasNextPage.Should().BeFalse();
        }

        #endregion

        #region Property Change Tests

        [Fact]
        public void SearchKeyword_WhenChanged_RaisesPropertyChanged()
        {
            // Arrange
            var propertyChangedRaised = false;
            _viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(_viewModel.SearchKeyword))
                    propertyChangedRaised = true;
            };

            // Act
            _viewModel.SearchKeyword = "test";

            // Assert
            propertyChangedRaised.Should().BeTrue();
        }

        #endregion
    }
}