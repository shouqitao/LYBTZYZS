#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
调试EF Core查询生成
"""

import requests
import json
import time

BASE_URL = "http://localhost:5298"

def test_debug_queries():
    """测试不同的查询方式"""
    print("=== 调试EF Core查询生成 ===")
    
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
        print("登录成功")
        
        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}"
        }
        
        # 2. 测试不同的Users API endpoint
        endpoints_to_test = [
            "/api/v1.0/Users",
            "/api/v1.0/Users?pageIndex=1&pageSize=10",
            "/api/v1.0/Users/simple",  # 如果存在简化版本
        ]
        
        for endpoint in endpoints_to_test:
            print(f"\n2. 测试 {endpoint}...")
            
            try:
                user_response = session.get(
                    f"{BASE_URL}{endpoint}",
                    headers=headers,
                    timeout=10
                )
                
                print(f"状态码: {user_response.status_code}")
                
                if user_response.status_code == 500:
                    result = user_response.json()
                    error_msg = result.get('message', '')
                    print(f"500错误: {error_msg}")
                    
                    # 分析具体是哪些字段导致问题
                    if 'Department' in error_msg:
                        print("- Department字段映射问题")
                    if 'Position' in error_msg:
                        print("- Position字段映射问题")
                    if 'Remark' in error_msg:
                        print("- Remark字段映射问题")
                    if 'UpdateTime' in error_msg:
                        print("- UpdateTime字段映射问题")
                        
                elif user_response.status_code == 200:
                    print("✅ 成功!")
                    result = user_response.json()
                    if result.get('success'):
                        data = result.get('data', {})
                        if isinstance(data, dict) and 'items' in data:
                            print(f"返回 {len(data['items'])} 条用户记录")
                        elif isinstance(data, list):
                            print(f"返回 {len(data)} 条用户记录")
                        else:
                            print("返回数据结构:", type(data))
                else:
                    print(f"其他状态码 {user_response.status_code}: {user_response.text}")
                    
            except requests.exceptions.RequestException as e:
                print(f"请求异常: {str(e)}")
            except Exception as e:
                print(f"其他异常: {str(e)}")
            
    except requests.exceptions.ConnectionError:
        print("无法连接到服务器，可能服务器未启动")
    except Exception as e:
        print(f"测试失败: {str(e)}")

def main():
    print("等待服务器启动...")
    time.sleep(3)
    test_debug_queries()

if __name__ == "__main__":
    main()