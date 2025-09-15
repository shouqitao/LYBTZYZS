# JWT认证失败证据 (修复前)

## 问题概述
**类型**: P0级 - JWT认证中间件系统性故障  
**影响**: 5个核心业务模块全部返回401 Unauthorized  
**来源**: 2025-09-15 Backend Full Acceptance Smoke Test  

## 失败端点清单

### Users模块
**端点**: `GET /api/v1/users`  
**期望状态码**: 200  
**实际状态码**: 401  
**错误信息**: "JWT authentication failed - unauthorized"  
**请求头**: `Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`  
**响应时间**: 2ms  
**测试时间**: 2025-09-15T18:43:15Z  

### Patients模块  
**端点**: `GET /api/v1/patients`  
**期望状态码**: 200  
**实际状态码**: 401  
**错误信息**: "JWT authentication failed - unauthorized"  
**响应时间**: 1ms  
**测试时间**: 2025-09-15T18:43:25Z  

### Herbs模块
**端点**: `GET /api/v1/herbs`  
**期望状态码**: 200  
**实际状态码**: 401  
**错误信息**: "JWT authentication failed - unauthorized"  
**响应时间**: 1ms  
**测试时间**: 2025-09-15T18:43:30Z  

### Formula模块  
**端点**: `GET /api/v1/formulas`  
**期望状态码**: 200  
**实际状态码**: 401  
**错误信息**: "JWT authentication failed - unauthorized"  
**响应时间**: 17ms  
**测试时间**: 2025-09-15T18:43:20Z  

### Prescriptions模块
**端点**: `GET /api/v1/prescriptions`  
**期望状态码**: 200  
**实际状态码**: 401  
**错误信息**: "JWT authentication failed - unauthorized"  
**响应时间**: 1ms  
**测试时间**: 2025-09-15T18:43:40Z  

## JWT令牌详情

**获取成功**: ✅ Auth模块登录正常，JWT令牌生成成功  
**令牌格式**: Bearer Token正确  
**令牌内容**: 
- Issuer: LYBT.WebAPI
- Audience: LYBT.Client  
- Subject: 00000000-0000-0000-0000-000000000001 (sysadmin)
- 角色: Admin
- 过期时间: 2025-10-15T10:43:11Z

**推测问题**:
1. JWT中间件配置错误 - 验证参数不匹配
2. 中间件注册顺序问题 - UseAuthentication位置错误
3. 签名验证失败 - 密钥配置不一致
4. Bearer Token解析异常 - Authorization头部处理错误

## 业务影响
- **功能不可用率**: 71.4% (5/7模块)
- **用户体验**: 绝大部分业务功能无法访问
- **系统可用性**: CRITICAL级别，不适合生产部署
- **数据访问**: 除Auth外的所有CRUD操作均被拒绝

## 修复优先级
**P0级紧急修复** - 影响核心业务功能，必须立即处理