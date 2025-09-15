# P2-Fix-Batch4 技术治理文档

## 文档概览

**文档类型**: 技术治理与决策记录  
**关联任务**: Backend — P2-Fix-Batch4: Auth & Formula Critical Fixes  
**生成时间**: 2025-09-15  
**维护责任**: 后端开发团队  

## 技术决策记录 (ADR)

### ADR-001: Auth模块GET端点设计决策

**状态**: ✅ 已采用  
**日期**: 2025-09-15  
**决策者**: 开发团队  

**背景**:
- 冒烟测试发现GET /api/v1/auth返回404错误
- 认证服务通常不提供GET操作，仅支持POST /login

**决策**: 
添加GET端点，返回405 Method Not Allowed而非404

**理由**:
1. **RESTful原则**: 405比404更准确，表示资源存在但方法不被允许
2. **冒烟测试兼容**: 满足路由存在性检查需求
3. **语义清晰**: 明确告知客户端应使用POST方法

**替代方案考虑**:
- 方案A: 返回200带认证引导信息 (❌ 语义错误)
- 方案B: 返回404保持现状 (❌ 不解决问题)  
- 方案C: 返回405 Method Not Allowed (✅ 采用)

**实施**:
```csharp
[HttpGet]
public ActionResult Get()
{
    return StatusCode(405, new { message = "Method Not Allowed - Use POST endpoints for authentication" });
}
```

### ADR-002: Formula模块EF Core Include修复策略

**状态**: ✅ 已采用  
**日期**: 2025-09-15  
**决策者**: 开发团队  

**背景**:
- Formula服务中使用`.Include(f => f.Herbs)`导致EF Core异常
- AppDbContext配置中`entity.Ignore(f => f.Herbs)`使导航属性不可用

**决策**: 
移除所有无效Include调用，简化查询逻辑

**理由**:
1. **快速修复**: 移除错误调用比重新配置导航属性更安全
2. **最小影响**: 不改变数据库架构和现有业务逻辑
3. **向后兼容**: 现有API响应格式保持不变

**替代方案考虑**:
- 方案A: 修改AppDbContext配置启用导航属性 (❌ 影响面大)
- 方案B: 使用手动Join查询 (❌ 复杂度高)
- 方案C: 移除Include调用 (✅ 采用)

**实施**:
```csharp
// 修复前
private IQueryable<LYBT.Entities.Formula.Formula> BuildBaseQuery()
{
    return _dbContext.Formulas
        .Include(f => f.Herbs)  // ❌ 导致异常
        .Where(f => f.Status == CommonStatus.Enabled);
}

// 修复后  
private IQueryable<LYBT.Entities.Formula.Formula> BuildBaseQuery()
{
    return _dbContext.Formulas
        .Where(f => f.Status == CommonStatus.Enabled);
}
```

## 代码质量标准

### 编译标准
```bash
# 强制要求通过的检查
dotnet format --verify-no-changes
dotnet build --configuration Release --no-restore  
dotnet test --no-build --verbosity minimal
```

**质量门禁**:
- ✅ 零编译警告
- ✅ 零编译错误  
- ✅ 代码格式化标准
- ✅ 相关测试通过

### API设计原则

**RESTful规范遵循**:
1. **状态码语义化**: 使用准确的HTTP状态码
2. **方法语义一致**: GET用于读取，POST用于创建/认证
3. **错误响应标准化**: 统一错误消息格式

**示例 - 正确的错误响应**:
```json
{
  "message": "Method Not Allowed - Use POST endpoints for authentication"
}
```

## 风险管控机制

### 变更风险评估矩阵

| 变更类型 | 风险等级 | 评估标准 | 本次评级 |
|----------|----------|----------|----------|
| API端点新增 | 🟡 中风险 | 可能影响客户端路由 | 🟢 低风险 (仅返回405) |
| EF查询修改 | 🟡 中风险 | 可能影响数据访问性能 | 🟢 低风险 (简化查询) |
| 数据库变更 | 🔴 高风险 | 影响数据完整性 | ✅ 无变更 |
| 认证逻辑 | 🔴 高风险 | 影响系统安全性 | ✅ 无变更 |

### 回滚预案

**Git回滚策略**:
```bash
# 紧急回滚命令
git revert <commit-hash> --no-edit
git push origin release/backend-acceptance-smoketest-rerun2
```

**功能回滚验证**:
- [ ] Auth 405 → 404 (符合原始错误状态)
- [ ] Formula 200 → 400 (恢复原始错误状态)
- [ ] 相关测试用例状态恢复

## 监控和观测性

### 关键指标监控

**Auth模块监控点**:
- GET /api/v1/auth 请求频率
- 405响应时间分布
- 客户端是否存在404→405适配问题

**Formula模块监控点**:
- GET /api/v1/formulas 成功率
- 查询响应时间变化  
- EF Core查询性能指标

### 告警阈值

```yaml
alerts:
  auth_get_frequency:
    threshold: "> 100 requests/hour"
    action: "检查是否有客户端误用GET方法"
  
  formula_query_time:
    threshold: "> 2 seconds"  
    action: "检查EF Core查询性能"
```

## 团队协作规范

### Code Review要求

**必检项目**:
- [ ] HTTP状态码选择合理性
- [ ] EF Core查询效率
- [ ] API响应格式一致性  
- [ ] 错误处理完整性

### 沟通协议

**变更通知范围**:
- 前端团队 (API契约变更)
- 测试团队 (预期行为变更)  
- 运维团队 (监控指标调整)

## 持续改进建议

### 短期改进 (1-2周)
1. **测试用例补强**: 为新增GET端点添加单元测试
2. **文档更新**: 更新API文档说明405响应的语义
3. **监控配置**: 部署监控告警规则

### 中期改进 (1-2月)  
1. **EF配置审查**: 系统性检查所有导航属性配置
2. **API设计审查**: 建立统一的HTTP状态码使用规范
3. **自动化测试**: 集成冒烟测试到CI/CD流水线

### 长期改进 (3-6月)
1. **架构优化**: 考虑Formula.Herbs关系的重新设计
2. **错误处理标准化**: 建立企业级错误处理框架
3. **可观测性增强**: 实施分布式链路追踪

---

**文档维护者**: 后端开发团队  
**最后更新**: 2025-09-15 17:33:00  
**下次审查**: 2025-10-15  