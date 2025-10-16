# 数据库设计指南

**基于凌隐宝堂中医诊所 11个核心实体的完整数据库设计** - 深入理解数据库架构、关系设计和优化策略

## 🗄️ 数据库架构概览

### 数据库架构图
```
                    ┌─────────────────────────────────────┐
                    │        Authentication Layer        │
                    │         (认证与权限层)               │
                    ├─────────────────────────────────────┤
                    │            Users Table               │
                    │        AdminSecrets Table           │
                    └─────────────────────────────────────┘
                                      │
                    ┌─────────────────────┼─────────────────────┐
                    │                     │                     │
        ┌───────────▼─────────┐   ┌─────▼─────┐   ┌─────────▼─────────┐
        │   Core Entities     │   │ Medical   │   │   Prescription    │
        │   (核心实体)         │   │ Entities  │   │    Entities       │
        └─────────────────────┘   └───────────┘   └──────────────────┘
                    │                     │                     │
    ┌───────────▼─────────┐   ┌─────▼─────┐   ┌─────────▼─────────┐
    │  Patients Table     │   │ Medical   │   │ Prescriptions     │
    │                     │   │ Cases     │   │ PrescriptionItems │
    └─────────────────────┘   │           │   └──────────────────┘
                              │           │
    ┌───────────▼─────────┐   │           │   ┌─────────▼─────────┐
    │ Consultations       │   │           │   │ Herbs Table       │
    │ Four Diagnostics    │   │           │   │                   │
    └─────────────────────┘   │           │   └──────────────────┘
                              │           │
    ┌───────────▼─────────┐   │           │   ┌─────────▼─────────┐
    │ Formula Templates   │   │           │   │ Formula Items     │
    │ Herb Combinations   │   │           │   │                   │
    └─────────────────────┘   │           │   └──────────────────┘
                              │           │
                              └───────────┘
```

### 核心实体关系图
```
Users (1) ←→ (N) MedicalCases (1) ←→ (N) Prescriptions (1) ←→ (N) PrescriptionItems (N) ←→ (1) Herbs
  ↑                      ↑                         ↑
  │                      │                         │
  │                      │                         │
AdminSecrets        Patients                Formulas (N) ←→ (N) FormulaItems (N) ←→ (1) Herbs
  │                      ↑
  │                      │
  │                      │
  └──────────────────────Consultations (1:N)
```

## 📊 核心表结构设计

### 1. 认证相关表

#### Users 表 - 用户信息
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    RealName NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(100) NULL,
    Role INT NOT NULL DEFAULT 0, -- 0: User, 1: Admin
    Status INT NOT NULL DEFAULT 1, -- 0: Disabled, 1: Enabled
    LastLoginAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    
    -- 索引
    INDEX IX_Users_UserName (UserName),
    INDEX IX_Users_Email (Email),
    INDEX IX_Users_Role (Role),
    INDEX IX_Users_Status (Status),
    INDEX IX_Users_CreatedAt (CreatedAt)
);

-- 用户角色枚举说明
-- Role: 0 = User (普通用户), 1 = Admin (管理员)
-- Status: 0 = Disabled (禁用), 1 = Enabled (启用)
```

#### AdminSecrets 表 - 超级管理员密钥
```sql
CREATE TABLE AdminSecrets (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    PasswordHash NVARCHAR(255) NOT NULL,
    Description NVARCHAR(200) NULL,
    Status INT NOT NULL DEFAULT 1, -- 0: Disabled, 1: Enabled
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastChangedAt DATETIME2 NULL,
    ChangedBy UNIQUEIDENTIFIER NULL,
    
    -- 索引
    INDEX IX_AdminSecrets_Status (Status),
    INDEX IX_AdminSecrets_CreatedAt (CreatedAt)
);

-- 超级管理员设计说明
-- 1. 物理隔离：与Users表完全分离
-- 2. 配置驱动：用户名从配置文件读取，不在数据库存储
-- 3. 安全加固：独立的密码哈希和更新机制
-- 4. 审计追踪：记录所有密码变更操作
```

### 2. 核心业务表

#### Patients 表 - 患者信息
```sql
CREATE TABLE Patients (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(50) NOT NULL,
    Gender INT NOT NULL DEFAULT 0, -- 0: Unknown, 1: Male, 2: Female
    BirthDate DATE NULL,
    IdNumber NVARCHAR(18) NULL UNIQUE,
    PhoneNumber NVARCHAR(20) NOT NULL UNIQUE,
    Address NVARCHAR(200) NULL,
    EmergencyContact NVARCHAR(100) NULL,
    EmergencyPhone NVARCHAR(20) NULL,
    MedicalHistory NVARCHAR(MAX) NULL,
    Allergies NVARCHAR(MAX) NULL,
    Status INT NOT NULL DEFAULT 1, -- 0: Deleted, 1: Active, 2: Inactive
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    
    -- 约束
    CONSTRAINT CK_Patients_PhoneNumber CHECK (PhoneNumber LIKE '1[3-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT CK_Patients_IdNumber CHECK (IdNumber IS NULL OR IdNumber LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][X0-9]'),
    CONSTRAINT CK_Patients_Gender CHECK (Gender IN (0, 1, 2)),
    CONSTRAINT CK_Patients_Status CHECK (Status IN (0, 1, 2)),
    
    -- 索引
    INDEX IX_Patients_Name (Name),
    INDEX IX_Patients_PhoneNumber (PhoneNumber),
    INDEX IX_Patients_IdNumber (IdNumber),
    INDEX IX_Patients_Gender (Gender),
    INDEX IX_Patients_Status (Status),
    INDEX IX_Patients_CreatedAt (CreatedAt),
    INDEX IX_Patients_Search (Name, PhoneNumber) INCLUDE (Id, Gender, Status)
);

