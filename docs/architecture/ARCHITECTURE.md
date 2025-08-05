# 系统架构文档

## 目录

1. [概述](#概述)
2. [系统架构](#系统架构)
3. [技术栈](#技术栈)
4. [架构原则](#架构原则)
5. [系统组件](#系统组件)
6. [数据架构](#数据架构)
7. [安全架构](#安全架构)
8. [性能架构](#性能架构)
9. [部署架构](#部署架构)
10. [集成架构](#集成架构)

## 概述

凌隐宝堂中医诊所诊疗系统（LYBTZYZS）是一个基于.NET 8的企业级中医诊所管理系统。系统采用前后端分离架构，后端使用ASP.NET Core Web API，前端使用WPF桌面应用程序。

### 核心特性

- **模块化设计**：15个独立业务模块，便于维护和扩展
- **统一数据访问**：所有模块共享单一数据上下文
- **整洁架构**：严格分离关注点，提高代码质量
- **现代技术栈**：使用最新的.NET 8技术
- **安全可靠**：JWT认证、角色授权、数据加密

## 系统架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                         前端层 (WPF Client)                      │
├─────────────────────────────────────────────────────────────────┤
│  Shell │ Authentication │ Doctor │ FrontDesk │ SystemManagement │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTP/HTTPS + JWT
┌────────────────────────┴────────────────────────────────────────┐
│                      API 网关层 (Web API)                        │
├─────────────────────────────────────────────────────────────────┤
│     Controllers │ Middleware │ Authentication │ Swagger         │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│                        业务模块层                                │
├─────────────────────────────────────────────────────────────────┤
│ Auth │ Users │ Patients │ Doctors │ Registration │ Diagnosis   │
│ Prescriptions │ Herbs │ FormulaTemplates │ Pharmacy │ Billing  │
│ Records │ Queueing │ TreatmentRoom │ Sync                      │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│                      基础设施层                                  │
├─────────────────────────────────────────────────────────────────┤
│   AppDbContext │ Repositories │ Services │ AutoMapper │ Cache  │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│                      数据持久层                                  │
├─────────────────────────────────────────────────────────────────┤
│              SQL Server Database (LYBTDB)                       │
└─────────────────────────────────────────────────────────────────┘
```

### 架构模式

#### 1. 整洁架构（Clean Architecture）

- **领域层**：核心业务逻辑和实体
- **应用层**：业务用例和服务接口
- **基础设施层**：数据访问、外部服务集成
- **表示层**：Web API控制器和WPF视图

#### 2. 模块化单体架构（Modular Monolith）

- 每个业务模块独立开发和维护
- 模块间通过定义良好的接口通信
- 共享基础设施和数据上下文
- 便于未来向微服务架构演进

## 技术栈

### 后端技术

- **.NET 8**：最新的跨平台开发框架
- **ASP.NET Core Web API**：RESTful API服务
- **Entity Framework Core 8.0.17**：ORM框架
- **AutoMapper**：对象映射
- **JWT Bearer**：身份认证
- **Swagger/Swashbuckle 9.0.1**：API文档
- **SQL Server**：关系型数据库

### 前端技术

- **WPF (.NET 8)**：Windows桌面应用程序框架
- **Prism.DryIoc 9.0.537**：MVVM框架和依赖注入
- **Refit**：类型安全的REST客户端
- **Material Design**：UI组件库

### 开发工具

- **Visual Studio 2022**：主要IDE
- **Git**：版本控制
- **PowerShell/Batch**：自动化脚本

## 架构原则

### 1. 单一职责原则（SRP）

- 每个模块负责单一业务领域
- 类和方法保持简单和专注
- 清晰的职责边界

### 2. 依赖倒置原则（DIP）

- 高层模块不依赖低层模块
- 通过接口抽象依赖
- 使用依赖注入容器管理依赖

### 3. 开闭原则（OCP）

- 对扩展开放，对修改封闭
- 通过接口和抽象类实现扩展性
- 插件式架构设计

### 4. DRY原则

- 避免重复代码
- 提取公共功能到基类或工具类
- 使用代码生成减少样板代码

## 系统组件

### 1. Web API层

```csharp
// 基础控制器
public abstract class BaseController : ControllerBase
{
    protected IActionResult ApiResponse<T>(T data, string message = "")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        });
    }
}
```

### 2. 业务服务层

```csharp
public interface IBaseService<TEntity, TDto>
{
    Task<ApiResponse<IEnumerable<TDto>>> GetAllAsync();
    Task<ApiResponse<TDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<TDto>> CreateAsync(TDto dto);
    Task<ApiResponse<TDto>> UpdateAsync(TDto dto);
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
```

### 3. 数据访问层

```csharp
public interface IBaseRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> GetAll();
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}
```

### 4. 统一数据上下文

```csharp
public class AppDbContext : IdentityDbContext<User, Role, Guid>
{
    // 所有业务实体的DbSet
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    // ... 其他实体
}
```

## 数据架构

### 数据库设计原则

1. **规范化**：至少满足第三范式
2. **索引优化**：对常用查询字段建立索引
3. **审计跟踪**：所有表包含创建时间、修改时间
4. **软删除**：使用IsDeleted标记而非物理删除
5. **数据完整性**：外键约束和检查约束

### 核心数据实体

- **用户相关**：User, Role, Permission
- **患者相关**：Patient, PatientRecord, PatientHistory
- **医生相关**：Doctor, DoctorSchedule, DoctorSpecialty
- **诊疗相关**：Registration, Diagnosis, Treatment, Prescription
- **药材相关**：Herb, HerbCategory, HerbStock
- **财务相关**：Bill, Payment, Invoice

## 安全架构

### 1. 身份认证

- JWT Bearer Token认证
- Token过期时间：8小时（可配置）
- Remember Me功能：30天有效期
- 刷新Token机制

### 2. 授权机制

- 基于角色的访问控制（RBAC）
- 权限细粒度控制
- API端点级别授权
- 数据级别权限过滤

### 3. 数据安全

- 敏感数据加密存储
- HTTPS传输加密
- SQL注入防护
- XSS攻击防护

### 4. 审计日志

- 操作日志记录
- 登录日志
- 数据变更历史
- 异常日志

## 性能架构

### 1. 缓存策略

- 内存缓存：热点数据
- 分布式缓存：共享数据
- 查询结果缓存
- 静态资源缓存

### 2. 数据库优化

- 查询优化
- 索引策略
- 分页查询
- 延迟加载

### 3. 异步处理

- 异步API端点
- 后台任务队列
- 并发控制
- 线程池管理

### 4. 性能监控

- API响应时间监控
- 数据库查询监控
- 资源使用监控
- 性能指标告警

## 部署架构

### 开发环境

```yaml
服务器: localhost
数据库: SQL Server (localhost)
API端口: https://localhost:7001
环境变量: ASPNETCORE_ENVIRONMENT=Development
```

### 生产环境

```yaml
API服务器: Windows Server 2019+
数据库服务器: SQL Server 2019+
负载均衡: 可选
缓存服务器: Redis (可选)
监控服务: Application Insights
```

### 部署流程

1. 代码编译和打包
2. 数据库迁移
3. 配置文件更新
4. 服务部署
5. 健康检查
6. 监控配置

## 集成架构

### 1. 第三方集成

- 短信服务：验证码发送
- 邮件服务：通知推送
- 支付接口：在线支付
- 打印服务：处方打印

### 2. 数据同步

- 主从数据库同步
- 跨系统数据交换
- 批量数据导入导出
- 实时数据推送

### 3. API集成模式

- RESTful API
- 消息队列（可选）
- WebSocket（实时通信）
- gRPC（高性能通信）

## 架构演进路线

### 第一阶段：单体应用（当前）

- 模块化单体架构
- 单一数据库
- 垂直扩展

### 第二阶段：服务化（6-12个月）

- 核心模块服务化
- API网关
- 服务发现

### 第三阶段：微服务（12-24个月）

- 完全微服务架构
- 分布式数据管理
- 容器化部署

## 最佳实践

1. **代码组织**
   - 遵循整洁架构原则
   - 保持模块独立性
   - 使用依赖注入

2. **API设计**
   - RESTful规范
   - 统一响应格式
   - 版本管理

3. **数据访问**
   - Repository模式
   - 工作单元模式
   - 异步操作

4. **错误处理**
   - 全局异常处理
   - 统一错误响应
   - 详细日志记录

5. **测试策略**
   - 单元测试
   - 集成测试
   - API测试
   - 性能测试

## 总结

凌隐宝堂中医诊所诊疗系统采用现代化的架构设计，既保证了系统的稳定性和可维护性，又为未来的扩展和演进预留了空间。通过模块化设计、整洁架构和统一的技术栈，系统能够满足中医诊所的复杂业务需求，并提供良好的用户体验。