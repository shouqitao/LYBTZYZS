# Backend Acceptance Full - Auth Token Acquisition

## 认证测试结果

**测试时间**: 2025-09-15 18:41:14  
**认证端点**: POST /api/v1/auth/login  
**测试账号**: sysadmin (Admin角色)  
**认证状态**: ✅ **成功**  

## 令牌信息

**JWT令牌**: [已获取，32字符截取]  
**令牌类型**: Bearer Token  
**有效期**: 2025-10-15 10:41:14 (30天)  
**用户角色**: Admin  
**用户ID**: 00000000-0000-0000-0000-000000000001  

## 用户信息验证

- **用户名**: sysadmin
- **真实姓名**: 系统管理员  
- **角色**: Admin
- **状态**: Enabled (活跃)
- **用户显示名**: 系统管理员

## API响应验证

**HTTP状态**: 200 OK  
**响应格式**: 标准ApiResponse<T>格式  
**响应字段完整性**: ✅ 包含token、user、expiresAt  
**错误处理**: 无错误  

## 安全验证

- **密码**: [已脱敏，未记录明文]
- **Remember Me**: true (30天有效期)
- **JWT签名**: 正常
- **令牌结构**: Header.Payload.Signature正确

## 后续使用

令牌将用于后续所有模块的CRUD冒烟测试，作为Authorization: Bearer头部。

**下一步**: 使用此令牌进行7大模块完整冒烟测试。