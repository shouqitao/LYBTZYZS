# Issue #812: 实现基于角色的登录导航功能

## 📋 需求描述

实现登录后根据用户角色自动导航到对应的工作台界面：
- 管理员（Admin）→ 导航到管理工作台
- 医生（Doctor）→ 导航到诊疗工作台

## 🎯 验收标准

### 后端部分
- [ ] 登录API返回用户角色信息（UserRole）
- [ ] JWT Token包含角色声明（role claim）
- [ ] 角色信息正确映射（Admin=10, Doctor=1）

### 前端部分
- [ ] 登录界面实现（用户名、密码、登录按钮）
- [ ] 登录成功后获取用户角色
- [ ] 根据角色进行路由导航：
  - Admin角色 → /admin（管理工作台）
  - Doctor角色 → /clinical（诊疗工作台）
- [ ] 角色路由守卫（防止越权访问）
- [ ] 登录状态持久化（Token存储）
- [ ] 登出功能实现

## 📊 当前状态

### 已有基础
- UserRole枚举定义完成（Admin=10, Doctor=1）
- JWT认证基础架构已实现
- AuthService登录接口已存在

### 待实现
- 前端登录界面
- 角色判断逻辑
- 路由导航机制
- 工作台界面框架

## 🔧 技术方案

### 登录流程
1. 用户输入凭据
2. 调用 /api/auth/login
3. 返回JWT Token和用户信息（含角色）
4. 解析角色并存储Token
5. 根据角色导航到对应工作台

### 路由配置
```
Admin (10) → /admin → AdminWorkstationView
Doctor (1) → /clinical → ClinicalWorkstationView
```

### 安全考虑
- Token安全存储（使用SecureStorage）
- 路由守卫验证角色权限
- 401/403错误处理
- Token过期自动跳转登录

## 📝 相关文档
- docs/requirements/desktop-clinical-workstation-ui-requirements-v3-2025-09-29.md
- docs/requirements/desktop-clinical-workstation-feature-checklist-2025-09-29.md

## 🔗 相关Issue
- 依赖：无
- 阻塞：诊疗台和管理台的具体界面实现