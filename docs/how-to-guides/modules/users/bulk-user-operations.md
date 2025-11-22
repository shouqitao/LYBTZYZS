# 用户批量操作指南 (Bulk User Operations Guide)

> **目标导向**: 解决LYBTZYZS用户管理中的批量操作问题
> **适合人群**: 系统管理员、IT运维人员、诊所管理人员
> **解决问题**: 批量导入、批量更新、批量删除、数据迁移

## 🔥 高频批量操作场景

### 场景1: 新员工入职批量开户
**问题**: 诊所招聘了10名新员工，需要批量创建系统账户
**解决步骤**:
1. 准备Excel模板包含员工基本信息
2. 使用批量导入功能创建账户
3. 批量设置初始密码和角色权限
4. 通知员工首次登录修改密码

### 场景2: 年度权限审查
**问题**: 需要审查所有用户的权限，停用离职人员账户
**解决步骤**:
1. 导出当前用户清单和权限配置
2. 对照HR离职名单识别需要停用的账户
3. 批量更新用户状态为禁用
4. 生成权限变更报告

### 场景3: 科室调整批量操作
**问题**: 内科重组，需要调整相关医生的权限和分组
**解决步骤**:
1. 筛选内科相关医生用户
2. 批量更新角色权限和科室信息
3. 通知相关医生权限变更
4. 验证新权限配置生效

## 📋 批量操作准备

### 数据准备和验证

#### Excel模板格式
```csv
用户名,真实姓名,手机号,邮箱,角色,科室,职称,备注
doctor_wang,王医生,13800138001,wang@clinic.com,Doctor,内科,主治医师,10年临床经验
nurse_li,李护士,13800138002,li@clinic.com,Nurse,外科,主管护师,护理经验丰富
admin_zhang,张管理员,13800138003,zhang@clinic.com,Admin,行政,IT管理员,系统维护
```

#### 数据验证规则
```csharp
public class BulkUserDataValidator
{
    public ValidationResult ValidateExcelData(DataTable excelData)
    {
        var result = new ValidationResult();

        foreach (DataRow row in excelData.Rows)
        {
            var userName = row["用户名"].ToString();
            var realName = row["真实姓名"].ToString();
            var phone = row["手机号"].ToString();
            var email = row["邮箱"].ToString();
            var role = row["角色"].ToString();

            // 用户名验证
            if (string.IsNullOrWhiteSpace(userName) || userName.Length < 3)
                result.AddError($"第{result.ErrorCount + 1}行: 用户名不能为空且长度至少3位");

            // 角色验证
            var validRoles = new[] { "Doctor", "Nurse", "Admin" };
            if (!validRoles.Contains(role))
                result.AddError($"第{result.ErrorCount + 1}行: 角色'{role}'无效");

            // 手机号格式验证
            if (!Regex.IsMatch(phone, @"^1[3-9]\d{9}$"))
                result.AddError($"第{result.ErrorCount + 1}行: 手机号格式不正确");
        }

        return result;
    }
}
```

#### 模板文件下载
```bash
GET /api/v1/users/bulk-template
Authorization: Bearer <admin_token>

# 下载标准Excel模板文件
# Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
```

## 🔧 批量导入功能

### 步骤1: 上传数据文件

#### API调用示例
```bash
POST /api/v1/users/bulk-import
Authorization: Bearer <admin_token>
Content-Type: multipart/form-data

file=@users_data.xlsx
options={
  "dryRun": false,
  "skipDuplicates": true,
  "defaultPassword": "TempPassword123!",
  "forcePasswordChange": true,
  "sendNotification": true
}
```

#### 请求参数说明
| 参数名 | 类型 | 必填 | 默认值 | 描述 |
|--------|------|------|--------|------|
| file | file | 是 | - | Excel文件（.xlsx格式） |
| dryRun | boolean | 否 | false | 预演模式，不实际创建用户 |
| skipDuplicates | boolean | 否 | true | 跳过重复的用户名 |
| defaultPassword | string | 否 | TempPass123! | 默认密码（文件中未提供时使用） |
| forcePasswordChange | boolean | 否 | true | 强制首次登录修改密码 |
| sendNotification | boolean | 否 | false | 发送账户通知邮件 |

### 步骤2: 预演和验证

