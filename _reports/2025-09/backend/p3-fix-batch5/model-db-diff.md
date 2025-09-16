# Entity-Database Model Comparison Report

**Execution Time**: 2025-09-16 11:33:28
**Analysis Scope**: Core entity models vs database schema
**Purpose**: P3-Fix Batch5 Entity-DTO alignment

## Executive Summary

- **Entities Analyzed**: 2
- **Total Issues Found**: 5
- **Critical Issues**: 0
- **Medium Issues**: 3  
- **Low Issues**: 2

## Key Findings

### 馃幆 Primary Issues for P3-Fix Batch5

1. **Naming Inconsistencies**:
   - Users entity uses RealName (DTOs/scripts may expect FullName)
   - Patients entity uses Name (DTOs/scripts may expect PatientName)

2. **Computed Fields**:
   - Patients.Age is [NotMapped] calculated from BirthDate
   - Validation scripts should check BirthDate, not Age directly

3. **Data Quality Alignment**:
   - Users.Email field exists (P3-Fix Batch4 found missing email data)
   - Patients.BirthDate is the source of truth for age validation

## Detailed Analysis

### Patients Entity

**Table**: Patients
**Status**: 鈿狅笍  3 issue(s) found
**Total Fields**: 23
**NotMapped Fields**: Age

#### Entity Field Definition
| Field Name | Type | Required | Max Length | Notes |
|------------|------|----------|------------|-------|
| MaritalStatus | int | Yes | N/A |  |
| Id | Guid | Yes | N/A |  |
| AllergyHistory | string | No | 500 |  |
| DisableReason | string | No | 128 |  |
| Name | string | Yes | 100 |  |
| BirthDate | DateTime | No | N/A |  |
| UpdateTime | DateTime | No | N/A |  |
| IdType | int | Yes | N/A |  |
| EmergencyContactPhone | string | No | N/A |  |
| LastVisitTime | DateTime | No | N/A |  |
| VisitCount | int | Yes | N/A |  |
| Gender | int | Yes | N/A |  |
| Address | string | No | 256 |  |
| UpdatedBy | Guid | No | N/A |  |
| PinYinCode | string | No | 20 |  |
| BloodType | int | Yes | N/A |  |
| Status | int | Yes | N/A |  |
| EmergencyContactRelation | string | No | N/A |  |
| PhoneNumber | string | No | 20 |  |
| EmergencyContactName | string | No | N/A |  |
| CreatedAt | DateTime | Yes | N/A |  |
| CreatedBy | Guid | No | N/A |  |
| IdNumber | string | No | 50 |  |
#### Issues Found
| Type | Severity | Field | Description | Action Required |
|------|----------|-------|-------------|-----------------|
| NamingConsistency | Medium | Name | Entity uses 'Name' field while scripts may expect 'PatientName' | Update DTOs and scripts to use Name not PatientName |
| ComputedField | Low | Age | Age is [NotMapped] computed field based on BirthDate | Ensure DTOs and scripts use BirthDate for age validation, not direct Age field |
| DataValidation | Medium | BirthDate | Age validation should be based on BirthDate calculation, not stored Age value | Update validation logic to check BirthDate range and calculate age dynamically |
### Users Entity

**Table**: Users
**Status**: 鈿狅笍  2 issue(s) found
**Total Fields**: 19
**NotMapped Fields**: 

#### Entity Field Definition
| Field Name | Type | Required | Max Length | Notes |
|------------|------|----------|------------|-------|
| FailedLoginCount | int | Yes | N/A |  |
| Id | Guid | Yes | N/A |  |
| PinYinCode | string | No | 50 |  |
| UpdateTime | DateTime | No | N/A |  |
| LockoutEnd | DateTime | No | N/A |  |
| Introduction | string | No | 1000 |  |
| Remark | string | No | 500 |  |
| LastLoginTime | DateTime | No | N/A |  |
| Username | string | Yes | 50 |  |
| Status | int | Yes | N/A |  |
| Role | int | Yes | N/A |  |
| LicenseNumber | string | No | 50 |  |
| PhoneNumber | string | No | 20 |  |
| CreatedTime | DateTime | Yes | N/A |  |
| RegistrationFee | decimal | No | N/A |  |
| PasswordHash | string | Yes | 256 |  |
| Email | string | No | 100 |  |
| RealName | string | Yes | 50 |  |
| Specialty | string | No | 200 |  |
#### Issues Found
| Type | Severity | Field | Description | Action Required |
|------|----------|-------|-------------|-----------------|
| DataQuality | Medium | Email | User entity correctly has Email field, no FullName field (as expected) | Validate data consistency scripts to check Email not FullName |
| NamingConsistency | Low | RealName | Entity uses 'RealName' while some DTOs/scripts may expect 'FullName' | Update DTOs and scripts to use RealName consistently |
## Recommendations for P3-Fix Batch5

### Immediate Actions (High Priority)

1. **Update Data Validation Scripts**:
   - Modify data-consistency-check.ps1 to check RealName instead of FullName for Users
   - Update Patient validation to check Name instead of PatientName
   - Change age validation to use BirthDate calculation instead of direct Age field

2. **DTO Alignment**:
   - Remove any FullName fields from User DTOs (use RealName)
   - Remove any PatientName fields from Patient DTOs (use Name)
   - Remove any direct Age fields from Patient DTOs (compute from BirthDate)

3. **Mapping Layer Updates**:
   - Update AutoMapper profiles to handle field name changes
   - Add round-trip tests for entity 鈫?DTO conversion
   - Ensure computed fields are properly handled

### Medium Priority

1. **Enum Storage Validation**:
   - Verify UserRole, CommonStatus, Gender enums stored as int
   - Ensure database constraints match entity validation

2. **String Length Consistency**:
   - Validate all StringLength attributes match database column definitions
   - Check decimal precision for RegistrationFee field

### Low Priority

1. **Documentation Updates**:
   - Update API documentation to reflect correct field names
   - Document computed field behavior for client developers

## Impact Assessment

**Breaking Changes**: Low - Field renames in DTOs only
**Data Migration**: None required - database schema already correct
**API Compatibility**: Maintained - only DTO field names change
**Client Impact**: Low - front-end should use correct field names

## Success Criteria

- [ ] Data validation scripts check correct entity field names
- [ ] DTOs aligned with entity field names and types  
- [ ] No references to non-existent fields (FullName, PatientName, direct Age)
- [ ] Computed fields properly handled in DTOs and mapping
- [ ] All entity 鈫?DTO round-trip tests pass

---
*Model-Database Comparison Report Generated: 2025-09-16 11:33:28*
*Script: model-db-diff.ps1*
*Purpose: P3-Fix Batch5 Entity-DTO alignment preparation*

