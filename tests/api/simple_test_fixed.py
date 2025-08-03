#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
简化的API修复测试脚本
"""

import requests
import json
import time

BASE_URL = "http://localhost:5297"
API_VERSION = "1.0"

def test_login():
    """测试登录接口"""
    print("测试登录接口...")
    
    login_data = {
        "username": "sysadmin",
        "password": "Admin@123456",
        "rememberMe": True
    }
    
    try:
        response = requests.post(
            f"{BASE_URL}/api/v{API_VERSION}/Auth/login",
            json=login_data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        print(f"状态码: {response.status_code}")
        if response.status_code == 200:
            result = response.json()
            if result.get('success'):
                token = result['data']['token']
                print("登录成功")
                return token
            else:
                print(f"登录失败: {result.get('message', '未知错误')}")
        else:
            print(f"登录请求失败: {response.text}")
    except Exception as e:
        print(f"登录异常: {str(e)}")
    return None

def test_debug_users(token):
    """测试调试用户接口"""
    print("\n测试调试用户接口...")
    
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {token}"
    }
    
    try:
        response = requests.get(
            f"{BASE_URL}/api/v{API_VERSION}/Debug/users",
            headers=headers,
            timeout=10
        )
        
        print(f"状态码: {response.status_code}")
        if response.status_code == 200:
            result = response.json()
            if result.get('success'):
                data = result.get('data', {})
                print(f"用户数据查询成功")
                print(f"总用户数: {data.get('TotalCount', 0)}")
                return True
            else:
                print(f"查询失败: {result.get('message', '未知错误')}")
        else:
            print(f"请求失败: {response.text}")
    except Exception as e:
        print(f"异常: {str(e)}")
    return False

def test_users_api(token):
    """测试用户API接口"""
    print("\n测试用户API接口...")
    
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {token}"
    }
    
    try:
        response = requests.get(
            f"{BASE_URL}/api/v{API_VERSION}/Users",
            headers=headers,
            timeout=10
        )
        
        print(f"状态码: {response.status_code}")
        if response.status_code == 200:
            result = response.json()
            if result.get('success'):
                data = result.get('data', {})
                print(f"用户列表获取成功")
                print(f"总记录数: {data.get('TotalCount', 0)}")
                return True
            else:
                print(f"获取失败: {result.get('message', '未知错误')}")
        else:
            print(f"请求失败: {response.text}")
    except Exception as e:
        print(f"异常: {str(e)}")
    return False

def main():
    print("=" * 50)
    print("修复后API测试开始")
    print("=" * 50)
    
    # 等待服务器启动
    print("等待服务器启动...")
    time.sleep(3)
    
    # 测试登录
    token = test_login()
    if not token:
        print("登录失败，无法继续测试")
        return
    
    # 测试调试接口
    test_debug_users(token)
    
    # 测试正式API接口  
    test_users_api(token)
    
    print("\n" + "=" * 50)
    print("测试完成")
    print("=" * 50)

if __name__ == "__main__":
    main()