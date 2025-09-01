# 功能需求规格说明

**最后更新**: 2025-09-01  
**文档性质**: 需求文档 (始终保持最新)  
**对应版本**: v1.0

---

## 📋 功能需求总览

### 需求分类统计
| 类别 | 已实现 | 已移除 | 计划中 | 总计 |
|------|--------|--------|--------|------|
| **用户管理** | 8个 | 3个 | 2个 | 13个 |
| **患者管理** | 6个 | 4个 | 3个 | 13个 |
| **诊疗流程** | 12个 | 2个 | 4个 | 18个 |
| **处方管理** | 10个 | 1个 | 2个 | 13个 |
| **药材管理** | 5个 | 0个 | 1个 | 6个 |
| **验方管理** | 6个 | 0个 | 2个 | 8个 |
| **系统管理** | 7个 | 5个 | 1个 | 13个 |
| **总计** | **54个** | **15个** | **15个** | **84个** |

---

## 1️⃣ 用户管理模块 (Users)

### 1.1 已实现功能 ✅

#### FR-U001: 完整用户账户管理系统
- **描述**: 医生和管理员账户的全生命周期管理
- **功能点**:
  - **基础CRUD**: 创建/更新/删除用户账户 (CreateAsync/UpdateAsync/DeleteAsync)
  - **账户查询**: 多种查询方式支持
    - 分页查询用户列表 (GetPagedAsync)
    - 根据ID精确查询 (GetByIdAsync)  
    - 根据用户名查询 (GetByUsernameAsync)
    - 获取活跃用户列表 (GetActiveUsersAsync)
  - **高级搜索**: 多条件用户搜索 (SearchAsync)
  - **用户名验证**: 用户名唯一性验证 (ValidateUsernameAsync)

#### FR-U002: 角色权限管理系统
- **描述**: 完整的基于角色的访问控制 (RBAC)
- **支持角色**:
  - **Doctor** (医生): 患者管理、诊疗记录、处方管理
  - **Admin** (管理员): 全部功能 + 用户管理
- **权限功能**:
  - **角色查询**: 获取用户角色信息 (GetRolesAsync)
  - **接口控制**: Controller/Action级别权限验证
  - **医疗特化**: 医疗专用角色功能

#### FR-U003: 全面密码安全管理
- **描述**: 用户密码的完整安全策略和管理
- **功能点**:
  - **密码策略**: 密码复杂度要求 (大小写+数字+特殊字符)
  - **安全存储**: 密码哈希存储 (AspNetCore Identity标准)
  - **密码修改**: 用户主动修改密码 (ChangePasswordAsync)
  - **密码重置**: 管理员重置用户密码 (ResetPasswordAsync)
  - **个人资料**: 用户个人信息修改 (ChangeProfileAsync)

#### FR-U004: 用户状态控制系统
- **描述**: 用户账户状态的全面管理
- **状态类型**:
  - Active (正常): 可正常登录使用
  - Inactive (停用): 暂时禁止登录
  - Locked (锁定): 因安全原因锁定
- **状态管理**:
  - **单用户控制**: 启用/禁用单个用户 (EnableAsync/DisableAsync)
  - **批量控制**: 批量启用/禁用用户 (BatchEnableAsync/BatchDisableAsync)

#### FR-U005: 医疗专用功能
- **描述**: 针对医疗场景的专业化用户管理功能
- **功能点**:
  - **医生管理**: 获取医生用户列表 (GetDoctorsAsync)
  - **可用性检查**: 检查医生工作可用性 (IsDoctorAvailableAsync)
  - **业务集成**: 与诊疗流程的深度集成

#### FR-U006: 用户操作审计系统
- **描述**: 用户操作的审计和日志记录
- **功能点**:
  - **操作日志**: 详细的用户操作记录 (GetOperationLogsAsync)
  - **审计追踪**: 用户行为的完整审计链路
  - **安全监控**: 异常操作检测和记录

---

## 2️⃣ 患者管理模块 (Patients)

### 已实现功能 ✅