#### 预演模式执行
```json
{
  "success": true,
  "message": "预演完成，发现5个问题需要处理",
  "data": {
    "totalRecords": 25,
    "validRecords": 22,
    "errorRecords": 3,
    "duplicateRecords": 2,
    "validationErrors": [
      {
        "rowNumber": 5,
        "fieldName": "手机号",
        "value": "13800138abc",
        "error": "手机号格式不正确"
      },
      {
        "rowNumber": 12,
        "fieldName": "邮箱",
        "value": "invalid-email",
        "error": "邮箱格式不正确"
      }
    ],
    "duplicateUsers": [
      "existing_user1",
      "existing_user2"
    ],
    "previewData": [
      {
        "userName": "doctor_new1",
        "realName": "新医生1",
        "role": "Doctor",
        "status": "WillCreate"
      }
    ]
  }
}
```

### 步骤3: 正式导入执行

#### 实际导入响应
```json
{
  "success": true,
  "message": "批量导入完成",
  "data": {
    "operationId": "bulk_import_20250101_120000",
    "summary": {
      "totalProcessed": 25,
      "successful": 20,
      "failed": 3,
      "skipped": 2,
      "duration": "00:02:15"
    },
    "results": [
      {
        "rowNumber": 1,
        "userName": "doctor_new1",
        "status": "Success",
        "userId": "00000000-0000-0000-0000-000000000001",
        "message": "用户创建成功"
      },
      {
        "rowNumber": 5,
        "userName": "doctor_new5",
        "status": "Failed",
        "error": "手机号格式不正确"
      }
    ]
  }
}
```

## 🔄 批量更新功能

### 批量更新用户信息

#### 更新请求示例
```bash
PUT /api/v1/users/bulk-update
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "filter": {
    "role": "Doctor",
    "status": "Active",
    "department": "内科"
  },
  "updates": {
    "department": "中西医结合科",
    "title": "主治医师",
    "remark": "科室调整升级"
  },
  "options": {
    "dryRun": false,
    "sendNotification": true,
    "logChanges": true
  }
}
```

#### 条件筛选参数
```json
{
  "filter": {
    "userIds": ["uuid1", "uuid2", "uuid3"],           // 指定用户ID列表
    "userNames": ["doctor1", "doctor2"],              // 指定用户名列表
    "role": "Doctor",                                 // 角色筛选
    "status": "Active",                               // 状态筛选
    "department": "内科",                              // 科室筛选
    "createdAfter": "2024-01-01",                     // 创建时间起始
    "createdBefore": "2024-12-31",                    // 创建时间结束
    "lastLoginAfter": "2024-06-01",                   // 最后登录时间起始
    "customFilter": {                                 // 自定义筛选条件
      "key": "value"
    }
  }
}
```

#### 批量状态切换
```bash
POST /api/v1/users/bulk-status-toggle
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "userIds": [
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002"
  ],
  "targetStatus": "Disabled",
  "reason": "系统维护期间临时禁用",
  "scheduledTime": "2025-01-01T23:00:00Z"  // 可选，定时执行
}
```

## 🗑️ 批量删除功能

### 安全删除机制

#### 删除前验证
```csharp
public class BulkDeleteService
{
    public async Task<BulkDeleteResult> ValidateForDeleteAsync(List<Guid> userIds)
    {
        var result = new BulkDeleteResult();

        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                result.AddError(userId, "用户不存在");
                continue;
            }

            // 检查用户是否有关联数据
            var hasPatients = await _patientRepository.AnyAsync(p => p.CreatedBy == userId);
            if (hasPatients)
            {
                result.AddWarning(userId, "用户有关联的患者数据，建议禁用而非删除");
            }

            var hasPrescriptions = await _prescriptionRepository.AnyAsync(p => p.DoctorId == userId);
            if (hasPrescriptions)
            {
                result.AddError(userId, "用户有处方数据，无法删除");
            }
        }

        return result;
    }
}
```

#### 软删除 vs 硬删除
```bash
POST /api/v1/users/bulk-delete
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "userIds": [
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002"
  ],
  "deleteMode": "Soft",                          // Soft/Hard
  "archiveData": true,                            // 是否归档数据
  "confirmationRequired": true,                   // 是否需要二次确认
  "reason": "员工离职，清理账户数据"
}
```

