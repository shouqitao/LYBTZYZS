---
type: concept
title: 敏感数据分级与保护
tags: [security, data-protection, privacy, compliance]
related: [data-masking-strategy, card-reader-integration, dpapi-photo-storage]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/02-requirements/17-nfr.md"]
---
# 敏感数据分级与保护

## 概述

敏感数据分级与保护是凌隐宝堂中医诊所管理系统数据安全架构的核心概念。根据数据的敏感程度和对个人隐私的影响，系统将数据划分为 L1、L2、L3 三个级别，并针对不同级别制定了差异化的存储保护、日志脱敏和访问控制策略。

## 分级标准

| 级别 | 定义 | 字段示例 | 所属实体 | 存储保护 | 日志保护 |
|------|------|---------|---------|---------|---------|
| **L1 - 高敏感** | 可直接标识个人身份或联系方式 | IdNumber (身份证号), PhoneNumber (电话) | Patient | **远程**: HTTPS + 访问控制<br>**本地**: 计划 AES-256 加密 (v2.0) | 完全脱敏 / 部分脱敏 (保留前3后4) |
| **L2 - 一般敏感** | 个人敏感信息或医疗诊断信息 | Address, AllergyHistory, MedicalHistory, TcmDiagnosis, PresentIllness | Patient, Consultation | 明文存储 + 严格访问控制 | 摘要脱敏 / 不记录到日志 |
| **L3 - 普通** | 业务标识信息 | Name, Gender, BirthDate, HerbName | 各实体 | 明文存储 | 正常记录 |

## 存储保护策略

### 远程模式 (SQL Server)
*   **传输层**：强制 HTTPS + HSTS。
*   **存储层**：依赖数据库访问控制和网络隔离，不进行字段级加密，以保证查询性能和兼容性。

### 本地模式 (SQL Server LocalDB)
*   **现状 (v1.0)**：本地模式使用 SQL Server LocalDB，L1 字段级加密方案**延期至 v2.0**。当前版本中，L1 字段在本地库中暂以明文形式存储，依赖操作系统文件权限保护。
*   **规划 (v2.0)**：
    *   **算法**：AES-256。
    *   **密钥管理**：使用 Windows DPAPI 保护 AES 密钥，绑定当前 Windows 用户。
    *   **实现**：通过 `EncryptedStringConverter` 在 EF Core 层面透明加解密。
    *   **限制**：加密字段不支持数据库层面的 LIKE 搜索，需在内存中过滤。

## 日志脱敏规则

日志脱敏在 Serilog Enricher 层实现，对业务代码透明。

| 敏感级别 | 脱敏方式 | 示例 |
|----------|---------|------|
| L1 (IdNumber) | 保留前3后4，中间星号 | `320***********1234` |
| L1 (PhoneNumber) | 保留前3后4，中间星号 | `138****5678` |
| L2 (Address) | 保留前6字符，其余星号 | `南京市鼓楼区***` |
| L2 (Allergy/Medical History) | 仅记录状态 | `[已填写]` / `[未填写]` |
| L2 (Diagnosis Fields) | **不记录** | - |
| L3 | 正常记录 | 原文 |

## 密钥生命周期管理

#### 现状 (v1.0)
本地敏感字段（L1/L2）以明文存储，依赖操作系统文件权限保护。DPAPI 仅用于照片加密和密码/令牌存储。

针对 v2.0 的本地加密方案，密钥管理流程如下：
1.  **生成**：首次启动时自动生成 256-bit 随机 AES 密钥。
2.  **存储**：使用 DPAPI 加密后存入 CredentialVault，绑定当前 Windows 用户。
3.  **使用**：启动时解密一次并缓存在内存中，由 `IEncryptionKeyProvider` 提供。
4.  **丢失/切换**：若密钥丢失或 Windows 用户切换，本地加密数据不可读，需从服务器重新同步数据。

## 相关链接

- data-masking-strategy (规划中)
- card-reader-integration (规划中)
- dpapi-photo-storage (规划中)
- serilog-integration (规划中)