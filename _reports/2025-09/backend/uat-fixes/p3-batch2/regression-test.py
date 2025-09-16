#!/usr/bin/env python3
"""
P3-Batch2: UAT Regression Test Script
验证事务修复后的关键功能
"""

import requests
import json
import time
from datetime import datetime

# Configuration
BASE_URL = "http://localhost:8080/api/v1"
TEST_USERNAME = "sysadmin"
TEST_PASSWORD = "LybtAdmin2025@SecurePass!"

class RegressionTester:
    def __init__(self):
        self.token = None
        self.test_results = []

    def log_test(self, test_name, status, details=""):
        result = {
            "test": test_name,
            "status": status,
            "timestamp": datetime.now().isoformat(),
            "details": details
        }
        self.test_results.append(result)
        status_icon = "[PASS]" if status == "PASS" else "[FAIL]" if status == "FAIL" else "[WARN]"
        print(f"{status_icon} {test_name}: {status}")
        if details:
            print(f"   Details: {details}")

    def authenticate(self):
        """Step 1: 管理员登录验证"""
        try:
            response = requests.post(f"{BASE_URL}/auth/login", json={
                "username": TEST_USERNAME,
                "password": TEST_PASSWORD,
                "rememberMe": False
            }, timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success") and data.get("data", {}).get("token"):
                    self.token = data["data"]["token"]
                    self.log_test("Admin Login", "PASS", f"Token: {self.token[:20]}...")
                    return True
                else:
                    self.log_test("Admin Login", "FAIL", f"No token in response: {data}")
            else:
                self.log_test("Admin Login", "FAIL", f"HTTP {response.status_code}: {response.text}")
                
        except Exception as e:
            self.log_test("Admin Login", "FAIL", f"Exception: {str(e)}")
        
        return False

    def test_patient_creation(self):
        """Step 2: 患者创建核心测试 (P3-Batch1问题点)"""
        if not self.token:
            self.log_test("Patient Creation", "SKIP", "No authentication token")
            return False
            
        headers = {"Authorization": f"Bearer {self.token}"}
        patient_data = {
            "Name": "测试患者_P3Batch2",
            "Gender": 1,  # Male
            "Age": 35,
            "PhoneNumber": "13800138002",
            "Address": "北京市朝阳区测试地址"
        }
        
        try:
            response = requests.post(f"{BASE_URL}/patients", 
                                   json=patient_data, 
                                   headers=headers, 
                                   timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success"):
                    patient_id = data.get("data", {}).get("id")
                    self.log_test("Patient Creation", "PASS", f"Created patient ID: {patient_id}")
                    return patient_id
                else:
                    self.log_test("Patient Creation", "FAIL", f"Success=false: {data.get('message', 'No message')}")
            else:
                # Check if it's the old DTO binding error (should be fixed)
                if "dto field is required" in response.text:
                    self.log_test("Patient Creation", "FAIL", "❌ P3-Batch1 DTO问题依然存在!")
                elif "ExecutionStrategy" in response.text:
                    self.log_test("Patient Creation", "FAIL", "❌ P3-Batch2 事务冲突依然存在!")
                else:
                    self.log_test("Patient Creation", "FAIL", f"HTTP {response.status_code}: {response.text}")
                    
        except Exception as e:
            self.log_test("Patient Creation", "FAIL", f"Exception: {str(e)}")
            
        return None

    def test_user_creation(self):
        """Step 3: 用户创建测试"""
        if not self.token:
            self.log_test("User Creation", "SKIP", "No authentication token")
            return False
            
        headers = {"Authorization": f"Bearer {self.token}"}
        user_data = {
            "Username": f"testuser_p3_{int(time.time())}",
            "Email": f"testuser{int(time.time())}@test.com",
            "PhoneNumber": "13800138003",
            "Role": "Doctor",
            "RealName": "测试医生_P3Batch2"
        }
        
        try:
            response = requests.post(f"{BASE_URL}/users", 
                                   json=user_data, 
                                   headers=headers, 
                                   timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success"):
                    user_id = data.get("data", {}).get("id")
                    self.log_test("User Creation", "PASS", f"Created user ID: {user_id}")
                    return user_id
                else:
                    self.log_test("User Creation", "FAIL", f"Success=false: {data.get('message', 'No message')}")
            else:
                if "ExecutionStrategy" in response.text:
                    self.log_test("User Creation", "FAIL", "❌ P3-Batch2 事务冲突依然存在!")
                else:
                    self.log_test("User Creation", "FAIL", f"HTTP {response.status_code}: {response.text}")
                    
        except Exception as e:
            self.log_test("User Creation", "FAIL", f"Exception: {str(e)}")
            
        return None

    def test_health_check(self):
        """Step 4: 系统健康检查"""
        try:
            response = requests.get(f"{BASE_URL}/health", timeout=5)
            
            if response.status_code == 200:
                data = response.json()
                if data.get("status") == "Healthy":
                    self.log_test("Health Check", "PASS", "System is healthy")
                else:
                    self.log_test("Health Check", "WARN", f"Status: {data.get('status', 'Unknown')}")
            else:
                self.log_test("Health Check", "FAIL", f"HTTP {response.status_code}: {response.text}")
                
        except Exception as e:
            self.log_test("Health Check", "FAIL", f"Exception: {str(e)}")

    def generate_report(self):
        """生成测试报告"""
        total_tests = len(self.test_results)
        passed_tests = len([r for r in self.test_results if r["status"] == "PASS"])
        failed_tests = len([r for r in self.test_results if r["status"] == "FAIL"])
        warned_tests = len([r for r in self.test_results if r["status"] == "WARN"])
        
        print(f"\n# P3-Batch2 UAT Regression Test Report")
        print(f"{'='*50}")
        print(f"总测试数: {total_tests}")
        print(f"通过: {passed_tests} [PASS]")
        print(f"失败: {failed_tests} [FAIL]")  
        print(f"警告: {warned_tests} [WARN]")
        print(f"成功率: {(passed_tests/total_tests*100):.1f}%" if total_tests > 0 else "0.0%")
        
        if failed_tests == 0:
            print(f"\n** P3-Batch2 修复验证成功! **")
            print(f"[PASS] 所有关键功能正常工作")
            print(f"[PASS] 事务冲突问题已解决")
            print(f"[PASS] 系统可以正常部署")
        else:
            print(f"\n[WARN] **发现问题，需要进一步调查**")
            for result in self.test_results:
                if result["status"] == "FAIL":
                    print(f"[FAIL] {result['test']}: {result['details']}")

        return failed_tests == 0

def main():
    print(">> 启动 P3-Batch2 UAT 回归验证...")
    print("目标: 验证事务修复后患者创建和用户创建功能")
    print()
    
    tester = RegressionTester()
    
    # Step 1: 身份验证
    if not tester.authenticate():
        print("❌ 认证失败，无法继续测试")
        return False
    
    # Step 2: 关键功能测试
    tester.test_patient_creation()  # P3-Batch1 原始问题
    tester.test_user_creation()     # P3-Batch2 修复功能
    tester.test_health_check()      # 系统状态检查
    
    # Step 3: 生成报告
    success = tester.generate_report()
    
    return success

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)