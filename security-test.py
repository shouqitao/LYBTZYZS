#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import json
import requests
from requests.packages.urllib3.exceptions import InsecureRequestWarning

# 禁用SSL警告
requests.packages.urllib3.disable_warnings(InsecureRequestWarning)

def load_env_file():
    """加载.env文件"""
    env_file = '.env'
    if not os.path.exists(env_file):
        print(f"错误: {env_file} 文件不存在")
        return False
        
    print(f"加载 {env_file} 文件:")
    with open(env_file, 'r', encoding='utf-8') as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            if not line or line.startswith('#'):
                continue
                
            if '=' in line:
                key, value = line.split('=', 1)
                key = key.strip()
                value = value.strip().strip('"').strip("'")
                os.environ[key] = value
                
                # 显示关键变量
                if key in ['JWT_SECRET', 'ADMIN_DEFAULT_PASSWORD', 'USER_DEFAULT_PASSWORD']:
                    if 'password' in key.lower() or 'secret' in key.lower():
                        display_value = f"{value[:4]}***{value[-4:]}" if len(value) > 8 else "****"
                    else:
                        display_value = value
                    print(f"  {key} = {display_value} (长度: {len(value)})")
    return True

def test_login():
    """测试登录"""
    admin_password = os.environ.get('ADMIN_DEFAULT_PASSWORD')
    if not admin_password:
        print("ADMIN_DEFAULT_PASSWORD未设置")
        return
        
    login_data = {
        'username': 'sysadmin',
        'password': admin_password,
        'rememberMe': False
    }
    
    try:
        response = requests.post(
            'https://localhost:7001/api/v1/auth/login',
            json=login_data,
            verify=False,
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            if data.get('success'):
                print("登录成功!")
                token = data.get('data', {}).get('token', '')
                print(f"JWT令牌长度: {len(token)} 字符")
            else:
                print(f"登录失败: {data.get('message', '未知错误')}")
        else:
            print(f"HTTP错误: {response.status_code}")
            print(f"响应: {response.text}")
            
    except Exception as e:
        print(f"请求异常: {e}")

def main():
    print("LYBT 安全配置验证")
    print("-" * 30)
    
    # 加载环境变量
    load_env_file()
    print()
    
    # 显示关键配置
    print("关键环境变量:")
    vars_to_check = [
        'JWT_SECRET', 
        'ADMIN_DEFAULT_PASSWORD', 
        'USER_DEFAULT_PASSWORD',
        'JwtOptions__Secret',
        'SysAdminOptions__DefaultPassword',
        'UserOptions__DefaultUserPassword'
    ]
    for var in vars_to_check:
        value = os.environ.get(var)
        if value:
            print(f"  {var}: 已设置 (长度: {len(value)})")
        else:
            print(f"  {var}: 未设置")
    print()
    
    # 测试登录
    print("测试登录:")
    test_login()

if __name__ == '__main__':
    main()