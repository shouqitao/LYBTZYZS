# TODO List - 凌隐宝堂中医诊所系统

**创建日期**: 2025年1月8日  
**最后更新**: 2025年1月8日  
**总体进度**: ▓▓▓▓▓░░░░░ 50%

## 📌 项目现状总结

### 后端模块（8个核心模块）
✅ **已完成**: Auth, Users, Patients, Herbs, Formula, Consultation, MedicalCase, Prescriptions

### 前端模块对齐情况
- ✅ **完全实现** (5/8): Auth, Users, Patients, Herbs, Prescriptions
- ⚠️ **部分实现** (2/8): Formula (80%), Consultation (60%)  
- ❌ **缺失UI** (1/8): MedicalCase

---

## 🔴 P0 - 紧急任务（本周必须完成）

### 1. MedicalCase UI模块开发 [预计: 3-5天]
- [ ] 创建MedicalCase模块目录结构
  ```
  src/Frontend/Desktop/Modules/MedicalCase/
  ├── MedicalCaseModule.cs
  ├── ViewModels/
  │   ├── MedicalCaseListViewModel.cs
  │   ├── MedicalCaseDetailViewModel.cs
  │   └── CreateMedicalCaseViewModel.cs
  └── Views/
      ├── MedicalCaseListView.xaml
      ├── MedicalCaseDetailView.xaml
      └── CreateMedicalCaseDialog.xaml
  ```
- [ ] 实现MedicalCase列表界面
  - [ ] 数据网格显示（患者、日期、诊断、状态）
  - [ ] 搜索和筛选功能
  - [ ] 分页控件
- [ ] 实现MedicalCase详情界面
  - [ ] 患者基本信息显示
  - [ ] 诊疗记录时间线
  - [ ] 四诊信息展示
  - [ ] 处方历史
  - [ ] 病历记录（原Records功能）
- [ ] 创建新案例对话框
  - [ ] 患者选择/创建
  - [ ] 初始症状录入
  - [ ] 自动关联到Consultation
- [ ] 在App.xaml.cs中注册模块
- [ ] 添加主菜单导航项

### 2. Consultation模块完善 [预计: 2-3天]
- [ ] 完善看诊主界面
  - [ ] 患者信息卡片
  - [ ] 当前MedicalCase关联
  - [ ] 快捷操作按钮组
- [ ] 实现中医四诊详细录入
  - [ ] 望诊表单（面色、舌象、形态等）
  - [ ] 闻诊表单（声音、气味等）
  - [ ] 问诊表单（症状、病史、生活习惯等）
  - [ ] 切诊表单（脉象、按诊等）
- [ ] 诊断与治疗
  - [ ] 中医证型选择/输入
  - [ ] 治疗原则设定
  - [ ] 医嘱录入
- [ ] 处方开具集成
  - [ ] 快速跳转到Prescriptions
  - [ ] 自动带入患者和诊断信息
- [ ] 保存并完成看诊流程

### 3. 代码清理 - Registration模块 [预计: 0.5天]
- [ ] 删除Registration模块目录
  ```bash
  rm -rf src/Frontend/Desktop/Modules/Registration/
  ```
- [ ] 移除相关服务引用
  - [ ] 删除IRegistrationApiService
  - [ ] 删除IRegistrationService
  - [ ] 删除RegistrationInfo模型
- [ ] 清理App.xaml.cs中的注册代码（如果有）
- [ ] 搜索并移除所有Registration相关引用

---

## 🟡 P1 - 重要任务（下周完成）

### 4. Formula模块标准化 [预计: 1天]
- [ ] 重命名PrescriptionTemplates为FormulaTemplates
  - [ ] 重命名文件夹
  - [ ] 更新命名空间
  - [ ] 更新所有引用
- [ ] 完善Formula管理界面
  - [ ] 经典验方库展示
  - [ ] 个人验方管理
  - [ ] 验方分类和标签
- [ ] 实现验方快速应用
  - [ ] 一键应用到处方
  - [ ] 剂量自动计算
  - [ ] 加减方支持

### 5. 系统导航优化 [预计: 1天]
- [ ] 更新SystemManagementViewModel
  - [ ] 移除RecordManagement导航
  - [ ] 添加MedicalCase导航
  - [ ] 调整菜单顺序
- [ ] 优化主窗口菜单
  - [ ] 按业务流程排序
  - [ ] 添加快捷方式
  - [ ] 实现工作流引导
- [ ] 创建快速操作工具栏
  - [ ] 新建患者
  - [ ] 开始看诊
  - [ ] 查看今日预约

