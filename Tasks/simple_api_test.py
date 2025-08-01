#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LYBT中医诊所管理系统 - 简化API测试脚本
"""

import requests
import json
from datetime import datetime

class SimpleAPITester:
    def __init__(self):
        self.base_url = "http://192.168.190.243:5000"
        self.token = None
        self.results = []
        
    def login(self):
        """登录获取token"""
        data = {
            "username": "sysadmin",
            "password": "Admin@123456",
            "rememberMe": False
        }
        
        try:
            response = requests.post(f"{self.base_url}/api/v1/Auth/login", json=data, timeout=10)
            if response.status_code == 200:
                result = response.json()
                if result.get("success"):
                    self.token = result["data"]["token"]
                    print("✅ 登录成功")
                    return True
            print(f"❌ 登录失败: {response.status_code}")
            return False
        except Exception as e:
            print(f"❌ 登录异常: {e}")
            return False
    
    def test_endpoint(self, module, path, method, description=""):
        """测试单个端点"""
        url = f"{self.base_url}{path}"
        headers = {"Content-Type": "application/json"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        
        try:
            if method == "GET":
                response = requests.get(url, headers=headers, timeout=10)
            elif method == "POST":
                # 根据路径提供合适的测试数据
                test_data = self.get_test_data(path)
                response = requests.post(url, json=test_data, headers=headers, timeout=10)
            else:
                response = requests.request(method, url, headers=headers, timeout=10)
            
            success = 200 <= response.status_code < 300
            result = {
                "module": module,
                "path": path,
                "method": method,
                "status_code": response.status_code,
                "success": success,
                "description": description
            }
            self.results.append(result)
            
            status = "✅" if success else "❌"
            print(f"{status} {method} {path} - {response.status_code}")
            
            return success
            
        except Exception as e:
            result = {
                "module": module,
                "path": path,
                "method": method,
                "status_code": 0,
                "success": False,
                "description": description,
                "error": str(e)
            }
            self.results.append(result)
            print(f"❌ {method} {path} - 异常: {e}")
            return False
    
    def get_test_data(self, path):
        """根据路径返回测试数据"""
        if "paged" in path:
            return {"currentPage": 1, "pageSize": 10}
        elif "login" in path:
            return {"username": "test", "password": "test123"}
        elif "Users" in path:
            return {"userName": "testuser", "realName": "测试用户", "role": 1}
        elif "Patients" in path:
            return {"name": "测试患者", "phone": "13800138000", "gender": 1}
        elif "Doctors" in path:
            return {"realName": "测试医生", "department": "内科", "title": "主治医师"}
        else:
            return {}
    
    def run_tests(self):
        """运行所有测试"""
        print("🚀 开始API测试...")
        
        if not self.login():
            return
        
        # 定义测试用例
        test_cases = [
            # 健康检查
            ("健康检查", "/api/Health", "GET", "基本健康检查"),
            ("健康检查", "/api/Health/database", "GET", "数据库健康检查"),
            
            # 认证模块
            ("认证", "/api/v1/Auth/login", "POST", "用户登录"),
            ("认证", "/api/v1/Auth/logout", "POST", "用户登出"),
            ("认证", "/api/v1/Auth/RefreshToken", "POST", "刷新token"),
            
            # 用户模块
            ("用户", "/api/v1/Users", "GET", "获取用户列表"),
            ("用户", "/api/v1/Users/paged", "POST", "分页查询用户"),
            ("用户", "/api/v1/Users/getRoles", "GET", "获取角色列表"),
            ("用户", "/api/v1/Users/active", "GET", "获取启用用户"),
            
            # 患者模块
            ("患者", "/api/v1/Patients", "GET", "获取患者列表"),
            ("患者", "/api/v1/Patients/paged", "POST", "分页查询患者"),
            ("患者", "/api/v1/Patients/active", "GET", "获取启用患者"),
            
            # 医生模块
            ("医生", "/api/v1/Doctors", "GET", "获取医生列表"),
            ("医生", "/api/v1/Doctors/paged", "POST", "分页查询医生"),
            ("医生", "/api/v1/Doctors/active", "GET", "获取启用医生"),
            
            # 药材模块
            ("药材", "/api/v1/Herbs", "GET", "获取药材列表"),
            ("药材", "/api/v1/Herbs/paged", "POST", "分页查询药材"),
            ("药材", "/api/v1/Herbs/active", "GET", "获取启用药材"),
            
            # 挂号模块
            ("挂号", "/api/v1/Registration", "GET", "获取挂号列表"),
            ("挂号", "/api/v1/Registration/paged", "POST", "分页查询挂号"),
            
            # 处方模块
            ("处方", "/api/v1/Prescriptions", "GET", "获取处方列表"),
            ("处方", "/api/v1/Prescriptions/paged", "POST", "分页查询处方"),
            
            # 诊断治疗模块
            ("诊断治疗", "/api/v1/DiagnosisTreatment", "GET", "获取诊断治疗列表"),
            
            # 药房模块
            ("药房", "/api/v1/Pharmacy", "GET", "获取药房列表"),
            
            # 费用结算模块
            ("费用结算", "/api/v1/Billing", "GET", "获取费用结算列表"),
            
            # 排队模块
            ("排队", "/api/v1/Queueing", "GET", "获取排队列表"),
            
            # 病历模块
            ("病历", "/api/v1/Records", "GET", "获取病历列表"),
            
            # 方剂模板模块
            ("方剂模板", "/api/v1/FormulaTemplates", "GET", "获取方剂模板列表"),
            
            # 治疗室模块
            ("治疗室", "/api/v1/TreatmentRoom", "GET", "获取治疗室列表"),
            
            # 统一配置模块
            ("统一配置", "/api/v1/UnifiedConfig", "GET", "获取统一配置"),
            
            # 统一日志模块
            ("统一日志", "/api/v1/UnifiedLogs", "GET", "获取统一日志"),
            
            # 数据同步模块
            ("数据同步", "/api/v1/Sync", "GET", "获取数据同步状态"),
        ]
        
        print(f"📋 共 {len(test_cases)} 个接口需要测试\n")
        
        for module, path, method, desc in test_cases:
            self.test_endpoint(module, path, method, desc)
        
        self.generate_reports()
    
    def generate_reports(self):
        """生成报告"""
        print("\n📝 生成测试报告...")
        
        total = len(self.results)
        success_count = len([r for r in self.results if r["success"]])
        fail_count = total - success_count
        success_rate = (success_count / total * 100) if total > 0 else 0
        
        # 生成测试报告
        report = f"""# LYBT中医诊所管理系统 - API测试报告