-- 患者表设计说明
-- 1. 唯一性约束：手机号和身份证号唯一
-- 2. 数据验证：手机号格式、身份证号格式验证
-- 3. 软删除：使用Status字段标记删除状态
-- 4. 审计字段：创建时间、更新时间、操作人
-- 5. 医疗信息：病史、过敏史等敏感信息
```

#### MedicalCases 表 - 医案信息
```sql
CREATE TABLE MedicalCases (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    CaseNumber NVARCHAR(20) NOT NULL UNIQUE,
    Title NVARCHAR(200) NOT NULL,
    ChiefComplaint NVARCHAR(MAX) NOT NULL,
    PresentIllness NVARCHAR(MAX) NULL,
    PastHistory NVARCHAR(MAX) NULL,
    FamilyHistory NVARCHAR(MAX) NULL,
    PhysicalExam NVARCHAR(MAX) NULL,
    Diagnosis NVARCHAR(500) NULL,
    TreatmentPrinciple NVARCHAR(500) NULL,
    Prognosis NVARCHAR(300) NULL,
    Status INT NOT NULL DEFAULT 0, -- 0: Registered, 1: InTreatment, 2: Completed, 3: Archived, 4: Cancelled
    Priority INT NOT NULL DEFAULT 1, -- 0: Low, 1: Normal, 2: High, 3: Urgent
    CompletedAt DATETIME2 NULL,
    ArchivedAt DATETIME2 NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    
    -- 外键约束
    CONSTRAINT FK_MedicalCases_Patients FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_MedicalCases_Doctors FOREIGN KEY (DoctorId) REFERENCES Users(Id) ON DELETE RESTRICT,
    
    -- 约束
    CONSTRAINT CK_MedicalCases_Status CHECK (Status IN (0, 1, 2, 3, 4)),
    CONSTRAINT CK_MedicalCases_Priority CHECK (Priority IN (0, 1, 2, 3)),
    
    -- 索引
    INDEX IX_MedicalCases_PatientId (PatientId),
    INDEX IX_MedicalCases_DoctorId (DoctorId),
    INDEX IX_MedicalCases_CaseNumber (CaseNumber),
    INDEX IX_MedicalCases_Status (Status),
    INDEX IX_MedicalCases_Priority (Priority),
    INDEX IX_MedicalCases_CreatedAt (CreatedAt),
    INDEX IX_MedicalCases_PatientStatus (PatientId, Status),
    INDEX IX_MedicalCases_DoctorStatus (DoctorId, Status)
);

-- 医案表设计说明
-- 1. 状态机：完整的医案生命周期管理
-- 2. 关联约束：患者和医生不能直接删除
-- 3. 编号规则：自动生成唯一医案编号
-- 4. 分级优先：支持紧急程度分级
-- 5. 完整记录：包含完整的诊疗信息
```

#### Consultations 表 - 诊疗记录
```sql
CREATE TABLE Consultations (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    MedicalCaseId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    ConsultationDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    SequenceNumber INT NOT NULL DEFAULT 1, -- 同一医案中的诊疗序号
    
    -- 四诊信息
    Inspection NVARCHAR(MAX) NULL, -- 望诊
    Auscultation NVARCHAR(MAX) NULL, -- 闻诊
    Inquiry NVARCHAR(MAX) NULL, -- 问诊
    Palpation NVARCHAR(MAX) NULL, -- 切诊
    
    -- 辨证论治
    SyndromeDifferentiation NVARCHAR(500) NULL, -- 辨证结果
    PatternIdentification NVARCHAR(300) NULL, -- 证型识别
    TreatmentPrinciple NVARCHAR(500) NULL, -- 治法
    HerbalFormula NVARCHAR(MAX) NULL, -- 药方组成
    DosageInstructions NVARCHAR(MAX) NULL, -- 用法用量
    LifestyleAdvice NVARCHAR(MAX) NULL, -- 生活建议
    
    -- 随访信息
    FollowUpDate DATE NULL,
    FollowUpNotes NVARCHAR(MAX) NULL,
    NextConsultationDate DATE NULL,
    
    Status INT NOT NULL DEFAULT 1, -- 0: Deleted, 1: Active
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    
    -- 外键约束
    CONSTRAINT FK_Consultations_MedicalCases FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Consultations_Doctors FOREIGN KEY (DoctorId) REFERENCES Users(Id) ON DELETE RESTRICT,
    
    -- 约束
    CONSTRAINT CK_Consultations_Status CHECK (Status IN (0, 1)),
    CONSTRAINT CK_Consultations_SequenceNumber CHECK (SequenceNumber > 0),
    
    -- 索引
    INDEX IX_Consultations_MedicalCaseId (MedicalCaseId),
    INDEX IX_Consultations_DoctorId (DoctorId),
    INDEX IX_Consultations_Date (ConsultationDate),
    INDEX IX_Consultations_Status (Status),
    INDEX IX_Consultations_Sequence (MedicalCaseId, SequenceNumber),
    INDEX IX_Consultations_FollowUp (FollowUpDate)
);

-- 诊疗记录表设计说明
-- 1. 四诊合参：完整记录望闻问切四诊信息
-- 2. 辨证论治：中医特色的诊断和治疗方案
-- 3. 序列管理：同一医案多次诊疗的序列记录
-- 4. 随访机制：完整的随访和复诊管理
-- 5. 级联删除：医案删除时自动删除相关诊疗记录
```

### 3. 处方相关表

#### Prescriptions 表 - 处方信息
```sql
CREATE TABLE Prescriptions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    MedicalCaseId UNIQUEIDENTIFIER NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    PrescriptionNo NVARCHAR(20) NOT NULL UNIQUE,
    Indication NVARCHAR(500) NULL, -- 适应症
    DosageCount INT NOT NULL DEFAULT 7, -- 帖数
    UsageInstructions NVARCHAR(MAX) NULL, -- 用法用量
    DecoctionMethod NVARCHAR(300) NULL, -- 煎服方法
    Contraindications NVARCHAR(MAX) NULL, -- 禁忌
    Advice NVARCHAR(MAX) NULL, -- 医嘱
    Discount DECIMAL(5,4) NOT NULL DEFAULT 1.0000, -- 折扣
    TotalAmount DECIMAL(18,2) NULL, -- 总金额（计算字段，数据库不存储）
    
    -- 打印相关
    PrintVersion INT NOT NULL DEFAULT 1,
    LastPrintedAt DATETIME2 NULL,
    PrintCount INT NOT NULL DEFAULT 0,
    
    -- 来源信息
    FormulaSource INT NULL, -- 来源：0: 手工开方, 1: 验方模板, 2: 智能推荐
    FormulaId UNIQUEIDENTIFIER NULL, -- 关联的验方模板ID
    Remark NVARCHAR(500) NULL,
    
    Status INT NOT NULL DEFAULT 0, -- 0: Draft, 1: Confirmed, 2: Dispensed, 3: Completed, 4: Cancelled
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    
    -- 外键约束
    CONSTRAINT FK_Prescriptions_MedicalCases FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Prescriptions_Doctors FOREIGN KEY (DoctorId) REFERENCES Users(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Prescriptions_Formulas FOREIGN KEY (FormulaId) REFERENCES Formulas(Id) ON DELETE SET NULL,
    
    -- 约束
    CONSTRAINT CK_Prescriptions_DosageCount CHECK (DosageCount > 0 AND DosageCount <= 30),
    CONSTRAINT CK_Prescriptions_Discount CHECK (Discount >= 0.5 AND Discount <= 1.0),
    CONSTRAINT CK_Prescriptions_PrintVersion CHECK (PrintVersion > 0),
    CONSTRAINT CK_Prescriptions_PrintCount CHECK (PrintCount >= 0),
    CONSTRAINT CK_Prescriptions_FormulaSource CHECK (FormulaSource IN (0, 1, 2)),
    CONSTRAINT CK_Prescriptions_Status CHECK (Status IN (0, 1, 2, 3, 4)),
    
    -- 索引
    INDEX IX_Prescriptions_MedicalCaseId (MedicalCaseId),
    INDEX IX_Prescriptions_PatientId (PatientId),
    INDEX IX_Prescriptions_DoctorId (DoctorId),
    INDEX IX_Prescriptions_PrescriptionNo (PrescriptionNo),
    INDEX IX_Prescriptions_Status (Status),
    INDEX IX_Prescriptions_CreatedAt (CreatedAt),
    INDEX IX_Prescriptions_FormulaId (FormulaId),
    INDEX IX_Prescriptions_PatientStatus (PatientId, Status)
);

