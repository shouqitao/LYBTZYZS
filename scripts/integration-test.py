#!/usr/bin/env python3
"""
系统集成测试脚本
用于验证控制器优化和前端服务更新后的系统集成情况
"""

import os
import sys
import time
import json
import subprocess
import requests
from datetime import datetime
from typing import Dict, List, Tuple, Optional
import urllib3

# 禁用SSL警告（开发环境）
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# 配置
API_BASE_URL = "https://localhost:7001/api"
ADMIN_USERNAME = "sysadmin"
ADMIN_PASSWORD = "Admin@123456"
TEST_TIMEOUT = 30

# 测试结果收集
test_results = []

class TestResult:
    def __init__(self, test_id: str, test_name: str):
        self.test_id = test_id
        self.test_name = test_name
        self.status = "pending"
        self.message = ""
        self.start_time = None
        self.end_time = None
        
    def start(self):
        self.start_time = datetime.now()
        print(f"\n[{self.test_id}] 开始测试: {self.test_name}")
        
    def pass_test(self, message: str = "测试通过"):
        self.status = "passed"
        self.message = message
        self.end_time = datetime.now()
        print(f"  ✓ {message}")
        
    def fail_test(self, message: str):
        self.status = "failed"
        self.message = message
        self.end_time = datetime.now()
        print(f"  ✗ {message}")
        
    def skip_test(self, message: str):
        self.status = "skipped"
        self.message = message
        self.end_time = datetime.now()
        print(f"  - {message}")

