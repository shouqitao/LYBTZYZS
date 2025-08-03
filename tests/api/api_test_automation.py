#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LYBT中医诊所管理系统 - API测试自动化脚本
自动测试所有模块的API接口，生成详细报告
"""

import json
import requests
import time
from datetime import datetime
from typing import Dict, List, Tuple
import re
import time

# 服务器配置
BASE_URL = "http://192.168.190.243:5000"
API_PREFIX = "/api/v1.0"

# 测试结果存储
test_results = []

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
            {"path": "/Users/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Users", "method": "POST", "auth": True, "data": {
                "username": "testuser",
                "password": "Admin@123456",
                "nickname": "测试用户",
                "phone": "13800138000",
                "email": "test@example.com",
                "roleNames": ["Doctor"]
            }},
            {"path": "/Users/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "username": "sysadmin",
                "nickname": "系统管理员",
                "phone": "13800138001",
                "email": "admin@example.com"
            }},
            {"path": "/Users/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Patients": {
        "endpoints": [
            {"path": "/Patients", "method": "GET", "auth": True},
            {"path": "/Patients/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Patients", "method": "POST", "auth": True, "data": {
                "name": "测试患者",
                "gender": "男",
                "birthDate": "1990-01-01",
                "phone": "13900139000",
                "address": "测试地址",
                "medicalHistory": "无"
            }},
            {"path": "/Patients/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "name": "更新患者",
                "gender": "男",
                "birthDate": "1990-01-01",
                "phone": "13900139001"
            }},
            {"path": "/Patients/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Doctors": {
        "endpoints": [
            {"path": "/Doctors", "method": "GET", "auth": True},
            {"path": "/Doctors/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Doctors", "method": "POST", "auth": True, "data": {
                "name": "测试医生",
                "gender": "男",
                "birthDate": "1980-01-01",
                "phone": "13700137000",
                "department": "中医内科",
                "title": "主治医师",
                "specialties": "内科疾病"
            }},
            {"path": "/Doctors/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "name": "更新医生",
                "department": "中医内科",
                "title": "副主任医师"
            }},
            {"path": "/Doctors/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Registration": {
        "endpoints": [
            {"path": "/Registration", "method": "GET", "auth": True},
            {"path": "/Registration/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Registration", "method": "POST", "auth": True, "data": {
                "patientId": 1,
                "doctorId": 1,
                "appointmentDate": datetime.now().strftime("%Y-%m-%d"),
                "appointmentTime": "09:00",
                "reason": "感冒发烧"
            }},
            {"path": "/Registration/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "status": "已就诊"
            }},
            {"path": "/Registration/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Queueing": {
        "endpoints": [
            {"path": "/Queueing", "method": "GET", "auth": True},
            {"path": "/Queueing/Current", "method": "GET", "auth": True},
            {"path": "/Queueing/Next", "method": "POST", "auth": True},
            {"path": "/Queueing/Call/{id}", "method": "POST", "auth": True, "params": {"id": 1}},
            {"path": "/Queueing/Skip/{id}", "method": "POST", "auth": True, "params": {"id": 1}}
        ]
    },
    "DiagnosisTreatment": {
        "endpoints": [
            {"path": "/DiagnosisTreatment", "method": "GET", "auth": True},
            {"path": "/DiagnosisTreatment/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/DiagnosisTreatment", "method": "POST", "auth": True, "data": {
                "patientId": 1,
                "doctorId": 1,
                "diagnosisDate": datetime.datetime.now().strftime("%Y-%m-%d"),
                "chiefComplaint": "咳嗽三天",
                "diagnosis": "风寒感冒",
                "treatment": "疏风散寒"
            }},
            {"path": "/DiagnosisTreatment/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "diagnosis": "风寒感冒",
                "treatment": "疏风散寒，宣肺止咳"
            }},
            {"path": "/DiagnosisTreatment/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Prescriptions": {
        "endpoints": [
            {"path": "/Prescriptions", "method": "GET", "auth": True},
            {"path": "/Prescriptions/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Prescriptions", "method": "POST", "auth": True, "data": {
                "patientId": 1,
                "doctorId": 1,
                "diagnosisId": 1,
                "prescriptionDate": datetime.datetime.now().strftime("%Y-%m-%d"),
                "items": [
                    {"herbId": 1, "quantity": 10, "unit": "克"},
                    {"herbId": 2, "quantity": 15, "unit": "克"}
                ]
            }},
            {"path": "/Prescriptions/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "status": "已配药"
            }},
            {"path": "/Prescriptions/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Herbs": {
        "endpoints": [
            {"path": "/Herbs", "method": "GET", "auth": True},
            {"path": "/Herbs/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Herbs", "method": "POST", "auth": True, "data": {
                "name": "麻黄",
                "pinyin": "MaHuang",
                "category": "解表药",
                "efficacy": "发汗解表，宣肺平喘",
                "usage": "3-10克",
                "stock": 1000,
                "unit": "克",
                "price": 0.5
            }},
            {"path": "/Herbs/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "name": "麻黄",
                "stock": 900,
                "price": 0.6
            }},
            {"path": "/Herbs/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "FormulaTemplates": {
        "endpoints": [
            {"path": "/FormulaTemplates", "method": "GET", "auth": True},
            {"path": "/FormulaTemplates/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/FormulaTemplates", "method": "POST", "auth": True, "data": {
                "name": "麻黄汤",
                "category": "解表剂",
                "indications": "外感风寒表实证",
                "composition": "麻黄9g，桂枝6g，杏仁9g，甘草3g",
                "usage": "水煎服"
            }},
            {"path": "/FormulaTemplates/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "name": "麻黄汤",
                "usage": "水煎服，日三次"
            }},
            {"path": "/FormulaTemplates/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Pharmacy": {
        "endpoints": [
            {"path": "/Pharmacy/Dispensing", "method": "GET", "auth": True},
            {"path": "/Pharmacy/Dispensing/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Pharmacy/Dispense", "method": "POST", "auth": True, "data": {
                "prescriptionId": 1,
                "pharmacistId": 1,
                "dispensingDate": datetime.datetime.now().strftime("%Y-%m-%d")
            }},
            {"path": "/Pharmacy/Return", "method": "POST", "auth": True, "data": {
                "dispensingId": 1,
                "reason": "患者要求退药"
            }}
        ]
    },
    "Billing": {
        "endpoints": [
            {"path": "/Billing", "method": "GET", "auth": True},
            {"path": "/Billing/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Billing", "method": "POST", "auth": True, "data": {
                "patientId": 1,
                "type": "挂号费",
                "amount": 30.00,
                "billingDate": datetime.datetime.now().strftime("%Y-%m-%d")
            }},
            {"path": "/Billing/Pay/{id}", "method": "POST", "auth": True, "params": {"id": 1}, "data": {
                "paymentMethod": "现金",
                "actualAmount": 30.00
            }},
            {"path": "/Billing/Refund/{id}", "method": "POST", "auth": True, "params": {"id": 1}, "data": {
                "reason": "退号"
            }}
        ]
    },
    "Records": {
        "endpoints": [
            {"path": "/Records", "method": "GET", "auth": True},
            {"path": "/Records/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/Records/Patient/{patientId}", "method": "GET", "auth": True, "params": {"patientId": 1}},
            {"path": "/Records", "method": "POST", "auth": True, "data": {
                "patientId": 1,
                "doctorId": 1,
                "visitDate": datetime.datetime.now().strftime("%Y-%m-%d"),
                "content": "患者主诉咳嗽三天，诊断为风寒感冒"
            }},
            {"path": "/Records/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "TreatmentRoom": {
        "endpoints": [
            {"path": "/TreatmentRoom", "method": "GET", "auth": True},
            {"path": "/TreatmentRoom/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/TreatmentRoom", "method": "POST", "auth": True, "data": {
                "roomNumber": "101",
                "name": "针灸室1",
                "type": "针灸室",
                "status": "空闲"
            }},
            {"path": "/TreatmentRoom/{id}", "method": "PUT", "auth": True, "params": {"id": 1}, "data": {
                "id": 1,
                "status": "使用中"
            }},
            {"path": "/TreatmentRoom/{id}", "method": "DELETE", "auth": True, "params": {"id": 999}}
        ]
    },
    "Sync": {
        "endpoints": [
            {"path": "/Sync/Status", "method": "GET", "auth": True},
            {"path": "/Sync/Start", "method": "POST", "auth": True},
            {"path": "/Sync/History", "method": "GET", "auth": True}
        ]
    },
    "UnifiedConfig": {
        "endpoints": [
            {"path": "/UnifiedConfig", "method": "GET", "auth": True},
            {"path": "/UnifiedConfig/{key}", "method": "GET", "auth": True, "params": {"key": "system.name"}},
            {"path": "/UnifiedConfig", "method": "POST", "auth": True, "data": {
                "key": "test.config",
                "value": "test value",
                "description": "测试配置"
            }},
            {"path": "/UnifiedConfig/{key}", "method": "PUT", "auth": True, "params": {"key": "test.config"}, "data": {
                "value": "updated value"
            }},
            {"path": "/UnifiedConfig/{key}", "method": "DELETE", "auth": True, "params": {"key": "test.config"}}
        ]
    },
    "UnifiedLogs": {
        "endpoints": [
            {"path": "/UnifiedLogs", "method": "GET", "auth": True},
            {"path": "/UnifiedLogs/{id}", "method": "GET", "auth": True, "params": {"id": 1}},
            {"path": "/UnifiedLogs/Export", "method": "GET", "auth": True}
        ]
    },
    "Health": {
        "endpoints": [
            {"path": "/Health", "method": "GET", "auth": False},
            {"path": "/Health/Database", "method": "GET", "auth": False}
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

def test_endpoint(module: str, endpoint: Dict[str, Any]) -> Tuple[bool, str]:
    """测试单个接口"""
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
            return True, ""
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
            return False, error_msg
            
    except requests.exceptions.ConnectionError:
        return False, "连接失败"
    except requests.exceptions.Timeout:
        return False, "请求超时"
    except Exception as e:
        return False, str(e)

def run_all_tests():
    """运行所有API测试"""
    global test_results
    
    print("开始API全量测试...")
    print(f"目标服务器: {BASE_URL}")
    print("-" * 80)
    
    # 首先登录
    if not login():
        print("登录失败，无法继续测试")
        return
    
    # 遍历所有模块和接口
    for module_name, module_config in API_MODULES.items():
        print(f"\n测试模块: {module_name}")
        
        for endpoint in module_config["endpoints"]:
            path = endpoint["path"]
            method = endpoint["method"]
            
            # 执行测试
            success, error = test_endpoint(module_name, endpoint)
            
            # 记录结果
            test_results.append({
                "module": module_name,
                "path": path,
                "method": method,
                "success": success,
                "error": error
            })
            
            # 打印进度
            status = "[OK]" if success else "[FAIL]"
            print(f"  {status} {method} {path}")
            
            # 避免请求过快
            time.sleep(0.1)
    
    print("\n" + "-" * 80)
    print("测试完成！")

def generate_report():
    """生成测试报告"""
    report_content = """# API测试报告

