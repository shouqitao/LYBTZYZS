#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修正文档中的模块冲突问题
纠正错误的模块信息，确保文档反映真实的系统架构
"""

import os
from pathlib import Path
from datetime import datetime

class DocConflictFixer:
    def __init__(self):
        self.script_dir = Path(__file__).parent
        self.project_root = self.script_dir.parent
        self.docs_dir = self.project_root / "docs"
        
        # 真实的模块列表（基于实际代码）
        self.actual_modules = {
            "infrastructure": ["Infrastructure", "Models", "Shared.Models"],
            "business": ["Auth", "Users", "Patients", "Herbs", "Formula", "Consultation", "MedicalCase", "Prescriptions"],
            "deleted": ["Doctors", "Registration", "Queueing", "Billing", "Pharmacy", "Records", "TreatmentRoom", "Sync", "DiagnosisTreatment", "Diagnostics", "Cashier"]
        }
        
        # 需要修正的文件列表
        self.files_to_fix = []
        
    def find_conflicting_docs(self):
        """查找包含错误模块信息的文档"""
        print("正在扫描文档目录...")
        
        # 扫描所有markdown文件
        for md_file in self.docs_dir.rglob("*.md"):
            try:
                content = md_file.read_text(encoding='utf-8')
                
                # 检查是否包含已删除的模块名称
                has_conflict = False
                for module in self.actual_modules["deleted"]:
                    if module in content and "已删除" not in content:
                        has_conflict = True
                        break
                
                if has_conflict:
                    self.files_to_fix.append(md_file)
                    print(f"  发现冲突文档: {md_file.relative_to(self.project_root)}")
                    
            except Exception as e:
                print(f"  读取文件失败 {md_file}: {e}")
        
        print(f"\n共发现 {len(self.files_to_fix)} 个需要修正的文档")
    
    def create_correct_module_doc(self):
        """创建正确的模块状态文档"""
        print("\n创建正确的模块状态文档...")
        
        content = f"""# 凌隐宝堂中医诊所系统 - 模块实现状态（纠正版）

**生成日期**: {datetime.now().strftime('%Y年%m月%d日')}
**版本**: 极简版（中医诊所版）

## 重要说明

本文档是系统模块的权威参考。系统采用极简架构，仅包含核心业务功能。

## 一、系统架构概述

### 1.1 架构特点
- **极简设计**：仅保留必要的核心功能
- **纯中医系统**：专注于中医诊疗，无西医元素
- **流程精简**：患者建档 → 医生看诊 → 开具处方

### 1.2 实际模块数量
- **总模块数**: 11个（3个基础设施 + 8个业务模块）
- **已实现**: 8个业务模块（100%）
- **无待开发模块**

## 二、实际模块清单

### 2.1 基础设施模块（3个）
1. **LYBT.Infrastructure** - 基础设施层
   - 数据访问（EF Core）
   - 日志服务
   - 通用配置
   
2. **LYBT.Models** - 领域模型层
   - 实体定义
   - 业务规则
   
3. **LYBT.Shared.Models** - 共享DTO模型
   - 前后端共享模型
   - 枚举定义
   - 通用工具类

### 2.2 业务模块（8个） [完成] 全部已实现

#### 基础功能（2个）
1. **Auth** - 身份认证和授权
   - JWT认证
   - 登录/登出
   - 密码管理
   - 防暴力破解

2. **Users** - 用户管理
   - 用户CRUD
   - 角色权限
   - **包含医生信息管理**（医生是特殊的用户角色）

#### 人员管理（1个）
3. **Patients** - 患者档案管理
   - 患者信息维护
   - 就诊历史
   - 过敏史管理
   - 档案合并

#### 诊疗核心（2个）
4. **Consultation** - 看诊管理
   - 中医四诊（望闻问切）
   - 中医诊断
   - 治疗建议
   - 医嘱记录

5. **MedicalCase** - 医疗案例
   - 诊疗流程聚合
   - 病例管理
   - 历史追踪

#### 处方药材（3个）
6. **Prescriptions** - 处方管理
   - 中药处方开具
   - 处方审核
   - 历史处方查询

7. **Herbs** - 中药材管理
   - 药材信息
   - 库存状态
   - 价格管理

8. **Formula** - 验方管理
   - 经典方剂
   - 验方模板
   - 方剂组成

## 三、不存在的模块（明确说明）

以下模块在当前极简版本中**不存在**：

### [已删除] 已删除的模块
1. **Doctors** - 独立的医生模块（功能已整合到Users）
2. **Registration** - 挂号模块
3. **Queueing** - 排队模块
4. **Billing/Cashier** - 收费模块
5. **Pharmacy** - 药房模块
6. **Records** - 病历档案模块
7. **TreatmentRoom** - 治疗室模块
8. **Sync** - 数据同步模块
9. **DiagnosisTreatment** - 诊断治疗模块

