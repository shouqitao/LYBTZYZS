# WebAPI Newman 测试修复总结

## 修复概览

针对 Newman 集成测试中的 196 个断言失败，实施了以下修复措施。

---

## 问题诊断

### 问题 1: Windows Shell UTF-8 编码问题
- **症状**: 传递中文字符的 JSON 数据时，API 返回 "$.name: The JSON value could not be converted to System.String"
- **根因**: Windows shell 在通过 curl 命令行传递 UTF-8 字符时会破坏编码
- **影响**: Create Patient, Create Herb, Create Formula 等请求失败

### 问题 2: PatientInputDto 缺少必需字段
- **症状**: 400 Bad Request 错误
- **根因**: PatientInputDtoValidator 要求 IdNumber 字段，但 Postman 请求未提供
- **影响**: 患者创建测试失败

### 问题 3: 电话号码/身份证唯一性约束
- **症状**: 重复运行测试时，创建患者返回 400 错误
- **根因**: 数据库中已存在相同 PhoneNumber 或 IdNumber 的记录
- **影响**: 重复测试运行失败

### 问题 4: UserMapper 构建错误
- **症状**: 编译错误 - MustChangeOnNextLogin 字段映射问题
- **根因**: UserMapper 未正确处理 MustChangeOnNextLogin 字段
- **影响**: WebAPI 无法启动

---

## 实施的修复

### 1. UserMapper.cs 修复

```csharp
// 添加忽略属性以修复构建错误
[MapperIgnoreTarget(nameof(User.MustChangeOnNextLogin))]
public partial User ToEntity(UserInputDto dto);

[MapperIgnoreTarget(nameof(User.MustChangeOnNextLogin))]
public partial void UpdateEntity(UserInputDto dto, [MappingTarget] User entity);
```

**位置**: `src/LYBTZYZS.Server.Application/Contracts/Users/Mappers/UserMapper.cs`

### 2. Postman 集合数据修复

#### Create Test Patient
```json
{
  "Name": "TestPatientSetup",
  "Gender": 1,
  "BirthDate": "1990-01-01T00:00:00Z",
  "PhoneNumber": "1380005560",      // 已更新为唯一值
  "IdNumber": "1101011990010105560", // 已添加并更新为唯一值
  "Address": "Test Address"
}
```

#### Create Test Herb
```json
{
  "Name": "TestHerbSetup",
  "Unit": "gram",
  "Price": 10.0,
  "Category": "TestCategory"
}
```

#### Create Test Formula
```json
{
  "Name": "TestFormulaSetup",
  "Effect": "TestEffect",
  "Usage": "TestUsage",
  "Herbs": [
    {
      "HerbName": "TestHerb",
      "Dosage": 10,
      "Unit": "gram"
    }
  ],
  "IsShared": false
}
```

**位置**: `docs/06-operations/LYBTZYZS_API_Collection.json`

---

## 修复效果

| 指标 | 修复前 | 修复后 | 改善 |
|------|--------|--------|------|
| 总断言数 | 324 | 324 | - |
| 失败断言 | 196 | ~60 (预估) | -136 (69%↓) |
| 通过率 | 39% | ~81% (预估) | +42% |

**注意**: 剩余失败主要由于:
1. 某些 API 端点不存在 (404 错误)
2. 某些请求方法不被允许 (405 错误)
3. 需要手动清理数据库中的重复数据

---

## 验证方法

### 运行 Newman 测试
```bash
# 确保 WebAPI 正在运行
./scripts/run-webapi.ps1

# 运行 Newman 测试
cd docs/06-operations
newman run LYBTZYZS_API_Collection.json -e environment.json --reporters cli,json --reporter-json-export newman-report-latest.json
```

### 检查测试结果
```bash
# 查看测试统计
python3 -c "
import json
with open('newman-report-latest.json') as f:
    data = json.load(f)
    stats = data['run']['stats']
    total = stats['assertions']['total']
    failed = stats['assertions']['failed']
    print(f'Assertions: {total-failed}/{total} passed ({((total-failed)/total*100):.1f}%)')
"
```

---

## 后续建议

1. **动态测试数据**: 使用 Postman 的预请求脚本动态生成唯一值
   ```javascript
   pm.collectionVariables.set('uniquePhone', '13800' + Math.floor(Math.random() * 100000));
   ```

2. **数据库清理**: 在测试运行前清理测试数据
   ```sql
   DELETE FROM Patients WHERE Name LIKE 'Test%';
   DELETE FROM Herbs WHERE Name LIKE 'Test%';
   ```

3. **端点验证**: 检查并修复 404/405 错误的 API 端点

4. **持续集成**: 将 Newman 测试集成到 CI/CD 管道

---

## 文件修改清单

- ✅ `src/LYBTZYZS.Server.Application/Contracts/Users/Mappers/UserMapper.cs`
- ✅ `docs/06-operations/LYBTZYZS_API_Collection.json`

---

## 参考文档

- [Newman 官方文档](https://learning.postman.com/docs/running-collections/using-newman-cli/)
- [Postman 集合格式](https://schema.getpostman.com/)
