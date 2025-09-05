#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
功能覆盖率报告生成工具 - Phase 1: 自动化扫描分析
综合分析前后端功能覆盖情况，生成完整的功能覆盖率报告

基于API端点扫描和模型结构分析结果，评估8个业务模块的功能完整性
"""

import os
import json
from pathlib import Path
from typing import Dict, List
from datetime import datetime

class FunctionCoverageReporter:
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.module_definitions = {
            "Auth": {
                "required_apis": ["login", "logout", "refresh", "validate", "changeSysAdminPassword"],
                "required_models": ["LoginDto", "TokenDto", "AuthResponse"],
                "priority": "critical"
            },
            "Users": {
                "required_apis": ["profile", "roles", "active", "reset-password"],
                "required_models": ["UserDto", "UserCreateDto", "UserUpdateDto"],
                "priority": "high"
            },
            "Patients": {
                "required_apis": ["import", "export", "export-template", "validate-import", "by-idcard", "by-phone"],
                "required_models": ["PatientDto", "PatientCreateDto", "PatientUpdateDto"],
                "priority": "critical"
            },
            "MedicalCase": {
                "required_apis": ["complete", "suspend", "resume", "archive", "statistics"],
                "required_models": ["MedicalCaseDto", "MedicalCaseCreateDto", "MedicalCaseUpdateDto"],
                "priority": "critical"
            },
            "Consultation": {
                "required_apis": ["start", "four-diagnosis", "patient", "medical-case", "statistics"],
                "required_models": ["ConsultationDto", "FourDiagnosisDto"],
                "priority": "critical"
            },
            "Prescriptions": {
                "required_apis": ["copy", "validate", "patient", "medical-case"],
                "required_models": ["PrescriptionDto", "PrescriptionItemDto"],
                "priority": "high"
            },
            "Herbs": {
                "required_apis": ["categories", "search", "import", "export", "export-template", "validate-import"],
                "required_models": ["HerbDto", "HerbCreateDto", "HerbUpdateDto"],
                "priority": "medium"
            },
            "Formulas": {
                "required_apis": ["templates", "by-type", "recommendations", "categories", "import", "export", "template"],
                "required_models": ["FormulaDto", "FormulaCreateDto", "FormulaUpdateDto"],
                "priority": "medium"
            }
        }
    
    def load_api_scan_results(self) -> Dict:
        """加载API端点扫描结果"""
        reports_dir = self.project_root / "docs" / "reports"
        
        # 寻找最新的API扫描报告
        api_reports = list(reports_dir.glob("api-endpoints-scan-*.json"))
        if not api_reports:
            raise FileNotFoundError("找不到API端点扫描报告")
        
        latest_report = max(api_reports, key=lambda x: x.stat().st_mtime)
        
        with open(latest_report, 'r', encoding='utf-8') as f:
            return json.load(f)
    
    def load_model_analysis_results(self) -> Dict:
        """加载模型结构分析结果"""
        reports_dir = self.project_root / "docs" / "reports"
        
        # 寻找最新的模型分析报告
        model_reports = list(reports_dir.glob("model-structure-analysis-*.json"))
        if not model_reports:
            raise FileNotFoundError("找不到模型结构分析报告")
        
        latest_report = max(model_reports, key=lambda x: x.stat().st_mtime)
        
        with open(latest_report, 'r', encoding='utf-8') as f:
            return json.load(f)
    
    def analyze_module_coverage(self, api_data: Dict, model_data: Dict) -> Dict:
        """分析各模块的功能覆盖率"""
        coverage_results = {
            'modules': {},
            'overall_stats': {
                'total_modules': len(self.module_definitions),
                'fully_covered': 0,
                'partially_covered': 0,
                'missing_coverage': 0,
                'critical_issues': []
            }
        }
        
        # 构建API端点索引
        api_index = {}
        for controller in api_data['controllers']:
            controller_name = controller['controller'].replace('Controller', '')
            if controller_name not in api_index:
                api_index[controller_name] = []
            
            for endpoint in controller['endpoints']:
                api_index[controller_name].append(endpoint['path'].split('/')[-1])
        
        # 分析每个模块
        for module_name, requirements in self.module_definitions.items():
            module_analysis = self.analyze_single_module(
                module_name, requirements, api_index, model_data
            )
            coverage_results['modules'][module_name] = module_analysis
            
            # 更新整体统计
            if module_analysis['coverage_score'] >= 0.9:
                coverage_results['overall_stats']['fully_covered'] += 1
            elif module_analysis['coverage_score'] >= 0.5:
                coverage_results['overall_stats']['partially_covered'] += 1
            else:
                coverage_results['overall_stats']['missing_coverage'] += 1
                
            # 收集关键问题
            if requirements['priority'] == 'critical' and module_analysis['coverage_score'] < 0.8:
                coverage_results['overall_stats']['critical_issues'].append({
                    'module': module_name,
                    'score': module_analysis['coverage_score'],
                    'issues': module_analysis['missing_features']
                })
        
        return coverage_results
    
    def analyze_single_module(self, module_name: str, requirements: Dict, 
                            api_index: Dict, model_data: Dict) -> Dict:
        """分析单个模块的覆盖情况"""
        result = {
            'module_name': module_name,
            'priority': requirements['priority'],
            'api_coverage': 0.0,
            'model_coverage': 0.0,
            'coverage_score': 0.0,
            'available_apis': [],
            'missing_apis': [],
            'available_models': [],
            'missing_models': [],
            'missing_features': []
        }
        
        # 检查API覆盖
        module_apis = api_index.get(module_name, [])
        required_apis = requirements['required_apis']
        
        found_apis = []
        missing_apis = []
        
        for required_api in required_apis:
            found = False
            for available_api in module_apis:
                if required_api.lower() in available_api.lower():
                    found_apis.append(required_api)
                    found = True
                    break
            
            if not found:
                missing_apis.append(required_api)
        
        result['available_apis'] = found_apis
        result['missing_apis'] = missing_apis
        result['api_coverage'] = len(found_apis) / len(required_apis) if required_apis else 1.0
        
        # 检查模型覆盖（简化检查，基于命名匹配）
        required_models = requirements['required_models']
        all_models = []
        
        # 收集所有模型名称
        for model_list in [model_data['frontend_models'], model_data['backend_models'], model_data['shared_models']]:
            all_models.extend([model['name'] for model in model_list])
        
        found_models = []
        missing_models = []
        
        for required_model in required_models:
            found = False
            for available_model in all_models:
                if required_model.lower().replace('dto', '') in available_model.lower():
                    found_models.append(required_model)
                    found = True
                    break
            
            if not found:
                missing_models.append(required_model)
        
        result['available_models'] = found_models
        result['missing_models'] = missing_models
        result['model_coverage'] = len(found_models) / len(required_models) if required_models else 1.0
        
        # 计算总体覆盖分数
        result['coverage_score'] = (result['api_coverage'] + result['model_coverage']) / 2
        
        # 收集缺失的功能
        result['missing_features'] = missing_apis + missing_models
        
        return result
    
    def analyze_frontend_backend_consistency(self, coverage_data: Dict) -> Dict:
        """分析前后端一致性"""
        consistency_analysis = {
            'consistent_modules': [],
            'inconsistent_modules': [],
            'major_gaps': [],
            'recommendations': []
        }
        
        for module_name, module_data in coverage_data['modules'].items():
            api_coverage = module_data['api_coverage']
            model_coverage = module_data['model_coverage']
            
            # 一致性评判
            consistency_score = 1.0 - abs(api_coverage - model_coverage)
            
            if consistency_score >= 0.8:
                consistency_analysis['consistent_modules'].append({
                    'module': module_name,
                    'score': consistency_score,
                    'status': 'good'
                })
            else:
                consistency_analysis['inconsistent_modules'].append({
                    'module': module_name,
                    'score': consistency_score,
                    'api_coverage': api_coverage,
                    'model_coverage': model_coverage,
                    'issues': module_data['missing_features']
                })
            
            # 识别重大差距
            if abs(api_coverage - model_coverage) > 0.5:
                consistency_analysis['major_gaps'].append({
                    'module': module_name,
                    'gap_type': 'api_ahead' if api_coverage > model_coverage else 'model_ahead',
                    'gap_size': abs(api_coverage - model_coverage)
                })
        
        # 生成建议
        if consistency_analysis['major_gaps']:
            consistency_analysis['recommendations'].append("存在重大前后端功能差距，需要优先修复")
        
        if len(consistency_analysis['inconsistent_modules']) > 4:
            consistency_analysis['recommendations'].append("超过半数模块存在一致性问题，建议系统性重构")
        
        return consistency_analysis
    
    def generate_comprehensive_report(self, output_path: str = None) -> Dict:
        """生成综合功能覆盖率报告"""
        try:
            # 加载数据
            api_data = self.load_api_scan_results()
            model_data = self.load_model_analysis_results()
            
            # 分析覆盖率
            coverage_data = self.analyze_module_coverage(api_data, model_data)
            
            # 分析一致性
            consistency_data = self.analyze_frontend_backend_consistency(coverage_data)
            
            # 组合完整报告
            comprehensive_report = {
                'generation_time': datetime.now().isoformat(),
                'data_sources': {
                    'api_scan_time': api_data['scan_time'],
                    'model_analysis_time': model_data['scan_time']
                },
                'coverage_analysis': coverage_data,
                'consistency_analysis': consistency_data,
                'executive_summary': self.generate_executive_summary(coverage_data, consistency_data)
            }
            
            # 生成输出文件
            if output_path is None:
                output_path = self.project_root / "docs" / "reports" / f"function-coverage-report-{datetime.now().strftime('%Y%m%d')}.json"
            
            os.makedirs(os.path.dirname(output_path), exist_ok=True)
            
            # JSON报告
            with open(output_path, 'w', encoding='utf-8') as f:
                json.dump(comprehensive_report, f, indent=2, ensure_ascii=False)
            
            # Markdown报告
            markdown_report = self.generate_markdown_report(comprehensive_report)
            markdown_path = str(output_path).replace('.json', '.md')
            
            with open(markdown_path, 'w', encoding='utf-8') as f:
                f.write(markdown_report)
            
            return {
                'json_report': output_path,
                'markdown_report': markdown_path,
                'report_data': comprehensive_report
            }
            
        except Exception as e:
            print(f"Error generating report: {e}")
            raise
    
    def generate_executive_summary(self, coverage_data: Dict, consistency_data: Dict) -> Dict:
        """生成执行摘要"""
        stats = coverage_data['overall_stats']
        
        summary = {
            'overall_health': 'good',
            'coverage_percentage': 0,
            'critical_issues_count': len(stats['critical_issues']),
            'key_findings': [],
            'priority_actions': []
        }
        
        # 计算整体覆盖率
        total_score = sum(module['coverage_score'] for module in coverage_data['modules'].values())
        summary['coverage_percentage'] = (total_score / len(coverage_data['modules'])) * 100
        
        # 健康状况评估
        if summary['coverage_percentage'] >= 80:
            summary['overall_health'] = 'excellent'
        elif summary['coverage_percentage'] >= 60:
            summary['overall_health'] = 'good'
        elif summary['coverage_percentage'] >= 40:
            summary['overall_health'] = 'fair'
        else:
            summary['overall_health'] = 'poor'
        
        # 关键发现
        if stats['fully_covered'] >= 6:
            summary['key_findings'].append(f"系统功能完整性良好：{stats['fully_covered']}/8 个模块完全覆盖")
        
        if len(consistency_data['inconsistent_modules']) > 0:
            summary['key_findings'].append(f"发现 {len(consistency_data['inconsistent_modules'])} 个模块存在前后端不一致")
        
        if consistency_data['major_gaps']:
            summary['key_findings'].append(f"检测到 {len(consistency_data['major_gaps'])} 个重大功能差距")
        
        # 优先行动
        if stats['critical_issues']:
            summary['priority_actions'].append("立即修复关键模块的功能缺失")
        
        if len(consistency_data['inconsistent_modules']) > 2:
            summary['priority_actions'].append("优化前后端API契约一致性")
        
        return summary
    
    def generate_markdown_report(self, report_data: Dict) -> str:
        """生成Markdown格式的综合报告"""
        coverage_data = report_data['coverage_analysis']
        consistency_data = report_data['consistency_analysis']
        summary = report_data['executive_summary']
        
        report = f"""# 功能覆盖率综合分析报告