-- 处方表设计说明
-- 1. 编号规则：RX + 日期 + 序号格式
-- 2. 价格计算：总金额通过处方项计算，不在数据库存储
-- 3. 打印管理：支持打印版本控制和打印统计
-- 4. 来源追踪：记录处方来源（手工/模板/推荐）
-- 5. 状态管理：完整的处方生命周期管理
```

#### PrescriptionItems 表 - 处方项
```sql
CREATE TABLE PrescriptionItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    HerbId UNIQUEIDENTIFIER NOT NULL,
    HerbName NVARCHAR(100) NOT NULL, -- 冗余存储，提高查询性能
    Quantity DECIMAL(10,2) NOT NULL, -- 数量
    Unit NVARCHAR(20) NOT NULL, -- 单位（克、两、钱等）
    UnitPrice DECIMAL(18,4) NOT NULL, -- 单价
    Subtotal DECIMAL(18,2) NULL, -- 小计（计算字段）
    Usage NVARCHAR(200) NULL, -- 特殊用法
    Position NVARCHAR(100) NULL, -- 药物地位（君臣佐使）
    Processing NVARCHAR(200) NULL, -- 炮制方法
    Remark NVARCHAR(300) NULL,
    SortOrder INT NOT NULL DEFAULT 0, -- 排序序号
    Status INT NOT NULL DEFAULT 1, -- 0: Deleted, 1: Active
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- 外键约束
    CONSTRAINT FK_PrescriptionItems_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PrescriptionItems_Herbs FOREIGN KEY (HerbId) REFERENCES Herbs(Id) ON DELETE RESTRICT,
    
    -- 约束
    CONSTRAINT CK_PrescriptionItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_PrescriptionItems_UnitPrice CHECK (UnitPrice > 0),
    CONSTRAINT CK_PrescriptionItems_SortOrder CHECK (SortOrder >= 0),
    CONSTRAINT CK_PrescriptionItems_Status CHECK (Status IN (0, 1)),
    
    -- 索引
    INDEX IX_PrescriptionItems_PrescriptionId (PrescriptionId),
    INDEX IX_PrescriptionItems_HerbId (HerbId),
    INDEX IX_PrescriptionItems_Status (Status),
    INDEX IX_PrescriptionItems_SortOrder (PrescriptionId, SortOrder),
    INDEX IX_PrescriptionItems_HerbName (HerbName)
);

-- 处方项表设计说明
-- 1. 冗余设计：存储药材名称，避免关联查询
-- 2. 价格管理：支持单价和小计计算
-- 3. 中医特色：支持君臣佐使、炮制方法等
-- 4. 排序管理：支持药材显示顺序
-- 5. 级联删除：处方删除时自动删除处方项
```

### 4. 药材相关表

#### Herbs 表 - 药材信息
```sql
CREATE TABLE Herbs (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL UNIQUE,
    PinYinCode NVARCHAR(50) NULL, -- 拼音码，用于快速检索
    EnglishName NVARCHAR(200) NULL,
    Alias NVARCHAR(300) NULL, -- 别名
    Category NVARCHAR(50) NULL, -- 分类（补益药、清热药等）
    Nature NVARCHAR(50) NULL, -- 药性（寒、热、温、凉）
    Flavor NVARCHAR(50) NULL, -- 药味（辛、甘、酸、苦、咸）
    Meridian NVARCHAR(200) NULL, -- 归经
    Efficacy NVARCHAR(MAX) NULL, -- 功效
    Usage NVARCHAR(MAX) NULL, -- 用法用量
    Contraindications NVARCHAR(MAX) NULL, -- 禁忌
    Compatibility NVARCHAR(MAX) NULL, -- 配伍禁忌
    
    -- 采购信息
    Origin NVARCHAR(100) NULL, -- 产地
    Specification NVARCHAR(100) NULL, -- 规格
    Grade NVARCHAR(50) NULL, -- 等级
    Unit NVARCHAR(20) NOT NULL DEFAULT '克',
    Price DECIMAL(18,4) NOT NULL DEFAULT 0.0000, -- 单价
    Stock DECIMAL(18,2) NULL, -- 库存数量
    MinStock DECIMAL(18,2) NULL, -- 最低库存
    MaxStock DECIMAL(18,2) NULL, -- 最高库存
    
    -- 供应商信息
    SupplierId UNIQUEIDENTIFIER NULL,
    SupplierName NVARCHAR(200) NULL,
    PurchasePrice DECIMAL(18,4) NULL, -- 采购价格
    
    -- 质量控制
    BatchNumber NVARCHAR(100) NULL, -- 批次号
    ProductionDate DATE NULL, -- 生产日期
    ExpiryDate DATE NULL, -- 有效期
    QualityStandard NVARCHAR(100) NULL, -- 质量标准
    
    -- 状态管理
    Status INT NOT NULL DEFAULT 1, -- 0: Disabled, 1: Enabled, 2: OutOfStock
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- 审计字段
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    
    -- 约束
    CONSTRAINT CK_Herbs_Price CHECK (Price >= 0),
    CONSTRAINT CK_Herbs_Stock CHECK (Stock >= 0),
    CONSTRAINT CK_Herbs_Status CHECK (Status IN (0, 1, 2)),
    CONSTRAINT CK_Herbs_Expiry CHECK (ExpiryDate IS NULL OR ExpiryDate > GETDATE()),
    
    -- 索引
    INDEX IX_Herbs_Name (Name),
    INDEX IX_Herbs_PinYinCode (PinYinCode),
    INDEX IX_Herbs_Category (Category),
    INDEX IX_Herbs_Status (Status),
    INDEX IX_Herbs_Stock (Stock),
    INDEX IX_Herbs_Price (Price),
    INDEX IX_Herbs_Search (Name, PinYinCode, Category),
    INDEX IX_Herbs_Expiry (ExpiryDate)
);

