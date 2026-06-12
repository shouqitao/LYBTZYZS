# Staging Deployment Guide - Frontend UX Optimization

**Project**: 凌隐宝堂中医诊所管理系统 (LYBTZYZS)  
**Initiative**: Frontend UX Optimization  
**Purpose**: Prepare staging environment for UAT (Alpha/Beta testing)  
**Date**: 2026-04-18  
**Status**: Ready for Deployment

---

## Overview

This guide provides step-by-step instructions for deploying the Frontend UX Optimization changes to a staging environment for User Acceptance Testing (UAT).

**Deployment Scope**:
- MedicalCase module (Compact mode only)
- 5-step WorkflowStepIndicator
- Enhanced Toast notifications
- Dynamic completeness checking
- Field-level validation feedback

---

## Prerequisites

### Infrastructure Requirements

- [ ] Windows Server 2019+ or Windows 10/11 Pro machine
- [ ] .NET 8.0 SDK or Runtime installed
- [ ] SQL Server 2019+ (or SQL Server Express)
- [ ] At least 4GB RAM available
- [ ] 10GB free disk space
- [ ] Network connectivity for client machines

### Software Requirements

- [ ] Git client (to pull source code)
- [ ] .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- [ ] Visual Studio 2022 17.8+ (for building) OR
- [ ] .NET CLI (for command-line build)
- [ ] SQL Server Management Studio (SSMS) - optional

### Access Requirements

- [ ] Database admin access (to create/restore database)
- [ ] File system access (for deployment folder)
- [ ] Network configuration (if deploying to remote server)

---

## Step 1: Prepare Environment

### 1.1 Create Deployment Directory

```powershell
# On staging server
New-Item -Path "C:\LYBTZYZS\Staging" -ItemType Directory -Force
New-Item -Path "C:\LYBTZYZS\Staging\App" -ItemType Directory -Force
New-Item -Path "C:\LYBTZYZS\Staging\Logs" -ItemType Directory -Force
New-Item -Path "C:\LYBTZYZS\Staging\Data" -ItemType Directory -Force
```

### 1.2 Create Application Settings

Create `appsettings.Staging.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=[SERVER_NAME];Database=LYBTZYZS_Staging;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Environment": "Staging",
  "EnableDetailedErrors": true,
  "EnableToastNotifications": true
}
```

---

## Step 2: Build Application

### 2.1 Clone or Pull Latest Code

```bash
cd C:\LYBTZYZS\Staging\Source
git pull origin main
git checkout pre-ux-optimization-backup
git checkout main
```

### 2.2 Restore NuGet Packages

```bash
cd src/Client/Desktop
dotnet restore LYBT.Desktop.sln
```

### 2.3 Build Solution

**Option A: Using Visual Studio**
1. Open `LYBT.Desktop.sln`
2. Select "Release" configuration
3. Build → Build Solution (Ctrl+Shift+B)
4. Verify no build errors

**Option B: Using .NET CLI**
```bash
dotnet build LYBT.Desktop.sln -c Release
```

### 2.4 Verify Build Output

Build output should be at:
```
src/Client/Desktop/Modules/LYBT.Desktop.App/bin/Release/net8.0-windows/
```

Key files to verify:
- [ ] `LYBT.Desktop.App.exe`
- [ ] `LYBT.Desktop.App.dll`
- [ ] All dependency DLLs present
- [ ] `appsettings.json` present

---

## Step 3: Deploy Application Files

### 3.1 Copy Build Output to Deployment Directory

```powershell
# Copy application files
Copy-Item -Path "src\Client\Desktop\Modules\LYBT.Desktop.App\bin\Release\net8.0-windows\*" -Destination "C:\LYBTZYZS\Staging\App\" -Recurse -Force

# Copy staging configuration
Copy-Item -Path "appsettings.Staging.json" -Destination "C:\LYBTZYZS\Staging\App\appsettings.json" -Force
```

### 3.2 Verify Deployment

```powershell
# Check application files exist
Get-ChildItem "C:\LYBTZYZS\Staging\App\LYBT.Desktop.App.exe"

# Verify file count (should be 100+ files)
(Get-ChildItem "C:\LYBTZYZS\Staging\App\" -Recurse -File).Count
```

---

## Step 4: Prepare Database

### 4.1 Create Staging Database

**Using SQL Server Management Studio**:
```sql
CREATE DATABASE LYBTZYZS_Staging;
GO

-- Create login if needed
CREATE USER [staging_user] FOR LOGIN [staging_login];
ALTER ROLE db_owner ADD MEMBER [staging_user];
GO
```