### 说明
- 医生管理功能已整合到Users模块（BaseUserModel包含医生字段）
- 系统采用直接看诊模式，无需挂号排队
- 收费功能可通过外部系统处理

## 四、当前任务重点

### 4.1 立即任务
1. **修复现有Bug**（8项）
   - 移动PrescriptionItemInfo类位置
   - 清理未使用的类
   - 修正文件命名
   - 优化查询性能
   - 实现处方打印
   - 处方保存功能
   - 添加输入验证
   - 实现数据缓存

### 4.2 功能完善
1. 完善中医四诊记录功能
2. 优化处方模板应用
3. 增强药材搜索功能
4. 提升系统性能

### 4.3 质量提升
1. 补充单元测试
2. 完善API文档
3. 优化用户界面
4. 增强错误处理

## 五、开发原则

1. **保持简洁**：不添加非必要功能
2. **专注中医**：深化中医诊疗特色
3. **用户友好**：优化操作流程
4. **稳定可靠**：确保核心功能稳定

## 六、总结

系统已完成所有核心模块开发，当前重点是：
- 修复已知Bug
- 优化现有功能
- 提升代码质量
- 增强用户体验

**请以此文档为准，忽略其他包含错误模块信息的旧文档。**
"""
        
        output_file = self.docs_dir / "模块实现状态-纠正版-20250108.md"
        output_file.write_text(content, encoding='utf-8')
        print(f"[完成] 创建文档: {output_file.relative_to(self.project_root)}")
    
    def update_todo_docs(self):
        """更新TODO文档"""
        print("\n更新TODO文档...")
        
        content = f"""# 凌隐宝堂中医诊所系统 - 任务清单（纠正版）

**更新日期**: {datetime.now().strftime('%Y年%m月%d日')}
**系统版本**: 极简版（纯中医诊所）

## 一、当前任务（基于实际模块）

### 1.1 紧急Bug修复（8项，约11.5小时）

| 编号 | 任务描述 | 位置 | 预计工时 |
|------|----------|------|----------|
| BUG-001 | 移动PrescriptionItemInfo类到Models目录 | 前端 | 0.5h |
| BUG-002 | 清理ConsultationInfo.cs中未使用的类 | 前端 | 0.5h |
| BUG-003 | 修正FormulaTemplateService文件名 | 前端 | 0.5h |
| BUG-004 | 优化FilterHerbs的LINQ查询性能 | 前端 | 1h |
| BUG-005 | 实现真正的处方打印功能 | 前端 | 3h |
| BUG-006 | 处方保存到后端Prescription模块 | 前端 | 2h |
| BUG-007 | 添加输入验证（数量、必填字段） | 前端 | 2h |
| BUG-008 | 添加数据缓存机制 | 前端 | 2h |

### 1.2 功能优化（基于现有8个模块）

#### Consultation模块优化
- [ ] 完善中医四诊记录界面
- [ ] 增加常见症状快速输入
- [ ] 优化诊断模板

#### Prescriptions模块优化
- [ ] 完善处方模板功能
- [ ] 增加剂量自动计算
- [ ] 实现处方预览功能

#### Herbs模块优化
- [ ] 优化药材搜索算法
- [ ] 完善库存预警机制
- [ ] 增加药材图片支持

#### Formula模块优化
- [ ] 整合FormulaTemplates功能
- [ ] 增加方剂分类管理
- [ ] 完善方剂检索功能

### 1.3 质量提升任务

1. **测试覆盖**
   - [ ] 为8个业务模块添加单元测试
   - [ ] 实现API集成测试
   - [ ] 目标覆盖率：50%

2. **文档完善**
   - [ ] 更新API文档
   - [ ] 编写用户操作手册
   - [ ] 完善开发文档

3. **性能优化**
   - [ ] 优化数据库查询
   - [ ] 实现Redis缓存
   - [ ] 前端响应优化

## 二、明确说明：不存在的任务

以下任务基于不存在的模块，**不应执行**：

[删除] ~~完成Doctors模块开发~~ - Doctors不是独立模块，医生功能在Users中
[删除] ~~实现Registration挂号模块~~ - 系统无挂号功能
[删除] ~~实现Queueing排队模块~~ - 系统无排队功能
[删除] ~~实现Billing收费模块~~ - 系统无内置收费功能
[删除] ~~实现Pharmacy药房模块~~ - 药房功能简化处理
[删除] ~~实现Records病历模块~~ - 病历功能由MedicalCase承担

## 三、开发重点

### 近期目标（1-2周）
1. 完成8项Bug修复
2. 优化核心业务流程
3. 提升系统稳定性