**生成时间**: {report_data['generation_time']}  
**数据源**: API扫描({report_data['data_sources']['api_scan_time'][:10]}) + 模型分析({report_data['data_sources']['model_analysis_time'][:10]})

## 🎯 执行摘要

**整体健康状况**: {summary['overall_health'].upper()}  
**功能覆盖率**: {summary['coverage_percentage']:.1f}%  
**关键问题数**: {summary['critical_issues_count']}

### 关键发现
"""
        
        for finding in summary['key_findings']:
            report += f"- {finding}\n"
        
        report += "\n### 优先行动\n"
        for action in summary['priority_actions']:
            report += f"- {action}\n"
        
        report += f"""

## 📊 模块覆盖情况

| 模块名 | 优先级 | API覆盖率 | 模型覆盖率 | 综合评分 | 状态 |
|--------|--------|-----------|-----------|----------|------|
"""
        
        for module_name, module_data in coverage_data['modules'].items():
            status = "✅" if module_data['coverage_score'] >= 0.9 else "⚠️" if module_data['coverage_score'] >= 0.5 else "❌"
            report += f"| {module_name} | {module_data['priority']} | {module_data['api_coverage']:.1%} | {module_data['model_coverage']:.1%} | {module_data['coverage_score']:.1%} | {status} |\n"
        
        # 详细模块分析
        report += "\n## 🔍 详细模块分析\n\n"
        
        for module_name, module_data in coverage_data['modules'].items():
            if module_data['coverage_score'] < 0.9:  # 只显示不完整的模块
                report += f"### {module_name} ({module_data['priority']}优先级)\n\n"
                report += f"**综合评分**: {module_data['coverage_score']:.1%}\n\n"
                
                if module_data['missing_apis']:
                    report += f"**缺失API**: {', '.join(module_data['missing_apis'])}\n"
                
                if module_data['missing_models']:
                    report += f"**缺失模型**: {', '.join(module_data['missing_models'])}\n"
                
                report += "\n"
        
        # 前后端一致性分析
        if consistency_data['inconsistent_modules']:
            report += "## ⚠️ 前后端一致性问题\n\n"
            
            for issue in consistency_data['inconsistent_modules']:
                report += f"### {issue['module']}\n"
                report += f"- 一致性评分: {issue['score']:.1%}\n"
                report += f"- API覆盖率: {issue['api_coverage']:.1%}\n"
                report += f"- 模型覆盖率: {issue['model_coverage']:.1%}\n"
                if issue['issues']:
                    report += f"- 问题: {', '.join(issue['issues'])}\n"
                report += "\n"
        
        # 重大差距
        if consistency_data['major_gaps']:
            report += "## 🚨 重大功能差距\n\n"
            
            for gap in consistency_data['major_gaps']:
                gap_desc = "API功能超前" if gap['gap_type'] == 'api_ahead' else "模型结构超前"
                report += f"- **{gap['module']}**: {gap_desc} ({gap['gap_size']:.1%}差距)\n"
            report += "\n"
        
        # 总体评估
        overall_coverage = coverage_data['overall_stats']
        report += f"""## 📈 总体评估

