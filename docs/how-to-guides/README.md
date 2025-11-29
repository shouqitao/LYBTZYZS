# How-to Guides (操作指南) - 解决问题

> **目标导向**: 面向有基础的用户，解决具体的实际问题
> **适合人群**: 有经验的开发者、运维人员、业务用户
> **使用方式**: 按需查找、问题驱动、实用高效

## 🎯 快速问题定位

### 🔥 高频问题 (热门指南)
最常遇到的问题和解决方案：

- **[系统无法启动](development/troubleshooting-startup.md)** - 启动问题和解决方法
- **[数据库连接失败](development/database-connection.md)** - 数据库配置和连接
- **[API接口调用失败](development/api-issues.md)** - API调用常见问题
- **[用户认证失败](development/auth-issues.md)** - 认证和权限问题
- **[性能优化技巧](development/performance-optimization.md)** - 系统性能提升

### 🛠️ 开发问题 (Development)
开发过程中遇到的技术问题：

#### 环境和配置
- **[开发环境配置](development/environment-setup.md)** - 完整的环境搭建指南
- **[项目构建失败](development/build-issues.md)** - 编译和构建问题
- **[依赖包冲突](development/dependency-conflicts.md)** - NuGet包依赖管理
- **[调试技巧](development/debugging-skills.md)** - 高效的调试方法

#### 功能开发
- **[新增模块开发](development/develop-new-module.md)** - 新业务模块开发
- **[API接口开发](developing-api-endpoints.md)** - REST API开发
- **[用户界面开发](developing-user-interface.md)** - WPF界面开发
- **[数据验证实现](data-validation.md)** - 业务规则和验证

#### 代码质量
- **[单元测试编写](writing-unit-tests.md)** - xUnit测试实践
- **[代码审查指南](code-review-guide.md)** - Code Review最佳实践
- **[重构技巧](refactoring-techniques.md)** - 代码重构方法
- **[性能分析](performance-analysis.md)** - 代码性能分析

#### 🔐 认证授权问题
- **[Auth模块问题排查](modules/auth/troubleshooting-auth-issues.md)** - 登录失败、权限错误、Token问题解决
  - 快速修复登录失败、Token无效、权限控制失效等高频问题
  - 提供诊断步骤、解决方案和预防措施

#### 👥 用户管理问题
- **[用户批量操作指南](modules/users/bulk-user-operations.md)** - 批量导入、更新、删除用户
  - 解决新员工入职、权限审查、科室调整等批量用户管理问题
  - 提供Excel导入模板、操作检查清单和性能优化方案

### 🚀 部署运维 (Deployment)
系统部署和运维相关：

#### 系统部署
- **[本地开发部署](deployment/local-deployment.md)** - 本地环境部署
- **[测试环境部署](deployment/test-environment.md)** - 测试环境配置
- **[生产环境部署](deployment/production-deployment.md)** - 生产环境上线
- **[容器化部署](deployment/docker-deployment.md)** - Docker容器部署

#### 监控和维护
- **[系统监控配置](deployment/monitoring-setup.md)** - 应用监控配置
- **[日志管理](deployment/log-management.md)** - 日志收集和分析
- **[备份恢复](deployment/backup-recovery.md)** - 数据备份策略
- **[更新升级](deployment/update-upgrade.md)** - 系统更新流程

### 🏥 业务流程 (Business Workflows)
中医诊所业务相关的操作指南：

#### 患者管理流程
- **[患者注册流程](business-workflows/patient-registration.md)** - 新患者注册
- **[患者信息更新](business-workflows/patient-update.md)** - 患者资料维护
- **[患者搜索查询](business-workflows/patient-search.md)** - 患者查找

#### 诊疗工作流程
- **[新病例创建](business-workflows/medical-case-creation.md)** - 病历建立
- **[四诊信息录入](business-workflows/four-diagnostics-input.md)** - 望闻问切记录
- **[诊断结果管理](business-workflows/diagnosis-management.md)** - 诊断信息维护

#### 处方管理流程
- **[处方开具指南](business-workflows/prescription-creation.md)** - 处方开具
- **[草药配伍操作](business-workflows/herb-combination.md)** - 草药配伍
- **[处方审核流程](business-workflows/prescription-review.md)** - 处方审核

### 🔧 故障排查 (Troubleshooting)
系统问题的诊断和解决：

#### 常见错误
- **[启动错误排查](troubleshooting/startup-errors.md)** - 应用启动问题
- **[数据库错误](troubleshooting/database-errors.md)** - 数据库相关问题
- **[EF Core并发问题](troubleshooting/efcore-concurrency-issues.md)** - RowVersion冲突、DbUpdateConcurrencyException
- **[WPF MVVM事件问题](troubleshooting/wpf-mvvm-event-issues.md)** - PropertyChanged副作用、数据显示异常
- **[网络连接问题](troubleshooting/network-issues.md)** - 网络连接错误
- **[内存性能问题](troubleshooting/memory-issues.md)** - 内存和性能问题

