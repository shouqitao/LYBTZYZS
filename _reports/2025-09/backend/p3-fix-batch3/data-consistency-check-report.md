# Data Consistency Check Report

**Execution Time**: 2025-09-16 09:31:32
**WebAPI URL**: http://localhost:8080

## Summary

- **Total Issues Found**: 2
- **Issues Fixed**: 1
- **Status**: CONDITIONAL_PASS

## Fixed Issues
- 鉁?Auth-Validate endpoint 405 error fixed

## Outstanding Issues
- 鉂?**Users API Access**: 1 issue(s)
  - 远程服务器返回错误: (401) 未经授权。
- 鉂?**Patients API Access**: 1 issue(s)
  - 远程服务器返回错误: (401) 未经授权。

## Recommendations
- Review and fix data consistency issues before production deployment
- Implement validation rules to prevent future data inconsistencies
- Consider running data cleanup scripts for existing invalid data

---
*Data Consistency Check Report Generated: 2025-09-16 09:31:32*
*Script: data-consistency-check.ps1*
