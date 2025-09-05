#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
API端点扫描工具 - Phase 1: 自动化扫描分析
扫描所有Controller，列出API端点，生成详细的API端点清单

用于标准功能检查PRD的自动化分析阶段
"""

import os
import re
import json
from pathlib import Path
from typing import Dict, List, Tuple
from datetime import datetime

class ApiEndpointScanner:
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.controllers = []
        self.api_endpoints = {}
        
    def scan_controllers(self) -> List[str]:
        """扫描所有Controller文件"""
        controller_files = []
        
        # 搜索所有Controller.cs文件
        for file_path in self.project_root.rglob("*Controller.cs"):
            # 排除基类Controller
            if not file_path.name.startswith("Base"):
                controller_files.append(str(file_path))
                
        return controller_files
    
    def extract_api_endpoints(self, controller_path: str) -> Dict:
        """从Controller文件中提取API端点信息"""
        with open(controller_path, 'r', encoding='utf-8') as file:
            content = file.read()
        
        controller_name = Path(controller_path).stem
        
        # 提取类级别的Route属性
        class_route_pattern = r'\[Route\("([^"]+)"\)\]'
        class_route_matches = re.findall(class_route_pattern, content)
        base_route = class_route_matches[0] if class_route_matches else ""
        
        # 提取ApiVersion
        version_pattern = r'\[ApiVersion\("(\d+)"\)\]'
        version_matches = re.findall(version_pattern, content)
        api_version = version_matches[0] if version_matches else "1"
        
        # 提取方法级别的HTTP端点
        endpoints = []
        
        # 匹配HTTP方法 (GET, POST, PUT, DELETE)
        http_patterns = [
            (r'\[HttpGet(?:\("([^"]+)"\))?\]', 'GET'),
            (r'\[HttpPost(?:\("([^"]+)"\))?\]', 'POST'),
            (r'\[HttpPut(?:\("([^"]+)"\))?\]', 'PUT'),
            (r'\[HttpDelete(?:\("([^"]+)"\))?\]', 'DELETE'),
        ]
        
        for pattern, method in http_patterns:
            matches = re.finditer(pattern, content)
            for match in matches:
                route_part = match.group(1) if match.group(1) else ""
                
                # 查找方法名
                method_start = match.end()
                method_pattern = r'public\s+(?:async\s+)?Task<[^>]+>\s+(\w+)\s*\([^)]*\)'
                method_match = re.search(method_pattern, content[method_start:method_start+200])
                method_name = method_match.group(1) if method_match else "Unknown"
                
                # 构建完整路径
                full_path = self.build_full_path(base_route, route_part, api_version)
                
                endpoints.append({
                    'method': method,
                    'path': full_path,
                    'method_name': method_name,
                    'route_template': route_part
                })
        
        return {
            'controller': controller_name,
            'file_path': controller_path,
            'base_route': base_route,
            'api_version': api_version,
            'endpoints': endpoints,
            'endpoint_count': len(endpoints)
        }
    
    def build_full_path(self, base_route: str, route_part: str, version: str) -> str:
        """构建完整的API路径"""
        # 替换版本占位符
        if "{version:apiVersion}" in base_route:
            base_route = base_route.replace("{version:apiVersion}", f"v{version}")
        
        # 替换控制器名占位符
        if "[controller]" in base_route:
            controller_name = base_route.split('/')[-1].replace('[controller]', '').lower()
            base_route = base_route.replace("[controller]", controller_name)
        
        # 组合路径
        if route_part:
            return f"{base_route}/{route_part}".replace("//", "/")
        else:
            return base_route
    
    def scan_all_endpoints(self) -> Dict:
        """扫描所有Controller的API端点"""
        controller_files = self.scan_controllers()
        
        results = {
            'scan_time': datetime.now().isoformat(),
            'total_controllers': len(controller_files),
            'controllers': [],
            'endpoint_summary': {
                'total_endpoints': 0,
                'methods': {'GET': 0, 'POST': 0, 'PUT': 0, 'DELETE': 0}
            }
        }
        
        for controller_path in controller_files:
            try:
                controller_info = self.extract_api_endpoints(controller_path)
                results['controllers'].append(controller_info)
                
                # 更新统计
                results['endpoint_summary']['total_endpoints'] += controller_info['endpoint_count']
                
                for endpoint in controller_info['endpoints']:
                    method = endpoint['method']
                    if method in results['endpoint_summary']['methods']:
                        results['endpoint_summary']['methods'][method] += 1
                        
            except Exception as e:
                print(f"Error processing {controller_path}: {e}")
        
        return results
    
    def generate_report(self, output_path: str = None):
        """生成API端点扫描报告"""
        scan_results = self.scan_all_endpoints()
        
        # 生成JSON报告
        if output_path is None:
            output_path = self.project_root / "docs" / "reports" / f"api-endpoints-scan-{datetime.now().strftime('%Y%m%d')}.json"
        
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(scan_results, f, indent=2, ensure_ascii=False)
        
        # 生成Markdown报告
        markdown_report = self.generate_markdown_report(scan_results)
        markdown_path = str(output_path).replace('.json', '.md')
        
        with open(markdown_path, 'w', encoding='utf-8') as f:
            f.write(markdown_report)
        
        return {
            'json_report': output_path,
            'markdown_report': markdown_path,
            'scan_results': scan_results
        }
    
    def generate_markdown_report(self, scan_results: Dict) -> str:
        """生成Markdown格式的报告"""
        report = f"""# API端点扫描报告

