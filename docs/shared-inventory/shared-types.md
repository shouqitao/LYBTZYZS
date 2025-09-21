# Shared 类型清单

> 生成时间：2025-09-21
> 覆盖范围：LYBT.Shared.* 全部项目
> 类型总数：268+

## 📊 项目概览

| 项目 | 类型数 | 主要职责 |
|------|--------|----------|
| LYBT.Shared.Models | 235+ | DTO、枚举、契约定义 |
| LYBT.Shared.Interfaces | 17 | 服务接口定义 |
| LYBT.Shared.Utilities | 16 | 通用工具类 |

## 🏗️ LYBT.Shared.Models

### 核心基础类

#### 通用结果类
| 类型 | 命名空间 | 用途 | 主要引用模块 |
|------|----------|------|--------------|
| ServiceResult<T> | LYBT.Shared.Models.Contracts.Common | 服务层统一返回结果 | 所有Service层 |
| ApiResponse<T> | LYBT.Shared.Models.Common | API统一响应格式 | WebAPI Controllers |
| PagedResult<T> | LYBT.Shared.Models.Contracts.Common | 分页结果包装 | 查询服务 |
| BatchResult<T> | LYBT.Shared.Models.Contracts.Common | 批量操作结果 | 批量业务服务 |

#### 异常类
| 类型 | 命名空间 | 用途 | 主要引用模块 |
|------|----------|------|--------------|
| BusinessException | LYBT.Shared.Models.Common | 业务异常基类 | 业务服务层 |
| NotFoundException | LYBT.Shared.Models.Common | 资源未找到异常 | Repository/Service |
| ValidationException | LYBT.Shared.Models.Common | 验证异常 | 验证器、服务层 |
| AuthorizationException | LYBT.Shared.Models.Common | 授权异常 | 认证模块 |

### 枚举类型

#### 系统枚举
| 类型 | 命名空间 | 用途 | 值范围 |
|------|----------|------|--------|
| CommonStatus | LYBT.Shared.Models.Enums | 通用状态 | Enabled(1), Disabled(0) |
| UserRole | LYBT.Shared.Models.Enums | 用户角色 | Admin(1), Doctor(2) |
| Gender | LYBT.Shared.Models.Enums | 性别 | Male(1), Female(2), Unknown(0) |
| AgeGroup | LYBT.Shared.Models.Enums | 年龄段 | Child, Youth, Adult, Elder |

#### 业务枚举
| 类型 | 命名空间 | 用途 | 值范围 |
|------|----------|------|--------|
| ConsultationStatus | LYBT.Shared.Models.Enums | 问诊状态 | Pending, InProgress, Completed |
| PrescriptionStatus | LYBT.Shared.Models.Enums | 处方状态 | Draft, Confirmed, Dispensed |
| PaymentStatus | LYBT.Shared.Models.Enums | 支付状态 | Pending, Paid, Refunded |
| DiagnosisMethod | LYBT.Shared.Models.Enums | 诊断方法 | 望闻问切 |

### 业务DTO类

#### Auth模块 (8个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| LoginDto | 登录请求 | Username, Password, RememberMe |
| LoginResponseDto | 登录响应 | Token, User, ExpiresIn |
| RegisterDto | 注册请求 | Username, Password, RealName |
| ChangePasswordDto | 修改密码 | OldPassword, NewPassword |
| TokenDto | Token信息 | AccessToken, RefreshToken |
| UserClaimsDto | 用户声明 | UserId, Username, Role |
| RefreshTokenDto | 刷新Token | RefreshToken |
| ValidateTokenDto | 验证Token | Token |

#### Users模块 (15个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| UserDto | 用户信息 | Id, Username, RealName, Role |
| UserCreateDto | 创建用户 | Username, Password, RealName |
| UserUpdateDto | 更新用户 | Id, RealName, PhoneNumber |
| UserSearchDto | 搜索条件 | Keyword, Role, Status |
| UserProfileDto | 用户档案 | 完整用户信息 |
| UserListDto | 列表展示 | 简化用户信息 |
| UserQueryDto | 查询参数 | 分页、排序、筛选 |
| UserImportDto | 批量导入 | Excel导入字段 |
| UserExportDto | 导出数据 | 导出字段定义 |
| UserStatisticsDto | 用户统计 | 总数、活跃数等 |