class IntegrationTester:
    def __init__(self):
        self.session = requests.Session()
        self.session.verify = False  # 开发环境禁用SSL验证
        self.token = None
        self.headers = {"Content-Type": "application/json"}
        
    def check_environment(self) -> bool:
        """检查测试环境"""
        test = TestResult("ENV-001", "环境检查")
        test.start()
        test_results.append(test)
        
        try:
            # 检查API服务是否运行
            response = self.session.get(f"{API_BASE_URL}/health", timeout=5)
            if response.status_code == 404:
                # 如果没有health端点，尝试访问swagger
                response = self.session.get("https://localhost:7001/swagger/index.html", timeout=5)
                if response.status_code == 200:
                    test.pass_test("API服务正在运行")
                    return True
            elif response.status_code == 200:
                test.pass_test("API服务正在运行")
                return True
                
            test.fail_test(f"API服务响应异常: {response.status_code}")
            return False
        except requests.exceptions.ConnectionError:
            test.fail_test("无法连接到API服务，请确保后端服务已启动")
            return False
        except Exception as e:
            test.fail_test(f"环境检查失败: {str(e)}")
            return False
            
    def test_login(self) -> bool:
        """测试用户登录 (TC-001)"""
        test = TestResult("TC-001", "用户登录测试")
        test.start()
        test_results.append(test)
        
        try:
            login_data = {
                "username": ADMIN_USERNAME,
                "password": ADMIN_PASSWORD
            }
            
            response = self.session.post(
                f"{API_BASE_URL}/auth/login",
                json=login_data,
                headers=self.headers
            )
            
            if response.status_code == 200:
                data = response.json()
                if "token" in data:
                    self.token = data["token"]
                    self.headers["Authorization"] = f"Bearer {self.token}"
                    test.pass_test("管理员登录成功，获取到JWT Token")
                    return True
                else:
                    test.fail_test("登录响应中没有token字段")
                    return False
            else:
                test.fail_test(f"登录失败: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            test.fail_test(f"登录测试异常: {str(e)}")
            return False
            
    def test_user_list(self) -> bool:
        """测试用户列表查询 (TC-002)"""
        test = TestResult("TC-002", "用户列表查询测试")
        test.start()
        test_results.append(test)
        
        if not self.token:
            test.skip_test("跳过测试：未登录")
            return False
            
        try:
            response = self.session.get(
                f"{API_BASE_URL}/users",
                headers=self.headers,
                params={"page": 1, "pageSize": 10}
            )
            
            if response.status_code == 200:
                data = response.json()
                if "items" in data:
                    test.pass_test(f"成功获取用户列表，共{len(data['items'])}条记录")
                    return True
                else:
                    test.fail_test("响应格式错误：缺少items字段")
                    return False
            else:
                test.fail_test(f"查询失败: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            test.fail_test(f"用户列表查询异常: {str(e)}")
            return False
            
    def test_patient_crud(self) -> bool:
        """测试患者管理功能 (TC-005, TC-006)"""
        test = TestResult("TC-005/006", "患者管理功能测试")
        test.start()
        test_results.append(test)
        
        if not self.token:
            test.skip_test("跳过测试：未登录")
            return False
            
        try:
            # 创建患者
            patient_data = {
                "name": f"测试患者_{int(time.time())}",
                "gender": "男",
                "age": 35,
                "phoneNumber": "13900139001"
            }
            
            response = self.session.post(
                f"{API_BASE_URL}/patients",
                json=patient_data,
                headers=self.headers
            )
            
            if response.status_code in [200, 201]:
                created_patient = response.json()
                patient_id = created_patient.get("id")
                
                if patient_id:
                    # 查询患者列表验证
                    list_response = self.session.get(
                        f"{API_BASE_URL}/patients",
                        headers=self.headers,
                        params={"keyword": patient_data["name"]}
                    )
                    
                    if list_response.status_code == 200:
                        test.pass_test(f"患者创建和查询成功，ID: {patient_id}")
                        return True
                    else:
                        test.fail_test("患者创建成功但查询失败")
                        return False
                else:
                    test.fail_test("创建响应中没有患者ID")
                    return False
            else:
                test.fail_test(f"创建患者失败: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            test.fail_test(f"患者管理测试异常: {str(e)}")
            return False
            
    def test_herb_management(self) -> bool:
        """测试药材管理功能 (TC-008, TC-009)"""
        test = TestResult("TC-008/009", "药材管理功能测试")
        test.start()
        test_results.append(test)
        
        if not self.token:
            test.skip_test("跳过测试：未登录")
            return False
            
        try:
            # 查询药材列表
            response = self.session.get(
                f"{API_BASE_URL}/herbs",
                headers=self.headers,
                params={"page": 1, "pageSize": 10}
            )
            
            if response.status_code == 200:
                data = response.json()
                
                # 创建新药材
                herb_data = {
                    "name": f"测试药材_{int(time.time())}",
                    "origin": "测试产地",
                    "specification": "10g/包",
                    "unitPrice": 50.00,
                    "stockQuantity": 100
                }
                
                create_response = self.session.post(
                    f"{API_BASE_URL}/herbs",
                    json=herb_data,
                    headers=self.headers
                )
                
                if create_response.status_code in [200, 201]:
                    test.pass_test("药材查询和创建功能正常")
                    return True
                else:
                    test.fail_test(f"创建药材失败: {create_response.status_code}")
                    return False
            else:
                test.fail_test(f"查询药材列表失败: {response.status_code}")
                return False
                
        except Exception as e:
            test.fail_test(f"药材管理测试异常: {str(e)}")
            return False
            
    def test_prescription_service(self) -> bool:
        """测试处方服务层 (TC-011)"""
        test = TestResult("TC-011", "处方服务层测试")
        test.start()
        test_results.append(test)
        
        if not self.token:
            test.skip_test("跳过测试：未登录")
            return False
            
        try:
            # 查询处方列表
            response = self.session.get(
                f"{API_BASE_URL}/prescriptions",
                headers=self.headers,
                params={"page": 1, "pageSize": 10}
            )
            
            if response.status_code == 200:
                data = response.json()
                test.pass_test("处方列表查询成功，服务层正常工作")
                return True
            else:
                test.fail_test(f"处方查询失败: {response.status_code}")
                return False
                
        except Exception as e:
            test.fail_test(f"处方服务测试异常: {str(e)}")
            return False
            
    def generate_report(self):
        """生成测试报告"""
        print("\n" + "=" * 60)
        print("系统集成测试报告")
        print("=" * 60)
        print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"API地址: {API_BASE_URL}")
        print("\n测试结果汇总:")
        print("-" * 60)
        
        passed = sum(1 for t in test_results if t.status == "passed")
        failed = sum(1 for t in test_results if t.status == "failed")
        skipped = sum(1 for t in test_results if t.status == "skipped")
        total = len(test_results)
        
        print(f"总测试数: {total}")
        print(f"通过: {passed} ({passed/total*100:.1f}%)")
        print(f"失败: {failed}")
        print(f"跳过: {skipped}")
        
        if failed > 0:
            print("\n失败的测试:")
            for test in test_results:
                if test.status == "failed":
                    print(f"  - [{test.test_id}] {test.test_name}: {test.message}")
                    
        # 保存详细报告
        report_file = f"integration_test_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        report_data = {
            "test_time": datetime.now().isoformat(),
            "api_url": API_BASE_URL,
            "summary": {
                "total": total,
                "passed": passed,
                "failed": failed,
                "skipped": skipped,
                "pass_rate": f"{passed/total*100:.1f}%"
            },
            "details": [
                {
                    "test_id": t.test_id,
                    "test_name": t.test_name,
                    "status": t.status,
                    "message": t.message,
                    "duration": str(t.end_time - t.start_time) if t.start_time and t.end_time else "N/A"
                }
                for t in test_results
            ]
        }
        
        with open(report_file, 'w', encoding='utf-8') as f:
            json.dump(report_data, f, ensure_ascii=False, indent=2)
            
        print(f"\n详细报告已保存到: {report_file}")
        
        return failed == 0

def main():
    print("凌隐宝堂中医诊所系统 - 集成测试")
    print("=" * 60)
    
    tester = IntegrationTester()
    
    # 运行测试
    if not tester.check_environment():
        print("\n环境检查失败，请确保：")
        print("1. SQL Server服务已启动")
        print("2. 后端API服务已启动 (https://localhost:7001)")
        print("3. 数据库已初始化")
        return 1
        
    # 核心功能测试
    tester.test_login()
    tester.test_user_list()
    tester.test_patient_crud()
    tester.test_herb_management()
    tester.test_prescription_service()
    
    # 生成报告
    success = tester.generate_report()
    
    return 0 if success else 1

if __name__ == "__main__":
    sys.exit(main())