# UltraThink业务模块功能精简决策记录
> 生成时间：2025-01-31
> 项目定位：20人以下中小型中医诊所管理系统
> 精简原则：实用主义，避免过度设计

## 📊 精简概况

- **精简前**：8个模块，223个功能
- **精简后**：8个模块，121个功能  
- **精简比例**：46%功能被移除
- **核心原则**：保留核心业务功能，删除统计分析、复杂工作流

## 🔐 Auth模块精简决策（10→7个功能，-30%）

### ✅ 保留功能（7个）
1. **LoginAsync** - 用户登录
2. **LogoutAsync** - 用户登出  
3. **ChangePasswordAsync** - 用户修改密码
4. **ResetPasswordAsync** - 管理员重置密码（重置为默认密码）
5. **RefreshTokenAsync** - 刷新令牌
6. **ValidateTokenAsync** - 验证令牌
7. **HandleSuccessfulLoginAsync** - 内部方法，处理登录成功逻辑

### ❌ 删除功能（3个）
1. **GetOperatorName** - 与AuthLoggingHelper重复
2. **GetLoginSettingsAsync** - 不需要配置记住密码功能
3. **SetLoginSettingsAsync** - 不需要配置记住密码功能

### 💡 重要说明
- 操作日志功能保留在AuthLoggingHelper中
- 密码策略：用户自主修改密码，管理员只能重置为默认密码

## 👥 Users模块精简决策（27→21个功能，-22%）

### ✅ 保留功能（21个）
1. **GetAllAsync** - 获取用户列表
2. **GetByIdAsync** - 获取用户详情
3. **CreateAsync** - 创建用户
4. **UpdateAsync** - 更新用户信息
5. **DeleteAsync** - 删除用户（软删除）
6. **UpdateStatusAsync** - 启用/禁用用户
7. **CheckDuplicateAsync** - 检查用户名重复
8. **GetByUsernameAsync** - 根据用户名查询
9. **UpdatePasswordAsync** - 更新密码
10. **UpdateLastLoginTimeAsync** - 更新最后登录时间
11. **ValidateUserCredentialsAsync** - 验证用户凭据
12. **SearchAsync** - 搜索用户
13. **GetPagedAsync** - 分页查询
14. **GetActiveUsersAsync** - 获取活跃用户
15. **BatchUpdateStatusAsync** - 批量启用/禁用
16. **GetUsersByRoleAsync** - 按角色查询用户
17. **UpdateUserRoleAsync** - 更新用户角色
18. **GetRolesAsync** - 获取所有角色
19. **CreateRoleAsync** - 创建角色
20. **UpdateRoleAsync** - 更新角色
21. **DeleteRoleAsync** - 删除角色

### ❌ 删除功能（6个）
1. **GetUserStatisticsAsync** - 用户统计
2. **GetUserActivityReportAsync** - 活动报告
3. **GetDepartmentsAsync** - 科室管理
4. **AssignUserToDepartmentAsync** - 分配科室
5. **GetUserScheduleAsync** - 排班管理
6. **UpdateUserScheduleAsync** - 更新排班

### 💡 重要说明
- 角色管理功能保留，支持后续扩展
- 删除所有统计分析功能
- 不需要科室和排班管理

## 🏥 Patients模块精简决策（43→15个功能，-65%）

### ✅ 保留功能（15个）
1. **GetAllAsync** - 获取患者列表
2. **GetByIdAsync** - 获取患者详情
3. **CreateAsync** - 创建患者档案
4. **UpdateAsync** - 更新患者信息
5. **DeleteAsync** - 删除患者（软删除）
6. **UpdateStatusAsync** - 启用/禁用患者
7. **SearchAsync** - 患者搜索（姓名、拼音码、电话、身份证）
8. **GetPagedAsync** - 分页查询
9. **CheckDuplicateAsync** - 重复患者检查
10. **ImportPatientsAsync** - 批量导入（从旧系统迁移）
11. **ExportPatientsAsync** - 批量导出
12. **GetByPhoneAsync** - 电话号码查询
13. **GetByIdCardAsync** - 身份证查询
14. **QuickSearchAsync** - 快速检索
15. **ValidatePatientDataAsync** - 数据验证

### ❌ 删除功能（28个）
- 所有标签管理功能（10个）
- 所有档案管理功能（8个）
- 所有统计分析功能（10个）

### 💡 重要说明
- 导入导出功能用于旧系统数据迁移
- 搜索支持：姓名、拼音码、电话、身份证
- 不需要标签和档案管理功能

