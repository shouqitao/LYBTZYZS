#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试用户API（使用5298端口）
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import requests
import json
import time
from config.parameter_memory import get_parameter

BASE_URL = "http://localhost:5298"

def test_user_api():
    """测试用户API"""
    print("=== 测试用户API (端口5298) ===")
    
    # 登录获取token
    api_version = get_parameter("apiVersion", "1.0")
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
            f"{BASE_URL}/api/v{api_version}/Auth/login",
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
        print("登录成功")
        
        # 2. 测试Users API
        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}"
        }
        
        print("\n2. 测试Users API...")
        
        user_response = session.get(
            f"{BASE_URL}/api/v{api_version}/Users",
            headers=headers,
            timeout=10
        )
        
        print(f"Users API状态码: {user_response.status_code}")
        if user_response.status_code == 500:
            result = user_response.json()
            print(f"仍然存在500错误: {result.get('message', 'N/A')}")
            
            # 分析错误信息
            error_msg = result.get('message', '')
            if '列名' in error_msg and '无效' in error_msg:
                print("字段映射问题仍然存在")
                print("需要检查EF Core模型缓存或应用程序配置")
            else:
                print("字段映射问题已解决，但存在其他错误")
        elif user_response.status_code == 200:
            print("Users API修复成功！")
            result = user_response.json()
            print(f"返回数据: {json.dumps(result, ensure_ascii=False, indent=2)}")
        else:
            print(f"未知状态码: {user_response.status_code}")
            print(f"响应内容: {user_response.text}")
            
    except requests.exceptions.ConnectAeionError:
        print("无法连接到服务器，可能服务器未启动")
    except Exception as e:
        print(f"测试失败: {str(e)}")

def main():
    print("等待服务器启动...")
    time.sleep(5)
    test_user_api()

if __name__ == "__main__":
    main()