#### FR-P001: 完整患者档案管理系统
- **描述**: 患者档案的全生命周期管理
- **功能点**:
  - **基础CRUD**: 创建/更新/删除患者档案 (CreateAsync/UpdateAsync/DeleteAsync)
  - **档案查询**: 根据ID获取患者详细信息 (GetByIdAsync)
  - **批量查询**: 获取所有患者或分页查询 (GetAllAsync/GetPagedAsync)
  - **状态管理**: 患者档案启用/禁用控制 (EnableAsync/DisableAsync/SetStatusAsync)
  - **活跃患者**: 获取活跃状态患者列表 (GetActivePatientsAsync)

#### FR-P002: 多维度患者搜索系统
- **描述**: 强大的患者查找和搜索功能
- **搜索方式**:
  - **基础搜索**: 通用条件搜索 (SearchAsync)
  - **高级搜索**: 复合条件高级搜索 (AdvancedSearchAsync)
  - **身份证查找**: 根据身份证号精确查找 (GetByIdCardAsync/GetByIDNumberAsync)
  - **电话查找**: 多种电话号码查找方式 (GetByPhoneAsync/GetByPhoneNumberAsync)

#### FR-P003: 完整数据导入导出系统
- **描述**: 患者数据的批量处理和交换
- **功能点**:
  - **数据导入**: Excel文件批量导入患者信息 (ImportPatientsAsync)
  - **数据导出**: 患者数据导出为Excel/CSV格式 (ExportPatientsAsync)
  - **导入模板**: 标准化导入模板获取 (GetImportTemplateAsync)
  - **数据验证**: 导入数据的完整性验证 (ValidatePatientAsync)

#### FR-P004: 患者数据质量管理
- **描述**: 确保患者数据的准确性和唯一性
- **功能点**:
  - **重复检测**: 患者信息重复检查 (CheckDuplicatePatientsAsync)
  - **数据验证**: 患者信息格式和完整性验证 (ValidatePatientAsync)
  - **数据清理**: 无效和重复数据的识别和处理

---

## 3️⃣ 医疗案例模块 (MedicalCase)

### 已实现功能 ✅

#### FR-M001: 完整医案生命周期管理
- **描述**: 医案从创建到完成的全生命周期管理
- **功能点**:
  - **基础CRUD**: 创建/更新/删除医案 (CreateAsync/UpdateAsync/DeleteAsync)
  - **医案查询**: 根据ID获取医案详情 (GetByIdAsync)
  - **分页查询**: 医案列表分页显示 (GetPagedAsync)
  - **患者关联**: 获取患者相关医案 (GetByPatientIdAsync/GetActiveByPatientIdAsync)

#### FR-M002: 高级医案状态管理系统
- **描述**: 医案在诊疗流程中的完整状态控制
- **状态操作**:
  - **完成医案**: 标记医案诊疗完成 (CompleteAsync)
  - **暂停诊疗**: 暂时中断诊疗流程 (SuspendAsync)
  - **恢复诊疗**: 恢复被暂停的诊疗 (ResumeAsync)  
  - **取消诊疗**: 取消当前诊疗过程 (CancelConsultationAsync)
  - **状态更新**: 单个和批量状态更新 (UpdateStatusAsync/BatchUpdateStatusAsync)

#### FR-M003: 医案查询与搜索系统
- **描述**: 多维度医案查询和搜索功能
- **查询功能**:
  - **基础搜索**: 通用条件搜索 (SearchAsync)
  - **历史查询**: 医案历史记录查询 (GetHistoryAsync)
  - **活跃检查**: 检查患者是否有活跃医案 (HasActiveCaseAsync)

#### FR-M004: 医案归档管理系统
- **描述**: 医案的长期保存和归档管理
- **功能点**:
  - **医案归档**: 完成医案的正式归档 (ArchiveAsync)
  - **归档管理**: 归档医案的分类和管理
  - **历史保存**: 医案数据的长期保存和检索

#### FR-M005: 医案统计分析系统
- **描述**: 医案数据的统计分析和报表生成
- **功能点**:
  - **统计分析**: 医案数量、趋势统计 (GetStatisticsAsync)
  - **数据洞察**: 诊疗效率和模式分析
  - **报表生成**: 统计数据的可视化展示

#### FR-M006: 医疗记录打印系统
- **描述**: 医案和诊疗记录的打印输出
- **功能点**:
  - **记录打印**: 完整医疗记录打印 (PrintMedicalRecordAsync)
  - **格式化输出**: 标准医疗记录格式
  - **打印管理**: 打印任务的管理和追踪

