# Patients 模块设计

> 日期: 2026-06-29
> US 数量: 13 (PRD)
> 复杂度: 6/10
> 状态: 草稿

## 模块概述

Patients 管理患者信息，支持拼音搜索、身份证读卡器集成、敏感数据加密。

**职责边界**:
- 患者的 CRUD、搜索、批量导入/导出
- 拼音首字母自动生成与搜索
- 身份证读卡器集成（去重链）
- 敏感数据标记（身份证号、手机号）

**依赖关系**:
- 上游: 无（基础模块）
- 下游: MedicalCase（患者信息）、Registration（挂号时校验）
- 无模块间直接引用

**关键 US 清单**:
PAT-001 ~ PAT-013（详见 PRD）

## 接口契约

### Service 接口（Server 端）

```csharp
public interface IPatientService
{
    Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, bool filterDisabled, CancellationToken ct);
    Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<PatientDetailDto>> CreateAsync(PatientInputDto dto, CancellationToken ct);
    Task<Result<PatientDetailDto>> UpdateAsync(Guid id, PatientInputDto dto, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<List<PatientDetailDto>>> SearchAsync(string keyword, CancellationToken ct);
    Task<Result<PatientDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct);
    Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, CancellationToken ct);
}
```

### API 端点映射

| HTTP | 路由 | 方法 | 权限 |
|------|------|------|------|
| GET | `/api/v1/patients` | GetList | DoctorOrAdmin |
| GET | `/api/v1/patients/{id}` | GetById | DoctorOrAdmin |
| POST | `/api/v1/patients` | Create | DoctorOrAdmin |
| PUT | `/api/v1/patients/{id}` | Update | DoctorOrAdmin |
| DELETE | `/api/v1/patients/{id}` | Delete | DoctorOrAdmin |
| POST | `/api/v1/patients/{id}/toggle-status` | ToggleStatus | DoctorOrAdmin |
| POST | `/api/v1/patients/batch-delete` | BatchDelete | DoctorOrAdmin |

### DTO 结构

```
PatientInputDto
├── Name: string（必填）
├── PinYinCode: string?（自动生成）
├── Gender: Gender
├── BirthDate: DateTime?
├── IdNumber: string?（Regex 验证）
├── PhoneNumber: string?
└── Id: Guid?（null=创建，有值=更新）

PatientDetailDto
├── 所有 Patient 字段
├── Age: int?（计算值）
├── Status: CommonStatus
├── CreatedAt, UpdatedAt, CreatedBy

PatientListDto
├── Id, Name, Gender, Age
├── PhoneNumber, PinYinCode
├── Status, CreatedAt
```

## 数据流

### 创建患者
```
Desktop → POST /api/v1/patients
Controller → PatientService.CreateAsync
  → FluentValidation 校验
  → 检查手机号唯一性
  → 检查身份证号唯一性
  → 自动生成 PinYinCode（PinYinHelper）
  → 创建 Patient 实体
  → 缓存失效（ICacheInvalidationService）
  → 返回 PatientDetailDto
```

### 搜索（拼音）
```
Desktop → GET /api/v1/patients?keyword=ZSH
Repository → ApplyKeywordFilter
  → Name.Contains(keyword) || PinYinCode.Contains(keyword)
  → 返回匹配结果
```

### 读卡器集成
```
Desktop → PatientCardReaderViewModel.ReadCardAsync()
  → 硬件读取身份证（HuaDa HD100 P/Invoke）
  → PatientCardReaderIntegration.MatchPatientAsync()
    → PRD-15 去重链:
      1. IdNumber 精确匹配
      2. Name + BirthDate 模糊匹配
      3. 多候选提示
      4. 无匹配→快速创建
  → 加密存储照片（IPhotoStorageService）
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| FluentValidationException | 输入校验失败 | 返回 400 + 字段级错误 |
| BusinessException | 手机号/身份证号重复 | 返回 400 |
| BusinessException | ToggleStatus 时有未完成医案 | 返回 400 |
| NotFoundException | GetById 找不到 | 返回 404 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| 拼音自动生成 | Name 变更时自动更新 PinYinCode | PAT-002 |
| 手机号唯一 | 创建/更新时校验 | PAT-003 |
| 身份证号唯一 | 创建/更新时校验 | PAT-004 |
| 敏感数据标记 | IdNumber/PhoneNumber 标记 SensitiveData | PAT-005 |
| 状态切换保护 | 有未完成医案时禁止禁用 | PAT-006 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| MedicalCase | 查询患者信息 | MedicalCase → Patients |
| Registration | 创建时校验患者存在 | Registration → Patients |

**已知问题**:
- 单删路径无引用检查（被医案引用的患者可删）
- 批量删反而有引用检查 → 单删比批删更不安全
