#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
刷新按钮功能测试脚本
测试管理模块中刷新按钮的API调用是否正常
"""

import requests
import json
import time
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

def test_refresh_functionality(token):
    """测试刷新功能 - 模拟RefreshCommand的行为"""
    print("\n[REFRESH TEST] Testing refresh functionality...")
    
    # 测试用户管理刷新功能
    user_endpoints = [
        "/api/v1/Users?PageIndex=1&PageSize=10",
        "/api/v1/Users?PageIndex=1&PageSize=20", # 不同页面大小测试
        "/api/v1/Users?PageIndex=2&PageSize=5",  # 不同页码测试
    ]
    
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    
    print("\n[TEST] Testing User Management Refresh...")
    
    for i, endpoint in enumerate(user_endpoints, 1):
        print(f"\n  [REFRESH {i}] Testing: {endpoint}")
        try:
            start_time = time.time()
            response = requests.get(f"https://localhost:7001{endpoint}", headers=headers, verify=False, timeout=10)
            end_time = time.time()
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success"):
                    total_count = data.get("data", {}).get("totalCount", 0)
                    current_count = len(data.get("data", {}).get("items", []))
                    response_time = int((end_time - start_time) * 1000)
                    
                    print(f"    [OK] Response time: {response_time}ms")
                    print(f"    [DATA] Total: {total_count}, Current page: {current_count} users")
                    
                    if current_count > 0:
                        first_user = data["data"]["items"][0]
                        print(f"    [SAMPLE] First user: {first_user.get('userName', 'N/A')} - {first_user.get('realName', 'N/A')}")
                else:
                    print(f"    [FAIL] API returned error: {data.get('message', 'Unknown error')}")
            else:
                print(f"    [FAIL] HTTP {response.status_code}: {response.text[:100]}")
                
        except Exception as e:
            print(f"    [ERROR] Request failed: {e}")
        
        # 小延迟避免过快请求
        time.sleep(0.5)
    
    # 测试搜索功能 (RefreshCommand with search keyword)
    print(f"\n[TEST] Testing Search-based Refresh...")
    search_keywords = ["admin", "sys", "医生", ""]  # 空字符串表示清除搜索
    
    for keyword in search_keywords:
        endpoint = f"/api/v1/Users?PageIndex=1&PageSize=10"
        if keyword:
            endpoint += f"&Keyword={keyword}"
        
        print(f"\n  [SEARCH] Keyword: '{keyword}'")
        try:
            response = requests.get(f"https://localhost:7001{endpoint}", headers=headers, verify=False, timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success"):
                    total_count = data.get("data", {}).get("totalCount", 0)
                    current_count = len(data.get("data", {}).get("items", []))
                    print(f"    [OK] Search results: {current_count}/{total_count} users")
                else:
                    print(f"    [FAIL] Search failed: {data.get('message', 'Unknown error')}")
            else:
                print(f"    [FAIL] HTTP {response.status_code}")
                
        except Exception as e:
            print(f"    [ERROR] Search request failed: {e}")
        
        time.sleep(0.3)

def test_other_modules_refresh(token):
    """测试其他管理模块的刷新功能"""
    print("\n[REFRESH TEST] Testing other modules refresh...")
    
    modules = {
        "患者管理": "/api/v1/Patients?PageIndex=1&PageSize=10",
        "中药材管理": "/api/v1/Herbs?PageIndex=1&PageSize=10", 
        "看诊记录": "/api/v1/Consultation?PageIndex=1&PageSize=10",
        "验方模板": "/api/v1/Formulas?PageIndex=1&PageSize=10",
    }
    
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    
    for module_name, endpoint in modules.items():
        print(f"\n  [MODULE] Testing {module_name} refresh...")
        try:
            response = requests.get(f"https://localhost:7001{endpoint}", headers=headers, verify=False, timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                if data.get("success"):
                    total_count = data.get("data", {}).get("totalCount", 0)
                    current_count = len(data.get("data", {}).get("items", []))
                    print(f"    [OK] {module_name}: {current_count}/{total_count} records")
                else:
                    print(f"    [FAIL] {module_name}: {data.get('message', 'API error')}")
            else:
                print(f"    [FAIL] {module_name}: HTTP {response.status_code}")
                
        except Exception as e:
            print(f"    [ERROR] {module_name}: {e}")

def main():
    """主测试函数"""
    print("=" * 60)
    print("刷新按钮功能测试 - Refresh Button Test")
    print(f"Test started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    # 获取认证Token
    token = get_auth_token()
    if not token:
        print("[ERROR] Cannot get authentication token, test terminated")
        return False
    
    # 测试刷新功能
    test_refresh_functionality(token)
    
    # 测试其他模块
    test_other_modules_refresh(token)
    
    # 总结
    print("\n" + "=" * 60)
    print("Refresh Test Summary")
    print("=" * 60)
    print("[ANALYSIS] If the above tests all show [OK] responses,")
    print("           then the refresh button APIs are working correctly.")
    print("[ISSUE]    If refresh button still doesn't work in WPF,")
    print("           the problem is likely in:")
    print("           1. WPF Command binding")
    print("           2. ViewModel command initialization")  
    print("           3. UI thread synchronization")
    print("           4. Exception handling in ViewModel")
    
    print(f"\n[RECOMMENDATION] Check WPF application debug output")
    print(f"                 for RefreshDataAsync debug messages.")
    
    return True

if __name__ == "__main__":
    success = main()
    exit_code = 0 if success else 1
    sys.exit(exit_code)