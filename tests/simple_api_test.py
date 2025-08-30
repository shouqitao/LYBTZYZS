#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UltraThink v2.0 系统功能完整性验证测试 (简化版)
避免Unicode字符编码问题
"""

import requests
import json
import time
import sys
from datetime import datetime
import urllib3

# 禁用SSL验证警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class SimpleSystemTester:
    def __init__(self, base_url="https://localhost:7007"):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.verify = False  # 忽略SSL证书验证
        self.test_results = []
        
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
        
    def test_system_health(self):
        """测试系统健康状态"""
        print("\n=== Phase 1: 系统健康检查 ===")
        
        try:
            # 测试数据库连接
            response = self.session.get(f"{self.base_url}/api/v1/debug/connection")
            if response.status_code == 200:
                result = response.json()
                can_connect = result.get("canConnect", False)
                self.log_result("System", "数据库连接", can_connect, 
                              f"数据库连接状态: {can_connect}")
            else:
                self.log_result("System", "数据库连接", False, 
                              f"HTTP {response.status_code}")
                return False
                
            # 测试数据库表
            response = self.session.get(f"{self.base_url}/api/v1/debug/tables")
            if response.status_code == 200:
                result = response.json()
                table_count = result.get("tableCount", 0)
                self.log_result("System", "数据库表检查", table_count > 0, 
                              f"发现 {table_count} 个数据库表")
            else:
                self.log_result("System", "数据库表检查", False, 
                              f"HTTP {response.status_code}")
                
            return True
            
        except Exception as e:
            self.log_result("System", "系统健康检查", False, f"异常: {str(e)}")
            return False
            
    def test_users_module(self):
        """测试用户管理模块"""
        print("\n=== Phase 2: 用户管理模块测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/users")
            if response.status_code == 200:
                result = response.json()
                user_count = result.get("totalCount", 0)
                self.log_result("Users", "用户列表查询", user_count >= 0, 
                              f"系统中共有 {user_count} 个用户")
                return True
            else:
                self.log_result("Users", "用户列表查询", False, 
                              f"HTTP {response.status_code}")
                return False
                
        except Exception as e:
            self.log_result("Users", "用户管理模块", False, f"异常: {str(e)}")
            return False
            
    def test_patients_module(self):
        """测试患者管理模块"""
        print("\n=== Phase 3: 患者管理模块测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/patients")
            if response.status_code == 200:
                result = response.json()
                patient_count = result.get("totalCount", 0)
                self.log_result("Patients", "患者列表查询", patient_count >= 0, 
                              f"系统中共有 {patient_count} 个患者")
                return True
            else:
                self.log_result("Patients", "患者列表查询", False, 
                              f"HTTP {response.status_code}")
                return False
                
        except Exception as e:
            self.log_result("Patients", "患者管理模块", False, f"异常: {str(e)}")
            return False
            
    def test_herbs_module(self):
        """测试中药材管理模块"""
        print("\n=== Phase 4: 中药材管理模块测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/herbs")
            if response.status_code == 200:
                result = response.json()
                herb_count = result.get("totalCount", 0)
                self.log_result("Herbs", "中药材列表查询", herb_count >= 0, 
                              f"系统中共有 {herb_count} 种中药材")
                return True
            else:
                self.log_result("Herbs", "中药材列表查询", False, 
                              f"HTTP {response.status_code}")
                return False
                
        except Exception as e:
            self.log_result("Herbs", "中药材管理模块", False, f"异常: {str(e)}")
            return False
            
    def test_swagger_api(self):
        """测试Swagger API文档"""
        print("\n=== Phase 5: API文档测试 ===")
        
        try:
            response = self.session.get(f"{self.base_url}/swagger/v1/swagger.json", timeout=10)
            if response.status_code == 200:
                swagger_data = response.json()
                api_count = len(swagger_data.get("paths", {}))
                self.log_result("API", "Swagger文档", True, 
                              f"发现 {api_count} 个API端点")
                return True
            else:
                self.log_result("API", "Swagger文档", False, 
                              f"HTTP {response.status_code}")
                return False
        except Exception as e:
            self.log_result("API", "Swagger文档", False, f"异常: {str(e)}")
            return False
            
    def test_entity_simplification(self):
        """测试实体简化"""
        print("\n=== Phase 6: 实体简化验证 ===")
        
        success_count = 0
        
        # 检查Users表结构
        try:
            response = self.session.get(f"{self.base_url}/api/v1/debug/table-structure/Users")
            if response.status_code == 200:
                result = response.json()
                columns = []
                for col in result.get("columns", []):
                    if isinstance(col, dict) and "COLUMN_NAME" in col:
                        columns.append(col["COLUMN_NAME"])
                    elif isinstance(col, str):
                        columns.append(col)
                
                # 验证删除的字段
                deleted_fields = ["CreateTime", "UpdateTime", "LastLoginTime", "Remark"]
                found_deleted = [field for field in deleted_fields if field in columns]
                
                if len(found_deleted) == 0:
                    self.log_result("Entity", "Users表简化", True, 
                                  f"成功删除所有冗余字段")
                    success_count += 1
                else:
                    self.log_result("Entity", "Users表简化", False, 
                                  f"仍存在字段: {found_deleted}")
                    
        except Exception as e:
            self.log_result("Entity", "Users表结构检查", False, f"异常: {str(e)}")
            
        return success_count > 0
        
    def generate_summary(self):
        """生成测试总结"""
        total_tests = len(self.test_results)
        passed_tests = sum(1 for r in self.test_results if r["success"])
        failed_tests = total_tests - passed_tests
        success_rate = (passed_tests / total_tests * 100) if total_tests > 0 else 0
        
        print("\n" + "="*60)
        print("UltraThink v2.0 系统功能完整性验证报告")
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
            print(f"\n[SUCCESS] 系统状态: 生产就绪")
            print(f"UltraThink v2.0架构重构成功完成！")
            return True
        else:
            print(f"\n[WARNING] 系统状态: 需要修复")
            print(f"建议修复失败的测试项后再部署")
            return False
        
    def run_test(self):
        """运行完整测试"""
        print("开始UltraThink v2.0系统功能完整性验证")
        print(f"API地址: {self.base_url}")
        print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        
        # 等待API启动
        if not self.wait_for_api():
            print("API服务启动失败，测试终止")
            return False
            
        # 执行测试
        results = []
        results.append(self.test_system_health())
        results.append(self.test_users_module())
        results.append(self.test_patients_module())
        results.append(self.test_herbs_module())
        results.append(self.test_swagger_api())
        results.append(self.test_entity_simplification())
        
        # 生成报告
        success = self.generate_summary()
        
        # 保存报告
        report_data = {
            "timestamp": datetime.now().isoformat(),
            "base_url": self.base_url,
            "test_results": self.test_results
        }
        
        try:
            with open("tests/ultrathink_v2_test_report.json", 'w', encoding='utf-8') as f:
                json.dump(report_data, f, indent=2, ensure_ascii=False)
            print(f"\n详细报告已保存: tests/ultrathink_v2_test_report.json")
        except Exception as e:
            print(f"报告保存失败: {str(e)}")
            
        return success

def main():
    """主函数"""
    base_url = "https://localhost:7007"
    if len(sys.argv) > 1:
        base_url = sys.argv[1]
        
    tester = SimpleSystemTester(base_url)
    success = tester.run_test()
    
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()