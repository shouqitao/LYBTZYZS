# Backend — P3-Fix Batch1: DTO绑定修复实施方案

## 🎯 修复策略

基于步骤①的证据分析，确定修复策略：

### 根本原因
ASP.NET Core模型绑定期望的JSON格式与UAT测试脚本发送的格式不匹配。错误"dto field is required"表明系统可能配置了特殊的模型绑定器或中间件。

### 修复方向
**最小改动原则**: 统一JSON属性名称的大小写，确保与DTO定义完全匹配。

## 📋 具体修复步骤

### Step 1: 患者创建DTO修复

**问题**: PatientCreateDto期望首字母大写的属性名
**解决**: UAT脚本使用正确的JSON格式

原有格式:
```json
{"name": "测试患者", "gender": 1, "age": 35}
```

修复格式:
```json
{"Name": "测试患者", "Gender": 1, "Age": 35}
```

### Step 2: 用户创建DTO修复

**问题**: UserMutationDto同样需要首字母大写
**解决**: 更新请求格式

修复格式:
```json
{"Username": "testuser", "RealName": "测试", "Password": "pass", "ConfirmPassword": "pass", "Role": "Doctor"}
```

### Step 3: 看诊开始DTO修复

**问题**: ConsultationStartDto属性名大小写
**解决**: 确保所有Guid字段和其他属性正确大写

修复格式:
```json
{"MedicalCaseId": "guid", "PatientId": "guid", "DoctorId": "guid", "EstimatedDuration": 30}
```

## 🔧 验证方法

1. **快速验证**: 使用curl命令测试正确格式
2. **UAT脚本更新**: 修改UAT测试脚本使用正确的JSON格式
3. **三端点验证**: 确保patients/users/consultations三个创建端点全部正常

## 🚀 实施时间线

- **立即**: 更新UAT测试脚本格式
- **验证**: 运行修复后的测试确认成功
- **文档**: 生成正确的API使用示例

## 📝 预期结果

修复完成后，三个创建端点应该：
- 返回201 Created状态码（成功创建）
- 或返回200 OK状态码（根据控制器实现）
- 消除"dto field is required"错误
- UAT基线测试完全通过

---

**修复实施**: 2025-09-15 22:00:00  
**预期完成**: 2025-09-15 22:15:00  
**策略**: 最小改动，格式统一