---

## 4️⃣ 看诊诊断模块 (Consultation)

### 已实现功能 ✅

#### FR-C001: 完整诊断记录管理系统
- **描述**: 诊断记录的全生命周期管理
- **功能点**:
  - **诊断查询**: 根据ID获取详细诊断记录 (GetByIdAsync)
  - **分页查询**: 诊断记录列表分页显示 (GetPagedAsync)
  - **多维查询**: 按患者/医生/医案查询 (GetByPatientIdAsync/GetByDoctorIdAsync/GetByMedicalCaseIdAsync)
  - **记录更新**: 更新诊断信息 (UpdateAsync)
  - **记录删除**: 删除诊断记录 (DeleteAsync)

#### FR-C002: 中医四诊专业记录系统
- **描述**: 传统中医四诊方法的完整数字化记录
- **四诊功能**:
  - **四诊获取**: 获取医案的四诊记录 (GetFourDiagnosisByMedicalCaseIdAsync)
  - **四诊保存**: 保存完整四诊数据 (SaveFourDiagnosisAsync)
  - **四诊内容**: 
    - **望诊**: 面色、神态、舌诊、体态观察记录
    - **闻诊**: 声音、气味、呼吸等听觉嗅觉记录
    - **问诊**: 主诉、现病史、既往史、家族史询问记录
    - **切诊**: 脉诊、按诊等触诊内容记录

#### FR-C003: 诊断流程管理系统
- **描述**: 诊断过程的工作流程管理
- **流程功能**:
  - **开始诊断**: 启动诊断流程 (StartAsync)
  - **工作流验证**: 验证诊断工作流状态 (ValidateWorkflowStateAsync)
  - **流程状态**: 诊断流程状态跟踪和管理

#### FR-C004: 诊断搜索与历史系统
- **描述**: 诊断记录的搜索和历史追踪功能
- **功能点**:
  - **诊断搜索**: 多条件搜索诊断记录 (SearchAsync)
  - **患者历史**: 获取患者完整诊断历史 (GetPatientHistoryAsync)
  - **历史追踪**: 诊断变化和演进过程记录

#### FR-C005: 诊断统计分析系统
- **描述**: 诊断数据的统计分析和报表功能
- **功能点**:
  - **统计分析**: 诊断数量、趋势统计 (GetStatisticsAsync)
  - **数据洞察**: 诊断模式和疾病分布分析
  - **医疗质量**: 诊断效果和医生诊断质量分析

---

## 5️⃣ 处方管理模块 (Prescriptions)

### 已实现功能 ✅

#### FR-PR001: 完整处方管理系统
- **描述**: 中药处方的全生命周期管理
- **功能点**:
  - **基础CRUD**: 创建/更新/删除处方 (CreateAsync/UpdateAsync/DeleteAsync)
  - **处方查询**: 根据ID获取处方详情 (GetByIdAsync)
  - **分页查询**: 处方列表分页显示 (GetPagedAsync)
  - **多维查询**: 按患者/医案查询 (GetByPatientIdAsync/GetByMedicalCaseIdAsync)
  - **批量查询**: 获取所有处方 (GetAllAsync)

#### FR-PR002: 智能处方系统 (IntelligentPrescriptionService)
- **描述**: 基于AI的智能处方管理和推荐系统
- **智能功能**:
  - **智能组方**: 从验方智能组合处方 (ComposeFromFormulasAsync)
  - **智能检测**: 重复药材智能检测 (DetectDuplicateHerbs)
  - **智能计算**: 处方价格智能计算 (CalculatePrescriptionPrice)
  - **智能配伍**: 药材配伍禁忌检查和安全提醒

#### FR-PR003: 高级处方操作系统
- **描述**: 处方的高级操作和管理功能
- **功能点**:
  - **处方复制**: 复制现有处方 (CopyAsync)
  - **历史复制**: 复制患者上次处方 (CopyLastPrescriptionAsync)  
  - **模板创建**: 从模板快速创建处方 (CreateFromTemplateAsync)
  - **快速保存**: 处方快速保存功能 (QuickSaveAsync)
  - **处方取消**: 取消处方操作 (CancelAsync)

