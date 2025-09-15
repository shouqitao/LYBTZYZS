# JWT配置审计报告

## 问题诊断结果

### 🔴 关键问题：JWT Secret配置缺失

**发现**: `appsettings.json` 中的 `JwtOptions` 配置缺少关键的 `Secret` 字段

**当前配置状态**:
```json
"JwtOptions": {
  "Issuer": "LYBT.WebAPI",
  "Audience": "LYBT.Client", 
  "ExpireMinutes": 480,
  "RememberMeExpireMinutes": 43200,
  "ClockSkewSeconds": 300
  // ❌ 缺失: "Secret": "..."
}
```

**代码中的期望**:
```csharp
// UnifiedServiceRegistration.cs 第194-195行
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
               configuration["JwtOptions:Secret"];
```

### JWT认证流程分析

#### Token生成流程 (Auth模块 - 正常工作)
1. ✅ Auth模块自有JWT生成逻辑
2. ✅ 使用内部密钥生成Token
3. ✅ 生成的Token格式正确，包含所需Claims

#### Token验证流程 (其他模块 - 失败)
1. ❌ JWT中间件读取配置 `JwtOptions:Secret` → **NULL**
2. ❌ 环境变量 `JWT_SECRET` → **未设置**
3. ✅ 回退到开发环境默认密钥：`DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction`
4. ❌ **密钥不匹配** → Token验证失败 → 401 Unauthorized

### 密钥不匹配分析

**Token生成端** (Auth模块):
- 可能使用不同的密钥或算法
- 需要检查Auth模块的JWT生成逻辑

**Token验证端** (JWT中间件):
- 使用默认开发密钥
- 与生成端密钥不一致

### 配置完整性检查

#### ✅ 正确的配置项
- `Issuer`: "LYBT.WebAPI" ✅
- `Audience`: "LYBT.Client" ✅  
- `ExpireMinutes`: 480 ✅
- `ClockSkewSeconds`: 300 ✅

#### ❌ 缺失的配置项
- `Secret`: **完全缺失** ⚠️ 

#### 🔧 验证逻辑正确性
- `ValidateIssuer`: true ✅
- `ValidateAudience`: true ✅
- `ValidateLifetime`: true ✅
- `ValidateIssuerSigningKey`: true ✅

### 环境变量状态
- `JWT_SECRET`: 未设置 (需要检查)
- `ASPNETCORE_ENVIRONMENT`: Development (推测)

## 修复方案

### 方案1：配置文件修复 (推荐)
在 `appsettings.json` 中添加 `Secret` 字段：
```json
"JwtOptions": {
  "Issuer": "LYBT.WebAPI",
  "Audience": "LYBT.Client",
  "Secret": "开发环境密钥_需要与Auth模块统一",
  "ExpireMinutes": 480,
  "RememberMeExpireMinutes": 43200,
  "ClockSkewSeconds": 300
}
```

### 方案2：环境变量配置
设置环境变量：
```bash
set JWT_SECRET=统一的JWT密钥
```

### 关键修复步骤
1. **确定Auth模块使用的实际密钥**
2. **统一验证端密钥配置**
3. **确保两端密钥完全一致**

## 下一步行动
1. 检查Auth模块JWT生成代码，确定实际使用的密钥
2. 修复配置文件，确保密钥统一
3. 重新测试JWT认证流程