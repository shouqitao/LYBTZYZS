#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import requests
import json
import time

BASE_URL = "http://localhost:8080"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzZTRhZmNjZC0yOGU2LTQ4ZDEtOGJlZC1hMTBmNzQzNzFkOWQiLCJ1bmlxdWVfbmFtZSI6InN5c2FkbWluIiwiQWRtaW4iOiJBZG1pbiIsIm5iZiI6MTcyNjM1MjY0MiwiZXhwIjoxNzI2MzgxNDQyLCJpYXQiOjE3MjYzNTI2NDIsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMSIsImF1ZCI6Imh0dHA6Ly9sb2NhbGhvc3Q6NTAwMSJ9.w-hX4A_1PrXhfyGXEt7YVKXhppVdPL4n97GGI-PXGWw"

HEADERS = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

def test_patients():
    print("Testing Patients Create API...")
    
    payload = {
        "Name": "Test Patient Fixed",
        "Gender": 1,
        "Age": 35,
        "PhoneNumber": "13800138001"
    }
    
    url = f"{BASE_URL}/api/v1/patients"
    print(f"URL: {url}")
    print(f"Data: {json.dumps(payload)}")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.text[:200]}")
        return response.status_code in [200, 201]
    except Exception as e:
        print(f"Error: {e}")
        return False

def test_users():
    print("\nTesting Users Create API...")
    
    timestamp = int(time.time())
    payload = {
        "Username": f"testuser{timestamp}",
        "Password": "TestPass123!",
        "ConfirmPassword": "TestPass123!",
        "RealName": "Test User",
        "Role": "Doctor"
    }
    
    url = f"{BASE_URL}/api/v1/users"
    print(f"URL: {url}")
    print(f"Data: {json.dumps(payload)}")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.text[:200]}")
        return response.status_code in [200, 201]
    except Exception as e:
        print(f"Error: {e}")
        return False

def test_consultations():
    print("\nTesting Consultations Start API...")
    
    payload = {
        "MedicalCaseId": "11111111-1111-1111-1111-111111111111",
        "PatientId": "22222222-2222-2222-2222-222222222222",
        "DoctorId": "3e4afccd-28e6-48d1-8bed-a10f74371d9d",
        "EstimatedDuration": 30
    }
    
    url = f"{BASE_URL}/api/v1/consultations/start"
    print(f"URL: {url}")
    print(f"Data: {json.dumps(payload)}")
    
    try:
        response = requests.post(url, headers=HEADERS, json=payload, timeout=10)
        print(f"Status: {response.status_code}")
        print(f"Response: {response.text[:200]}")
        return response.status_code in [200, 201]
    except Exception as e:
        print(f"Error: {e}")
        return False

if __name__ == "__main__":
    print("Backend P3-Fix Batch1: DTO Binding Fix Validation")
    print("=" * 60)
    
    results = []
    results.append(test_patients())
    results.append(test_users())
    results.append(test_consultations())
    
    print("\n" + "=" * 60)
    print(f"Results: {sum(results)}/3 tests passed")
    
    for i, result in enumerate(results, 1):
        status = "PASS" if result else "FAIL"
        print(f"Test {i}: {status}")