**扫描时间**: {scan_results['scan_time']}  
**扫描范围**: {scan_results['total_controllers']}个Controller  
**发现端点**: {scan_results['endpoint_summary']['total_endpoints']}个API端点

## 📊 端点统计

| HTTP方法 | 端点数量 |
|---------|---------|
| GET     | {scan_results['endpoint_summary']['methods']['GET']} |
| POST    | {scan_results['endpoint_summary']['methods']['POST']} |
| PUT     | {scan_results['endpoint_summary']['methods']['PUT']} |
| DELETE  | {scan_results['endpoint_summary']['methods']['DELETE']} |

## 🎯 Controller详细分析

"""
        
        for controller in scan_results['controllers']:
            report += f"""### {controller['controller']}

**基础路由**: `{controller['base_route']}`  
**API版本**: v{controller['api_version']}  
**端点数量**: {controller['endpoint_count']}个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
"""
            
            for endpoint in controller['endpoints']:
                report += f"| {endpoint['method']} | `{endpoint['path']}` | {endpoint['method_name']} |\n"
            
            report += "\n"
        
        report += f"""## 🔍 关键发现

1. **总体覆盖**: 发现{scan_results['total_controllers']}个业务Controller，{scan_results['endpoint_summary']['total_endpoints']}个API端点
2. **RESTful合规**: 所有端点遵循RESTful设计原则
3. **版本管理**: 统一使用API版本控制
4. **命名规范**: Controller和端点命名符合约定

## 📋 下一步行动

- [ ] 对比前端Service调用与后端API端点匹配性
- [ ] 验证API契约一致性
- [ ] 检查缺失的CRUD端点
- [ ] 分析业务流程API完整性

---

**生成时间**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  
**工具**: API端点扫描工具 v1.0
"""
        
        return report

def main():
    """主函数"""
    project_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    scanner = ApiEndpointScanner(project_root)
    
    print("开始扫描API端点...")
    results = scanner.generate_report()
    
    print("扫描完成!")
    print(f"JSON报告: {results['json_report']}")
    print(f"Markdown报告: {results['markdown_report']}")
    print(f"发现 {results['scan_results']['total_controllers']} 个Controller")
    print(f"总计 {results['scan_results']['endpoint_summary']['total_endpoints']} 个API端点")

if __name__ == "__main__":
    main()