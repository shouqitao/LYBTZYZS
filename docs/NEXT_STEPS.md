# 下一步开发任务清单

## 🚨 紧急任务（阻塞业务流程）

### 1. Registration 模块实现
```bash
# 后端任务
- [ ] 创建 src/Backend/Modules/LYBT.Module.Registration/
- [ ] 实现 RegistrationService.cs
- [ ] 实现 RegistrationRepository.cs  
- [ ] 创建 RegistrationController.cs
- [ ] 添加 RegistrationCreateDto, RegistrationDto 等

# 前端任务
- [ ] 创建 src/Frontend/Desktop/Modules/Registration/
- [ ] 实现挂号界面 RegistrationView.xaml
- [ ] 实现 RegistrationViewModel.cs
- [ ] 集成到主界面菜单
```

### 2. Queueing 模块实现
```bash
# 后端任务
- [ ] 创建 src/Backend/Modules/LYBT.Module.Queueing/
- [ ] 实现 QueueService.cs（排队逻辑）
- [ ] 实现 QueueRepository.cs
- [ ] 创建 QueueingController.cs
- [ ] 实现叫号WebSocket通信

# 前端任务  
- [ ] 创建排队显示界面
- [ ] 实现叫号组件
- [ ] 添加语音播报功能
```

### 3. Cashier 模块实现
```bash
# 后端任务
- [ ] 创建 src/Backend/Modules/LYBT.Module.Cashier/
- [ ] 实现 CashierService.cs
- [ ] 创建 CashierController.cs
- [ ] 实现支付接口集成
- [ ] 添加退费功能

# 前端任务
- [ ] 创建收银台界面
- [ ] 实现费用计算组件
- [ ] 添加支付方式选择
- [ ] 实现小票打印功能
```

## ⚡ 高优先级任务

### 4. Pharmacy 模块实现
```bash
# 后端
- [ ] 创建药房模块
- [ ] 实现发药服务
- [ ] 库存扣减逻辑
- [ ] 发药记录管理

# 前端
- [ ] 药房工作台界面
- [ ] 待发药列表
- [ ] 发药确认功能
```

### 5. 完善 ConsultationController
```bash
- [ ] 实现 Create 方法
- [ ] 实现 Update 方法  
- [ ] 实现 GetByPatient 方法
- [ ] 添加中医四诊记录接口
```

### 6. 完善 PrescriptionsController
```bash
- [ ] 实现 Create 方法
- [ ] 实现 GetByPatient 方法
- [ ] 实现 Print 方法
- [ ] 添加处方模板功能
```

## 💡 常规任务

### 7. 前端业务模块完善
```bash
# FrontDesk 模块
- [ ] 创建前台接待界面
- [ ] 患者签到功能
- [ ] 预约管理界面

# Doctor 模块  
- [ ] 医生工作台主界面
- [ ] 患者列表
- [ ] 看诊界面集成

# Pharmacist 模块
- [ ] 药师工作台
- [ ] 处方审核界面
- [ ] 库存查询功能
```

### 8. API 方法补充
```bash
# PatientsController
- [ ] GetById 方法
- [ ] Create 方法
- [ ] Update 方法

# UsersController
- [ ] Create 方法
```

## 📝 技术债务清理

### 9. 代码质量改进
```bash
- [ ] 移除所有 TODO 占位实现
- [ ] 统一 API 响应格式
- [ ] 完善异常处理
- [ ] 添加日志记录
```

### 10. 测试补充
```bash
- [ ] 添加单元测试
- [ ] 添加集成测试
- [ ] API 自动化测试完善
```

## 执行建议

### 开发顺序
1. **第1周**：完成 Registration 模块
2. **第2周**：完成 Queueing 和 Cashier 模块基础功能
3. **第3周**：完成 Pharmacy 模块，打通完整流程
4. **第4周**：前端界面完善和集成测试

### 每日检查清单
- [ ] 代码是否可编译
- [ ] 新功能是否有测试
- [ ] API 文档是否更新
- [ ] 是否有新的 TODO 添加

### 关键里程碑
- **里程碑1**：完成挂号功能（第1周）
- **里程碑2**：实现排队叫号（第2周）  
- **里程碑3**：打通收费流程（第2周）
- **里程碑4**：完整诊疗流程可用（第3周）

## 资源和参考

### 参考实现
- Registration 参考 Patients 模块结构
- Queueing 使用 SignalR 实现实时通信
- Cashier 参考电商系统支付流程

### 技术文档
- [模块开发规范](./开发规范.md)
- [API 响应标准](./API响应标准.md)
- [前后端契约规范](./前后端契约规范.md)

### 联系支持
- 架构问题：查看 `docs/03-架构设计/`
- 业务流程：参考 `docs/04-模块实现/`
- 开发指南：查看 `docs/05-开发指南/`

---
*更新时间：2025-01-30*
*优先级：🚨紧急 > ⚡高 > 💡常规*