#### FR-PR004: 处方搜索与查询系统
- **描述**: 强大的处方搜索和专业查询功能
- **查询功能**:
  - **基础搜索**: 多条件处方搜索 (SearchAsync)
  - **医生查询**: 获取医生今日处方列表 (GetDoctorTodayPrescriptionsAsync)
  - **患者历史**: 按患者查看完整处方历史
  - **医案关联**: 根据医案获取相关处方

#### FR-PR005: 处方验证与质量控制系统
- **描述**: 处方的完整性验证和质量管理
- **验证功能**:
  - **处方验证**: 完整的处方验证系统 (ValidateAsync)
  - **配伍检查**: 药材配伍禁忌验证 (十八反十九畏)
  - **用量检查**: 药材用量合理性验证
  - **安全提醒**: 配伍冲突警告和安全建议

#### FR-PR006: 处方模板与验方应用系统
- **描述**: 验方模板的应用和个性化调整
- **模板功能**:
  - **验方应用**: 经典验方一键应用到处方
  - **模板管理**: 个人常用处方模板创建和管理
  - **智能调整**: 基于患者情况的验方个性化调整
  - **快速开方**: 基于模板的快速处方开具

---

## 6️⃣ 中药材管理模块 (Herbs)

### 已实现功能 ✅

#### FR-H001: 完整药材信息管理系统
- **描述**: 基于UltraThink双层架构的完整药材管理体系
- **功能点**:
  - **基础CRUD**: 药材信息的创建、查询、更新、软删除操作 (CreateAsync/GetByIdAsync/UpdateAsync/DeleteAsync)
  - **智能拼音码**: 自动生成拼音码，支持中文字符转换 (GenerateSimplePinyinCode)
  - **状态管理**: 药材启用/禁用状态控制，软删除机制 (SetStatusAsync)
  - **数据验证**: 完整的输入验证和业务规则检查 (ValidateCreateDto/ValidateImportDto)
  - **引用检查**: 删除前检查是否被处方引用，防止数据完整性破坏 (SoftDeleteAsync)
- **技术实现**: HerbService + HerbQueryService + HerbBusinessService双层架构
- **API接口**: 完整的RESTful API，符合统一响应格式标准

#### FR-H002: 高级查询和搜索系统
- **描述**: 多维度、智能化的药材查询检索功能
- **功能点**:
  - **分页查询**: 支持关键词搜索、价格范围筛选、状态筛选的分页查询 (GetPagedAsync)
  - **智能搜索**: 同时搜索药材名称和拼音码，智能匹配优先级排序 (SearchAsync)
  - **精确查询**: 根据药材名称精确匹配查询 (GetByNameAsync)
  - **批量查询**: 通过ID列表批量获取药材信息 (GetByIdsAsync)
  - **价格区间**: 支持最小价格和最大价格范围查询 (GetByPriceRangeAsync)
  - **可用药材**: 快速获取启用状态的药材列表 (GetAvailableHerbsAsync)
  - **热门药材**: 获取常用药材列表 (GetPopularHerbsAsync)
- **搜索优化**: 以关键词开头的结果优先显示，支持拼音码匹配
- **API实现**: GET /api/v1/herbs, GET /api/v1/herbs/search

#### FR-H003: 药材分类和目录管理
- **描述**: 基于功效的自动分类系统和预设分类管理
- **功能点**:
  - **自动分类**: 从药材功效字段自动提取和生成分类
  - **预设分类**: 中医传统分类预设（清热类、补益类、解表类、理气类、活血类、止血类、化痰类、消食类、其他）
  - **动态分类**: 根据实际药材数据动态生成分类列表
  - **分类统计**: 每个分类下的药材数量统计
- **API接口**: GET /api/v1/herbs/categories

#### FR-H004: 批量导入导出系统
- **描述**: 完整的药材数据批量处理功能，支持Excel等格式
- **功能点**:
  - **批量导入**: 
    - 支持大批量药材数据导入 (ImportHerbsAsync)
    - 完整数据验证（必填字段、格式检查、业务规则）
    - 重复数据检查和处理
    - 事务处理保证数据一致性
    - 详细的导入结果报告和错误信息
  - **数据导出**: 
    - 导出所有启用状态的药材数据 (ExportHerbsAsync)
    - 支持JSON格式导出
    - 操作日志记录
  - **导入模板**: 
    - 提供标准导入模板下载 (GetImportTemplateAsync)
    - 模板包含所有必要字段和格式说明
  - **数据验证**:
    - 导入前验证API接口 (ValidateImport)
    - 实时验证结果反馈
    - 详细错误信息和修复建议
