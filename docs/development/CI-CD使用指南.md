# CI/CD 使用指南

## 概述

本项目已配置完整的 CI/CD 流程，使用 GitHub Actions 实现自动化构建、测试和发布。

## 工作流说明

### 1. 持续集成 (CI) - `.github/workflows/ci.yml`

**触发条件**：
- 推送到 `main`、`master`、`develop` 分支
- 针对这些分支的 Pull Request

**执行内容**：
- ✓ 还原 NuGet 包
- ✓ 构建解决方案
- ✓ 运行单元测试
- ✓ 生成代码覆盖率报告
- ✓ 代码质量检查
- ✓ 上传构建产物

### 2. 测试工作流 - `.github/workflows/test.yml`

**触发条件**：
- 手动触发
- 每日定时运行（UTC 0:00）

**特点**：
- 矩阵策略测试（.NET 8.0, 9.0）
- 包含集成测试（SQL Server）
- 详细的测试报告

### 3. 发布工作流 - `.github/workflows/release.yml`

**触发条件**：
- 创建版本标签（如 `v1.0.0`）

**执行内容**：
- 构建生产版本
- 打包 Web API
- 打包桌面客户端
- 自动创建 GitHub Release
- 上传发布资产

## 本地测试

### 使用本地 CI 脚本

```bash
# 完整测试
python scripts/run-ci-local.py

# 跳过测试
python scripts/run-ci-local.py --skip-tests

# 跳过格式检查
python scripts/run-ci-local.py --skip-format
```

### 单独构建后端

由于前端模块还有编译问题，可以单独构建和测试后端：

```bash
# 构建后端
dotnet build LYBT.Backend.sln --configuration Release

# 运行后端测试
dotnet test LYBT.Backend.sln
```

## 发布流程

### 1. 创建版本标签

```bash
# 创建并推送标签
git tag v1.0.0
git push origin v1.0.0
```

### 2. 自动发布

GitHub Actions 会自动：
1. 构建项目
2. 运行所有测试
3. 创建发布包
4. 上传到 GitHub Releases

### 3. 发布产物

- `LYBT-WebAPI-v1.0.0.zip` - Web API 部署包
- `LYBT-Desktop-v1.0.0.zip` - 桌面客户端安装包

## 依赖更新

Dependabot 已配置为自动检查更新：
- NuGet 包 - 每周检查
- GitHub Actions - 每周检查
- 自动创建 PR 进行更新

## 最佳实践

### 1. 分支策略

- `main/master` - 生产分支
- `develop` - 开发分支
- `feature/*` - 功能分支
- `hotfix/*` - 紧急修复分支

### 2. 提交规范

建议使用语义化提交信息：
- `feat:` 新功能
- `fix:` 错误修复
- `docs:` 文档更新
- `test:` 测试相关
- `refactor:` 代码重构
- `chore:` 构建/工具链更新

### 3. PR 流程

1. 创建功能分支
2. 提交代码变更
3. 推送并创建 PR
4. 等待 CI 检查通过
5. Code Review
6. 合并到目标分支

## 故障排除

### CI 失败常见原因

1. **编译错误**
   - 检查是否有未提交的文件
   - 确认所有依赖正确

2. **测试失败**
   - 本地运行失败的测试
   - 检查测试环境配置

3. **超时**
   - 优化长时间运行的测试
   - 考虑并行执行

### 临时解决方案

当前前端模块存在编译问题，可以：

1. 修改 CI 配置，暂时只构建后端：
   ```yaml
   - name: Build Backend
     run: dotnet build LYBT.Backend.sln --configuration Release
   ```

2. 或者在 workflow 中添加 continue-on-error：
   ```yaml
   - name: Build All
     run: dotnet build LYBT.All.sln --configuration Release
     continue-on-error: true
   ```

## 监控和通知

### 查看构建状态

1. 访问项目的 Actions 标签页
2. 查看各个工作流的运行历史
3. 点击具体运行查看详细日志

### 获取通知

- GitHub 默认会发送邮件通知
- 可以配置 Slack/Teams 集成
- 使用 GitHub Mobile App 获取推送通知

---

更新时间：2025-01-08  
作者：Claude Assistant