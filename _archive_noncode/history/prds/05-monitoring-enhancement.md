# 监控体系完善需求 (PRD-005)

## 📋 需求概述

| 字段 | 内容 |
|------|------|
| 需求编号 | PRD-005 |
| 需求名称 | 监控体系完善 - 业务级监控与运维可观测性 |
| 优先级 | P3 (一般) |
| 预估工期 | 20工作日 |
| 风险等级 | 🔴 → 🟢 (监控盲区消除) |
| 负责模块 | 全系统横切关注点 |

## 🎯 需求背景

根据架构分析报告，系统存在**监控盲区**这一高风险项：
- 只有基础的健康检查端点，缺乏业务级监控
- 没有用户行为追踪和业务指标监控
- 缺乏实时告警机制和故障排查工具
- 运维可观测性不足，问题发现和定位困难

**问题影响**:
- 无法及时发现业务异常和性能问题
- 故障排查依赖人工日志查看，效率低
- 缺乏数据驱动的系统优化依据
- 用户体验问题无法量化和追踪

## 🎯 需求目标

### 主要目标
1. **建立完整的业务监控指标体系**
2. **实现实时告警和异常检测机制**
3. **提供可视化监控面板和分析工具**
4. **建立日志聚合和分析体系**

### 成功指标
- ✅ 关键业务指标监控覆盖率 = 100%
- ✅ 系统异常发现时间 < 5分钟
- ✅ 故障定位时间减少 70% (当前~30分钟 → <10分钟)
- ✅ 监控数据可视化仪表板完整性 = 100%

## 📊 当前监控现状分析

### 现有监控能力

#### 基础健康检查
**当前实现**:
```csharp
// HealthController.cs - 现有基础监控
[HttpGet]
public async Task<IActionResult> CheckHealth()
{
    try
    {
        // 仅检查数据库连接
        await _context.Database.CanConnectAsync();
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { status = "unhealthy", error = ex.Message });
    }
}

[HttpGet("database")]
public async Task<IActionResult> CheckDatabase()
{
    // 简单数据库连接检查
    var canConnect = await _context.Database.CanConnectAsync();
    return Ok(new { database = canConnect ? "healthy" : "unhealthy" });
}
```

**问题分析**:
- 监控维度过于简单，只能检测系统是否运行
- 无法反映业务健康状态和用户体验
- 缺乏性能指标和趋势分析
- 没有告警机制和问题通知

#### 日志记录现状
**当前日志实现**:
```csharp
// 分散在各个服务中的简单日志记录
public async Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto dto)
{
    try
    {
        _logger.LogInformation($"开始创建患者: {dto.Name}");
        
        var patient = _mapper.Map<Patient>(dto);
        await _repository.CreateAsync(patient);
        
        _logger.LogInformation($"患者创建成功: {patient.Id}");
        return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(patient));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"患者创建失败: {dto.Name}");
        return ServiceResult<PatientDto>.Failure("创建失败");
    }
}
```

**问题分析**:
- 日志格式不统一，缺乏结构化信息
- 无法进行日志聚合和统计分析
- 缺乏关联ID追踪完整业务流程
- 重要业务事件缺乏专门记录

### 监控需求缺口分析

#### 业务监控缺口
1. **诊疗流程监控**: 无法监控诊疗流程完成率和异常情况
2. **用户行为监控**: 缺乏用户操作轨迹和使用模式分析
3. **业务指标监控**: 无法监控关键业务KPI(患者数量、诊断成功率等)
4. **性能体验监控**: 缺乏用户体验相关的性能监控

#### 技术监控缺口  
1. **API性能监控**: 缺乏API响应时间和吞吐量监控
2. **资源使用监控**: CPU、内存、磁盘等资源使用趋势
3. **依赖服务监控**: 数据库、缓存等依赖服务健康状态
4. **错误率监控**: 系统错误率和异常模式分析

## 🔧 解决方案设计

### 监控架构设计

