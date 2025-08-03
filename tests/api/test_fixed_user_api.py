#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试修复后的用户API（端口5001）
"""

import requests
import json
import time

BASE_URL = "http://localhost:5298"

def test_fixed_user_api():
    """测试修复后的用户API"""
    print("=== 测试修复后的用户API (端口5298) ===")
    
    # 登录获取token
    login_data = {
        "username": "sysadmin",
        "password": "Admin@123456",
        "rememberMe": True
    }
    
    session = requests.Session()
    try:
        # 1. 登录
        print("1. 正在登录...")
        response = session.post(
            f"{BASE_URL}/api/v1.0/Auth/login",
            json=login_data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        if response.status_code != 200:
            print(f"登录失败: {response.status_code} - {response.text}")
            return
            
        result = response.json()
        if not result.get('success'):
            print(f"登录失败: {result.get('message', 'N/A')}")
            return
            
        token = result['data']['token']
        print("✅ 登录成功")
        
        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}"
        }
        
        # 2. 测试Users API
        print("\n2. 测试Users API...")
        
        user_response = session.get(
            f"{BASE_URL}/api/v1.0/Users",
            headers=headers,
            timeout=10
        )
        
        print(f"Users API状态码: {user_response.status_code}")
        
        if user_response.status_code == 500:
            result = user_response.json()
            error_msg = result.get('message', '')
            print(f"❌ 仍然存在500错误: {error_msg}")
            
            # 检查是否仍然是字段映射问题
            if '列名' in error_msg and '无效' in error_msg:
                print("❌ 字段映射问题仍然存在")
                if 'Department' in error_msg:
                    print("  - Department字段问题")
                if 'Position' in error_msg:
                    print("  - Position字段问题")
                if 'Remark' in error_msg:
                    print("  - Remark字段问题")
                if 'UpdateTime' in error_msg:
                    print("  - UpdateTime字段问题")
            else:
                print("❓ 字段映射问题已解决，但存在其他错误")
                
        elif user_response.status_code == 200:
            print("🎉 Users API修复成功！")
            result = user_response.json()
            if result.get('success'):
                data = result.get('data', {})
                if isinstance(data, dict) and 'items' in data:
                    users = data['items']
                    print(f"✅ 返回 {len(users)} 条用户记录")
                    
                    # 检查返回的用户记录是否包含新字段
                    if users:
                        first_user = users[0]
                        fields_to_check = ['department', 'position', 'remark', 'updateTime']
                        print("\n📋 用户字段检查:")
                        for field in fields_to_check:
                            if field in first_user:
                                value = first_user[field]
                                status = "✅" if value is not None else "⚪"
                                print(f"  {status} {field}: {value}")
                            else:
                                print(f"  ❌ {field}: 字段不存在")
                                
                elif isinstance(data, list):
                    print(f"✅ 返回 {len(data)} 条用户记录")
                else:
                    print("❓ 数据结构异常:", type(data))
                    
        else:
            print(f"❓ 未知状态码 {user_response.status_code}")
            print(f"响应内容: {user_response.text[:500]}")
            
    except requests.exceptions.ConnectionError:
        print("❌ 无法连接到服务器，请确认服务器已启动")
    except Exception as e:
        print(f"❌ 测试失败: {str(e)}")

def main():
    print("等待服务器完全启动...")
    time.sleep(3)
    test_fixed_user_api()

if __name__ == "__main__":
    main()