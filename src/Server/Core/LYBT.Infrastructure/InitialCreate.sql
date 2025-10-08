IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AdminSecrets] (
    [Id] uniqueidentifier NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_AdminSecrets] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AuthSessions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(256) NOT NULL,
    [LoginTime] datetime2 NOT NULL,
    [LogoutTime] datetime2 NULL,
    [ExpiryTime] datetime2 NOT NULL,
    [IpAddress] nvarchar(45) NOT NULL,
    [UserAgent] nvarchar(500) NULL,
    [IsRevoked] bit NOT NULL,
    [Status] int NOT NULL,
    CONSTRAINT [PK_AuthSessions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Formulas] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Effect] nvarchar(500) NULL,
    [Usage] nvarchar(500) NULL,
    [Remark] nvarchar(500) NULL,
    [Property] nvarchar(300) NULL,
    [Status] int NOT NULL,
    [IsShared] bit NOT NULL DEFAULT CAST(0 AS bit),
    [Category] nvarchar(50) NULL,
    [FormulaType] int NOT NULL,
    [UserId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Formulas] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Herbs] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [PinYinCode] nvarchar(50) NULL,
    [Origin] nvarchar(100) NULL,
    [Spec] nvarchar(100) NULL,
    [Unit] nvarchar(10) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [CostPrice] decimal(18,2) NULL,
    [Effect] nvarchar(500) NULL,
    [Usage] nvarchar(500) NULL,
    [Remark] nvarchar(500) NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Herbs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [MedicalCases] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [PatientName] nvarchar(50) NOT NULL,
    [DoctorId] uniqueidentifier NOT NULL,
    [DoctorName] nvarchar(50) NOT NULL,
    [ConsultationDate] datetime2 NOT NULL,
    [Status] nvarchar(450) NOT NULL,
    [Remark] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_MedicalCases] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Patients] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [PinYinCode] nvarchar(20) NULL,
    [Gender] int NOT NULL,
    [MaritalStatus] int NOT NULL,
    [BirthDate] datetime2 NULL,
    [IdType] int NOT NULL,
    [IdNumber] nvarchar(50) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [Address] nvarchar(256) NULL,
    [AllergyHistory] nvarchar(500) NULL,
    [BloodType] int NOT NULL,
    [EmergencyContactName] nvarchar(max) NULL,
    [EmergencyContactPhone] nvarchar(max) NULL,
    [EmergencyContactRelation] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [DisableReason] nvarchar(128) NULL,
    [LastVisitTime] datetime2 NULL,
    [VisitCount] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SystemLogs] (
    [Id] int NOT NULL IDENTITY,
    [Timestamp] datetime2 NOT NULL,
    [Level] nvarchar(50) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Exception] nvarchar(max) NULL,
    [LoggerName] nvarchar(255) NULL,
    [UserId] uniqueidentifier NULL,
    [RequestId] nvarchar(36) NULL,
    [MachineName] nvarchar(100) NULL,
    [ThreadId] int NULL,
    [Properties] nvarchar(max) NULL,
    CONSTRAINT [PK_SystemLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [RealName] nvarchar(50) NOT NULL,
    [PinYinCode] nvarchar(50) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [Email] nvarchar(100) NULL,
    [Role] int NOT NULL,
    [Status] int NOT NULL,
    [PasswordHash] nvarchar(256) NOT NULL,
    [FailedLoginCount] int NOT NULL,
    [LockoutEnd] datetime2 NULL,
    [LastLoginTime] datetime2 NULL,
    [Remark] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Consultations] (
    [Id] uniqueidentifier NOT NULL,
    [ChiefComplaint] nvarchar(500) NULL,
    [PresentIllness] nvarchar(1000) NULL,
    [Inspection] nvarchar(500) NULL,
    [AuscultationOlfaction] nvarchar(500) NULL,
    [Inquiry] nvarchar(1000) NULL,
    [Palpation] nvarchar(500) NULL,
    [TCMDiagnosis] nvarchar(500) NULL,
    [TreatmentPrinciple] nvarchar(500) NULL,
    [MedicalAdvice] nvarchar(500) NULL,
    [Status] int NOT NULL,
    [Remark] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Consultations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Consultations_MedicalCases_Id] FOREIGN KEY ([Id]) REFERENCES [MedicalCases] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Prescriptions] (
    [Id] uniqueidentifier NOT NULL,
    [MedicalCaseId] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NULL,
    [UserId] uniqueidentifier NULL,
    [Indication] nvarchar(500) NULL,
    [DosageCount] int NOT NULL,
    [Discount] decimal(5,4) NOT NULL,
    [Advice] nvarchar(500) NULL,
    [FormulaSource] nvarchar(200) NULL,
    [Status] int NOT NULL,
    [Remark] nvarchar(500) NULL,
    [PrintVersion] int NOT NULL,
    [LastPrintedAt] datetime2 NULL,
    [PrintCount] int NOT NULL,
    [IsPrinted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Prescriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Prescriptions_MedicalCases_MedicalCaseId] FOREIGN KEY ([MedicalCaseId]) REFERENCES [MedicalCases] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [Token] nvarchar(500) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Jti] nvarchar(128) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedReason] nvarchar(200) NULL,
    [RevokedAt] datetime2 NULL,
    [RevokedBy] nvarchar(128) NULL,
    [ClientIp] nvarchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    [DeviceId] nvarchar(128) NULL,
    [DeviceName] nvarchar(200) NULL,
    [UsageCount] int NOT NULL,
    [LastUsedAt] datetime2 NULL,
    [ReplacedByToken] nvarchar(500) NULL,
    [FamilyId] nvarchar(128) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PrescriptionItems] (
    [Id] uniqueidentifier NOT NULL,
    [PrescriptionId] uniqueidentifier NOT NULL,
    [HerbId] uniqueidentifier NOT NULL,
    [HerbName] nvarchar(100) NOT NULL,
    [Quantity] int NOT NULL,
    [Unit] nvarchar(16) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Usage] nvarchar(200) NULL,
    [Remark] nvarchar(200) NULL,
    CONSTRAINT [PK_PrescriptionItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PrescriptionItems_Prescriptions_PrescriptionId] FOREIGN KEY ([PrescriptionId]) REFERENCES [Prescriptions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PrescriptionPrintLogs] (
    [Id] uniqueidentifier NOT NULL,
    [PrescriptionId] uniqueidentifier NOT NULL,
    [PrintVersion] int NOT NULL,
    [PrintedAt] datetime2 NOT NULL,
    [PrintedBy] uniqueidentifier NULL,
    [PrintedByName] nvarchar(50) NULL,
    [PrinterName] nvarchar(100) NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [Remark] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_PrescriptionPrintLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PrescriptionPrintLogs_Prescriptions_PrescriptionId] FOREIGN KEY ([PrescriptionId]) REFERENCES [Prescriptions] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'PasswordHash') AND [object_id] = OBJECT_ID(N'[AdminSecrets]'))
    SET IDENTITY_INSERT [AdminSecrets] ON;