## 🩺 Consultation模块精简决策（16→13个功能，-19%）

### ✅ 保留功能（13个）
1. **GetAllAsync** - 获取看诊记录列表
2. **GetByIdAsync** - 获取看诊详情
3. **CreateAsync** - 创建看诊记录
4. **UpdateAsync** - 更新看诊信息
5. **DeleteAsync** - 删除看诊记录（软删除）
6. **GetByPatientIdAsync** - 获取患者看诊记录
7. **GetByDoctorIdAsync** - 获取医生看诊记录
8. **GetTodayConsultationsAsync** - 今日看诊列表
9. **UpdateDiagnosisAsync** - 更新诊断结果
10. **GetPatientHistoryAsync** - 获取患者历史诊断记录
11. **SearchConsultationsAsync** - 搜索看诊记录
12. **GetPagedAsync** - 分页查询
13. **UpdateStatusAsync** - 更新看诊状态

### ❌ 删除功能（3个）
1. **GetConsultationStatisticsAsync** - 看诊统计
2. **GetDoctorPerformanceAsync** - 医生绩效统计
3. **GenerateConsultationReportAsync** - 生成统计报告

### 💡 重要说明
- 保留历史诊断记录查询功能
- 管理员可查看所有业务模块的台账记录
- 不需要统计分析功能

## 📋 MedicalCase模块精简决策（21→11个功能，-48%）

### ✅ 保留功能（11个）
1. **GetAllAsync** - 获取案例列表
2. **GetByIdAsync** - 获取案例详情
3. **CreateAsync** - 创建医疗案例
4. **UpdateAsync** - 更新案例信息
5. **DeleteAsync** - 删除案例（软删除）
6. **GetByPatientIdAsync** - 获取患者案例
7. **CompleteAsync** - 完成案例（归档）
8. **SuspendAsync** - 暂停案例（中途离开/急诊）
9. **SearchAsync** - 搜索案例（按患者姓名）
10. **GetPagedAsync** - 分页查询
11. **PrintMedicalRecordAsync** - 打印病历/处方（新增）

### ❌ 删除功能（10个）
1. **CloneAsync** - 克隆案例
2. **BatchDeleteAsync** - 批量删除
3. **GetStatisticsAsync** - 案例统计
4. **ExportAsync** - 导出案例
5. **GetByDateRangeAsync** - 日期范围查询
6. **GetByDoctorIdAsync** - 医生案例查询
7. **GetTemplatesAsync** - 案例模板
8. **CreateFromTemplateAsync** - 从模板创建
9. **ShareAsync** - 分享案例
10. **GetSharedCasesAsync** - 获取分享案例

### 💡 重要说明
- 一次就诊对应一个案例
- 完成即归档，支持暂停功能（急诊情况）
- 打印功能从Prescriptions模块迁移至此
- 打印内容包括：诊断结果、处方组成、费用等

## 💊 Prescriptions模块精简决策（25→10个功能，-60%）

### ✅ 保留功能（10个）
1. **GetAllAsync** - 获取处方列表（分页）
2. **GetByIdAsync** - 获取处方详情
3. **CreateAsync** - 创建处方
4. **UpdateAsync** - 更新处方
5. **DeleteAsync** - 删除处方（软删除）
6. **GetByPatientIdAsync** - 获取患者历史处方
7. **GetRecentPrescriptionsAsync** - 获取最近处方（快速查询）
8. **CopyPrescriptionAsync** - 导入历史处方（复制后调整）
9. **SearchAsync** - 搜索处方
10. **GetPagedAsync** - 分页查询

### ❌ 删除功能（15个）
1. **审批相关功能**（3个）
2. **统计分析功能**（4个）
3. **分享功能**（2个）
4. **打印功能**（移至MedicalCase）
5. **无分页查询功能**
6. **其他复杂功能**（4个）

### 💡 架构调整
- **适应症自动关联**：从Consultation.Diagnosis字段自动获取
- **处方编辑逻辑**：前端ViewModel层处理
  - 重复药材检测
  - 剂量自动计算
  - 配伍禁忌检查
- **打印功能迁移**：移至MedicalCase.PrintMedicalRecordAsync

## 🌿 Herbs模块精简决策（40→18个功能，-55%）

