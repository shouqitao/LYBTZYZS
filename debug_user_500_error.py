#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
调试用户API 500错误专用脚本
深入分析剩余的字段映射问题
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import requests
import json
import time
from config.parameter_memory import get_parameter

BASE_URL = "http://localhost:5297"

class UserAPI500Debugger:
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

    def login(self):
        """登录获取token"""
        print("=== 登录获取Token ===")
        
        api_version = get_parameter("apiVersion", "1.0")
        login_data = {
            "username": "sysadmin",
            "password": "Admin@123456",
            "rememberMe": True
        }
        
        try:
            response = self.session.post(
                f"{BASE_URL}/api/v{api_version}/Auth/login",
                json=login_data,
                headers=self.get_headers(),
                timeout=10
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    self.token = result['data']['token']
                    print("登录成功")
                    return True
            
            print(f"登录失败: {response.status_code} - {response.text}")
            return False
            
        except Exception as e:
            print(f"登录异常: {str(e)}")
            return False

    def test_users_get_simple(self):
        """测试简单的用户GET请求"""
        print("\n=== 测试 GET /Users (简单请求) ===")
        
        api_version = get_parameter("apiVersion", "1.0")
        
        try:
            response = self.session.get(
                f"{BASE_URL}/api/v{api_version}/Users",
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            print(f"响应头: {dict(response.headers)}")
            
            if response.status_code == 500:
                try:
                    error_data = response.json()
                    print("500错误详情:")
                    print(f"  成功: {error_data.get('success')}")
                    print(f"  状态码: {error_data.get('statusCode')}")
                    print(f"  错误消息: {error_data.get('message')}")
                    print(f"  时间戳: {error_data.get('timestamp')}")
                    
                    # 分析错误消息中的具体字段问题
                    message = error_data.get('message', '')
                    if '列名' in message and '无效' in message:
                        print("\n字段映射问题分析:")
                        # 提取所有无效的列名
                        invalid_columns = []
                        lines = message.split('\\r\\n')
                        for line in lines:
                            if '列名' in line and '无效' in line:
                                # 提取列名
                                start = line.find("'") + 1
                                end = line.find("'", start)
                                if start > 0 and end > start:
                                    column_name = line[start:end]
                                    invalid_columns.append(column_name)
                        
                        print(f"  无效列名: {invalid_columns}")
                        return invalid_columns
                        
                except json.JSONDecodeError:
                    print("响应不是JSON格式")
                    print(f"原始响应: {response.text}")
            
            elif response.status_code == 200:
                try:
                    result = response.json()
                    print("请求成功:")
                    print(f"  成功: {result.get('success')}")
                    if result.get('success'):
                        data = result.get('data', {})
                        print(f"  总记录数: {data.get('TotalCount', 0)}")
                        print("  用户API修复成功!")
                        return True
                except json.JSONDecodeError:
                    print("响应不是JSON格式")
            
            else:
                print(f"其他错误: {response.text}")
                
        except Exception as e:
            print(f"请求异常: {str(e)}")
        
        return False

    def test_users_paged_post(self):
        """测试用户分页POST请求"""
        print("\n=== 测试 POST /Users/paged (分页请求) ===")
        
        api_version = get_parameter("apiVersion", "1.0")
        
        # 简单的分页查询数据
        query_data = {
            "currentPage": 1,
            "pageSize": 10
        }
        
        try:
            response = self.session.post(
                f"{BASE_URL}/api/v{api_version}/Users/paged",
                json=query_data,
                headers=self.get_headers(),
                timeout=10
            )
            
            print(f"状态码: {response.status_code}")
            
            if response.status_code == 500:
                try:
                    error_data = response.json()
                    print("500错误详情:")
                    print(f"  错误消息: {error_data.get('message')}")
                    return self._analyze_500_error(error_data.get('message', ''))
                except json.JSONDecodeError:
                    print(f"原始响应: {response.text}")
            
            elif response.status_code == 200:
                try:
                    result = response.json()
                    if result.get('success'):
                        data = result.get('data', {})
                        print(f"分页查询成功，总记录数: {data.get('TotalCount', 0)}")
                        return True
                except json.JSONDecodeError:
                    print("响应解析失败")
            
            else:
                print(f"其他状态码: {response.text}")
                
        except Exception as e:
            print(f"请求异常: {str(e)}")
        
        return False

    def _analyze_500_error(self, message):
        """分析500错误消息"""
        if not message:
            return []
            
        print("\n详细错误分析:")
        invalid_columns = []
        
        # 解析错误消息中的无效列名
        if '列名' in message and '无效' in message:
            import re
            # 使用正则表达式提取所有被单引号包围的列名
            column_matches = re.findall(r"列名 '([^']+)' 无效", message)
            invalid_columns.extend(column_matches)
            
            print(f"发现无效列名: {invalid_columns}")
            
            # 分析可能的解决方案
            for column in invalid_columns:
                print(f"  - {column}: 需要在AppDbContext中忽略或正确映射")
        
        return invalid_columns

    def run_debug_tests(self):
        """运行调试测试"""
        print("=" * 60)
        print("用户API 500错误深度调试")
        print("=" * 60)
        
        # 先登录
        if not self.login():
            print("登录失败，无法继续调试")
            return
        
        print(f"Token获取成功: {self.token[:50]}...")
        
        # 测试简单GET请求
        invalid_columns_get = self.test_users_get_simple()
        
        # 测试分页POST请求  
        invalid_columns_post = self.test_users_paged_post()
        
        # 汇总需要修复的字段
        all_invalid_columns = []
        if isinstance(invalid_columns_get, list):
            all_invalid_columns.extend(invalid_columns_get)
        if isinstance(invalid_columns_post, list):
            all_invalid_columns.extend(invalid_columns_post)
        
        # 去重
        unique_invalid_columns = list(set(all_invalid_columns))
        
        if unique_invalid_columns:
            print(f"\n=== 需要修复的字段汇总 ===")
            for column in unique_invalid_columns:
                print(f"  - {column}")
            
            print(f"\n建议修复方案:")
            print(f"1. 在AppDbContext的ConfigureUsers方法中添加:")
            for column in unique_invalid_columns:
                print(f"   entity.Ignore(u => u.{column});")
            
            print(f"\n2. 或检查UserModel和相关DTO中是否有这些字段的定义")
            
            return unique_invalid_columns
        else:
            print(f"\n=== 调试结果 ===")
            print("未发现具体的字段映射错误，可能是其他问题")
            return []

def main():
    print("等待服务器启动...")
    time.sleep(2)
    
    debugger = UserAPI500Debugger()
    invalid_columns = debugger.run_debug_tests()
    
    if invalid_columns:
        print(f"\n发现需要修复的字段: {invalid_columns}")
    else:
        print(f"\n调试完成，可能需要其他方式解决500错误")

if __name__ == "__main__":
    main()