- **API实现**: 
  - POST /api/v1/herbs/import （批量导入）
  - GET /api/v1/herbs/export （数据导出）
  - GET /api/v1/herbs/export-template （导入模板）
  - POST /api/v1/herbs/validate-import （导入验证）

#### FR-H005: 批量状态管理
- **描述**: 高效的批量操作功能，适用于大量药材管理
- **功能点**:
  - **批量状态更新**: 同时启用或禁用多个药材 (BatchUpdateStatusAsync)
  - **性能优化**: 使用EF Core ExecuteUpdate批量更新，避免逐条加载
  - **操作日志**: 详细记录批量操作的影响范围和结果
  - **权限控制**: 需要相应权限才能执行批量操作

#### FR-H006: 拼音码自动生成系统
- **描述**: 智能的中文药材名称拼音码生成功能
- **功能点**:
  - **自动生成**: 根据中文药材名称自动生成拼音码 (GenerateSimplePinyinCode)
  - **字符映射**: 中文字符到拼音首字母的智能映射 (GetChineseCharacterInitial)
  - **长度控制**: 拼音码长度限制在10字符以内
  - **混合支持**: 支持中英文混合名称处理
  - **手动覆盖**: 支持手动指定拼音码覆盖自动生成

---

## 7️⃣ 验方管理模块 (Formula)

### 已实现功能 ✅

#### FR-F001: 完整验方查询管理系统
- **描述**: 基于UltraThink双层架构的验方查询和管理体系
- **功能点**:
  - **分页查询**: 支持关键词、分类、类型筛选的分页查询 (GetPagedAsync)
  - **高级搜索**: 多条件验方搜索，支持名称、功效、用法匹配 (SearchFormulasAsync)
  - **分类查询**: 获取验方分类列表 (GetCategoriesAsync)
  - **全量查询**: 获取所有验方列表 (GetAllFormulasAsync)
  - **灵活筛选**: 支持关键词和分类的组合筛选 (GetFormulasAsync)
- **预设分类**: 经典验方、临床验方、个人验方三大类
- **API实现**: GET /api/v1/formulas, GET /api/v1/formulas/search

#### FR-F002: 智能验方推荐系统
- **描述**: 基于症状和诊断的智能验方推荐功能
- **功能点**:
  - **症候推荐**: 根据中医症候推荐适合的验方 (GetRecommendationsForSyndromeAsync)
  - **综合推荐**: 基于症状、诊断、医生ID的综合推荐 (GetRecommendationsAsync)
  - **推荐评分**: 自动计算验方匹配度和置信度
  - **推荐理由**: 提供推荐验方的匹配原因说明
  - **智能排序**: 按匹配度和使用频率排序推荐结果
- **API实现**: 
  - GET /api/v1/formulas/recommendations/syndrome/{syndrome}
  - GET /api/v1/formulas/recommendations

#### FR-F003: 验方模板和类型管理
- **描述**: 验方模板和分类管理功能
- **功能点**:
  - **模板查询**: 获取公开分享的验方模板 (GetTemplatesAsync)
  - **按类型查询**: 根据验方类型筛选验方 (GetByTypeAsync)  
  - **分享机制**: 验方公开分享和私有管理
  - **模板应用**: 模板验方可直接应用于处方开具
- **API实现**: 
  - GET /api/v1/formulas/templates
  - GET /api/v1/formulas/by-type/{type}

#### FR-F004: 验方业务操作系统
- **描述**: 验方的高级业务操作和管理功能
- **功能点**:
  - **验方复制**: 复制现有验方为新验方 (CopyAsync)
  - **从处方创建**: 将处方转化为可重复使用的验方 (CreateFromPrescriptionAsync)
  - **验方分享**: 设置验方为公开分享状态 (ShareFormulaAsync)
  - **取消分享**: 取消验方的公开分享 (UnshareFormulaAsync)
  - **状态管理**: 验方启用/禁用状态切换
