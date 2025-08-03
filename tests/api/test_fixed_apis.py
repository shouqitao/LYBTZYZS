#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复后的API测试脚本
测试用户、患者、药材模块的核心接口
"""

import requests
import json
import time
from typing import Dict, Any

# 配置
BASE_URL = "http://localhost:5297"
API_VERSION = "1.0"

class APITester:
    def __init__(self):
        self.token = None
        self.session = requests.Session()
        
    def get_headers(self):
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json"
        }
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return headers
    
    def test_login(self):
        """测试登录接口"""
        print("测试登录接口...")
        
        login_data = {
            "username": "sysadmin",
            "password": "Admin@123456",
            "rememberMe": True
        }
        
        try:
            response = self.session.post(
                f"{BASE_URL}/api/v{API_VERSION}/Auth/login",
                json=login_data,
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    self.token = result['data']['token']
                    print("✅ 登录成功")
                    print(f"Token: {self.token[:50]}...")
                    return True
                else:
                    print(f"❌ 登录失败: {result.get('message', '未知错误')}")
            else:
                print(f"❌ 登录请求失败: {response.text}")
                
        except Exception as e:
            print(f"❌ 登录异常: {str(e)}")
            
        return False
    
    def test_debug_users(self):
        """测试调试用户接口"""
        print("\n🔍 测试调试用户接口...")
        
        try:
            response = self.session.get(
                f"{BASE_URL}/api/v{API_VERSION}/Debug/users",
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    data = result.get('data', {})
                    print(f"✅ 用户数据查询成功")
                    print(f"总用户数: {data.get('TotalCount', 0)}")
                    users = data.get('Users', [])
                    for user in users[:3]:  # 显示前3个用户
                        print(f"- ID: {user.get('Id')}, 用户名: {user.get('Username')}, 姓名: {user.get('RealName')}")
                    return True
                else:
                    print(f"❌ 查询失败: {result.get('message', '未知错误')}")
            else:
                print(f"❌ 请求失败: {response.text}")
                
        except exception as e:
            print(f"❌ 异常: {str(e)}")
            
        return False
    
    def test_debug_patients(self):
        """测试调试患者接口"""
        print("\n🔍 测试调试患者接口...")
        
        try:
            response = self.session.get(
                f"{BASE_URL}/api/v{API_VERSION}/Debug/patients",
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    data = result.get('data', {})
                    print(f"✅ 患者数据查询成功")
                    print(f"总患者数: {data.get('TotalCount', 0)}")
                    patients = data.get('Patients', [])
                    for patient in patients[:3]:  # 显示前3个患者
                        print(f"- ID: {patient.get('Id')}, 姓名: {patient.get('Name')}, 性别: {patient.get('Gender')}")
                    return True
                else:
                    print(f"❌ 查询失败: {result.get('message', '未知错误')}")
            else:
                print(f"❌ 请求失败: {response.text}")
                
        except Exception as e:
            print(f"❌ 异常: {str(e)}")
            
        return False
    
    def test_debug_herbs(self):
        """测试调试药材接口"""
        print("\n🔍 测试调试药材接口...")
        
        try:
            response = self.session.get(
                f"{BASE_URL}/api/v{API_VERSION}/Debug/herbs",
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    data = result.get('data', {})
                    print(f"✅ 药材数据查询成功")
                    print(f"总药材数: {data.get('TotalCount', 0)}")
                    herbs = data.get('Herbs', [])
                    for herb in herbs[:3]:  # 显示前3个药材
                        print(f"- ID: {herb.get('Id')}, 名称: {herb.get('Name')}, 单位: {herb.get('Unit')}")
                    return True
                else:
                    print(f"❌ 查询失败: {result.get('message', '未知错误')}")
            else:
                print(f"❌ 请求失败: {response.text}")
                
        except Exception as e:
            print(f"❌ 异常: {str(e)}")
            
        return False
    
    def test_users_api(self):
        """测试用户API接口"""
        print("\n👥 测试用户API接口...")
        
        try:
            # 测试获取用户列表
            response = self.session.get(
                f"{BASE_URL}/api/v{API_VERSION}/Users",
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    data = result.get('data', {})
                    print(f"✅ 用户列表获取成功")
                    print(f"总记录数: {data.get('TotalCount', 0)}")
                    return True
                else:
                    print(f"❌ 获取失败: {result.get('message', '未知错误')}")
            else:
                print(f"❌ 请求失败: {response.text}")
                
        except Exception as e:
            print(f"❌ 异常: {str(e)}")
            
        return False
    
    def run_tests(self):
        """运行所有测试"""
        print("=" * 60)
        print("🧪 修复后API测试开始")
        print("=" * 60)
        
        # 测试登录
        if not self.test_login():
            print("❌ 登录失败，无法继续测试")
            return
        
        # 测试调试接口
        self.test_debug_users()
        self.test_debug_patients()
        self.test_debug_herbs()
        
        # 测试正式API接口
        self.test_users_api()
        
        print("\n" + "=" * 60)
        print("🧪 测试完成")
        print("=" * 60)

if __name__ == "__main__":
    # 等待服务器启动
    print("等待服务器启动...")
    time.sleep(3)
    
    tester = APITester()
    tester.run_tests()