# 小诊所部署指南 - Phase E1 资源保守配置

**版本**: Phase E1  
**适用环境**: 2-5名医生、<20用户、日访问量<1000次的小型诊所

## 🎯 部署目标

为小诊所环境提供资源保守、稳定可靠的系统配置，确保在有限资源条件下的最佳性能表现。

## 📋 硬件要求

### 最低配置
- **CPU**: 2核心 2.0GHz
- **内存**: 4GB RAM
- **存储**: 50GB 可用空间
- **网络**: 10Mbps带宽

### 推荐配置
- **CPU**: 4核心 2.5GHz
- **内存**: 8GB RAM
- **存储**: 100GB SSD
- **网络**: 50Mbps带宽

## 🔧 配置部署步骤

### 1. 复制小诊所优化配置

```bash
# 在生产服务器上
cp appsettings.ClinicOptimized.json appsettings.Production.json
```

### 2. 环境变量配置

**必须设置的生产环境变量**:

```bash
# JWT密钥 (32字符)
JWT_SECRET="您的32字符长度的安全密钥"

# 数据库连接字符串  
CONNECTION_STRING="Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=10;Command Timeout=15;Max Pool Size=10;Min Pool Size=1;Pooling=true"

# 管理员初始密码
SYSADMIN_PASSWORD="您的强密码"
```

### 3. 数据库配置优化

**SQL Server设置建议**:

```sql
-- 设置最大内存使用 (保留2GB给操作系统)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'max server memory', 2048; -- 2GB for 4GB system
RECONFIGURE;

-- 优化文件增长设置
ALTER DATABASE LYBTDB 
MODIFY FILE (NAME = 'LYBTDB', FILEGROWTH = 100MB);

ALTER DATABASE LYBTDB 
MODIFY FILE (NAME = 'LYBTDB_Log', FILEGROWTH = 50MB);
```

## 📊 资源配置详解

### 1. 数据库连接池优化

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...;Max Pool Size=10;Min Pool Size=1;Connection Timeout=10;Command Timeout=15;..."
  }
}
```

**参数说明**:
- `Max Pool Size=10`: 最大10个并发连接，适合<20用户
- `Min Pool Size=1`: 最小资源占用
- `Connection Timeout=10`: 快速连接超时，避免资源浪费
- `Command Timeout=15`: 适中的命令超时，平衡性能和稳定性

### 2. 内存缓存优化

```json
{
  "CacheOptions": {
    "MemoryCache": {
      "SizeLimit": 50,
      "CompactionPercentage": 0.20,
      "ExpirationScanFrequency": 60
    }
  }
}
```

**参数说明**:
- `SizeLimit=50`: 缓存项目限制为50个，控制内存使用
- `CompactionPercentage=0.20`: 当达到限制时清理20%的项目
- `ExpirationScanFrequency=60`: 每60秒扫描过期项目

### 3. 日志配置优化

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "retainedFileCountLimit": 7,
          "fileSizeLimitBytes": 5242880
        }
      }
    ]
  }
}
```

**参数说明**:
- `Default": "Warning"`: 只记录警告级别以上的日志
- `retainedFileCountLimit": 7`: 只保留7天的日志文件
- `fileSizeLimitBytes": 5242880`: 单个日志文件最大5MB

### 4. 批量操作限制

```json
{
  "UserOptions": {
    "MaxBatchOperationSize": 50
  }
}
```

**参数说明**:
- `MaxBatchOperationSize=50`: 批量操作限制为50条记录，避免内存溢出

## 🚀 部署验证检查清单

### 系统启动验证
- [ ] 服务正常启动，无错误日志
- [ ] 数据库连接成功
- [ ] 健康检查端点返回正常状态

### 性能验证
- [ ] 内存使用 < 512MB
- [ ] CPU使用 < 70%
- [ ] 响应时间 < 2秒
- [ ] 数据库连接数 < 10

### 功能验证
- [ ] 用户登录正常
- [ ] 患者管理功能正常
- [ ] 处方开具功能正常
- [ ] 数据备份正常

## 📈 监控建议

### 1. 系统资源监控

**监控指标**:
- CPU使用率 (目标: <70%)
- 内存使用率 (目标: <80%)
- 磁盘使用率 (目标: <90%)
- 网络带宽使用

### 2. 应用性能监控

**监控指标**:
- API响应时间 (目标: <2秒)
- 数据库连接数 (目标: <10)
- 并发用户数 (目标: <20)
- 错误率 (目标: <1%)

### 3. 业务指标监控

**监控指标**:
- 日均登录用户数
- 日均处方数量
- 数据库大小增长
- 日志文件大小

## 🔧 故障排除

### 常见问题及解决方案

#### 1. 内存使用过高
**症状**: 系统响应慢，内存使用>80%  
**解决**: 
- 减少缓存大小限制
- 增加缓存清理频率
- 重启应用释放内存

#### 2. 数据库连接超时
**症状**: 频繁出现连接超时错误  
**解决**:
- 检查网络连接
- 增加连接超时时间
- 优化慢查询

#### 3. 磁盘空间不足
**症状**: 日志显示磁盘空间警告  
**解决**:
- 清理旧日志文件
- 减少日志保留天数
- 压缩数据库文件

## 📞 技术支持

### 日志位置
- **应用日志**: `logs/lybt-{date}.log`
- **系统日志**: Windows事件查看器
- **IIS日志**: `C:\inetpub\logs\LogFiles\`

### 配置文件位置
- **主配置**: `appsettings.Production.json`
- **连接字符串**: 环境变量或配置文件
- **日志配置**: Serilog配置段

---

**注意**: 此配置专为小诊所环境优化，如需更高性能或更大规模部署，请参考企业级部署指南。