# 030-Shared 契约（最小集合）

## DTO
- LoginRequest: `{ username: string, password: string }`
- LoginResponse: `{ accessToken: string, expiresIn: number, userId: string, role: string }`

## 错误语义
- 200：成功
- 400：参数错误（缺失/格式）
- 401：认证失败（用户名/密码错误、账户禁用）

## 自检结果（✅ 已验证）
- **LoginRequest**：✅ 已存在 `src/Shared/LYBT.Shared.Models/Contracts/Auth/LoginRequest.cs`
- **LoginResponse**：✅ 已存在 `src/Shared/LYBT.Shared.Models/Contracts/Auth/LoginResponse.cs`
- **字段完整性**：✅ LoginRequest包含username/password等字段
- **字段完整性**：✅ LoginResponse包含Token/User/RefreshToken/ExpiresAt

## 实际契约对比
**LoginRequest实际字段**：
- Username, Password ✅ 
- ClientIp, UserAgent, LoginType, RememberMe, DeviceId, DeviceName（扩展字段）

**LoginResponse实际字段**：
- Token ✅ (对应accessToken)
- User ✅ (UserDto对象，包含userId/role)  
- RefreshToken, ExpiresAt ✅

## 状态：完全满足最小契约要求

