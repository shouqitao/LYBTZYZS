# LYBTZYZS 开发任务清单

## 项目概述
凌隐宝堂中医诊所诊疗系统 - 开发任务跟踪  
更新时间：2025-01-30  
项目完成度：约35%

---

## ✅ 已完成模块

### 基础设施层
| 模块 | 完成度 | 备注 |
|-----|--------|-----|
| Auth | 95% | JWT认证、登录登出、令牌刷新已完成 |
| Users | 85% | 用户管理基本完成，缺少Create方法 |
| UnifiedConfig | 100% | 统一配置管理已完成 |
| UnifiedLogs | 100% | 统一日志服务已完成 |

### 业务模块层
| 模块 | 完成度 | 备注 |
|-----|--------|-----|
| Herbs | 90% | 药材管理基本完成，库存功能待整合 |
| Patients | 70% | 基础CRUD完成，部分方法缺失 |
| Prescriptions | 40% | 框架完成，核心方法未实现 |
| Consultation | 30% | 仅框架，业务方法未实现 |

### 前端模块
| 模块 | 完成度 | 备注 |
|-----|--------|-----|
| Authentication | 100% | 登录认证完整 |
| SystemManagement | 85% | 管理后台基本完整 |
| Consultation | 20% | 仅基础框架 |

---

## 🚨 紧急任务（阻塞业务流程）

### 1. Registration 模块（挂号管理）- 预计3-5天
#### 后端开发
- [ ] 创建 LYBT.Module.Registration 项目
- [ ] 实现 RegistrationService.cs
  - [ ] 现场挂号功能
  - [ ] 预约挂号功能
  - [ ] 挂号单号生成（GH+日期+流水号）
  - [ ] 退号功能
- [ ] 实现 RegistrationRepository.cs
- [ ] 创建 RegistrationController.cs
  - [ ] POST /api/v1/registration/register - 现场挂号
  - [ ] POST /api/v1/registration/appointment - 预约挂号
  - [ ] GET /api/v1/registration/today - 当日挂号列表
  - [ ] DELETE /api/v1/registration/{id} - 退号
- [ ] 创建 DTOs
  - [ ] RegistrationCreateDto
  - [ ] RegistrationDto
  - [ ] RegistrationQueryDto

#### 前端开发
- [ ] 创建 Registration 模块目录
- [ ] 实现挂号界面 RegistrationView.xaml
- [ ] 实现 RegistrationViewModel.cs
- [ ] 添加患者选择组件
- [ ] 添加医生选择组件
- [ ] 集成到主界面菜单

### 2. Queueing 模块（排队叫号）- 预计5-7天
#### 后端开发
- [ ] 创建 LYBT.Module.Queueing 项目
- [ ] 实现 QueueService.cs
  - [ ] 入队功能
  - [ ] 叫号功能
  - [ ] 过号处理
  - [ ] 重新排队
  - [ ] 队列状态查询
- [ ] 实现 QueueRepository.cs
- [ ] 创建 QueueingController.cs
  - [ ] POST /api/v1/queue/enqueue - 入队
  - [ ] POST /api/v1/queue/call - 叫号
  - [ ] POST /api/v1/queue/pass - 过号
  - [ ] GET /api/v1/queue/waiting - 等待列表
  - [ ] GET /api/v1/queue/status - 队列状态
- [ ] 实现 SignalR Hub
  - [ ] QueueHub.cs - 实时通知

#### 前端开发
- [ ] 创建排队显示大屏界面
- [ ] 实现叫号工作台
- [ ] 添加语音播报功能
- [ ] 实现实时更新（SignalR）

### 3. Cashier 模块（收费结算）- 预计5-7天
#### 后端开发
- [ ] 创建 LYBT.Module.Cashier 项目
- [ ] 实现 CashierService.cs
  - [ ] 费用计算
  - [ ] 收费处理
  - [ ] 退费功能
  - [ ] 发票管理
- [ ] 实现 CashierRepository.cs
- [ ] 创建 CashierController.cs
  - [ ] POST /api/v1/cashier/calculate - 计算费用
  - [ ] POST /api/v1/cashier/charge - 收费
  - [ ] POST /api/v1/cashier/refund - 退费
  - [ ] GET /api/v1/cashier/bill/{id} - 账单详情
  - [ ] GET /api/v1/cashier/daily - 日结报表

#### 前端开发
- [ ] 创建收银台界面
- [ ] 实现费用明细显示
- [ ] 添加支付方式选择
  - [ ] 现金支付
  - [ ] 微信支付
  - [ ] 支付宝支付
  - [ ] 医保支付
