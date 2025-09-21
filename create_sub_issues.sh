#!/bin/bash

# 子任务#2
gh sub-issue create --parent 637 --title "修复JWT声明与解析一致性" --body "## 📋 任务描述
确保JWT令牌生成和解析的声明一致性，支持标准ClaimTypes。

## 🎯 技术要求
- JwtAuthenticationService.GenerateToken增加ClaimTypes声明
- 同时保留JwtRegisteredClaimNames向后兼容
- BaseControllerCore.GetOperator()兼容多源解析

## ✅ 验收标准
- [ ] 新令牌包含ClaimTypes.NameIdentifier/Name/Role
- [ ] 正确解析OperatorId/Name/Role
- [ ] 向后兼容旧令牌

依赖：#638
预估工时：2h"

# 子任务#3
gh sub-issue create --parent 637 --title "加固授权边界和策略" --body "## 📋 任务描述
实现全局授权策略，加固敏感端点访问控制。

## 🎯 技术要求
- 配置全局FallbackPolicy要求认证
- AuthController敏感端点加[Authorize(Roles=\"Admin\")]
- 登录/健康检查加[AllowAnonymous]

## ✅ 验收标准
- [ ] 未登录访问返回401
- [ ] 普通用户访问管理端点返回403
- [ ] 匿名端点正常访问

依赖：#639
预估工时：3h"

# 子任务#4
gh sub-issue create --parent 637 --title "修复密码验证参数顺序" --body "## 📋 任务描述
修复PasswordHelper.Verify参数顺序错误导致的验证失败问题。

## 🎯 技术要求
- 修正Verify(hash, password)参数顺序
- 更新所有调用点

## ✅ 验收标准
- [ ] 旧密码验证正确
- [ ] 单元测试通过

依赖：无
预估工时：1h"

# 子任务#5
gh sub-issue create --parent 637 --title "实现密码复杂度策略" --body "## 📋 任务描述
统一密码复杂度校验，支持环境差异化策略。

## 🎯 技术要求
- 集成SecurityOptions.PasswordPolicy
- 生产环境：最小12位，含大小写/数字/特殊字符
- 开发环境：可配置放宽

## ✅ 验收标准
- [ ] 弱密码被拒
- [ ] 策略可配置
- [ ] 错误提示友好

依赖：#641
预估工时：2h"

# 子任务#6
gh sub-issue create --parent 637 --title "统一限流配置绑定" --body "## 📋 任务描述
限流参数从配置文件读取，移除硬编码。

## 🎯 技术要求
- 从SecurityOptions.RateLimit读取配置
- 保留登录白名单审计

## ✅ 验收标准
- [ ] 配置修改后生效
- [ ] 限流正常工作

依赖：无
预估工时：2h"

# 子任务#7
gh sub-issue create --parent 637 --title "最小化健康检查信息" --body "## 📋 任务描述
生产环境限制详细健康检查访问。

## 🎯 技术要求
- /health/details需授权
- /health保持匿名

## ✅ 验收标准
- [ ] 生产环境details需认证
- [ ] 基础健康检查匿名可用

依赖：#640
预估工时：1h"

# 子任务#8
gh sub-issue create --parent 637 --title "收紧生产CSP策略" --body "## 📋 任务描述
生产环境CSP策略收紧，提升XSS防护。

## 🎯 技术要求
- 移除unsafe-inline/unsafe-eval
- 更新appsettings.Security.json

## ✅ 验收标准
- [ ] CSP头严格模式
- [ ] API正常工作

依赖：无
预估工时：1h"

# 子任务#9
gh sub-issue create --parent 637 --title "密钥轮换与清理" --body "## 📋 任务描述
清理泄露密钥，实施密钥轮换。

## 🎯 技术要求
- 删除.encryption-key文件
- 清理git历史
- 环境变量注入

## ✅ 验收标准
- [ ] 仓库无密钥文件
- [ ] 密钥从环境变量读取

依赖：无（并行）
预估工时：2h"

# 子任务#10
gh sub-issue create --parent 637 --title "安全测试与验证" --body "## 📋 任务描述
添加完整的安全测试覆盖。

## 🎯 技术要求
- 授权测试用例
- 日志脱敏验证
- 密码策略测试

## ✅ 验收标准
- [ ] 所有安全测试通过
- [ ] 覆盖率达标

依赖：#638-#646
预估工时：3h"
