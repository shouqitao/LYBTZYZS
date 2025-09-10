# LYBT.WebAPI.Tests - 项目事实表

## 1) 基本信息
- **项目名称**: LYBT.WebAPI.Tests
- **相对路径**: tests/Backend/LYBT.WebAPI.Tests
- **项目类型**: Test
- **目标框架**: net8.0
- **输出类型**: Library
- **可空引用**: enable
- **语言版本**: (unknown)

## 2) 依赖与引用

### 项目引用 (3个)
- ../../../src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj
- ../../../src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj
- ../../../src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj

### NuGet包引用 (6个)
- Microsoft.NET.Test.Sdk
- xunit
- xunit.runner.visualstudio
- coverlet.collector
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.EntityFrameworkCore.InMemory

## 3) 公共暴露面
- **WebAPI**: 不适用 (测试项目)
- **WPF**: 不适用 (测试项目)

## 4) 数据模型
- **DbContext**: 无
- **DbSet列表**: 无
- **主要实体**: 无
- **DTO类型**: 无
- **实体↔DTO匹配**: 无

## 5) 测试特征

### 测试框架
- **测试框架**: xUnit
- **测试基础设施**: Microsoft.AspNetCore.Mvc.Testing

### 典型测试组件
- **测试夹具**: WebApplicationFactory<T>
- **测试服务器**: TestServer  
- **启动方式**: Microsoft.AspNetCore.Mvc.Testing

### 集成测试支持
- **包含集成测试**: true
- **集成测试特征**: 检测到WebApplicationFactory和TestServer组件

## 6) 特殊标识
- **IsIntegrationTest**: true
- **IsCore**: false
- **备注**: WebAPI集成测试项目，检测到Microsoft.AspNetCore.Mvc.Testing和WebApplicationFactory，用于端到端API测试