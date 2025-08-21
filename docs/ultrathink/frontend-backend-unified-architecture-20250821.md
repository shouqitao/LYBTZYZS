# UltraThink前后端统一架构方案

## 🎯 **核心设计原则**

> **"前后端功能严格对应，模块职责单一明确"** - 确保架构一致性

## 📋 **模块职责重新定义**

### 🔥 **核心业务模块（8个）**

#### 1. **Prescriptions 处方模块**
```yaml
后端职责: 
  - 处方CRUD操作
  - 处方项计算验证
  - 处方状态管理

前端职责:
  - 处方组成编辑 ✅
  - 价格自动计算 ✅
  - 基础验证 ✅
  
移除功能:
  - 历史管理 → MedicalCase
  - 药材选择 → Herbs调用
  - 验方管理 → Formula调用
  - 复杂协调 → Consultation
```

#### 2. **Formula 验方模块** 
```yaml
后端职责:
  - 验方模板CRUD
  - 验方分类管理
  - 验方搜索

前端职责:
  - 验方模板选择
  - 验方快速应用
  - 验方预览
```

#### 3. **Herbs 中药材模块**
```yaml
后端职责:
  - 药材基础信息
  - 价格管理
  - 单位管理

前端职责:
  - 药材选择界面
  - 药材搜索
  - 用量设置
```

#### 4. **MedicalCase 医疗案例模块**
```yaml
后端职责:
  - 诊疗流程聚合根
  - 案例状态管理
  - 历史记录

前端职责:
  - 案例流程控制
  - 历史处方管理 ✨
  - 诊疗记录查看
```

#### 5. **Consultation 看诊模块**
```yaml
后端职责:
  - 中医四诊记录
  - 诊断分析
  - 看诊状态

前端职责:
  - 四诊数据采集
  - 辨证分析
  - 工作流导航
```

#### 6. **Patients 患者模块**
```yaml
后端职责:
  - 患者档案管理
  - 基础挂号

前端职责:
  - 患者信息管理
  - 接待登记
```

#### 7. **Users 用户模块**
```yaml
后端职责:
  - 用户账号管理
  - 角色权限

前端职责:
  - 用户设置
  - 权限控制
```

#### 8. **Auth 认证模块**
```yaml
后端职责:
  - 身份认证
  - Token管理

前端职责:
  - 登录界面
  - 会话管理
```

## 🔄 **模块间协作标准**

### 标准调用模式
```csharp
// 1. Service接口调用（推荐）
public class PrescriptionViewModel 
{
    public async Task SelectHerbs()
    {
        // 调用Herbs模块服务
        var herbs = await _herbService.SelectHerbsAsync();
        ApplySelectedHerbs(herbs);
    }
    
    public async Task ApplyFormula()
    {
        // 调用Formula模块服务  
        var formula = await _formulaService.SelectFormulaAsync();
        ApplyFormulaTemplate(formula);
    }
}

// 2. 事件通信（解耦）
public class ConsultationViewModel
{
    public void CompleteDiagnosis()
    {
        // 发布四诊完成事件
        _eventAggregator.GetEvent<FourDiagnosisCompletedEvent>()
            .Publish(new FourDiagnosisData(diagnosisResult));
    }
}

// 3. 导航参数传递
public void NavigateToPrescriptions()
{
    var parameters = new NavigationParameters 
    { 
        { "MedicalCaseId", currentMedicalCaseId }
    };
    _regionManager.RequestNavigate("MainContentRegion", "PrescriptionsMainView", parameters);
}
```

### 依赖关系图
```
MedicalCase (聚合根)
    ├── Consultation (1:N)
    ├── Prescriptions (1:N)  
    └── Patients (N:1)

Prescriptions
    ├── 调用 → Herbs (选择药材)
    └── 调用 → Formula (应用验方)

Consultation  
    └── 导航 → Prescriptions (开具处方)
```

## 📁 **简化后的目录结构**