### 6. 患者模块增强 [预计: 1天]
- [ ] 整合基础接待功能
  - [ ] 快速登记入口
  - [ ] 今日就诊列表
  - [ ] 等待队列管理
- [ ] 患者详情优化
  - [ ] 就诊历史时间线
  - [ ] 历史处方查看
  - [ ] 过敏史标注
- [ ] 高级搜索功能
  - [ ] 多条件组合搜索
  - [ ] 搜索历史记录
  - [ ] 常用筛选条件保存

---

## 🟢 P2 - 优化任务（持续进行）

### 7. UI/UX改进
- [ ] 统一界面风格
  - [ ] 颜色主题调整
  - [ ] 图标规范化
  - [ ] 字体大小优化
- [ ] 响应式布局
  - [ ] 支持不同分辨率
  - [ ] 自适应窗口大小
- [ ] 加载状态优化
  - [ ] 添加加载动画
  - [ ] 骨架屏实现
  - [ ] 进度条显示

### 8. 性能优化
- [ ] 数据加载优化
  - [ ] 实现懒加载
  - [ ] 添加数据缓存
  - [ ] 优化API调用
- [ ] 内存管理
  - [ ] 及时释放资源
  - [ ] 避免内存泄漏
  - [ ] 优化图片加载

### 9. 错误处理完善
- [ ] 全局异常捕获
- [ ] 友好的错误提示
- [ ] 错误日志记录
- [ ] 重试机制实现

---

## 📊 进度跟踪

### 本周目标（Week 1）
| 任务 | 负责人 | 状态 | 进度 |
|-----|--------|------|------|
| MedicalCase UI开发 | - | 🔄 进行中 | 0% |
| Consultation完善 | - | ⏸️ 待开始 | 0% |
| Registration清理 | - | ⏸️ 待开始 | 0% |

### 下周计划（Week 2）
| 任务 | 优先级 | 预计工时 |
|-----|--------|---------|
| Formula标准化 | P1 | 1天 |
| 导航优化 | P1 | 1天 |
| 患者增强 | P1 | 1天 |

---

## 🐛 已知问题

### 需要修复
1. **命名不一致**: PrescriptionTemplates vs Formula
2. **死链接**: RecordManagement导航指向不存在的视图
3. **API不匹配**: 某些前端API调用可能与后端不一致

### 技术债务
1. Registration模块残留代码
2. 未使用的服务和接口
3. 重复的ViewModel代码
4. 缺少单元测试

---

## 📝 开发规范提醒

### 命名规范
- **模块**: `LYBT.WPF.Client.Modules.{ModuleName}`
- **服务**: `{ModuleName}Service`
- **视图模型**: `{ViewName}ViewModel`
- **视图**: `{FunctionName}View.xaml`

### 文件组织
```
Modules/{ModuleName}/
├── {ModuleName}Module.cs       # 模块注册
├── ViewModels/                  # 视图模型
├── Views/                       # 视图文件
├── Models/                      # 模块特定模型
└── Services/                    # 模块特定服务
```

### Git提交规范
```bash
feat: 添加MedicalCase列表界面
fix: 修复Consultation保存失败问题
refactor: 重构Formula模块命名
docs: 更新前端开发文档
chore: 清理Registration模块
```

---

## 🎯 里程碑

### Milestone 1: 核心功能完整（目标: 1月15日）
- [x] 后端8个模块全部完成
- [ ] 前端8个模块全部对齐
- [ ] 核心业务流程可用

### Milestone 2: 功能优化（目标: 1月22日）
- [ ] UI/UX优化完成
- [ ] 性能优化完成
- [ ] 错误处理完善

### Milestone 3: 测试就绪（目标: 1月31日）
- [ ] 单元测试覆盖率>60%
- [ ] 集成测试通过
- [ ] 用户验收测试

---

## 📞 相关负责人

| 模块 | 负责人 | 联系方式 |
|-----|--------|---------|
| 后端API | - | - |
| 前端开发 | - | - |
| UI设计 | - | - |
| 测试 | - | - |

---

## 🔄 更新记录

### 2025-01-08
- 初始创建TODO清单
- 基于后端8个模块制定前端开发计划
- 识别需要清理的冗余模块
- 设定开发优先级

---

**注意事项**:
1. 每完成一个任务，请及时更新状态
2. 遇到阻塞问题，立即上报
3. 代码提交前必须通过编译
4. 重要变更需要代码评审

**下一步行动**: 
1. 立即开始MedicalCase UI模块开发
2. 同步进行Registration模块清理工作