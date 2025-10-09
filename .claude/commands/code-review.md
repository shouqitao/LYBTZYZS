# 代码审查命令 (/code-review)

执行完整的代码审查，基于项目标准检查代码质量、安全性和最佳实践。

## 📋 审查清单

### 1️⃣ 代码质量
- [ ] 命名清晰（类、方法、变量）
- [ ] 单一职责原则
- [ ] 代码复杂度合理（圈复杂度<10）
- [ ] 无重复代码
- [ ] 注释适当（复杂逻辑有注释）

### 2️⃣ .NET最佳实践
- [ ] 异步方法正确使用async/await
- [ ] IDisposable正确实现using
- [ ] LINQ查询优化
- [ ] 异常处理恰当
- [ ] 资源管理正确

### 3️⃣ 架构合规
- [ ] 依赖方向正确
- [ ] 无黑名单技术（CQRS/Redis/微服务）
- [ ] 接口设计合理
- [ ] 依赖注入正确使用

### 4️⃣ 安全性
- [ ] 无SQL注入风险
- [ ] 无XSS漏洞
- [ ] 密码正确加密
- [ ] API认证授权
- [ ] 敏感信息不泄露

### 5️⃣ 性能
- [ ] 无N+1查询
- [ ] 分页正确实现
- [ ] 大集合使用yield
- [ ] 缓存合理使用

## 🎯 输出格式

```markdown
# 🤖 代码审查报告

**文件**: src/path/to/file.cs
**作者**: {author}
**审查时间**: {timestamp}

## ✅ 通过项（{count}）
- 命名规范：通过
- 异步使用：通过
...

## ⚠️ 建议改进（{count}）
### 1. {改进点}
- **位置**: Line {number}
- **当前代码**:
  \`\`\`csharp
  {code}
  \`\`\`
- **建议**:
  \`\`\`csharp
  {improved_code}
  \`\`\`
- **理由**: {reason}

## ❌ 必须修复（{count}）
### 1. {问题}
- **严重性**: P{0-3}
- **位置**: Line {number}
- **问题描述**: {description}
- **修复建议**: {fix}

## 📊 审查统计
- 总行数: {lines}
- 通过项: {pass}
- 建议项: {suggestions}
- 必修项: {must_fix}
- 综合评分: {score}/100
```

## ⚡ 使用方式
```
/code-review src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs
```
