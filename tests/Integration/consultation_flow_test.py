#!/usr/bin/env python3
"""
看诊流程集成测试脚本
测试完整的看诊业务流程
"""

import requests
import json
import time
import uuid
from datetime import datetime
from typing import Dict, Any, Optional

# 配置
BASE_URL = "https://localhost:7001/api/v1"
VERIFY_SSL = False  # 开发环境禁用SSL验证

# 禁用SSL警告
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)


class ConsultationFlowTest:
    """看诊流程测试类"""
    
    def __init__(self):
        self.session = requests.Session()
        self.session.verify = VERIFY_SSL
        self.token = None
        self.test_data = {}
        
    def login(self, username: str = "sysadmin", password: str = "Admin@123456") -> bool:
        """登录系统"""
        print(f"正在登录系统 (用户名: {username})...")
        
        response = self.session.post(
            f"{BASE_URL}/auth/login",
            json={"username": username, "password": password, "rememberMe": False}
        )
        
        if response.status_code == 200:
            data = response.json()
            self.token = data.get("token")
            self.session.headers.update({"Authorization": f"Bearer {self.token}"})
            print("✓ 登录成功")
            return True
        else:
            print(f"✗ 登录失败: {response.status_code} - {response.text}")
            return False
    
    def create_patient(self) -> Optional[Dict[str, Any]]:
        """创建测试患者"""
        print("\n创建测试患者...")
        
        patient_data = {
            "name": f"测试患者_{uuid.uuid4().hex[:8]}",
            "gender": 1,  # 男性
            "age": 35,
            "phoneNumber": "13800138000",
            "idCard": f"110101198801{str(int(time.time()))[-6:]}",
            "address": "北京市测试地址",
            "medicalHistory": "既往体健",
            "allergyHistory": "无药物过敏史"
        }
        
        response = self.session.post(f"{BASE_URL}/patients", json=patient_data)
        
        if response.status_code in [200, 201]:
            patient = response.json()
            self.test_data["patient"] = patient
            print(f"✓ 患者创建成功: {patient['name']} (ID: {patient['id']})")
            return patient
        else:
            print(f"✗ 患者创建失败: {response.status_code} - {response.text}")
            return None
    
    def create_medical_case(self, patient_id: str) -> Optional[Dict[str, Any]]:
        """创建医疗案例"""
        print("\n创建医疗案例...")
        
        case_data = {
            "patientId": patient_id,
            "chiefComplaint": "疲劳乏力3月余",
            "caseType": "初诊"
        }
        
        response = self.session.post(f"{BASE_URL}/medicalcase", json=case_data)
        
        if response.status_code in [200, 201]:
            medical_case = response.json()
            self.test_data["medicalCase"] = medical_case
            print(f"✓ 医疗案例创建成功 (ID: {medical_case['id']})")
            return medical_case
        else:
            print(f"✗ 医疗案例创建失败: {response.status_code} - {response.text}")
            return None
    
    def start_consultation(self, medical_case_id: str, patient_id: str) -> Optional[Dict[str, Any]]:
        """开始看诊"""
        print("\n开始看诊...")
        
        start_data = {
            "medicalCaseId": medical_case_id,
            "patientId": patient_id
        }
        
        response = self.session.post(f"{BASE_URL}/consultation/start", json=start_data)
        
        if response.status_code in [200, 201]:
            consultation = response.json()
            self.test_data["consultation"] = consultation
            print(f"✓ 看诊开始成功 (ID: {consultation['id']})")
            return consultation
        else:
            print(f"✗ 开始看诊失败: {response.status_code} - {response.text}")
            return None
    
    def update_consultation_tcm(self, consultation_id: str) -> bool:
        """更新看诊的中医四诊信息"""
        print("\n更新中医四诊信息...")
        
        tcm_data = {
            "inspection": "面色偏黄，精神尚可，形体偏瘦",
            "auscultationOlfaction": "语音低微，口气正常，无特殊气味",
            "inquiry": "主诉：疲劳乏力3月余。现病史：患者3月前无明显诱因出现疲劳乏力，活动后加重，休息后可缓解，伴有食欲不振，大便偏稀，每日2-3次",
            "palpation": "脉象：脉细弱，尺脉尤甚",
            "tongueInspection": "舌质淡，边有齿痕，苔薄白",
            "pulseCondition": "脉细弱",
            "tcmDiagnosis": "脾气虚证",
            "diagnosis": "慢性疲劳综合征（脾气虚证）"
        }
        
        response = self.session.put(f"{BASE_URL}/consultation/{consultation_id}", json=tcm_data)
        
        if response.status_code == 200:
            print("✓ 四诊信息更新成功")
            return True
        else:
            print(f"✗ 四诊信息更新失败: {response.status_code} - {response.text}")
            return False
    
    def create_prescription(self, consultation_id: str, patient_id: str) -> Optional[Dict[str, Any]]:
        """开具处方"""
        print("\n开具中药处方...")
        
        prescription_data = {
            "consultationId": consultation_id,
            "patientId": patient_id,
            "type": "中药处方",
            "usage": "每日一剂，水煎服，早晚分服",
            "days": 7,
            "notes": "忌生冷油腻",
            "items": [
                {"herbName": "黄芪", "dosage": 30, "unit": "g"},
                {"herbName": "党参", "dosage": 15, "unit": "g"},
                {"herbName": "白术", "dosage": 15, "unit": "g"},
                {"herbName": "茯苓", "dosage": 15, "unit": "g"},
                {"herbName": "甘草", "dosage": 6, "unit": "g"},
                {"herbName": "当归", "dosage": 10, "unit": "g"},
                {"herbName": "陈皮", "dosage": 10, "unit": "g"},
                {"herbName": "升麻", "dosage": 6, "unit": "g"},
                {"herbName": "柴胡", "dosage": 6, "unit": "g"}
            ]
        }
        
        response = self.session.post(f"{BASE_URL}/prescriptions", json=prescription_data)
        
        if response.status_code in [200, 201]:
            prescription = response.json()
            self.test_data["prescription"] = prescription
            print(f"✓ 处方开具成功 (ID: {prescription['id']})")
            print(f"  处方类型: {prescription_data['type']}")
            print(f"  药材数量: {len(prescription_data['items'])} 味")
            print(f"  服用天数: {prescription_data['days']} 天")
            return prescription
        else:
            print(f"✗ 处方开具失败: {response.status_code} - {response.text}")
            return None
    
    def complete_consultation(self, consultation_id: str) -> bool:
        """完成看诊"""
        print("\n完成看诊...")
        
        complete_data = {
            "summary": "患者脾气虚证诊断明确，予补中益气方加减治疗。嘱患者注意休息，饮食规律，忌生冷油腻。",
            "followUpAdvice": "一周后复诊，如有不适随时就诊。"
        }
        
        response = self.session.post(
            f"{BASE_URL}/consultation/{consultation_id}/complete", 
            json=complete_data
        )
        
        if response.status_code == 200:
            print("✓ 看诊完成")
            return True
        else:
            print(f"✗ 看诊完成失败: {response.status_code} - {response.text}")
            return False
    
    def verify_consultation_status(self, consultation_id: str) -> bool:
        """验证看诊最终状态"""
        print("\n验证看诊状态...")
        
        response = self.session.get(f"{BASE_URL}/consultation/{consultation_id}")
        
        if response.status_code == 200:
            consultation = response.json()
            status = consultation.get("status", -1)
            
            # 假设状态 2 表示已完成
            if status == 2:
                print(f"✓ 看诊状态正确：已完成")
                return True
            else:
                print(f"✗ 看诊状态异常：{status}")
                return False
        else:
            print(f"✗ 获取看诊信息失败: {response.status_code}")
            return False
    
    def run_complete_flow(self) -> bool:
        """运行完整的看诊流程测试"""
        print("=" * 60)
        print("开始看诊流程集成测试")
        print("=" * 60)
        
        # 步骤1: 登录
        if not self.login():
            return False
        
        # 步骤2: 创建患者
        patient = self.create_patient()
        if not patient:
            return False
        
        # 步骤3: 创建医疗案例
        medical_case = self.create_medical_case(patient["id"])
        if not medical_case:
            return False
        
        # 步骤4: 开始看诊
        consultation = self.start_consultation(medical_case["id"], patient["id"])
        if not consultation:
            return False
        
        # 步骤5: 更新四诊信息
        if not self.update_consultation_tcm(consultation["id"]):
            return False
        
        # 步骤6: 开具处方
        prescription = self.create_prescription(consultation["id"], patient["id"])
        if not prescription:
            return False
        
        # 步骤7: 完成看诊
        if not self.complete_consultation(consultation["id"]):
            return False
        
        # 步骤8: 验证最终状态
        if not self.verify_consultation_status(consultation["id"]):
            return False
        
        print("\n" + "=" * 60)
        print("✓ 看诊流程测试全部通过！")
        print("=" * 60)
        
        # 输出测试数据汇总
        print("\n测试数据汇总：")
        print(f"- 患者ID: {self.test_data['patient']['id']}")
        print(f"- 医疗案例ID: {self.test_data['medicalCase']['id']}")
        print(f"- 看诊ID: {self.test_data['consultation']['id']}")
        print(f"- 处方ID: {self.test_data['prescription']['id']}")
        
        return True
    
    def run_error_scenarios(self):
        """测试异常场景"""
        print("\n" + "=" * 60)
        print("开始异常场景测试")
        print("=" * 60)
        
        # 场景1: 使用不存在的患者ID
        print("\n测试场景1: 使用不存在的患者ID创建医疗案例")
        fake_patient_id = str(uuid.uuid4())
        response = self.session.post(
            f"{BASE_URL}/medicalcase",
            json={"patientId": fake_patient_id, "chiefComplaint": "测试", "caseType": "初诊"}
        )
        if response.status_code >= 400:
            print("✓ 正确拒绝了无效的患者ID")
        else:
            print("✗ 未能拒绝无效的患者ID")
        
        # 场景2: 重复开始看诊
        if "medicalCase" in self.test_data and "patient" in self.test_data:
            print("\n测试场景2: 对同一医疗案例重复开始看诊")
            response = self.session.post(
                f"{BASE_URL}/consultation/start",
                json={
                    "medicalCaseId": self.test_data["medicalCase"]["id"],
                    "patientId": self.test_data["patient"]["id"]
                }
            )
            # 根据业务逻辑，这可能被允许或拒绝
            print(f"重复开始看诊响应: {response.status_code}")


def main():
    """主函数"""
    tester = ConsultationFlowTest()
    
    # 运行正常流程测试
    success = tester.run_complete_flow()
    
    # 运行异常场景测试
    if success:
        tester.run_error_scenarios()
    
    return 0 if success else 1


if __name__ == "__main__":
    exit(main())