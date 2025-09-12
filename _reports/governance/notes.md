# Pass 8 治理违规修复笔记

## 发现的违规项 (2025-01-31)

### 1. 控制器位置违规
**问题**: Base 控制器类位于 Infrastructure 层而非 WebAPI 项目
- `LYBT.Infrastructure.BaseApiController`
- `LYBT.Infrastructure.BaseControllerCore`
- `LYBT.Infrastructure.BaseSystemController`

**修复策略**: 这些是基础架构类，实际应该保留在Infrastructure中。需要调整架构测试规则排除基础类。

### 2. 用户字段命名违规
**问题**: 匿名类型中仍使用 UserName 而非 Username
- `<>f__AnonymousType*.UserName` 多个实例

**修复策略**: 需要在 LINQ 查询中将字段统一改为 Username

### 3. 层依赖违规
**问题**: WebAPI 控制器直接依赖 Infrastructure 层
- 所有控制器: AuthController, ConsultationController 等

**修复策略**: 这是误报，WebAPI 控制器继承Infrastructure基类是合理的，需要调整测试规则。

### 4. 禁止命名违规
**问题**: 包含禁止命名模式的类
- 含"Engine": BusinessException, HerbUsagePrecaution 等
- 含"Intelligence": HerbRecommendation, FormulaRecommendation 等

**修复策略**: 重命名或标记为例外项

### 5. 复杂事务模式违规
**问题**: Infrastructure 中存在复杂事务协调类
- `ITransactionCoordinator`
- `TransactionCoordinator`
- 相关迁移

**修复策略**: 评估是否为过度设计，考虑简化或移除

## 修复原则

1. **架构合理性优先**: 不破坏合理的架构设计
2. **渐进式修复**: 每项单独提交，可独立回滚
3. **兼容性保护**: API 契约保持向后兼容
4. **测试调整**: 某些违规可能需要调整测试规则而非修复代码

## 风险评估

- **低风险**: 命名统一、测试规则调整
- **中风险**: 复杂事务组件移除
- **高风险**: 架构层次重大调整 (暂不执行)

## 暂不修复项 (留待后续)

- 基础架构层的复杂组件 (如需要评估业务影响)
- 可能引起反射/序列化问题的重命名