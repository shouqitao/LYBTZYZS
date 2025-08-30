#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
UI线程刷新问题测试脚本
测试WPF应用中刷新按钮的UI更新机制
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

def test_user_data_changes(token):
    """测试用户数据变化"""
    print("\n[DATA TEST] Testing user data changes...")
    
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    
    # 1. 首先获取当前用户列表
    print("\n[STEP 1] Getting current user list...")
    response = requests.get("https://localhost:7001/api/v1/Users?PageIndex=1&PageSize=10", 
                          headers=headers, verify=False, timeout=10)
    
    if response.status_code == 200:
        data = response.json()
        if data.get("success"):
            original_count = data.get("data", {}).get("totalCount", 0)
            original_items = data.get("data", {}).get("items", [])
            print(f"    [INITIAL] Total users: {original_count}")
            
            if original_items:
                first_user = original_items[0]
                print(f"    [INITIAL] First user: {first_user.get('userName', 'N/A')} - Status: {first_user.get('status', 'N/A')}")
                user_id = first_user.get("id")
                original_status = first_user.get("status")
                
                # 2. 修改用户状态 (禁用/启用切换)
                print(f"\n[STEP 2] Toggling user status...")
                if original_status == 1:  # 启用 -> 禁用
                    toggle_url = f"https://localhost:7001/api/v1/Users/{user_id}/disable"
                    expected_new_status = 0
                    action = "disable"
                else:  # 禁用 -> 启用
                    toggle_url = f"https://localhost:7001/api/v1/Users/{user_id}/enable"  
                    expected_new_status = 1
                    action = "enable"
                
                toggle_response = requests.put(toggle_url, headers=headers, verify=False, timeout=10)
                
                if toggle_response.status_code == 200:
                    print(f"    [SUCCESS] User status {action} request successful")
                    
                    # 3. 短暂等待数据库更新
                    time.sleep(1)
                    
                    # 4. 再次查询用户列表验证变化
                    print(f"\n[STEP 3] Verifying data changes...")
                    verify_response = requests.get("https://localhost:7001/api/v1/Users?PageIndex=1&PageSize=10", 
                                                 headers=headers, verify=False, timeout=10)
                    
                    if verify_response.status_code == 200:
                        verify_data = verify_response.json()
                        if verify_data.get("success"):
                            verify_items = verify_data.get("data", {}).get("items", [])
                            if verify_items:
                                verify_user = next((u for u in verify_items if u.get("id") == user_id), None)
                                if verify_user:
                                    new_status = verify_user.get("status")
                                    print(f"    [VERIFY] User status change: {original_status} -> {new_status}")
                                    
                                    if new_status == expected_new_status:
                                        print(f"    [SUCCESS] ✅ Backend data changed correctly")
                                        return {
                                            "data_changed": True,
                                            "user_id": user_id,
                                            "original_status": original_status,
                                            "new_status": new_status,
                                            "action": action
                                        }
                                    else:
                                        print(f"    [FAIL] ❌ Status change failed - expected {expected_new_status}, got {new_status}")
                                        return {"data_changed": False, "reason": "Status change failed"}
                                else:
                                    print(f"    [ERROR] User with ID {user_id} not found in verification")
                                    return {"data_changed": False, "reason": "User not found in verification"}
                            else:
                                print("    [ERROR] No users returned in verification query")
                                return {"data_changed": False, "reason": "No users in verification"}
                        else:
                            print(f"    [ERROR] Verification query failed: {verify_data.get('message', 'Unknown error')}")
                            return {"data_changed": False, "reason": "Verification query failed"}
                    else:
                        print(f"    [ERROR] Verification request failed: HTTP {verify_response.status_code}")
                        return {"data_changed": False, "reason": "Verification request failed"}
                else:
                    print(f"    [FAIL] Status toggle failed: HTTP {toggle_response.status_code}")
                    print(f"    Response: {toggle_response.text[:200]}")
                    return {"data_changed": False, "reason": "Status toggle failed"}
            else:
                print("    [ERROR] No users found in initial query")
                return {"data_changed": False, "reason": "No users found"}
        else:
            print(f"    [ERROR] Initial query failed: {data.get('message', 'Unknown error')}")
            return {"data_changed": False, "reason": "Initial query failed"}
    else:
        print(f"    [ERROR] Initial request failed: HTTP {response.status_code}")
        return {"data_changed": False, "reason": "Initial request failed"}

def analyze_ui_refresh_issue(change_result):
    """分析UI刷新问题"""
    print("\n" + "="*60)
    print("UI REFRESH ISSUE ANALYSIS")
    print("="*60)
    
    if change_result.get("data_changed"):
        print("✅ BACKEND STATUS: Data changes are working correctly")
        print(f"   User {change_result['user_id']} status: {change_result['original_status']} → {change_result['new_status']}")
        print(f"   Action performed: {change_result['action']}")
        print()
        print("❌ FRONTEND ISSUE: If refresh button shows old data, the problem is:")
        print()
        print("1. 🔄 UI Thread Synchronization:")
        print("   - ObservableCollection updates must happen on UI thread")
        print("   - WPF DataBinding requires UI thread for PropertyChanged events")
        print("   - Solution: Use Dispatcher.Invoke for UI updates")
        print()
        print("2. 📊 Data Binding Problems:")
        print("   - XAML ItemsSource='{Binding Items}' not triggering updates")
        print("   - ObservableCollection PropertyChanged events not firing")
        print("   - Solution: Ensure proper INotifyPropertyChanged implementation")
        print()
        print("3. 🔧 LoadItemsAsync Implementation:")
        print("   - Line 221: Items = new ObservableCollection<TItem>(result.Data.Items)")
        print("   - This may not be executing on UI thread")
        print("   - Solution: Wrap in Application.Current.Dispatcher.Invoke()")
        print()
        print("4. 🎯 RefreshCommand Flow:")
        print("   - RefreshCommand → RefreshDataAsync → LoadItemsAsync → Items update")
        print("   - Each step must preserve UI thread context")
        print("   - Debug: Add Dispatcher.CheckAccess() calls")
        print()
        print("RECOMMENDED FIX:")
        print("```csharp")
        print("// In LoadItemsAsync around line 221:")
        print("if (result.IsSuccess && result.Data != null)")
        print("{")
        print("    Application.Current.Dispatcher.Invoke(() => {")
        print("        Items = new ObservableCollection<TItem>(result.Data.Items);")
        print("    });")
        print("    PaginationCoordinator.UpdatePagination(result.Data.TotalCount);")
        print("}")
        print("```")
        
    else:
        print("❌ BACKEND ISSUE: Data changes are not working")
        print(f"   Reason: {change_result.get('reason', 'Unknown')}")
        print("   → Fix backend issue first before investigating UI refresh")

def main():
    """主测试函数"""
    print("="*60)
    print("UI线程刷新问题诊断 - UI Thread Refresh Diagnosis")
    print(f"Test started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("="*60)
    
    # 获取认证Token
    token = get_auth_token()
    if not token:
        print("[ERROR] Cannot get authentication token, test terminated")
        return False
    
    # 测试数据变化
    change_result = test_user_data_changes(token)
    
    # 分析UI刷新问题
    analyze_ui_refresh_issue(change_result)
    
    return True

if __name__ == "__main__":
    success = main()
    exit_code = 0 if success else 1
    sys.exit(exit_code)