-- 药材表设计说明
-- 1. 中医特色：完整的药性、药味、归经信息
-- 2. 库存管理：支持库存预警和采购管理
-- 3. 质量控制：批次号、有效期、质量标准
-- 4. 快速检索：拼音码支持快速输入
-- 5. 成本管理：采购价格和销售价格分离
```

#### Formulas 表 - 验方模板
```sql
CREATE TABLE Formulas (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(50) NULL, -- 验方编码
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(50) NULL, -- 分类（经方、时方、自拟方等）
    Source NVARCHAR(200) NULL, -- 来源（伤寒论、金匮要略等）
    Dynasty NVARCHAR(50) NULL, -- 朝代
    Author NVARCHAR(100) NULL, -- 作者
    
    -- 功效主治
    Indications NVARCHAR(MAX) NULL, -- 适应症
    Functions NVARCHAR(MAX) NULL, -- 功效
    Syndrome NVARCHAR(500) NULL, -- 主治病症
    Contraindications NVARCHAR(MAX) NULL, -- 禁忌
    
    -- 方剂组成
    Composition NVARCHAR(MAX) NULL, -- 方解
    Modification NVARCHAR(MAX) NULL, -- 加减法
    DosageInstructions NVARCHAR(MAX) NULL, -- 用法用量
    DecoctionMethod NVARCHAR(300) NULL, -- 煎服方法
    
    -- 现代应用
    ModernIndications NVARCHAR(MAX) NULL, -- 现代适应症
    ClinicalApplications NVARCHAR(MAX) NULL, -- 临床应用
    ResearchSummary NVARCHAR(MAX) NULL, -- 研究进展
    
    -- 使用统计
    UsageCount INT NOT NULL DEFAULT 0, -- 使用次数
    LastUsedAt DATETIME2 NULL,
    AverageRating DECIMAL(3,2) NULL, -- 平均评分
    RatingCount INT NOT NULL DEFAULT 0, -- 评分次数
    
    -- 状态管理
    Status INT NOT NULL DEFAULT 1, -- 0: Disabled, 1: Enabled, 2: Draft
    IsPublic BIT NOT NULL DEFAULT 1, -- 是否公开
    IsRecommended BIT NOT NULL DEFAULT 0, -- 是否推荐
    
    -- 审计字段
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    
    -- 约束
    CONSTRAINT CK_Formulas_Status CHECK (Status IN (0, 1, 2)),
    CONSTRAINT CK_Formulas_UsageCount CHECK (UsageCount >= 0),
    CONSTRAINT CK_Formulas_AverageRating CHECK (AverageRating >= 0 AND AverageRating <= 5),
    
    -- 索引
    INDEX IX_Formulas_Name (Name),
    INDEX IX_Formulas_Code (Code),
    INDEX IX_Formulas_Category (Category),
    INDEX IX_Formulas_Source (Source),
    INDEX IX_Formulas_Status (Status),
    INDEX IX_Formulas_UsageCount (UsageCount),
    INDEX IX_Formulas_Rating (AverageRating),
    INDEX IX_Formulas_IsRecommended (IsRecommended),
    INDEX IX_Formulas_Search (Name, Code, Category)
);

-- 验方表设计说明
-- 1. 中医特色：完整的方剂理论和应用信息
-- 2. 现代化：现代医学适应症和临床应用
-- 3. 智能推荐：使用统计和评分支持智能推荐
-- 4. 版本管理：支持验方模板的迭代和优化
-- 5. 知识管理：完整的方剂知识库
```

#### FormulaItems 表 - 验方药材组成
```sql
CREATE TABLE FormulaItems (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    FormulaId UNIQUEIDENTIFIER NOT NULL,
    HerbId UNIQUEIDENTIFIER NOT NULL,
    HerbName NVARCHAR(100) NOT NULL, -- 冗余存储
    Quantity DECIMAL(10,2) NOT NULL, -- 标准用量
    Unit NVARCHAR(20) NOT NULL DEFAULT '克',
    Position NVARCHAR(50) NULL, -- 君臣佐使
    Role NVARCHAR(100) NULL, -- 作用
    Processing NVARCHAR(200) NULL, -- 炮制方法
    AlternativeHerbs NVARCHAR(500) NULL, -- 替代药材
    Notes NVARCHAR(500) NULL, -- 备注
    SortOrder INT NOT NULL DEFAULT 0,
    Status INT NOT NULL DEFAULT 1, -- 0: Deleted, 1: Active
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- 外键约束
    CONSTRAINT FK_FormulaItems_Formulas FOREIGN KEY (FormulaId) REFERENCES Formulas(Id) ON DELETE CASCADE,
    CONSTRAINT FK_FormulaItems_Herbs FOREIGN KEY (HerbId) REFERENCES Herbs(Id) ON DELETE RESTRICT,
    
    -- 约束
    CONSTRAINT CK_FormulaItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_FormulaItems_SortOrder CHECK (SortOrder >= 0),
    CONSTRAINT CK_FormulaItems_Status CHECK (Status IN (0, 1)),
    
    -- 索引
    INDEX IX_FormulaItems_FormulaId (FormulaId),
    INDEX IX_FormulaItems_HerbId (HerbId),
    INDEX IX_FormulaItems_Position (Position),
    INDEX IX_FormulaItems_SortOrder (FormulaId, SortOrder),
    INDEX IX_FormulaItems_HerbName (HerbName)
);

-- 验方药材项表设计说明
-- 1. 君臣佐使：记录药材在方剂中的地位
-- 2. 标准用量：为临床开方提供参考
-- 3. 替代方案：支持药材替代选择
-- 4. 排序管理：按传统方剂结构排序
-- 5. 灵活配置：支持特殊用法和炮制方法
```

## 🔗 数据库关系设计

### 1. 主要关系映射

#### 核心业务流程关系
```sql
-- 患者就医流程
Patients (1) ←→ (N) MedicalCases (1) ←→ (N) Consultations
    ↓                    ↓                    ↓
    |                    |                    |
    ↓                    ↓                    ↓
