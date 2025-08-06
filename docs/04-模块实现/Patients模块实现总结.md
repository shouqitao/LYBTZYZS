# Patients模块实现总结

## 模块概述
Patients模块负责患者档案管理，包括患者信息的增删改查、档案管理、就诊历史跟踪、统计分析等功能。该模块是整个系统的核心数据模块之一，为挂号、就诊、处方等其他模块提供患者基础信息支撑。

## 已完成功能

### 1. 患者CRUD操作 ✅
**核心文件**:
- `PatientsController.cs` - 控制器层
- `PatientService.cs` - 服务层
- `PatientRepository.cs` - 数据访问层

**实现特点**:
- 软删除策略（患者档案只能禁用，不能物理删除）
- 身份证号和手机号唯一性验证
- 自动解析身份证号提取出生日期和年龄
- 自动生成拼音码便于快速检索

### 2. 患者查询功能 ✅
**查询方式**:
- 分页查询（支持姓名筛选）
- 按ID查询
- 按手机号查询
- 按身份证号查询
- 关键词搜索（姓名、手机号、身份证号）
- 高级搜索（多条件组合）

**权限控制**:
- 管理员可查看所有患者（包括禁用的）
- 普通用户只能查看启用的患者

### 3. 患者档案管理 ✅
**功能列表**:
- 获取患者就诊历史
- 更新患者过敏史
- 批量导入患者档案
- 导出患者档案
- 合并重复患者档案
- 患者标签管理

**批量操作特性**:
- 支持Excel/CSV格式导入
- 自动检测重复记录
- 详细的导入结果报告
- 支持按条件导出

### 4. 患者统计分析 ✅
**统计维度**:
- 患者总体统计（总数、新增、流失等）
- 年龄分布统计
- 性别分布统计
- 新增患者趋势（按月）
- 活跃度分析
- 就诊频次统计

**预警功能**:
- 流失患者预警（180天未就诊）
- 重复档案检测
- 过敏史提醒

## 数据模型

### PatientModel（数据库实体）
```csharp
public class PatientModel : BasePatientModel {
    // 基础信息
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? PinYinCode { get; set; }
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public DateTime? BirthDate { get; set; }
    
    // 证件信息
    public string? IdType { get; set; }
    public string? IdNumber { get; set; }
    
    // 联系方式
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    
    // 社会信息
    public string? Occupation { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Ethnicity { get; set; }
    public string? Education { get; set; }
    
    // 医疗信息
    public string? AllergyHistory { get; set; }
    public DateTime? LastVisitTime { get; set; }
    public int VisitCount { get; set; }
    
    // 系统字段
    public PatientStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? DisableReason { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

## API接口清单

### 基础CRUD接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/patients` | GET | 获取患者列表 |
| `/api/v1/patients/{id}` | GET | 获取患者详情 |
| `/api/v1/patients` | POST | 创建患者 |
| `/api/v1/patients/{id}` | PUT | 更新患者 |
| `/api/v1/patients/{id}` | DELETE | 删除患者（软删除） |

### 查询接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/patients/paged` | POST | 分页查询 |
| `/api/v1/patients/search` | GET | 搜索患者 |
| `/api/v1/patients/by-phone/{phone}` | GET | 按手机号查询 |
| `/api/v1/patients/by-idnumber/{idNumber}` | GET | 按身份证查询 |
| `/api/v1/patients/active` | GET | 获取活跃患者 |
| `/api/v1/patients/advanced-search` | POST | 高级搜索 |

### 档案管理接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/patients/{id}/visit-history` | GET | 就诊历史 |
| `/api/v1/patients/{id}/allergy` | PATCH | 更新过敏史 |
| `/api/v1/patients/import` | POST | 批量导入 |
| `/api/v1/patients/export` | POST | 导出档案 |
| `/api/v1/patients/merge` | POST | 合并档案 |
| `/api/v1/patients/{id}/tags` | GET | 获取标签 |
| `/api/v1/patients/{id}/tags` | PUT | 设置标签 |
| `/api/v1/patients/check-duplicate` | POST | 检查重复 |

### 统计分析接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/patients/statistics` | GET | 总体统计 |
| `/api/v1/patients/statistics/age-distribution` | GET | 年龄分布 |
| `/api/v1/patients/statistics/gender-distribution` | GET | 性别分布 |
| `/api/v1/patients/statistics/trend` | GET | 新增趋势 |
| `/api/v1/patients/recent-active` | GET | 活跃患者 |
| `/api/v1/patients/inactive` | GET | 流失患者 |
| `/api/v1/patients/today-new` | GET | 今日新增 |

