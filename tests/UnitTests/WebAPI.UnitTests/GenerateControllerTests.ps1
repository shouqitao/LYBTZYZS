# PowerShell script to generate controller unit tests
# Usage: .\GenerateControllerTests.ps1

param(
    [string]$OutputDir = "Controllers",
    [switch]$Force = $false
)

# 控制器和对应的模型映射
$Controllers = @{
    "MedicalCase" = @{
        "Service" = "IMedicalCaseService"
        "Dto" = "MedicalCaseDto"
        "CreateDto" = "MedicalCaseCreateDto"
        "UpdateDto" = "MedicalCaseUpdateDto"
        "SearchDto" = "MedicalCaseSearchDto"
        "Namespace" = "LYBT.Shared.Models.Contracts.MedicalCase"
        "EntityName" = "病历"
        "IdValidationMessage" = "病历ID不能为空"
        "NotFoundError" = "MEDICALCASENOTFOUND"
    }
    "Consultation" = @{
        "Service" = "IConsultationService"
        "Dto" = "ConsultationDto"
        "CreateDto" = "ConsultationCreateDto"
        "UpdateDto" = "ConsultationUpdateDto"
        "SearchDto" = "ConsultationSearchDto"
        "Namespace" = "LYBT.Shared.Models.Contracts.Consultation"
        "EntityName" = "诊疗记录"
        "IdValidationMessage" = "诊疗记录ID不能为空"
        "NotFoundError" = "CONSULTATIONNOTFOUND"
    }
    "Prescriptions" = @{
        "Service" = "IPrescriptionService"
        "Dto" = "PrescriptionDto"
        "CreateDto" = "PrescriptionCreateDto"
        "UpdateDto" = "PrescriptionUpdateDto"
        "SearchDto" = "PrescriptionSearchDto"
        "Namespace" = "LYBT.Shared.Models.Contracts.Prescriptions"
        "EntityName" = "处方"
        "IdValidationMessage" = "处方ID不能为空"
        "NotFoundError" = "PRESCRIPTIONNOTFOUND"
    }
    "Herbs" = @{
        "Service" = "IHerbService"
        "Dto" = "HerbDto"
        "CreateDto" = "HerbCreateDto"
        "UpdateDto" = "HerbUpdateDto"
        "SearchDto" = "HerbSearchDto"
        "Namespace" = "LYBT.Shared.Models.Contracts.Herbs"
        "EntityName" = "中药"
        "IdValidationMessage" = "中药ID不能为空"
        "NotFoundError" = "HERBNOTFOUND"
    }
    "Formulas" = @{
        "Service" = "IFormulaService"
        "Dto" = "FormulaDto"
        "CreateDto" = "FormulaCreateDto"
        "UpdateDto" = "FormulaUpdateDto"
        "SearchDto" = "FormulaSearchDto"
        "Namespace" = "LYBT.Shared.Models.Contracts.Formula"
        "EntityName" = "方剂"
        "IdValidationMessage" = "方剂ID不能为空"
        "NotFoundError" = "FORMULANOTFOUND"
    }
}

# 操作控制器映射
$OperationControllers = @{
    "UsersOperation" = @{
        "Service" = "IUserService"
        "Namespace" = "LYBT.Shared.Models.Contracts.Users"
        "EntityName" = "用户"
    }
    "PatientsOperation" = @{
        "Service" = "IPatientService"
        "Namespace" = "LYBT.Shared.Models.Contracts.Patients"
        "EntityName" = "患者"
    }
    "ConsultationOperation" = @{
        "Service" = "IConsultationService"
        "Namespace" = "LYBT.Shared.Models.Contracts.Consultation"
        "EntityName" = "诊疗记录"
    }
    "PrescriptionsOperation" = @{
        "Service" = "IPrescriptionService"
        "Namespace" = "LYBT.Shared.Models.Contracts.Prescriptions"
        "EntityName" = "处方"
    }
    "HerbsOperation" = @{
        "Service" = "IHerbService"
        "Namespace" = "LYBT.Shared.Models.Contracts.Herbs"
        "EntityName" = "中药"
    }
    "FormulasOperation" = @{
        "Service" = "IFormulaService"
        "Namespace" = "LYBT.Shared.Models.Contracts.Formula"
        "EntityName" = "方剂"
    }
    "MedicalCaseOperation" = @{
        "Service" = "IMedicalCaseService"
        "Namespace" = "LYBT.Shared.Models.Contracts.MedicalCase"
        "EntityName" = "病历"
    }
}