### 前端模块结构（简化版）
```
src/Client/Desktop/Modules/
├── Auth/
│   ├── Views/LoginView.xaml
│   ├── ViewModels/LoginViewModel.cs
│   └── Services/AuthService.cs
├── Patients/
│   ├── Views/PatientManagementView.xaml
│   ├── ViewModels/PatientManagementViewModel.cs  
│   └── Services/PatientService.cs
├── Consultation/
│   ├── Views/FourDiagnosisView.xaml
│   ├── Views/PrescriptionPlaceholderView.xaml
│   ├── ViewModels/ConsultationViewModel.cs
│   └── Services/ConsultationService.cs
├── MedicalCase/
│   ├── Views/CaseHistoryView.xaml
│   ├── Views/PrescriptionHistoryView.xaml ✨
│   ├── ViewModels/MedicalCaseViewModel.cs
│   └── Services/MedicalCaseService.cs  
├── Prescriptions/
│   ├── Views/PrescriptionComposerView.xaml ✨
│   ├── ViewModels/PrescriptionComposerViewModel.cs
│   └── Services/PrescriptionService.cs
├── Herbs/
│   ├── Views/HerbSelectionDialog.xaml
│   ├── ViewModels/HerbSelectionViewModel.cs
│   └── Services/HerbService.cs
├── Formula/
│   ├── Views/FormulaSelectionDialog.xaml
│   ├── ViewModels/FormulaSelectionViewModel.cs
│   └── Services/FormulaService.cs
└── Users/
    ├── Views/UserSettingsView.xaml
    ├── ViewModels/UserViewModel.cs
    └── Services/UserService.cs
```

### 移除的冗余结构
```
❌ 删除: *Api 模块 (8个)
❌ 删除: 复杂的协调器类
❌ 删除: 过度的验证器
❌ 删除: 冗余的管理器
```

## 🚀 **处方模块具体重构方案**

### 当前问题
```yaml
文件数量: 35+
职责混乱: 
  - 历史管理 ❌
  - 药材选择 ❌  
  - 验方管理 ❌
  - 复杂协调 ❌
```

### 重构目标
```yaml
文件数量: 8-10
核心职责:
  - 处方组成编辑 ✅
  - 价格自动计算 ✅
  - 基础验证 ✅
  - 保存操作 ✅
```

### 重构后结构
```
Prescriptions/
├── Views/
│   └── PrescriptionComposerView.xaml     # 唯一主界面
├── ViewModels/
│   ├── PrescriptionComposerViewModel.cs  # 主ViewModel
│   └── PrescriptionItemViewModel.cs      # 药材项ViewModel
├── Components/
│   ├── PriceCalculator.cs                # 价格计算组件
│   └── BasicValidator.cs                 # 基础验证组件
└── Services/
    └── PrescriptionService.cs            # 服务接口
```

## 📊 **重构收益分析**

| 维度 | 重构前 | 重构后 | 改进 |
|------|-------|-------|------|
| 模块数量 | 8 + 8个Api | 8 | ↓50% |
| Prescriptions文件 | 35+ | 8 | ↓77% |
| 功能复杂度 | 极高 | 简单 | ↓80% |
| 职责清晰度 | 混乱 | 清晰 | ↑90% |
| 维护成本 | 很高 | 低 | ↓70% |
| 启动速度 | 慢 | 快 | ↑60% |

## 📈 **实施路线图**

### Phase 1: 架构设计 (1天)
- [x] 分析现有架构问题
- [x] 制定统一架构方案
- [x] 定义模块职责边界

### Phase 2: 处方模块重构 (2天)
- [ ] 创建PrescriptionComposerView
- [ ] 简化PrescriptionComposerViewModel  
- [ ] 移除历史管理功能
- [ ] 实现Service调用模式

### Phase 3: 模块间接口 (1天)
- [ ] 标准化Herbs调用接口
- [ ] 标准化Formula调用接口
- [ ] 实现事件通信机制

### Phase 4: 全面验证 (1天)
- [ ] 测试模块协作流程
- [ ] 验证前后端一致性
- [ ] 性能优化

## 🎯 **成功标准**

### 功能标准
- ✅ 处方编辑功能完整
- ✅ 模块间调用顺畅  
- ✅ 前后端职责对齐

### 技术标准
- ✅ 代码结构清晰
- ✅ 性能提升明显
- ✅ 维护成本降低

### 用户标准  
- ✅ 界面简洁直观
- ✅ 操作流程顺畅
- ✅ 响应速度快

---

**结论**: 通过严格的前后端架构对齐，处方模块将从35+文件的复杂系统简化为8文件的专注系统，真正实现"只关注处方组成"的设计目标。