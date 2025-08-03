#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
智能API测试脚本 - 集成参数记忆功能
自动学习和记忆正确的参数值，避免重复错误
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import requests
import json
import time
from config.parameter_memory import get_parameter, record_error

# 基础配置
BASE_URL = "http://localhost:5297"

class SmartAPITester:
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
        """测试登录接口 - 使用智能参数"""
        print("测试登录接口...")
        
        # 使用参数记忆功能获取API版本
        api_version = get_parameter("apiVersion", "V1.0")  # 默认生成可能错误的值
        
        login_data = {
            "username": "sysadmin", 
            "password": "Admin@123456",
            "rememberMe": True
        }
        
        login_url = f"{BASE_URL}/api/v{api_version}/Auth/login"
        
        try:
            response = self.session.post(
                login_url,
                json=login_data,
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            print(f"使用的API版本: v{api_version}")
            
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    self.token = result['data']['token']
                    print("登录成功")
                    return True
                else:
                    print(f"❌ 登录失败: {result.get('message', '未知错误')}")
                    # 这里可能是业务逻辑错误，不是参数错误
                    return False
            elif response.status_code == 404:
                # 404错误通常意味着API版本错误
                print(f"❌ API接口未找到 (404) - 可能是版本号错误")
                
                # 尝试常见的正确版本号
                correct_versions = ["1.0", "1", "2.0", "v1", "v1.0"]
                for correct_version in correct_versions:
                    if self._try_login_with_version(correct_version, login_data):
                        # 记录参数错误并自动学习
                        record_error("apiVersion", api_version, correct_version)
                        return True
                
                print("❌ 尝试所有常见版本号都失败")
                return False
            else:
                print(f"❌ 登录请求失败: {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ 登录异常: {str(e)}")
            return False
    
    def _try_login_with_version(self, version: str, login_data: dict) -> bool:
        """尝试使用特定版本号登录"""
        try:
            login_url = f"{BASE_URL}/api/v{version}/Auth/login"
            response = self.session.post(
                login_url,
                json=login_data,
                headers=self.get_headers(),
                timeout=10
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    self.token = result['data']['token']
                    print(f"✅ 使用版本 v{version} 登录成功")
                    return True
        except:
            pass
        return False
    
    def test_users_api(self):
        """测试用户API接口"""
        if not self.token:
            print("❌ 未获取到token，跳过用户API测试")
            return False
            
        print("\n👥 测试用户API接口...")
        
        # 使用记忆的API版本
        api_version = get_parameter("apiVersion", "1.0")
        
        try:
            response = self.session.get(
                f"{BASE_URL}/api/v{api_version}/Users",
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
    
    def test_patients_api(self):
        """测试患者API接口"""
        if not self.token:
            print("❌ 未获取到token，跳过患者API测试")
            return False
            
        print("\n🏥 测试患者API接口...")
        
        # 使用记忆的API版本
        api_version = get_parameter("apiVersion", "1.0")
        
        try:
            response = self.session.get(
                f"{BASE_URL}/api/v{api_version}/Patients",
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    data = result.get('data', {})
                    print(f"✅ 患者列表获取成功")
                    print(f"总记录数: {data.get('TotalCount', 0)}")
                    return True
                else:
                    print(f"❌ 获取失败: {result.get('message', '未知错误')}")
            else:
                print(f"❌ 请求失败: {response.text}")
                
        except Exception as e:
            print(f"❌ 异常: {str(e)}")
            
        return False
    
    def test_herbs_api(self):
        """测试药材API接口"""
        if not self.token:
            print("❌ 未获取到token，跳过药材API测试")
            return False
            
        print("\n🌿 测试药材API接口...")
        
        # 使用记忆的API版本
        api_version = get_parameter("apiVersion", "1.0")
        
        try:
            response = self.session.get(
                f"{BASE_URL}/api/v{api_version}/Herbs",
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    data = result.get('data', {})
                    print(f"✅ 药材列表获取成功")
                    print(f"总记录数: {data.get('TotalCount', 0)}")
                    return True
                else:
                    print(f"❌ 获取失败: {result.get('message', '未知错误')}")
            else:
                print(f"❌ 请求失败: {response.text}")
                
        except Exception as e:
            print(f"❌ 异常: {str(e)}")
            
        return False
    
    def run_smart_tests(self):
        """运行智能测试"""
        print("=" * 60)
        print("🧠 智能API测试开始 - 集成参数自动学习")
        print("=" * 60)
        
        # 测试登录
        if not self.test_login():
            print("❌ 登录失败，无法继续测试")
            return
        
        # 测试各个API模块
        self.test_users_api()
        self.test_patients_api()
        self.test_herbs_api()
        
        print("\n" + "=" * 60)
        print("🧠 智能测试完成")
        print("=" * 60)

def main():
    # 等待服务器启动
    print("等待服务器启动...")
    time.sleep(3)
    
    tester = SmartAPITester()
    tester.run_smart_tests()

if __name__ == "__main__":
    main()