- **API实现**:
  - POST /api/v1/formulas/{id}/copy
  - POST /api/v1/formulas/from-prescription/{prescriptionId}
  - POST /api/v1/formulas/{id}/share, POST /api/v1/formulas/{id}/unshare

#### FR-F005: 验方智能分析系统
- **描述**: 验方组成和安全性的智能分析功能
- **功能点**:
  - **组成分析**: 分析验方药材组成和复杂度 (AnalyzeFormulaAsync)
  - **安全评估**: 评估验方安全等级和风险提示
  - **配伍检查**: 基础药材配伍禁忌检查（如甘草与甘遂）
  - **用药建议**: 提供用药注意事项和禁忌症
  - **复杂度评级**: 根据药材数量评估验方复杂程度
- **分析结果**: 包含功效总结、禁忌症、安全警告等完整信息
- **API实现**: POST /api/v1/formulas/{id}/analyze

#### FR-F006: 完整API接口体系
- **描述**: RESTful API设计，支持验方管理的各种操作
- **基础接口**:
  - GET /api/v1/formulas - 分页查询验方列表  
  - GET /api/v1/formulas/{id} - 获取验方详情
  - POST /api/v1/formulas - 创建新验方  
  - PUT /api/v1/formulas/{id} - 更新验方信息
  - DELETE /api/v1/formulas/{id} - 删除验方
- **高级功能接口**:
  - GET /api/v1/formulas/categories - 获取验方分类
  - POST /api/v1/formulas/{id}/toggle-status - 切换验方状态
  - GET /api/v1/formulas/search - 高级搜索验方
- **统一响应格式**: ApiResponse<T>标准格式，包含成功状态、数据、时间戳

### 🚨 已知技术问题

#### 待完善功能 ⚠️
- **基础CRUD部分未完成**: CreateAsync/UpdateAsync/DeleteAsync/GetByIdAsync方法在FormulaService中返回"需要在BusinessService中实现"的失败信息
- **服务注册被禁用**: UnifiedServiceRegistration.cs中Formula服务注册被注释，标记为"TODO: 等待修复"
- **详情查询缺失**: GetByIdAsync未在QueryService中实现

#### 功能完整性评估
- ✅ **查询功能**: 100%完成 - 分页、搜索、分类、模板查询全部实现
- ✅ **智能推荐**: 100%完成 - 症候推荐、综合推荐、评分系统
- ✅ **高级操作**: 90%完成 - 复制、分享、分析功能完整
- ❌ **基础CRUD**: 10%完成 - 仅删除和状态管理部分实现
- ❌ **服务集成**: 0%完成 - 服务注册被禁用，无法正常使用

#### 修复建议
1. **完善BusinessService**: 实现CreateAsync、UpdateAsync等基础CRUD方法
2. **实现GetByIdAsync**: 在QueryService中添加详情查询功能
3. **启用服务注册**: 修复UnifiedServiceRegistration.cs中的服务注册
4. **完善依赖注入**: 确保所有Formula相关服务正确注册和注入

### 业务价值与特色

#### 核心优势
- **智能化程度高**: 验方推荐算法基于症状匹配，提供个性化推荐
- **临床实用性强**: 支持从处方创建验方，积累临床经验
- **分享协作机制**: 验方模板分享促进医生间经验交流  
- **安全性保障**: 内置配伍禁忌检查，提升用药安全

#### 与其他模块协作
- **与Prescriptions模块深度集成**: 验方可直接应用于处方开具
- **与Herbs模块协作**: 验方组成基于药材数据
- **支持临床工作流**: 从诊断→推荐验方→开具处方的完整链路

---

## 8️⃣ 认证授权模块 (Auth)

### 8.1 已实现功能 ✅

#### FR-A001: 完整用户身份认证系统
- **描述**: 用户登录和身份验证的完整流程管理
- **功能点**:
  - **基础认证**: 用户名密码登录验证 (VerifyCredentialsAsync)
  - **登录处理**: 完整登录业务流程 (ProcessLoginAsync)
  - **登录状态**: 实时登录状态验证 (ValidateTokenAsync)  
  - **Remember Me**: 长期登录记忆功能
  - **失败处理**: 登录失败的完整处理机制
  - **凭据验证**: 独立的凭据验证接口