### 4.2 Run Database Migrations

```bash
cd src/Server
dotnet ef database update --connection "Server=[SERVER_NAME];Database=LYBTZYZS_Staging;Trusted_Connection=True;" --project LYBT.Server.Migrations
```

**Or run SQL scripts** if migrations are not available:
```sql
-- Run schema creation scripts
-- Run stored procedure scripts
-- Run seed data scripts
```

### 4.3 Seed Test Data

Execute seed script to populate test data:

```sql
-- Seed patients (3-5 test patients)
INSERT INTO Patients (Id, Name, Age, Gender, Phone) VALUES
(NEWID(), '测试患者1', 45, '男', '13800138001'),
(NEWID(), '测试患者2', 32, '女', '13800138002'),
(NEWID(), '测试患者3', 58, '男', '13800138003');

-- Seed formulas (验方)
INSERT INTO Formulas (Id, Name, Description) VALUES
(NEWID(), '补中益气汤', '脾胃虚弱，中气下陷'),
(NEWID(), '四物汤', '血虚证，面色萎黄'),
(NEWID(), '六味地黄丸', '肾阴虚，腰膝酸软');

-- Seed herbs (药材)
INSERT INTO Herbs (Id, Name, Pinyin, Category) VALUES
(NEWID(), '黄芪', 'Huang Qi', '补气药'),
(NEWID(), '党参', 'Dang Shen', '补气药'),
(NEWID(), '当归', 'Dang Gui', '补血药'),
(NEWID(), '熟地黄', 'Shu Di Huang', '补血药'),
(NEWID(), '白术', 'Bai Zhu', '补气药');
```

---

## Step 5: Configure Application

### 5.1 Update Connection String

Edit `C:\LYBTZYZS\Staging\App\appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=LYBTZYZS_Staging;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 5.2 Test Database Connectivity

Run a quick connection test:
```bash
cd C:\LYBTZYZS\Staging\App
.\LYBT.Desktop.App.exe --test-connection
```

Expected output:
```
[INFO] Database connection successful
[INFO] Found 5 patients
[INFO] Found 3 formulas
[INFO] Found 5 herbs
```

---

## Step 6: Create Test Accounts

### 6.1 Create Clinician Account

```sql
INSERT INTO Users (Id, Username, PasswordHash, Role, DisplayName) VALUES
(NEWID(), 'clinician1', '[HASHED_PASSWORD]', 'Clinician', '测试医师1');

-- Grant permissions
INSERT INTO UserPermissions (UserId, Permission) VALUES
((SELECT Id FROM Users WHERE Username = 'clinician1'), 'MedicalCase.Create'),
((SELECT Id FROM Users WHERE Username = 'clinician1'), 'MedicalCase.Edit'),
((SELECT Id FROM Users WHERE Username = 'clinician1'), 'MedicalCase.View'),
((SELECT Id FROM Users WHERE Username = 'clinician1'), 'MedicalCase.Complete');
```

### 6.2 Create Administrator Account

```sql
INSERT INTO Users (Id, Username, PasswordHash, Role, DisplayName) VALUES
(NEWID(), 'admin1', '[HASHED_PASSWORD]', 'Administrator', '测试管理员1');

