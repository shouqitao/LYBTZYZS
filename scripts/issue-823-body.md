## 📋 概述

修复 Desktop 项目剩余的编译错误，主要涉及 4 个模块缺少 Shared 库和其他依赖的项目引用。

## 🎯 背景

在 Issue #822 修复 Formula 模块编译错误后，Desktop 解决方案仍有 **210 个编译错误**，来自 4 个模块缺少必要的项目引用。

## 🐛 错误详情

### 1. Auth 模块 - 缺少 Shared 库引用

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/LYBT.Desktop.Auth.csproj`

**错误**:
```
error CS0234: 命名空间"LYBT"中不存在类型或命名空间名"Shared"
error CS0234: 命名空间"Microsoft"中不存在类型或命名空间名"Extensions"
error CS0246: 未能找到类型或命名空间名"LoginRequest"
error CS0246: 未能找到类型或命名空间名"ServiceResult"
error CS0246: 未能找到类型或命名空间名"ILoggerFactory"
```

**缺少依赖**:
- `LYBT.Shared.Models` - LoginRequest、LoginResponse、UserDto 等 DTO
- `LYBT.Shared.Interfaces` - ServiceResult 等通用接口
- `Microsoft.Extensions.Logging.Abstractions` - ILoggerFactory（已有 PackageReference，可能需要添加到某些文件）

### 2. Consultation 模块 - 缺少 Shared 库引用

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/LYBT.Desktop.Consultation.csproj`

**错误**:
```
error CS0234: 命名空间"LYBT"中不存在类型或命名空间名"Shared"
error CS0246: 未能找到类型或命名空间名"ConsultationDto"
error CS0246: 未能找到类型或命名空间名"PatientDto"
error CS0246: 未能找到类型或命名空间名"IConsultationService"
```

**缺少依赖**:
- `LYBT.Shared.Models` - ConsultationDto、PatientDto 等
- `LYBT.Shared.Interfaces` - ServiceResult 等

### 3. MedicalCase 模块 - 缺少 Shared 库引用

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj`

**错误**:
```
error CS0234: 命名空间"LYBT"中不存在类型或命名空间名"Shared"
error CS0246: 未能找到类型或命名空间名"MedicalCaseDto"
error CS0246: 未能找到类型或命名空间名"CommonStatus"
error CS0246: 未能找到类型或命名空间名"IMedicalCaseService"
```

**缺少依赖**:
- `LYBT.Shared.Models` - MedicalCaseDto、CommonStatus 等
- `LYBT.Shared.Interfaces` - ServiceResult 等

### 4. Prescriptions 模块 - 缺少 Shared 库引用

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/LYBT.Desktop.Prescriptions.csproj`

**错误**:
```
error CS0234: 命名空间"LYBT"中不存在类型或命名空间名"Shared"
error CS0246: 未能找到类型或命名空间名"PrescriptionDto"
error CS0246: 未能找到类型或命名空间名"IPrescriptionService"
```

**缺少依赖**:
- `LYBT.Shared.Models` - PrescriptionDto 等
- `LYBT.Shared.Interfaces` - ServiceResult 等

## 🔍 问题分析

### 根本原因

1. **依赖引用不完整**: Issue #820 重构时，4 个模块的 Shared 库引用被遗漏
2. **不一致性**: 对比发现：
   - ✅ **已有引用**: Herbs、Formula、Users、Patients 模块都有 Shared 库引用
   - ❌ **缺少引用**: Auth、Consultation、MedicalCase、Prescriptions 缺少引用

### 受影响范围

| 模块 | 缺少 LYBT.Shared.Models | 缺少 LYBT.Shared.Interfaces | 估计错误数 |
|------|------------------------|---------------------------|-----------|
| Auth | ✅ | ✅ | ~60 errors |
| Consultation | ✅ | ✅ | ~50 errors |
| MedicalCase | ✅ | ✅ | ~50 errors |
| Prescriptions | ✅ | ✅ | ~50 errors |

## ✅ 解决方案

### 步骤 1: 添加 Shared 库引用

- [ ] **[FIX-1]** 修复 Auth.csproj
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />`
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />`
  - 验收: Auth 模块编译成功

- [ ] **[FIX-2]** 修复 Consultation.csproj
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />`
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />`
  - 验收: Consultation 模块编译成功

- [ ] **[FIX-3]** 修复 MedicalCase.csproj
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />`
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />`
  - 验收: MedicalCase 模块编译成功

- [ ] **[FIX-4]** 修复 Prescriptions.csproj
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />`
  - 添加 `<ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />`
  - 验收: Prescriptions 模块编译成功

### 步骤 2: 编译验证

- [ ] **[BUILD-1]** 逐个编译验证
  - `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Auth/LYBT.Desktop.Auth.csproj -c Release`
  - `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Consultation/LYBT.Desktop.Consultation.csproj -c Release`
  - `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj -c Release`
  - `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/LYBT.Desktop.Prescriptions.csproj -c Release`
  - 验收: 每个模块 0 errors

- [ ] **[BUILD-2]** 全量编译 Desktop 解决方案
  - `dotnet build LYBT.Desktop.sln -c Release`
  - 验收: 0 errors（警告数与基线一致）

- [ ] **[BUILD-3]** 全量编译 All 解决方案
  - `dotnet build LYBTZYZS.sln -c Release`
  - 验收: 0 errors

### 步骤 3: Git 提交

- [ ] **[COMMIT-1]** 创建 Git 提交
  - 提交信息: "fix(desktop): 为4个模块添加Shared库项目引用 - Issue #823"
  - 包含: Auth、Consultation、MedicalCase、Prescriptions 的 .csproj 修改
  - 验收: `git log` 显示提交

## ⚠️ 风险评估

| 风险项 | 严重程度 | 影响范围 | 缓解措施 |
|--------|----------|----------|----------|
| 引用路径错误 | 低 | 单个模块 | 参考已有模块（Herbs、Formula）的正确引用路径 |
| 循环依赖 | 低 | 跨模块 | Shared 库是基础库，不依赖 Desktop 模块 |
| 新增编译错误 | 低 | 单个模块 | 增量验证，每修复一个模块立即编译检查 |

## 📚 参考资料

- Issue #822: 修复 Desktop Formula 模块编译错误
- Issue #820: Desktop 架构优化 - 统一文件夹命名规范
- 已有正确引用的模块:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/LYBT.Desktop.Herbs.csproj`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/LYBT.Desktop.Formula.csproj`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Users/LYBT.Desktop.Users.csproj`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/LYBT.Desktop.Patients.csproj`

---

**创建时间**: 2025-09-30
**预计工作量**: 0.5 小时
**优先级**: 高（阻塞编译）
**依赖**: Issue #822 (已完成)
**相关**: Issue #820
