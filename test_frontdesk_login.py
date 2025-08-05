"""
前台角色登录测试脚本
测试用户: frontdesk
密码: Front@123456
"""

import requests
import json
import time
from datetime import datetime

# API基础配置
BASE_URL = "https://localhost:7001"
HEADERS = {
    "Content-Type": "application/json"
}

# 禁用SSL警告（开发环境）
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

def test_frontdesk_login():
    """测试前台用户登录"""
    
    print("=" * 60)
    print("前台用户登录测试")
    print("=" * 60)
    
    # 1. 测试前台用户登录
    login_data = {
        "username": "frontdesk",
        "password": "Front@123456",
        "rememberMe": False
    }
    
    print(f"\n[{datetime.now().strftime('%H:%M:%S')}] 正在测试前台用户登录...")
    print(f"用户名: {login_data['username']}")
    
    try:
        response = requests.post(
            f"{BASE_URL}/api/v1/auth/login",
            json=login_data,
            headers=HEADERS,
            verify=False  # 跳过SSL验证（仅开发环境）
        )
        
        if response.status_code == 200:
            result = response.json()
            if result.get("success"):
                token = result["data"]["token"]
                user_info = result["data"]["user"]
                
                print(f"✅ 登录成功！")
                print(f"   用户名: {user_info.get('username', '')}")
                print(f"   真实姓名: {user_info.get('realName', '')}")
                print(f"   角色: {get_role_name(user_info.get('role', -1))}")
                print(f"   部门: {user_info.get('department', '')}")
                print(f"   职位: {user_info.get('position', '')}")
                print(f"\n   Token (前30字符): {token[:30]}...")
                
                return token
            else:
                print(f"❌ 登录失败: {result.get('message', '未知错误')}")
        else:
            print(f"❌ 请求失败: HTTP {response.status_code}")
            print(f"   响应内容: {response.text}")
            
    except Exception as e:
        print(f"❌ 发生错误: {str(e)}")
    
    return None

def get_role_name(role_value):
    """获取角色名称"""
    role_map = {
        0: "挂号人员",
        1: "主治医生",
        2: "收费人员",
        3: "药剂师",
        4: "理疗师",
        99: "管理员"
    }
    return role_map.get(role_value, f"未知角色({role_value})")

def test_frontdesk_permissions(token):
    """测试前台用户权限"""
    
    print("\n" + "=" * 60)
    print("前台用户权限测试")
    print("=" * 60)
    
    headers_with_auth = {
        **HEADERS,
        "Authorization": f"Bearer {token}"
    }
    
    # 测试能访问的API
    test_apis = [
        ("/api/v1/patients", "GET", "患者列表"),
        ("/api/v1/registration", "GET", "挂号列表"),
        ("/api/v1/doctors/active", "GET", "医生列表"),
    ]
    
    for api_path, method, description in test_apis:
        print(f"\n测试访问: {description} ({method} {api_path})")
        
        try:
            if method == "GET":
                response = requests.get(
                    f"{BASE_URL}{api_path}",
                    headers=headers_with_auth,
                    verify=False
                )
            
            if response.status_code == 200:
                print(f"✅ 可以访问 {description}")
            elif response.status_code == 403:
                print(f"⚠️  无权访问 {description} (403 Forbidden)")
            elif response.status_code == 401:
                print(f"⚠️  需要认证 {description} (401 Unauthorized)")
            else:
                print(f"❓ 访问 {description} 返回: HTTP {response.status_code}")
                
        except Exception as e:
            print(f"❌ 测试 {description} 时出错: {str(e)}")

def test_create_registration(token):
    """测试创建挂号"""
    
    print("\n" + "=" * 60)
    print("测试创建挂号功能")
    print("=" * 60)
    
    headers_with_auth = {
        **HEADERS,
        "Authorization": f"Bearer {token}"
    }
    
    # 模拟创建挂号数据
    registration_data = {
        "patientName": "测试患者",
        "patientPhone": "13800138888",
        "patientGender": 0,  # 男
        "patientAge": 30,
        "doctorId": "00000000-0000-0000-0000-000000000000",  # 测试用空ID
        "registrationType": 0,  # 普通号
        "visitDate": datetime.now().isoformat(),
        "complaint": "测试挂号功能",
        "remark": "前台用户测试创建"
    }
    
    print(f"\n正在创建挂号...")
    print(f"患者姓名: {registration_data['patientName']}")
    print(f"患者电话: {registration_data['patientPhone']}")
    
    try:
        response = requests.post(
            f"{BASE_URL}/api/v1/registration",
            json=registration_data,
            headers=headers_with_auth,
            verify=False
        )
        
        if response.status_code == 200:
            result = response.json()
            if result.get("success"):
                print(f"✅ 挂号创建成功！")
                print(f"   挂号ID: {result.get('data', {}).get('id', '')}")
            else:
                print(f"❌ 挂号创建失败: {result.get('message', '未知错误')}")
        elif response.status_code == 403:
            print(f"⚠️  无权创建挂号 (403 Forbidden)")
        else:
            print(f"❌ 请求失败: HTTP {response.status_code}")
            print(f"   响应内容: {response.text}")
            
    except Exception as e:
        print(f"❌ 发生错误: {str(e)}")

def main():
    """主测试函数"""
    
    print("\n" + "🏥" * 30)
    print("凌隐宝堂中医诊所 - 前台角色功能测试")
    print("🏥" * 30)
    
    # 1. 测试登录
    token = test_frontdesk_login()
    
    if token:
        # 2. 测试权限
        test_frontdesk_permissions(token)
        
        # 3. 测试创建挂号
        test_create_registration(token)
    else:
        print("\n⚠️  无法继续测试，因为登录失败")
    
    print("\n" + "=" * 60)
    print("测试完成")
    print("=" * 60)

if __name__ == "__main__":
    # 等待API服务启动
    print("等待3秒以确保API服务已启动...")
    time.sleep(3)
    
    main()