# 用户功能稳固 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 通过测试驱动修复，确保登录/注销、用户 CRUD + 角色管理、账户设置、权限边界四个领域全部稳固可用。

**Architecture:** 先运行现有 21 个测试类找出失败项 → 修复 → 补缺口 → 验证全通过。

**Tech Stack:** C# / .NET 8 / xUnit / ASP.NET Core Identity

---

### Task 1: 运行全部现有用户/认证测试，收集失败项

**Files:** Read-only — no modifications

- [ ] **Step 1: 运行 Server 端认证/用户测试**

```bash
dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~Auth|FullyQualifiedName~User|FullyQualifiedName~Login" --no-build
```

记录：通过数、失败数、失败详情。

- [ ] **Step 2: 运行 Desktop 端认证/用户测试**

```bash
dotnet test tests/LYBT.Tests.Desktop --filter "FullyQualifiedName~Auth|FullyQualifiedName~User|FullyQualifiedName~Login" --no-build
```

记录：通过数、失败数、失败详情。

- [ ] **Step 3: 汇总测试报告**

分类记录每个失败项：
- 编译失败（代码变更导致签名不匹配）
- 断言失败（功能 bug）
- 基础设施失败（数据库/连接问题）
- 过时测试（测试已删除的功能）

### Task 2: 修复编译失败的测试

- [ ] **Step 1: 构建测试项目**

```bash
dotnet build tests/LYBT.Tests.Server
dotnet build tests/LYBT.Tests.Desktop
```

- [ ] **Step 2: 逐个修复编译错误**

修复所有因 API 签名变更、删除类型、重命名等导致的编译错误。不改变测试意图，只适配当前 API。

- [ ] **Step 3: Commit**

```bash
git commit -m "fix(tests): repair compilation errors in user/auth tests"
```

### Task 3: 修复断言失败的测试

- [ ] **Step 1: 逐个分析失败断言**

对每个断言失败：
1. 读取测试代码理解预期行为
2. 读取实现代码找到偏差
3. 判断是测试错误还是实现 bug

- [ ] **Step 2: 修复 bug 或更新测试**

如果实现有 bug → 修复实现代码。
如果测试预期过时 → 更新测试断言。

- [ ] **Step 3: Commit**

```bash
git commit -m "fix(auth): repair assertion failures in user/auth tests"
```

### Task 4: 补充关键缺口测试

**识别现有测试未覆盖的场景：**

- [ ] **Step 1: 权限边界测试**

为每个角色验证：
- Doctor 不能访问 Admin 端点（Users CRUD、SystemSettings）
- Receptionist 不能访问 Doctor 端点（MedicalCase 创建/编辑）
- Admin 不能删除自己或 sysadmin
- 普通用户不能修改他人资料

- [ ] **Step 2: 登录流程边界测试**

- 密码错误 3 次后锁定（远程模式）
- 禁用用户尝试登录
- Token 过期后的行为
- 本地模式简化认证流程

- [ ] **Step 3: 密码管理测试**

- 管理员重置密码 → 用户用新密码登录
- 用户修改密码 → 旧密码失效
- 密码强度验证

- [ ] **Step 4: Commit**

```bash
git commit -m "test(auth): add missing coverage for permission boundaries and password management"
```

### Task 5: 最终验证

- [ ] **Step 1: 全量构建**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 2: 运行全部测试**

```bash
dotnet test LYBTZYZS.sln
```

- [ ] **Step 3: 确认通过率**

所有用户/认证相关测试必须通过。记录最终通过率。

- [ ] **Step 4: Commit + Push**

```bash
git commit -m "feat(auth): user management stabilized — all tests passing"
git push origin master
```
