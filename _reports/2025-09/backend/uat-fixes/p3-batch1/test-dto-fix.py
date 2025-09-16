#!/usr/bin/env python3
"""
Backend P3-Fix Batch1: DTO绑定修复验证脚本
目标：测试不同的请求格式以找出正确的DTO绑定方式
"""

import requests
import json
import time

BASE_URL = "http://localhost:5001"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzZTRhZmNjZC0yOGU2LTQ4ZDEtOGJlZC1hMTBmNzQzNzFkOWQiLCJ1bmlxdWVfbmFtZSI6InN5c2FkbWluIiwiQWRtaW4iOiJBZG1pbiIsIm5iZiI6MTcyNjM1MjY0MiwiZXhwIjoxNzI2MzgxNDQyLCJpYXQiOjE3MjYzNTI2NDIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMSIsImF1ZCI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMSJ9.w-hX4A_1PrXhfyGXEt7YVKXhppVdPL4n97GGI-PXGWw"

HEADERS = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

def test_patients_create():
    """测试患者创建API的不同请求格式"""
    print("🧪 测试患者创建API")
    
    # 格式1：直接DTO格式
    payload1 = {
        "name": "测试患者-直接格式",
        "gender": 1,
        "age": 35,
        "phoneNumber": "13800138001"
    }
    
    # 格式2：嵌套dto格式
    payload2 = {
        "dto": {
            "name": "测试患者-嵌套格式",
            "gender": 1,
            "age": 35,
            "phoneNumber": "13800138002"
        }
    }
    
    # 格式3：使用正确的字段名（首字母大写）
    payload3 = {
        "Name": "测试患者-大写格式",
        "Gender": 1,
        "Age": 35,
        "PhoneNumber": "13800138003"
    }
    
    url = f"{BASE_URL}/api/v1/patients"
    
    for i, payload in enumerate([payload1, payload2, payload3], 1):
        print(f"\n格式{i}: {json.dumps(payload, ensure_ascii=False)}")
        try:
            response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
            print(f"状态码: {response.status_code}")
            print(f"响应: {response.text}")
        except Exception as e:
            print(f"请求失败: {e}")
        print("-" * 60)

def test_users_create():
    """测试用户创建API"""
    print("\n🧪 测试用户创建API")
    
    payload = {
        "Username": f"testuser{int(time.time())}",
        "RealName": "测试用户",
        "Password": "TestPass123!",
        "ConfirmPassword": "TestPass123!",
        "Role": "Doctor"
    }
    
    url = f"{BASE_URL}/api/v1/users"
    
    print(f"请求: {json.dumps(payload, ensure_ascii=False)}")
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"状态码: {response.status_code}")
        print(f"响应: {response.text}")
    except Exception as e:
        print(f"请求失败: {e}")

def test_consultation_start():
    """测试看诊开始API"""
    print("\n🧪 测试看诊开始API")
    
    payload = {
        "MedicalCaseId": "11111111-1111-1111-1111-111111111111",
        "PatientId": "22222222-2222-2222-2222-222222222222", 
        "DoctorId": "3e4afccd-28e6-48d1-8bed-a10f74371d9d",
        "EstimatedDuration": 30,
        "ConsultationType": "初诊",
        "InitialComplaint": "测试主诉"
    }
    
    url = f"{BASE_URL}/api/v1/consultations/start"
    
    print(f"请求: {json.dumps(payload, ensure_ascii=False)}")
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"状态码: {response.status_code}")
        print(f"响应: {response.text}")
    except Exception as e:
        print(f"请求失败: {e}")

if __name__ == "__main__":
    print("Backend P3-Fix Batch1: DTO绑定修复验证测试")
    print("=" * 80)
    
    # 测试健康检查
    try:
        response = requests.get(f"{BASE_URL}/api/health", timeout=5)
        print(f"✅ 服务器健康状态: {response.status_code}")
    except:
        print("❌ 服务器无法访问")
        exit(1)
    
    # 依次测试三个端点
    test_patients_create()
    test_users_create()
    test_consultation_start()
    
    print("\n" + "=" * 80)
    print("测试完成 - 分析结果以确定正确的DTO格式")