# .pen 设计稿模板审计报告
> 日期：2026-08-23 | 样板：patient-list.pen

## 总览
| 维度 | P0 | P1 | P2 | 合计 |
| 顶部 | 8 | 0 | 0 | 8 |
| 左侧 | 8 | 0 | 0 | 8 |
| 底部 | 8 | 0 | 0 | 8 |
| 中间 | 0 | 0 | 0 | 0 |
| 变量表 | 0 | 88 | 20 | 108 |
| 品牌 | 0 | 0 | 12 | 12 |
| **合计** | **24** | **88** | **32** | **144** |

## 问题详情
### dialog-common.pen
- [P1] 变量表 : 缺失 $bg=#FBF7F3
- [P1] 变量表 : 缺失 $primary-dark=#4E342E
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P1] 变量表 : 缺失 $text-primary=#3E2723
- [P1] 变量表 : 缺失 $text-secondary=#8D6E63
- [P1] 变量表 : 缺失 $border=#E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3

### dialog-formula-import.pen
- [P1] 变量表 : 缺失 $bg=#FBF7F3
- [P1] 变量表 : 缺失 $primary-dark=#4E342E
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P2] 变量表 : $text-primary=#212121 应为 #3E2723
- [P2] 变量表 : $text-secondary=#757575 应为 #8D6E63
- [P1] 变量表 : 缺失 $border=#E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P1] 变量表 : 缺失 $success=#2E7D32
- [P0] 顶部 Import Formula Dialog: 缺失顶部应用栏
- [P0] 左侧 Import Formula Dialog: 缺失左侧导航
- [P0] 底部 Import Formula Dialog: 缺失底部状态栏

### dialog-history-copy.pen
- [P1] 变量表 : 缺失 $bg=#FBF7F3
- [P1] 变量表 : 缺失 $surface=#FFFFFF
- [P1] 变量表 : 缺失 $primary=#6D4C41
- [P1] 变量表 : 缺失 $primary-dark=#4E342E
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P2] 变量表 : $text-primary=#212121 应为 #3E2723
- [P2] 变量表 : $text-secondary=#757575 应为 #8D6E63
- [P1] 变量表 : 缺失 $border=#E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P1] 变量表 : 缺失 $success=#2E7D32
- [P0] 顶部 Modal Overlay: 缺失顶部应用栏
- [P0] 左侧 Modal Overlay: 缺失左侧导航
- [P0] 底部 Modal Overlay: 缺失底部状态栏

### dialog-registration-create.pen
- [P1] 变量表 : 缺失 $bg=#FBF7F3
- [P1] 变量表 : 缺失 $surface=#FFFFFF
- [P1] 变量表 : 缺失 $primary=#6D4C41
- [P1] 变量表 : 缺失 $primary-dark=#4E342E
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P1] 变量表 : 缺失 $text-primary=#3E2723
- [P1] 变量表 : 缺失 $text-secondary=#8D6E63
- [P1] 变量表 : 缺失 $border=#E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P1] 变量表 : 缺失 $success=#2E7D32
- [P0] 顶部 Dialog Overlay: 缺失顶部应用栏
- [P0] 左侧 Dialog Overlay: 缺失左侧导航
- [P0] 底部 Dialog Overlay: 缺失底部状态栏

### form-formula-edit.pen
- [P1] 变量表 : 缺失 $bg=#FBF7F3
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P2] 变量表 : $text-primary=#212121 应为 #3E2723
- [P2] 变量表 : $text-secondary=#757575 应为 #8D6E63
- [P2] 变量表 : $border=#E0E0E0 应为 #E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P1] 变量表 : 缺失 $success=#2E7D32
- [P0] 顶部 验方编辑页面: 缺失顶部应用栏
- [P0] 左侧 验方编辑页面: 缺失左侧导航
- [P0] 底部 验方编辑页面: 缺失底部状态栏
- [P2] 品牌 验方编辑页面: 非标准名「凌隐宝堂」

### form-herb-edit.pen
- [P2] 变量表 : $bg=#F5F5F5 应为 #FBF7F3
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P2] 变量表 : $text-primary=#212121 应为 #3E2723
- [P2] 变量表 : $text-secondary=#757575 应为 #8D6E63
- [P1] 变量表 : 缺失 $border=#E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P1] 变量表 : 缺失 $success=#2E7D32
- [P0] 顶部 Herb Edit Form: 缺失顶部应用栏
- [P0] 左侧 Herb Edit Form: 缺失左侧导航
- [P0] 底部 Herb Edit Form: 缺失底部状态栏
- [P2] 品牌 Herb Edit Form: 非标准名「凌隐宝堂」

