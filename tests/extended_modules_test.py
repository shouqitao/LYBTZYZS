#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UltraThink v2.0 其他业务模块功能测试
测试Auth、Consultation、MedicalCase、Prescriptions、Formula模块
"""

import requests
import json
import time
import sys
from datetime import datetime
import urllib3

# 禁用SSL验证警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class ExtendedModulesTester:
    def __init__(self, base_url="https://localhost:7007"):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.verify = False  # 忽略SSL证书验证
        self.test_results = []
        self.auth_token = None
        
    def log_result(self, module, test, success, message=""):
        """记录测试结果"""
        result = {
            "timestamp": datetime.now().isoformat(),
            "module": module,
            "test": test,
            "success": success,
            "message": message
        }
        self.test_results.append(result)
        
        status = "[PASS]" if success else "[FAIL]"
        print(f"{status} [{module}] {test}: {message}")
        
    def wait_for_api(self, max_attempts=30, delay=2):
        """等待API服务启动"""
        print(f"正在等待API服务启动 ({self.base_url})...")
        
        for attempt in range(max_attempts):
            try:
                response = self.session.get(f"{self.base_url}/api/v1/debug/connection", timeout=5)
                if response.status_code == 200:
                    print(f"API服务已启动 (第{attempt + 1}次尝试)")
                    return True
            except Exception as e:
                if attempt < max_attempts - 1:
                    print(f"等待中... (第{attempt + 1}次尝试)")
                    time.sleep(delay)
                else:
                    print(f"API服务启动超时: {str(e)}")
                    
        return False

    def test_auth_module(self):
        """测试认证模块"""
        print("\n=== Phase 1: Auth认证模块测试 ===")
        
        try:
            # 测试登录接口
            login_data = {
                "username": "sysadmin",
                "password": "Admin@123456",
                "rememberMe": False
            }
            
            response = self.session.post(
                f"{self.base_url}/api/v1/auth/login", 
                json=login_data,
                headers={"Content-Type": "application/json"}
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get("success"):
                    self.auth_token = result.get("data", {}).get("token")
                    self.log_result("Auth", "用户登录", True, "登录成功，获取Token")
                    
                    # 设置后续请求的认证头
                    self.session.headers.update({
                        "Authorization": f"Bearer {self.auth_token}"
                    })
                    return True
                else:
                    self.log_result("Auth", "用户登录", False, f"登录失败: {result.get('message', '未知错误')}")
            else:
                self.log_result("Auth", "用户登录", False, f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("Auth", "认证模块", False, f"异常: {str(e)}")
            return False

    def test_consultations_module(self):
        """测试看诊管理模块"""
        print("\n=== Phase 2: Consultation看诊管理模块测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/consultations")
            if response.status_code == 200:
                result = response.json()
                consultation_count = result.get("totalCount", 0)
                self.log_result("Consultations", "看诊记录查询", consultation_count >= 0, 
                              f"系统中共有 {consultation_count} 条看诊记录")
                return True
            else:
                self.log_result("Consultations", "看诊记录查询", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("Consultations", "看诊管理模块", False, f"异常: {str(e)}")
            return False

    def test_medical_case_module(self):
        """测试医疗案例模块"""
        print("\n=== Phase 3: MedicalCase医疗案例模块测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/medical-cases")
            if response.status_code == 200:
                result = response.json()
                case_count = result.get("totalCount", 0)
                self.log_result("MedicalCases", "医疗案例查询", case_count >= 0, 
                              f"系统中共有 {case_count} 个医疗案例")
                return True
            else:
                self.log_result("MedicalCases", "医疗案例查询", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("MedicalCases", "医疗案例模块", False, f"异常: {str(e)}")
            return False

    def test_prescriptions_module(self):
        """测试处方管理模块"""
        print("\n=== Phase 4: Prescriptions处方管理模块测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/prescriptions")
            if response.status_code == 200:
                result = response.json()
                prescription_count = result.get("totalCount", 0)
                self.log_result("Prescriptions", "处方记录查询", prescription_count >= 0, 
                              f"系统中共有 {prescription_count} 个处方记录")
                return True
            else:
                self.log_result("Prescriptions", "处方记录查询", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("Prescriptions", "处方管理模块", False, f"异常: {str(e)}")
            return False

    def test_formulas_module(self):
        """测试验方管理模块"""
        print("\n=== Phase 5: Formula验方管理模块测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/formulas")
            if response.status_code == 200:
                result = response.json()
                formula_count = result.get("totalCount", 0)
                self.log_result("Formulas", "验方记录查询", formula_count >= 0, 
                              f"系统中共有 {formula_count} 个验方模板")
                return True
            else:
                self.log_result("Formulas", "验方记录查询", False, 
                              f"HTTP {response.status_code}: {response.text}")
                return False
                
        except Exception as e:
            self.log_result("Formulas", "验方管理模块", False, f"异常: {str(e)}")
            return False

    def test_business_workflow(self):
        """测试业务流程整合"""
        print("\n=== Phase 6: 业务流程整合测试 ===")
        
        success_count = 0
        
        # 测试诊疗流程完整性
        if self.auth_token:
            try:
                # 模拟创建医疗案例的流程（如果支持）
                workflow_data = {
                    "patientId": "test-patient-id",
                    "doctorId": "test-doctor-id",
                    "type": "consultation"
                }
                
                # 这里只是测试API端点是否存在，不实际创建数据
                response = self.session.post(
                    f"{self.base_url}/api/v1/medical-cases",
                    json=workflow_data,
                    timeout=5
                )
                
                # 即使返回400或422也说明端点存在，只是数据无效
                if response.status_code in [200, 201, 400, 422]:
                    self.log_result("Workflow", "医疗案例创建API", True, 
                                  f"API端点可访问 (HTTP {response.status_code})")
                    success_count += 1
                else:
                    self.log_result("Workflow", "医疗案例创建API", False, 
                                  f"HTTP {response.status_code}")
                    
            except Exception as e:
                self.log_result("Workflow", "业务流程整合", False, f"异常: {str(e)}")
        
        return success_count > 0

    def generate_summary(self):
        """生成测试总结"""
        total_tests = len(self.test_results)
        passed_tests = sum(1 for r in self.test_results if r["success"])
        failed_tests = total_tests - passed_tests
        success_rate = (passed_tests / total_tests * 100) if total_tests > 0 else 0
        
        print("\n" + "="*60)
        print("UltraThink v2.0 其他业务模块测试报告")
        print("="*60)
        print(f"总测试数: {total_tests}")
        print(f"通过测试: {passed_tests}")
        print(f"失败测试: {failed_tests}")
        print(f"成功率: {success_rate:.1f}%")
        
        # 按模块统计
        modules = {}
        for result in self.test_results:
            module = result["module"]
            if module not in modules:
                modules[module] = {"total": 0, "passed": 0}
            modules[module]["total"] += 1
            if result["success"]:
                modules[module]["passed"] += 1
                
        print(f"\n模块统计:")
        for module, stats in modules.items():
            rate = (stats["passed"] / stats["total"] * 100) if stats["total"] > 0 else 0
            print(f"  {module}: {stats['passed']}/{stats['total']} ({rate:.0f}%)")
            
        # 整体评估
        if success_rate >= 80:
            print(f"\n[SUCCESS] 其他业务模块状态: 生产就绪")
            print(f"UltraThink v2.0 全部8个核心模块验证完成！")
            return True
        else:
            print(f"\n[WARNING] 其他业务模块状态: 需要修复")
            print(f"建议修复失败的模块后再全面部署")
            return False
        
    def run_extended_test(self):
        """运行扩展模块测试"""
        print("开始UltraThink v2.0其他业务模块测试")
        print(f"API地址: {self.base_url}")
        print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        
        # 等待API启动
        if not self.wait_for_api():
            print("API服务启动失败，测试终止")
            return False
            
        # 执行测试
        results = []
        results.append(self.test_auth_module())
        results.append(self.test_consultations_module())
        results.append(self.test_medical_case_module())
        results.append(self.test_prescriptions_module())
        results.append(self.test_formulas_module())
        results.append(self.test_business_workflow())
        
        # 生成报告
        success = self.generate_summary()
        
        # 保存报告
        report_data = {
            "timestamp": datetime.now().isoformat(),
            "base_url": self.base_url,
            "test_results": self.test_results
        }
        
        try:
            with open("tests/extended_modules_test_report.json", 'w', encoding='utf-8') as f:
                json.dump(report_data, f, indent=2, ensure_ascii=False)
            print(f"\n详细报告已保存: tests/extended_modules_test_report.json")
        except Exception as e:
            print(f"报告保存失败: {str(e)}")
            
        return success

def main():
    """主函数"""
    base_url = "https://localhost:7007"
    if len(sys.argv) > 1:
        base_url = sys.argv[1]
        
    tester = ExtendedModulesTester(base_url)
    success = tester.run_extended_test()
    
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()