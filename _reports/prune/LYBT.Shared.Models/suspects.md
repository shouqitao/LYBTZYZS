# LYBT.Shared.Models 可疑代码分析报告

**项目**: src/Shared/LYBT.Shared.Models/  
**分析时间**: 2025-09-07  
**分析重点**: 可能间接使用的代码（反射/序列化/XAML）

## 🔍 Suspect 详细分析

### 1. DiagnosisCatalogDto
**文件**: `DTOs/Configuration/DiagnosisCatalogDto.cs`  
**状态**: 可疑 - 可能功能预留

#### 代码结构
```csharp
public class DiagnosisCatalogDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    // ... 其他属性
}
```

#### 风险因素
- **序列化依赖**: 可能被JSON/XML序列化框架使用
- **配置系统**: 可能被未来的诊断配置功能调用
- **数据库映射**: 可能存在对应的Entity映射关系
- **反射访问**: 配置系统可能通过反射动态访问

#### 间接使用检查
- ✅ **JSON属性**: 无JsonPropertyName标记
- ❓ **实体映射**: 需检查是否有对应Entity
- ❓ **配置引用**: 需检查appsettings.json等配置文件
- ❓ **模块扫描**: 可能被依赖注入容器扫描

### 2. TreatmentCatalogDto
**文件**: `DTOs/Configuration/TreatmentCatalogDto.cs`  
**状态**: 可疑 - 与DiagnosisCatalogDto类似

#### 代码结构
```csharp
public class TreatmentCatalogDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    // ... 治疗方案相关属性
}
```

#### 风险因素
- 同DiagnosisCatalogDto的风险模式
- 可能被治疗方案管理功能预留使用
- 中医治疗标准化可能依赖此类型

### 3. LogDto
**文件**: `DTOs/Logging/LogDto.cs`  
**状态**: 高度可疑 - 可能被日志系统使用

#### 代码结构
```csharp
public class LogDto
{
    public Guid Id { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
    public string Exception { get; set; }
    public DateTime Timestamp { get; set; }
    // ... 日志相关属性
}
```

#### 高风险因素
- **日志框架**: Serilog/NLog可能序列化此类型
- **审计系统**: 可能被安全审计功能使用
- **监控平台**: 可能被外部监控系统调用
- **API导出**: 可能提供日志查询API

#### 特殊保护建议
**建议保留**: 日志系统通常涉及运行时序列化，删除风险极高

## 📋 可疑代码处理策略

### 观察期标记方案

#### 第一优先级（建议添加Obsolete）
1. **DiagnosisCatalogDto** - 配置预留功能
2. **TreatmentCatalogDto** - 配置预留功能

```csharp
[Obsolete("Under review for removal - analysis period ends 2025-09-21", false)]
public class DiagnosisCatalogDto
```

#### 第二优先级（建议保留）
1. **LogDto** - 日志系统高风险，建议直接保留

### 监控方法

#### 运行时使用监控
```bash
# 搜索字符串引用
grep -r "DiagnosisCatalogDto" --include="*.cs" --include="*.json" --include="*.xml" src/
grep -r "TreatmentCatalogDto" --include="*.cs" --include="*.json" --include="*.xml" src/
grep -r "LogDto" --include="*.cs" --include="*.json" --include="*.xml" src/
```

#### 序列化检查
- 检查appsettings.json中的配置引用
- 检查Startup.cs中的类型注册
- 检查AutoMapper配置中的映射定义

## 🎯 风险评估

| 类型 | 删除风险 | 业务影响 | 技术影响 | 建议 |
|------|----------|----------|----------|------|
| DiagnosisCatalogDto | 中等 | 可能影响诊断配置 | 可能破坏未来功能 | 观察期 |
| TreatmentCatalogDto | 中等 | 可能影响治疗方案 | 可能破坏未来功能 | 观察期 |
| LogDto | 高 | 可能影响审计日志 | 可能破坏监控系统 | 直接保留 |

## ⚠️ 特别注意事项

### 共享模型库特殊性
1. **API契约稳定性**: 任何删除都可能破坏前后端通信
2. **版本兼容性**: 客户端可能依赖特定的DTO结构
3. **序列化依赖**: JSON序列化可能在运行时动态使用
4. **未来扩展性**: 预留的DTO类可能支持计划中的功能

### 建议的安全删除流程
1. 添加[Obsolete]标记
2. 监控14天使用情况
3. 检查运行时日志是否有相关错误
4. 确认所有测试通过
5. 逐一删除，而非批量删除

**重要提醒**: 共享模型库的任何变更都应当经过完整的回归测试。