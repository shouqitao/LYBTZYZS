# Data Consistency Check Report

**Execution Time**: 2025-09-16 15:39:36
**WebAPI URL**: http://localhost:8080

## Summary

- **Total Issues Found**: 2
- **Issues Fixed**: 1
- **Status**: CONDITIONAL_PASS

## Fixed Issues
- 鉁?Auth-Validate endpoint 405 error fixed

## Outstanding Issues
- 鉂?**Users Data Consistency**: 1 issue(s)
  - shouqitao - Missing email - ff52d7a6-e232-4dae-b717-20019fc6d8e0
- 鉂?**Patients Data Consistency**: 1 issue(s)
  - e83cc68f-55d4-48c2-a525-d647cad4cc4e - Missing birthDate - Zhang San

## Recommendations
- Review and fix data consistency issues before production deployment
- Implement validation rules to prevent future data inconsistencies
- Consider running data cleanup scripts for existing invalid data

---
*Data Consistency Check Report Generated: 2025-09-16 15:39:36*
*Script: data-consistency-check.ps1*