### 模块完成度
- **完全覆盖**: {overall_coverage['fully_covered']}/8 模块 ({overall_coverage['fully_covered']/8:.1%})
- **部分覆盖**: {overall_coverage['partially_covered']}/8 模块
- **覆盖不足**: {overall_coverage['missing_coverage']}/8 模块

### 系统健康指标
- **功能完整性**: {summary['coverage_percentage']:.1f}% ({summary['overall_health']})
- **前后端一致**: {len(consistency_data['consistent_modules'])}/{len(coverage_data['modules'])} 模块
- **关键问题**: {len(overall_coverage['critical_issues'])} 个

## 📋 行动建议

### 立即行动 (高优先级)
1. **修复关键模块缺失功能** - 优先处理 Auth、Patients、MedicalCase、Consultation
2. **解决前后端不一致** - 重点关注API契约和数据模型匹配

### 近期行动 (中优先级) 
3. **完善Excel导入导出** - Herbs、Formulas模块功能增强
4. **统一模型定义** - 将重复模型迁移到Shared.Models

### 长期优化 (低优先级)
5. **代码重构** - 清理冗余模型和未使用的API
6. **测试覆盖** - 增加端到端测试验证

---

**生成时间**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  
**工具**: 功能覆盖率分析工具 v1.0  
**基础数据**: API端点扫描 + 模型结构分析
"""
        
        return report

def main():
    """主函数"""
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    reporter = FunctionCoverageReporter(project_root)
    
    print("开始生成功能覆盖率报告...")
    
    try:
        results = reporter.generate_comprehensive_report()
        
        print("报告生成完成!")
        print(f"JSON报告: {results['json_report']}")
        print(f"Markdown报告: {results['markdown_report']}")
        
        # 显示摘要信息
        summary = results['report_data']['executive_summary']
        print(f"整体覆盖率: {summary['coverage_percentage']:.1f}%")
        print(f"系统健康状况: {summary['overall_health']}")
        print(f"关键问题数: {summary['critical_issues_count']}")
        
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    main()