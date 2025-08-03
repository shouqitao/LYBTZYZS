#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
项目记忆·参数自动学习功能
自动记忆和管理重复出现的参数错误
"""

import json
import os
import subprocess
from datetime import datetime
from typing import Dict, Any, Optional, List

class ParameterMemory:
    def __init__(self, config_path: str = "config/memo.json"):
        self.config_path = config_path
        self.config = self._load_config()
    
    def _load_config(self) -> Dict[str, Any]:
        """加载配置文件"""
        if os.path.exists(self.config_path):
            try:
                with open(self.config_path, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except Exception as e:
                print(f"⚠️ 加载配置文件失败: {e}")
                return self._get_default_config()
        else:
            return self._get_default_config()
    
    def _get_default_config(self) -> Dict[str, Any]:
        """获取默认配置"""
        return {
            "fixedParameters": {},
            "history": {}
        }
    
    def _save_config(self):
        """保存配置文件"""
        os.makedirs(os.path.dirname(self.config_path), exist_ok=True)
        with open(self.config_path, 'w', encoding='utf-8') as f:
            json.dump(self.config, f, ensure_ascii=False, indent=2)
    
    def get_parameter(self, key: str, generated_value: Any) -> Any:
        """
        获取参数值，如果已记忆则返回正确值，否则返回生成值
        
        Args:
            key: 参数键名
            generated_value: AI生成的参数值
            
        Returns:
            最终使用的参数值
        """
        # 如果已在固定参数中，直接返回记忆的值
        if key in self.config["fixedParameters"]:
            remembered_value = self.config["fixedParameters"][key]
            if str(generated_value) != str(remembered_value):
                print(f"使用记忆参数 {key}: {remembered_value} (而非生成值: {generated_value})")
            return remembered_value
        
        # 否则返回生成值
        return generated_value
    
    def record_error(self, key: str, wrong_value: Any, correct_value: Any) -> bool:
        """
        记录参数错误
        
        Args:
            key: 参数键名
            wrong_value: 错误的值
            correct_value: 正确的值
            
        Returns:
            是否达到记忆阈值并已自动记忆
        """
        # 如果已经在固定参数中，则不需要再记录
        if key in self.config["fixedParameters"]:
            return False
        
        # 初始化历史记录
        if key not in self.config["history"]:
            self.config["history"][key] = []
        
        # 查找是否已有相同的错误记录
        existing_record = None
        for record in self.config["history"][key]:
            if str(record["wrong"]) == str(wrong_value) and str(record["correct"]) == str(correct_value):
                existing_record = record
                break
        
        if existing_record:
            # 增加错误次数
            existing_record["count"] += 1
            current_count = existing_record["count"]
        else:
            # 创建新的错误记录
            new_record = {
                "wrong": wrong_value,
                "correct": correct_value,
                "count": 1,
                "first_seen": datetime.now().isoformat(),
                "last_seen": datetime.now().isoformat()
            }
            self.config["history"][key].append(new_record)
            current_count = 1
        
        # 更新最后见到时间
        if existing_record:
            existing_record["last_seen"] = datetime.now().isoformat()
        
        print(f"记录参数错误: {key} = {wrong_value} -> {correct_value} (第{current_count}次)")
        
        # 检查是否达到记忆阈值
        if current_count >= 2:
            self._memorize_parameter(key, correct_value, wrong_value, current_count)
            self._save_config()
            self._commit_to_git(key, correct_value)
            return True
        
        self._save_config()
        return False
    
    def _memorize_parameter(self, key: str, correct_value: Any, wrong_value: Any, count: int):
        """将参数加入固定记忆"""
        self.config["fixedParameters"][key] = correct_value
        print(f"已将 {key}:{correct_value} 自动记忆，下一次不会再犯此错 (累计{count}次错误)")
    
    def _commit_to_git(self, key: str, value: Any):
        """提交配置文件到Git"""
        try:
            # 添加配置文件到Git
            subprocess.run(["git", "add", self.config_path], 
                         cwd=os.path.dirname(os.path.abspath(self.config_path)), 
                         check=True)
            
            # 提交更改
            commit_message = f"Claude 自动记忆参数 {key}:{value} 规则"
            subprocess.run(["git", "commit", "-m", commit_message], 
                         cwd=os.path.dirname(os.path.abspath(self.config_path)), 
                         check=True)
            
            print(f"已自动提交参数记忆到Git: {key}:{value}")
            
            # 尝试推送（如果失败也不影响主流程）
            try:
                subprocess.run(["git", "push"], 
                             cwd=os.path.dirname(os.path.abspath(self.config_path)), 
                             check=True, timeout=10)
                print(f"已推送到远程仓库")
            except (subprocess.CalledProcessError, subprocess.TimeoutExpired):
                print(f"推送失败，但本地提交成功")
                
        except subprocess.CalledProcessError as e:
            print(f"Git操作失败: {e}")
    
    def get_memorized_parameters(self) -> Dict[str, Any]:
        """获取所有已记忆的参数"""
        return self.config["fixedParameters"].copy()
    
    def get_error_history(self, key: Optional[str] = None) -> Dict[str, List[Dict]]:
        """获取错误历史记录"""
        if key:
            return {key: self.config["history"].get(key, [])}
        return self.config["history"].copy()
    
    def clear_history(self, key: Optional[str] = None):
        """清空历史记录"""
        if key:
            if key in self.config["history"]:
                del self.config["history"][key]
                print(f"✅ 已清空 {key} 的历史记录")
        else:
            self.config["history"] = {}
            print(f"✅ 已清空所有历史记录")
        
        self._save_config()
    
    def remove_memorized_parameter(self, key: str):
        """移除已记忆的参数"""
        if key in self.config["fixedParameters"]:
            del self.config["fixedParameters"][key]
            print(f"✅ 已移除记忆参数: {key}")
            self._save_config()
        else:
            print(f"⚠️ 参数 {key} 未被记忆")

# 全局实例
memory = ParameterMemory()

def get_parameter(key: str, generated_value: Any) -> Any:
    """便捷函数：获取参数（如果已记忆则使用记忆值）"""
    return memory.get_parameter(key, generated_value)

def record_error(key: str, wrong_value: Any, correct_value: Any) -> bool:
    """便捷函数：记录参数错误"""
    return memory.record_error(key, wrong_value, correct_value)

if __name__ == "__main__":
    # 演示用法
    print("=== 项目记忆·参数自动学习功能演示 ===")
    
    # 获取API版本参数
    api_version = get_parameter("apiVersion", "V1.0")  # 生成了错误值
    print(f"使用API版本: {api_version}")
    
    # 记录错误
    memorized = record_error("apiVersion", "V1.0", "1.0")
    print(f"是否已记忆: {memorized}")
    
    # 再次获取参数
    api_version2 = get_parameter("apiVersion", "V2.0")  # 生成了另一个错误值
    print(f"再次使用API版本: {api_version2}")
    
    # 显示记忆的参数
    print("\n已记忆的参数:", memory.get_memorized_parameters())
    print("错误历史:", memory.get_error_history())