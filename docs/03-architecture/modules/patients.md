# Patients 模块设计

> v1.0 | 2026-06-28

## 模块概述

Patients 管理患者档案全生命周期：CRUD、拼音搜索、身份证读卡器集成、敏感数据脱敏、导入导出。

**职责边界**：患者档案管理（创建→查询→更新→软删除/恢复）

## 接口契约

| 端点 | Service | 权限 | 关键功能 |
|------|---------|------|----------|
| GET /patients | GetPagedAsync | DoctorOrReceptionist | 分页+拼音搜索 |
| GET /patients/{id} | GetByIdAsync | DoctorOrReceptionist | 患者详情 |
| POST /patients | CreateAsync | DoctorOrReceptionist | 身份证去重检查 |
| PUT /patients/{id} | UpdateAsync | DoctorOrReceptionist | |
| DELETE /patients/{id} | SoftDeleteAsync | DoctorOrReceptionist | BR-DEL-001引用检查 |
| POST /patients/{id}/restore | RestoreAsync | Admin | 软删除恢复 |
| POST /patients/batch-delete | BatchDeleteAsync | DoctorOrReceptionist | 批量删除 |

## 关键业务规则

| 规则 | 约束 | US |
|------|------|-----|
| 身份证去重 | 同一IdNumber不允许重复建档(比对姓名+IdNumber+BirthDate) | US-PAT-001 |
| 拼音搜索 | 按姓名拼音首字母检索 | US-PAT-003 |
| 敏感数据 | IdNumber/Phone/Address/AllergyHistory脱敏([SensitiveData]特性) | US-PAT-013 |
| 引用检查 | 删除前检查是否被MedicalCase引用(BR-DEL-001) | US-PAT-005 |
| 读卡器集成 | 华大HD100→自动创建/查重 | US-CARD-001/002 |

## 异常处理

| 场景 | 异常 | HTTP |
|------|------|:---:|
| 身份证重复 | ConflictException(409) | 409 |
| 删除被引用患者 | BusinessException(400)+返回引用列表 | 400 |
| 读卡器故障 | 手动录入降级 | - |