#### 整体架构图
```
前端监控
├── 用户行为追踪 (Application Insights)
├── 性能监控 (Core Web Vitals)
├── 错误监控 (JavaScript Exception)
└── 业务事件监控 (Custom Events)
                    ↓
              API Gateway
                    ↓
后端监控
├── API性能监控 (Middleware)
├── 业务指标监控 (Custom Metrics)  
├── 系统资源监控 (Performance Counters)
└── 健康检查监控 (Health Check Extensions)
                    ↓
监控数据存储
├── 指标数据 (Prometheus/InfluxDB)
├── 日志数据 (Elasticsearch)
├── 追踪数据 (Jaeger/Zipkin)
└── 告警规则 (AlertManager)
                    ↓
可视化展示
├── 业务仪表板 (Grafana)
├── 系统监控面板 (Prometheus UI)
├── 日志查询界面 (Kibana)
└── 告警管理界面 (AlertManager UI)
```

### 业务监控指标体系

#### 1. 核心业务指标(KBI - Key Business Indicators)

```csharp
public class BusinessMetricsService : IBusinessMetricsService
{
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<BusinessMetricsService> _logger;

    // 患者管理指标
    public async Task TrackPatientCreated(PatientDto patient)
    {
        _metrics.Increment("patients.created.total");
        _metrics.Gauge("patients.total", await GetTotalPatientsAsync());
        
        // 按年龄段统计
        var ageGroup = GetAgeGroup(patient.Age);
        _metrics.Increment($"patients.created.by_age_group.{ageGroup}");
        
        _logger.LogInformation("业务指标记录: 新患者创建 {@Patient}", patient);
    }

    // 诊疗流程指标
    public async Task TrackConsultationStarted(Guid patientId, Guid doctorId)
    {
        _metrics.Increment("consultations.started.total");
        _metrics.Histogram("consultation.start_time", DateTime.UtcNow.Hour);
        
        await TrackDoctorWorkloadAsync(doctorId);
    }

    public async Task TrackConsultationCompleted(Guid consultationId, TimeSpan duration)
    {
        _metrics.Increment("consultations.completed.total");
        _metrics.Histogram("consultation.duration.minutes", duration.TotalMinutes);
        
        // 计算完成率
        var completionRate = await CalculateConsultationCompletionRateAsync();
        _metrics.Gauge("consultations.completion_rate", completionRate);
        
        _logger.LogInformation("诊疗完成: ID={ConsultationId}, 用时={Duration}", 
            consultationId, duration);
    }

    // 处方管理指标
    public async Task TrackPrescriptionCreated(PrescriptionDto prescription)
    {
        _metrics.Increment("prescriptions.created.total");
        _metrics.Histogram("prescription.herb_count", prescription.Herbs.Count);
        _metrics.Histogram("prescription.total_price", (double)prescription.TotalPrice);
        
        // 常用药材统计
        foreach (var herb in prescription.Herbs)
        {
            _metrics.Increment($"herbs.prescribed.{herb.HerbId}");
        }
    }

    // 系统使用指标
    public void TrackUserLogin(Guid userId, string role, bool isRememberMe)
    {
        _metrics.Increment("users.login.total");
        _metrics.Increment($"users.login.by_role.{role.ToLower()}");
        
        if (isRememberMe)
            _metrics.Increment("users.login.remember_me");
            
        _logger.LogInformation("用户登录: ID={UserId}, 角色={Role}", userId, role);
    }
}
```

#### 2. 技术性能指标(KPI - Key Performance Indicators)

```csharp
public class PerformanceMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<PerformanceMetricsMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // 添加请求ID到响应头
        context.Response.Headers.Add("X-Request-ID", requestId);
        
        try
        {
            await _next(context);
            stopwatch.Stop();
            
            // 记录成功请求指标
            var path = context.Request.Path.Value;
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            
            _metrics.Histogram($"http.request.duration.{method}", stopwatch.ElapsedMilliseconds);
            _metrics.Increment($"http.requests.total.{method}.{statusCode}");
            
            // 慢请求告警
            if (stopwatch.ElapsedMilliseconds > 2000)
            {
                _logger.LogWarning("慢请求检测: {Method} {Path} 用时 {Duration}ms [RequestId: {RequestId}]",
                    method, path, stopwatch.ElapsedMilliseconds, requestId);
                    
                _metrics.Increment("http.requests.slow");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 记录异常请求指标
            _metrics.Increment("http.requests.errors");
            _metrics.Increment($"http.requests.errors.{ex.GetType().Name}");
            
            _logger.LogError(ex, "请求异常: {Method} {Path} [RequestId: {RequestId}]",
                context.Request.Method, context.Request.Path.Value, requestId);
            
            throw;
        }
    }
}
```

