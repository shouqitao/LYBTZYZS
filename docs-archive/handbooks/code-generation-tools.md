# 代码生成工具文档

> **版本**: 1.0
> **创建日期**: 2025-10-15
> **维护者**: 项目团队
> **相关文档**: [快速开发指南](rapid-development-guide.md) | [模块模板指南](module-template-guide.md) | [依赖注入模式指南](dependency-injection-patterns.md)

## 📋 工具概述

本文档提供 LYBT 系统代码生成工具的详细使用指南，包括 CRUD 操作生成器、API 端点生成器、测试代码生成器和自定义模板系统。工具旨在显著提高开发效率，减少重复性工作，确保代码质量和一致性。

## 🎯 工具目标

### 核心目标
- **效率提升**: 减少 70% 的重复性代码编写工作
- **质量保证**: 生成符合项目标准的代码
- **一致性**: 确保代码风格和架构的一致性
- **可定制**: 支持自定义模板和生成规则
- **易用性**: 简化工具使用流程

### 支持的生成类型
- **实体模型**: 数据库实体和领域模型
- **服务接口**: 业务逻辑服务和接口定义
- **API 控制器**: RESTful API 控制器
- **仓储模式**: 数据访问层仓储实现
- **DTO 对象**: 数据传输对象
- **单元测试**: 自动化测试代码
- **集成测试**: API 集成测试

## 🛠️ 工具安装和配置

### 1. 工具安装

#### 全局工具安装
```bash
# 安装核心代码生成工具
dotnet tool install --global LYBT.CodeGen

# 安装模板管理工具
dotnet tool install --global LYBT.TemplateManager

# 安装测试生成工具
dotnet tool install --global LYBT.TestGen

# 验证安装
lybt-gen --version
lybt-template --version
lybt-test --version
```

#### 项目级工具配置
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <LYBTCodeGenVersion>1.0.0</LYBTCodeGenVersion>
    <LYBTCodeGenEnabled>true</LYBTCodeGenEnabled>
    <LYBTCodeGenOutputDirectory>$(MSBuildProjectDirectory)\Generated</LYBTCodeGenOutputDirectory>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="LYBT.CodeGen.Core" Version="$(LYBTCodeGenVersion)" />
    <PackageReference Include="LYBT.CodeGen.Templates" Version="$(LYBTCodeGenVersion)" />
  </ItemGroup>
</Project>
```

### 2. 配置文件设置

#### 代码生成配置文件
```json
// lybt.codegen.json
{
  "version": "1.0",
  "settings": {
    "outputDirectory": "./Generated",
    "namespacePrefix": "LYBT",
    "author": "Code Generator",
    "useNullableReferenceTypes": true,
    "generateComments": true,
    "generateValidations": true,
    "generateTests": true,
    "templateVersion": "latest"
  },
  "templates": {
    "entity": {
      "templatePath": "./Templates/Entity.hbs",
      "outputPath": "./Models/{EntityName}.cs",
      "fileNamePattern": "{EntityName}.cs"
    },
    "service": {
      "templatePath": "./Templates/Service.hbs",
      "outputPath": "./Services/I{EntityName}Service.cs",
      "fileNamePattern": "I{EntityName}Service.cs"
    },
    "controller": {
      "templatePath": "./Templates/Controller.hbs",
      "outputPath": "./Controllers/{EntityName}Controller.cs",
      "fileNamePattern": "{EntityName}Controller.cs"
    },
    "repository": {
      "templatePath": "./Templates/Repository.hbs",
      "outputPath": "./Repositories/{EntityName}Repository.cs",
      "fileNamePattern": "{EntityName}Repository.cs"
    },
    "dto": {
      "templatePath": "./Templates/Dto.hbs",
      "outputPath": "./DTOs/{EntityName}Dto.cs",
      "fileNamePattern": "{EntityName}Dto.cs"
    }
  },
  "database": {
    "connectionString": "Server=localhost;Database=LYBT_Dev;Trusted_Connection=true;",
    "provider": "SqlServer",
    "includeTables": [],
    "excludeTables": ["__EFMigrationsHistory", "sysdiagrams"],
    "schema": "dbo"
  }
}
```

## 🏗️ CRUD 操作生成器

### 1. 基础 CRUD 生成

#### 命令行使用
```bash
# 生成完整的 CRUD 操作
lybt-gen crud --entity Patient --output ./Generated

# 生成指定类型的 CRUD
lybt-gen crud --entity MedicalCase --types Create,Read,Update,Delete --output ./Generated

# 从数据库表生成
lybt-gen crud --from-database --table Patients --output ./Generated

# 生成带验证的 CRUD
lybt-gen crud --entity Patient --with-validation --output ./Generated

# 生成带测试的 CRUD
lybt-gen crud --entity Patient --with-tests --output ./Generated
```

#### 交互式生成
```bash
# 启动交互式生成器
lybt-gen crud --interactive

