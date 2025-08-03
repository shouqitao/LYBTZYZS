#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
按模块测试API接口脚本
"""

import requests
import json
import datetime
from typing import Dict, List, Tuple, Any
import time

# 服务器配置
BASE_URL = "http://192.168.190.243:5000"
API_PREFIX = "/api/v1.0"

# JWT 令牌存储
jwt_token = None

# API 模块和接口定义
API_MODULES = {
    "Auth": {
        "endpoints": [
            {"path": "/Auth/Login", "method": "POST", "auth": False, "data": {"username": "sysadmin", "password": "Admin@123456"}},
            {"path": "/Auth/Logout", "method": "POST", "auth": True},
            {"path": "/Auth/RefreshToken", "method": "POST", "auth": True},
            {"path": "/Auth/ChangePassword", "method": "POST", "auth": True, "data": {"oldPassword": "Admin@123456", "newPassword": "Admin@123456"}}
        ]
    },
    "Users": {
        "endpoints": [
            {"path": "/Users", "method": "GET", "auth": True},
            {"path": "/Users/{id}", "method": "GET", "auth": True, "params": {"id": "f217f17e-7b51-43b7-81b2-138188cb84cd"}},
            {"path": "/Users", "method": "POST", "auth": True, "data": {
                "userName": "testuser",
                "password": "Admin@123456",
                "realName": "测试用户",
                "phoneNumber": "13800138000",
                "email": "test@example.com",
                "role": "Doctor"
            }},
            {"path": "/Users/{id}", "method": "PUT", "auth": True, "params": {"id": "f217f17e-7b51-43b7-81b2-138188cb84cd"}, "data": {
                "id": "f217f17e-7b51-43b7-81b2-138188cb84cd",
                "userName": "sysadmin",
                "realName": "系统管理员",
                "phoneNumber": "13800138001",
                "email": "admin@example.com"
            }},
            {"path": "/Users/{id}", "method": "DELETE", "auth": True, "params": {"id": "99999999-7b51-43b7-81b2-138188cb84cd"}}
        ]
    },
    "Patients": {
        "endpoints": [
            {"path": "/Patients", "method": "GET", "auth": True},
            {"path": "/Patients/{id}", "method": "GET", "auth": True, "params": {"id": "b9afacda-a33f-474d-9c02-852132fa45b9"}},
            {"path": "/Patients", "method": "POST", "auth": True, "data": {
                "name": "测试患者",
                "gender": 1,  # 使用枚举值：1=男，2=女，0=未知
                "age": 30,
                "phoneNumber": "13900139000",
                "address": "测试地址",
                "allergyHistory": "无"
            }},
            {"path": "/Patients/{id}", "method": "PUT", "auth": True, "params": {"id": "b9afacda-a33f-474d-9c02-852132fa45b9"}, "data": {
                "id": "b9afacda-a33f-474d-9c02-852132fa45b9",
                "name": "更新患者",
                "gender": 1,  # 使用枚举值：1=男，2=女，0=未知
                "age": 31,
                "phoneNumber": "13900139001",
                "address": "新地址"
            }},
            {"path": "/Patients/{id}", "method": "DELETE", "auth": True, "params": {"id": "99999999-a33f-474d-9c02-852132fa45b9"}}
        ]
    },
    "Doctors": {
        "endpoints": [
            {"path": "/Doctors", "method": "GET", "auth": True},
            {"path": "/Doctors/{id}", "method": "GET", "auth": True, "params": {"id": "c9afacda-a33f-474d-9c02-852132fa45b9"}},
            {"path": "/Doctors", "method": "POST", "auth": True, "data": {
                "name": "测试医生",
                "gender": 1,  # 使用枚举值：1=男，2=女，0=未知
                "phoneNumber": "13700137000",
                "department": "中医内科",
                "title": "主治医师",
                "specialties": "内科疾病"
            }},
            {"path": "/Doctors/{id}", "method": "PUT", "auth": True, "params": {"id": "c9afacda-a33f-474d-9c02-852132fa45b9"}, "data": {
                "id": "c9afacda-a33f-474d-9c02-852132fa45b9",
                "name": "更新医生",
                "department": "中医内科",
                "title": "副主任医师"
            }},
            {"path": "/Doctors/{id}", "method": "DELETE", "auth": True, "params": {"id": "99999999-a33f-474d-9c02-852132fa45b9"}}
        ]
    },
    "Registration": {
        "endpoints": [
            {"path": "/Registration", "method": "GET", "auth": True},
            {"path": "/Registration/{id}", "method": "GET", "auth": True, "params": {"id": "d9afacda-a33f-474d-9c02-852132fa45b9"}},
            {"path": "/Registration", "method": "POST", "auth": True, "data": {
                "patientId": "b9afacda-a33f-474d-9c02-852132fa45b9",
                "doctorId": "c9afacda-a33f-474d-9c02-852132fa45b9",
                "appointmentDate": datetime.datetime.now().strftime("%Y-%m-%d"),
                "appointmentTime": "09:00",
                "reason": "感冒发烧"
            }},
            {"path": "/Registration/{id}", "method": "PUT", "auth": True, "params": {"id": "d9afacda-a33f-474d-9c02-852132fa45b9"}, "data": {
                "id": "d9afacda-a33f-474d-9c02-852132fa45b9",
                "status": "已就诊"
            }},
            {"path": "/Registration/{id}", "method": "DELETE", "auth": True, "params": {"id": "99999999-a33f-474d-9c02-852132fa45b9"}}
        ]
    },
    "Queueing": {
        "endpoints": [
            {"path": "/Queueing", "method": "GET", "auth": True},
            {"path": "/Queueing/Current", "method": "GET", "auth": True},
            {"path": "/Queueing/Next", "method": "POST", "auth": True},
            {"path": "/Queueing/Call/{id}", "method": "POST", "auth": True, "params": {"id": "e9afacda-a33f-474d-9c02-852132fa45b9"}},
            {"path": "/Queueing/Skip/{id}", "method": "POST", "auth": True, "params": {"id": "e9afacda-a33f-474d-9c02-852132fa45b9"}}
        ]
    },
    "DiagnosisTreatment": {
        "endpoints": [
            {"path": "/DiagnosisTreatment", "method": "GET", "auth": True},
            {"path": "/DiagnosisTreatment/{id}", "method": "GET", "auth": True, "params": {"id": "f9afacda-a33f-474d-9c02-852132fa45b9"}},
            {"path": "/DiagnosisTreatment", "method": "POST", "auth": True, "data": {
                "patientId": "b9afacda-a33f-474d-9c02-852132fa45b9",
                "doctorId": "c9afacda-a33f-474d-9c02-852132fa45b9",
                "diagnosisDate": datetime.datetime.now().strftime("%Y-%m-%d"),
                "chiefComplaint": "咳嗽三天",
                "diagnosis": "风寒感冒",
                "treatment": "疏风散寒"
            }},
            {"path": "/DiagnosisTreatment/{id}", "method": "PUT", "auth": True, "params": {"id": "f9afacda-a33f-474d-9c02-852132fa45b9"}, "data": {
                "id": "f9afacda-a33f-474d-9c02-852132fa45b9",
                "diagnosis": "风寒感冒",
                "treatment": "疏风散寒，宣肺止咳"
            }},
            {"path": "/DiagnosisTreatment/{id}", "method": "DELETE", "auth": True, "params": {"id": "99999999-a33f-474d-9c02-852132fa45b9"}}
        ]
    },
    "Prescriptions": {
        "endpoints": [
            {"path": "/Prescriptions", "method": "GET", "auth": True},
            {"path": "/Prescriptions/{id}", "method": "GET", "auth": True, "params": {"id": "a9afacda-a33f-474d-9c02-852132fa45b9"}},
            {"path": "/Prescriptions", "method": "POST", "auth": True, "data": {
                "patientId": "b9afacda-a33f-474d-9c02-852132fa45b9",
                "doctorId": "c9afacda-a33f-474d-9c02-852132fa45b9",
                "diagnosisId": "f9afacda-a33f-474d-9c02-852132fa45b9",
                "prescriptionDate": datetime.datetime.now().strftime("%Y-%m-%d"),
                "items": [
                    {"herbId": "b9afacda-a33f-474d-9c02-852132fa45b9", "quantity": 10, "unit": "克"},
                    {"herbId": "b9afacda-a33f-474d-9c02-852132fa45b9", "quantity": 15, "unit": "克"}
                ]
            }},
            {"path": "/Prescriptions/{id}", "method": "PUT", "auth": True, "params": {"id": "a9afacda-a33f-474d-9c02-852132fa45b9"}, "data": {
                "id": "a9afacda-a33f-474d-9c02-852132fa45b9",
                "status": "已配药"
            }},
            {"path": "/Prescriptions/{id}", "method": "DELETE", "auth": True, "params": {"id": "99999999-a33f-474d-9c02-852132fa45b9"}}
        ]
    },
    "Herbs": {
        "endpoints": [
            {"path": "/Herbs", "method": "GET", "auth": True},
            {"path": "/Herbs/{id}", "method": "GET", "auth": True, "params": {"id": "b9afacda-a33f-474d-9c02-852132fa45b9"}},
            {"path": "/Herbs", "method": "POST", "auth": True, "data": {
                "name": "麻黄测试",
                "pinyin": "MaHuangTest",
                "category": "解表药",
                "efficacy": "发汗解表，宣肺平喘",
                "usage": "3-10克",
                "stock": 1000,
                "unit": "克",
                "price": 0.5
            }},
            {"path": "/Herbs/{id}", "method": "PUT", "auth": True, "params": {"id": "b9afacda-a33f-474d-9c02-852132fa45b9"}, "data": {
                "id": "b9afacda-a33f-474d-9c02-852132fa45b9",
                "name": "麻黄",
                "stock": 900,
                "price": 0.6
            }},
            {"path": "/Herbs/{id}", "method": "DELETE", "auth": True, "params": {"id": "99999999-a33f-474d-9c02-852132fa45b9"}}
        ]
    }
}

def login() -> bool:
    """执行登录获取JWT令牌"""
    global jwt_token
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/Login",
            json={"username": "sysadmin", "password": "Admin@123456"},
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        if response.status_code == 200:
            data = response.json()
            if data.get("success") and data.get("data") and data["data"].get("token"):
                jwt_token = data["data"]["token"]
                print(f"[OK] 登录成功，获取到JWT令牌")
                return True
            else:
                print(f"[FAIL] 登录失败: {data.get('message', '未知错误')}")
                return False
        print(f"[FAIL] 登录失败: {response.status_code} - {response.text}")
    except Exception as e:
        print(f"[ERROR] 登录异常: {str(e)}")
    return False

def build_url(endpoint: Dict[str, Any]) -> str:
    """构建完整的URL"""
    path = endpoint["path"]
    if "params" in endpoint:
        for key, value in endpoint["params"].items():
            path = path.replace(f"{{{key}}}", str(value))
    return f"{BASE_URL}{API_PREFIX}{path}"

def test_endpoint(module: str, endpoint: Dict[str, Any]) -> Tuple[bool, str, str]:
    """测试单个接口，返回成功状态、错误信息和响应内容"""
    url = build_url(endpoint)
    method = endpoint["method"]
    auth = endpoint.get("auth", True)
    
    headers = {"Content-Type": "application/json"}
    if auth and jwt_token:
        headers["Authorization"] = f"Bearer {jwt_token}"
    
    try:
        data = endpoint.get("data")
        response = None
        
        if method == "GET":
            response = requests.get(url, headers=headers, timeout=10)
        elif method == "POST":
            response = requests.post(url, json=data, headers=headers, timeout=10)
        elif method == "PUT":
            response = requests.put(url, json=data, headers=headers, timeout=10)
        elif method == "DELETE":
            response = requests.delete(url, headers=headers, timeout=10)
        
        if response and response.status_code in [200, 201, 204]:
            return True, "", response.text[:200] + "..." if len(response.text) > 200 else response.text
        else:
            error_msg = f"状态码: {response.status_code}"
            try:
                error_data = response.json()
                if "message" in error_data:
                    error_msg = error_data["message"]
                elif "errors" in error_data:
                    error_msg = str(error_data["errors"])
            except:
                pass
            return False, error_msg, response.text[:200] + "..." if len(response.text) > 200 else response.text
            
    except requests.exceptions.ConnectionError:
        return False, "连接失败", ""
    except requests.exceptions.Timeout:
        return False, "请求超时", ""
    except Exception as e:
        return False, str(e), ""

def test_module(module_name: str, module_config: Dict):
    """测试单个模块的所有接口"""
    print(f"\n{'='*60}")
    print(f"测试模块: {module_name}")
    print(f"{'='*60}")
    
    success_count = 0
    total_count = len(module_config["endpoints"])
    
    for i, endpoint in enumerate(module_config["endpoints"], 1):
        path = endpoint["path"]
        method = endpoint["method"]
        
        print(f"\n[{i}/{total_count}] {method} {path}")
        print("-" * 40)
        
        # 执行测试
        success, error, response_content = test_endpoint(module_name, endpoint)
        
        if success:
            print(f"[OK] 状态: 成功")
            success_count += 1
            if response_content:
                try:
                    # 尝试格式化JSON响应
                    json_data = json.loads(response_content)
                    print(f"[RESPONSE] 响应: {json.dumps(json_data, ensure_ascii=False, indent=2)[:300]}...")
                except:
                    print(f"[RESPONSE] 响应: {response_content}")
        else:
            print(f"[FAIL] 状态: 失败")
            print(f"[ERROR] 错误: {error}")
            if response_content:
                print(f"[RESPONSE] 响应: {response_content}")
        
        # 避免请求过快
        time.sleep(0.2)
    
    print(f"\n[SUMMARY] 模块 {module_name} 测试结果:")
    print(f"   - 成功: {success_count}/{total_count}")
    print(f"   - 成功率: {success_count/total_count*100:.1f}%")
    
    return success_count, total_count

def run_module_tests():
    """运行所有模块测试"""
    print("开始按模块测试API接口...")
    print(f"目标服务器: {BASE_URL}")
    print("=" * 80)
    
    # 首先登录
    if not login():
        print("登录失败，无法继续测试")
        return
    
    total_success = 0
    total_interfaces = 0
    
    # 按模块测试
    for module_name, module_config in API_MODULES.items():
        success, total = test_module(module_name, module_config)
        total_success += success
        total_interfaces += total
    
    # 总结
    print(f"\n{'='*80}")
    print("测试完成 - 总体结果")
    print(f"{'='*80}")
    print(f"[STATISTICS] 总体统计:")
    print(f"   - 总接口数: {total_interfaces}")
    print(f"   - 成功数: {total_success}")
    print(f"   - 失败数: {total_interfaces - total_success}")
    print(f"   - 总成功率: {total_success/total_interfaces*100:.2f}%")

if __name__ == "__main__":
    run_module_tests()