#### 删除确认流程
```bash
# 第一步：预删除验证
POST /api/v1/users/bulk-delete/validate
Content-Type: application/json

{
  "userIds": ["uuid1", "uuid2"],
  "deleteMode": "Soft"
}

# 响应：验证结果和风险评估
{
  "canDelete": true,
  "warnings": [
    "用户uuid1有关联的患者数据，建议使用软删除"
  ],
  "blockingIssues": [],
  "riskLevel": "Low"
}

# 第二步：确认执行删除
POST /api/v1/users/bulk-delete/confirm
Content-Type: application/json

{
  "validationId": "delete_validation_12345",
  "confirmedBy": "admin",
  "confirmedAt": "2025-01-01T12:00:00Z"
}
```

## 📊 批量操作监控和报告

### 操作状态跟踪

#### 操作进度查询
```bash
GET /api/v1/users/bulk-operations/{operationId}/status
Authorization: Bearer <admin_token>
```

#### 进度状态响应
```json
{
  "operationId": "bulk_import_20250101_120000",
  "operationType": "Import",
  "status": "InProgress",
  "progress": {
    "total": 100,
    "completed": 45,
    "failed": 2,
    "percentage": 45
  },
  "startTime": "2025-01-01T12:00:00Z",
  "estimatedCompletion": "2025-01-01T12:05:30Z",
  "currentStep": "Processing users 41-50"
}
```

### 操作结果报告

#### 生成操作报告
```bash
GET /api/v1/users/bulk-operations/{operationId}/report
Authorization: Bearer <admin_token>
```

#### 报告内容结构
```json
{
  "operationId": "bulk_import_20250101_120000",
  "operationType": "Import",
  "executedBy": "admin",
  "executionTime": "2025-01-01T12:00:00Z",
  "duration": "00:02:15",
  "summary": {
    "totalRecords": 100,
    "successful": 95,
    "failed": 3,
    "skipped": 2,
    "successRate": 95.0
  },
  "details": {
    "successfulOperations": [
      {
        "recordId": 1,
        "userName": "doctor_new1",
        "operation": "Create",
        "timestamp": "2025-01-01T12:00:05Z",
        "userId": "00000000-0000-0000-0000-000000000001"
      }
    ],
    "failedOperations": [
      {
        "recordId": 5,
        "userName": "doctor_new5",
        "operation": "Create",
        "error": "手机号格式不正确",
        "timestamp": "2025-01-01T12:00:25Z"
      }
    ]
  },
  "recommendations": [
    "建议在执行前进行数据验证",
    "考虑分批次处理大量数据"
  ]
}
```

### 错误处理和重试

#### 失败操作重试
```bash
POST /api/v1/users/bulk-operations/{operationId}/retry
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "failedRecordIds": [5, 12, 23],
  "retryMode": "Selective",                        // All/Selective
  "fixData": {
    "5": {
      "phoneNumber": "13800138005"  // 修正第5行的手机号
    }
  }
}
```

## 🛡️ 安全和合规

### 操作权限控制

#### 批量操作权限矩阵
| 操作类型 | SuperAdmin | Admin | Doctor | Nurse |
|----------|------------|-------|--------|-------|
| 批量导入 | ✅ | ⚠️¹ | ❌ | ❌ |
| 批量更新 | ✅ | ⚠️² | ❌ | ❌ |
| 批量删除 | ✅ | ❌ | ❌ | ❌ |
| 批量禁用 | ✅ | ⚠️³ | ❌ | ❌ |

**权限说明**:
- ⚠️¹: 只能创建Doctor和Nurse角色，不能创建Admin
- ⚠️²: 只能更新Doctor和Nurse信息，不能修改Admin用户
- ⚠️³: 只能禁用Doctor和Nurse，不能禁用其他Admin

#### 操作审计
```csharp
public class BulkOperationAuditService
{
    public async Task LogBulkOperationAsync(BulkOperation operation)
    {
        var audit = new BulkOperationAudit
        {
            OperationId = operation.Id,
            OperationType = operation.Type,
            OperatorId = GetCurrentUserId(),
            OperatorName = GetCurrentUserName(),
            TotalRecords = operation.TotalRecords,
            SuccessfulRecords = operation.SuccessfulRecords,
            FailedRecords = operation.FailedRecords,
            ExecutionTime = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(operation.Details),
            IPAddress = GetClientIPAddress(),
            UserAgent = GetUserAgent()
        };

        await _auditRepository.AddAsync(audit);
    }
}
```

### 数据保护措施