#### 3. 健康检查增强

```csharp
public class EnhancedHealthCheckService : IHealthCheck
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IBusinessMetricsService _businessMetrics;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var isHealthy = true;
        var issues = new List<string>();

        // 1. 数据库健康检查
        var dbHealth = await CheckDatabaseHealthAsync();
        healthData["database"] = dbHealth;
        if (!dbHealth.IsHealthy)
        {
            isHealthy = false;
            issues.Add($"数据库异常: {dbHealth.Description}");
        }

        // 2. 缓存健康检查  
        var cacheHealth = CheckCacheHealth();
        healthData["cache"] = cacheHealth;
        if (!cacheHealth.IsHealthy)
        {
            isHealthy = false;
            issues.Add($"缓存异常: {cacheHealth.Description}");
        }

        // 3. 业务健康检查
        var businessHealth = await CheckBusinessHealthAsync();
        healthData["business"] = businessHealth;
        if (!businessHealth.IsHealthy)
        {
            isHealthy = false;
            issues.Add($"业务异常: {businessHealth.Description}");
        }

        // 4. 系统资源检查
        var resourceHealth = CheckSystemResources();
        healthData["resources"] = resourceHealth;
        if (!resourceHealth.IsHealthy)
        {
            issues.Add($"资源告警: {resourceHealth.Description}");
            // 资源告警不影响整体健康状态，只做记录
        }

        var status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
        var description = isHealthy ? "系统运行正常" : string.Join("; ", issues);

        return new HealthCheckResult(status, description, data: healthData);
    }

    private async Task<HealthInfo> CheckBusinessHealthAsync()
    {
        try
        {
            // 检查业务关键指标
            var last24hConsultations = await _context.Consultations
                .Where(c => c.CreateTime >= DateTime.UtcNow.AddDays(-1))
                .CountAsync();

            var last24hErrors = await _context.SystemLogs
                .Where(l => l.Level == "Error" && l.Timestamp >= DateTime.UtcNow.AddDays(-1))
                .CountAsync();

            // 业务健康评估规则
            if (last24hErrors > 50) // 24小时内错误超过50个
            {
                return new HealthInfo(false, $"24小时内系统错误过多: {last24hErrors}个");
            }

            if (DateTime.UtcNow.Hour >= 8 && DateTime.UtcNow.Hour <= 18 && last24hConsultations == 0)
            {
                return new HealthInfo(false, "工作时间内无诊疗活动，可能存在业务异常");
            }

            return new HealthInfo(true, $"业务正常 - 24h诊疗: {last24hConsultations}次, 错误: {last24hErrors}个");
        }
        catch (Exception ex)
        {
            return new HealthInfo(false, $"业务健康检查异常: {ex.Message}");
        }
    }
}
```

### 告警机制设计

#### 1. 智能告警规则引擎

