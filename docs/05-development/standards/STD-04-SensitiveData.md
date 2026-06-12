# STD-04: 敏感数据标记规范

## 适用范围

全系统包含个人身份信息 (PII) 的实体字段。

## 规范内容

### 标记方式

使用 `[SensitiveData]` 特性标记 PII 字段，日志框架 `SensitiveDataMasker` 自动脱敏。

```csharp
[SensitiveData(MaskingMode = MaskingMode.Partial)]
public string PhoneNumber { get; set; }
```

### 必须标记的字段

| 实体 | 字段 | MaskingMode | 脱敏效果 |
|------|------|-------------|---------|
| Patient | PhoneNumber | Partial | 138****1234 |
| Patient | IdNumber | Partial | 110***********1234 |
| Patient | EmergencyContactPhone | Partial | 138****5678 |
| User | PhoneNumber | Partial | 138****1234 |
| User | Email | Partial | z***@example.com |
| User | PasswordHash | Full | ******** |

### MaskingMode 选择规则

| 模式 | 用途 | 说明 |
|------|------|------|
| Partial | 电话/身份证/邮箱 | 保留首尾部分，中间掩码，便于人工辨识 |
| Full | 密码/密钥/Token | 完全掩码，不保留任何可辨识内容 |
| Hash | 需要关联但不可读的场景 | 输出哈希值，可用于日志关联分析 |

### 规则

1. 新增包含 PII 的字段时，必须添加 `[SensitiveData]` 特性
2. 日志输出禁止直接拼接 PII 字段值，使用结构化日志 `{@Entity}` 自动触发脱敏
3. API 响应中的 PII 字段按角色脱敏: Admin/SuperAdmin 完整显示，Doctor 及以下角色掩码显示
4. DTO 到日志的映射必须经过 `SensitiveDataMasker` 处理

## 参考

- 日志规范: `docs/02-requirements/14-logging.md`
- 患者信息脱敏: `docs/02-requirements/04-patients.md`

---

创建日期: 2026-02-26