- [ ] 实现小票打印功能

---

## ⚡ 高优先级任务

### 4. Pharmacy 模块（药房管理）- 预计7-10天
#### 后端开发
- [ ] 创建 LYBT.Module.Pharmacy 项目
- [ ] 实现 PharmacyService.cs
  - [ ] 发药功能
  - [ ] 库存扣减
  - [ ] 入库管理
  - [ ] 库存盘点
- [ ] 创建 PharmacyController.cs
  - [ ] POST /api/v1/pharmacy/dispense - 发药
  - [ ] POST /api/v1/pharmacy/purchase - 入库
  - [ ] GET /api/v1/pharmacy/pending - 待发药列表
  - [ ] POST /api/v1/pharmacy/inventory - 库存盘点

#### 前端开发
- [ ] 创建药房工作台
- [ ] 实现待发药列表
- [ ] 添加发药确认界面
- [ ] 实现库存查询功能

### 5. 完善现有控制器方法
#### ConsultationController
- [ ] 实现 Create 方法
- [ ] 实现 Update 方法
- [ ] 实现 GetByPatient 方法
- [ ] 添加中医四诊接口

#### PrescriptionsController
- [ ] 实现 Create 方法
- [ ] 实现 GetByPatient 方法
- [ ] 实现 Print 方法
- [ ] 添加处方模板功能

#### PatientsController
- [ ] 实现 GetById 方法
- [ ] 实现 Create 方法
- [ ] 实现 Update 方法

---

## 💡 常规任务

### 6. 前端业务模块完善
#### FrontDesk 模块
- [ ] 创建前台接待主界面
- [ ] 实现患者签到功能
- [ ] 添加预约管理界面
- [ ] 实现排队状态显示

#### Doctor 模块
- [ ] 创建医生工作台
- [ ] 实现患者列表
- [ ] 集成看诊界面
- [ ] 添加处方开具功能

#### Pharmacist 模块
- [ ] 创建药师工作台
- [ ] 实现处方审核界面
- [ ] 添加库存查询功能
- [ ] 实现发药记录管理

### 7. 中医特色功能
- [ ] 中医四诊记录界面
- [ ] 体质辨识功能
- [ ] 证型诊断辅助
- [ ] 经络穴位管理

### 8. 统计报表功能
- [ ] 日报表（收入、就诊量、药品销售）
- [ ] 月报表（经营分析）
- [ ] 医生工作量统计
- [ ] 患者来源分析

---

## 📋 技术债务

### 代码质量
- [ ] 移除所有TODO占位实现（约30个）
- [ ] 统一API响应格式
- [ ] 完善异常处理机制
- [ ] 添加操作日志记录

### 测试覆盖
- [ ] Auth模块单元测试
- [ ] Users模块单元测试
- [ ] Herbs模块单元测试
- [ ] API集成测试
- [ ] 前端UI测试

### 文档完善
- [ ] API文档更新
- [ ] 部署文档编写
- [ ] 用户手册编写
- [ ] 运维手册编写

---

## 📅 开发计划

### 第1周（当前）
- Registration模块开发
- Queueing模块基础框架

### 第2周
- Queueing模块完成
- Cashier模块开发
- 前端FrontDesk模块

### 第3周
- Pharmacy模块开发
- 完善控制器方法
- 前端Doctor模块

### 第4周
- 集成测试
- Bug修复
- 性能优化
- 部署准备

---

## 🎯 里程碑

| 里程碑 | 目标日期 | 完成标准 |
|-------|---------|---------|
| M1: 挂号功能可用 | 第1周末 | 可以完成患者挂号 |
| M2: 排队系统运行 | 第2周中 | 叫号系统正常工作 |
| M3: 收费流程打通 | 第2周末 | 可以完成收费操作 |
| M4: MVP版本完成 | 第3周末 | 完整诊疗流程可用 |
| M5: 正式版本发布 | 第4周末 | 所有功能稳定运行 |

---

## 📝 备注

1. **优先级说明**
   - 🚨 紧急：阻塞核心业务流程
   - ⚡ 高：影响用户体验
   - 💡 常规：功能增强

2. **开发原则**
   - 先打通核心流程，再完善细节
   - 每个模块完成后立即测试
   - 保持代码可编译状态

3. **团队协作**
   - 每日更新任务状态
   - 遇到阻塞及时沟通
   - 代码提交前进行自测

---
*最后更新：2025-01-30*