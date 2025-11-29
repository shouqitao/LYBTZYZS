---
name: lybtzyzs-architecture-auditor
description: 为LYBTZYZS项目执行深度架构审计，主动发现架构问题、技术债务、设计缺陷和潜在风险。生成详细审计报告并提供改进建议。触发关键词：架构审计、审计架构、发现架构问题、architecture audit、analyze architecture、架构分析
---

# LYBTZYZS 架构审计器

## 核心能力

本Skill用于**主动发现**项目中的架构问题，与`lybtzyzs-arch-compliance`的区别:

| Skill | 目的 | 模式 |
|-------|------|------|
| **arch-compliance** | 验证代码是否符合规范 | 被动检查（Pass/Fail） |
| **architecture-auditor** | 发现潜在架构问题 | 主动审计（发现问题） |

---

## 审计维度

### 1. 依赖健康度审计

**检查项**:
- 循环依赖检测
- 高耦合模块识别（依赖数 > 5）
- 不稳定依赖（频繁变更的模块被多处依赖）
- 跨层依赖违规

**输出指标**:
```
依赖健康度评分: 85/100
- 循环依赖: 0个
- 高耦合模块: 2个（MedicalCaseService, PatientService）
- 跨层违规: 1个（ViewModel直接调用Repository）
```

### 2. 代码复杂度审计

**检查项**:
- 大型类检测（>500行）
- 大型方法检测（>50行）
- 高圈复杂度方法（>10）
- 深层嵌套（>4层）

**输出指标**:
```
代码复杂度评分: 78/100
- 大型类: 3个
- 大型方法: 8个
- 高复杂度方法: 2个
```

### 3. 分层架构审计

**检查项**:
- Controller是否包含业务逻辑
- Service是否包含数据访问代码
- Repository是否包含业务规则
- ViewModel是否直接访问API

**检测模式**:
```csharp
// ❌ Controller包含业务逻辑
public class MedicalCaseController
{
    public IActionResult Create(CreateDto dto)
    {
        // 业务验证不应在Controller
        if (dto.PatientId <= 0) return BadRequest();
        var age = DateTime.Now.Year - patient.BirthDate.Year; // 业务计算
    }
}

// ❌ Repository包含业务规则
public class PatientRepository
{
    public bool IsValidPatient(Patient p)
    {
        // 业务验证不应在Repository
        return p.Age >= 0 && p.Age <= 150;
    }
}
```

### 4. DDD合规审计

**检查项**:
- 聚合根边界是否清晰
- 值对象是否不可变
- 领域事件是否正确使用
- Repository是否按聚合根划分

**检测模式**:
```csharp
// ❌ 聚合根边界不清晰
public class ConsultationService
{
    // 直接操作子实体，绕过聚合根
    public void AddSymptom(int consultationId, Symptom symptom)
    {
        var consultation = _consultationRepo.GetById(consultationId);
        consultation.Symptoms.Add(symptom); // 应通过MedicalCase
    }
}
```

### 5. API设计审计

**检查项**:
- RESTful规范符合度
- 端点命名一致性
- HTTP方法使用正确性
- 版本控制策略

**检测模式**:
```
❌ 非RESTful端点: POST /api/v1/patients/getByName
✅ RESTful端点: GET /api/v1/patients?name=xxx

❌ 命名不一致: /api/v1/MedicalCases vs /api/v1/patients
✅ 命名一致: /api/v1/medical-cases, /api/v1/patients
```

### 6. 技术债务审计

**检查项**:
- TODO/FIXME/HACK注释
- 废弃API使用（Obsolete）
- 硬编码配置
- 魔法数字/字符串

**输出**:
```
技术债务统计:
- TODO注释: 15个
- FIXME注释: 3个
- HACK注释: 2个
- 废弃API使用: 5处
- 硬编码配置: 8处
```

### 7. 安全审计

**检查项**:
- SQL注入风险（原始SQL拼接）
- 敏感数据暴露（日志中输出密码）
- 认证授权缺失
- CORS配置不当

**检测模式**:
```csharp
// ❌ SQL注入风险
var sql = $"SELECT * FROM Patients WHERE Name = '{name}'";

// ❌ 敏感数据暴露
_logger.LogInformation($"User login: {username}, password: {password}");
```

### 8. 性能审计

**检查项**:
- N+1查询问题
- 缺少索引提示
- 大对象频繁创建
- 同步阻塞调用

**检测模式**:
```csharp
// ❌ N+1查询
foreach (var patient in patients)
{
    var cases = _context.MedicalCases.Where(c => c.PatientId == patient.Id).ToList();
}

// ✅ 使用Include避免N+1
var patients = _context.Patients.Include(p => p.MedicalCases).ToList();
```

---

## 执行流程