## 测试概览
- **测试时间**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}
- **服务器地址**: {self.base_url}
- **总接口数**: {total}
- **成功数量**: {success_count}
- **失败数量**: {fail_count}
- **成功率**: {success_rate:.1f}%

## 详细测试结果

| 模块 | 接口路径 | 方法 | 状态码 | 结果 | 描述 |
|------|----------|------|---------|------|------|
"""
        
        for result in self.results:
            status = "✅ 成功" if result["success"] else "❌ 失败"
            report += f"| {result['module']} | {result['path']} | {result['method']} | {result.get('status_code', 'N/A')} | {status} | {result['description']} |\n"
        
        # 按模块统计
        modules = {}
        for result in self.results:
            module = result["module"]
            if module not in modules:
                modules[module] = {"total": 0, "success": 0}
            modules[module]["total"] += 1
            if result["success"]:
                modules[module]["success"] += 1
        
        report += "\n## 按模块统计\n"
        for module, stats in modules.items():
            rate = (stats["success"] / stats["total"] * 100) if stats["total"] > 0 else 0
            report += f"- **{module}**: {stats['success']}/{stats['total']} ({rate:.1f}% 成功)\n"
        
        report += f"\n---\n*报告生成时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*\n"
        
        # 写入文件
        with open("Tasks/API测试报告.md", "w", encoding="utf-8") as f:
            f.write(report)
        
        # 生成修复工作列表
        failed_results = [r for r in self.results if not r["success"]]
        if failed_results:
            fix_list = f"""# LYBT中医诊所管理系统 - 修复工作列表

## 失败接口修复清单
*生成时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*

共发现 **{len(failed_results)}** 个需要修复的接口问题：

"""
            
            for i, failed in enumerate(failed_results, 1):
                status_code = failed.get('status_code', 0)
                error_msg = failed.get('error', '未知错误')
                
                # 修复建议
                if status_code == 401:
                    suggestion = "检查认证token是否有效或是否需要登录"
                elif status_code == 404:
                    suggestion = "检查API路径是否正确，确认接口是否存在"
                elif status_code == 500:
                    suggestion = "服务器内部错误，检查后端日志和数据库连接"
                else:
                    suggestion = "检查服务器状态和API文档"
                
                priority = "高" if status_code == 500 else "中" if status_code in [401, 403] else "低"
                
                fix_list += f"""### {i}. {failed['module']} - {failed['description']}

**接口信息:**
- 路径: `{failed['method']} {failed['path']}`
- 状态码: `{status_code}`
- 错误信息: `{error_msg[:100]}`

**修复建议:** {suggestion}

**优先级:** {priority}

---

"""
            
            fix_list += """## 修复进度跟踪

- [ ] 批量修复认证相关问题 (401/403错误)
- [ ] 修复服务器内部错误 (500错误)
- [ ] 修复路径不存在问题 (404错误)
- [ ] 修复参数验证问题 (400/422错误)
- [ ] 重新测试所有修复接口

"""
            
            with open("Tasks/修复工作列表.md", "w", encoding="utf-8") as f:
                f.write(fix_list)
        
        print("✅ 报告生成完成")
        print(f"\n📊 测试汇总:")
        print(f"   总接口: {total}")
        print(f"   成功: {success_count}")
        print(f"   失败: {fail_count}")
        print(f"   成功率: {success_rate:.1f}%")

if __name__ == "__main__":
    tester = SimpleAPITester()
    tester.run_tests()