INSERT INTO [AdminSecrets] ([Id], [PasswordHash])
VALUES ('00000000-0000-0000-0000-000000000001', N'AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ==');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'PasswordHash') AND [object_id] = OBJECT_ID(N'[AdminSecrets]'))
    SET IDENTITY_INSERT [AdminSecrets] OFF;
GO

CREATE INDEX [IX_AuthSessions_LoginTime] ON [AuthSessions] ([LoginTime]);
GO

CREATE INDEX [IX_AuthSessions_Status] ON [AuthSessions] ([Status]);
GO

CREATE INDEX [IX_AuthSessions_UserId] ON [AuthSessions] ([UserId]);
GO

CREATE INDEX [IX_Herbs_Name] ON [Herbs] ([Name]);
GO

CREATE INDEX [IX_Herbs_PinYinCode] ON [Herbs] ([PinYinCode]);
GO

CREATE INDEX [IX_MedicalCase_Doctor_Status] ON [MedicalCases] ([DoctorId], [Status]);
GO

CREATE INDEX [IX_MedicalCase_Patient_Date] ON [MedicalCases] ([PatientId], [CreatedAt]);
GO

CREATE INDEX [IX_MedicalCase_Status_Date] ON [MedicalCases] ([Status], [CreatedAt]);
GO

