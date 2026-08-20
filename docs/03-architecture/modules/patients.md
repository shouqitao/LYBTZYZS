# Patients 模块设计

> 复杂度: 6/10 | 状态: 草稿

## 概述

Patients 管理患者信息，支持拼音搜索、身份证读卡器集成、敏感数据加密。

**职责**: 患者 CRUD/搜索/批量导入导出、拼音首字母自动生成、身份证读卡器集成（去重链）。

**依赖**: 上游无，下游 MedicalCase+Registration。

## API 端点

| HTTP | 路由 | 权限 |
|------|------|------|
| GET | `/api/v1/patients` | Doctor/Receptionist/Admin |
| POST | `/api/v1/patients` | Doctor/Receptionist |
| PUT | `/api/v1/patients/{id}` | Doctor/Receptionist |
| DELETE | `/api/v1/patients/{id}` | Admin+ |
| POST | `/api/v1/patients/{id}/restore` | Admin |
| POST | `/api/v1/patients/batch-import` | Admin+ |
| GET | `/api/v1/patients/by-id-number/{idNumber}` | Doctor/Receptionist/Admin |

## 独有设计

### 拼音搜索
创建/更新时自动生成 PinYinCode（PinYinHelper）。查询时 `Name.Contains(kw) || PinYinCode.Contains(kw)`。

### 身份证读卡器集成
```
硬件读取(HuaDa HD100 P/Invoke) → IdNumber 精确匹配 → 未找到则快速创建 → 加密存储照片
```

## 业务规则

| 规则 | 描述 |
|------|------|
| 拼音自动生成 | Name 变更时自动更新 PinYinCode |
| 手机号唯一 | 创建/更新时校验 |
| 身份证号唯一 | 创建/更新时校验 |
| 状态切换保护 | 有未完成医案时禁止禁用 |

## 已知问题

- 单删无引用检查（被医案引用的患者可删），批量删反而有检查
