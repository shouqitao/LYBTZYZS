#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
模型结构对比工具 - Phase 1: 自动化扫描分析
对比前端DTO与后端DTO结构，检查前后端数据契约一致性

用于标准功能检查PRD的模型一致性验证
"""

import os
import re
import json
from pathlib import Path
from typing import Dict, List, Tuple, Set
from datetime import datetime

class ModelStructureAnalyzer:
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.frontend_models = {}
        self.backend_models = {}
        self.shared_models = {}
        
    def extract_class_properties(self, file_content: str, file_path: str) -> List[Dict]:
        """从C#类文件中提取属性信息"""
        classes = []
        
        # 匹配类定义
        class_pattern = r'public\s+(?:partial\s+)?class\s+(\w+)(?:\s*:\s*([^{]+))?\s*{'
        class_matches = re.finditer(class_pattern, file_content, re.MULTILINE)
        
        for class_match in class_matches:
            class_name = class_match.group(1)
            base_classes = class_match.group(2).strip() if class_match.group(2) else ""
            
            # 查找类的属性
            class_start = class_match.end()
            brace_count = 1
            class_end = class_start
            
            # 找到类的结束位置
            for i, char in enumerate(file_content[class_start:], class_start):
                if char == '{':
                    brace_count += 1
                elif char == '}':
                    brace_count -= 1
                    if brace_count == 0:
                        class_end = i
                        break
            
            class_body = file_content[class_start:class_end]
            properties = self.extract_properties(class_body)
            
            classes.append({
                'name': class_name,
                'file_path': file_path,
                'base_classes': base_classes,
                'properties': properties,
                'property_count': len(properties)
            })
        
        return classes
    
    def extract_properties(self, class_body: str) -> List[Dict]:
        """提取类的属性信息"""
        properties = []
        
        # 匹配属性定义 - 更精确的正则表达式
        property_patterns = [
            # public Type PropertyName { get; set; }
            r'public\s+([^\s]+)\s+(\w+)\s*{\s*get;\s*set;\s*}',
            # public Type PropertyName { get; set; } = default_value;
            r'public\s+([^\s]+)\s+(\w+)\s*{\s*get;\s*set;\s*}\s*=\s*([^;]+);',
            # [Required] public Type PropertyName { get; set; }
            r'\[[\w\s,()="]+\]\s*public\s+([^\s]+)\s+(\w+)\s*{\s*get;\s*set;\s*}',
        ]
        
        for pattern in property_patterns:
            matches = re.finditer(pattern, class_body, re.MULTILINE)
            for match in matches:
                property_type = match.group(1).strip()
                property_name = match.group(2).strip()
                default_value = match.group(3).strip() if len(match.groups()) > 2 and match.group(3) else None
                
                # 提取属性的特性（Attributes）
                attributes = self.extract_property_attributes(class_body, match.start())
                
                properties.append({
                    'name': property_name,
                    'type': property_type,
                    'default_value': default_value,
                    'attributes': attributes,
                    'nullable': '?' in property_type
                })
        
        return properties
    
    def extract_property_attributes(self, class_body: str, property_pos: int) -> List[str]:
        """提取属性的特性标注"""
        attributes = []
        
        # 向前查找属性特性
        lines_before = class_body[:property_pos].split('\n')[-5:]  # 查看前5行
        
        for line in reversed(lines_before):
            line = line.strip()
            if line.startswith('[') and line.endswith(']'):
                # 提取特性名称
                attr_match = re.search(r'\[([^\]]+)\]', line)
                if attr_match:
                    attributes.append(attr_match.group(1))
            elif line and not line.startswith('//'):
                break  # 非注释非特性行，停止查找
                
        return list(reversed(attributes))
    
    def scan_models(self) -> Dict:
        """扫描所有模型文件"""
        model_files = {
            'frontend': [],
            'backend': [],
            'shared': []
        }
        
        # 扫描共享模型（Shared.Models）
        for file_path in self.project_root.rglob("**/LYBT.Shared.Models/**/*.cs"):
            if not any(skip in str(file_path) for skip in ['obj', 'bin']):
                model_files['shared'].append(str(file_path))
        
        # 扫描前端模型（Client/Desktop）
        for file_path in self.project_root.rglob("**/Client/Desktop/**/*Model*.cs"):
            if not any(skip in str(file_path) for skip in ['obj', 'bin']):
                model_files['frontend'].append(str(file_path))
                
        # 扫描前端DTO（Client/Desktop）
        for file_path in self.project_root.rglob("**/Client/Desktop/**/*Dto*.cs"):
            if not any(skip in str(file_path) for skip in ['obj', 'bin']):
                model_files['frontend'].append(str(file_path))
        
        # 扫描后端模型（Server）
        for file_path in self.project_root.rglob("**/Server/**/*Dto*.cs"):
            if not any(skip in str(file_path) for skip in ['obj', 'bin']):
                model_files['backend'].append(str(file_path))
                
        # 扫描实体模型
        for file_path in self.project_root.rglob("**/LYBT.Entities/**/*.cs"):
            if not any(skip in str(file_path) for skip in ['obj', 'bin']):
                model_files['backend'].append(str(file_path))
        
        return model_files
    
    def analyze_model_files(self, model_files: Dict) -> Dict:
        """分析模型文件结构"""
        results = {
            'scan_time': datetime.now().isoformat(),
            'frontend_models': [],
            'backend_models': [],
            'shared_models': [],
            'statistics': {
                'frontend_classes': 0,
                'backend_classes': 0,
                'shared_classes': 0,
                'total_properties': 0
            }
        }
        
        # 分析各类模型
        for category, files in model_files.items():
            for file_path in files:
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    classes = self.extract_class_properties(content, file_path)
                    
                    target_list = f"{category}_models"
                    results[target_list].extend(classes)
                    results['statistics'][f"{category}_classes"] += len(classes)
                    
                    for cls in classes:
                        results['statistics']['total_properties'] += cls['property_count']
                        
                except Exception as e:
                    print(f"Error processing {file_path}: {e}")
        
        return results
    
    def find_matching_models(self, analysis_results: Dict) -> Dict:
        """查找前后端匹配的模型"""
        matches = {
            'exact_matches': [],
            'similar_matches': [],
            'frontend_only': [],
            'backend_only': [],
            'inconsistencies': []
        }
        
        # 创建模型名称索引
        frontend_models = {cls['name']: cls for cls in analysis_results['frontend_models']}
        backend_models = {cls['name']: cls for cls in analysis_results['backend_models']}
        shared_models = {cls['name']: cls for cls in analysis_results['shared_models']}
        
        # 查找精确匹配
        common_names = set(frontend_models.keys()) & set(backend_models.keys())
        for name in common_names:
            frontend_model = frontend_models[name]
            backend_model = backend_models[name]
            
            consistency = self.compare_models(frontend_model, backend_model)
            
            if consistency['is_consistent']:
                matches['exact_matches'].append({
                    'name': name,
                    'frontend': frontend_model,
                    'backend': backend_model,
                    'consistency': consistency
                })
            else:
                matches['inconsistencies'].append({
                    'name': name,
                    'frontend': frontend_model,
                    'backend': backend_model,
                    'issues': consistency['issues']
                })
        
        # 查找相似匹配（名称相似但不完全相同）
        for frontend_name in frontend_models:
            if frontend_name not in common_names:
                for backend_name in backend_models:
                    if backend_name not in common_names:
                        similarity = self.calculate_name_similarity(frontend_name, backend_name)
                        if similarity > 0.8:  # 80%相似度
                            matches['similar_matches'].append({
                                'frontend_name': frontend_name,
                                'backend_name': backend_name,
                                'similarity': similarity,
                                'frontend': frontend_models[frontend_name],
                                'backend': backend_models[backend_name]
                            })
        
        # 记录只在前端或后端存在的模型
        matches['frontend_only'] = [name for name in frontend_models if name not in common_names]
        matches['backend_only'] = [name for name in backend_models if name not in common_names]
        
        return matches
    
    def compare_models(self, frontend_model: Dict, backend_model: Dict) -> Dict:
        """比较两个模型的结构一致性"""
        issues = []
        
        # 比较属性数量
        if frontend_model['property_count'] != backend_model['property_count']:
            issues.append(f"属性数量不匹配: 前端{frontend_model['property_count']}个，后端{backend_model['property_count']}个")
        
        # 创建属性名称映射
        frontend_props = {prop['name']: prop for prop in frontend_model['properties']}
        backend_props = {prop['name']: prop for prop in backend_model['properties']}
        
        # 检查属性匹配
        all_prop_names = set(frontend_props.keys()) | set(backend_props.keys())
        
        for prop_name in all_prop_names:
            if prop_name not in frontend_props:
                issues.append(f"属性 '{prop_name}' 只在后端存在")
            elif prop_name not in backend_props:
                issues.append(f"属性 '{prop_name}' 只在前端存在")
            else:
                # 比较属性类型
                frontend_type = frontend_props[prop_name]['type']
                backend_type = backend_props[prop_name]['type']
                
                if frontend_type != backend_type:
                    issues.append(f"属性 '{prop_name}' 类型不匹配: 前端{frontend_type}，后端{backend_type}")
        
        return {
            'is_consistent': len(issues) == 0,
            'issues': issues,
            'consistency_score': 1.0 - (len(issues) / max(len(all_prop_names), 1))
        }
    
    def calculate_name_similarity(self, name1: str, name2: str) -> float:
        """计算两个名称的相似度"""
        # 简单的字符串相似度计算
        name1 = name1.lower()
        name2 = name2.lower()
        
        if name1 == name2:
            return 1.0
        
        # 计算最长公共子序列
        def lcs_length(s1, s2):
            m, n = len(s1), len(s2)
            dp = [[0] * (n + 1) for _ in range(m + 1)]
            
            for i in range(1, m + 1):
                for j in range(1, n + 1):
                    if s1[i-1] == s2[j-1]:
                        dp[i][j] = dp[i-1][j-1] + 1
                    else:
                        dp[i][j] = max(dp[i-1][j], dp[i][j-1])
            
            return dp[m][n]
        
        lcs_len = lcs_length(name1, name2)
        max_len = max(len(name1), len(name2))
        
        return lcs_len / max_len if max_len > 0 else 0.0
    
    def generate_report(self, output_path: str = None) -> Dict:
        """生成模型结构分析报告"""
        model_files = self.scan_models()
        analysis_results = self.analyze_model_files(model_files)
        matches = self.find_matching_models(analysis_results)
        
        report_data = {
            **analysis_results,
            'matches': matches,
            'summary': {
                'total_model_files': sum(len(files) for files in model_files.values()),
                'exact_matches': len(matches['exact_matches']),
                'inconsistencies': len(matches['inconsistencies']),
                'similar_matches': len(matches['similar_matches']),
                'frontend_only': len(matches['frontend_only']),
                'backend_only': len(matches['backend_only'])
            }
        }
        
        # 生成输出文件
        if output_path is None:
            output_path = self.project_root / "docs" / "reports" / f"model-structure-analysis-{datetime.now().strftime('%Y%m%d')}.json"
        
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        
        # JSON报告
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(report_data, f, indent=2, ensure_ascii=False)
        
        # Markdown报告
        markdown_report = self.generate_markdown_report(report_data)
        markdown_path = str(output_path).replace('.json', '.md')
        
        with open(markdown_path, 'w', encoding='utf-8') as f:
            f.write(markdown_report)
        
        return {
            'json_report': output_path,
            'markdown_report': markdown_path,
            'analysis_data': report_data
        }
    
    def generate_markdown_report(self, report_data: Dict) -> str:
        """生成Markdown格式的分析报告"""
        summary = report_data['summary']
        matches = report_data['matches']
        stats = report_data['statistics']
        
        report = f"""# 模型结构一致性分析报告

**分析时间**: {report_data['scan_time']}  
**扫描文件**: {summary['total_model_files']}个模型文件  
**发现类**: 前端{stats['frontend_classes']}个，后端{stats['backend_classes']}个，共享{stats['shared_classes']}个

## 📊 分析统计

| 类别 | 数量 |
|------|------|
| 精确匹配 | {summary['exact_matches']} |
| 不一致问题 | {summary['inconsistencies']} |
| 相似匹配 | {summary['similar_matches']} |
| 仅前端存在 | {summary['frontend_only']} |
| 仅后端存在 | {summary['backend_only']} |

## ✅ 精确匹配的模型

"""
        
        for match in matches['exact_matches']:
            report += f"### {match['name']}\n\n"
            report += f"- **前端位置**: {Path(match['frontend']['file_path']).name}\n"
            report += f"- **后端位置**: {Path(match['backend']['file_path']).name}\n"
            report += f"- **属性数量**: {match['frontend']['property_count']}\n"
            report += f"- **一致性分数**: {match['consistency']['consistency_score']:.2%}\n\n"
        
        if matches['inconsistencies']:
            report += "## ⚠️ 不一致的模型\n\n"
            
            for inconsistency in matches['inconsistencies']:
                report += f"### {inconsistency['name']}\n\n"
                report += f"**发现的问题**:\n"
                for issue in inconsistency['issues']:
                    report += f"- {issue}\n"
                report += "\n"
        
        if matches['similar_matches']:
            report += "## 🔍 相似匹配的模型\n\n"
            
            for similar in matches['similar_matches']:
                report += f"- **{similar['frontend_name']}** (前端) ≈ **{similar['backend_name']}** (后端)\n"
                report += f"  - 相似度: {similar['similarity']:.1%}\n"
        
        if matches['frontend_only']:
            report += "## 📱 仅前端存在的模型\n\n"
            for name in matches['frontend_only']:
                report += f"- {name}\n"
            report += "\n"
        
        if matches['backend_only']:
            report += "## 🖥️ 仅后端存在的模型\n\n"
            for name in matches['backend_only']:
                report += f"- {name}\n"
            report += "\n"
        
        # 一致性评分
        total_models = len(matches['exact_matches']) + len(matches['inconsistencies'])
        if total_models > 0:
            consistency_rate = len(matches['exact_matches']) / total_models
            report += f"""## 🎯 一致性评估

**整体一致性**: {consistency_rate:.1%}  
**匹配模型**: {len(matches['exact_matches'])}/{total_models}  
**问题模型**: {len(matches['inconsistencies'])}/{total_models}

"""
            
            if consistency_rate >= 0.9:
                report += "✅ **评级**: 优秀 - 前后端模型高度一致\n"
            elif consistency_rate >= 0.7:
                report += "⚠️ **评级**: 良好 - 存在少量不一致需要修复\n"
            else:
                report += "❌ **评级**: 需要改进 - 存在较多前后端不一致问题\n"
        
        report += f"""
## 📋 建议行动

1. **修复不一致问题**: 优先处理{len(matches['inconsistencies'])}个不一致的模型
2. **检查相似匹配**: 验证{len(matches['similar_matches'])}个相似模型是否应该统一
3. **清理冗余模型**: 评估单独存在的前端/后端模型是否必要
4. **完善共享模型**: 将通用模型迁移到Shared.Models项目

---

**生成时间**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  
**工具**: 模型结构对比工具 v1.0
"""
        
        return report

def main():
    """主函数"""
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    analyzer = ModelStructureAnalyzer(project_root)
    
    print("开始分析模型结构...")
    results = analyzer.generate_report()
    
    print("分析完成!")
    print(f"JSON报告: {results['json_report']}")
    print(f"Markdown报告: {results['markdown_report']}")
    
    data = results['analysis_data']
    print(f"扫描到前端模型类 {data['statistics']['frontend_classes']} 个")
    print(f"扫描到后端模型类 {data['statistics']['backend_classes']} 个")
    print(f"扫描到共享模型类 {data['statistics']['shared_classes']} 个")
    print(f"精确匹配 {data['summary']['exact_matches']} 对，不一致 {data['summary']['inconsistencies']} 对")

if __name__ == "__main__":
    main()