#### FR-A002: 高级JWT Token管理
- **描述**: JWT令牌的全生命周期管理
- **功能点**:
  - **Token生成**: JWT访问令牌生成和签发
  - **刷新机制**: 专用刷新令牌管理 (GenerateRefreshTokenAsync)
  - **Token刷新**: 访问令牌刷新服务 (RefreshAccessTokenAsync)
  - **Token验证**: 令牌有效性验证和解析
  - **Token失效**: 单个令牌失效 (InvalidateTokenAsync)
  - **批量失效**: 用户所有令牌批量失效 (InvalidateAllUserTokensAsync)
  - **过期处理**: 自动过期检测和处理

#### FR-A003: 基于角色的访问控制 (RBAC)
- **描述**: 完整的角色权限控制体系
- **权限级别**:
  - **接口权限**: Controller/Action级别权限控制
  - **功能权限**: 模块功能访问权限控制  
  - **数据权限**: 数据访问范围权限控制
  - **操作权限**: 具体操作权限验证
- **实现特色**:
  - 专用AuthorizationService授权服务
  - 与JWT集成的权限验证

#### FR-A004: 高级会话管理系统
- **描述**: 用户登录会话的全面管理
- **功能点**:
  - **会话创建**: 用户登录会话建立和维护
  - **会话信息**: 当前会话详情查询 (GetSessionInfoAsync)
  - **超时处理**: 会话超时自动处理
  - **多终端控制**: 多设备登录状态管理
  - **强制下线**: 管理员强制用户下线 (ForceLogoutAsync)
  - **优雅登出**: 完整登出流程处理 (ProcessLogoutAsync)
- **实现特色**:
  - 专用AuthSessionService会话服务
  - 会话状态数据库持久化

#### FR-A005: 全面安全审计日志系统
- **描述**: 认证相关的完整安全审计体系
- **日志类型**:
  - **登录审计**: 成功登录记录 (LogSuccessfulLoginAsync)
  - **失败审计**: 登录失败详细记录 (LogFailedLoginAsync)  
  - **异常审计**: 登录异常和错误记录 (LogLoginExceptionAsync)
  - **登出审计**: 用户登出行为记录 (LogLogoutAsync)
  - **强制审计**: 强制下线操作记录 (LogForceLogoutAsync)
  - **令牌审计**: 令牌刷新操作记录 (LogTokenRefreshAsync)
- **审计特色**:
  - 结构化日志记录
  - 安全事件分类存储
  - 完整操作链路追踪

#### FR-A006: 系统管理员安全管理
- **描述**: 超级管理员账户的特殊安全管理
- **功能点**:
  - **密码管理**: 系统管理员密码修改 (ChangeSysAdminPasswordAsync)
  - **安全策略**: 管理员账户特殊安全策略
  - **权限控制**: 最高权限账户管理

### 8.2 计划功能 🔮 (v2.0)

#### FR-A007: 双因素认证
- **描述**: 增强安全性的双因素认证
- **功能点**: 短信验证码、邮箱验证、应用令牌

---

## 📊 需求追溯矩阵

### 需求变更历史
| 变更日期 | 变更类型 | 影响模块 | 变更原因 | 变更内容 |
|----------|----------|----------|----------|----------|
| 2025-09-01 | 删除 | Patients | 简化系统 | 移除复杂统计分析功能 |
| 2025-09-01 | 删除 | Users | 简化系统 | 移除批量操作和复杂权限 |
| 2025-09-01 | 删除 | MedicalCase | 简化系统 | 移除统计和归档功能 |
| 2025-08-30 | 优化 | All | 架构重构 | UltraThink双层架构应用 |
| 2025-08-20 | 删除 | All | 避免过度设计 | 移除AI智能功能 |

### 需求优先级
| 优先级 | 功能数量 | 说明 |
|--------|----------|------|
| **P0 (核心)** | 25个 | 诊疗核心流程，必须实现 |
| **P1 (重要)** | 19个 | 重要辅助功能，提升效率 |
| **P2 (一般)** | 10个 | 便民功能，改善体验 |
| **P3 (未来)** | 15个 | 计划功能，后续版本 |

---

**文档维护说明**: 本文档反映系统当前功能需求的最新状态。每次功能变更后及时更新对应章节。功能变更的详细过程记录请查看 `docs/process/` 目录。