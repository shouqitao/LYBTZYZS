# Test Suite Rationalization Notes

## 分类依据 (Record-Only 基线原则)

### KEEP - Record-Only基线CRUD/历史查询
核心业务模块的基础增删改查和历史记录查询功能测试

### ARCHIVE - 复杂功能归档  
超出Record-Only范围的复杂功能，包括：
- 配伍检查、智能推荐、统计分析  
- 自动价格计算、复杂患者状态流转
- 会话/心跳/锁定机制
- 事务监控、性能分析等基础设施复杂度

### TRIM - 精简保留
属于Record-Only但测试过度复杂的场景，仅保留Happy-path + 常见异常

### DEFER - 延迟处理
疑似反射/序列化/XAML依赖，需要额外评估的测试

## 回滚记录

(记录任何因依赖问题而回滚的操作)