## 请求/响应示例

### 创建患者请求
```json
POST /api/v1/patients
{
    "name": "张三",
    "gender": 1,
    "age": 35,
    "idNumber": "110101198801011234",
    "phoneNumber": "13800138000",
    "address": "北京市朝阳区xxx街道",
    "occupation": "工程师",
    "maritalStatus": "已婚",
    "allergyHistory": "青霉素过敏"
}
```

### 患者统计响应
```json
GET /api/v1/patients/statistics
{
    "totalPatients": 1523,
    "activePatients": 1456,
    "inactivePatients": 67,
    "newPatients": 128,
    "maleCount": 782,
    "femaleCount": 741,
    "averageAge": 42.5,
    "totalVisits": 8956,
    "averageVisits": 5.88,
    "patientsWithAllergy": 234,
    "todayNewPatients": 12,
    "monthNewPatients": 128,
    "lostPatients": 45
}
```

### 年龄分布响应
```json
GET /api/v1/patients/statistics/age-distribution
[
    {
        "ageRange": "0-18岁（儿童）",
        "minAge": 0,
        "maxAge": 18,
        "count": 156,
        "percentage": 10.24,
        "maleCount": 82,
        "femaleCount": 74
    },
    {
        "ageRange": "19-35岁（青年）",
        "minAge": 19,
        "maxAge": 35,
        "count": 423,
        "percentage": 27.77,
        "maleCount": 215,
        "femaleCount": 208
    }
]
```

## 业务特性

### 1. 身份证号智能解析
- 自动提取出生日期
- 自动计算年龄
- 身份证号格式验证
- 重复性检查

### 2. 档案合并功能
- 检测重复患者
- 合并就诊记录
- 保留历史信息
- 操作日志记录

### 3. 批量操作
- 批量导入支持Excel/CSV
- 导入时自动检测重复
- 详细的错误报告
- 支持增量更新

### 4. 统计分析
- 实时统计计算
- 多维度数据分析
- 趋势预测
- 流失预警

## 与其他模块的协作

### 1. 与Registration模块
- Registration创建挂号时查询患者信息
- Registration可创建新患者
- 更新患者最后就诊时间

### 2. 与Consultation模块
- Consultation记录就诊信息
- 更新患者就诊次数
- 记录过敏史变更

### 3. 与Prescriptions模块
- Prescriptions查询患者过敏史
- 提供患者基本信息
- 用药禁忌提醒

### 4. 与MedicalCase模块
- MedicalCase关联患者档案
- 统计就诊历史
- 生成病历档案

## 安全特性

1. **数据保护**
   - 软删除机制，保留历史数据
   - 敏感信息加密存储
   - 操作日志完整记录

2. **权限控制**
   - 基于角色的访问控制
   - 禁用患者仅管理员可见
   - 批量操作权限限制

3. **数据验证**
   - 身份证号格式验证
   - 手机号格式验证
   - 重复性检查
   - 必填字段验证

## 性能优化

1. **查询优化**
   - 拼音码索引加速搜索
   - 分页查询减少数据传输
   - 缓存常用查询结果

2. **批量处理**
   - 批量导入使用事务
   - 异步处理大数据量
   - 分批次处理避免超时

3. **统计优化**
   - 定时计算统计数据
   - 增量更新统计结果
   - 缓存统计报表

## 测试覆盖

- [x] 患者CRUD操作测试
- [x] 身份证号解析测试
- [x] 重复检测测试
- [x] 批量导入测试
- [x] 档案合并测试
- [x] 统计功能测试
- [ ] 并发操作测试
- [ ] 大数据量性能测试

## 待优化项

1. **功能增强**
   - 患者画像分析
   - 就诊预约提醒
   - 健康档案管理
   - 家庭成员关联

2. **数据分析**
   - 疾病趋势分析
   - 用药习惯分析
   - 就诊行为分析
   - 健康评估报告

3. **用户体验**
   - 快速建档功能
   - 智能搜索建议
   - 批量操作向导
   - 数据可视化展示

## 总结

Patients模块已完成所有核心功能：
- ✅ 完整的CRUD操作
- ✅ 强大的查询功能
- ✅ 全面的档案管理
- ✅ 丰富的统计分析
- ✅ 智能的数据处理
- ✅ 严格的权限控制

该模块为整个系统提供了完善的患者档案管理功能，支持从患者建档、信息维护到统计分析的全流程管理，为中医诊所的日常运营提供了坚实的数据基础。