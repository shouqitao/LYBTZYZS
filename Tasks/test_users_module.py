#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试用户模块API接口
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

def login():
    """登录获取JWT令牌"""
    global jwt_token
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/Login",
            json={"username": "sysadmin", "password": "Admin@123456"},
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        if response.status_code == 200:
            data = response.json()
            if data.get("success") and data.get("data") and data["data"].get("token"):
                jwt_token = data["data"]["token"]
                print(f"[LOGIN] [OK] 登录成功，获取到JWT令牌")
                return True
        print(f"[LOGIN] [FAIL] 登录失败")
        return False
    except Exception as e:
        print(f"[LOGIN] [ERROR] 登录异常: {str(e)}")
        return False

def test_get_users():
    """测试获取用户列表接口"""
    print("\n" + "-" * 40)
    print("测试 GET /Users 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[GET_USERS] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    try:
        response = requests.get(
            f"{BASE_URL}{API_PREFIX}/Users",
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[GET_USERS] 状态码: {response.status_code}")
        print(f"[GET_USERS] 响应内容: {response.text[:300]}...")
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success"):
                print(f"[GET_USERS] [OK] 获取用户列表成功")
                return True
            else:
                print(f"[GET_USERS] [FAIL] 获取用户列表失败: {data.get('message')}")
                return False
        else:
            print(f"[GET_USERS] [FAIL] 获取用户列表失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[GET_USERS] [ERROR] 获取用户列表异常: {str(e)}")
        return False

def test_get_user_by_id():
    """测试根据ID获取用户接口"""
    print("\n" + "-" * 40)
    print("测试 GET /Users/{id} 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[GET_USER] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    # 使用一个测试用的GUID
    test_id = "f217f17e-7b51-43b7-81b2-138188cb84cd"
    
    try:
        response = requests.get(
            f"{BASE_URL}{API_PREFIX}/Users/{test_id}",
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[GET_USER] 状态码: {response.status_code}")
        print(f"[GET_USER] 响应内容: {response.text[:300]}...")
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success"):
                print(f"[GET_USER] [OK] 获取用户详情成功")
                return True
            else:
                print(f"[GET_USER] [FAIL] 获取用户详情失败: {data.get('message')}")
                return False
        elif response.status_code == 404:
            print(f"[GET_USER] [OK] 用户不存在返回404，接口正常工作")
            return True
        else:
            print(f"[GET_USER] [FAIL] 获取用户详情失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[GET_USER] [ERROR] 获取用户详情异常: {str(e)}")
        return False

def test_create_user():
    """测试创建用户接口"""
    print("\n" + "-" * 40)
    print("测试 POST /Users 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[CREATE_USER] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    # 创建测试用户数据
    user_data = {
        "userName": "testuser" + str(int(time.time())),  # 避免重复
        "password": "Admin@123456",
        "realName": "测试用户",
        "phoneNumber": "13800138000",
        "email": "test@example.com",
        "role": "Doctor"
    }
    
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Users",
            json=user_data,
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[CREATE_USER] 状态码: {response.status_code}")
        print(f"[CREATE_USER] 响应内容: {response.text[:300]}...")
        
        if response.status_code in [200, 201]:
            data = response.json()
            if data.get("success"):
                print(f"[CREATE_USER] [OK] 创建用户成功")
                # 保存创建的用户ID供后续测试使用
                if data.get("data") and data["data"].get("id"):
                    global created_user_id
                    created_user_id = data["data"]["id"]
                return True
            else:
                print(f"[CREATE_USER] [FAIL] 创建用户失败: {data.get('message')}")
                return False
        else:
            print(f"[CREATE_USER] [FAIL] 创建用户失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[CREATE_USER] [ERROR] 创建用户异常: {str(e)}")
        return False

def test_update_user():
    """测试更新用户接口"""
    print("\n" + "-" * 40)
    print("测试 PUT /Users/{id} 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[UPDATE_USER] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    # 使用测试ID
    test_id = "f217f17e-7b51-43b7-81b2-138188cb84cd"
    
    # 更新用户数据
    update_data = {
        "id": test_id,
        "userName": "sysadmin",
        "realName": "系统管理员",
        "phoneNumber": "13800138001",
        "email": "admin@example.com"
    }
    
    try:
        response = requests.put(
            f"{BASE_URL}{API_PREFIX}/Users/{test_id}",
            json=update_data,
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[UPDATE_USER] 状态码: {response.status_code}")
        print(f"[UPDATE_USER] 响应内容: {response.text[:300]}...")
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success"):
                print(f"[UPDATE_USER] [OK] 更新用户成功")
                return True
            else:
                print(f"[UPDATE_USER] [FAIL] 更新用户失败: {data.get('message')}")
                return False
        elif response.status_code == 404:
            print(f"[UPDATE_USER] [OK] 用户不存在返回404，接口正常工作")
            return True
        else:
            print(f"[UPDATE_USER] [FAIL] 更新用户失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[UPDATE_USER] [ERROR] 更新用户异常: {str(e)}")
        return False

def test_delete_user():
    """测试删除用户接口"""
    print("\n" + "-" * 40)
    print("测试 DELETE /Users/{id} 接口")
    print("-" * 40)
    
    if not jwt_token:
        print("[DELETE_USER] [SKIP] 无JWT令牌，跳过测试")
        return False
    
    # 使用一个不存在的测试ID
    test_id = "99999999-7b51-43b7-81b2-138188cb84cd"
    
    try:
        response = requests.delete(
            f"{BASE_URL}{API_PREFIX}/Users/{test_id}",
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {jwt_token}"
            },
            timeout=10
        )
        
        print(f"[DELETE_USER] 状态码: {response.status_code}")
        print(f"[DELETE_USER] 响应内容: {response.text[:300]}...")
        
        if response.status_code == 200:
            data = response.json()
            if data.get("success"):
                print(f"[DELETE_USER] [OK] 删除用户成功")
                return True
            else:
                print(f"[DELETE_USER] [FAIL] 删除用户失败: {data.get('message')}")
                return False
        elif response.status_code == 404:
            print(f"[DELETE_USER] [OK] 用户不存在返回404，接口正常工作")
            return True
        else:
            print(f"[DELETE_USER] [FAIL] 删除用户失败，状态码: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"[DELETE_USER] [ERROR] 删除用户异常: {str(e)}")
        return False

def run_users_tests():
    """运行所有用户模块测试"""
    print("开始测试用户模块...")
    print(f"目标服务器: {BASE_URL}")
    print("=" * 60)
    
    # 首先登录
    if not login():
        print("登录失败，无法继续测试")
        return
    
    results = []
    
    # 1. 测试获取用户列表
    results.append(("GET /Users", test_get_users()))
    
    # 2. 测试根据ID获取用户
    results.append(("GET /Users/{id}", test_get_user_by_id()))
    
    # 3. 测试创建用户
    results.append(("POST /Users", test_create_user()))
    
    # 4. 测试更新用户
    results.append(("PUT /Users/{id}", test_update_user()))
    
    # 5. 测试删除用户
    results.append(("DELETE /Users/{id}", test_delete_user()))
    
    # 统计结果
    print("\n" + "=" * 60)
    print("用户模块测试结果汇总")
    print("=" * 60)
    
    success_count = 0
    total_count = len(results)
    
    for test_name, success in results:
        status = "[OK] 成功" if success else "[FAIL] 失败"
        print(f"[{test_name:20}] {status}")
        if success:
            success_count += 1
    
    print(f"\n[总体结果] 成功: {success_count}/{total_count}")
    print(f"[总体结果] 成功率: {success_count/total_count*100:.1f}%")
    
    return success_count, total_count

if __name__ == "__main__":
    run_users_tests()