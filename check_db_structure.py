#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
检查数据库实际表结构
"""

import sys
import os
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import requests
import json
import time
from config.parameter_memory import get_parameter

BASE_URL = "http://localhost:5297"

def check_database_structure():
    """检查数据库结构"""
    print("=== 检查数据库表结构 ===")
    
    # 登录获取token
    api_version = get_parameter("apiVersion", "1.0")
    login_data = {
        "username": "sysadmin",
        "password": "Admin@123456",
        "rememberMe": True
    }
    
    session = requests.Session()
    try:
        response = session.post(
            f"{BASE_URL}/api/v{api_version}/Auth/login",
            json=login_data,
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        if response.status_code != 200:
            print(f"登录失败: {response.status_code}")
            return
            
        result = response.json()
        if not result.get('success'):
            print("登录失败")
            return
            
        token = result['data']['token']
        print("登录成功")
        
        # 检查调试接口
        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}"
        }
        
        # 检查Users表结构
        debug_response = session.get(
            f"{BASE_URL}/api/v{api_version}/Debug/users-table-info",
            headers=headers,
            timeout=10
        )
        
        print(f"调试接口状态码: {debug_response.status_code}")
        if debug_response.status_code == 200:
            debug_result = debug_response.json()
            if debug_result.get('success'):
                table_info = debug_result.get('data', {})
                print("Users表结构信息:")
                print(f"  表名: {table_info.get('tableName', 'N/A')}")
                
                columns = table_info.get('columns', [])
                if columns:
                    print(f"  列数: {len(columns)}")
                    print("  列信息:")
                    for col in columns:
                        print(f"    - {col.get('name', 'N/A')} ({col.get('type', 'N/A')})")
                else:
                    print("  无列信息")
                    
                # 检查是否存在问题字段
                problem_fields = ['Department', 'Position', 'Remark', 'UpdateTime']
                existing_columns = [col.get('name', '') for col in columns]
                
                print("\n问题字段分析:")
                for field in problem_fields:
                    if field in existing_columns:
                        print(f"  ✓ {field} - 存在于数据库中")
                    else:
                        print(f"  ✗ {field} - 不存在于数据库中 (应该被忽略)")
                        
            else:
                print(f"调试接口返回失败: {debug_result.get('message', 'N/A')}")
        else:
            print(f"调试接口调用失败: {debug_response.text}")
            
    except Exception as e:
        print(f"检查数据库结构失败: {str(e)}")

def main():
    print("等待服务器启动...")
    time.sleep(2)
    check_database_structure()

if __name__ == "__main__":
    main()