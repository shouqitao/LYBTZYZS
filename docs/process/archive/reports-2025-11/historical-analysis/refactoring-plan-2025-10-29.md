# 项目重构计划

**创建时间**：2025-10-29
**计划类型**：文档驱动的全面重构
**预计周期**：4-6周

---

## 📋 重构背景

### 触发原因
1. **运行时问题积累**：Issue #1701实施后发现多个运行时问题
2. **文档缺失严重**：模块文档完成度仅22%（2/9）
3. **代码与文档不同步**：index.md声称完整文档，实际缺失7个模块
4. **Issue/PR积压**：8个Open Issues，1个未合并PR

### 决策
- ✅ 关闭所有Issue和PR（已完成）
- ✅ 删除feature分支（已完成）
- ✅ 切换到master分支（已完成）
- 🔄 采用**文档驱动开发**方式重构

---

## 🎯 重构目标

### 核心目标
1. **文档完整性**：补齐7个缺失的模块文档（达到100%）
2. **文档代码对齐**：确保文档与代码100%同步
3. **架构清晰化**：基于文档重新梳理三层对齐架构
4. **质量可控**：建立文档审查和质量检查流程

### 成功指标
- ✅ 模块文档：9/9（100%完成度）
- ✅ 文档链接：0个404错误
- ✅ 代码同步率：100%
- ✅ 编译通过：0 errors, 0 warnings
- ✅ 运行时验证：所有核心功能可用

---

## 📊 当前状态基线

### 文档现状（基于documentation-gap-analysis-2025-10-29.md）
| 分类 | 总数 | 存在 | 缺失 | 完成度 |
|-----|------|------|------|--------|
| Level 1 快速参考 | 5 | 5 | 0 | 100% |
| Level 2 架构指南 | 18 | 18 | 0 | 100% |
| Level 3 深度参考 | 5 | 5 | 0 | 100% |
| Level 3 API文档 | 1 | 1 | 0 | 100% |
| **Level 3 模块文档** | **9** | **2** | **7** | **22%** |

### 缺失的7个模块文档
1. ❌ `modules/auth/README.md` - 认证模块（P0高优先级）
2. ❌ `modules/patients/README.md` - 患者模块（P0高优先级）
3. ❌ `modules/consultation/README.md` - 诊疗模块（P1中优先级）
4. ❌ `modules/prescriptions/README.md` - 处方模块（P1中优先级）
5. ❌ `modules/herbs/README.md` - 药材模块（P2低优先级）
6. ❌ `modules/formula/README.md` - 验方模块（P2低优先级）
7. ❌ `modules/users/README.md` - 用户模块（P2低优先级）

### 代码现状
- ✅ 编译通过：0 errors, 1 warning（MedicalCaseRepository.cs:161）
- ✅ 项目结构完整：Server端8个模块、Desktop端8个模块
- ⚠️ 运行时问题：部分功能未经过完整测试

---

## 🗓️ 4阶段重构计划

### Phase 1：文档清理与规范（1周）

**目标**：清理当前文档体系，建立规范模板

**任务清单**：
- [ ] **Task 1.1**：更新docs/index.md
  - 移除7个缺失模块的链接
  - 标注"（文档待补充）"或临时隐藏
  - 更新完成度统计（实际26/33而非声称的100%）

- [ ] **Task 1.2**：创建模块文档模板
  - 基于现有modules/medical-case/README.md创建标准模板
  - 模板包含：模块概述、架构设计、核心实体、API列表、业务流程、代码示例、常见问题
  - 验证：模板至少包含8个标准章节

- [ ] **Task 1.3**：清理过时文档
  - 扫描docs/目录，识别过时文档
  - 归档到docs/archive/
  - 更新文档导航链接

**输出物**：
- ✅ docs/index.md（修正版）
- ✅ docs/templates/module-template.md（模块文档模板）
- ✅ 过时文档归档清单

---

### Phase 2：核心模块文档补齐（2周）

**目标**：补齐P0和P1优先级的4个核心模块文档

#### Week 1：P0核心模块（2个）

**Task 2.1：认证模块文档（Auth）**
- [ ] 使用Serena MCP分析代码结构
  - 分析src/Server/Modules/LYBT.Module.Auth/
  - 分析src/Client/Desktop/Modules/LYBT.Desktop.Auth/
