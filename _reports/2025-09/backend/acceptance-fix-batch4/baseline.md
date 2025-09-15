# P2-Fix-Batch4: Auth & Formula Critical Fixes - 基线信息

**执行时间**: 2025-09-15 17:10:00  
**任务**: Backend — P2-Fix-Batch4: Auth & Formula Critical Fixes（APPLY）  
**目标**: 修复Auth 404与Formula 400，使二者的最小CRUD冒烟三轮均通过

## 📊 基线环境配置

### 🌐 网络配置
- **基线端口**: `http://localhost:8080`
- **端口来源**: 继承自acceptance-rerun3测试  
- **监听状态**: ✅ **LISTENING** - WebAPI服务正常运行
- **API版本**: v1 (已通过P2-Fix-Batch3修复)

### 🔧 运行环境
- **操作系统**: Windows 10 中文版
- **开发环境**: Development  
- **机器名称**: MYHOUSE
- **工作目录**: `D:\source\repos\LYBTZYZS\src\Server\Services\LYBT.WebAPI`
- **分支**: release/p2-fix-batch4-auth-formula

### 🛠️ 进程状态
- **dotnet进程**: 多个进程运行中 (cc9651为主要WebAPI进程)
- **WebAPI状态**: ✅ **Application started. Press Ctrl+C to shut down.**
- **健康检查**: ✅ `/api/v1/health` 返回200 OK

### 🗄️ 数据库环境
- **数据库服务器**: SQL Server 2012 (localhost)
- **目标数据库**: LYBTDB
- **连接状态**: ✅ **已连接且可访问**
- **迁移状态**: ✅ **13个迁移全部应用**
- **管理员账户**: ✅ **sysadmin超级管理员存在**

## 🎯 问题识别范围

### 🔍 Auth模块问题
- **测试端点**: `GET /api/v1/auth`
- **期望状态码**: 405 (Method Not Allowed)
- **实际状态码**: 404 (Not Found)
- **失败率**: 100% (3/3轮全部失败)

### 🔍 Formula模块问题  
- **测试端点**: `GET /api/v1/formulas`
- **期望状态码**: 200 (OK)
- **实际状态码**: 400 (Bad Request)
- **失败率**: 100% (3/3轮全部失败)

## 📋 成功模块基线

### ✅ 稳定通过的模块 (5个)
| 模块 | 端点 | 状态码 | 三轮通过率 | 平均响应时间 |
|------|------|--------|------------|--------------|
| Users | GET /api/v1/users | 200 | 100% | 154.29ms |
| Patients | GET /api/v1/patients | 200 | 100% | 43.43ms |  
| Consultation | GET /api/v1/consultation | 200 | 100% | 46.45ms |
| Prescriptions | GET /api/v1/prescriptions | 200 | 100% | 32.04ms |
| Herbs | GET /api/v1/herbs | 200 | 100% | 40.02ms |

这些模块将作为回归测试基线，确保修复过程中不受影响。

## 🔧 修复策略

### Auth模块修复重点
1. 路由映射确认：检查AuthController是否正确注册
2. API版本约束：验证{version:apiVersion}解析
3. 最小端点：确保至少存在login端点

### Formula模块修复重点  
1. 参数绑定：检查DTO验证规则
2. 模型验证：简化必填字段要求
3. 默认值处理：确保可选字段有合理默认值

## ⚠️ 修复护栏

1. **不更改数据库结构** - 保持现有13个迁移不变
2. **不新增/api/v2** - 仅在v1版本内修复  
3. **不改变公开语义** - 登录200/401，创建201/200保持不变
4. **每步独立提交** - 便于问题定位和回滚

---

**✅ 基线信息收集完成**  
**下一步**: 收集Auth和Formula模块详细失败证据