测试时间: {}
服务器地址: {}

## 测试结果汇总

| 模块名 | 接口路径 | 方法 | 测试结果 | 备注 |
|--------|----------|------|----------|------|
""".format(
        datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        BASE_URL
    )
    
    # 统计信息
    total_count = len(test_results)
    success_count = sum(1 for r in test_results if r["success"])
    failure_count = total_count - success_count
    
    if total_count == 0:
        print("没有测试结果可生成报告")
        return
    
    # 生成表格
    for result in test_results:
        status = "成功" if result["success"] else "失败"
        error = result["error"] if result["error"] else ""
        report_content += f"| {result['module']} | {result['path']} | {result['method']} | {status} | {error} |\n"
    
    # 添加统计
    report_content += f"\n## 统计信息\n\n"
    report_content += f"- 总接口数: {total_count}\n"
    report_content += f"- 成功数: {success_count}\n"
    report_content += f"- 失败数: {failure_count}\n"
    report_content += f"- 成功率: {success_count/total_count*100:.2f}%\n"
    
    # 失败接口汇总
    if failure_count > 0:
        report_content += "\n## 失败接口汇总\n\n"
        for result in test_results:
            if not result["success"]:
                report_content += f"- **{result['module']}** - {result['method']} {result['path']}: {result['error']}\n"
    
    # 保存报告
    with open("D:/source/repos/LYBTZYZS/Tasks/API测试报告.md", "w", encoding="utf-8") as f:
        f.write(report_content)
    
    print(f"\n报告已生成: Tasks/API测试报告.md")
    print(f"成功率: {success_count}/{total_count} ({success_count/total_count*100:.2f}%)")

if __name__ == "__main__":
    run_all_tests()
    generate_report()