- [ ] 基于模板创建modules/auth/README.md
  - 双轨认证系统说明（Users表 + AdminSecrets表）
  - JWT机制（AccessToken 2h + RefreshToken 7d）
  - 认证流程图（登录、刷新、超级管理员）
  - API端点列表（/api/v1/auth/*）
- [ ] 验证：代码示例可运行，API端点准确

**Task 2.2：患者模块文档（Patients）**
- [ ] 使用Serena MCP分析代码结构
  - 分析src/Server/Modules/LYBT.Module.Patients/
  - 分析src/Client/Desktop/Modules/LYBT.Desktop.Patients/
- [ ] 基于模板创建modules/patients/README.md
  - 患者管理功能（CRUD、搜索、统计）
  - Excel导入功能（Issue #1202相关）
  - 患者档案结构（基础信息、联系方式、就诊历史）
  - API端点列表（/api/v1/patients/*）
- [ ] 验证：功能列表与实际代码对齐

#### Week 2：P1核心模块（2个）

**Task 2.3：诊疗模块文档（Consultation）**
- [ ] 使用Serena MCP分析代码结构
- [ ] 基于模板创建modules/consultation/README.md
  - 四诊合参功能（望闻问切）
  - 辨证论治流程（中医诊断、治法方案）
  - 与MedicalCase/Prescription的关系
  - API端点列表
- [ ] 验证：业务流程图与实际流程一致

**Task 2.4：处方模块文档（Prescriptions）**
- [ ] 使用Serena MCP分析代码结构
- [ ] 基于模板创建modules/prescriptions/README.md
  - 四种录入方式（8列快速输入、详细编辑、历史处方、验方模板）
  - 药材配伍检查（如有实现）
  - 价格计算逻辑
  - 处方状态管理（草稿/正式/作废）
  - API端点列表
- [ ] 验证：包含Issue #1701的教训总结

**输出物**：
- ✅ modules/auth/README.md（P0）
- ✅ modules/patients/README.md（P0）
- ✅ modules/consultation/README.md（P1）
- ✅ modules/prescriptions/README.md（P1）

---

### Phase 3：辅助模块文档补齐（1周）

**目标**：补齐P2优先级的3个辅助模块文档

**Task 3.1：药材模块文档（Herbs）**
- [ ] 基于模板创建modules/herbs/README.md
  - 药材字典功能（2000+药材）
  - 拼音码检索（Issue #1362相关）
  - 价格管理
  - API端点列表

**Task 3.2：验方模块文档（Formula）**
- [ ] 基于模板创建modules/formula/README.md
  - 验方模板管理
  - 智能推荐功能（如有）
  - 统计分析
  - API端点列表

**Task 3.3：用户模块文档（Users）**
- [ ] 基于模板创建modules/users/README.md
  - 用户管理（CRUD）
  - 角色权限系统
  - 密码安全（加密、策略）
  - 与Auth模块的关系
  - API端点列表

**输出物**：
- ✅ modules/herbs/README.md（P2）
- ✅ modules/formula/README.md（P2）
- ✅ modules/users/README.md（P2）

---

### Phase 4：文档与代码对齐验证（1周）

**目标**：确保文档与代码100%同步，建立持续维护机制

**Task 4.1：代码验证**
- [ ] 逐模块编译验证（0 errors, 0 warnings）
- [ ] 修复1个existing warning（MedicalCaseRepository.cs:161）
- [ ] 运行时功能测试（核心流程）

**Task 4.2：文档质量检查**
- [ ] 使用文档检查脚本验证所有链接有效（0个404）
- [ ] 验证代码示例可编译
- [ ] 验证API端点准确性（对比Swagger）
- [ ] 验证架构图与实际代码一致

**Task 4.3：建立持续维护机制**
- [ ] 创建文档审查Checklist（docs/support/documentation-review-checklist.md）
- [ ] PR模板添加"文档更新"检查项
- [ ] 建立月度文档同步机制

**输出物**：
- ✅ 文档质量检查报告
- ✅ 文档审查Checklist
- ✅ PR模板更新

---

## 🔧 工具与方法

### MCP工具使用策略

**代码分析工具**（优先使用）：
1. **Serena MCP**：
   - `find_symbol`：查找类、方法、接口
   - `get_symbols_overview`：获取文件符号概览
   - `search_for_pattern`：搜索特定代码模式
   - `find_referencing_symbols`：查找引用关系

2. **Filesystem MCP**：
   - `list_directory`：列出目录结构
   - `read_text_file`：读取代码文件
   - `search_files`：查找文件

3. **Context7 MCP**：
   - 查询最新.NET 8、WPF、Prism文档
   - 验证最佳实践

**文档生成流程**：
```
1. Serena分析代码结构
   → 2. 提取核心类和方法
   → 3. 基于模板生成文档框架
   → 4. 补充业务逻辑说明
   → 5. 添加代码示例
   → 6. 验证与代码同步
```

---

## ⚠️ 风险与应对

### 风险1：文档生成工作量超预期
**应对**：
- 优先完成P0模块（Auth、Patients）
- P1和P2可分阶段交付
- 使用MCP工具加速代码分析

### 风险2：文档与代码不一致
**应对**：
- 每个模块文档完成后立即运行时验证
- 使用Serena MCP确保API准确性
- 建立文档审查流程

### 风险3：重构期间新需求出现
**应对**：
- 重构期间冻结新功能开发
- 新需求记录到Backlog，重构后评估
- 紧急Bug修复可插队，但需同步文档

---

## 📈 进度跟踪

### 里程碑
| 里程碑 | 预计时间 | 关键交付物 |
|-------|---------|----------|
| **M1：清理完成** | Week 1 | index.md修正、模块模板 |
| **M2：核心模块文档** | Week 3 | 4个P0/P1模块文档 |
| **M3：辅助模块文档** | Week 4 | 3个P2模块文档 |
| **M4：对齐验证** | Week 5 | 质量检查报告、维护机制 |

### 每周检查点
- **每周一**：回顾上周进度，调整本周计划
- **每周五**：提交本周完成的文档，运行质量检查脚本

---

## ✅ 验收标准

### 文档完整性
- [ ] 模块文档完成度：9/9（100%）
- [ ] 所有index.md链接有效（0个404）
- [ ] 每个模块文档包含8个标准章节

### 文档质量
- [ ] 代码示例可编译
- [ ] API端点与Swagger一致
- [ ] 架构图与代码一致
- [ ] 业务流程图准确

### 代码质量
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 核心功能运行时验证通过
- [ ] 单元测试覆盖率≥60%（如有）

---

## 📚 参考资料

**相关文档**：
- docs/reports/documentation-gap-analysis-2025-10-29.md - 文档缺失分析
- docs/index.md - 文档导航中心
- docs/architecture/README.md - 三层对齐架构
- modules/medical-case/README.md - 现有模块文档示例

**关闭的Issue/PR**：
- PR #1702：PrescriptionView合并（未合并，教训参考）
- Issue #1701-1709：已关闭（not_planned）

---

**计划创建**：2025-10-29
**计划负责**：项目团队
**下次审查**：Week 1结束（2025-11-05）