-- Grant all permissions
INSERT INTO UserPermissions (UserId, Permission) VALUES
((SELECT Id FROM Users WHERE Username = 'admin1'), 'MedicalCase.Create'),
((SELECT Id FROM Users WHERE Username = 'admin1'), 'MedicalCase.Edit'),
((SELECT Id FROM Users WHERE Username = 'admin1'), 'MedicalCase.View'),
((SELECT Id FROM Users WHERE Username = 'admin1'), 'MedicalCase.Complete'),
((SELECT Id FROM Users WHERE Username = 'admin1'), 'MedicalCase.Delete'),
((SELECT Id FROM Users WHERE Username = 'admin1'), 'MedicalCase.Management');
```

**Default Test Credentials** (change these before production!):
- Clinician: `clinician1` / `Test@1234`
- Administrator: `admin1` / `Admin@1234`

---

## Step 7: Deploy Application

### 7.1 Create Desktop Shortcut (Optional)

For testers, create a shortcut to:
```
C:\LYBTZYZS\Staging\App\LYBT.Desktop.App.exe
```

### 7.2 Configure Firewall (if needed)

```powershell
# Allow application through Windows Firewall
New-NetFirewallRule -DisplayName "LYBTZYZS Staging" -Direction Inbound -Program "C:\LYBTZYZS\Staging\App\LYBT.Desktop.App.exe" -Action Allow
```

### 7.3 Set File Permissions

```powershell
# Grant Full Control to staging users
$acl = Get-Acl "C:\LYBTZYZS\Staging"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Everyone","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($accessRule)
Set-Acl "C:\LYBTZYZS\Staging" $acl
```

---

## Step 8: Smoke Testing

### 8.1 Launch Application

```powershell
cd C:\LYBTZYZS\Staging\App
.\LYBT.Desktop.App.exe
```

### 8.2 Verify Login

- [ ] Application launches successfully
- [ ] Login screen displays
- [ ] Can login with `clinician1` / `Test@1234`

### 8.3 Verify Patient List

- [ ] Patient list loads
- [ ] Test patients are visible
- [ ] Can search/filter patients

### 8.4 Verify New Case Creation

1. Select test patient
2. Click "开始看诊"
3. Verify:
   - [ ] MedicalCaseWorkspaceView opens
   - [ ] WorkflowStepIndicator shows Step 1 (四诊采集) active
   - [ ] No green checkmarks initially
   - [ ] Can type in PresentIllness field

### 8.5 Verify Step Indicator

1. Type ≥5 characters in PresentIllness
2. Verify:
   - [ ] Green checkmark appears next to field
   - [ ] Step indicator advances to Step 2 (中医辨证)
   - [ ] Smooth transition animation

### 8.6 Verify Toast Notifications

1. Fill in diagnosis
2. Click "暂存医案" (Suspend)
3. Verify:
   - [ ] Toast notification: "医案已暂存，可稍后继续"
   - [ ] Blue background (Info type)
   - [ ] Persists for 5 seconds
   - [ ] Smooth fade-out animation

### 8.7 Verify Completeness Check

1. Fill in all required fields
2. Verify:
   - [ ] CompletenessCheck shows "可以完成看诊" in green
   - [ ] "完成看诊" button becomes enabled
   - [ ] All validation states show green

### 8.8 Verify Prescription Workflow

1. Click "套验方" (Import Formula)
2. Select test formula
3. Click OK
4. Verify:
   - [ ] Toast: "已导入验方「XXX」，共N味药材"
   - [ ] Prescription items populated
   - [ ] Checkmark appears next to "共N味药材"
   - [ ] Step indicator advances to Step 5

### 8.9 Verify Case Completion

1. Click "完成看诊" (Complete)
2. Verify:
   - [ ] Toast: "看诊完成，医案已归档"
   - [ ] Returns to patient list
   - [ ] Case shows as "Completed"

---

## Step 9: UAT Preparation

### 9.1 Create UAT Documentation Folder

```powershell
New-Item -Path "C:\LYBTZYZS\Staging\Docs" -ItemType Directory -Force
```

### 9.2 Copy UAT Materials

Copy these files to `C:\LYBTZYZS\Staging\Docs\`:
- [ ] `integration-test-checklist-phase3-2.md`
- [ ] `user-acceptance-testing-plan-phase3-4.md`
- [ ] `frontend-ux-optimization-completion-report.md`
- [ ] This deployment guide

### 9.3 Create Quick Reference Guide

Create `C:\LYBTZYZS\Staging\Docs\QuickReference.txt`:

```
LYBTZYZS Frontend UX Optimization - Quick Reference
====================================================

Login Credentials:
- Clinician: clinician1 / Test@1234
- Administrator: admin1 / Admin@1234

Test Patients:
- 测试患者1 (45岁, 男)
- 测试患者2 (32岁, 女)
- 测试患者3 (58岁, 男)

Key Features:
1. 5-Step Workflow Indicator
2. Visual Success Indicators (green checkmarks)
3. Modern Toast Notifications (4-5 seconds)
4. Dynamic Completeness Checking
5. Enhanced Loading Messages

Common Workflows:
- Create New Case: Select Patient → 开始看诊
- Import Formula: 套验方 → Select Formula → OK
- Suspend Case: 暂存医案 (can resume later)
- Complete Case: 完成看诊 (archives case)