### ✅ 保留功能（18个）
1. **GetAllAsync** - 获取药材列表
2. **GetByIdAsync** - 获取药材详情
3. **CreateAsync** - 创建药材
4. **UpdateAsync** - 更新药材信息
5. **DeleteAsync** - 删除药材（软删除）
6. **UpdateStatusAsync** - 启用/禁用药材
7. **BatchUpdateStatusAsync** - 批量启用/禁用
8. **UpdatePriceAsync** - 更新单个价格
9. **BatchUpdatePricesAsync** - 批量更新价格
10. **ImportHerbsAsync** - 导入药材数据
11. **ExportHerbsAsync** - 导出药材数据
12. **SearchAsync** - 搜索药材
13. **GetPagedAsync** - 分页查询
14. **CheckDuplicateAsync** - 检查重复
15. **GetByNameAsync** - 按名称查询
16. **GetByPinyinAsync** - 按拼音查询
17. **GetActiveherbsAsync** - 获取启用的药材
18. **ValidateHerbDataAsync** - 数据验证

### ❌ 删除功能（22个）
- 所有统计分析功能（8个）
- 分类管理功能（6个）
- 供应商管理功能（4个）
- 库存管理功能（4个）

### 💡 重要说明
- 批量价格更新用于统一调价
- 导入导出用于数据迁移和备份
- 价格使用最新价格即可
- 不涉及库存管理

## 📜 Formula模块精简决策（31→16个功能，-48%）

### ✅ 保留功能（16个）
1. **GetAllAsync** - 获取验方列表
2. **GetByIdAsync** - 获取验方详情
3. **CreateAsync** - 创建验方
4. **UpdateAsync** - 更新验方
5. **DeleteAsync** - 删除验方（软删除）
6. **UpdateStatusAsync** - 启用/禁用验方
7. **ShareFormulaAsync** - 分享验方（医生间共享）
8. **GetSharedFormulasAsync** - 获取分享的验方
9. **CopyFormulaAsync** - 复制验方（创建个人副本）
10. **ImportFormulasAsync** - 导入验方
11. **ExportFormulasAsync** - 导出验方
12. **SearchAsync** - 搜索验方
13. **GetPagedAsync** - 分页查询
14. **GetByDoctorIdAsync** - 获取医生的验方
15. **GetClassicFormulasAsync** - 获取经典验方
16. **ValidateFormulaAsync** - 验证验方数据

### ❌ 删除功能（15个）
1. **推荐相关功能**（3个）
2. **统计分析功能**（5个）
3. **分类管理功能**（4个）
4. **评价功能**（3个）

### 💡 重要说明
- 分享功能用于医生间经验交流
- 复制功能用于基于现有验方创建个人版本
- 导入导出用于验方库管理

## 🏗️ 架构层面调整

### 后端服务层
1. **删除所有统计Service方法**
2. **简化Repository层查询逻辑**
3. **移除复杂的业务规则引擎**
4. **保持简单的CRUD + 基础业务逻辑**

### 前端ViewModel层  
1. **处方编辑逻辑前移**
   - 药材重复检测
   - 剂量自动计算
   - 配伍禁忌提示
2. **数据展示逻辑**
   - 列表过滤
   - 本地搜索
   - 临时状态管理

### API层
1. **简化控制器方法**
2. **移除统计相关端点**
3. **保持RESTful风格**

### 数据库层
1. **保留核心表结构**
2. **删除统计相关表**
3. **简化关联关系**

## 📈 精简效果总结

| 模块 | 原功能数 | 新功能数 | 精简比例 | 重点变化 |
|------|---------|---------|----------|----------|
| Auth | 10 | 7 | -30% | 删除重复功能 |
| Users | 27 | 21 | -22% | 删除统计、科室、排班 |
| Patients | 43 | 15 | -65% | 删除标签、档案、统计 |
| Consultation | 16 | 13 | -19% | 删除统计分析 |
| MedicalCase | 21 | 11 | -48% | 新增打印功能 |
| Prescriptions | 25 | 10 | -60% | 逻辑前移至ViewModel |
| Herbs | 40 | 18 | -55% | 删除库存、供应商管理 |
| Formula | 31 | 16 | -48% | 删除推荐、评价功能 |
| **总计** | **223** | **121** | **-46%** | **大幅简化** |

## 🎯 下一步行动

1. **更新API文档** - 反映精简后的接口
2. **调整数据库Schema** - 删除不需要的表和字段
3. **重构服务层代码** - 移除相关方法
4. **更新前端界面** - 隐藏或删除相关功能
5. **更新用户手册** - 反映新的功能集

---
*本文档记录了2025年1月31日的功能精简决策，作为项目重构的指导文件。*