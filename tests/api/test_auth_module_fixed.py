#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试认证模块API接口 - 修复版
"""

import requests
import json
import datetime
from typing import Dict, List, Tuple, Any
import time

# 服务器配置
BASE_URL = "http://192.168.190.243:5000"
API_PREFIX = "/api/v1.0"

# JWT 令牌存储
jwt_token = None

def test_login():
    """测试登录接口"""
    print("=" * 60)
    print("测试 Auth 模块 - 登录接口")
    print("=" * 60)
    
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/Login",
            json={"username": "sysadmin", "password": "Admin@123456"},
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        print(f"[LOGIN] 状态码: {response.status_code}")
        
        if response.status_code == 200:
            data = response.json()
            print(f"[LOGIN] 响应内容: {json.dumps(data, ensure_ascii=False, indent=2)}")
            
            if data.get("success") and data.get("data") and data["data"].get("token"):
                global jwt_token
                jwt_token = data["data"]["token"]
                print(f"[LOGIN] [OK] 登录成功，获取到JWT令牌")
                return True
            else:
                print(f"[LOGIN] [FAIL] 登录失败: {data.get('message', '未知错误')}")
                return False
        else:
            print(f"[LOGIN] [FAIL] 登录失败: {response.text}")
            return False
            
    except Exception as e:
        print(f"[LOGIN] [ERROR] 登录异常: {str(e)}")
        return False

def test_refresh_token():
    """测试刷新令牌接口"""
    print("\n" + "-" * 40)
    print("测试 RefreshToken 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[REFRESH] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/RefreshToken",
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[REFRESH] 状态码: {response.status_code}")
        print(f"[REFRESH] 响应内容: {response.text}")
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success"):
                print(f"[REFRESH] [OK] 令牌刷新成功")
                return True
            else:
                print(f"[REFRESH] [FAIL] 令牌刷新失败: {data.get('message')}")
                return False
        else:
            print(f"[REFRESH] [FAIL] 令牌刷新失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[REFRESH] [ERROR] 令牌刷新异常: {str(e)}")
        return False

def test_change_password():
    """测试修改密码接口"""
    print("\n" + "-" * 40)
    print("测试 ChangePassword 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[CHANGE_PWD] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    try:
        # 使用相同密码进行测试（不真正修改）
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/ChangePassword",
            json={
                "oldPassword": "Admin@123456",
                "newPassword": "Admin@123456"
            },
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[CHANGE_PWD] 状态码: {response.status_code}")
        print(f"[CHANGE_PWD] 响应内容: {response.text}")
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success"):
                print(f"[CHANGE_PWD] [OK] 密码修改成功")
                return True
            else:
                print(f"[CHANGE_PWD] [FAIL] 密码修改失败: {data.get('message')}")
                return False
        else:
            print(f"[CHANGE_PWD] [FAIL] 密码修改失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[CHANGE_PWD] [ERROR] 密码修改异常: {str(e)}")
        return False

def test_logout():
    """测试登出接口"""
    print("\n" + "-" * 40)
    print("测试 Logout 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[LOGOUT] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/Logout",
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[LOGOUT] 状态码: {response.status_code}")
        print(f"[LOGOUT] 响应内容: {response.text}")
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success"):
                print(f"[LOGOUT] [OK] 登出成功")
                return True
            else:
                print(f"[LOGOUT] [FAIL] 登出失败: {data.get('message')}")
                return False
        else:
            print(f"[LOGOUT] [FAIL] 登出失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[LOGOUT] [ERROR] 登出异常: {str(e)}")
        return False

def run_auth_tests():
    """运行所有认证模块测试"""
    print("开始测试认证模块...")
    print(f"目标服务器: {BASE_URL}")
    
    results = []
    
    # 1. 测试登录
    results.append(("Login", test_login()))
    
    # 2. 测试刷新令牌
    results.append(("RefreshToken", test_refresh_token()))
    
    # 3. 测试修改密码
    results.append(("ChangePassword", test_change_password()))
    
    # 4. 测试登出
    results.append(("Logout", test_logout()))
    
    # 统计结果
    print("\n" + "=" * 60)
    print("认证模块测试结果汇总")
    print("=" * 60)
    
    success_count = 0
    total_count = len(results)
    
    for test_name, success in results:
        status = "[OK] 成功" if success else "[FAIL] 失败"
        print(f"[{test_name:15}] {status}")
        if success:
            success_count += 1
    
    print(f"\n[总体结果] 成功: {success_count}/{total_count}")
    print(f"[总体结果] 成功率: {success_count/total_count*100:.1f}%")
    
    return success_count, total_count

if __name__ == "__main__":
    run_auth_tests()