#### 调试技巧
- **[日志分析方法](troubleshooting/log-analysis.md)** - 日志解读技巧
- **[性能瓶颈定位](troubleshooting/performance-bottlenecks.md)** - 性能问题定位
- **[错误代码解析](troubleshooting/error-codes.md)** - 错误代码含义
- **[调试工具使用](troubleshooting/debugging-tools.md)** - 调试工具应用

## 🔍 问题查找方式

### 按问题类型查找
- **环境问题** → [开发环境](development/environment-setup.md)
- **代码问题** → [开发指南](development/)
- **部署问题** → [部署指南](deployment/)
- **业务问题** → [业务流程](business-workflows/)
- **系统问题** → [故障排查](troubleshooting/)

### 按错误现象查找
- **程序无法启动** → [启动错误排查](troubleshooting/startup-errors.md)
- **数据库连接失败** → [数据库连接](development/database-connection.md)
- **界面显示异常** → [界面调试](development/ui-debugging.md)
- **API调用失败** → [API问题](development/api-issues.md)
- **性能明显下降** → [性能优化](development/performance-optimization.md)

### 按用户角色查找
- **新手开发者** → [基础开发指南](development/)
- **有经验开发者** → [高级开发技巧](development/advanced-techniques.md)
- **运维人员** → [部署运维指南](deployment/)
- **业务用户** → [业务流程](business-workflows/)

## 📋 操作标准清单

### 开发前检查清单
- [ ] 开发环境已正确配置
- [ ] 代码仓库已克隆到本地
- [ ] 数据库连接已测试通过
- [ ] 必要的开发工具已安装
- [ ] 相关文档已阅读

### 部署前检查清单
- [ ] 代码已通过所有测试
- [ ] 配置文件已正确设置
- [ ] 数据库迁移已执行
- [ ] 备份策略已制定
- [ ] 监控告警已配置

### 问题排查检查清单
- [ ] 错误日志已收集
- [ ] 重现步骤已明确
- [ ] 环境信息已记录
- [ ] 相关配置已检查
- [ ] 可能原因已列举

## ⚡ 高效解决方案

### 快速修复 (5分钟内解决)
- **[常见配置错误修复](troubleshooting/quick-fixes.md)** - 一键修复配置
- **[环境变量问题](troubleshooting/environment-variables.md)** - 环境配置
- **[权限问题解决](troubleshooting/permission-issues.md)** - 权限配置

### 标准解决方案 (15-30分钟)
- **[完整的环境重建](development/environment-rebuild.md)** - 环境重置
- **[数据库重新初始化](development/database-reinit.md)** - 数据库重建
- **[依赖包重装](development/dependency-reinstall.md)** - 依赖管理

### 深度解决方案 (1小时以上)
- **[系统性能调优](development/performance-tuning.md)** - 性能全面优化
- **[架构问题分析](architecture-analysis.md)** - 架构层面问题
- **[数据迁移策略](data-migration-strategy.md)** - 复杂数据处理

## 🔗 相关资源

### 文档资源
- 📚 **[Tutorials](../tutorials/)** - 系统学习教程
- 📖 **[Reference](../reference/)** - 技术参考文档
- 🧠 **[Explanation](../explanation/)** - 深入理解文档

### 工具资源
- 🛠️ **[调试工具清单](tools/debugging-tools.md)** - 推荐调试工具
- 📊 **[性能监控工具](tools/monitoring-tools.md)** - 性能分析工具
- 🔧 **[开发工具配置](tools/development-tools.md)** - 开发环境工具

### 社区资源
- 💬 **[技术论坛](https://github.com/shouqitao/LYBTZYZS/discussions)** - 社区讨论
- 🐛 **[问题跟踪](https://github.com/shouqitao/LYBTZYZS/issues)** - Bug报告
- 📧 **[技术支持](mailto:support@example.com)** - 联系支持团队

## 📞 获取帮助

### 自助服务
- 🔍 **[搜索问题](https://github.com/shouqitao/LYBTZYZS/search)** - 搜索已有问题和解决方案
- 📋 **[FAQ常见问题](faq.md)** - 最常见问题解答
- 🎯 **[问题分类导航](problem-classification.md)** - 按类型快速定位

### 人工支持
- 🎫 **[创建Support Ticket](https://support.example.com)** - 提交技术支持请求
- 📱 **[技术交流群](https://t.me/lybtzyzs)** - 即时技术交流
- 📅 **[在线答疑时间](office-hours.md)** - 定期在线答疑

---

**文档类型**: How-to Guides Index
**更新时间**: 2025-11-29
**维护团队**: 架构组 + 技术支持团队
**质量保证**: 所有指南都经过实践验证