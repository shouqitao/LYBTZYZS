# Proposal: consolidate-code-quality

## Why

基于Visual Studio Code Metrics分析报告，项目存在以下代码质量问题需要解决：

### 问题1: 高圈复杂度代码 (7处)

| CC | LOC | 位置 | 问题 |
|----|-----|------|------|
| 37 | 33 | `BaseApiController.GetOperator()` | 过多条件分支 |
| 30 | 116 | `PatientImportExecutor.ImportWorker_RunWorkerCompleted()` | 异步回调逻辑过于复杂 |
| 28 | 132 | `MedicalCaseRepository.UpdateAsync()` | 更新逻辑分支过多 |
| 25 | 86 | `ExcelHelper.ConvertValueToPropertyType()` | 类型转换switch过大 |
| 23 | 221 | `MedicalCaseCommandService.SaveAsync()` | 保存逻辑过于集中 |
| 22 | 41 | `ExcelHelper.SetCellValue()` | 类型处理分支过多 |
| 21 | 35 | `PatientImportDataMapper.CreatePatientDtoFromRow()` | 字段映射逻辑复杂 |

### 问题2: EF Core迁移膨胀

**现状分析**：
- 两个迁移目录（历史遗留问题）：
  - `Migrations/` - 32个迁移文件
  - `Data/Migrations/` - 5个迁移文件
- 共计35个迁移，~41,000行代码
- 每个迁移的`BuildTargetModel`平均1,169行
- 最大的迁移文件超过1,270行

**根因分析**：
1. **EF Core设计特性** - 每次迁移包含完整模型快照，随实体增长线性膨胀
2. **频繁Schema变更** - 开发期间35次迁移，平均每周2-3次
3. **目录混乱** - 两个迁移目录导致管理困难
4. **未执行迁移压缩** - 从未执行过`dotnet ef migrations squash`

**为什么Code First变复杂**：
- Code First本身是简洁的，复杂度来自迁移历史
- 22个实体文件仅1,780行，但迁移累积到41,000行
- 迁移文件比实体代码多23倍

## What Changes

### Phase 1: 高复杂度代码重构 (优先级：高)

#### 1.1 BaseApiController.GetOperator() 重构
- 提取权限检查为独立方法
- 使用策略模式替代条件分支
- 目标CC: < 15

#### 1.2 PatientImportExecutor 重构
- 拆分`ImportWorker_RunWorkerCompleted`为多个小方法
- 提取验证逻辑、转换逻辑、错误处理为独立方法
- 目标CC: < 15

#### 1.3 MedicalCaseRepository.UpdateAsync() 重构
- 提取部分更新逻辑为私有方法
- 简化条件判断
- 目标CC: < 15

#### 1.4 ExcelHelper 重构
- 使用类型转换器字典替代switch
- 策略模式处理不同类型
- 目标CC: < 10

#### 1.5 MedicalCaseCommandService.SaveAsync() 重构
- 拆分为CreateAsync和UpdateAsync
- 提取验证、转换、持久化为独立方法
- 目标CC: < 15

#### 1.6 PatientImportDataMapper 重构
- 使用AutoMapper或手动映射器
- 简化字段转换逻辑
- 目标CC: < 10

### Phase 2: EF迁移整合 (优先级：中)

#### 2.1 迁移目录统一
- 将`Data/Migrations/`中5个迁移合并到`Migrations/`
- 删除`Data/Migrations/`目录
- 更新DbContext迁移配置

#### 2.2 迁移压缩 (可选，需评估风险)
- 备份当前数据库
- 执行`dotnet ef migrations squash`压缩历史迁移
- 从35个迁移压缩为1个InitialCreate
- 预计代码行从41,000降到~1,500

**迁移压缩风险评估**：
- 需要所有环境数据库Schema一致
- 需要备份和回滚计划
- 建议在v1.0发布后执行

## Success Criteria

1. [ ] 所有7个高复杂度方法CC降到20以下
2. [ ] 迁移目录统一为一个
3. [ ] 代码度量报告无CC>20的业务代码（排除迁移/生成代码）
4. [ ] 所有现有测试通过
5. [ ] 编译无警告

## Risks and Mitigations

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 重构引入回归 | 中 | 高 | 每个方法重构后立即运行单元测试 |
| 迁移压缩失败 | 低 | 高 | 完整数据库备份，保留回滚脚本 |
| 性能下降 | 低 | 中 | 重构前后性能对比测试 |

## Timeline

- Phase 1: 3-4天 (可并行处理多个方法)
- Phase 2: 1天 (迁移目录统一)
- Phase 2 可选: 2天 (迁移压缩，建议v1.0后执行)

## References

- Visual Studio Code Metrics Report: `工作簿1.xlsx`
- EF Core Migrations Best Practices: https://learn.microsoft.com/ef/core/managing-schemas/migrations/
- Cyclomatic Complexity Guidelines: CC < 10 理想，CC < 20 可接受
