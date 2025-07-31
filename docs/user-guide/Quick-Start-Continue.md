# 快速启动脚本 - 重启后继续任务

## 1. 环境准备
```bash
cd "D:\source\repos\LYBTZYZS"

# 检查并停止可能残留的进程
powershell "Get-Process -Name '*LYBT*' -ErrorAction SilentlyContinue | Stop-Process -Force"
```

## 2. 启动WebAPI服务
```bash
cd "D:\source\repos\LYBTZYZS\src\Backend\Services\LYBT.WebAPI"
dotnet run
```

## 3. 验证服务状态
```bash
# 健康检查
curl -X GET "http://localhost:5297/api/v1/Health/basic"

# 密码哈希生成测试
curl -X GET "http://localhost:5297/api/v1/Auth/hashPassword?password=Admin@123456"
```

## 4. 测试超级管理员登录
```bash
curl -X POST "http://localhost:5297/api/v1/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"Admin@123456","rememberMe":true,"loginType":"Password"}'
```

## 5. 如果登录失败，执行数据库重置
```sql
-- 连接数据库
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "LYBTDB"

-- 删除旧记录
DELETE FROM AdminSecrets WHERE UserName = 'sysadmin';

-- 使用API生成的新哈希插入记录
-- INSERT INTO AdminSecrets (Id, UserName, PasswordHash) VALUES (NEWID(), 'sysadmin', '[新哈希]');
```

## 6. 成功登录后进行API测试
- 访问 Swagger UI: http://localhost:5297/swagger
- 使用获得的JWT令牌测试业务API
- 生成完整的API测试报告

## 重要提醒
- 确保LocalDB服务正在运行
- 如遇到端口占用，检查并关闭其他.NET进程  
- 登录成功后会获得JWT令牌，用于后续API调用

## 当前任务状态
所有基础设施已配置完成，重启后可直接继续认证验证和API测试工作。