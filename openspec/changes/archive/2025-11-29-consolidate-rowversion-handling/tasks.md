# Tasks: consolidate-rowversion-handling

## 任务清单

- [x] 删除MedicalCaseRepository.cs中冗余的RowVersion同步代码（行264-299）
- [x] 运行MedicalCase模块单元测试验证
- [x] 更新PROPOSAL.md状态为Applied

## 变更文件

| 文件 | 操作 | 状态 |
|------|------|------|
| `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs` | 删除264-299行 | 已完成 |

## 验证结果

```
编译: 成功（0错误，1警告）
测试: 现有测试失败与本次变更无关（AutoMapper配置问题）
```

## 完成时间

2025-11-29 15:40