### form-medical-case-edit.pen
- [P2] 变量表 : $bg=#F5F5F5 应为 #FBF7F3
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P2] 变量表 : $text-primary=#212121 应为 #3E2723
- [P2] 变量表 : $text-secondary=#757575 应为 #8D6E63
- [P1] 变量表 : 缺失 $border=#E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P0] 顶部 医案编辑页面: 缺失顶部应用栏
- [P0] 左侧 医案编辑页面: 缺失左侧导航
- [P0] 底部 医案编辑页面: 缺失底部状态栏

### form-patient-edit.pen
- [P2] 变量表 : $bg=#F5F5F5 应为 #FBF7F3
- [P2] 变量表 : $primary-dark=#3E2723 应为 #4E342E
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P2] 变量表 : $text-primary=#212121 应为 #3E2723
- [P2] 变量表 : $text-secondary=#757575 应为 #8D6E63
- [P1] 变量表 : 缺失 $border=#E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P1] 变量表 : 缺失 $success=#2E7D32
- [P0] 顶部 Patient Edit Form: 缺失顶部应用栏
- [P0] 左侧 Patient Edit Form: 缺失左侧导航
- [P0] 底部 Patient Edit Form: 缺失底部状态栏
- [P2] 品牌 Patient Edit Form: 非标准名「凌隐宝堂」

### form-user-edit.pen
- [P1] 变量表 : 缺失 $bg=#FBF7F3
- [P1] 变量表 : 缺失 $surface=#FFFFFF
- [P1] 变量表 : 缺失 $primary-dark=#4E342E
- [P1] 变量表 : 缺失 $primary-soft=#F1E9E4
- [P1] 变量表 : 缺失 $accent=#FFB300
- [P1] 变量表 : 缺失 $accent-dark=#8A5A00
- [P2] 变量表 : $text-primary=#212121 应为 #3E2723
- [P2] 变量表 : $text-secondary=#757575 应为 #8D6E63
- [P2] 变量表 : $border=#E0E0E0 应为 #E9DFD7
- [P1] 变量表 : 缺失 $on-primary=#FFFFFF
- [P1] 变量表 : 缺失 $font-ui=Noto Sans SC
- [P1] 变量表 : 缺失 $bg-warm=#FBF7F3
- [P1] 变量表 : 缺失 $success=#2E7D32
- [P0] 顶部 Edit User Page: 缺失顶部应用栏
- [P0] 左侧 Edit User Page: 缺失左侧导航
- [P0] 底部 Edit User Page: 缺失底部状态栏
- [P2] 品牌 Edit User Page: 非标准名「凌隐宝堂」

### medical-case.pen
- [P2] 品牌 医案工作台: 非标准名「凌隐宝堂 · 中医医案工作台」
- [P2] 品牌 医案工作台-收拢: 非标准名「凌隐宝堂 · 中医医案工作台」

### patient-selection.pen
- [P2] 品牌 患者选择页面: 非标准名「凌隐宝堂 · 患者选择」
- [P2] 品牌 患者选择页面-收拢: 非标准名「凌隐宝堂 · 患者选择」

### receptionist-home.pen
- [P2] 品牌 前台工作台首页: 非标准名「凌隐宝堂 · 前台工作台」
- [P2] 品牌 前台工作台首页-收拢: 非标准名「凌隐宝堂 · 前台工作台」

### server-config.pen
- [P2] 品牌 Server Config Page: 非标准名「凌隐宝堂」
- [P2] 品牌 Server Config Page-收拢: 非标准名「凌隐宝堂」

## 无问题文件（14个）
- account-settings.pen
- admin-home.pen
- audit-log.pen
- backup-management.pen
- cardreader-diagnostics.pen
- clinical-workspace.pen
- data-import-export.pen
- deployment.pen
- formula-management.pen
- herb-management.pen
- log-level.pen
- medical-case-management.pen
- patient-list.pen
- registration.pen
- reports.pen
- security-audit-log.pen
- sysadmin-home.pen
- system-settings.pen
- user-management.pen

## 排除文件（组件/对话框/登录/向导）
- first-run.pen
- login.pen
- main-window.pen