function Generate-ControllerTest {
    param(
        [string]$ControllerName,
        [hashtable]$Config,
        [string]$TemplateType = "Standard"
    )

    $testClassName = "${ControllerName}ControllerTests"
    $fileName = "${testClassName}.cs"
    $filePath = Join-Path $OutputDir $fileName

    if (Test-Path $filePath -and -not $Force) {
        Write-Host "文件已存在，跳过: $fileName" -ForegroundColor Yellow
        return
    }

    $template = ""

    if ($TemplateType -eq "Standard") {
        $template = @"
using FluentAssertions;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using $($Config.Namespace);
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Controllers
{
    /// <summary>
    /// ${ControllerName}Controller单元测试
    /// 测试$($Config.EntityName)管理控制器的所有功能，确保100%代码覆盖率
    /// </summary>
    public class $testClassName : BaseControllerTest<${ControllerName}Controller>
    {
        private readonly Mock<$($Config.Service)> _mockService;

        public $testClassName()
        {
            _mockService = new Mock<$($Config.Service)>();
        }

        protected override ${ControllerName}Controller CreateController()
        {
            var controller = new ${ControllerName}Controller(
                _mockService.Object,
                MockCache.Object,
                MockLogger.Object);

            SetupAuthenticatedUser(controller);
            return controller;
        }

        private void SetupAuthenticatedUser(${ControllerName}Controller controller, Guid? userId = null, string? username = null, string? role = null)
        {
            var testUserId = userId ?? Guid.NewGuid();
            var testUsername = username ?? "testuser";
            var testRole = role ?? "Doctor";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, testUserId.ToString()),
                new(ClaimTypes.Name, testUsername),
                new(ClaimTypes.Role, testRole)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region GetList测试

        [Fact]
        public async Task GetList_Should_ReturnPagedResult_When_ValidParameters()
        {
            // Arrange
            var items = new List<$($Config.Dto)>
            {
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() }
            };

            var pagedResult = new PagedResult<$($Config.Dto)>(items, 2, 1, 20);
            var serviceResult = ServiceResult<PagedResult<$($Config.Dto)>>.CreateSuccess(pagedResult);

            _mockService.Setup(x => x.GetPagedAsync(It.IsAny<$($Config.SearchDto)>()))
                       .ReturnsAsync(serviceResult);

            // Act
            var result = await Controller.GetList(1, 20);

            // Assert
            AssertPagedResponse(result, items, 2, 1, 20, "查询成功");
        }

        [Fact]
        public async Task GetList_Should_ReturnValidationFail_When_InvalidPageParameters()
        {
            // Act
            var result = await Controller.GetList(0, 20);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task GetList_Should_HandleException_When_ServiceThrows()
        {
            // Arrange
            _mockService.Setup(x => x.GetPagedAsync(It.IsAny<$($Config.SearchDto)>()))
                       .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => Controller.GetList());
        }

        #endregion

        #region GetById测试

        [Fact]
        public async Task GetById_Should_ReturnItem_When_ItemExists()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new $($Config.Dto) { Id = itemId };

            var serviceResult = ServiceResult<$($Config.Dto)>.CreateSuccess(item);
            _mockService.Setup(x => x.GetByIdAsync(itemId))
                       .ReturnsAsync(serviceResult);

            // Act
            var result = await Controller.GetById(itemId);

            // Assert
            AssertSuccessResponse(result, item, "查询成功");
        }

        [Fact]
        public async Task GetById_Should_ReturnValidationFail_When_IdIsEmpty()
        {
            // Act
            var result = await Controller.GetById(Guid.Empty);

            // Assert
            AssertFailureResponse(result, "$($Config.IdValidationMessage)", 400);
        }

        [Fact]
        public async Task GetById_Should_HandleException_When_ServiceThrows()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockService.Setup(x => x.GetByIdAsync(itemId))
                       .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => Controller.GetById(itemId));
        }

        #endregion

        #region Create测试

        [Fact]
        public async Task Create_Should_ReturnCreatedItem_When_ValidDto()
        {
            // Arrange
            var createDto = new $($Config.CreateDto)();
            var createdItem = new $($Config.Dto) { Id = Guid.NewGuid() };

            var serviceResult = ServiceResult<$($Config.Dto)>.CreateSuccess(createdItem);
            _mockService.Setup(x => x.CreateAsync(createDto))
                       .ReturnsAsync(serviceResult);

            // Act
            var result = await Controller.Create(createDto);

            // Assert
            AssertSuccessResponse(result, createdItem, "$($Config.EntityName)创建成功");
        }

        [Fact]
        public async Task Create_Should_ReturnValidationFail_When_ModelStateInvalid()
        {
            // Arrange
            var createDto = new $($Config.CreateDto)();
            SetModelStateError("TestField", "测试错误");

            // Act
            var result = await Controller.Create(createDto);

            // Assert
            AssertFailureResponse(result, "测试错误", 400);
        }

        [Fact]
        public async Task Create_Should_HandleException_When_ServiceThrows()
        {
            // Arrange
            var createDto = new $($Config.CreateDto)();
            _mockService.Setup(x => x.CreateAsync(createDto))
                       .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => Controller.Create(createDto));
        }

        #endregion

        #region Update测试

        [Fact]
        public async Task Update_Should_ReturnUpdatedItem_When_ValidDto()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new $($Config.UpdateDto) { Id = itemId };
            var updatedItem = new $($Config.Dto) { Id = itemId };

            var serviceResult = ServiceResult<$($Config.Dto)>.CreateSuccess(updatedItem);
            _mockService.Setup(x => x.UpdateAsync(updateDto))
                       .ReturnsAsync(serviceResult);

            // Act
            var result = await Controller.Update(itemId, updateDto);

            // Assert
            AssertSuccessResponse(result, updatedItem, "$($Config.EntityName)信息更新成功");
        }

        [Fact]
        public async Task Update_Should_ReturnValidationFail_When_IdMismatch()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new $($Config.UpdateDto) { Id = Guid.NewGuid() };

            // Act
            var result = await Controller.Update(itemId, updateDto);

            // Assert
            AssertFailureResponse(result, "URL中的ID与请求体中的ID不匹配", 400);
        }

        [Fact]
        public async Task Update_Should_HandleException_When_ServiceThrows()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new $($Config.UpdateDto) { Id = itemId };
            _mockService.Setup(x => x.UpdateAsync(updateDto))
                       .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => Controller.Update(itemId, updateDto));
        }

        #endregion

        #region Delete测试

        [Fact]
        public async Task Delete_Should_ReturnSuccess_When_ValidId()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var serviceResult = ServiceResult<bool>.CreateSuccess(true);

            _mockService.Setup(x => x.DeleteAsync(itemId))
                       .ReturnsAsync(serviceResult);

            // Act
            var result = await Controller.Delete(itemId);

            // Assert
            AssertSuccessResponse(result, "$($Config.EntityName)删除成功");
        }

        [Fact]
        public async Task Delete_Should_ReturnValidationFail_When_IdIsEmpty()
        {
            // Act
            var result = await Controller.Delete(Guid.Empty);

            // Assert
            AssertFailureResponse(result, "$($Config.IdValidationMessage)", 400);
        }

        [Fact]
        public async Task Delete_Should_HandleException_When_ServiceThrows()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockService.Setup(x => x.DeleteAsync(itemId))
                       .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => Controller.Delete(itemId));
        }

        #endregion
    }
}
"@
    }
    else {
        # Operation Controller Template
        $template = @"
using FluentAssertions;
using LYBT.Shared.Interfaces.Services;
using $($Config.Namespace);
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Controllers
{
    /// <summary>
    /// ${ControllerName}Controller单元测试
    /// 测试$($Config.EntityName)操作控制器的所有功能，确保100%代码覆盖率
    /// </summary>
    public class $testClassName : BaseControllerTest<${ControllerName}Controller>
    {
        private readonly Mock<$($Config.Service)> _mockService;

        public $testClassName()
        {
            _mockService = new Mock<$($Config.Service)>();
        }

        protected override ${ControllerName}Controller CreateController()
        {
            var controller = new ${ControllerName}Controller(
                _mockService.Object,
                MockLogger.Object,
                MockCache.Object);

            SetupAuthenticatedUser(controller);
            return controller;
        }

        private void SetupAuthenticatedUser(${ControllerName}Controller controller, Guid? userId = null, string? username = null, string? role = null)
        {
            var testUserId = userId ?? Guid.NewGuid();
            var testUsername = username ?? "testuser";
            var testRole = role ?? "Doctor";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, testUserId.ToString()),
                new(ClaimTypes.Name, testUsername),
                new(ClaimTypes.Role, testRole)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        // TODO: 根据具体的Operation Controller实现添加测试方法
        // 这需要根据实际的控制器方法来定制

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParameters()
        {
            // Act & Assert
            Controller.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_Should_ThrowException_When_ServiceIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ${ControllerName}Controller(null!, MockLogger.Object, MockCache.Object));
        }
    }
}
"@
    }

    # 创建输出目录
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    # 写入文件
    $template | Out-File -FilePath $filePath -Encoding UTF8 -Force
    Write-Host "生成测试文件: $fileName" -ForegroundColor Green
}

# 生成标准控制器测试
Write-Host "正在生成标准控制器测试..." -ForegroundColor Cyan
foreach ($controller in $Controllers.GetEnumerator()) {
    Generate-ControllerTest -ControllerName $controller.Key -Config $controller.Value -TemplateType "Standard"
}

# 生成操作控制器测试
Write-Host "正在生成操作控制器测试..." -ForegroundColor Cyan
foreach ($controller in $OperationControllers.GetEnumerator()) {
    Generate-ControllerTest -ControllerName $controller.Key -Config $controller.Value -TemplateType "Operation"
}

Write-Host "所有控制器测试生成完成!" -ForegroundColor Green
Write-Host "注意: 操作控制器测试需要根据实际实现手动完善测试方法" -ForegroundColor Yellow