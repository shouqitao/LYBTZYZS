# Sprint 3 - 调研发现

## 审计结论 (2026-03-08)

### 已完成 US (无需工作)
| US | 原因 |
|----|------|
| US-MC-015 | 打印触发已实现 (CODE-02 已修复) |
| US-HERB-008 | 批量删除 + 引用检查 (CODE-11) 均已实现 |
| US-MC-010 | 跨医案搜索已完成; 延期 EditModeStateMachine 为独立 MC-19 |

### CODE-08: 价格同步架构缺陷
- **根因**: PrescriptionImportExtensions 不填充 UnitPrice，委托给 UI 层被动同步
- **验方导入**: ToPrescriptionItemDtos() 不填 UnitPrice，依赖 HerbListControl.OnSelectedHerbChanged()
- **历史复制**: 直接复制历史价格，未刷新为当前价格
- **修复方向**: Service 层主动查询当前价格填充，不依赖 UI 事件

### CODE-22: 患者状态缺活跃医案检查
- PatientService.ToggleStatusAsync() 未查询关联医案
- 需查询 MedicalCases WHERE PatientId=id AND Status IN (Active, Draft)
- 有活跃医案时返回 422

### US-REG-007: 挂号历史缺高级过滤
- GetPagedAsync 仅支持 keyword 搜索
- 缺: startDate, endDate, patientId, doctorId 参数
- 需修改 Repository + Service + Controller 4 层

### US-PRINT-001: 三个修复项
- CODE-24: PrescriptionPrintService.PrintAsync() 不校验空处方
- CODE-36: A4 模板使用 A5 缩放版，非独立适配 (边距/字号不对)
- CODE-37: 药名截断用 TextTrimming=WordEllipsis，应为 10 字符截断

### US-AUTH-013: 3 个缺失事件
- LoginStarted: 登录开始前发布
- LogoutStarted: 登出开始前发布
- SessionExtended: Token 刷新成功时发布
- SessionExpiring 已被 simplify-auth 设计移除，不实现
