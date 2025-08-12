#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
处方管理端到端测试脚本
测试阶段3实现的所有核心功能
"""

import asyncio
import aiohttp
import json
import ssl
import time
from datetime import datetime
from typing import Dict, Any, List, Optional

class PrescriptionE2ETest:
    def __init__(self):
        self.base_url = "https://localhost:7001"
        self.auth_token = None
        self.test_results = []
        
        # 忽略SSL证书验证（开发环境）
        self.ssl_context = ssl.create_default_context()
        self.ssl_context.check_hostname = False
        self.ssl_context.verify_mode = ssl.CERT_NONE

    async def log_test(self, test_name: str, success: bool, message: str, details: Any = None):
        """记录测试结果"""
        result = {
            "test": test_name,
            "success": success,
            "message": message,
            "timestamp": datetime.now().isoformat(),
            "details": details
        }
        self.test_results.append(result)
        status = "PASS" if success else "FAIL"
        print(f"{status} {test_name}: {message}")
        if details and not success:
            print(f"   详情: {details}")

    async def wait_for_service(self, max_attempts=30):
        """等待服务启动"""
        print("等待API服务启动...")
        
        for attempt in range(max_attempts):
            try:
                async with aiohttp.ClientSession(
                    connector=aiohttp.TCPConnector(ssl=self.ssl_context)
                ) as session:
                    async with session.get(f"{self.base_url}/swagger/v1/swagger.json") as response:
                        if response.status == 200:
                            await self.log_test(
                                "服务连通性测试", 
                                True, 
                                f"API服务已启动，耗时 {attempt + 1} 秒"
                            )
                            return True
            except Exception as e:
                if attempt < max_attempts - 1:
                    await asyncio.sleep(1)
                else:
                    await self.log_test(
                        "服务连通性测试", 
                        False, 
                        f"API服务启动失败", 
                        str(e)
                    )
                    return False
        return False

    async def authenticate(self):
        """用户认证测试"""
        login_data = {
            "username": "sysadmin",
            "password": "Admin@123456",
            "rememberMe": False
        }
        
        try:
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                async with session.post(
                    f"{self.base_url}/api/v1/auth/login",
                    json=login_data,
                    headers={"Content-Type": "application/json"}
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        if data.get("success") and data.get("data", {}).get("token"):
                            self.auth_token = data["data"]["token"]
                            await self.log_test(
                                "用户认证测试", 
                                True, 
                                "认证成功，获取到访问令牌"
                            )
                            return True
                        else:
                            await self.log_test(
                                "用户认证测试", 
                                False, 
                                "认证响应格式异常", 
                                data
                            )
                            return False
                    else:
                        error_text = await response.text()
                        await self.log_test(
                            "用户认证测试", 
                            False, 
                            f"认证失败，状态码: {response.status}", 
                            error_text
                        )
                        return False
        except Exception as e:
            await self.log_test(
                "用户认证测试", 
                False, 
                "认证请求异常", 
                str(e)
            )
            return False

    async def get_auth_headers(self) -> Dict[str, str]:
        """获取认证头"""
        return {
            "Authorization": f"Bearer {self.auth_token}",
            "Content-Type": "application/json"
        }

    async def test_herbs_api(self):
        """药材管理API测试"""
        if not self.auth_token:
            await self.log_test(
                "药材API测试", 
                False, 
                "未获取到认证令牌，跳过测试"
            )
            return False

        try:
            headers = await self.get_auth_headers()
            
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                # 测试获取药材列表
                async with session.get(
                    f"{self.base_url}/api/v1/herbs",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        herb_count = len(data.get("data", []))
                        await self.log_test(
                            "药材列表API测试", 
                            True, 
                            f"成功获取药材列表，共 {herb_count} 个药材"
                        )
                        return True
                    else:
                        error_text = await response.text()
                        await self.log_test(
                            "药材列表API测试", 
                            False, 
                            f"获取药材列表失败，状态码: {response.status}", 
                            error_text
                        )
                        return False
                        
        except Exception as e:
            await self.log_test(
                "药材API测试", 
                False, 
                "药材API请求异常", 
                str(e)
            )
            return False

    async def test_prescriptions_api(self):
        """处方管理API测试"""
        if not self.auth_token:
            await self.log_test(
                "处方API测试", 
                False, 
                "未获取到认证令牌，跳过测试"
            )
            return False

        try:
            headers = await self.get_auth_headers()
            
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                
                # 1. 测试获取处方列表
                async with session.get(
                    f"{self.base_url}/api/v1/prescriptions",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        prescription_count = len(data.get("data", []))
                        await self.log_test(
                            "处方列表API测试", 
                            True, 
                            f"成功获取处方列表，共 {prescription_count} 个处方"
                        )
                    else:
                        error_text = await response.text()
                        await self.log_test(
                            "处方列表API测试", 
                            False, 
                            f"获取处方列表失败，状态码: {response.status}", 
                            error_text
                        )
                        return False

                # 2. 测试分页查询（阶段3新增功能）
                query_params = {
                    "pageIndex": 1,
                    "pageSize": 10
                }
                
                async with session.get(
                    f"{self.base_url}/api/v1/prescriptions/paged",
                    headers=headers,
                    params=query_params
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        total_count = data.get("data", {}).get("totalCount", 0)
                        items_count = len(data.get("data", {}).get("items", []))
                        await self.log_test(
                            "处方分页查询测试", 
                            True, 
                            f"分页查询成功，总数: {total_count}，当前页: {items_count} 条"
                        )
                    else:
                        error_text = await response.text()
                        await self.log_test(
                            "处方分页查询测试", 
                            False, 
                            f"分页查询失败，状态码: {response.status}", 
                            error_text
                        )
                        
                return True
                        
        except Exception as e:
            await self.log_test(
                "处方API测试", 
                False, 
                "处方API请求异常", 
                str(e)
            )
            return False

    async def test_batch_operations(self):
        """测试批量操作功能（阶段3新增）"""
        if not self.auth_token:
            await self.log_test(
                "批量操作测试", 
                False, 
                "未获取到认证令牌，跳过测试"
            )
            return False

        try:
            headers = await self.get_auth_headers()
            
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                
                # 测试药材批量状态更新（阶段3修复的功能）
                batch_update_data = {
                    "ids": [],  # 空列表测试，验证SQL语法错误是否修复
                    "status": 1,
                    "isEnabled": True,
                    "reason": "端到端测试批量状态更新"
                }
                
                async with session.patch(
                    f"{self.base_url}/api/v1/herbs/batch-status",
                    headers=headers,
                    json=batch_update_data
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        await self.log_test(
                            "药材批量状态更新测试", 
                            True, 
                            "批量状态更新API正常工作（空列表测试通过）"
                        )
                    else:
                        error_text = await response.text()
                        await self.log_test(
                            "药材批量状态更新测试", 
                            False, 
                            f"批量状态更新失败，状态码: {response.status}", 
                            error_text
                        )
                        
                return True
                        
        except Exception as e:
            await self.log_test(
                "批量操作测试", 
                False, 
                "批量操作请求异常", 
                str(e)
            )
            return False

    async def test_template_functionality(self):
        """测试处方模板功能（阶段3新增）"""
        if not self.auth_token:
            await self.log_test(
                "处方模板功能测试", 
                False, 
                "未获取到认证令牌，跳过测试"
            )
            return False

        try:
            headers = await self.get_auth_headers()
            
            async with aiohttp.ClientSession(
                connector=aiohttp.TCPConnector(ssl=self.ssl_context)
            ) as session:
                
                # 测试获取验方模板列表
                async with session.get(
                    f"{self.base_url}/api/v1/FormulaTemplate",
                    headers=headers
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        template_count = len(data.get("data", []))
                        await self.log_test(
                            "验方模板列表测试", 
                            True, 
                            f"成功获取验方模板列表，共 {template_count} 个模板"
                        )
                    else:
                        error_text = await response.text()
                        await self.log_test(
                            "验方模板列表测试", 
                            False, 
                            f"获取验方模板列表失败，状态码: {response.status}", 
                            error_text
                        )
                        return False

                # 测试创建验方模板
                test_template = {
                    "name": "端到端测试模板",
                    "herbs": [
                        {
                            "id": "11111111-1111-1111-1111-111111111111",
                            "name": "测试药材1",
                            "price": 12.5,
                            "stock": 100,
                            "unit": "g"
                        }
                    ],
                    "remark": "这是端到端测试创建的模板"
                }
                
                async with session.post(
                    f"{self.base_url}/api/v1/FormulaTemplate",
                    headers=headers,
                    json=test_template
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        await self.log_test(
                            "验方模板创建测试", 
                            True, 
                            "成功创建验方模板"
                        )
                        return True
                    else:
                        error_text = await response.text()
                        await self.log_test(
                            "验方模板创建测试", 
                            False, 
                            f"创建验方模板失败，状态码: {response.status}", 
                            error_text
                        )
                        # 即使创建失败也继续测试
                        return True
                        
        except Exception as e:
            await self.log_test(
                "处方模板功能测试", 
                False, 
                "处方模板功能请求异常", 
                str(e)
            )
            return False

    async def generate_test_report(self):
        """生成测试报告"""
        total_tests = len(self.test_results)
        passed_tests = len([r for r in self.test_results if r["success"]])
        failed_tests = total_tests - passed_tests
        success_rate = (passed_tests / total_tests * 100) if total_tests > 0 else 0
        
        report = {
            "测试概要": {
                "总测试数": total_tests,
                "通过数": passed_tests,
                "失败数": failed_tests,
                "成功率": f"{success_rate:.1f}%"
            },
            "测试详情": self.test_results,
            "测试时间": datetime.now().isoformat()
        }
        
        # 保存到文件
        report_file = f"tests/results/prescription_e2e_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        
        print(f"\n测试报告总结:")
        print(f"总测试数: {total_tests}")
        print(f"通过数: {passed_tests}")
        print(f"失败数: {failed_tests}")
        print(f"成功率: {success_rate:.1f}%")
        
        if failed_tests > 0:
            print("\n失败的测试:")
            for result in self.test_results:
                if not result["success"]:
                    print(f"  - {result['test']}: {result['message']}")
        
        return report

    async def run_all_tests(self):
        """运行所有测试"""
        print("开始处方管理端到端测试")
        print("=" * 50)
        
        # 1. 等待服务启动
        if not await self.wait_for_service():
            print("× 服务启动失败，测试终止")
            return
        
        # 2. 认证测试
        if not await self.authenticate():
            print("× 认证失败，测试终止")
            return
            
        # 3. 基础API测试
        await self.test_herbs_api()
        await self.test_prescriptions_api()
        
        # 4. 阶段3新增功能测试
        await self.test_batch_operations()
        await self.test_template_functionality()
        
        # 5. 生成报告
        print("\n" + "=" * 50)
        await self.generate_test_report()
        print("端到端测试完成！")

async def main():
    """主函数"""
    tester = PrescriptionE2ETest()
    await tester.run_all_tests()

if __name__ == "__main__":
    asyncio.run(main())