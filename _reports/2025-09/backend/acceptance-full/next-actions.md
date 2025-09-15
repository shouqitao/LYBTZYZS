# 下一步修复行动指南

## 🚨 立即行动 (P0级) - JWT认证修复

### 问题诊断清单

**第1步：检查JWT配置**
```bash
# 检查配置文件
cat src/Server/Services/LYBT.WebAPI/appsettings.json | grep -A 10 "Jwt"
cat src/Server/Services/LYBT.WebAPI/appsettings.Development.json | grep -A 10 "Jwt"
```

**第2步：验证中间件注册顺序**
```bash
# 检查Program.cs中的中间件配置
grep -n "UseAuthentication\|UseAuthorization" src/Server/Services/LYBT.WebAPI/Program.cs
```

**第3步：检查JWT服务注册**
```bash
# 检查JWT服务配置
grep -n "AddJwtBearer\|AddAuthentication" src/Server/Services/LYBT.WebAPI/Program.cs
```

### 常见修复方案

#### 方案1：中间件顺序问题
```csharp
// 正确的顺序 (Program.cs)
app.UseAuthentication();  // 必须在前
app.UseAuthorization();   // 必须在后
```

#### 方案2：JWT配置参数错误
```csharp
// 检查JWT配置 (appsettings.json)
{
  "JwtSettings": {
    "SecretKey": "确保密钥长度足够",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client", 
    "ExpiryMinutes": 480
  }
}
```

#### 方案3：Bearer Token格式问题
```bash
# 测试Bearer Token格式
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" http://localhost:8080/api/v1/users
```

### 验证步骤
```bash
# 重启服务
cd src/Server/Services/LYBT.WebAPI
dotnet run --launch-profile http

# 重新获取Token
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"Admin@123456","rememberMe":false}'

# 测试认证API
JWT_TOKEN="YOUR_NEW_TOKEN"
curl -H "Authorization: Bearer $JWT_TOKEN" http://localhost:8080/api/v1/users
```

## 🔧 次要修复 (P1级) - 路由问题

### Consultation模块路由修复

**诊断方法**:
```bash
# 检查控制器是否存在
ls src/Server/Modules/*/Controllers/*Consultation*Controller.cs

# 检查路由配置
grep -n "Route\|ApiController" src/Server/Modules/*/Controllers/*Consultation*Controller.cs
```

**可能解决方案**:
1. 检查ConsultationController路由注解
2. 确认模块服务注册: `AddConsultationModule()`
3. 验证依赖注入配置

### MedicalCase模块路由修复

**诊断方法**:
```bash
# 检查控制器是否存在  
ls src/Server/Modules/*/Controllers/*MedicalCase*Controller.cs

# 检查服务注册
grep -n "MedicalCase" src/Server/Services/LYBT.WebAPI/Program.cs
```

## 🧪 修复验证流程

### 自动化验证脚本

创建快速验证脚本：
```bash
#!/bin/bash
# quick-test.sh

echo "=== 快速验证修复效果 ==="

# 1. 健康检查
echo "1. 健康检查..."
curl -s http://localhost:8080/health

# 2. 获取新Token
echo "2. 获取认证令牌..."
TOKEN=$(curl -s -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"Admin@123456","rememberMe":false}' \
  | jq -r '.data.token')

# 3. 测试所有模块
echo "3. 测试核心模块..."
modules=("users" "patients" "herbs" "formulas" "prescriptions" "consultations" "medicalcases")

for module in "${modules[@]}"; do
  status=$(curl -s -w "%{http_code}" -H "Authorization: Bearer $TOKEN" \
    http://localhost:8080/api/v1/$module -o /dev/null)
  echo "   $module: HTTP $status"
done

echo "=== 验证完成 ==="
```

### 成功标准

**P0修复成功标准**:
- ✅ Users模块: 200状态码
- ✅ Patients模块: 200状态码  
- ✅ Herbs模块: 200状态码
- ✅ Formula模块: 200状态码
- ✅ Prescriptions模块: 200状态码

**P1修复成功标准**:
- ✅ Consultation模块: 200状态码
- ✅ MedicalCase模块: 200状态码

**整体成功标准**:
- ✅ 7个模块全部返回200
- ✅ 整体通过率达到100%
- ✅ 平均响应时间<50ms

## 📞 紧急联系与升级

### 如果修复遇到困难

**第一层支持**:
1. 检查系统日志: `dotnet run` 控制台输出
2. 检查IIS应用程序日志 (如使用IIS)
3. 查看Windows事件查看器

**第二层支持**:
1. 回滚到已知良好版本
2. 检查数据库连接状态
3. 验证依赖服务运行状态

**升级条件**:
- P0修复超过4小时无进展
- 出现新的系统性问题
- 数据完整性受到影响

## 🎯 预期时间线

| 阶段 | 预估时间 | 关键里程碑 |
|------|----------|-----------|
| P0诊断 | 30分钟 | 找到JWT问题根因 |
| P0修复 | 2-3小时 | 5个模块恢复200 |
| P1修复 | 1-2小时 | 7个模块全部正常 |
| 验证测试 | 30分钟 | 完整冒烟测试通过 |
| **总计** | **4-6小时** | **系统完全恢复** |

## 📋 修复完成检查清单

- [ ] P0: JWT认证问题完全解决
- [ ] P1: 路由配置问题完全解决  
- [ ] 重新执行完整冒烟测试
- [ ] 7个模块全部返回200状态码
- [ ] 整体通过率达到100%
- [ ] 响应时间指标正常
- [ ] 系统健康检查全绿
- [ ] 提交修复代码到版本控制
- [ ] 更新测试报告状态
- [ ] 通知相关团队修复完成

修复完成后，请重新执行原始测试套件以验证所有问题已解决。