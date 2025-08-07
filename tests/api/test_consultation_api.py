#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
看诊模块 API 集成测试
测试完整的看诊工作流
"""

import json
import requests
import time
import uuid
from datetime import datetime

# 服务器配置
BASE_URL = "https://localhost:7001"
API_PREFIX = "/api/v1"

# 禁用 SSL 警告（仅用于测试）
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# JWT 令牌存储
jwt_token = None

# 测试数据存储
test_data = {
    "patient_id": None,
    "doctor_id": None,
    "medical_case_id": None,
    "consultation_id": None
}

def login():
    """执行登录获取JWT令牌"""
    global jwt_token
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Auth/Login",
            json={"username": "sysadmin", "password": "Admin@123456"},
            headers={"Content-Type": "application/json"},
            timeout=10,
            verify=False  # 仅用于测试环境
        )
        if response.status_code == 200:
            data = response.json()
            if data.get("success") and data.get("data") and data["data"].get("token"):
                jwt_token = data["data"]["token"]
                print(f"✅ 登录成功，获取到JWT令牌")
                return True
            else:
                print(f"❌ 登录失败: {data.get('message', '未知错误')}")
                return False
        print(f"❌ 登录失败: {response.status_code} - {response.text}")
    except Exception as e:
        print(f"❌ 登录异常: {str(e)}")
    return False

def create_test_patient():
    """创建测试患者"""
    global test_data
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {jwt_token}"
    }
    
    patient_data = {
        "name": f"测试患者_{datetime.now().strftime('%Y%m%d%H%M%S')}",
        "gender": 0,  # Male
        "birthDate": "1990-01-01",
        "phoneNumber": f"138{datetime.now().strftime('%m%d%H%M')}",
        "address": "测试地址"
    }
    
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Patients",
            json=patient_data,
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            data = response.json()
            test_data["patient_id"] = data["id"]
            print(f"✅ 创建测试患者成功，ID: {test_data['patient_id']}")
            return True
        else:
            print(f"❌ 创建患者失败: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ 创建患者异常: {str(e)}")
        return False

def get_doctor():
    """获取一个医生用户"""
    global test_data
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {jwt_token}"
    }
    
    try:
        # 获取用户列表
        response = requests.get(
            f"{BASE_URL}{API_PREFIX}/Users",
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            users = response.json()
            # 获取第一个用户作为医生
            if users and len(users) > 0:
                test_data["doctor_id"] = users[0]["id"]
                print(f"✅ 获取医生用户成功，ID: {test_data['doctor_id']}")
                return True
        
        print(f"❌ 获取医生失败")
        return False
    except Exception as e:
        print(f"❌ 获取医生异常: {str(e)}")
        return False

def create_medical_case():
    """创建医疗案例"""
    global test_data
    # 暂时使用随机 GUID
    test_data["medical_case_id"] = str(uuid.uuid4())
    print(f"✅ 生成测试医疗案例ID: {test_data['medical_case_id']}")
    return True

def test_start_consultation():
    """测试开始看诊"""
    global test_data
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {jwt_token}"
    }
    
    consultation_data = {
        "medicalCaseId": test_data["medical_case_id"],
        "patientId": test_data["patient_id"],
        "userId": test_data["doctor_id"]
    }
    
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Consultation/start",
            json=consultation_data,
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            data = response.json()
            test_data["consultation_id"] = data["id"]
            print(f"✅ 开始看诊成功，ID: {test_data['consultation_id']}")
            return True
        else:
            print(f"❌ 开始看诊失败: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ 开始看诊异常: {str(e)}")
        return False

def test_update_consultation():
    """测试更新看诊信息"""
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {jwt_token}"
    }
    
    update_data = {
        "inspection": "面色偏白，精神欠佳",
        "auscultationOlfaction": "语声低微，无异常气味",
        "inquiry": "自诉疲乏无力，食欲不振，大便溏薄",
        "palpation": "脉沉细无力",
        "tongueInspection": "舌淡胖，边有齿痕，苔白",
        "pulseCondition": "沉细无力",
        "tcmDiagnosis": "脾虚气弱证",
        "diagnosis": "脾虚证",
        "treatmentPrinciple": "健脾益气，温中和胃",
        "medicalAdvice": "注意保暖，忌生冷食物，规律作息",
        "remark": "患者症状典型，建议配合艾灸治疗"
    }
    
    try:
        response = requests.put(
            f"{BASE_URL}{API_PREFIX}/Consultation/{test_data['consultation_id']}",
            json=update_data,
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            print(f"✅ 更新看诊信息成功")
            return True
        else:
            print(f"❌ 更新看诊失败: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ 更新看诊异常: {str(e)}")
        return False

def test_get_consultation():
    """测试获取看诊详情"""
    headers = {
        "Authorization": f"Bearer {jwt_token}"
    }
    
    try:
        response = requests.get(
            f"{BASE_URL}{API_PREFIX}/Consultation/{test_data['consultation_id']}",
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            data = response.json()
            print(f"✅ 获取看诊详情成功")
            print(f"   - 患者: {data.get('patientName', '未知')}")
            print(f"   - 医生: {data.get('doctorName', '未知')}")
            print(f"   - 诊断: {data.get('diagnosis', '未知')}")
            return True
        else:
            print(f"❌ 获取看诊详情失败: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ 获取看诊详情异常: {str(e)}")
        return False

def test_complete_consultation():
    """测试完成看诊"""
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {jwt_token}"
    }
    
    complete_data = {
        "diagnosis": "脾虚证（轻度）",
        "tcmDiagnosis": "脾虚气弱，运化失职",
        "treatmentPrinciple": "健脾益气，燥湿和中",
        "medicalAdvice": "1. 按时服用中药，每日两次\n2. 饮食清淡，避免生冷油腻\n3. 适量运动，增强体质\n4. 两周后复诊"
    }
    
    try:
        response = requests.post(
            f"{BASE_URL}{API_PREFIX}/Consultation/{test_data['consultation_id']}/complete",
            json=complete_data,
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            print(f"✅ 完成看诊成功")
            return True
        else:
            print(f"❌ 完成看诊失败: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ 完成看诊异常: {str(e)}")
        return False

def test_patient_history():
    """测试获取患者历史看诊记录"""
    headers = {
        "Authorization": f"Bearer {jwt_token}"
    }
    
    try:
        response = requests.get(
            f"{BASE_URL}{API_PREFIX}/Consultation/patient/{test_data['patient_id']}/history",
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            data = response.json()
            print(f"✅ 获取患者历史看诊记录成功，共 {len(data)} 条记录")
            return True
        else:
            print(f"❌ 获取患者历史失败: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ 获取患者历史异常: {str(e)}")
        return False

def test_doctor_statistics():
    """测试医生看诊统计"""
    headers = {
        "Authorization": f"Bearer {jwt_token}"
    }
    
    try:
        response = requests.get(
            f"{BASE_URL}{API_PREFIX}/Consultation/doctor/{test_data['doctor_id']}/count",
            headers=headers,
            timeout=10,
            verify=False
        )
        
        if response.status_code == 200:
            data = response.json()
            print(f"✅ 获取医生看诊统计成功，总数: {data.get('count', 0)}")
            return True
        else:
            print(f"❌ 获取医生统计失败: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ 获取医生统计异常: {str(e)}")
        return False

def run_consultation_workflow():
    """运行完整的看诊工作流测试"""
    print("=" * 60)
    print("开始看诊模块集成测试")
    print("=" * 60)
    
    # 1. 登录
    if not login():
        print("❌ 登录失败，测试终止")
        return
    
    # 2. 准备测试数据
    print("\n📋 准备测试数据...")
    if not create_test_patient():
        print("❌ 创建患者失败，测试终止")
        return
    
    if not get_doctor():
        print("❌ 获取医生失败，测试终止")
        return
    
    if not create_medical_case():
        print("❌ 创建医疗案例失败，测试终止")
        return
    
    # 3. 测试看诊流程
    print("\n🏥 开始测试看诊流程...")
    
    # 开始看诊
    if not test_start_consultation():
        print("❌ 开始看诊失败，测试终止")
        return
    
    time.sleep(1)  # 等待1秒
    
    # 更新看诊信息
    if not test_update_consultation():
        print("❌ 更新看诊失败")
    
    time.sleep(1)
    
    # 获取看诊详情
    if not test_get_consultation():
        print("❌ 获取看诊详情失败")
    
    time.sleep(1)
    
    # 完成看诊
    if not test_complete_consultation():
        print("❌ 完成看诊失败")
    
    # 4. 测试查询功能
    print("\n📊 测试查询功能...")
    
    # 患者历史
    test_patient_history()
    
    # 医生统计
    test_doctor_statistics()
    
    print("\n" + "=" * 60)
    print("✅ 看诊模块集成测试完成")
    print("=" * 60)

if __name__ == "__main__":
    run_consultation_workflow()