# 凌隐宝堂中医诊所诊疗系统 - NSSM服务部署说明

## 📋 部署准备

### 1. 下载NSSM
- 访问官网：https://nssm.cc/download
- 下载最新版本的NSSM
- 解压后将 `nssm.exe` 复制到 `D:\source\repos\LYBTZYZS\LYBT.WebAPI\` 目录中

### 2. 确认文件结构
```
D:\source\repos\LYBTZYZS\LYBT.WebAPI\
├── LYBT.WebAPI.exe              # 主程序
├── nssm.exe                     # NSSM工具 (需要下载)
├── install-service.bat          # 服务安装脚本
├── uninstall-service.bat        # 服务卸载脚本
├── start-service.bat            # 启动服务脚本
├── stop-service.bat             # 停止服务脚本
├── status-service.bat           # 状态查询脚本
├── service-manager.bat          # 服务管理器
├── start-webapi.bat             # 直接启动脚本
├── appsettings*.json            # 配置文件
└── 其他DLL和依赖文件
```

## 🚀 快速部署

### 方式一：图形化管理
1. 双击运行 `service-manager.bat`
2. 选择 `[1] 安装服务`
3. 选择 `[2] 启动服务`
4. 选择 `[8] 打开系统网页` 验证部署

### 方式二：命令行部署
1. **以管理员身份**运行 `install-service.bat`
2. 运行 `start-service.bat` 启动服务
3. 运行 `status-service.bat` 检查状态

## 📊 服务配置详情

### 服务信息
- **服务名称**: `LYBT.WebAPI`
- **显示名称**: `凌隐宝堂中医诊所诊疗系统`
- **端口**: `5000`
- **环境**: `Production`
- **启动类型**: `自动启动`

### 访问地址
- **系统首页**: http://localhost:5000
- **API文档**: http://localhost:5000/swagger
- **健康检查**: http://localhost:5000/health

### 日志配置
- **输出日志**: `logs\service-output.log`
- **错误日志**: `logs\service-error.log`
- **日志轮转**: 每天或10MB时自动轮转

### 恢复设置
- **重启延迟**: 5秒
- **故障恢复**: 自动重启
- **限流保护**: 1.5秒内不重复重启

## 🛠️ 常用操作

### 安装服务
```cmd
# 以管理员身份运行
install-service.bat
```

### 启动/停止服务
```cmd
start-service.bat    # 启动服务
stop-service.bat     # 停止服务
```

### 查看状态
```cmd
status-service.bat   # 详细状态信息
```

### 卸载服务
```cmd
# 以管理员身份运行
uninstall-service.bat
```

### 手动操作
```cmd
# 使用NSSM直接操作
nssm start LYBT.WebAPI
nssm stop LYBT.WebAPI
nssm restart LYBT.WebAPI
nssm status LYBT.WebAPI

# 使用Windows服务命令
net start LYBT.WebAPI
net stop LYBT.WebAPI
sc query LYBT.WebAPI
```

## 🔧 故障排除

### 常见问题

#### 1. 端口占用
```cmd
# 检查端口占用
netstat -an | findstr :5000

# 结束占用进程
taskkill /f /pid [进程ID]
```

#### 2. 权限问题
- 确保以**管理员身份**运行安装/卸载脚本
- 检查当前用户是否有服务管理权限

#### 3. 服务启动失败
1. 查看错误日志：`logs\service-error.log`
2. 检查数据库连接配置
3. 确认所有依赖文件存在
4. 检查.NET 8运行时是否安装

#### 4. API无法访问
1. 确认服务正在运行：`sc query LYBT.WebAPI`
2. 检查防火墙设置
3. 确认端口5000未被其他程序占用
4. 查看服务日志确认启动状态

### 日志分析
```cmd
# 查看最新日志
tail -f logs\service-output.log  # 如果有tail命令
notepad logs\service-output.log  # 使用记事本查看
```

### 配置修改
如需修改服务配置：
1. 停止服务：`stop-service.bat`
2. 修改配置：`appsettings.Production.json`
3. 启动服务：`start-service.bat`

## 📈 性能监控

### 系统资源
```cmd
# 查看进程资源使用
tasklist /fi "imagename eq LYBT.WebAPI.exe"

# 查看内存使用
wmic process where name="LYBT.WebAPI.exe" get PageFileUsage,WorkingSetSize
```

### 日志监控
- 定期检查日志文件大小
- 关注错误日志中的异常信息
- 监控服务重启次数

## 🔒 安全建议

### 生产环境配置
1. **数据库连接**: 使用生产数据库连接字符串
2. **HTTPS配置**: 建议配置SSL证书
3. **防火墙**: 仅开放必要端口
4. **日志管理**: 定期清理和归档日志
5. **权限控制**: 使用专用服务账户运行

### 备份策略
1. **配置备份**: 定期备份 `appsettings.Production.json`
2. **应用备份**: 保留多个版本的发布包
3. **数据库备份**: 建立自动数据库备份机制

## 📞 技术支持

### 系统信息
- **系统名称**: 凌隐宝堂中医诊所诊疗系统
- **版本**: WebAPI v1.0
- **框架**: .NET 8.0
- **服务管理**: NSSM 2.24+

### 联系方式
如遇技术问题，请联系系统管理员并提供：
1. 错误日志内容
2. 服务运行状态
3. 系统环境信息
4. 问题复现步骤

---

*🎉 感谢使用凌隐宝堂中医诊所诊疗系统！*