#### Patients模块 (20个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| PatientDto | 患者信息 | Id, Name, Gender, Age |
| PatientCreateDto | 创建患者 | 基本信息、联系方式 |
| PatientUpdateDto | 更新患者 | 可更新字段 |
| PatientSearchDto | 搜索条件 | 姓名、手机、就诊号 |
| PatientDetailDto | 详细信息 | 完整患者档案 |
| PatientHistoryDto | 就诊历史 | 历史记录列表 |
| PatientImportDto | 批量导入 | Excel导入格式 |
| MedicalHistoryDto | 病史信息 | 既往史、现病史 |
| AllergyHistoryDto | 过敏史 | 过敏源、反应 |
| FamilyHistoryDto | 家族史 | 遗传病史 |
| PatientStatisticsDto | 患者统计 | 总数、新增数等 |

#### MedicalCase模块 (15个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| MedicalCaseDto | 病例信息 | Id, PatientId, DoctorId |
| MedicalCaseCreateDto | 创建病例 | PatientId, ChiefComplaint |
| MedicalCaseUpdateDto | 更新病例 | 诊断、处方 |
| MedicalCaseSearchDto | 搜索条件 | 患者、日期范围 |
| MedicalCaseDetailDto | 详细信息 | 完整病例 |
| MedicalCaseListDto | 列表展示 | 简化病例信息 |
| MedicalCaseStatisticsDto | 病例统计 | 总数、分布 |
| ChiefComplaintDto | 主诉 | 症状描述 |
| DiagnosisDto | 诊断结果 | 中医诊断、西医诊断 |
| TreatmentPlanDto | 治疗方案 | 治则治法 |

#### Consultation模块 (25个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| ConsultationDto | 问诊记录 | Id, MedicalCaseId |
| ConsultationCreateDto | 创建问诊 | 四诊信息 |
| ConsultationUpdateDto | 更新问诊 | 修改诊断 |
| InspectionDto | 望诊 | 面色、舌象等 |
| AuscultationDto | 闻诊 | 声音、气味 |
| InquiryDto | 问诊 | 症状、病史 |
| PalpationDto | 切诊 | 脉象、腹诊 |
| TongueDto | 舌诊 | 舌质、舌苔 |
| PulseDto | 脉诊 | 脉象特征 |
| SyndromeDto | 证型 | 辨证结果 |
| TCMDiagnosisDto | 中医诊断 | 病名、证型 |
| WesternDiagnosisDto | 西医诊断 | ICD编码 |
| SymptomDto | 症状 | 症状描述 |
| ConsultationTemplateDto | 问诊模板 | 模板内容 |
| FourDiagnosticDto | 四诊合参 | 综合诊断 |

#### Prescriptions模块 (20个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| PrescriptionDto | 处方信息 | Id, MedicalCaseId |
| PrescriptionCreateDto | 创建处方 | 药物列表 |
| PrescriptionUpdateDto | 更新处方 | 调整剂量 |
| PrescriptionItemDto | 处方项 | 药物、剂量 |
| HerbDosageDto | 药物剂量 | 药物ID、用量 |
| PrescriptionTemplateDto | 处方模板 | 常用处方 |
| CompatibilityCheckDto | 配伍检查 | 药物相互作用 |
| DosageCalculationDto | 剂量计算 | 年龄、体重调整 |
| PrescriptionStatisticsDto | 处方统计 | 用药频次 |
| DispensingRecordDto | 发药记录 | 发药信息 |
| PrescriptionReviewDto | 处方审核 | 审核意见 |
| RefillRequestDto | 续方申请 | 续方信息 |

#### Herbs模块 (15个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| HerbDto | 药材信息 | Id, Name, Category |
| HerbCreateDto | 创建药材 | 基本信息 |
| HerbUpdateDto | 更新药材 | 可更新字段 |
| HerbSearchDto | 搜索条件 | 名称、类别 |
| HerbCategoryDto | 药材分类 | 分类信息 |
| HerbPropertyDto | 药性 | 性味归经 |
| HerbFunctionDto | 功效 | 主治功能 |
| HerbContraindicationDto | 禁忌 | 配伍禁忌 |
| HerbDosageRangeDto | 用量范围 | 常用剂量 |
| HerbAliasDto | 别名 | 药材别名 |
| HerbImportDto | 批量导入 | 导入格式 |

#### Formula模块 (12个)
| 类型 | 用途 | 主要字段 |
|------|------|----------|
| FormulaDto | 方剂信息 | Id, Name, Source |
| FormulaCreateDto | 创建方剂 | 组成、功效 |
| FormulaUpdateDto | 更新方剂 | 可更新字段 |
| FormulaSearchDto | 搜索条件 | 名称、来源 |
| FormulaCompositionDto | 方剂组成 | 药物配比 |
| FormulaFunctionDto | 方剂功效 | 主治病症 |
| FormulaCategoryDto | 方剂分类 | 类别信息 |
| FormulaTemplateDto | 方剂模板 | 经典方 |
| FormulaModificationDto | 加减方 | 方剂加减 |
| FormulaStatisticsDto | 方剂统计 | 使用频次 |

