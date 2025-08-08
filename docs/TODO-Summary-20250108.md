# TODO Summary - 快速执行清单

## 🚀 立即执行（今天）

### 上午
- [ ] 删除Registration模块 (30分钟)
  ```bash
  rm -rf src/Frontend/Desktop/Modules/Registration/
  ```
- [ ] 清理相关引用 (30分钟)
  - 搜索 "Registration" 关键词
  - 删除所有相关import和引用

### 下午  
- [ ] 创建MedicalCase模块基础结构 (2小时)
  - 创建文件夹结构
  - 创建Module类
  - 创建基础ViewModel和View

---

## 📅 本周任务（1月8日-1月12日）

### Day 1-2: MedicalCase核心功能
- [ ] MedicalCase列表界面
- [ ] MedicalCase详情界面
- [ ] 创建案例对话框

### Day 3: Consultation中医四诊
- [ ] 望诊录入表单
- [ ] 闻诊录入表单
- [ ] 问诊录入表单
- [ ] 切诊录入表单

### Day 4: 集成与测试
- [ ] MedicalCase与Consultation联动
- [ ] 处方开具流程测试
- [ ] 修复发现的问题

### Day 5: 代码规范化
- [ ] Formula模块重命名
- [ ] 导航菜单调整
- [ ] 代码审查和优化

---

## 📋 快速检查清单

### 前端模块对齐检查
```
✅ Auth         → Authentication模块
✅ Users        → SystemManagement/Users  
✅ Patients     → SystemManagement/Patients
✅ Herbs        → SystemManagement/Herbs
⚠️ Formula      → SystemManagement/Formulas (需改名)
⚠️ Consultation → Consultation模块 (需完善)
❌ MedicalCase  → 需要创建UI模块
✅ Prescriptions→ SystemManagement/Prescriptions
```

### 需要删除的
```
❌ Registration模块
❌ RecordManagement导航
❌ 未使用的服务接口
```

### 需要创建的
```
✅ MedicalCase UI模块
✅ 中医四诊表单
✅ 医疗案例管理界面
```

---

## 🎯 关键指标

| 指标 | 当前 | 目标 | 截止日期 |
|-----|------|------|---------|
| 前端模块完成度 | 5/8 (62.5%) | 8/8 (100%) | 1月15日 |
| Formula功能 | 80% | 100% | 1月12日 |
| Consultation功能 | 60% | 100% | 1月12日 |
| MedicalCase UI | 0% | 100% | 1月10日 |
| 代码清理 | 0% | 100% | 1月8日 |

---

## 🔥 今日必做（1月8日）

1. **09:00-10:00** 删除Registration，清理引用
2. **10:00-12:00** 创建MedicalCase模块框架
3. **14:00-16:00** 实现MedicalCase列表基础功能
4. **16:00-17:00** 提交代码，更新进度

---

## 💡 快速命令

### 创建MedicalCase模块结构
```bash
mkdir -p src/Frontend/Desktop/Modules/MedicalCase/{ViewModels,Views,Models,Services}
```

### 查找需要清理的引用
```bash
grep -r "Registration" src/Frontend/ --include="*.cs" --include="*.xaml"
grep -r "RecordManagement" src/Frontend/ --include="*.cs" --include="*.xaml"
```

### 编译测试
```bash
dotnet build LYBT.Desktop.sln
```

---

**更新时间**: 2025-01-08 | **总进度**: 50% | **下一个检查点**: 1月8日 17:00