Reporting Issues:
- Email: uat-support@example.com
- Form: [Link to issue tracker]
- Daily Standup: [Time for Alpha testers]
```

---

## Step 10: Pre-UAT Checklist

### Application Readiness

- [ ] Application builds without errors
- [ ] Application launches successfully
- [ ] Login functionality works
- [ ] Database connectivity verified
- [ ] All test data seeded (patients, formulas, herbs)

### Feature Verification

- [ ] WorkflowStepIndicator displays correctly
- [ ] Field validation feedback works
- [ ] Toast notifications appear with correct timing
- [ ] CompletenessCheck updates in real-time
- [ ] All commands (Save, Suspend, Complete) work

### Documentation Readiness

- [ ] UAT plan distributed to testers
- [ ] Quick reference guide created
- [ ] Issue reporting system configured
- [ ] Test accounts created and credentials shared

### Communication Channels

- [ ] UAT email distribution list configured
- [ ] Daily standup scheduled (Alpha)
- [ ] Weekly check-in scheduled (Beta)
- [ ] Support contact info distributed

---

## Step 11: Go/No-Go for UAT

### Go Criteria (Proceed to Alpha UAT)

- [ ] All smoke tests pass (Section 8)
- [ ] Zero critical bugs (P0)
- [ ] ≤ 3 high-priority bugs (P1) with acceptable workarounds
- [ ] Test data fully seeded
- [ ] Test accounts functional
- [ ] Documentation complete

### No-Go Criteria (Delay UAT)

- [ ] Application fails to launch
- [ ] Database connectivity issues
- [ ] Critical workflow broken
- [ ] Data loss bugs present
- [ ] Security vulnerabilities identified

---

## Rollback Plan

If critical issues are found during deployment:

### Option 1: Restore Previous Version

```bash
cd C:\LYBTZYZS\Source
git checkout pre-ux-optimization-backup
# Rebuild and redeploy
```

### Option 2: Restore Database

```sql
-- Restore from backup
RESTORE DATABASE LYBTZYZS_Staging 
FROM DISK = 'C:\Backups\LYBTZYZS_Staging_BeforeUX.bak'
WITH REPLACE;
```

### Option 3: Disable New Features

Temporarily disable features via configuration:
```json
{
  "Features": {
    "EnableWorkflowStepIndicator": false,
    "EnableToastNotifications": false,
    "EnableCompletenessCheck": false
  }
}
```

---

## Monitoring and Logging

### Application Logs

Logs location: `C:\LYBTZYZS\Staging\Logs\`

Monitor daily:
- Error logs
- Performance metrics
- User activity
- Toast notification failures

### Key Metrics to Track

- Application startup time
- Case creation time
- Toast notification display rate
- Validation feedback responsiveness
- User-reported issues

---

## Support Contacts

**Deployment Issues**: [DevOps Contact]  
**Database Issues**: [DBA Contact]  
**Application Bugs**: [Development Lead]  
**UAT Coordination**: [UAT Coordinator]  

---

## Post-Deployment Checklist

### Immediate (Day 1)

- [ ] Verify application accessible from all tester machines
- [ ] Conduct first test session with Alpha testers
- [ ] Collect initial feedback
- [ ] Address any critical deployment issues

### Week 1 (Alpha Testing)

- [ ] Daily standup meetings (15 minutes)
- [ ] Track all issues in issue tracker
- [ ] Fix P0/P1 issues within 24 hours
- [ ] Document all workarounds

### Week 2-3 (Beta Testing)

- [ ] Weekly check-in meetings (30 minutes)
- [ ] Monitor usage patterns
- [ ] Collect feedback questionnaires
- [ ] Prepare Go/No-Go recommendation

---

## Next Steps After Successful Deployment

1. **Alpha Testing** (Week 1)
   - 2 clinicians test daily
   - Daily feedback sessions
   - Quick iteration on issues

2. **Beta Testing** (Weeks 2-3)
   - 5 clinicians use in real scenarios
   - Weekly feedback collection
   - Final issue resolution

3. **Go/No-Go Decision** (End of Week 3)
   - Analyze all feedback
   - Compare against success criteria
   - Make production rollout decision

---

**Deployment Checklist Version**: 1.0  
**Last Updated**: 2026-04-18  
**Next Review**: After Alpha UAT completion

---

## Appendix: Useful Commands

### Database Backup

```bash
sqlcmd -S [SERVER_NAME] -Q "BACKUP DATABASE LYBTZYZS_Staging TO DISK = 'C:\Backups\LYBTZYZS_Staging_$(Get-Date -Format 'yyyyMMdd').bak'"
```

### Database Restore

```bash
sqlcmd -S [SERVER_NAME] -Q "RESTORE DATABASE LYBTZYZS_Staging FROM DISK = 'C:\Backups\LYBTZYZS_Staging_20260418.bak' WITH REPLACE"
```

### Application Restart

```powershell
Stop-Process -Name "LYBT.Desktop.App" -Force
Start-Process "C:\LYBTZYZS\Staging\App\LYBT.Desktop.App.exe"
```

### Log Monitoring

```powershell
Get-Content "C:\LYBTZYZS\Staging\Logs\*.log" -Tail 50 -Wait
```
