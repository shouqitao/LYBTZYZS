---
type: concept
title: 授权策略
created: 2026-06-10
updated: 2026-06-10
tags: [authorization, aspnet-core, rbac, policy]
related: [authentication, user, medical-case]
sources: ["docs/01-product/04-user-roles.md"]
---

# 授权策略

授权策略是ASP.NET Core中实现基于角色的访问控制（RBAC）的技术机制。它定义了允许哪些角色访问特定的API端点或资源操作。

## 系统策略定义

系统定义了两个核心授权策略：

1.  **`AdminOnly`**：仅允许超级管理员（SuperAdmin）和管理员（Admin）角色访问。应用于用户管理模块等高权限操作。
2.  **`DoctorOrAdmin`**：允许超级管理员、管理员和医生（Doctor）角色访问。应用于大部分业务模块，如患者管理、药材管理、验方管理和医案管理。

## 实现与应用

授权策略通过ASP.NET Core的`[Authorize(Policy = "PolicyName")]`特性应用于控制器或操作方法。例如，用户管理模块的控制器使用`[Authorize(Policy = "AdminOnly")]`，而医案管理模块的创建操作使用`[Authorize(Roles = "Doctor")]`进行更细粒度的资源级控制。

## 与角色的关系

授权策略是角色权限的集合。它简化了权限管理，将多个角色的访问权限封装到一个策略名称中。前台接待（Receptionist）角色不在任何写操作策略中，因此在v1.0中无任何写操作权限。

## 相关概念

- [认证模块](07-authentication.md)：授权策略是认证后进行访问控制的核心组成部分。
- 用户：策略定义了哪些用户角色可以执行操作。
- 医案：策略应用于医案管理的API端点，但更细粒度的权限通过资源级检查实现。