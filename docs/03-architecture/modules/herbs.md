# Herbs 模块设计

> v1.0 | 2026-06-28

## 模块概述

Herbs 管理中药药材库：CRUD、分类管理、拼音检索、批量导入(Excel)、引用检查。

**职责边界**：药材数据维护（创建→查询→更新→软删除/恢复）

## 接口契约

| 端点 | Service | 权限 | 关键功能 |
|------|---------|------|----------|
| GET /herbs | GetPagedAsync | DoctorOrAdmin | 分页+拼音检索 |
| POST /herbs | CreateAsync | Admin | 药材创建 |
| PUT /herbs/{id} | UpdateAsync | Admin | 药材更新 |
| DELETE /herbs/{id} | SoftDeleteAsync | Admin | BR-DEL-001引用检查 |
| POST /herbs/batch-import | BatchImportAsync | Admin | Excel批量导入 |

## 关键业务规则

| 规则 | 约束 | US |
|------|------|-----|
| 引用检查 | 删除前检查是否被PrescriptionItem引用(BR-DEL-001) | US-HERB-005 |
| 拼音检索 | 按拼音首字母检索 | US-HERB-002 |
| Excel导入 | 批量导入400+味药材(数天→<1h) | US-HERB-006 |
| 禁用后不可删 | 禁用状态下不允许删除 | US-HERB-010 |
| 恢复 | 软删除后可恢复(Admin) | US-HERB-011 |

## 异常处理

| 场景 | 异常 | HTTP | 说明 |
|------|------|:---:|------|
| 删除被引用药材 | BusinessException | 400 | 返回引用的 PrescriptionItem 列表 |
| Excel格式错误 | ValidationException | 400 | 行号+错误描述 |
| 拼音编码冲突 | BusinessException | 400 | 提示手动指定拼音 |

## 模块交互

| 依赖模块 | 调用签名 | 异常传播 | 场景 |
|----------|----------|----------|------|
| MedicalCase | `IHerbCrossModuleService.GetHerbPrescriptionReferencesAsync()` | BusinessException(400)+引用列表 | BR-DEL-001 删除前引用检查 |
| Auth | `GetOperator()` | UnauthorizedException(401) | 操作员记录 |