### 中期目标（1个月）
1. 测试覆盖率达到50%
2. 完成所有文档更新
3. 系统性能优化

### 长期规划
1. 根据用户反馈迭代优化
2. 考虑移动端支持
3. 数据分析功能

## 四、资源分配

- **Bug修复**: 1人，2天
- **功能优化**: 1人，1周
- **测试编写**: 1人，1周
- **文档更新**: 按需分配

## 五、注意事项

1. **坚持极简原则**：不盲目添加功能
2. **专注核心业务**：深化中医诊疗特色
3. **保证质量**：每个功能都要稳定可靠
4. **用户体验优先**：操作简单直观

---

**本文档基于系统实际架构编写，请忽略其他包含错误模块信息的文档。**
"""
        
        output_file = self.docs_dir / "TODO-纠正版-20250108.md"
        output_file.write_text(content, encoding='utf-8')
        print(f"[完成] 创建文档: {output_file.relative_to(self.project_root)}")
    
    def archive_conflicting_docs(self):
        """归档有冲突的文档"""
        print("\n归档冲突文档...")
        
        archive_dir = self.docs_dir / "09-项目记录" / "历史文档" / "存在冲突的文档"
        archive_dir.mkdir(parents=True, exist_ok=True)
        
        # 需要归档的文档列表
        docs_to_archive = [
            "TODO-Latest-20250108.md",
            "TODO-Latest-20250108-V2.md",
            "项目实现状态报告-20250108.md",
            "04-模块实现/模块开发进度追踪-20250108.md"
        ]
        
        for doc_name in docs_to_archive:
            doc_path = self.docs_dir / doc_name
            if doc_path.exists():
                new_path = archive_dir / f"[冲突]{doc_path.name}"
                doc_path.rename(new_path)
                print(f"  归档: {doc_name} -> 历史文档/存在冲突的文档/")
    
    def create_summary_report(self):
        """创建修正总结报告"""
        print("\n创建修正总结报告...")
        
        content = f"""# 文档冲突修正报告

**修正日期**: {datetime.now().strftime('%Y年%m月%d日')}
**执行人**: 自动修正脚本

## 一、问题说明

发现多个文档包含了不存在的模块信息，造成了严重的文档与代码不一致问题。

### 错误信息包括：
1. 提到了不存在的Doctors独立模块（实际医生功能在Users模块中）
2. 提到了已删除的Registration、Queueing、Billing等模块
3. TODO列表包含了基于不存在模块的任务

## 二、修正措施

### 2.1 创建权威文档
- 《模块实现状态-纠正版-20250108.md》
- 《TODO-纠正版-20250108.md》

### 2.2 归档冲突文档
将包含错误信息的文档移至历史文档目录，并标记为[冲突]

### 2.3 明确系统架构
- 系统只有8个业务模块
- 医生功能集成在Users模块
- 无挂号、排队、收费等模块

## 三、正确的模块列表

### 业务模块（8个）
1. Auth - 认证授权
2. Users - 用户管理（包含医生）
3. Patients - 患者管理
4. Consultation - 看诊管理
5. MedicalCase - 病例管理
6. Prescriptions - 处方管理
7. Herbs - 药材管理
8. Formula - 验方管理

## 四、后续建议

1. **使用新文档**：以纠正版文档为准
2. **更新CLAUDE.md**：添加正确的模块列表
3. **团队同步**：确保所有人了解正确的系统架构
4. **定期检查**：避免旧信息再次出现

## 五、经验教训

1. 文档应该与代码保持同步
2. 架构变更需要及时更新所有相关文档
3. 建立文档审查机制
4. 定期清理过时信息

---

**重要提醒**：今后请以纠正版文档为准，确保不再引用已删除的模块。
"""
        
        output_file = self.docs_dir / "10-项目规范" / "文档冲突修正报告-20250108.md"
        output_file.write_text(content, encoding='utf-8')
        print(f"[完成] 创建报告: {output_file.relative_to(self.project_root)}")
    
    def run(self):
        """执行修正流程"""
        print("="*50)
        print("开始修正文档冲突问题")
        print("="*50)
        
        # 查找冲突文档
        self.find_conflicting_docs()
        
        # 创建正确的文档
        self.create_correct_module_doc()
        self.update_todo_docs()
        
        # 归档冲突文档
        self.archive_conflicting_docs()
        
        # 创建总结报告
        self.create_summary_report()
        
        print("\n[完成] 文档冲突修正完成！")
        print("请使用纠正版文档作为参考。")

def main():
    try:
        fixer = DocConflictFixer()
        fixer.run()
    except Exception as e:
        print(f"\n[错误] 发生错误: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()