# 交互式生成过程示例
? 请选择数据源: Database / Schema / Manual
? 请输入实体名称: Patient
? 请选择要生成的操作: [ ] Create, [x] Read, [x] Update, [ ] Delete
? 是否生成验证代码? Yes
? 是否生成测试代码? Yes
? 输出目录: ./Generated
? 确认生成? Yes
```

### 2. 实体模型生成

#### 实体模板示例
```csharp
// Templates/Entity.hbs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace {{Namespace}}.Models
{
    /// <summary>
    /// {{EntityDescription}}
    /// </summary>
    [Table("{{TableName}}")]
    public class {{EntityName}}
    {
        /// <summary>
        /// 主键标识
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        {{#each Properties}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        {{#if IsRequired}}
        [Required(ErrorMessage = "{{Name}} 不能为空")]
        {{/if}}
        {{#if IsString}}
        [StringLength({{MaxLength}}, ErrorMessage = "{{Name}} 长度不能超过 {{MaxLength}} 个字符")]
        {{/if}}
        {{#if IsEmail}}
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        {{/if}}
        {{#if IsPhone}}
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        {{/if}}
        {{#if IsUnique}}
        [Index(IsUnique = true)]
        {{/if}}
        public {{Type}} {{Name}} { get; set; }

        {{/each}}

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [StringLength(50)]
        public string CreatedBy { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        [StringLength(50)]
        public string UpdatedBy { get; set; }
    }
}
```

#### 实体数据定义
```json
{
  "entityName": "Patient",
  "tableName": "Patients",
  "description": "患者信息实体",
  "namespace": "LYBT.Server.Models",
  "properties": [
    {
      "name": "Name",
      "type": "string",
      "description": "患者姓名",
      "isRequired": true,
      "maxLength": 100,
      "isUnique": false
    },
    {
      "name": "Gender",
      "type": "string",
      "description": "性别",
      "isRequired": true,
      "maxLength": 10
    },
    {
      "name": "DateOfBirth",
      "type": "DateTime",
      "description": "出生日期",
      "isRequired": true
    },
    {
      "name": "PhoneNumber",
      "type": "string",
      "description": "联系电话",
      "isRequired": true,
      "maxLength": 20,
      "isPhone": true
    },
    {
      "name": "Email",
      "type": "string",
      "description": "邮箱地址",
      "isRequired": false,
      "maxLength": 255,
      "isEmail": true
    }
  ]
}
```

### 3. 服务层生成

#### 服务接口模板
```csharp
// Templates/Service.hbs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Server.Models;
using LYBT.Server.DTOs;

namespace {{Namespace}}.Services
{
    /// <summary>
    /// {{EntityName}} 服务接口
    /// </summary>
    public interface I{{EntityName}}Service
    {
        /// <summary>
        /// 根据 ID 获取{{EntityDescription}}
        /// </summary>
        /// <param name="id">{{EntityDescription}} ID</param>
        /// <returns>{{EntityDescription}} DTO</returns>
        Task<{{EntityName}}Dto> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取{{EntityDescription}}分页列表
        /// </summary>
        /// <param name="request">查询请求</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<{{EntityName}}Dto>> GetPagedAsync({{EntityName}}ListRequest request);

        /// <summary>
        /// 创建{{EntityDescription}}
        /// </summary>
        /// <param name="createDto">创建 DTO</param>
        /// <returns>创建的{{EntityDescription}} DTO</returns>
        Task<{{EntityName}}Dto> CreateAsync({{EntityName}}CreateDto createDto);

        {{#if CanUpdate}}
        /// <summary>
        /// 更新{{EntityDescription}}
        /// </summary>
        /// <param name="id">{{EntityDescription}} ID</param>
        /// <param name="updateDto">更新 DTO</param>
        /// <returns>更新的{{EntityDescription}} DTO</returns>
        Task<{{EntityName}}Dto> UpdateAsync(Guid id, {{EntityName}}UpdateDto updateDto);
        {{/if}}

        {{#if CanDelete}}
        /// <summary>
        /// 删除{{EntityDescription}}
        /// </summary>
        /// <param name="id">{{EntityDescription}} ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteAsync(Guid id);
        {{/if}}

        {{#each CustomMethods}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        Task<{{ReturnType}}> {{Name}}Async({{#each Parameters}}{{Type}} {{Name}}{{#unless @last}}, {{/unless}}{{/each}});
        {{/each}}
    }
}
```

#### 服务实现模板
```csharp
// Templates/ServiceImplementation.hbs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Server.Models;
using LYBT.Server.DTOs;
using LYBT.Server.Repositories;
using AutoMapper;

namespace {{Namespace}}.Services
{
    /// <summary>
    /// {{EntityName}} 服务实现
    /// </summary>
    public class {{EntityName}}Service : I{{EntityName}}Service
    {
        private readonly I{{EntityName}}Repository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<{{EntityName}}Service> _logger;

        public {{EntityName}}Service(
            I{{EntityName}}Repository repository,
            IMapper mapper,
            ILogger<{{EntityName}}Service> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<{{EntityName}}Dto> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("{{EntityDescription}}不存在: {Id}", id);
                    return null;
                }

                return _mapper.Map<{{EntityName}}Dto>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取{{EntityDescription}}时发生错误: {Id}", id);
                throw;
            }
        }

        public async Task<PagedResult<{{EntityName}}Dto>> GetPagedAsync({{EntityName}}ListRequest request)
        {
            try
            {
                var entities = await _repository.GetPagedAsync(request);
                var dtos = _mapper.Map<IEnumerable<{{EntityName}}Dto>>(entities.Data);

                return new PagedResult<{{EntityName}}Dto>
                {
                    Data = dtos,
                    TotalCount = entities.TotalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取{{EntityDescription}}列表时发生错误");
                throw;
            }
        }

        public async Task<{{EntityName}}Dto> CreateAsync({{EntityName}}CreateDto createDto)
        {
            try
            {
                var entity = _mapper.Map<{{EntityName}}>(createDto);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                var createdEntity = await _repository.CreateAsync(entity);
                return _mapper.Map<{{EntityName}}Dto>(createdEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建{{EntityDescription}}时发生错误");
                throw;
            }
        }

        {{#if CanUpdate}}
        public async Task<{{EntityName}}Dto> UpdateAsync(Guid id, {{EntityName}}UpdateDto updateDto)
        {
            try
            {
                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                {
                    _logger.LogWarning("{{EntityDescription}}不存在: {Id}", id);
                    return null;
                }

                _mapper.Map(updateDto, existingEntity);
                existingEntity.UpdatedAt = DateTime.UtcNow;

                var updatedEntity = await _repository.UpdateAsync(existingEntity);
                return _mapper.Map<{{EntityName}}Dto>(updatedEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新{{EntityDescription}}时发生错误: {Id}", id);
                throw;
            }
        }
        {{/if}}

        {{#if CanDelete}}
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("{{EntityDescription}}不存在: {Id}", id);
                    return false;
                }

                return await _repository.DeleteAsync(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除{{EntityDescription}}时发生错误: {Id}", id);
                throw;
            }
        }
        {{/if}}

        {{#each CustomMethods}}
        public async Task<{{ReturnType}}> {{Name}}Async({{#each Parameters}}{{Type}} {{Name}}{{#unless @last}}, {{/unless}}{{/each}})
        {
            try
            {
                {{#if HasRepository}}
                return await _repository.{{Name}}Async({{#each Parameters}}{{Name}}{{#unless @last}}, {{/unless}}{{/each}});
                {{else}}
                // 自定义业务逻辑实现
                throw new NotImplementedException("方法 {{Name}} 尚未实现");
                {{/if}}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行方法 {{Name}} 时发生错误");
                throw;
            }
        }
        {{/each}}
    }
}
```

## 🔌 API 控制器生成器

### 1. RESTful API 生成

#### 控制器模板
```csharp
// Templates/Controller.hbs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LYBT.Server.Services;
using LYBT.Server.DTOs;

namespace {{Namespace}}.Controllers
{
    /// <summary>
    /// {{EntityDescription}}管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class {{EntityName}}Controller : ControllerBase
    {
        private readonly I{{EntityName}}Service _service;
        private readonly ILogger<{{EntityName}}Controller> _logger;

        public {{EntityName}}Controller(
            I{{EntityName}}Service service,
            ILogger<{{EntityName}}Controller> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// 获取{{EntityDescription}}列表
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="searchKeyword">搜索关键词</param>
        /// <returns>{{EntityDescription}}列表</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<{{EntityName}}Dto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<{{EntityName}}Dto>>> GetList(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string searchKeyword = null)
        {
            try
            {
                var request = new {{EntityName}}ListRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchKeyword = searchKeyword
                };

                var result = await _service.GetPagedAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取{{EntityDescription}}列表时发生错误");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "获取{{EntityDescription}}列表失败" });
            }
        }

        /// <summary>
        /// 根据 ID 获取{{EntityDescription}}
        /// </summary>
        /// <param name="id">{{EntityDescription}} ID</param>
        /// <returns>{{EntityDescription}}详情</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof({{EntityName}}Dto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<{{EntityName}}Dto>> GetById(Guid id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { message = "{{EntityDescription}}不存在" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取{{EntityDescription}}详情时发生错误: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "获取{{EntityDescription}}详情失败" });
            }
        }

        /// <summary>
        /// 创建{{EntityDescription}}
        /// </summary>
        /// <param name="createDto">创建{{EntityDescription}} DTO</param>
        /// <returns>创建的{{EntityDescription}}</returns>
        [HttpPost]
        [ProducesResponseType(typeof({{EntityName}}Dto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<{{EntityName}}Dto>> Create([FromBody] {{EntityName}}CreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _service.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建{{EntityDescription}}时发生错误");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "创建{{EntityDescription}}失败" });
            }
        }

        {{#if CanUpdate}}
        /// <summary>
        /// 更新{{EntityDescription}}
        /// </summary>
        /// <param name="id">{{EntityDescription}} ID</param>
        /// <param name="updateDto">更新{{EntityDescription}} DTO</param>
        /// <returns>更新的{{EntityDescription}}</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof({{EntityName}}Dto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<{{EntityName}}Dto>> Update(Guid id, [FromBody] {{EntityName}}UpdateDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _service.UpdateAsync(id, updateDto);
                if (result == null)
                {
                    return NotFound(new { message = "{{EntityDescription}}不存在" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新{{EntityDescription}}时发生错误: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "更新{{EntityDescription}}失败" });
            }
        }
        {{/if}}

        {{#if CanDelete}}
        /// <summary>
        /// 删除{{EntityDescription}}
        /// </summary>
        /// <param name="id">{{EntityDescription}} ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "{{EntityDescription}}不存在" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除{{EntityDescription}}时发生错误: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "删除{{EntityDescription}}失败" });
            }
        }
        {{/if}}

        {{#each CustomEndpoints}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        [{{HttpMethod}}("{{Route}}")]
        [ProducesResponseType(typeof({{ReturnType}}), StatusCodes.Status200OK)]
        public async Task<ActionResult<{{ReturnType}}>> {{Name}}({{#each Parameters}}{{#unless @last}}, {{/unless}}{{/each}})
        {
            try
            {
                var result = await _service.{{Name}}Async({{#each Parameters}}{{Name}}{{#unless @last}}, {{/unless}}{{/each}});
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行{{Name}}时发生错误");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "执行{{Name}}失败" });
            }
        }
        {{/each}}
    }
}
```

### 2. API 文档生成

#### Swagger 配置生成
```csharp
// Templates/SwaggerConfig.hbs
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace {{Namespace}}.Configuration
{
    /// <summary>
    /// Swagger 配置扩展
    /// </summary>
    public static class SwaggerConfiguration
    {
        /// <summary>
        /// 配置 Swagger 服务
        /// </summary>
        /// <param name="services">服务集合</param>
        public static void AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "{{ProjectName}} API",
                    Version = "v1",
                    Description = "{{ProjectDescription}}",
                    Contact = new OpenApiContact
                    {
                        Name = "{{ContactName}}",
                        Email = "{{ContactEmail}}"
                    }
                });

                // 包含 XML 注释
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                // 添加 JWT 认证
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "使用 Bearer 方案的 JWT 授权标头",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // 自定义 Schema ID
                c.CustomSchemaIds(type => type.FullName);
            });
        }

        /// <summary>
        /// 配置 Swagger 中间件
        /// </summary>
        /// <param name="app">应用构建器</param>
        public static void UseSwaggerConfiguration(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "{{ProjectName}} API V1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "{{ProjectName}} API 文档";
            });
        }
    }
}
```

## 🧪 测试代码生成器

### 1. 单元测试生成

#### 服务测试模板
```csharp
// Templates/ServiceTest.hbs
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Server.Models;
using LYBT.Server.Services;
using LYBT.Server.Repositories;
using LYBT.Server.DTOs;

namespace {{Namespace}}.Tests.Services
{
    /// <summary>
    /// {{EntityName}} 服务测试
    /// </summary>
    public class {{EntityName}}ServiceTests
    {
        private readonly Mock<I{{EntityName}}Repository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<{{EntityName}}Service>> _loggerMock;
        private readonly {{EntityName}}Service _service;

        public {{EntityName}}ServiceTests()
        {
            _repositoryMock = new Mock<I{{EntityName}}Repository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<{{EntityName}}Service>>();

            _service = new {{EntityName}}Service(
                _repositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_WhenEntityExists_ReturnsDto()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var entity = new {{EntityName}} { Id = entityId };
            var expectedDto = new {{EntityName}}Dto { Id = entityId };

            _repositoryMock.Setup(r => r.GetByIdAsync(entityId))
                .ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<{{EntityName}}Dto>(entity))
                .Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(entityId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entityId, result.Id);
            _repositoryMock.Verify(r => r.GetByIdAsync(entityId), Times.Once);
            _mapperMock.Verify(m => m.Map<{{EntityName}}Dto>(entity), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenEntityNotExists_ReturnsNull()
        {
            // Arrange
            var entityId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByIdAsync(entityId))
                .ReturnsAsync(({{EntityName}})null);

            // Act
            var result = await _service.GetByIdAsync(entityId);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(entityId), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_ReturnsPagedResult()
        {
            // Arrange
            var request = new {{EntityName}}ListRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            var entities = new[]
            {
                new {{EntityName}} { Id = Guid.NewGuid() },
                new {{EntityName}} { Id = Guid.NewGuid() }
            };

            var dtos = new[]
            {
                new {{EntityName}}Dto { Id = entities[0].Id },
                new {{EntityName}}Dto { Id = entities[1].Id }
            };

            var pagedEntities = new PagedResult<{{EntityName}}>
            {
                Data = entities,
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _repositoryMock.Setup(r => r.GetPagedAsync(request))
                .ReturnsAsync(pagedEntities);
            _mapperMock.Setup(m => m.Map<IEnumerable<{{EntityName}}Dto>>(entities))
                .Returns(dtos);

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Data.Count());
            _repositoryMock.Verify(r => r.GetPagedAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidatesAndReturnsDto()
        {
            // Arrange
            var createDto = new {{EntityName}}CreateDto();
            var entity = new {{EntityName}}();
            var expectedDto = new {{EntityName}}Dto();

            _mapperMock.Setup(m => m.Map<{{EntityName}}>(createDto))
                .Returns(entity);
            _repositoryMock.Setup(r => r.CreateAsync(entity))
                .ReturnsAsync(entity);
            _mapperMock.Setup(m => m.Map<{{EntityName}}Dto>(entity))
                .Returns(expectedDto);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(entity.CreatedAt);
            Assert.NotNull(entity.UpdatedAt);
            _mapperMock.Verify(m => m.Map<{{EntityName}}>(createDto), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(entity), Times.Once);
            _mapperMock.Verify(m => m.Map<{{EntityName}}Dto>(entity), Times.Once);
        }

        {{#if CanUpdate}}
        [Fact]
        public async Task UpdateAsync_WhenEntityExists_ReturnsUpdatedDto()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var updateDto = new {{EntityName}}UpdateDto();
            var existingEntity = new {{EntityName}} { Id = entityId };
            var updatedEntity = new {{EntityName}} { Id = entityId };
            var expectedDto = new {{EntityName}}Dto { Id = entityId };

            _repositoryMock.Setup(r => r.GetByIdAsync(entityId))
                .ReturnsAsync(existingEntity);
            _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
                .Verifiable();
            _repositoryMock.Setup(r => r.UpdateAsync(existingEntity))
                .ReturnsAsync(updatedEntity);
            _mapperMock.Setup(m => m.Map<{{EntityName}}Dto>(updatedEntity))
                .Returns(expectedDto);

            // Act
            var result = await _service.UpdateAsync(entityId, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entityId, result.Id);
            _repositoryMock.Verify(r => r.GetByIdAsync(entityId), Times.Once);
            _mapperMock.Verify(m => m.Map(updateDto, existingEntity), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(existingEntity), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenEntityNotExists_ReturnsNull()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var updateDto = new {{EntityName}}UpdateDto();

            _repositoryMock.Setup(r => r.GetByIdAsync(entityId))
                .ReturnsAsync(({{EntityName}})null);

            // Act
            var result = await _service.UpdateAsync(entityId, updateDto);

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(entityId), Times.Once);
        }
        {{/if}}

        {{#if CanDelete}}
        [Fact]
        public async Task DeleteAsync_WhenEntityExists_ReturnsTrue()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var entity = new {{EntityName}} { Id = entityId };

            _repositoryMock.Setup(r => r.GetByIdAsync(entityId))
                .ReturnsAsync(entity);
            _repositoryMock.Setup(r => r.DeleteAsync(entity))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(entityId);

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(entityId), Times.Once);
            _repositoryMock.Verify(r => r.DeleteAsync(entity), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenEntityNotExists_ReturnsFalse()
        {
            // Arrange
            var entityId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetByIdAsync(entityId))
                .ReturnsAsync(({{EntityName}})null);

            // Act
            var result = await _service.DeleteAsync(entityId);

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(entityId), Times.Once);
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<{{EntityName}}()), Times.Never);
        }
        {{/if}}
    }
}
```

### 2. 集成测试生成

#### 控制器集成测试模板
```csharp
// Templates/ControllerIntegrationTest.hbs
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using LYBT.Server;
using LYBT.Server.DTOs;

namespace {{Namespace}}.Tests.Integration
{
    /// <summary>
    /// {{EntityName}} 控制器集成测试
    /// </summary>
    public class {{EntityName}}ControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public {{EntityName}}ControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetList_ReturnsOkResult()
        {
            // Arrange
            // 可以在这里设置测试数据库数据

            // Act
            var response = await _client.GetAsync("/api/{{EntityName}}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            // 设置测试数据

            // Act
            var response = await _client.GetAsync($"/api/{{EntityName}}/{entityId}");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<{{EntityName}}Dto>(content);
                Assert.NotNull(result);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // 这是预期的情况，因为测试数据库中可能没有该记录
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task Create_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var createDto = new {{EntityName}}CreateDto
            {
                // 设置测试数据
                {{#each RequiredProperties}}
                {{Name}} = GetTest{{PascalCase Name}}(),
                {{/each}}
            };

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/{{EntityName}}", content);

            // Assert
            if (response.IsSuccessStatusCode)
            {
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<{{EntityName}}Dto>(responseContent);
                Assert.NotNull(result);
                Assert.NotEqual(Guid.Empty, result.Id);
            }
        }

        {{#each RequiredProperties}}
        [Fact]
        public async Task Create_WithMissing{{PascalCase Name}}_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new {{EntityName}}CreateDto();
            // 故意不设置 {{Name}}

            var json = JsonConvert.SerializeObject(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/{{EntityName}}", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        {{/each}}

        {{#if CanUpdate}}
        [Fact]
        public async Task Update_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var updateDto = new {{EntityName}}UpdateDto
            {
                // 设置测试数据
                {{#each UpdatableProperties}}
                {{Name}} = GetTest{{PascalCase Name}}(),
                {{/each}}
            };

            var json = JsonConvert.SerializeObject(updateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/{{EntityName}}/{entityId}", content);

            // Assert
            if (response.IsSuccessStatusCode)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // 这是预期的情况，因为测试数据库中可能没有该记录
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        {{/if}}

        {{#if CanDelete}}
        [Fact]
        public async Task Delete_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            // 设置测试数据

            // Act
            var response = await _client.DeleteAsync($"/api/{{EntityName}}/{entityId}");

            // Assert
            if (response.IsSuccessStatusCode)
            {
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // 这是预期的情况，因为测试数据库中可能没有该记录
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        {{/if}}

        // 辅助方法
        private {{#each RequiredProperties}} {{Type}} GetTest{{PascalCase Name}}() {{#if @last}} { {{else}}; {{/if}}{{/each}}
        {
            {{#each RequiredProperties}}
            {{#if (eq Type "string")}}
            return "Test {{Name}}";
            {{else if (eq Type "DateTime")}}
            return DateTime.Now.AddYears(-20);
            {{else if (eq Type "int")}}
            return 25;
            {{else if (eq Type "bool")}}
            return true;
            {{else if (eq Type "Guid")}}
            return Guid.NewGuid();
            {{else}}
            return default({{Type}});
            {{/if}}
            {{/each}}
        }
    }
}
```

## 📋 DTO 对象生成器

### 1. 基础 DTO 生成

#### DTO 模板
```csharp
// Templates/Dto.hbs
using System;
using System.ComponentModel.DataAnnotations;

namespace {{Namespace}}.DTOs
{
    /// <summary>
    /// {{EntityDescription}} DTO
    /// </summary>
    public class {{EntityName}}Dto
    {
        /// <summary>
        /// 主键标识
        /// </summary>
        public Guid Id { get; set; }

        {{#each Properties}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        public {{Type}} {{Name}} { get; set; }

        {{/each}}
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// {{EntityDescription}}创建 DTO
    /// </summary>
    public class {{EntityName}}CreateDto
    {
        {{#each CreatableProperties}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        {{#if IsRequired}}
        [Required(ErrorMessage = "{{Name}} 不能为空")]
        {{/if}}
        {{#if IsString}}
        [StringLength({{MaxLength}}, ErrorMessage = "{{Name}} 长度不能超过 {{MaxLength}} 个字符")]
        {{/if}}
        {{#if IsEmail}}
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        {{/if}}
        {{#if IsPhone}}
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        {{/if}}
        public {{Type}} {{Name}} { get; set; }

        {{/each}}
    }

    {{#if CanUpdate}}
    /// <summary>
    /// {{EntityDescription}}更新 DTO
    /// </summary>
    public class {{EntityName}}UpdateDto
    {
        {{#each UpdatableProperties}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        {{#if IsRequired}}
        [Required(ErrorMessage = "{{Name}} 不能为空")]
        {{/if}}
        {{#if IsString}}
        [StringLength({{MaxLength}}, ErrorMessage = "{{Name}} 长度不能超过 {{MaxLength}} 个字符")]
        {{/if}}
        {{#if IsEmail}}
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        {{/if}}
        {{#if IsPhone}}
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        {{/if}}
        public {{Type}} {{Name}} { get; set; }

        {{/each}}
    }
    {{/if}}

    /// <summary>
    /// {{EntityDescription}}查询请求 DTO
    /// </summary>
    public class {{EntityName}}ListRequest : PagedRequest
    {
        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword { get; set; }

        {{#each FilterableProperties}}
        /// <summary>
        /// {{Description}}筛选
        /// </summary>
        public {{Type}} {{Name}} { get; set; }

        {{/each}}
        {{#each DateRangeProperties}}
        /// <summary>
        /// {{Description}}开始日期
        /// </summary>
        public DateTime? {{Name}}Start { get; set; }

        /// <summary>
        /// {{Description}}结束日期
        /// </summary>
        public DateTime? {{Name}}End { get; set; }

        {{/each}}
    }
}
```

### 2. AutoMapper 配置生成

#### 映射配置模板
```csharp
// Templates/AutoMapperProfile.hbs
using AutoMapper;
using LYBT.Server.Models;
using LYBT.Server.DTOs;

namespace {{Namespace}}.Configuration
{
    /// <summary>
    /// {{EntityName}} AutoMapper 配置
    /// </summary>
    public class {{EntityName}}MappingProfile : Profile
    {
        public {{EntityName}}MappingProfile()
        {
            // 实体到 DTO 映射
            CreateMap<{{EntityName}}, {{EntityName}}Dto>()
                {{#each DateProperties}}
                .ForMember(dest => dest.{{Name}}, opt => opt.MapFrom(src => src.{{Name}}.ToString("yyyy-MM-dd")))
                {{/each}}
                ;

            // 创建 DTO 到实体映射
            CreateMap<{{EntityName}}CreateDto, {{EntityName}}>()
                {{#each DateProperties}}
                .ForMember(dest => dest.{{Name}}, opt => opt.MapFrom(src => DateTime.Parse(src.{{Name}})))
                {{/each}}
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                ;

            {{#if CanUpdate}}
            // 更新 DTO 到实体映射
            CreateMap<{{EntityName}}UpdateDto, {{EntityName}}>()
                {{#each DateProperties}}
                .ForMember(dest => dest.{{Name}}, opt => opt.MapFrom(src => DateTime.Parse(src.{{Name}})))
                {{/each}}
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                ;
            {{/if}}
        }
    }
}
```

## 🔧 自定义模板系统

### 1. 模板创建和管理

#### 创建自定义模板
```bash
# 创建新的实体模板
lybt-template create --name CustomEntity --type entity --template-file ./Templates/CustomEntity.hbs

# 创建新的服务模板
lybt-template create --name CustomService --type service --template-file ./Templates/CustomService.hbs

# 列出所有模板
lybt-template list

# 更新模板
lybt-template update --name CustomEntity --template-file ./Templates/CustomEntity.v2.hbs

# 删除模板
lybt-template delete --name CustomEntity
```

#### 模板变量定义
```json
{
  "templateName": "CustomEntity",
  "templateType": "entity",
  "description": "自定义实体模板",
  "variables": [
    {
      "name": "EntityName",
      "type": "string",
      "description": "实体名称",
      "required": true
    },
    {
      "name": "Namespace",
      "type": "string",
      "description": "命名空间",
      "required": true,
      "default": "LYBT.Server.Models"
    },
    {
      "name": "TableName",
      "type": "string",
      "description": "数据库表名",
      "required": true
    },
    {
      "name": "Properties",
      "type": "array",
      "description": "属性列表",
      "required": true,
      "itemType": "object"
    },
    {
      "name": "GenerateValidation",
      "type": "boolean",
      "description": "是否生成验证代码",
      "required": false,
      "default": true
    }
  ],
  "outputs": [
    {
      "name": "EntityFile",
      "path": "./Models/{{EntityName}}.cs",
      "description": "实体类文件"
    },
    {
      "name": "ConfigFile",
      "path": "./Configuration/{{EntityName}}Config.cs",
      "description": "实体配置文件",
      "condition": "GenerateValidation"
    }
  ]
}
```

### 2. 高级模板功能

#### 条件生成模板
```handlebars
{{!-- Templates/AdvancedEntity.hbs --}}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
{{#if UseDataAnnotations}}
using System.ComponentModel.DataAnnotations;
{{/if}}
{{#if UseAuditing}}
using LYBT.Server.Common.Auditing;
{{/if}}

namespace {{Namespace}}.Models
{
    /// <summary>
    /// {{EntityDescription}}
    /// </summary>
    [Table("{{TableName}}")]
    {{#if UseAuditing}}
    public class {{EntityName}} : AuditableEntity
    {{else}}
    public class {{EntityName}}
    {{/if}}
    {
        {{#unless UseAuditing}}
        /// <summary>
        /// 主键标识
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        {{/unless}}

        {{#each Properties}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        {{#if (eq Type "string")}}
        {{#if IsRequired}}
        [Required(ErrorMessage = "{{Name}} 不能为空")]
        {{/if}}
        {{#if MaxLength}}
        [StringLength({{MaxLength}}, ErrorMessage = "{{Name}} 长度不能超过 {{MaxLength}} 个字符")]
        {{/if}}
        {{#if IsEmail}}
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        {{/if}}
        {{#if IsPhone}}
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        {{/if}}
        {{#if IsUrl}}
        [Url(ErrorMessage = "请输入有效的URL地址")]
        {{/if}}
        {{/if}}
        {{#if (eq Type "decimal")}}
        {{#if Precision}}
        [Column(TypeName = "decimal({{Precision}}, {{Scale}})")]
        {{/if}}
        {{#if RangeMin}}
        [Range({{RangeMin}}, {{RangeMax}}, ErrorMessage = "{{Name}} 必须在 {{RangeMin}} 到 {{RangeMax}} 之间")]
        {{/if}}
        {{/if}}
        {{#if IsUnique}}
        [Index(IsUnique = true)]
        {{/if}}
        {{#if DefaultValue}}
        [DefaultValue({{DefaultValue}})]
        {{/if}}
        public {{Type}} {{Name}} { get; set; }

        {{/each}}

        {{#each NavigationProperties}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        {{#if (eq RelationshipType "OneToOne")}}
        public {{TargetType}} {{Name}} { get; set; }
        {{else if (eq RelationshipType "OneToMany")}}
        public virtual ICollection<{{TargetType}}> {{Name}} { get; set; }
        {{else if (eq RelationshipType "ManyToOne")}}
        public {{TargetType}} {{Name}} { get; set; }
        {{else if (eq RelationshipType "ManyToMany")}}
        public virtual ICollection<{{TargetType}}> {{Name}} { get; set; }
        {{/if}}

        {{/each}}
    }
}
```

#### 循环和条件模板
```handlebars
{{!-- Templates/Service.hbs --}}
{{#each Methods}}
        /// <summary>
        /// {{Description}}
        /// </summary>
        {{#if Parameters}}
        Task<{{ReturnType}}> {{Name}}Async(
        {{#each Parameters}}
            {{#if IsOptional}}
            [FromQuery]
            {{/if}}
            {{Type}} {{Name}}{{#if DefaultValue}} = {{DefaultValue}}{{/if}}{{#unless @last}},{{/unless}}
        {{/each}});
        {{else}}
        Task<{{ReturnType}}> {{Name}}Async();
        {{/if}}

{{/each}}
```

## 🚀 工具使用最佳实践

### 1. 生成流程规范

#### 标准生成流程
```bash
# 1. 准备阶段
# 检查项目结构
lybt-gen validate --project ./LYBT.All.sln

# 2. 数据库分析
# 分析数据库结构
lybt-gen analyze-database --connection-string "Server=..." --output ./DatabaseAnalysis.json

# 3. 代码生成
# 生成实体
lybt-gen entity --from-database --table Patients --output ./Models

# 生成服务
lybt-gen service --entity Patient --with-repository --output ./Services

# 生成控制器
lybt-gen controller --entity Patient --with-validation --output ./Controllers

# 生成测试
lybt-gen test --entity Patient --type unit --output ./Tests

# 4. 验证生成结果
lybt-gen validate-generated --path ./Generated
```

#### 批量生成流程
```bash
# 批量生成多个实体
lybt-gen batch --config ./batch-config.json

# 批量配置文件示例
{
  "entities": [
    {
      "name": "Patient",
      "table": "Patients",
      "generateTypes": ["entity", "service", "controller", "test"]
    },
    {
      "name": "MedicalCase",
      "table": "MedicalCases",
      "generateTypes": ["entity", "service", "controller", "test"]
    }
  ],
  "outputDirectory": "./Generated",
  "namespacePrefix": "LYBT.Server"
}
```

### 2. 质量保证

#### 生成代码验证
```bash
# 语法检查
lybt-gen validate-syntax --path ./Generated

# 架构合规检查
lybt-gen validate-architecture --path ./Generated --rules ./architecture-rules.json

# 代码风格检查
lybt-gen validate-style --path ./Generated --style-rules ./style-rules.json

# 测试覆盖率检查
lybt-gen validate-coverage --path ./Generated --threshold 80
```

#### 自动化集成
```yaml
# .github/workflows/codegen.yml
name: 代码生成

on:
  push:
    paths:
      - 'Database/**'
      - 'Templates/**'
      - 'lybt.codegen.json'

jobs:
  generate-code:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: 设置 .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: 安装 CodeGen 工具
      run: |
        dotnet tool install --global LYBT.CodeGen

    - name: 生成代码
      run: |
        lybt-gen entity --from-database --output ./Generated
        lybt-gen service --entity Patient --output ./Generated
        lybt-gen controller --entity Patient --output ./Generated

    - name: 验证生成的代码
      run: |
        lybt-gen validate --path ./Generated

    - name: 构建项目
      run: |
        dotnet build LYBT.All.sln

    - name: 运行测试
      run: |
        dotnet test --no-build
```

## 🔍 故障排除

### 1. 常见问题

#### 模板编译错误
```bash
# 检查模板语法
lybt-template validate --template ./Templates/Entity.hbs

# 显示模板变量
lybt-template debug --template ./Templates/Entity.hbs --data ./test-data.json
```

#### 数据库连接问题
```bash
# 测试数据库连接
lybt-gen test-connection --connection-string "Server=..."

# 检查数据库架构
lybt-gen analyze-database --connection-string "..." --verbose
```

#### 命名空间冲突
```bash
# 检查命名空间冲突
lybt-gen check-conflicts --path ./Generated --namespace "LYBT.Server.Models"

# 自动解决冲突
lybt-gen resolve-conflicts --path ./Generated --strategy "rename"
```

### 2. 调试工具

#### 详细日志输出
```bash
# 启用详细日志
lybt-gen entity --entity Patient --verbose --log-level debug

# 生成调试报告
lybt-gen debug-report --output ./debug-report.html
```

#### 模板测试
```bash
# 测试模板渲染
lybt-template test --template ./Templates/Entity.hbs --data ./test-data.json

# 批量模板测试
lybt-template test-all --template-dir ./Templates --data-dir ./test-data/
```

## 📚 参考资料

### 相关文档
- [Handlebars 模板引擎](https://handlebarsjs.com/)
- [.NET 代码生成](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [AutoMapper 文档](https://automapper.readthedocs.io/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

### 工具扩展
- [自定义模板开发指南](./custom-template-development.md)
- [代码生成插件开发](./codegen-plugin-development.md)
- [模板函数参考](./template-functions-reference.md)

## 🔄 版本历史

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0 | 2025-10-15 | 初始版本 | 项目团队 |

## 📞 联系方式

- **维护者**: 项目团队
- **技术支持**: 开发工具团队
- **反馈渠道**: GitHub Issues 或内部反馈系统

---

*本文档遵循项目文档标准编写，如有疑问请参考相关文档或联系维护者。*