## 🔌 LYBT.Shared.Interfaces

### 服务接口定义 (17个)

| 接口 | 用途 | 主要方法 | 实现模块 |
|------|------|----------|----------|
| IAuthService | 认证服务 | Login, Logout, Refresh | Auth模块 |
| IUserService | 用户服务 | CRUD, Search, Profile | Users模块 |
| IPatientService | 患者服务 | CRUD, History, Statistics | Patients模块 |
| IMedicalCaseService | 病例服务 | CRUD, Search, Archive | MedicalCase模块 |
| IConsultationService | 问诊服务 | CRUD, Diagnosis, Template | Consultation模块 |
| IPrescriptionService | 处方服务 | CRUD, Check, Dispense | Prescriptions模块 |
| IHerbService | 药材服务 | CRUD, Search, Compatibility | Herbs模块 |
| IFormulaService | 方剂服务 | CRUD, Template, Modify | Formula模块 |
| IStatisticsService | 统计服务 | Reports, Analytics | 统计模块 |
| IExportService | 导出服务 | Excel, PDF, CSV | 导出模块 |
| IImportService | 导入服务 | Excel, CSV | 导入模块 |
| IValidationService | 验证服务 | Validate, Rules | 验证模块 |
| ICacheService | 缓存服务 | Get, Set, Remove | 缓存模块 |
| IConfigurationService | 配置服务 | Get, Set, Reload | 配置模块 |
| ILogService | 日志服务 | Log, Query | 日志模块 |
| IAuditService | 审计服务 | Audit, Trail | 审计模块 |
| INotificationService | 通知服务 | Send, Queue | 通知模块 |

## 🛠️ LYBT.Shared.Utilities

### 工具类 (16个)

| 类型 | 命名空间 | 用途 | 主要方法 |
|------|----------|------|----------|
| PasswordHelper | Helpers | 密码处理 | Hash, Verify |
| ValidationHelper | Helpers | 数据验证 | Validate, Rules |
| ExcelHelper | Helpers | Excel操作 | Import, Export |
| JsonHelper | Helpers | JSON处理 | Serialize, Deserialize |
| DateTimeHelper | Helpers | 日期处理 | Format, Calculate |
| StringHelper | Extensions | 字符串扩展 | Format, Convert |
| EnumHelper | Extensions | 枚举扩展 | ToList, GetDescription |
| CollectionHelper | Extensions | 集合扩展 | Page, Filter |
| ExpressionHelper | Extensions | 表达式扩展 | Build, Combine |
| ReflectionHelper | Helpers | 反射工具 | GetProperties, Invoke |
| CryptoHelper | Security | 加密解密 | Encrypt, Decrypt |
| ClaimsHelper | Security | 声明处理 | GetClaim, SetClaim |
| HttpContextHelper | Web | HTTP上下文 | GetHeader, GetIP |
| ConfigHelper | Configuration | 配置读取 | GetValue, GetSection |
| LogHelper | Logging | 日志工具 | Log, Error |
| CacheHelper | Caching | 缓存工具 | Get, Set, Remove |

## 📈 统计分析

### 类型分布

| 类别 | 数量 | 占比 |
|------|------|------|
| DTO类 | 200+ | 74.6% |
| 枚举类 | 35 | 13.1% |
| 接口定义 | 17 | 6.3% |
| 工具类 | 16 | 6.0% |

### 模块使用频率

| 模块 | DTO数量 | 复杂度 |
|------|---------|---------|
| Consultation | 25 | 高 |
| Patients | 20 | 高 |
| Prescriptions | 20 | 高 |
| MedicalCase | 15 | 中 |
| Users | 15 | 中 |
| Herbs | 15 | 中 |
| Formula | 12 | 中 |
| Auth | 8 | 低 |

## 📝 维护建议

1. **命名规范**：保持DTO后缀统一，接口以I开头
2. **分类清晰**：按模块组织DTO，避免交叉引用
3. **文档完善**：为复杂DTO添加XML注释
4. **版本控制**：重要变更需要版本标记
5. **定期清理**：移除未使用的类型定义

---

*此文档由自动扫描生成，建议定期更新以保持与代码同步*