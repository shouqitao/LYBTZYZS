# 架构审查命令 (/review-arch)

执行完整的架构合规性审查，适用于重大重构或架构变更的PR。

## 📋 执行流程

### 1️⃣ 读取架构标准文档
```
必读文档：
- docs/development/standards.md（技术标准）
- docs/architecture/server-module-design-standard.md（Server端标准）
- docs/architecture/client/unified-design-standard.md（Client端标准）
- docs/PROJECT-STATUS-2025-09-27.md（技术决策与黑名单）
```

### 2️⃣ 分析当前变更
使用以下工具分析代码变更：
- `git diff --name-only HEAD~1 HEAD` - 获取变更文件列表
- `mcp__serena__find_symbol` - 分析新增/修改的类和方法
- `mcp__serena__find_referencing_symbols` - 检查影响范围

### 3️⃣ 架构合规性检查清单

#### ✅ 黑名单技术检查
- [ ] 无Redis缓存引入
- [ ] 无消息队列引入
- [ ] 无微服务架构引入
- [ ] 无CQRS模式引入
- [ ] 无容器化/Docker引入
- [ ] 无GraphQL引入

#### ✅ 三层架构验证（Server端）
- [ ] Controller → Service → Repository 依赖方向正确
- [ ] 无Repository直接调用Controller
- [ ] 无跨层调用（Controller不能直接访问Repository）

#### ✅ 四层架构验证（Desktop端）
- [ ] Shell → Workstation → Module → Core 依赖方向正确
- [ ] 无反向依赖
- [ ] Module之间无直接依赖

#### ✅ 命名规范
- [ ] 类名使用PascalCase
- [ ] 私有字段使用_camelCase
- [ ] 异步方法以Async结尾
- [ ] 接口以I开头

#### ✅ 依赖注入规范
- [ ] 仅使用构造函数注入
- [ ] 无ServiceLocator或Container.Resolve
- [ ] 依赖接口而非具体实现

### 4️⃣ 运行架构测试
```bash
# Server端架构测试
dotnet test tests/Architecture/LYBT.ArchTests.csproj -c Release --filter "FullyQualifiedName~Server"

# Desktop端架构测试
dotnet test tests/Architecture/LYBT.ArchTests.csproj -c Release --filter "FullyQualifiedName~Desktop"
```

### 5️⃣ 生成审查报告
输出格式：
```markdown
# 🤖 架构审查报告

## ✅ 合规性检查
- 黑名单技术：通过/失败
- 三层架构：通过/失败
- 命名规范：通过/失败
- 依赖注入：通过/失败

## ⚠️ 发现的问题
[列出所有问题]

## 💡 修复建议
[针对每个问题的修复建议]

## 📊 架构测试结果
[测试通过率]
```

## 🎯 使用场景

- PR合并前的最终检查
- 重大架构重构后的验证
- 新模块开发完成后的合规性确认
- 定期架构健康度检查

## ⚡ 快速使用

在对话中输入：`/review-arch`

Claude Code将自动执行上述所有步骤并生成完整的架构审查报告。
