# WebAPI 架构改进计划

基于 findings.md 架构评估报告（评分 B+），按优先级实施 5 项改进。

## Phase 1: 统一使用 Mapperly，移除手写映射 ⏳
- 删除 MedicalCaseMapper 中手写的 MapToMedicalCaseDetailDto
- Controller 统一使用 Mapperly 的 ToDetailDto
- 确保所有映射场景覆盖

## Phase 2: 引入工作单元模式
- BaseRepository 拆分操作和提交
- 提供 AddWithoutSave/UpdateWithoutSave 变体
- Service 层控制事务边界

## Phase 3: 消除 IConfiguration 直接注入
- UserService/AuthService 替换为 IOptions<T>
- 提取强类型 Options 类

## Phase 4: 解耦 Patients → MedicalCase 模块引用
- Patients 不直接引用 MedicalCase
- 通过 ICrossModuleService 接口解耦

## Phase 5: 统一文件编码为 UTF-8
- .editorconfig 中强制 charset = utf-8
- 批量转换现有文件

## 状态: IN PROGRESS
