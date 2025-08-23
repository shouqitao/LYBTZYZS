# 前端服务接口化重构计划

## 🎯 重构目标

将前端ModuleService类实现对应的共享接口，提升架构一致性、可测试性和扩展性。

## 📋 重构范围

### 核心业务模块 (8个)

| 模块 | 服务类 | 共享接口 | 优先级 | 状态 |
|------|--------|----------|--------|------|
| **Users** | UserModuleService | IUserService | 🔥 P0 | ⏳ 进行中 |
| **Patients** | PatientModuleService | IPatientService | 🔥 P0 | ⏸️ 待开始 |
| **Herbs** | HerbModuleService | IHerbService | 🔥 P0 | ⏸️ 待开始 |
| **Formula** | FormulaModuleService | IFormulaService | 🔶 P1 | ⏸️ 待开始 |
| **Prescriptions** | PrescriptionsModuleService | IPrescriptionService | 🔶 P1 | ⏸️ 待开始 |
| **MedicalCase** | MedicalCaseModuleService | IMedicalCaseService | 🔶 P1 | ⏸️ 待开始 |
| **Consultation** | ConsultationModuleService | IConsultationService | 🔶 P1 | ⏸️ 待开始 |
| **Auth** | AuthModuleService | IAuthService | 🔹 P2 | ⏸️ 待开始 |

## 🛠️ 重构策略

### Phase 1: 核心示范模块 (Users)
- ✅ 分析现有UserModuleService方法
- ✅ 对比IUserService接口要求
- 🔄 添加接口实现声明
- 🔄 调整方法签名匹配
- 🔄 测试验证功能正常

### Phase 2: 核心业务模块 (P0优先级)
- Patients模块接口化
- Herbs模块接口化  
- Formula模块接口化

### Phase 3: 其他业务模块 (P1优先级)
- Prescriptions模块接口化
- MedicalCase模块接口化
- Consultation模块接口化

### Phase 4: IoC容器配置更新
```csharp
// 更新服务注册
containerRegistry.RegisterSingleton<IUserService, UserModuleService>();
containerRegistry.RegisterSingleton<IPatientService, PatientModuleService>();
containerRegistry.RegisterSingleton<IHerbService, HerbModuleService>();
// ... 其他模块
```

### Phase 5: ViewModel依赖更新
```csharp
// 从具体类依赖改为接口依赖
public class UserManagementViewModel
{
    private readonly IUserService _userService; // 而非 UserModuleService
}
```

### Phase 6: 验证和测试
- 编译验证所有模块
- 功能测试确保稳定性
- 单元测试可测试性验证

## 🔧 每个模块重构步骤

### 标准重构流程
1. **现状分析**
   ```bash
   # 检查现有服务类位置和方法
   src/Client/Desktop/Modules/{Module}/Services/{Module}ModuleService.cs
   ```

2. **接口对比**
   ```bash
   # 对比共享接口定义
   src/Shared/LYBT.Shared.Interfaces/Services/I{Module}Service.cs
   ```

3. **接口实现**
   ```csharp
   // 添加接口实现
   public class {Module}ModuleService : I{Module}Service
   ```

4. **方法签名调整**
   - 确保返回类型匹配
   - 确保参数类型匹配
   - 添加缺失的接口方法

5. **编译测试**
   ```bash
   dotnet build LYBT.Desktop.sln
   ```

6. **功能验证**
   - 启动应用程序
   - 测试相关功能正常

## 🚨 风险控制

### 安全重构原则
1. **渐进式重构** - 一次只改一个模块
2. **保持现有逻辑** - 不改变业务逻辑，只添加接口约束
3. **及时测试** - 每个模块完成后立即测试
4. **版本备份** - 重大改动前创建Git分支

### 回滚策略
```bash
# 如果遇到问题，可以快速回滚
git checkout HEAD~1 -- src/Client/Desktop/Modules/{Module}/Services/
```

## 📊 预期收益

### 架构质量提升
- ✅ **一致性**: 前后端服务层架构统一
- ✅ **可测试性**: Mock接口而非具体类
- ✅ **扩展性**: 便于切换不同实现
- ✅ **可维护性**: 契约明确，团队协作更顺畅

### 技术债务偿还
- 🔄 解决前端服务层无接口约束问题
- 🔄 提升代码质量和可读性
- 🔄 为未来离线模式、缓存策略奠定基础

## ⏱️ 时间估算

| Phase | 模块数 | 预计时间 | 累计时间 |
|-------|--------|----------|----------|
| Phase 1 | 1个 | 2小时 | 2小时 |
| Phase 2 | 3个 | 4小时 | 6小时 |
| Phase 3 | 4个 | 6小时 | 12小时 |
| Phase 4-6 | 配置+测试 | 3小时 | 15小时 |

**总计**: 约15小时 (分3天完成)

## 📝 实施记录

### 完成情况跟踪
- [ ] Phase 1: Users模块 (示范)
- [ ] Phase 2: 核心业务模块
- [ ] Phase 3: 其他模块  
- [ ] Phase 4: IoC配置
- [ ] Phase 5: ViewModel更新
- [ ] Phase 6: 验证测试

---

**重构负责人**: Claude (UltraThink架构师)  
**开始时间**: 2025-08-22  
**预计完成**: 2025-08-24