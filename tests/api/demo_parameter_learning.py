#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
演示参数自动学习功能
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import requests
import time
from config.parameter_memory import get_parameter, record_error

BASE_URL = "http://localhost:5297"

def demo_parameter_learning():
    """演示参数自动学习功能"""
    print("=" * 50)
    print("参数自动学习功能演示")
    print("=" * 50)
    
    # 第一次：使用可能错误的参数
    print("\n第一次测试 - 使用生成的参数")
    api_version = get_parameter("apiVersion", "V1.0")  # 生成错误的版本号
    print(f"生成的API版本: {api_version}")
    
    # 尝试登录
    success = try_login(api_version)
    if not success:
        print("登录失败，尝试修正参数")
        # 记录错误并尝试正确值
        correct_version = "1.0"
        if try_login(correct_version):
            record_error("apiVersion", api_version, correct_version)
    
    print("\n" + "-" * 50)
    
    # 第二次：再次使用错误参数（演示自动记忆）
    print("\n第二次测试 - 再次使用错误参数")
    api_version2 = get_parameter("apiVersion", "V2.0")  # 又生成了错误的版本号
    print(f"这次生成的API版本: {api_version2}")
    
    # 如果之前已经记忆，这里应该自动使用正确值
    success2 = try_login(api_version2)
    
    print("\n" + "=" * 50)
    print("演示完成")
    print("=" * 50)

def try_login(api_version):
    """尝试登录"""
    login_data = {
        "username": "sysadmin",
        "password": "Admin@123456",
        "rememberMe": True
    }
    
    try:
        login_url = f"{BASE_URL}/api/v{api_version}/Auth/login"
        print(f"尝试登录URL: {login_url}")
        
        response = requests.post(
            login_url,
            json=login_data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        print(f"状态码: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            if result.get('success'):
                print("登录成功!")
                return True
            else:
                print(f"登录失败: {result.get('message', '未知错误')}")
        elif response.status_code == 404:
            print("API接口未找到 (404) - 版本号错误")
        else:
            print(f"请求失败: {response.text}")
            
    except Exception as e:
        print(f"请求异常: {str(e)}")
    
    return False

if __name__ == "__main__":
    # 等待服务器启动
    print("等待服务器启动...")
    time.sleep(2)
    
    demo_parameter_learning()