```
Step 1: 收集项目信息
  → 使用serena分析项目结构
  → 使用netcontext-server获取.NET项目信息
  ↓
Step 2: 依赖分析
  → 分析项目引用关系
  → 检测循环依赖
  → 识别高耦合模块
  ↓
Step 3: 代码扫描
  → 扫描大型类/方法
  → 检测分层违规
  → 识别代码异味
  ↓
Step 4: DDD合规检查
  → 验证聚合根边界
  → 检查Repository粒度
  ↓
Step 5: API审计
  → 验证RESTful规范
  → 检查命名一致性
  ↓
Step 6: 技术债务统计
  → 收集TODO/FIXME
  → 识别废弃API
  ↓
Step 7: 安全/性能扫描
  → 检测安全风险
  → 识别性能问题
  ↓
Step 8: 生成审计报告
  → 汇总所有发现
  → 计算健康度评分
  → 生成改进建议
```

---

## 输出格式

### 架构审计报告

```markdown
# 架构审计报告

**项目**: LYBTZYZS
**审计时间**: 2025-11-29 10:30:00
**审计范围**: 全项目 / 指定模块

---

## 📊 总体健康度评分

| 维度 | 评分 | 等级 |
|------|------|------|
| 依赖健康度 | 85/100 | 良好 |
| 代码复杂度 | 78/100 | 一般 |
| 分层架构 | 92/100 | 优秀 |
| DDD合规 | 88/100 | 良好 |
| API设计 | 95/100 | 优秀 |
| 技术债务 | 70/100 | 需改进 |
| 安全性 | 90/100 | 良好 |
| 性能 | 82/100 | 良好 |
| **综合评分** | **85/100** | **良好** |

---

## 🔴 严重问题（需立即修复）

### Issue #1: 循环依赖
- **位置**: LYBT.Module.Auth ↔ LYBT.Module.Users
- **影响**: 编译警告，维护困难
- **建议**: 提取共享接口到LYBT.Shared.Interfaces

### Issue #2: SQL注入风险
- **位置**: PatientRepository.cs:45
- **代码**: `$"SELECT * FROM Patients WHERE Name = '{name}'"`
- **建议**: 使用参数化查询

---

## 🟡 中等问题（建议修复）

### Issue #3: 大型类
- **位置**: MedicalCaseService.cs（650行）
- **影响**: 难以维护和测试
- **建议**: 拆分为ConsultationService, PrescriptionService

### Issue #4: N+1查询
- **位置**: PatientService.cs:GetAllWithCases()
- **影响**: 性能下降
- **建议**: 使用Include预加载

---

## 🟢 建议改进

### Suggestion #1: 增加单元测试
- **现状**: 测试覆盖率 45%
- **目标**: 提升至 80%
- **优先模块**: MedicalCaseService, AuthService

---

## 📈 技术债务统计

| 类型 | 数量 | 优先级 |
|------|------|--------|
| TODO | 15 | 低 |
| FIXME | 3 | 中 |
| HACK | 2 | 高 |
| Obsolete API | 5 | 中 |
| 硬编码配置 | 8 | 低 |

---

## 🎯 改进路线图

### Phase 1: 紧急修复（1周内）
1. 修复SQL注入风险
2. 解决循环依赖

### Phase 2: 重构优化（1个月内）
1. 拆分大型类
2. 修复N+1查询
3. 清理HACK注释

### Phase 3: 持续改进（长期）
1. 提升测试覆盖率
2. 清理技术债务
3. 优化API设计
```

---

## 使用示例

### 示例1: 全项目审计

```
用户: 执行架构审计

Skill: 
1. 分析项目结构
2. 扫描所有模块
3. 生成完整审计报告

输出: 架构审计报告（包含8个维度评分）
```

### 示例2: 指定模块审计

```
用户: 审计MedicalCase模块架构

Skill:
1. 聚焦MedicalCase相关文件
2. 深度分析该模块
3. 生成模块审计报告

输出: MedicalCase模块审计报告
```

### 示例3: 指定维度审计

```
用户: 检查项目中的技术债务

Skill:
1. 扫描TODO/FIXME/HACK
2. 识别废弃API
3. 统计硬编码

输出: 技术债务报告
```

---

## 工具协同

| 工具 | 用途 |
|------|------|
| **serena** | 分析代码结构、符号关系 |
| **netcontext-server** | 获取.NET项目信息 |
| **grep** | 搜索代码模式 |
| **sequential-thinking** | 深度分析复杂问题 |

---

## 配置选项

可通过参数调整审计行为:

```
--scope: all | server | client | shared | module:ModuleName
--dimensions: all | dependency | complexity | layering | ddd | api | debt | security | performance
--severity: all | critical | warning | info
--output: markdown | json | html
```

---

## 与其他Skill的协同

```
lybtzyzs-architecture-auditor（发现问题）
  ↓
lybtzyzs-arch-compliance（验证修复）
  ↓
lybtzyzs-code-review（代码审查）
  ↓
lybtzyzs-quality-reporter（质量报告）
```

---

## 限制条件

1. **静态分析**: 无法检测运行时问题
2. **启发式规则**: 某些检测可能存在误报
3. **性能影响**: 全项目审计可能需要较长时间
4. **人工确认**: 审计结果需要人工审查确认

---

**最后更新**: 2025-11-29
**版本**: v1.0