MedicalCases (1) ←→ (N) Prescriptions (1) ←→ (N) PrescriptionItems
                                                    ↓
                                                    ↓
                                               Herbs (N) ←→ (1) PrescriptionItems

-- 验方管理流程
Formulas (1) ←→ (N) FormulaItems (N) ←→ (1) Herbs
    ↓                    ↓
    ↓                    ↓
Prescriptions (N) ←→ (1) Formulas (引用关系)
```

#### 关系强度说明
- **强关系**：Patients ↔ MedicalCases (业务核心关系)
- **中等关系**：MedicalCases ↔ Prescriptions (业务扩展关系)  
- **弱关系**：Prescriptions ↔ Formulas (参考引用关系)

### 2. 外键约束设计

#### 约束策略
```sql
-- 核心数据保护：RESTRICT 约束
ALTER TABLE MedicalCases ADD CONSTRAINT FK_MedicalCases_Patients 
    FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE RESTRICT;

ALTER TABLE Prescriptions ADD CONSTRAINT FK_Prescriptions_Patients 
    FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE RESTRICT;

-- 级联删除：CASCADE 约束
ALTER TABLE Consultations ADD CONSTRAINT FK_Consultations_MedicalCases 
    FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id) ON DELETE CASCADE;

ALTER TABLE PrescriptionItems ADD CONSTRAINT FK_PrescriptionItems_Prescriptions 
    FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id) ON DELETE CASCADE;

-- 置空处理：SET NULL 约束
ALTER TABLE Prescriptions ADD CONSTRAINT FK_Prescriptions_MedicalCases 
    FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id) ON DELETE SET NULL;
```

### 3. 索引设计策略

#### 查询性能优化
```sql
-- 复合索引：支持多字段查询
CREATE INDEX IX_Patients_Search ON Patients(Name, PhoneNumber) INCLUDE (Id, Gender, Status);
CREATE INDEX IX_MedicalCases_PatientStatus ON MedicalCases(PatientId, Status) INCLUDE (Id, CreatedAt);
CREATE INDEX IX_Prescriptions_PatientStatus ON Prescriptions(PatientId, Status) INCLUDE (Id, CreatedAt, TotalAmount);

-- 覆盖索引：支持特定查询完全覆盖
CREATE INDEX IX_Consultations_MedicalCase_Sequence ON Consultations(MedicalCaseId, SequenceNumber) 
    INCLUDE (Id, ConsultationDate, Diagnosis, TreatmentPrinciple);

-- 分区索引：支持大数据量查询
CREATE INDEX IX_Prescriptions_CreatedDate ON Prescriptions(CreatedAt) 
    WHERE Status IN (1, 2, 3); -- 过滤索引，只索引活跃处方
```

## 📈 数据库性能优化

### 1. 查询优化策略

#### 常见查询模式优化
```sql
-- 患者列表查询优化
-- 原始查询
SELECT p.*, mc.CreatedAt as LastVisitDate
FROM Patients p
LEFT JOIN MedicalCases mc ON p.Id = mc.PatientId
WHERE p.Status = 1
ORDER BY p.CreatedAt DESC;

-- 优化后查询（使用索引提示）
SELECT p.Id, p.Name, p.PhoneNumber, p.Gender, p.Status, p.CreatedAt
FROM Patients p WITH (INDEX(IX_Patients_Status))
WHERE p.Status = 1
ORDER BY p.CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;

-- 医案统计查询优化
-- 原始查询
SELECT 
    p.Name as PatientName,
    COUNT(mc.Id) as CaseCount,
    COUNT(pr.Id) as PrescriptionCount,
    SUM(pr.TotalAmount) as TotalAmount
FROM Patients p
LEFT JOIN MedicalCases mc ON p.Id = mc.PatientId
LEFT JOIN Prescriptions pr ON mc.Id = pr.MedicalCaseId
WHERE p.Status = 1
GROUP BY p.Id, p.Name;

-- 优化后查询（使用临时表和索引）
-- 首先获取活跃患者
DECLARE @ActivePatients TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(100));
INSERT INTO @ActivePatients
SELECT Id, Name FROM Patients WHERE Status = 1;

-- 然后分别统计并合并结果
SELECT 
    ap.Id,
    ap.Name,
    ISNULL(mc.CaseCount, 0) as CaseCount,
    ISNULL(pr.PrescriptionCount, 0) as PrescriptionCount,
    ISNULL(pr.TotalAmount, 0) as TotalAmount
FROM @ActivePatients ap
LEFT JOIN (
    SELECT PatientId, COUNT(*) as CaseCount 
    FROM MedicalCases 
    WHERE PatientId IN (SELECT Id FROM @ActivePatients)
    GROUP BY PatientId
) mc ON ap.Id = mc.PatientId
LEFT JOIN (
    SELECT mc.PatientId, COUNT(*) as PrescriptionCount, SUM(TotalAmount) as TotalAmount
    FROM Prescriptions pr
    JOIN MedicalCases mc ON pr.MedicalCaseId = mc.Id
    WHERE mc.PatientId IN (SELECT Id FROM @ActivePatients)
    GROUP BY mc.PatientId
) pr ON ap.Id = pr.PatientId;
```

### 2. 分页查询优化

#### 高效分页实现
```sql
-- 基于OFFSET-FETCH的分页（SQL Server 2012+）
CREATE PROCEDURE sp_GetPatientsPaged
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @Keyword NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    -- 计算总数
    DECLARE @TotalCount INT;
    SELECT @TotalCount = COUNT(*)
    FROM Patients p
    WHERE p.Status = 1
      AND (@Keyword IS NULL OR p.Name LIKE '%' + @Keyword + '%' 
                                   OR p.PhoneNumber LIKE '%' + @Keyword + '%');
    
    -- 分页查询
    SELECT 
        p.Id,
        p.Name,
        p.PhoneNumber,
        p.Gender,
        p.BirthDate,
        p.Status,
        p.CreatedAt,
        @TotalCount as TotalCount,
        CEILING(CAST(@TotalCount AS FLOAT) / @PageSize) as TotalPages
    FROM Patients p
    WHERE p.Status = 1
      AND (@Keyword IS NULL OR p.Name LIKE '%' + @Keyword + '%' 
                                   OR p.PhoneNumber LIKE '%' + @Keyword + '%')
    ORDER BY p.CreatedAt DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;

