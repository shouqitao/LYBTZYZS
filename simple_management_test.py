#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
简化版管理模块API测试脚本
测试7个管理模块的API端点是否正常工作
"""

import requests
import json
import sys
from datetime import datetime
import urllib3

# 禁用SSL警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

def get_auth_token():
    """获取认证Token"""
    print("[AUTH] Getting authentication token...")
    
    login_url = "https://localhost:7001/api/v1/auth/login"
    login_data = {
        "username": "sysadmin",
        "password": "Admin@123456",
        "rememberMe": False
    }
    
    try:
        response = requests.post(login_url, json=login_data, verify=False, timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success") and data.get("data", {}).get("token"):
                token = data["data"]["token"]
                print("[SUCCESS] Authentication successful")
                return token
            else:
                print(f"[ERROR] Authentication failed: {data.get('message', 'Unknown error')}")
                return None
        else:
            print(f"[ERROR] Authentication request failed: HTTP {response.status_code}")
            return None
            
    except Exception as e:
        print(f"[ERROR] Authentication exception: {e}")
        return None

def test_api_endpoint(token, module_name, endpoint):
    """测试单个API端点"""
    url = f"https://localhost:7001{endpoint}"
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    
    try:
        response = requests.get(url, headers=headers, verify=False, timeout=10)
        success = 200 <= response.status_code < 300
        
        status = "[OK]" if success else "[FAIL]"
        print(f"  {status} GET {endpoint} -> HTTP {response.status_code}")
        
        if response.status_code == 200:
            try:
                data = response.json()
                if isinstance(data, dict) and data.get("success"):
                    if "data" in data and isinstance(data["data"], dict):
                        total_count = data["data"].get("totalCount", 0)
                        current_count = len(data["data"].get("items", []))
                        print(f"    [DATA] Total: {total_count}, Current: {current_count} records")
                    elif "data" in data:
                        print(f"    [DATA] Response type: {type(data['data']).__name__}")
                else:
                    print(f"    [WARN] Response: {data.get('message', 'Non-standard API response')}")
            except:
                print(f"    [INFO] Response length: {len(response.text)} characters")
        
        return success
        
    except requests.exceptions.ConnectTimeout:
        print(f"  [FAIL] GET {endpoint} -> Connection timeout")
        return False
    except requests.exceptions.ConnectionError:
        print(f"  [FAIL] GET {endpoint} -> Connection failed")
        return False
    except Exception as e:
        print(f"  [FAIL] GET {endpoint} -> Exception: {e}")
        return False

def main():
    """主测试函数"""
    print("=" * 60)
    print("凌隐宝堂中医诊所系统 - 管理模块功能测试")
    print(f"Test started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    # 获取认证Token
    token = get_auth_token()
    if not token:
        print("[ERROR] Cannot get authentication token, test terminated")
        return False
    
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
    
    print(f"\n[TEST] Testing {len(management_modules)} management modules...")
    
    results = {}
    working_modules = []
    failed_modules = []
    
    for module_name, endpoints in management_modules.items():
        print(f"\n[MODULE] Testing: {module_name}")
        module_results = []
        
        for endpoint in endpoints:
            success = test_api_endpoint(token, module_name, endpoint)
            module_results.append(success)
        
        # 模块整体状态
        all_success = all(module_results)
        success_count = sum(module_results)
        total_count = len(module_results)
        
        if all_success:
            status = "[OK] Normal"
            working_modules.append(module_name)
        else:
            status = f"[PARTIAL] Failed ({success_count}/{total_count})"
            failed_modules.append(module_name)
        
        results[module_name] = all_success
        print(f"  [RESULT] {module_name}: {status}")
    
    # 打印测试结果摘要
    print("\n" + "=" * 60)
    print(f"Test Summary ({datetime.now().strftime('%H:%M:%S')})")
    print("=" * 60)
    
    for module_name, success in results.items():
        status = "[OK]" if success else "[FAIL]"
        print(f"{status} {module_name}")
    
    print("=" * 60)
    print(f"[SUMMARY] Working modules: {len(working_modules)}/7")
    print(f"[SUMMARY] Failed modules: {len(failed_modules)}/7")
    
    if failed_modules:
        print(f"\n[REPAIR] Modules needing repair:")
        for module in failed_modules:
            print(f"  - {module}")
    
    if len(working_modules) == 7:
        print(f"\n[SUCCESS] All management module buttons are working properly!")
    elif len(working_modules) >= 5:
        print(f"\n[GOOD] Most management module buttons are working normally")
    else:
        print(f"\n[WARNING] Multiple management modules have issues, need focused repair")
    
    return len(working_modules) == 7

if __name__ == "__main__":
    success = main()
    exit_code = 0 if success else 1
    sys.exit(exit_code)