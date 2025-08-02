#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import requests
import json
import time

BASE_URL = "https://localhost:7157"

def test_user_api():
    """简单测试用户API"""
    print("测试修复后的用户API")
    
    login_data = {
        "username": "sysadmin",
        "password": "Admin@123456",
        "rememberMe": True
    }
    
    session = requests.Session()
    session.verify = False  # 忽略SSL证书验证
    try:
        # 登录
        print("正在登录...")
        response = session.post(
            f"{BASE_URL}/api/v1.0/Auth/login",
            json=login_data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        if response.status_code != 200:
            print(f"登录失败: {response.status_code}")
            return
            
        result = response.json()
        if not result.get('success'):
            print(f"登录失败: {result.get('message', 'N/A')}")
            return
            
        token = result['data']['token']
        print("登录成功")
        
        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}"
        }
        
        # 测试Users API
        print("测试Users API...")
        
        user_response = session.get(
            f"{BASE_URL}/api/v1.0/Users",
            headers=headers,
            timeout=10
        )
        
        print(f"状态码: {user_response.status_code}")
        
        if user_response.status_code == 500:
            result = user_response.json()
            error_msg = result.get('message', '')
            print(f"500错误: {error_msg}")
            
            if '列名' in error_msg and '无效' in error_msg:
                print("字段映射问题仍然存在")
                return False
            else:
                print("字段映射问题已解决，但存在其他错误")
                return False
                
        elif user_response.status_code == 200:
            print("SUCCESS: Users API修复成功!")
            result = user_response.json()
            if result.get('success'):
                data = result.get('data', {})
                if isinstance(data, dict) and 'items' in data:
                    users = data['items']
                    print(f"返回 {len(users)} 条用户记录")
                    
                    if users:
                        first_user = users[0]
                        print("用户字段:")
                        print(f"  username: {first_user.get('username', 'N/A')}")
                        print(f"  realName: {first_user.get('realName', 'N/A')}")
                        print(f"  department: {first_user.get('department', 'N/A')}")
                        print(f"  position: {first_user.get('position', 'N/A')}")
                        print(f"  remark: {first_user.get('remark', 'N/A')}")
                        print(f"  updateTime: {first_user.get('updateTime', 'N/A')}")
                    return True
        else:
            print(f"未知状态码: {user_response.status_code}")
            return False
            
    except requests.exceptions.ConnectionError:
        print("无法连接到服务器")
        return False
    except Exception as e:
        print(f"测试失败: {str(e)}")
        return False

if __name__ == "__main__":
    time.sleep(2)
    success = test_user_api()
    if success:
        print("\n=== 修复验证成功 ===")
    else:
        print("\n=== 修复验证失败 ===")