```csharp
public class AlertRuleEngine : IAlertRuleEngine
{
    private readonly List<IAlertRule> _rules;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AlertRuleEngine> _logger;

    public async Task EvaluateMetricAsync(MetricData metric)
    {
        foreach (var rule in _rules.Where(r => r.MatchesMetric(metric)))
        {
            var alertResult = await rule.EvaluateAsync(metric);
            if (alertResult.ShouldAlert)
            {
                await TriggerAlertAsync(alertResult);
            }
        }
    }

    private async Task TriggerAlertAsync(AlertResult result)
    {
        // 告警去重和抑制
        var alertKey = GenerateAlertKey(result);
        if (await ShouldSuppressAlertAsync(alertKey))
            return;

        // 发送告警通知
        await _notificationService.SendAlertAsync(new AlertNotification
        {
            Level = result.Level,
            Title = result.Title,
            Message = result.Message,
            Timestamp = DateTime.UtcNow,
            Source = result.Source,
            MetricValue = result.MetricValue,
            Threshold = result.Threshold
        });

        // 记录告警历史
        await RecordAlertHistoryAsync(result);
        
        _logger.LogWarning("触发告警: {Title} - {Message}", result.Title, result.Message);
    }
}

// 具体告警规则实现
public class ResponseTimeAlertRule : IAlertRule
{
    public bool MatchesMetric(MetricData metric) => metric.Name.Contains("response_time");

    public async Task<AlertResult> EvaluateAsync(MetricData metric)
    {
        var averageResponseTime = metric.Value;
        
        if (averageResponseTime > 5000) // 5秒
        {
            return new AlertResult
            {
                ShouldAlert = true,
                Level = AlertLevel.Critical,
                Title = "API响应时间过长",
                Message = $"平均响应时间 {averageResponseTime}ms 超过阈值 5000ms",
                MetricValue = averageResponseTime,
                Threshold = 5000,
                Source = metric.Source
            };
        }
        
        if (averageResponseTime > 2000) // 2秒
        {
            return new AlertResult
            {
                ShouldAlert = true,
                Level = AlertLevel.Warning,
                Title = "API响应时间告警",
                Message = $"平均响应时间 {averageResponseTime}ms 超过阈值 2000ms",
                MetricValue = averageResponseTime,
                Threshold = 2000,
                Source = metric.Source
            };
        }

        return AlertResult.NoAlert();
    }
}
```

#### 2. 多渠道通知机制

```csharp
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly IWeChatService _wechatService;
    private readonly ISmsService _smsService;
    private readonly IConfiguration _configuration;

    public async Task SendAlertAsync(AlertNotification alert)
    {
        var channels = GetNotificationChannels(alert.Level);
        
        foreach (var channel in channels)
        {
            try
            {
                switch (channel)
                {
                    case NotificationChannel.Email:
                        await SendEmailAlertAsync(alert);
                        break;
                    case NotificationChannel.WeChat:
                        await SendWeChatAlertAsync(alert);
                        break;
                    case NotificationChannel.SMS:
                        await SendSmsAlertAsync(alert);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "告警通知发送失败: 渠道={Channel}", channel);
            }
        }
    }

    private List<NotificationChannel> GetNotificationChannels(AlertLevel level)
    {
        return level switch
        {
            AlertLevel.Critical => new List<NotificationChannel> 
            { 
                NotificationChannel.Email, 
                NotificationChannel.SMS, 
                NotificationChannel.WeChat 
            },
            AlertLevel.Warning => new List<NotificationChannel> 
            { 
                NotificationChannel.Email, 
                NotificationChannel.WeChat 
            },
            AlertLevel.Info => new List<NotificationChannel> 
            { 
                NotificationChannel.Email 
            },
            _ => new List<NotificationChannel>()
        };
    }
}
```

### 可视化监控面板

#### 1. 监控面板配置

```json
{
  "dashboards": [
    {
      "name": "业务监控面板",
      "panels": [
        {
          "title": "今日诊疗统计",
          "type": "stat",
          "metrics": [
            "consultations.completed.total",
            "consultations.started.total", 
            "consultations.completion_rate"
          ]
        },
        {
          "title": "诊疗流程用时分布",
          "type": "histogram",
          "metrics": ["consultation.duration.minutes"]
        },
        {
          "title": "患者注册趋势",
          "type": "graph",
          "metrics": ["patients.created.total"],
          "timeRange": "24h"
        }
      ]
    },
    {
      "name": "技术监控面板", 
      "panels": [
        {
          "title": "API响应时间",
          "type": "graph",
          "metrics": ["http.request.duration.*"]
        },
        {
          "title": "错误率统计",
          "type": "stat",
          "metrics": ["http.requests.errors", "http.requests.total"]
        },
        {
          "title": "系统资源使用",
          "type": "gauge",
          "metrics": ["system.cpu.usage", "system.memory.usage"]
        }
      ]
    }
  ]
}
```

## 📝 详细需求规格

### 功能需求

#### FR-001: 业务指标监控
- **诊疗流程监控**: 诊疗开始、完成、用时统计
- **患者管理监控**: 患者注册、信息更新、查询统计  
- **处方管理监控**: 处方创建、药材使用、价格统计
- **用户行为监控**: 登录、操作、使用时长统计