#### 敏感数据处理
```csharp
public class DataProtectionService
{
    public string MaskSensitiveData(string data, string dataType)
    {
        return dataType switch
        {
            "PhoneNumber" => MaskPhoneNumber(data),
            "Email" => MaskEmail(data),
            "IdCard" => MaskIdCard(data),
            _ => data
        };
    }

    private string MaskPhoneNumber(string phone)
    {
        return phone.Length >= 7
            ? $"{phone.Substring(0, 3)}****{phone.Substring(phone.Length - 4)}"
            : "****";
    }
}
```

#### 操作确认机制
```bash
# 高风险操作需要二次确认
POST /api/v1/users/bulk-delete/confirm
Content-Type: application/json

{
  "operationId": "bulk_delete_20250101_120000",
  "confirmationCode": "123456",  // 发送到管理员手机的验证码
  "adminPassword": "admin_password_hash",
  "reason": "确认执行批量删除操作"
}
```

## 🚀 性能优化

### 批处理策略

#### 分批处理实现
```csharp
public class BatchProcessor
{
    private const int DefaultBatchSize = 50;
    private const int MaxConcurrentBatches = 3;

    public async Task<BulkOperationResult> ProcessAsync<T>(
        IEnumerable<T> items,
        Func<T, Task<OperationResult>> processor,
        CancellationToken cancellationToken = default)
    {
        var batches = items.Chunk(DefaultBatchSize);
        var semaphore = new SemaphoreSlim(MaxConcurrentBatches);
        var tasks = new List<Task<OperationResult>>();

        foreach (var batch in batches)
        {
            await semaphore.WaitAsync(cancellationToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    var results = new List<OperationResult>();
                    foreach (var item in batch)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var result = await processor(item);
                        results.Add(result);
                    }
                    return results;
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        var batchResults = await Task.WhenAll(tasks);
        return ConsolidateResults(batchResults);
    }
}
```

#### 数据库优化
```sql
-- 批量插入优化
CREATE TABLE #TempUsers (
    UserName NVARCHAR(50),
    RealName NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    Email NVARCHAR(100),
    Role NVARCHAR(20),
    PinYinCode NVARCHAR(100)
)

-- 批量插入临时表
BULK INSERT #TempUsers
FROM 'C:\temp\users_data.csv'
WITH (
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    FIRSTROW = 2
)

-- 批量插入正式表，处理重复数据
INSERT INTO Users (UserName, RealName, PhoneNumber, Email, Role, PinYinCode, CreatedAt)
SELECT
    t.UserName,
    t.RealName,
    t.PhoneNumber,
    t.Email,
    t.Role,
    t.PinYinCode,
    GETUTCDATE()
FROM #TempUsers t
LEFT JOIN Users u ON t.UserName = u.UserName
WHERE u.UserName IS NULL
```

## 📋 操作检查清单

### 批量导入前检查
- [ ] 数据文件格式正确（Excel .xlsx）
- [ ] 必填字段完整且格式正确
- [ ] 用户名唯一性检查
- [ ] 角色权限配置合理
- [ ] 手机号邮箱格式验证
- [ ] 执行预演模式验证结果
- [ ] 确认目标环境和数据备份

### 批量更新前检查
- [ ] 筛选条件准确无误
- [ ] 更新数据符合业务规则
- [ ] 影响范围评估合理
- [ ] 相关用户已通知
- [ ] 操作日志记录开启

### 批量删除前检查
- [ ] 用户数据关联分析完成
- [ ] 重要数据已备份归档
- [ ] 删除权限确认
- [ ] 二次确认流程完成
- [ ] 恢复方案准备就绪

## 🔗 相关资源

### API文档
- [Users API参考](../../reference/api/users.md)
- [批量操作API](../../reference/api/bulk-operations.md)
- [错误代码参考](../../reference/error-codes.md)

### 操作指南
- [Excel数据处理技巧](../development/excel-data-processing.md)
- [数据验证最佳实践](../development/data-validation.md)
- [操作审计和监控](../monitoring/audit-logging.md)

### 外部资源
- [EPPlus库文档](https://epplussoftware.com/)
- [批量处理最佳实践](https://docs.microsoft.com/aspnet/core/performance/caching/)
- [数据保护法规](https://gdpr-info.eu/)

---

**文档类型**: How-to Guide
**更新时间**: 2025-11-22
**维护团队**: 架构组 + 运维团队
**质量保证**: 所有批量操作都经过性能和安全测试