#!/usr/bin/env python3
"""
Backend P3-Fix Batch1: 修复后的DTO绑定验证
使用正确的JSON格式（首字母大写）测试三个创建端点
"""

import requests
import json
import time

# 服务器配置
BASE_URL = "http://localhost:8080"  # 使用正确的端口
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzZTRhZmNjZC0yOGU2LTQ4ZDEtOGJlZC1hMTBmNzQzNzFkOWQiLCJ1bmlxdWVfbmFtZSI6InN5c2FkbWluIiwiQWRtaW4iOiJBZG1pbiIsIm5iZiI6MTcyNjM1MjY0MiwiZXhwIjoxNzI2MzgxNDQyLCJpYXQiOjE3MjYzNTI2NDIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMSIsImF1ZCI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMSJ9.w-hX4A_1PrXhfyGXEt7YVKXhppVdPL4n97GGI-PXGWw"

HEADERS = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

def test_patients_create_fixed():
    """测试患者创建API - 使用正确的JSON格式（首字母大写）"""
    print("✅ 测试患者创建API - 修复格式")
    
    # 正确格式：属性名首字母大写，匹配PatientCreateDto定义
    payload = {
        "Name": "测试患者-修复格式",  # 首字母大写
        "Gender": 1,               # 首字母大写 
        "Age": 35,                 # 首字母大写
        "PhoneNumber": "13800138001",  # 首字母大写
        "BirthDate": "1989-01-01T00:00:00",  # 可选，但格式正确
        "Status": 1                # CommonStatus.Enabled = 1
    }
    
    url = f"{BASE_URL}/api/v1/patients"
    
    print(f"请求URL: {url}")
    print(f"请求数据: {json.dumps(payload, ensure_ascii=False, indent=2)}")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"状态码: {response.status_code}")
        print(f"响应头: {dict(response.headers)}")
        print(f"响应内容: {response.text}")
        
        if response.status_code in [200, 201]:
            print("✅ 患者创建成功！")
            return True
        else:
            print("❌ 患者创建失败")
            return False
            
    except Exception as e:
        print(f"❌ 请求异常: {e}")
        return False

def test_users_create_fixed():
    """测试用户创建API - 使用正确的JSON格式"""
    print("\n✅ 测试用户创建API - 修复格式")
    
    # 生成唯一用户名
    timestamp = int(time.time())
    
    # 正确格式：属性名首字母大写，匹配UserMutationDto定义
    payload = {
        "Username": f"testuser{timestamp}",  # 首字母大写，唯一值
        "Password": "TestPass123!",          # 首字母大写
        "ConfirmPassword": "TestPass123!",   # 首字母大写
        "RealName": "测试用户-修复",           # 首字母大写
        "Role": "Doctor",                    # 首字母大写
        "PhoneNumber": "13800138002",        # 可选
        "Email": f"test{timestamp}@lybt.com", # 可选
        "Status": 1                          # CommonStatus.Enabled = 1
    }
    
    url = f"{BASE_URL}/api/v1/users"
    
    print(f"请求URL: {url}")
    print(f"请求数据: {json.dumps(payload, ensure_ascii=False, indent=2)}")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"状态码: {response.status_code}")
        print(f"响应内容: {response.text}")
        
        if response.status_code in [200, 201]:
            print("✅ 用户创建成功！")
            return True
        else:
            print("❌ 用户创建失败")
            return False
            
    except Exception as e:
        print(f"❌ 请求异常: {e}")
        return False

def test_consultation_start_fixed():
    """测试看诊开始API - 使用正确的JSON格式"""
    print("\n✅ 测试看诊开始API - 修复格式")
    
    # 正确格式：属性名首字母大写，匹配ConsultationStartDto定义
    payload = {
        "MedicalCaseId": "11111111-1111-1111-1111-111111111111",  # 首字母大写，测试GUID
        "PatientId": "22222222-2222-2222-2222-222222222222",      # 首字母大写，测试GUID
        "DoctorId": "3e4afccd-28e6-48d1-8bed-a10f74371d9d",       # 首字母大写，真实admin GUID
        "EstimatedDuration": 30,        # 首字母大写
        "ConsultationType": "初诊",     # 首字母大写，可选
        "InitialComplaint": "测试主诉-修复格式",  # 首字母大写，可选
        "Remark": "UAT测试数据"         # 首字母大写，可选
    }
    
    url = f"{BASE_URL}/api/v1/consultations/start"
    
    print(f"请求URL: {url}")
    print(f"请求数据: {json.dumps(payload, ensure_ascii=False, indent=2)}")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"状态码: {response.status_code}")
        print(f"响应内容: {response.text}")
        
        if response.status_code in [200, 201]:
            print("✅ 看诊开始成功！")
            return True
        else:
            print("❌ 看诊开始失败")
            return False
            
    except Exception as e:
        print(f"❌ 请求异常: {e}")
        return False

def check_server_health():
    """检查服务器健康状态"""
    try:
        response = requests.get(f"{BASE_URL}/api/health", timeout=5)
        print(f"服务器健康检查: {response.status_code}")
        if response.status_code == 200:
            return True
    except:
        pass
    
    # 尝试其他可能的健康检查端点
    try:
        response = requests.get(f"{BASE_URL}/api/v1/health", timeout=5)
        print(f"服务器健康检查(v1): {response.status_code}")
        if response.status_code == 200:
            return True
    except:
        pass
    
    return False

if __name__ == "__main__":
    print("Backend P3-Fix Batch1: 修复后的DTO绑定验证测试")
    print("=" * 80)
    
    # 检查服务器状态
    if not check_server_health():
        print("❌ 服务器无法访问，请检查服务是否启动")
        print(f"预期服务地址: {BASE_URL}")
        exit(1)
    
    print("✅ 服务器连接正常")
    print("-" * 60)
    
    # 执行三个修复测试
    results = []
    results.append(test_patients_create_fixed())
    results.append(test_users_create_fixed())  
    results.append(test_consultation_start_fixed())
    
    print("\n" + "=" * 80)
    print("📊 测试结果汇总:")
    print(f"患者创建: {'✅ 成功' if results[0] else '❌ 失败'}")
    print(f"用户创建: {'✅ 成功' if results[1] else '❌ 失败'}")
    print(f"看诊开始: {'✅ 成功' if results[2] else '❌ 失败'}")
    
    success_count = sum(results)
    print(f"\n总体结果: {success_count}/3 个端点测试通过")
    
    if success_count == 3:
        print("🎉 所有创建端点修复成功！DTO绑定问题已解决")
    else:
        print("⚠️  部分端点仍有问题，需要进一步分析")
    
    print("\n修复方案验证完成")
    print("=" * 80)