CREATE INDEX [IX_MedicalCases_CreatedAt] ON [MedicalCases] ([CreatedAt]);
GO

CREATE INDEX [IX_MedicalCases_DoctorId] ON [MedicalCases] ([DoctorId]);
GO

CREATE INDEX [IX_MedicalCases_Status] ON [MedicalCases] ([Status]);
GO

CREATE UNIQUE INDEX [UX_MedicalCases_Patient_ActiveOnly] ON [MedicalCases] ([PatientId]) WHERE [Status] = 'Active';
GO

CREATE INDEX [IX_Patient_CreatedAt] ON [Patients] ([CreatedAt]);
GO

CREATE INDEX [IX_Patient_IdNumber] ON [Patients] ([IdNumber]);
GO

CREATE INDEX [IX_Patient_Name_Phone] ON [Patients] ([Name], [PhoneNumber]);
GO

CREATE INDEX [IX_Patient_Phone] ON [Patients] ([PhoneNumber]);
GO

CREATE INDEX [IX_Patient_PinYin_Deleted] ON [Patients] ([PinYinCode], [IsDeleted]);
GO

CREATE INDEX [IX_PrescriptionItems_PrescriptionId] ON [PrescriptionItems] ([PrescriptionId]);
GO

CREATE INDEX [IX_PrescriptionPrintLogs_PrescriptionId] ON [PrescriptionPrintLogs] ([PrescriptionId]);
GO

CREATE INDEX [IX_PrescriptionPrintLogs_PrintedAt] ON [PrescriptionPrintLogs] ([PrintedAt]);
GO

CREATE INDEX [IX_Prescription_MedicalCase_Status] ON [Prescriptions] ([MedicalCaseId], [Status]);
GO

CREATE INDEX [IX_Prescription_Patient_Date] ON [Prescriptions] ([PatientId], [CreatedAt]);
GO

CREATE INDEX [IX_Prescription_Status] ON [Prescriptions] ([Status]);
GO

CREATE UNIQUE INDEX [UX_Prescriptions_MedicalCaseId] ON [Prescriptions] ([MedicalCaseId]);
GO

CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [RefreshTokens] ([ExpiresAt]);
GO

CREATE INDEX [IX_RefreshTokens_IsRevoked] ON [RefreshTokens] ([IsRevoked]);
GO

CREATE INDEX [IX_RefreshTokens_Jti] ON [RefreshTokens] ([Jti]);
GO

CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
GO

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO

CREATE INDEX [IX_RefreshTokens_UserId_IsRevoked] ON [RefreshTokens] ([UserId], [IsRevoked]);
GO

CREATE INDEX [IX_SystemLogs_Level] ON [SystemLogs] ([Level]);
GO

CREATE INDEX [IX_SystemLogs_LoggerName] ON [SystemLogs] ([LoggerName]);
GO

CREATE INDEX [IX_SystemLogs_Timestamp] ON [SystemLogs] ([Timestamp]);
GO

CREATE INDEX [IX_SystemLogs_Timestamp_Level] ON [SystemLogs] ([Timestamp], [Level]);
GO

CREATE INDEX [IX_SystemLogs_UserId] ON [SystemLogs] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_User_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL;
GO

CREATE INDEX [IX_User_Phone] ON [Users] ([PhoneNumber]);
GO

CREATE INDEX [IX_User_Role] ON [Users] ([Role]);
GO

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251007164429_InitialCreate', N'8.0.20');
GO

COMMIT;
GO