-- 基于Keyset的分页（大数据量优化）
CREATE PROCEDURE sp_GetPrescriptionsKeysetPaged
    @LastPrescriptionId UNIQUEIDENTIFIER = NULL,
    @PageSize INT = 20,
    @PatientId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 使用ID作为分页键，避免OFFSET性能问题
    SELECT TOP (@PageSize)
        pr.Id,
        pr.PrescriptionNo,
        pr.Indication,
        pr.DosageCount,
        pr.TotalAmount,
        pr.CreatedAt,
        p.Name as PatientName
    FROM Prescriptions pr
    JOIN Patients p ON pr.PatientId = p.Id
    WHERE 
        (@LastPrescriptionId IS NULL OR pr.Id < @LastPrescriptionId)
        AND (@PatientId IS NULL OR pr.PatientId = @PatientId)
        AND pr.Status IN (1, 2, 3)
    ORDER BY pr.Id DESC;
END;
```

### 3. 缓存策略

#### 应用层缓存设计
```csharp
/// <summary>
/// 缓存策略实现
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;

    // 缓存键定义
    private const string PATIENT_LIST_KEY = "patients:list";
    private const string HERB_LIST_KEY = "herbs:list";
    private const string FORMULA_LIST_KEY = "formulas:list";
    private const string PATIENT_DETAIL_KEY = "patient:detail:{0}";
    private const string PRESCRIPTION_STATS_KEY = "prescriptions:stats";

    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者列表（内存缓存，短期）
    /// </summary>
    public async Task<List<PatientDto>> GetPatientsAsync()
    {
        if (!_memoryCache.TryGetValue(PATIENT_LIST_KEY, out List<PatientDto>? patients))
        {
            patients = await LoadPatientsFromDatabaseAsync();
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };
            
            _memoryCache.Set(PATIENT_LIST_KEY, patients, cacheOptions);
        }

        return patients ?? new List<PatientDto>();
    }

    /// <summary>
    /// 获取患者详情（分布式缓存，中期）
    /// </summary>
    public async Task<PatientDto?> GetPatientAsync(Guid patientId)
    {
        string cacheKey = string.Format(PATIENT_DETAIL_KEY, patientId);
        
        var cachedData = await _distributedCache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            return JsonSerializer.Deserialize<PatientDto>(cachedData);
        }

        var patient = await LoadPatientFromDatabaseAsync(patientId);
        if (patient != null)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            };
            
            var jsonData = JsonSerializer.Serialize(patient);
            await _distributedCache.SetStringAsync(cacheKey, jsonData, cacheOptions);
        }

        return patient;
    }

    /// <summary>
    /// 获取处方统计（长期缓存）
    /// </summary>
    public async Task<PrescriptionStatisticsDto> GetPrescriptionStatisticsAsync()
    {
        if (!_memoryCache.TryGetValue(PRESCRIPTION_STATS_KEY, out PrescriptionStatisticsDto? stats))
        {
            stats = await CalculatePrescriptionStatisticsAsync();
            
            // 统计数据每天更新一次
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
                Priority = CacheItemPriority.High
            };
            
            _memoryCache.Set(PRESCRIPTION_STATS_KEY, stats, cacheOptions);
        }

        return stats;
    }

    /// <summary>
    /// 清除相关缓存
    /// </summary>
    public void InvalidatePatientCache(Guid patientId)
    {
        // 清除患者列表缓存
        _memoryCache.Remove(PATIENT_LIST_KEY);
        
        // 清除患者详情缓存
        string cacheKey = string.Format(PATIENT_DETAIL_KEY, patientId);
        _distributedCache.Remove(cacheKey);
        
        // 清除处方统计缓存
        _memoryCache.Remove(PRESCRIPTION_STATS_KEY);
    }
}
```

## 🔒 数据库安全设计

### 1. 访问控制

#### 数据库用户权限设计
```sql
-- 创建应用用户
CREATE USER LYBT_App_User WITH PASSWORD = 'StrongPassword123!';

-- 创建只读用户（报表用途）
CREATE USER LYBT_Report_User WITH PASSWORD = 'ReadOnlyPassword123!';

-- 创建备份用户（备份用途）
CREATE USER LYBT_Backup_User WITH PASSWORD = 'BackupPassword123!';

-- 授予应用用户权限
GRANT SELECT, INSERT, UPDATE, DELETE ON Patients TO LYBT_App_User;
GRANT SELECT, INSERT, UPDATE, DELETE ON MedicalCases TO LYBT_App_User;
GRANT SELECT, INSERT, UPDATE, DELETE ON Prescriptions TO LYBT_App_User;
GRANT SELECT, INSERT, UPDATE, DELETE ON PrescriptionItems TO LYBT_App_User;
GRANT SELECT, INSERT, UPDATE, DELETE ON Consultations TO LYBT_App_User;
GRANT SELECT, INSERT, UPDATE, DELETE ON Herbs TO LYBT_App_User;
GRANT SELECT, INSERT, UPDATE, DELETE ON Formulas TO LYBT_App_User;
GRANT SELECT, INSERT, UPDATE, DELETE ON FormulaItems TO LYBT_App_User;

-- 授予认证表权限（受限）
GRANT SELECT, UPDATE ON Users TO LYBT_App_User;
GRANT SELECT, UPDATE ON AdminSecrets TO LYBT_App_User;

-- 授予只读用户权限
GRANT SELECT ON Patients TO LYBT_Report_User;
GRANT SELECT ON MedicalCases TO LYBT_Report_User;
GRANT SELECT ON Prescriptions TO LYBT_Report_User;
GRANT SELECT ON Herbs TO LYBT_Report_User;
GRANT SELECT ON Formulas TO LYBT_Report_User;

-- 授予备份用户权限
GRANT SELECT ON ALL OBJECTS TO LYBT_Backup_User;
GRANT BACKUP DATABASE TO LYBT_Backup_User;
```

### 2. 数据加密

#### 敏感数据加密
```sql
-- 创建加密函数
CREATE FUNCTION fn_EncryptSensitiveData(@data NVARCHAR(MAX))
RETURNS VARBINARY(MAX)
WITH SCHEMABINDING
AS
BEGIN
    RETURN ENCRYPTBYPASSPHRASE('LYBT_Encryption_Key_2024', @data);
END;