#### FR-002: 技术性能监控
- **API性能监控**: 响应时间、吞吐量、错误率
- **系统资源监控**: CPU、内存、磁盘、网络使用率
- **数据库监控**: 连接数、查询耗时、锁等待
- **缓存监控**: 命中率、内存使用、失效统计

#### FR-003: 智能告警系统
- **阈值告警**: 基于指标阈值的自动告警
- **异常检测**: 基于历史数据的异常模式检测
- **告警分级**: Critical/Warning/Info三级告警机制
- **告警去重**: 防止告警风暴，智能合并相似告警

#### FR-004: 可视化监控面板
- **业务仪表板**: 关键业务指标实时展示
- **技术监控面板**: 系统性能指标可视化
- **告警管理界面**: 告警历史、处理状态管理
- **趋势分析图表**: 长期趋势和对比分析

### 非功能需求

#### NFR-001: 性能要求
- **数据采集延迟**: < 30秒
- **告警响应时间**: < 5分钟
- **监控系统可用性**: > 99.5%
- **数据保留期**: 指标数据30天，日志数据7天

#### NFR-002: 扩展性要求
- **指标扩展**: 支持自定义业务指标
- **告警规则扩展**: 支持复杂告警规则配置
- **通知渠道扩展**: 支持新增通知渠道
- **面板定制**: 支持用户自定义监控面板

#### NFR-003: 安全要求
- **访问控制**: 监控面板基于角色的访问控制
- **数据加密**: 敏感监控数据传输加密
- **审计日志**: 监控系统操作审计
- **数据隐私**: 个人敏感信息脱敏处理

## 🔧 技术实现

### 开发任务分解

#### 任务1: 指标收集基础设施 (8天)
- [ ] 实现业务指标收集服务
- [ ] 创建性能监控中间件
- [ ] 实现健康检查增强
- [ ] 集成指标存储系统

**交付物**:
- `BusinessMetricsService.cs` - 业务指标收集
- `PerformanceMetricsMiddleware.cs` - 性能监控中间件
- `EnhancedHealthCheckService.cs` - 增强健康检查
- Prometheus/InfluxDB集成配置

#### 任务2: 告警系统实现 (6天)
- [ ] 实现告警规则引擎
- [ ] 创建多渠道通知服务
- [ ] 实现告警去重和抑制机制
- [ ] 创建告警管理界面

**交付物**:
- `AlertRuleEngine.cs` - 告警规则引擎
- `NotificationService.cs` - 多渠道通知
- `AlertSuppressionService.cs` - 告警抑制
- 告警管理Web界面

#### 任务3: 监控面板开发 (4天)
- [ ] 设计监控面板布局
- [ ] 实现业务监控仪表板
- [ ] 创建技术监控面板
- [ ] 集成Grafana监控面板

**交付物**:
- 业务监控仪表板
- 技术监控面板
- Grafana面板配置
- 监控面板API

#### 任务4: 日志聚合系统 (2天)
- [ ] 配置结构化日志记录
- [ ] 集成Elasticsearch日志存储
- [ ] 实现日志查询和分析接口
- [ ] 创建日志查看界面

**交付物**:
- 结构化日志配置
- ELK Stack集成
- 日志查询API
- 日志管理界面

### 监控数据流设计

```csharp
// 指标数据流处理管道
public class MetricsPipeline
{
    private readonly IMetricsBuffer _buffer;
    private readonly IMetricsProcessor _processor;
    private readonly IMetricsStorage _storage;
    private readonly IAlertRuleEngine _alertEngine;

    public async Task ProcessMetricAsync(RawMetric rawMetric)
    {
        // 1. 数据清洗和标准化
        var cleanedMetric = await _processor.CleanAsync(rawMetric);
        
        // 2. 聚合计算
        var aggregatedMetrics = await _processor.AggregateAsync(cleanedMetric);
        
        // 3. 存储指标数据
        await _storage.StoreAsync(aggregatedMetrics);
        
        // 4. 触发告警检查
        foreach (var metric in aggregatedMetrics)
        {
            await _alertEngine.EvaluateMetricAsync(metric);
        }
        
        // 5. 实时推送到监控面板
        await PushToRealtimeDashboardAsync(aggregatedMetrics);
    }
}
```

