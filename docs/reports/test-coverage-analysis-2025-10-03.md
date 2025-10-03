# 单元测试覆盖率分析报告

**日期**: 2025-10-03
**分析范围**: Server端业务模块
**相关Issue**: #864 - Epic: 完善整个单元测试覆盖

---

## 执行摘要

当前Server端8个业务模块的测试文件覆盖率为 **14.77%**,远低于目标的80%。所有模块都需要补充测试。

### 关键发现

- ✅ 所有模块都有基础测试项目
- ❌ 所有模块测试覆盖率严重不足(<25%)
- ⚠️ 平均每个模块只有 1-3 个测试文件
- 📊 88个源文件 vs 13个测试文件

---

## 模块详细分析

| 模块 | 源文件数 | 测试文件数 | 覆盖率 | 状态 | 优先级 |
|------|----------|------------|--------|------|--------|
| **Patients** | 13 | 3 | 23.08% | ⚠️ 覆盖不足 | 🔴 高 |
| **Consultation** | 14 | 3 | 21.43% | ⚠️ 覆盖不足 | 🔴 高 |
| **Users** | 12 | 2 | 16.67% | ⚠️ 覆盖不足 | 🔴 高 |
| **Auth** | 8 | 1 | 12.5% | ⚠️ 覆盖不足 | 🔴 高 |
| **Prescriptions** | 9 | 1 | 11.11% | ⚠️ 覆盖不足 | 🟡 中 |
| **Formula** | 9 | 1 | 11.11% | ⚠️ 覆盖不足 | 🟡 中 |
| **MedicalCase** | 10 | 1 | 10% | ⚠️ 覆盖不足 | 🟡 中 |
| **Herbs** | 13 | 1 | 7.69% | ⚠️ 覆盖不足 | 🟡 中 |

**汇总**:
- 总源文件数: 88
- 总测试文件数: 13
- 平均覆盖率: **14.77%**

---

## 优先级建议

### 🔴 Phase 2 - 核心模块(最高优先级)

基于业务重要性和当前覆盖率,建议优先完善以下模块:

1. **Patients (患者)** - 23.08%
   - 现状: 3个测试文件,13个源文件
   - 需补充: ~10个测试文件
   - 关键测试点: CRUD操作、数据验证、AutoMapper映射

2. **Users (用户)** - 16.67%
   - 现状: 2个测试文件,12个源文件
   - 需补充: ~8个测试文件
   - 关键测试点: 用户认证、角色管理、权限验证

3. **Auth (认证)** - 12.5%
   - 现状: 1个测试文件,8个源文件
   - 需补充: ~6个测试文件
   - 关键测试点: 登录流程、Token生成、Session管理

4. **Consultation (就诊)** - 21.43%
   - 现状: 3个测试文件,14个源文件
   - 需补充: ~10个测试文件
   - 关键测试点: 就诊流程、状态管理、业务规则

### 🟡 Phase 3 - 业务模块(高优先级)

5. **Prescriptions (处方)** - 11.11%
6. **Formula (方剂)** - 11.11%
7. **MedicalCase (病历)** - 10%
8. **Herbs (药材)** - 7.69%

---

## 测试类型缺失分析

基于现有测试文件分析,以下类型的测试普遍缺失:

### 缺失的测试类型

1. **Service层测试** - 业务逻辑验证
   - 输入验证
   - 业务规则检查
   - 异常处理

2. **Repository层测试** - 数据访问验证
   - CRUD操作
   - 查询逻辑
   - 事务处理

3. **Validator测试** - 数据验证规则
   - 必填字段验证
   - 格式验证
   - 业务规则验证

4. **Mapper测试** - AutoMapper配置
   - Entity ↔ DTO 映射
   - 嵌套对象映射
   - 集合映射

5. **边界条件测试**
   - Null值处理
   - 空字符串处理
   - 数值边界

6. **异常场景测试**
   - 数据库连接失败
   - 并发冲突
   - 业务规则违反

---

## 实施路线图

### Phase 2.1 - Patients模块(预计1周)

**目标**: 覆盖率从23% → 80%

- [ ] `PatientService`完整测试 (CRUD + 验证)
- [ ] `PatientRepository`测试
- [ ] `PatientValidator`测试
- [ ] AutoMapper Profile测试
- [ ] 集成测试补充

### Phase 2.2 - Users模块(预计1周)

**目标**: 覆盖率从17% → 80%

- [ ] `UserService`完整测试
- [ ] `RoleService`测试
- [ ] 权限验证测试
- [ ] 用户Repository测试

### Phase 2.3 - Auth模块(预计1周)

**目标**: 覆盖率从12.5% → 80%

- [ ] 登录流程测试
- [ ] Token生成与验证测试
- [ ] Session管理测试
- [ ] 密码加密测试

### Phase 2.4 - Consultation模块(预计1周)

**目标**: 覆盖率从21% → 80%

- [ ] 就诊流程测试
- [ ] 状态机测试
- [ ] 业务规则验证

---

## 技术规范重申

### 测试命名

```csharp
public class PatientServiceTests
{
    [Fact]
    public void CreatePatient_WithValidData_ReturnsSuccess()
    [Fact]
    public void CreatePatient_WithNullName_ThrowsArgumentNullException()
    [Fact]
    public void GetPatient_WithNonExistentId_ReturnsNotFound()
}
```

### AAA 模式

```csharp
[Fact]
public void CreatePatient_WithValidData_ReturnsSuccess()
{
    // Arrange
    var dto = new PatientCreateDto { Name = "张三", Gender = Gender.Male };
    var service = CreateService();

    // Act
    var result = service.Create(dto);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    result.Data.Name.Should().Be("张三");
}
```

### Mock 使用

```csharp
private readonly Mock<IPatientRepository> _mockRepo;
private readonly Mock<IMapper> _mockMapper;

public PatientServiceTests()
{
    _mockRepo = new Mock<IPatientRepository>();
    _mockMapper = new Mock<IMapper>();
}
```

---

## 度量标准

### 覆盖率目标

- **行覆盖率**: ≥80%
- **分支覆盖率**: ≥70%
- **方法覆盖率**: 100% (所有public方法)

### 质量标准

- ✅ 每个public方法至少1个测试
- ✅ 每个业务规则至少1个测试
- ✅ 边界条件必须覆盖
- ✅ 异常路径必须覆盖
- ✅ 使用FluentAssertions断言
- ✅ 遵循AAA模式

---

## 下一步行动

1. **立即行动** (本周):
   - [ ] 将本报告添加到 Issue #864
   - [ ] 创建 Phase 2.1 子Issue (Patients模块)
   - [ ] 开始Patients模块测试补充

2. **短期目标** (2周内):
   - [ ] 完成Patients模块测试 (→80%)
   - [ ] 完成Users模块测试 (→80%)

3. **中期目标** (1个月内):
   - [ ] 完成所有Phase 2核心模块
   - [ ] 建立测试编写规范文档

---

## 附录

### 分析脚本

分析脚本位于: `scripts/analysis/analyze-test-coverage.ps1`

运行方式:
```powershell
pwsh -File scripts/analysis/analyze-test-coverage.ps1
```

### 相关文档

- Epic Issue: #864
- 测试规范: `docs/development/testing-guidelines.md` (待创建)
- 架构文档: `docs/architecture/`

---

**报告生成时间**: 2025-10-03
**生成工具**: 自动化分析脚本 + Claude Code
**下次更新**: Phase 2完成后
