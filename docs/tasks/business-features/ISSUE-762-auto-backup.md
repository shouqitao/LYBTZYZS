# Issue #762: 【数据安全】实现自动备份机制

## 概述
**优先级**: P0-必须  
**类型**: 数据安全  
**预计工时**: 16小时  
**业务价值**: 保护医疗数据安全，防止数据丢失

## 背景
医疗数据极其重要，一旦丢失将造成严重后果。目前系统缺少自动备份机制，存在重大数据安全隐患。

## 需求说明

### 功能需求
1. **自动备份**
   - 每日凌晨2点自动备份
   - 支持手动触发备份
   - 备份前验证数据库连接

2. **备份内容**
   - 数据库完整备份（.bak文件）
   - 配置文件备份
   - 上传的文件备份（如处方图片）

3. **备份管理**
   - 保留最近30天的备份
   - 自动清理过期备份
   - 备份文件命名规则：LYBT_Backup_YYYYMMDD_HHmmss.bak

4. **恢复功能**
   - 选择备份文件恢复
   - 恢复前自动备份当前数据
   - 恢复进度显示

## 技术方案

### 1. 备份服务
```csharp
public interface IBackupService
{
    Task<BackupResult> BackupDatabaseAsync();
    Task<RestoreResult> RestoreDatabaseAsync(string backupFile);
    Task<List<BackupInfo>> GetBackupListAsync();
    Task CleanupOldBackupsAsync(int daysToKeep);
}
```

### 2. 定时任务（使用Hangfire）
```csharp
public class BackupJob
{
    public async Task ExecuteDailyBackup()
    {
        // 1. 执行数据库备份
        // 2. 压缩备份文件
        // 3. 清理旧备份
        // 4. 发送通知
    }
}
```

### 3. 配置选项
```json
{
  "Backup": {
    "Enabled": true,
    "BackupPath": "D:\\Backups\\LYBT",
    "DaysToKeep": 30,
    "DailyBackupTime": "02:00",
    "CompressBackup": true
  }
}
```

## 实施步骤

### Phase 1: 备份基础设施（8小时）
- [ ] 创建IBackupService接口
- [ ] 实现SqlServerBackupService
- [ ] 创建备份文件管理器
- [ ] 添加备份配置选项

### Phase 2: 定时任务（4小时）
- [ ] 集成Hangfire
- [ ] 创建BackupJob
- [ ] 配置定时任务
- [ ] 添加手动触发接口

### Phase 3: 恢复功能（4小时）
- [ ] 实现数据库恢复
- [ ] 添加恢复前备份
- [ ] 创建恢复UI界面
- [ ] 添加恢复日志

## 验收标准
- [ ] 自动备份每日执行成功
- [ ] 备份文件可以成功恢复
- [ ] 旧备份自动清理
- [ ] 备份失败有告警通知
- [ ] 恢复操作有审计日志

## 注意事项
1. 备份路径必须有足够空间
2. 备份过程不能影响系统运行
3. 恢复操作需要管理员权限
4. 备份文件需要加密存储

## 测试要求
1. 备份恢复完整性测试
2. 备份性能测试（不超过5分钟）
3. 存储空间不足处理
4. 并发备份冲突测试

---
*创建日期: 2025-09-27*  
*负责人: 待分配*