CREATE FUNCTION fn_DecryptSensitiveData(@encryptedData VARBINARY(MAX))
RETURNS NVARCHAR(MAX)
WITH SCHEMABINDING
AS
BEGIN
    RETURN CONVERT(NVARCHAR(MAX), DECRYPTBYPASSPHRASE('LYBT_Encryption_Key_2024', @encryptedData));
END;

-- 在表中使用加密
ALTER TABLE Patients ADD 
    EncryptedIdNumber VARBINARY(MAX) NULL,
    EncryptedPhoneNumber VARBINARY(MAX) NULL;

-- 创建触发器自动加密敏感数据
CREATE TRIGGER tr_Patients_EncryptSensitiveData
ON Patients
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 加密身份证号
    UPDATE p
    SET p.EncryptedIdNumber = dbo.fn_EncryptSensitiveData(i.IdNumber)
    FROM Patients p
    JOIN inserted i ON p.Id = i.Id
    WHERE i.IdNumber IS NOT NULL AND i.IdNumber <> '';
    
    -- 加密手机号
    UPDATE p
    SET p.EncryptedPhoneNumber = dbo.fn_EncryptSensitiveData(i.PhoneNumber)
    FROM Patients p
    JOIN inserted i ON p.Id = i.Id
    WHERE i.PhoneNumber IS NOT NULL AND i.PhoneNumber <> '';
END;
```

### 3. 审计日志

#### 审计表设计
```sql
-- 创建审计表
CREATE TABLE AuditLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(100) NOT NULL,
    RecordId UNIQUEIDENTIFIER NOT NULL,
    Action NVARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    ChangedColumns NVARCHAR(MAX) NULL,
    ChangedBy UNIQUEIDENTIFIER NOT NULL,
    ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IPAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    ApplicationName NVARCHAR(100) NULL,
    
    -- 索引
    INDEX IX_AuditLog_TableName (TableName),
    INDEX IX_AuditLog_RecordId (RecordId),
    INDEX IX_AuditLog_ChangedBy (ChangedBy),
    INDEX IX_AuditLog_ChangedAt (ChangedAt)
);

-- 创建审计触发器模板
CREATE TRIGGER tr_Audit_Patients
ON Patients
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 处理插入操作
    IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (TableName, RecordId, Action, NewValues, ChangedBy, ChangedAt)
        SELECT 
            'Patients',
            i.Id,
            'INSERT',
            (SELECT * FROM inserted FOR JSON PATH),
            i.CreatedBy,
            GETUTCDATE()
        FROM inserted i;
    END
    
    -- 处理更新操作
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO AuditLog (TableName, RecordId, Action, OldValues, NewValues, ChangedColumns, ChangedBy, ChangedAt)
        SELECT 
            'Patients',
            i.Id,
            'UPDATE',
            (SELECT * FROM deleted FOR JSON PATH),
            (SELECT * FROM inserted FOR JSON PATH),
            STRING_AGG(COLUMN_NAME, ',') WITHIN GROUP (ORDER BY ORDINAL_POSITION),
            i.UpdatedBy,
            GETUTCDATE()
        FROM inserted i
        JOIN deleted d ON i.Id = d.Id
        CROSS APPLY (
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'Patients'
              AND COLUMN_NAME IN ('Name', 'PhoneNumber', 'Address', 'Status')
              AND (SELECT COLUMN_VALUE FROM OPENJSON((SELECT * FROM deleted FOR JSON PATH)) WHERE [Key] = COLUMN_NAME) 
               <> (SELECT COLUMN_VALUE FROM OPENJSON((SELECT * FROM inserted FOR JSON PATH)) WHERE [Key] = COLUMN_NAME)
        ) changed_columns;
    END
    
    -- 处理删除操作
    IF EXISTS (SELECT * FROM deleted) AND NOT EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditLog (TableName, RecordId, Action, OldValues, ChangedBy, ChangedAt)
        SELECT 
            'Patients',
            d.Id,
            'DELETE',
            (SELECT * FROM deleted FOR JSON PATH),
            d.UpdatedBy,
            GETUTCDATE()
        FROM deleted d;
    END
END;
```

## 📊 数据库维护策略

### 1. 备份策略

#### 自动化备份方案
```sql
-- 创建备份存储过程
CREATE PROCEDURE sp_CreateDatabaseBackup
    @BackupPath NVARCHAR(500),
    @BackupType NVARCHAR(20) = 'FULL' -- FULL, DIFFERENTIAL, LOG
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @FileName NVARCHAR(500);
    DECLARE @DatabaseName NVARCHAR(100) = DB_NAME();
    DECLARE @TimeStamp NVARCHAR(20) = REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR(20), GETDATE(), 120), '-', ''), ' ', '_'), ':', '');
    
    SET @FileName = @BackupPath + '\' + @DatabaseName + '_' + @BackupType + '_' + @TimeStamp + '.bak';
    
    IF @BackupType = 'FULL'
    BEGIN
        BACKUP DATABASE @DatabaseName 
        TO DISK = @FileName 
        WITH COMPRESSION, CHECKSUM, STATS = 10;
        
        PRINT '完整备份已完成: ' + @FileName;
    END
    ELSE IF @BackupType = 'DIFFERENTIAL'
    BEGIN
        BACKUP DATABASE @DatabaseName 
        TO DISK = @FileName 
        WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, STATS = 10;
        
        PRINT '差异备份已完成: ' + @FileName;
    END
    ELSE IF @BackupType = 'LOG'
    BEGIN
        BACKUP LOG @DatabaseName 
        TO DISK = @FileName 
        WITH COMPRESSION, CHECKSUM, STATS = 10;
        
        PRINT '事务日志备份已完成: ' + @FileName;
    END
END;

-- 创建备份作业
DECLARE @BackupPath NVARCHAR(500) = 'D:\Database\Backups';

-- 每日完整备份（凌晨2点）
EXEC sp_add_job 
    @job_name = 'Daily Full Backup',
    @enabled = 1,
    @description = '每日完整数据库备份';

EXEC sp_add_jobstep 
    @job_name = 'Daily Full Backup',
    @step_name = 'Execute Full Backup',
    @subsystem = 'TSQL',
    @command = 'EXEC sp_CreateDatabaseBackup @BackupPath = ''' + @BackupPath + ''', @BackupType = ''FULL''',
    @database_name = 'LYBT_DB';

EXEC sp_add_jobschedule 
    @job_name = 'Daily Full Backup',
    @name = 'Daily 2AM Schedule',
    @freq_type = 4, -- daily
    @freq_interval = 1,
    @active_start_time = 20000; -- 2:00 AM

