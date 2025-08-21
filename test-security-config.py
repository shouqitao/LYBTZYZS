#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LYBT 系统安全配置验证工具
UltraThink v2.0 Security Enhancement
"""

import os
import json
import requests
from requests.packages.urllib3.exceptions import InsecureRequestWarning
import sys

# 禁用SSL警告
requests.packages.urllib3.disable_warnings(InsecureRequestWarning)

def test_env_variables():
    """测试环境变量是否正确加载"""
    print("测试环境变量状态:")
    
    env_vars = [
        'JWT_SECRET',
        'ADMIN_DEFAULT_PASSWORD', 
        'USER_DEFAULT_PASSWORD',
        'ASPNETCORE_ENVIRONMENT'
    ]
    
    for var in env_vars:
        value = os.environ.get(var)
        if value:
            # 遮蔽敏感信息
            if 'password' in var.lower() or 'secret' in var.lower():
                display_value = f"{value[:4]}***{value[-4:]}" if len(value) > 8 else "****"
            else:
                display_value = value
            print(f"  ✅ {var}: {display_value}")
        else:
            print(f"  ❌ {var}: 未设置")
    print()

def load_dotenv_file():
    """手动加载.env文件"""
    env_file = '.env'
    if not os.path.exists(env_file):
        print(f"❌ {env_file} 文件不存在")
        return False
        
    print(f"📋 加载 {env_file} 文件:")
    env_vars = {}
    
    with open(env_file, 'r', encoding='utf-8') as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            if not line or line.startswith('#'):
                continue
                
            if '=' in line:
                key, value = line.split('=', 1)
                key = key.strip()
                value = value.strip().strip('"').strip("'")
                env_vars[key] = value
                
                # 设置到系统环境变量
                os.environ[key] = value
                
                # 显示加载状态
                if 'password' in key.lower() or 'secret' in key.lower():
                    display_value = f"{value[:4]}***{value[-4:]}" if len(value) > 8 else "****"
                else:
                    display_value = value
                print(f"  第{line_num}行: {key} = {display_value}")
    
    print(f"✅ 成功加载 {len(env_vars)} 个环境变量")
    return True

def test_api_endpoint():
    """测试API端点是否正常工作"""
    print("🌐 测试API端点:")
    
    try:
        # 测试健康检查端点
        response = requests.get('https://localhost:7001/health', verify=False, timeout=5)
        if response.status_code == 200:
            print("  ✅ Health端点响应正常")
        else:
            print(f"  ⚠️ Health端点响应异常: {response.status_code}")
    except requests.exceptions.RequestException as e:
        print(f"  ❌ 无法连接到API: {e}")
    
    try:
        # 测试Swagger端点
        response = requests.get('https://localhost:7001/swagger/index.html', verify=False, timeout=5)
        if response.status_code == 200:
            print("  ✅ Swagger端点响应正常")
        else:
            print(f"  ⚠️ Swagger端点响应异常: {response.status_code}")
    except requests.exceptions.RequestException as e:
        print(f"  ❌ 无法连接到Swagger: {e}")
    print()

def test_jwt_configuration():
    """测试JWT配置"""
    print("🔐 测试JWT配置:")
    
    jwt_secret = os.environ.get('JWT_SECRET')
    if jwt_secret:
        print(f"  ✅ JWT_SECRET长度: {len(jwt_secret)} 字符")
        
        # 检查强度
        has_upper = any(c.isupper() for c in jwt_secret)
        has_lower = any(c.islower() for c in jwt_secret)
        has_digit = any(c.isdigit() for c in jwt_secret)
        has_special = any(not c.isalnum() for c in jwt_secret)
        
        strength_score = sum([has_upper, has_lower, has_digit, has_special])
        if len(jwt_secret) >= 32 and strength_score >= 3:
            print("  ✅ JWT密钥强度: 强")
        elif len(jwt_secret) >= 16 and strength_score >= 2:
            print("  ⚠️ JWT密钥强度: 中等")
        else:
            print("  ❌ JWT密钥强度: 弱")
    else:
        print("  ❌ JWT_SECRET未设置")
    print()

def test_password_policy():
    """测试密码策略"""
    print("🔒 测试密码策略:")
    
    admin_password = os.environ.get('ADMIN_DEFAULT_PASSWORD')
    user_password = os.environ.get('USER_DEFAULT_PASSWORD')
    
    passwords = [
        ('管理员密码', admin_password),
        ('用户密码', user_password)
    ]
    
    for name, password in passwords:
        if password:
            # 检查密码强度
            length_ok = len(password) >= 12
            has_upper = any(c.isupper() for c in password)
            has_lower = any(c.islower() for c in password)
            has_digit = any(c.isdigit() for c in password)
            has_special = any(not c.isalnum() for c in password)
            
            strength_checks = [
                ('长度≥12字符', length_ok),
                ('包含大写字母', has_upper),
                ('包含小写字母', has_lower),
                ('包含数字', has_digit),
                ('包含特殊字符', has_special)
            ]
            
            passed_checks = sum(check[1] for check in strength_checks)
            
            print(f"  {name} (长度{len(password)}字符):")
            for check_name, passed in strength_checks:
                status = "✅" if passed else "❌"
                print(f"    {status} {check_name}")
            
            if passed_checks >= 4:
                print(f"    ✅ 密码强度: 强 ({passed_checks}/5)")
            elif passed_checks >= 3:
                print(f"    ⚠️ 密码强度: 中等 ({passed_checks}/5)")
            else:
                print(f"    ❌ 密码强度: 弱 ({passed_checks}/5)")
        else:
            print(f"  ❌ {name}未设置")
    print()

def test_login_with_env_credentials():
    """使用环境变量中的凭据测试登录"""
    print("🚪 测试登录功能:")
    
    admin_password = os.environ.get('ADMIN_DEFAULT_PASSWORD')
    if not admin_password:
        print("  ❌ ADMIN_DEFAULT_PASSWORD未设置，跳过登录测试")
        return
        
    try:
        login_data = {
            'username': 'sysadmin',
            'password': admin_password,
            'rememberMe': False
        }
        
        response = requests.post(
            'https://localhost:7001/api/v1/auth/login',
            json=login_data,
            verify=False,
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            if data.get('success'):
                print("  ✅ 使用.env文件中的管理员密码登录成功")
                token = data.get('data', {}).get('token', '')
                if token:
                    print(f"  ✅ 获取JWT令牌成功 (长度: {len(token)} 字符)")
                else:
                    print("  ⚠️ 未获取到JWT令牌")
            else:
                print(f"  ❌ 登录失败: {data.get('message', '未知错误')}")
        else:
            print(f"  ❌ 登录请求失败: HTTP {response.status_code}")
            
    except requests.exceptions.RequestException as e:
        print(f"  ❌ 登录测试异常: {e}")
    print()

def main():
    """主函数"""
    print("LYBT 系统安全配置验证工具")
    print("=" * 50)
    
    # 1. 加载.env文件
    load_dotenv_file()
    print()
    
    # 2. 测试环境变量
    test_env_variables()
    
    # 3. 测试JWT配置
    test_jwt_configuration()
    
    # 4. 测试密码策略
    test_password_policy()
    
    # 5. 测试API端点
    test_api_endpoint()
    
    # 6. 测试登录功能
    test_login_with_env_credentials()
    
    print("🏁 安全配置验证完成")

if __name__ == '__main__':
    main()