## 🧪 测试策略

### 监控功能测试

#### 指标收集测试
```csharp
[Test]
public async Task BusinessMetrics_WhenPatientCreated_ShouldRecordMetrics()
{
    // Arrange
    var patient = new PatientCreateDto { Name = "测试患者", Phone = "13812345678" };
    var metricsCollector = new Mock<IMetricsCollector>();
    
    // Act
    await _patientService.CreatePatientAsync(patient);
    
    // Assert
    metricsCollector.Verify(m => m.Increment("patients.created.total"), Times.Once);
    metricsCollector.Verify(m => m.Gauge("patients.total", It.IsAny<double>()), Times.Once);
}
```

#### 告警规则测试
```csharp
[Test]
public async Task AlertRule_WhenResponseTimeHigh_ShouldTriggerAlert()
{
    // Arrange
    var rule = new ResponseTimeAlertRule();
    var metric = new MetricData 
    { 
        Name = "api.response_time",
        Value = 6000, // 6秒
        Timestamp = DateTime.UtcNow
    };
    
    // Act
    var result = await rule.EvaluateAsync(metric);
    
    // Assert
    result.ShouldAlert.Should().BeTrue();
    result.Level.Should().Be(AlertLevel.Critical);
}
```

### 性能测试

#### 监控系统性能测试
- [ ] **高频指标采集**: 1000 TPS指标写入性能
- [ ] **告警响应时间**: 告警触发到通知发送的端到端时间
- [ ] **监控面板加载**: 复杂面板加载时间 < 3秒
- [ ] **资源使用影响**: 监控系统对主业务的性能影响 < 5%

## 📊 验收标准

### 功能验收标准
- [ ] **指标完整性**: 所有定义的业务和技术指标都能正常采集
- [ ] **告警及时性**: 关键告警在5分钟内触发并发送通知
- [ ] **面板可用性**: 监控面板能正确展示所有指标数据
- [ ] **历史数据**: 支持30天的历史数据查询和分析

### 可靠性验收标准
- [ ] **监控系统可用性**: > 99.5%
- [ ] **数据准确性**: 指标数据准确率 > 99%
- [ ] **告警准确性**: 误报率 < 5%，漏报率 < 1%
- [ ] **系统影响**: 对主业务性能影响 < 5%

### 易用性验收标准
- [ ] **面板易用性**: 非技术人员能快速理解监控面板
- [ ] **告警可理解**: 告警信息描述清晰，包含处理建议
- [ ] **查询便捷**: 日志和指标查询界面友好易用
- [ ] **配置简单**: 新增监控指标和告警规则配置简单

## 🚀 部署和运维

### 部署架构
```
监控系统部署架构:

应用服务器
├── LYBT应用 (集成监控SDK)
├── 指标收集Agent
└── 日志收集Agent
         ↓
监控服务器
├── Prometheus (指标存储)
├── Elasticsearch (日志存储)  
├── Grafana (可视化面板)
├── AlertManager (告警管理)
└── 自定义监控API
         ↓
通知服务
├── 邮件服务
├── 短信服务
└── 企业微信服务
```

### 部署策略
1. **Phase 1**: 监控基础设施部署(Prometheus, Grafana等)
2. **Phase 2**: 指标收集和基础监控部署
3. **Phase 3**: 告警系统和通知服务部署
4. **Phase 4**: 监控面板和管理界面部署

### 运维监控
- **监控系统自监控**: 监控系统自身的健康状态监控
- **数据质量监控**: 监控数据的完整性和准确性
- **性能影响监控**: 监控系统对主业务的性能影响
- **成本监控**: 监控系统的资源消耗和成本控制

---

## 📞 项目信息

**需求负责人**: Senior .NET Architecture Analyst  
**开发预估**: 20工作日  
**测试预估**: 3工作日  
**发布时间**: Phase 3 实施期  
**风险等级**: 🔴 → 🟢 (监控盲区全面消除，运维可观测性大幅提升)

**依赖项目**: 建议在PRD-003(性能优化)完成后实施，可更好地监控性能优化效果