EXEC sp_add_jobserver 
    @job_name = 'Daily Full Backup',
    @server_name = @@SERVERNAME;
```

### 2. 性能监控

#### 性能监控查询
```sql
-- 查看当前执行情况
SELECT 
    r.session_id,
    r.status,
    r.command,
    r.cpu_time,
    r.total_elapsed_time,
    r.reads,
    r.writes,
    t.text,
    SUBSTRING(t.text, (r.statement_start_offset/2)+1, 
        ((CASE r.statement_end_offset
            WHEN -1 THEN DATALENGTH(t.text)
            ELSE r.statement_end_offset END
            - r.statement_start_offset)/2) + 1) AS statement_text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id > 50
ORDER BY r.cpu_time DESC;

-- 查看索引使用情况
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates,
    s.last_user_seek,
    s.last_user_scan
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats s ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
ORDER BY s.user_seeks + s.user_scans + s.user_lookups DESC;

-- 查看表大小统计
SELECT 
    t.name AS TableName,
    p.rows AS RowCounts,
    SUM(a.total_pages) * 8.0 / 1024 AS TotalSpaceMB,
    SUM(a.used_pages) * 8.0 / 1024 AS UsedSpaceMB,
    (SUM(a.total_pages) - SUM(a.used_pages)) * 8.0 / 1024 AS UnusedSpaceMB
FROM sys.tables t
INNER JOIN sys.indexes i ON t.object_id = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.is_ms_shipped = 0
    AND i.object_id > 255
GROUP BY t.name, p.rows
ORDER BY TotalSpaceMB DESC;
```

### 3. 数据清理策略

#### 历史数据归档
```sql
-- 创建归档表
CREATE TABLE MedicalCases_Archive (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    CaseNumber NVARCHAR(20) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    ChiefComplaint NVARCHAR(MAX) NOT NULL,
    Diagnosis NVARCHAR(500) NULL,
    Status INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    ArchivedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- 创建归档存储过程
CREATE PROCEDURE sp_ArchiveMedicalCases
    @ArchiveDays INT = 730, -- 默认归档2年前的数据
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ArchivedCount INT = 0;
    DECLARE @TotalCount INT = 0;
    
    -- 获取需要归档的总数
    SELECT @TotalCount = COUNT(*)
    FROM MedicalCases
    WHERE Status = 3 -- Completed
      AND CreatedAt < DATEADD(DAY, -@ArchiveDays, GETDATE());
    
    PRINT '需要归档的医案总数: ' + CAST(@TotalCount AS VARCHAR(10));
    
    WHILE @ArchivedCount < @TotalCount
    BEGIN
        -- 分批归档
        INSERT INTO MedicalCases_Archive (
            Id, PatientId, DoctorId, CaseNumber, Title, ChiefComplaint,
            Diagnosis, Status, CreatedAt, UpdatedAt
        )
        SELECT TOP (@BatchSize)
            Id, PatientId, DoctorId, CaseNumber, Title, ChiefComplaint,
            Diagnosis, Status, CreatedAt, UpdatedAt
        FROM MedicalCases
        WHERE Status = 3
          AND CreatedAt < DATEADD(DAY, -@ArchiveDays, GETDATE())
          AND Id NOT IN (SELECT Id FROM MedicalCases_Archive);
        
        SET @ArchivedCount = @ArchivedCount + @@ROWCOUNT;
        
        -- 删除已归档的数据
        DELETE FROM PrescriptionItems
        WHERE PrescriptionId IN (
            SELECT pr.Id 
            FROM Prescriptions pr
            WHERE pr.MedicalCaseId IN (
                SELECT Id 
                FROM MedicalCases 
                WHERE Status = 3
                  AND CreatedAt < DATEADD(DAY, -@ArchiveDays, GETDATE())
                  AND Id IN (SELECT Id FROM MedicalCases_Archive)
            )
        );
        
        DELETE FROM Prescriptions
        WHERE MedicalCaseId IN (
            SELECT Id 
            FROM MedicalCases 
            WHERE Status = 3
              AND CreatedAt < DATEADD(DAY, -@ArchiveDays, GETDATE())
              AND Id IN (SELECT Id FROM MedicalCases_Archive)
        );
        
        DELETE FROM Consultations
        WHERE MedicalCaseId IN (
            SELECT Id 
            FROM MedicalCases 
            WHERE Status = 3
              AND CreatedAt < DATEADD(DAY, -@ArchiveDays, GETDATE())
              AND Id IN (SELECT Id FROM MedicalCases_Archive)
        );
        
        DELETE FROM MedicalCases
        WHERE Status = 3
          AND CreatedAt < DATEADD(DAY, -@ArchiveDays, GETDATE())
          AND Id IN (SELECT Id FROM MedicalCases_Archive);
        
        PRINT '已归档 ' + CAST(@ArchivedCount AS VARCHAR(10)) + ' 条记录';
        
        -- 提交事务，避免长事务
        WAITFOR DELAY '00:00:01'; -- 等待1秒
    END
    
    PRINT '归档完成，共归档 ' + CAST(@ArchivedCount AS VARCHAR(10)) + ' 条记录';
END;
```

---

## 📚 数据库设计最佳实践

### ✅ 推荐做法

1. **规范化设计**
   - 遵循第三范式（3NF）
   - 合理使用反规范化提高性能
   - 避免数据冗余和更新异常

2. **索引策略**
   - 为常用查询创建复合索引
   - 使用覆盖索引减少IO操作
   - 定期分析索引使用情况

3. **事务管理**
   - 保持事务简短
   - 避免长时间锁表
   - 使用适当的隔离级别

4. **安全防护**
   - 实施最小权限原则
   - 加密敏感数据
   - 启用审计日志

5. **性能优化**
   - 使用参数化查询
   - 避免SELECT * 查询
   - 合理使用缓存

### ❌ 避免做法

1. **过度设计**
   - 不必要的复杂约束
   - 过度的规范化
   - 过多的索引

2. **性能问题**
   - N+1查询问题
   - 缺少适当的索引
   - 大事务操作

3. **安全风险**
   - 明文存储敏感信息
   - 过度的数据库权限
   - 缺少审计机制

4. **维护困难**
   - 命名不规范
   - 缺少文档说明
   - 复杂的外键关系

---

*此数据库设计指南基于凌隐宝堂中医诊所项目的11个核心实体编写，为数据库设计、优化和维护提供完整的指导原则和最佳实践。*