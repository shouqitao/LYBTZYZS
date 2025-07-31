# LYBT 自动化部署脚本使用说明

## 📁 脚本文件列表

### 🚀 部署相关脚本
| 脚本名称 | 说明 | 使用场景 |
|---------|------|----------|
| `auto-deploy.bat` | 主部署脚本 | 本地一键部署 |
| `upload-to-server.ps1` | 文件上传脚本 | 自动上传部署包 |
| `trigger-server-deploy.ps1` | 远程部署触发 | 触发服务器端部署 |
| `server-deploy.bat` | 服务器端部署脚本 | 服务器自动部署 |

### 🔧 服务器配置脚本
| 脚本名称 | 说明 | 使用场景 |
|---------|------|----------|
| `setup-server.bat` | 服务器环境初始化 | 首次服务器配置 |
| `install-service.bat` | Windows服务安装 | 安装系统服务 |
| `file-monitor.bat` | 文件监控脚本 | 自动监控部署信号 |

### 🧪 测试和检查脚本
| 脚本名称 | 说明 | 使用场景 |
|---------|------|----------|
| `test-encoding.bat` | 中文编码测试 | 验证字符显示 |
| `test-deploy-system.bat` | 部署系统测试 | 验证部署环境 |
| `test-full-deployment.bat` | 完整部署测试 | 端到端测试 |
| `health-check.bat` | 服务健康检查 | 验证服务状态 |

## 🎯 使用流程

### 首次设置
```batch
# 1. 服务器端初始化（服务器上运行）
setup-server.bat

# 2. 安装Windows服务（可选）
install-service.bat

# 3. 测试中文编码
test-encoding.bat
```

### 日常部署
```batch
# 1. 测试部署环境
test-deploy-system.bat

# 2. 执行自动部署
auto-deploy.bat

# 3. 检查服务状态
health-check.bat
```

### 完整测试
```batch
# 端到端完整测试
test-full-deployment.bat
```

## ⚙️ 配置说明

### 主要配置参数
在 `auto-deploy.bat` 中修改：
```batch
set "SERVER_IP=192.168.190.243"           # 服务器IP
set "SERVER_USER=Administrator"           # 服务器用户名
set "LOCAL_PROJECT_PATH=D:\source\repos\LYBTZYZS\src\Backend\Services\LYBT.WebAPI"
set "LOCAL_PUBLISH_PATH=D:\source\repos\LYBTZYZS\Release\WebAPI"
```

### 服务器端配置
在 `server-deploy.bat` 中修改：
```batch
set "DEPLOY_PATH=C:\LYBT\WebAPI"          # 部署路径
set "BACKUP_PATH=C:\LYBT\Backup"          # 备份路径
set "SERVICE_NAME=LYBTWebAPI"             # 服务名称
```

## 🔍 故障排除

### 常见问题及解决方案

#### 1. 中文字符显示乱码
```batch
# 运行编码测试
test-encoding.bat
```
**解决方案**：确保控制台支持UTF-8编码

#### 2. 网络连接失败
```batch
# 测试网络连通性
ping 192.168.190.243
```
**解决方案**：检查防火墙和网络配置

#### 3. PowerShell执行策略错误
```powershell
# 设置执行策略
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### 4. 服务启动失败
```batch
# 检查服务状态
health-check.bat
```
**解决方案**：检查端口占用和权限设置

## 📊 日志和监控

### 日志位置
- **部署日志**：`C:\LYBT\Logs\deploy.log`
- **健康检查报告**：`C:\LYBT\Logs\health-check-[日期时间].txt`
- **应用程序日志**：Windows事件查看器

### 监控端点
- **健康检查**：`http://服务器IP:5297/health`
- **API文档**：`http://服务器IP:5297/swagger`

## 🔐 安全建议

1. **限制网络访问**：配置防火墙规则
2. **使用专用账户**：避免使用Administrator账户
3. **定期备份**：保持自动备份机制
4. **监控日志**：定期检查部署和应用程序日志

## 📞 技术支持

### 手动操作命令
```batch
# 手动停止服务
net stop LYBTWebAPI

# 手动启动服务  
net start LYBTWebAPI

# 查看服务状态
sc query LYBTWebAPI

# 查看进程
tasklist | findstr LYBT.WebAPI.exe

# 查看端口占用
netstat -an | findstr :5297
```

### API测试命令
```bash
# 健康检查
curl http://192.168.190.243:5297/health

# 登录测试
curl -X POST http://192.168.190.243:5297/api/v1/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"Admin@123456"}'
```

---

**注意**：首次使用建议在测试环境完整验证后再部署到生产环境。