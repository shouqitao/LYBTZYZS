using LYBT.Shared.Utilities.Configuration;

namespace LYBT.Tests.Unit.Utilities.Configuration
{
    /// <summary>
    /// EnvironmentHelper工具类单元测试
    /// </summary>
    public class EnvironmentHelperTests
    {
        #region Environments常量测试

        [Fact]
        public void Environments_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            EnvironmentHelper.Environments.Development.Should().Be("Development");
            EnvironmentHelper.Environments.Staging.Should().Be("Staging");
            EnvironmentHelper.Environments.Production.Should().Be("Production");
        }

        #endregion

        #region GetCurrentEnvironment方法测试

        [Fact]
        public void GetCurrentEnvironment_WithAspNetCoreEnvironment_ShouldReturnAspNetCoreValue()
        {
            // Arrange
            var envValue = "Testing";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", envValue);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            try
            {
                // Act
                var result = EnvironmentHelper.GetCurrentEnvironment();

                // Assert
                result.Should().Be(envValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void GetCurrentEnvironment_WithDotNetEnvironment_ShouldReturnDotNetValue()
        {
            // Arrange
            var envValue = "Testing";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", envValue);

            try
            {
                // Act
                var result = EnvironmentHelper.GetCurrentEnvironment();

                // Assert
                result.Should().Be(envValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void GetCurrentEnvironment_WithBothEnvironments_ShouldPrioritizeAspNetCore()
        {
            // Arrange
            var aspNetCoreValue = "AspNetCoreValue";
            var dotNetValue = "DotNetValue";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspNetCoreValue);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", dotNetValue);

            try
            {
                // Act
                var result = EnvironmentHelper.GetCurrentEnvironment();

                // Assert
                result.Should().Be(aspNetCoreValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
                Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void GetCurrentEnvironment_WithNoEnvironmentVariables_ShouldReturnDefaultDevelopment()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            try
            {
                // Act
                var result = EnvironmentHelper.GetCurrentEnvironment();

                // Assert
                result.Should().Be(EnvironmentHelper.Environments.Development);
            }
            finally
            {
                // Cleanup environment is not needed as we set to null
            }
        }

        [Fact]
        public void GetCurrentEnvironment_WithCustomDefault_ShouldReturnCustomDefault()
        {
            // Arrange
            var customDefault = "CustomDefault";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            try
            {
                // Act
                var result = EnvironmentHelper.GetCurrentEnvironment(customDefault);

                // Assert
                result.Should().Be(customDefault);
            }
            finally
            {
                // Cleanup environment is not needed as we set to null
            }
        }

        #endregion

        #region IsDevelopment方法测试

        [Theory]
        [InlineData("Development", true)]
        [InlineData("development", true)]
        [InlineData("DEVELOPMENT", true)]
        [InlineData("Production", false)]
        [InlineData("Staging", false)]
        [InlineData("Testing", false)]
        public void IsDevelopment_WithDifferentEnvironments_ShouldReturnCorrectResult(string environment, bool expected)
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            try
            {
                // Act
                var result = EnvironmentHelper.IsDevelopment();

                // Assert
                result.Should().Be(expected);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        #endregion

        #region IsStaging方法测试

        [Theory]
        [InlineData("Staging", true)]
        [InlineData("staging", true)]
        [InlineData("STAGING", true)]
        [InlineData("Development", false)]
        [InlineData("Production", false)]
        [InlineData("Testing", false)]
        public void IsStaging_WithDifferentEnvironments_ShouldReturnCorrectResult(string environment, bool expected)
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            try
            {
                // Act
                var result = EnvironmentHelper.IsStaging();

                // Assert
                result.Should().Be(expected);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        #endregion

        #region IsProduction方法测试

        [Theory]
        [InlineData("Production", true)]
        [InlineData("production", true)]
        [InlineData("PRODUCTION", true)]
        [InlineData("Development", false)]
        [InlineData("Staging", false)]
        [InlineData("Testing", false)]
        public void IsProduction_WithDifferentEnvironments_ShouldReturnCorrectResult(string environment, bool expected)
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            try
            {
                // Act
                var result = EnvironmentHelper.IsProduction();

                // Assert
                result.Should().Be(expected);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        #endregion

        #region GetEnvironmentVariable方法测试

        [Fact]
        public void GetEnvironmentVariable_WithExistingVariable_ShouldReturnValue()
        {
            // Arrange
            var key = "TEST_ENV_VAR";
            var value = "TestValue";
            Environment.SetEnvironmentVariable(key, value);

            try
            {
                // Act
                var result = EnvironmentHelper.GetEnvironmentVariable(key);

                // Assert
                result.Should().Be(value);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        [Fact]
        public void GetEnvironmentVariable_WithNonExistingVariable_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "NON_EXISTING_VAR";
            var defaultValue = "DefaultValue";

            // Act
            var result = EnvironmentHelper.GetEnvironmentVariable(key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetEnvironmentVariable_WithNonExistingVariableAndNoDefault_ShouldReturnEmptyString()
        {
            // Arrange
            var key = "NON_EXISTING_VAR";

            // Act
            var result = EnvironmentHelper.GetEnvironmentVariable(key);

            // Assert
            result.Should().Be(string.Empty);
        }

        #endregion

        #region GetRequiredEnvironmentVariable方法测试

        [Fact]
        public void GetRequiredEnvironmentVariable_WithExistingVariable_ShouldReturnValue()
        {
            // Arrange
            var key = "TEST_REQUIRED_VAR";
            var value = "TestValue";
            Environment.SetEnvironmentVariable(key, value);

            try
            {
                // Act
                var result = EnvironmentHelper.GetRequiredEnvironmentVariable(key);

                // Assert
                result.Should().Be(value);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetRequiredEnvironmentVariable_WithInvalidVariable_ShouldThrowException(string? value)
        {
            // Arrange
            var key = "TEST_REQUIRED_VAR";
            Environment.SetEnvironmentVariable(key, value);

            try
            {
                // Act & Assert
                var act = () => EnvironmentHelper.GetRequiredEnvironmentVariable(key);
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage($"环境变量 '{key}' 未设置");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        #endregion

        #region SetEnvironmentVariable方法测试

        [Fact]
        public void SetEnvironmentVariable_WithProcessTarget_ShouldSetVariable()
        {
            // Arrange
            var key = "TEST_SET_VAR";
            var value = "TestValue";

            try
            {
                // Act
                EnvironmentHelper.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);

                // Assert
                Environment.GetEnvironmentVariable(key).Should().Be(value);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        [Fact]
        public void SetEnvironmentVariable_WithDefaultTarget_ShouldSetVariable()
        {
            // Arrange
            var key = "TEST_SET_VAR_DEFAULT";
            var value = "TestValue";

            try
            {
                // Act
                EnvironmentHelper.SetEnvironmentVariable(key, value);

                // Assert
                Environment.GetEnvironmentVariable(key).Should().Be(value);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(key, null);
            }
        }

        #endregion

        #region GetEnvironmentSpecificFileName方法测试

        [Fact]
        public void GetEnvironmentSpecificFileName_WithCurrentEnvironment_ShouldReturnCorrectFileName()
        {
            // Arrange
            var baseFileName = "appsettings.json";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            try
            {
                // Act
                var result = EnvironmentHelper.GetEnvironmentSpecificFileName(baseFileName);

                // Assert
                result.Should().Be("appsettings.Development.json");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void GetEnvironmentSpecificFileName_WithSpecificEnvironment_ShouldReturnCorrectFileName()
        {
            // Arrange
            var baseFileName = "config.xml";
            var environment = "Production";

            // Act
            var result = EnvironmentHelper.GetEnvironmentSpecificFileName(baseFileName, environment);

            // Assert
            result.Should().Be("config.Production.xml");
        }

        [Fact]
        public void GetEnvironmentSpecificFileName_WithNoExtension_ShouldReturnCorrectFileName()
        {
            // Arrange
            var baseFileName = "logfile";
            var environment = "Staging";

            // Act
            var result = EnvironmentHelper.GetEnvironmentSpecificFileName(baseFileName, environment);

            // Assert
            result.Should().Be("logfile.Staging");
        }

        [Fact]
        public void GetEnvironmentSpecificFileName_WithMultipleDots_ShouldWorkCorrectly()
        {
            // Arrange
            var baseFileName = "app.config.json";
            var environment = "Test";

            // Act
            var result = EnvironmentHelper.GetEnvironmentSpecificFileName(baseFileName, environment);

            // Assert
            result.Should().Be("app.config.Test.json");
        }

        #endregion

        #region SelectByEnvironment方法测试

        [Fact]
        public void SelectByEnvironment_WithDevelopmentEnvironment_ShouldReturnDevelopmentValue()
        {
            // Arrange
            var devValue = "DevValue";
            var stagingValue = "StagingValue";
            var prodValue = "ProdValue";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            try
            {
                // Act
                var result = EnvironmentHelper.SelectByEnvironment(devValue, stagingValue, prodValue);

                // Assert
                result.Should().Be(devValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void SelectByEnvironment_WithStagingEnvironment_ShouldReturnStagingValue()
        {
            // Arrange
            var devValue = "DevValue";
            var stagingValue = "StagingValue";
            var prodValue = "ProdValue";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Staging");

            try
            {
                // Act
                var result = EnvironmentHelper.SelectByEnvironment(devValue, stagingValue, prodValue);

                // Assert
                result.Should().Be(stagingValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void SelectByEnvironment_WithProductionEnvironment_ShouldReturnProductionValue()
        {
            // Arrange
            var devValue = "DevValue";
            var stagingValue = "StagingValue";
            var prodValue = "ProdValue";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

            try
            {
                // Act
                var result = EnvironmentHelper.SelectByEnvironment(devValue, stagingValue, prodValue);

                // Assert
                result.Should().Be(prodValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void SelectByEnvironment_WithUnknownEnvironment_ShouldReturnDevelopmentValue()
        {
            // Arrange
            var devValue = "DevValue";
            var stagingValue = "StagingValue";
            var prodValue = "ProdValue";
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnknownEnv");

            try
            {
                // Act
                var result = EnvironmentHelper.SelectByEnvironment(devValue, stagingValue, prodValue);

                // Assert
                result.Should().Be(devValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        [Fact]
        public void SelectByEnvironment_WithDifferentTypes_ShouldWorkCorrectly()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

            try
            {
                // Act
                var result = EnvironmentHelper.SelectByEnvironment(10, 20, 30);

                // Assert
                result.Should().Be(10);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            }
        }

        #endregion

        #region GetMachineInfo方法测试

        [Fact]
        public void GetMachineInfo_ShouldReturnValidMachineInfo()
        {
            // Act
            var result = EnvironmentHelper.GetMachineInfo();

            // Assert
            result.Should().NotBeNull();
            result.MachineName.Should().NotBeNullOrEmpty();
            result.OSVersion.Should().NotBeNullOrEmpty();
            result.ProcessorCount.Should().BeGreaterThan(0);
            result.UserName.Should().NotBeNullOrEmpty();
            result.UserDomainName.Should().NotBeNullOrEmpty();
            result.CurrentDirectory.Should().NotBeNullOrEmpty();
            result.SystemDirectory.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GetMachineInfo_ShouldReturnConsistentResults()
        {
            // Act
            var result1 = EnvironmentHelper.GetMachineInfo();
            var result2 = EnvironmentHelper.GetMachineInfo();

            // Assert
            result1.MachineName.Should().Be(result2.MachineName);
            result1.OSVersion.Should().Be(result2.OSVersion);
            result1.ProcessorCount.Should().Be(result2.ProcessorCount);
            result1.Is64BitOperatingSystem.Should().Be(result2.Is64BitOperatingSystem);
            result1.Is64BitProcess.Should().Be(result2.Is64BitProcess);
        }

        #endregion

        #region ValidateEnvironment方法测试

        [Fact]
        public void ValidateEnvironment_WithAllVariablesPresent_ShouldReturnValidResult()
        {
            // Arrange
            var variables = new[] { "TEST_VAR1", "TEST_VAR2", "TEST_VAR3" };
            Environment.SetEnvironmentVariable("TEST_VAR1", "Value1");
            Environment.SetEnvironmentVariable("TEST_VAR2", "Value2");
            Environment.SetEnvironmentVariable("TEST_VAR3", "Value3");

            try
            {
                // Act
                var result = EnvironmentHelper.ValidateEnvironment(variables);

                // Assert
                result.IsValid.Should().BeTrue();
                result.MissingVariables.Should().BeEmpty();
            }
            finally
            {
                // Cleanup
                foreach (var variable in variables)
                {
                    Environment.SetEnvironmentVariable(variable, null);
                }
            }
        }

        [Fact]
        public void ValidateEnvironment_WithMissingVariables_ShouldReturnInvalidResult()
        {
            // Arrange
            var variables = new[] { "TEST_VAR1", "MISSING_VAR2", "TEST_VAR3" };
            Environment.SetEnvironmentVariable("TEST_VAR1", "Value1");
            Environment.SetEnvironmentVariable("MISSING_VAR2", null);
            Environment.SetEnvironmentVariable("TEST_VAR3", "Value3");

            try
            {
                // Act
                var result = EnvironmentHelper.ValidateEnvironment(variables);

                // Assert
                result.IsValid.Should().BeFalse();
                result.MissingVariables.Should().Contain("MISSING_VAR2");
                result.MissingVariables.Should().NotContain("TEST_VAR1", "TEST_VAR3");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("TEST_VAR1", null);
                Environment.SetEnvironmentVariable("TEST_VAR3", null);
            }
        }

        [Fact]
        public void ValidateEnvironment_WithEmptyVariableList_ShouldReturnValidResult()
        {
            // Act
            var result = EnvironmentHelper.ValidateEnvironment();

            // Assert
            result.IsValid.Should().BeTrue();
            result.MissingVariables.Should().BeEmpty();
        }

        [Fact]
        public void ValidateEnvironment_WithEmptyStringVariable_ShouldTreatAsMissing()
        {
            // Arrange
            var variable = "EMPTY_VAR";
            Environment.SetEnvironmentVariable(variable, "");

            try
            {
                // Act
                var result = EnvironmentHelper.ValidateEnvironment(variable);

                // Assert
                result.IsValid.Should().BeFalse();
                result.MissingVariables.Should().Contain(variable);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(variable, null);
            }
        }

        [Fact]
        public void ValidateEnvironment_WithWhitespaceVariable_ShouldTreatAsMissing()
        {
            // Arrange
            var variable = "WHITESPACE_VAR";
            Environment.SetEnvironmentVariable(variable, "   ");

            try
            {
                // Act
                var result = EnvironmentHelper.ValidateEnvironment(variable);

                // Assert
                result.IsValid.Should().BeFalse();
                result.MissingVariables.Should().Contain(variable);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(variable, null);
            }
        }

        #endregion

        #region MachineInfo类测试

        [Fact]
        public void MachineInfo_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var machineInfo = new MachineInfo();

            // Assert
            machineInfo.MachineName.Should().Be(string.Empty);
            machineInfo.OSVersion.Should().Be(string.Empty);
            machineInfo.ProcessorCount.Should().Be(0);
            machineInfo.Is64BitOperatingSystem.Should().BeFalse();
            machineInfo.Is64BitProcess.Should().BeFalse();
            machineInfo.UserName.Should().Be(string.Empty);
            machineInfo.UserDomainName.Should().Be(string.Empty);
            machineInfo.CurrentDirectory.Should().Be(string.Empty);
            machineInfo.SystemDirectory.Should().Be(string.Empty);
        }

        #endregion

        #region EnvironmentValidationResult类测试

        [Fact]
        public void EnvironmentValidationResult_WithNoMissingVariables_ShouldBeValid()
        {
            // Arrange
            var result = new EnvironmentValidationResult();

            // Assert
            result.IsValid.Should().BeTrue();
            result.GetErrorMessage().Should().BeEmpty();
        }

        [Fact]
        public void EnvironmentValidationResult_WithMissingVariables_ShouldBeInvalid()
        {
            // Arrange
            var result = new EnvironmentValidationResult();
            result.MissingVariables.Add("VAR1");
            result.MissingVariables.Add("VAR2");

            // Assert
            result.IsValid.Should().BeFalse();
            result.GetErrorMessage().Should().Be("以下环境变量缺失: VAR1, VAR2");
        }

        #endregion

        #region 综合集成测试

        [Fact]
        public void Integration_EnvironmentDetection_ShouldWorkCorrectly()
        {
            // Arrange
            var environments = new[] { "Development", "Staging", "Production" };

            foreach (var env in environments)
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", env);

                try
                {
                    // Act
                    var currentEnv = EnvironmentHelper.GetCurrentEnvironment();
                    var isDev = EnvironmentHelper.IsDevelopment();
                    var isStaging = EnvironmentHelper.IsStaging();
                    var isProd = EnvironmentHelper.IsProduction();

                    // Assert
                    currentEnv.Should().Be(env);

                    switch (env)
                    {
                        case "Development":
                            isDev.Should().BeTrue();
                            isStaging.Should().BeFalse();
                            isProd.Should().BeFalse();
                            break;
                        case "Staging":
                            isDev.Should().BeFalse();
                            isStaging.Should().BeTrue();
                            isProd.Should().BeFalse();
                            break;
                        case "Production":
                            isDev.Should().BeFalse();
                            isStaging.Should().BeFalse();
                            isProd.Should().BeTrue();
                            break;
                    }
                }
                finally
                {
                    // Cleanup
                    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
                }
            }
        }

        #endregion
    }
}
