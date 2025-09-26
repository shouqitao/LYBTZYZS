# #758 测试覆盖率提升 - 任务清单

## 任务概述
将系统测试覆盖率从当前45%提升至70%，建立完善的测试体系。

## 当前状态分析
- **单元测试覆盖率**：45%（目标75%）
- **集成测试覆盖率**：20%（目标50%）
- **E2E测试覆盖率**：10%（目标30%）
- **核心模块严重缺失测试**：SessionManager、ConsultationService、PrescriptionService

## 详细任务清单

### Phase 1: 测试基础设施（第1周）

#### 1.1 测试框架搭建（8小时）
- [ ] 配置测试项目模板
- [ ] 安装测试包：xUnit、FluentAssertions、Moq、Bogus
- [ ] 创建测试基类TestBase
- [ ] 配置InMemory数据库

#### 1.2 测试数据工厂（8小时）
- [ ] 创建TestDataFactory类
- [ ] 实现PatientFaker
- [ ] 实现UserFaker
- [ ] 实现PrescriptionFaker
- [ ] 实现ConsultationFaker

#### 1.3 Mock基础设施（8小时）
- [ ] 创建MockHelper类
- [ ] 实现ServiceMockBuilder
- [ ] 配置AutoMapper测试配置
- [ ] 创建测试用DbContext

### Phase 2: 核心服务测试（第2-3周）

#### 2.1 SessionManager测试（16小时）
- [ ] StartConsultation测试（5个用例）
- [ ] EndConsultation测试（5个用例）
- [ ] PatientSelection测试（3个用例）
- [ ] UserLogin/Logout测试（4个用例）
- [ ] 事件发布测试（3个用例）

#### 2.2 UserService测试（16小时）
- [ ] GetByIdAsync测试（3个用例）
- [ ] CreateAsync测试（5个用例）
- [ ] UpdateAsync测试（4个用例）
- [ ] DeleteAsync测试（3个用例）
- [ ] 业务规则验证测试（5个用例）

#### 2.3 PatientService测试（16小时）
- [ ] CRUD操作测试（8个用例）
- [ ] 搜索功能测试（4个用例）
- [ ] 分页查询测试（3个用例）
- [ ] 数据验证测试（5个用例）

### Phase 3: 业务流程测试（第4-5周）

#### 3.1 ConsultationService测试（20小时）
- [ ] 诊疗创建测试（5个用例）
- [ ] 诊疗状态转换测试（6个用例）
- [ ] 诊疗记录查询测试（4个用例）
- [ ] 诊疗完成流程测试（5个用例）
- [ ] 异常场景测试（5个用例）

#### 3.2 PrescriptionService测试（20小时）
- [ ] 处方创建测试（5个用例）
- [ ] 处方验证测试（6个用例）
- [ ] 药品计算测试（4个用例）
- [ ] 处方查询测试（3个用例）
- [ ] 处方修改测试（4个用例）
- [ ] 处方打印测试（3个用例）

#### 3.3 AuthService/JWT测试（16小时）
- [ ] 用户认证测试（5个用例）
- [ ] Token生成测试（4个用例）
- [ ] Token验证测试（5个用例）
- [ ] Token刷新测试（3个用例）
- [ ] 权限验证测试（3个用例）

### Phase 4: Repository层测试（第6周）

#### 4.1 UserRepository测试（12小时）
- [ ] 基础CRUD测试（6个用例）
- [ ] 查询优化测试（3个用例）
- [ ] 事务处理测试（3个用例）

#### 4.2 PatientRepository测试（12小时）
- [ ] 数据持久化测试（5个用例）
- [ ] 复杂查询测试（4个用例）
- [ ] 并发处理测试（3个用例）

### Phase 5: 集成测试（第7-8周）

#### 5.1 API集成测试（20小时）
- [ ] UserController集成测试（8个用例）
- [ ] PatientController集成测试（8个用例）
- [ ] ConsultationController集成测试（10个用例）
- [ ] PrescriptionController集成测试（10个用例）

#### 5.2 端到端测试（16小时）
- [ ] 用户登录流程测试
- [ ] 患者注册流程测试
- [ ] 完整诊疗流程测试
- [ ] 处方生成流程测试

### Phase 6: CI/CD集成（第9周）

#### 6.1 GitHub Actions配置（8小时）
- [ ] 创建test-coverage.yml
- [ ] 配置测试运行器
- [ ] 设置覆盖率报告
- [ ] 配置Codecov集成

#### 6.2 本地测试脚本（4小时）
- [ ] 创建run-tests.ps1
- [ ] 配置覆盖率生成
- [ ] 创建HTML报告
- [ ] 设置覆盖率门禁

#### 6.3 测试监控（4小时）
- [ ] 配置测试Dashboard
- [ ] 设置失败告警
- [ ] 创建覆盖率趋势图
- [ ] 配置团队通知

### Phase 7: 文档和培训（第10周）

#### 7.1 测试文档（8小时）
- [ ] 编写测试规范文档
- [ ] 创建测试最佳实践指南
- [ ] 编写Mock使用指南
- [ ] 创建测试数据管理文档

#### 7.2 团队培训（8小时）
- [ ] 准备培训材料
- [ ] 进行TDD培训
- [ ] 代码审查培训
- [ ] 测试工具使用培训

## 里程碑检查点

| 周次 | 目标覆盖率 | 关键交付物 |
|-----|-----------|-----------|
| 第2周 | 50% | 核心服务测试完成 |
| 第4周 | 55% | 业务流程测试完成 |
| 第6周 | 60% | Repository层测试完成 |
| 第8周 | 65% | 集成测试完成 |
| 第10周 | 70% | CI/CD集成，文档完成 |

## 测试指标监控

```yaml
metrics:
  - name: 单元测试覆盖率
    current: 45%
    target: 75%
    priority: P0
  - name: 测试执行时间
    current: 未知
    target: <5分钟
    priority: P1
  - name: 测试通过率
    current: 未知
    target: 100%
    priority: P0
  - name: 测试维护成本
    current: 高
    target: 中
    priority: P2
```

## 风险管理

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|-----|------|----------|
| 进度延期 | 中 | 高 | 增加资源投入 |
| 测试质量不高 | 低 | 高 | 加强代码审查 |
| 团队抵触 | 中 | 中 | 展示测试价值 |
| 维护成本高 | 中 | 中 | 使用测试模板 |

## 资源需求
- **人力**：3名开发人员×10周
- **工具**：Visual Studio、ReSharper、NCrunch
- **环境**：测试服务器、CI/CD管道

## 成功标准
- [ ] 测试覆盖率达到70%
- [ ] 核心模块100%覆盖
- [ ] CI/CD完全自动化
- [ ] 测试执行时间<5分钟
- [ ] 团队掌握TDD实践