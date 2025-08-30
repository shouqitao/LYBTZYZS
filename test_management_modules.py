#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
管理模块API功能测试脚本
测试7个管理模块的API端点是否正常工作
"""

import requests
import json
import sys
from datetime import datetime
import urllib3

# 禁用SSL警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class ManagementModuleTester:
    def __init__(self):
        self.base_url = "https://localhost:7001"
        self.token = None
        
    def authenticate(self):
        """认证获取Token"""
        print("[AUTH] 正在获取认证Token...")
        
        login_url = f"{self.base_url}/api/v1/auth/login"
        login_data = {
            "username": "sysadmin",
            "password": "Admin@123456",
            "rememberMe": False
        }
        
        try:
            response = requests.post(
                login_url, 
                json=login_data, 
                verify=False,
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success") and data.get("data", {}).get("token"):
                    self.token = data["data"]["token"]
                    print("[SUCCESS] 认证成功")
                    return True
                else:
                    print(f"[ERROR] 认证失败: {data.get('message', '未知错误')}")
                    return False
            else:
                print(f"[ERROR] 认证请求失败: HTTP {response.status_code}")
                return False
                
        except Exception as e:
            print(f"[ERROR] 认证异常: {e}")
            return False
    
    def test_endpoint(self, module_name, endpoint, method="GET"):
        """测试API端点"""
        url = f"{self.base_url}{endpoint}"
        headers = {
            "Authorization": f"Bearer {self.token}",
            "Content-Type": "application/json"
        }
        
        try:
            if method == "GET":
                response = requests.get(url, headers=headers, verify=False, timeout=10)
            else:
                response = requests.request(method, url, headers=headers, verify=False, timeout=10)
            
            success = 200 <= response.status_code < 300
            status = "✅" if success else "❌"
            
            print(f"  {status} {method} {endpoint} -> HTTP {response.status_code}")
            
            if response.status_code == 200:
                try:
                    data = response.json()
                    if isinstance(data, dict) and data.get("success"):
                        if "data" in data and isinstance(data["data"], dict):
                            total_count = data["data"].get("totalCount", 0)
                            current_count = len(data["data"].get("items", []))
                            print(f"    📊 数据: 总数{total_count}, 当前页{current_count}条")
                        elif "data" in data:
                            print(f"    📄 响应: {type(data['data']).__name__}")
                    else:
                        print(f"    ⚠️  响应格式: {data.get('message', 'API响应非标准格式')}")
                except:
                    print(f"    📝 响应长度: {len(response.text)} 字符")
            
            return success
            
        except requests.exceptions.ConnectTimeout:
            print(f"  ❌ {method} {endpoint} -> 连接超时")
            return False
        except requests.exceptions.ConnectionError:
            print(f"  ❌ {method} {endpoint} -> 连接失败")
            return False
        except Exception as e:
            print(f"  ❌ {method} {endpoint} -> 异常: {e}")
            return False
    
    def test_all_management_modules(self):
        """测试所有7个管理模块"""
        print(f"\n🧪 开始测试管理模块API功能 - {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        
        # 管理模块API端点映射
        management_modules = {
            "用户管理": [
                "/api/v1/users?pageIndex=1&pageSize=10",
                "/api/v1/users/count"
            ],
            "患者管理": [
                "/api/v1/patients?pageIndex=1&pageSize=10",
                "/api/v1/patients/count"
            ],
            "医疗案例": [
                "/api/v1/medical-cases?pageIndex=1&pageSize=10",
                "/api/v1/medical-cases/count"
            ],
            "看诊记录": [
                "/api/v1/consultation?pageIndex=1&pageSize=10",
                "/api/v1/consultation/count"
            ],
            "中药材管理": [
                "/api/v1/herbs?pageIndex=1&pageSize=10",
                "/api/v1/herbs/count"
            ],
            "验方模板": [
                "/api/v1/FormulaTemplate?pageIndex=1&pageSize=10",
                "/api/v1/FormulaTemplate/count"
            ],
            "处方管理": [
                "/api/v1/prescriptions?pageIndex=1&pageSize=10",
                "/api/v1/prescriptions/count"
            ]
        }
        
        results = {}
        
        for module_name, endpoints in management_modules.items():
            print(f"\n📋 测试模块: {module_name}")
            module_results = []
            
            for endpoint in endpoints:
                success = self.test_endpoint(module_name, endpoint)
                module_results.append(success)
            
            # 模块整体状态
            all_success = all(module_results)
            success_count = sum(module_results)
            total_count = len(module_results)
            
            status = "✅ 正常" if all_success else f"⚠️ 部分失败 ({success_count}/{total_count})"
            results[module_name] = all_success
            
            print(f"  🏷️ {module_name}: {status}")
        
        return results
    
    def print_summary(self, results):
        """打印测试结果摘要"""
        print(f"\n📊 测试结果摘要 ({datetime.now().strftime('%H:%M:%S')})")
        print("=" * 50)
        
        working_modules = []
        failed_modules = []
        
        for module_name, success in results.items():
            status = "✅" if success else "❌"
            print(f"{status} {module_name}")
            
            if success:
                working_modules.append(module_name)
            else:
                failed_modules.append(module_name)
        
        print("=" * 50)
        print(f"✅ 正常工作的模块: {len(working_modules)}/7")
        print(f"❌ 有问题的模块: {len(failed_modules)}/7")
        
        if failed_modules:
            print(f"\n🔧 需要修复的模块:")
            for module in failed_modules:
                print(f"  - {module}")
        
        if len(working_modules) == 7:
            print(f"\n🎉 所有管理模块按钮功能正常！")
        elif len(working_modules) >= 5:
            print(f"\n👍 大部分管理模块按钮正常工作")
        else:
            print(f"\n⚠️ 管理模块存在较多问题，需要重点修复")
    
    def run(self):
        """运行完整测试流程"""
        if not self.authenticate():
            print("❌ 无法获取认证Token，测试终止")
            return False
        
        results = self.test_all_management_modules()
        self.print_summary(results)
        
        return len([r for r in results.values() if r]) == 7

def main():
    """主函数"""
    print("🚀 凌隐宝堂中医诊所系统 - 管理模块功能测试")
    
    tester = ManagementModuleTester()
    success = tester.run()
    
    return 0 if success else 1

if __name__ == "__main__":
    exit_code = main()
    sys.exit(exit_code)