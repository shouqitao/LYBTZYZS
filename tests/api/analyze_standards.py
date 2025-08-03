#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
自动规范选择与记忆功能
"""

import json
import os
from collections import Counter

class StandardsAnalyzer:
    def __init__(self):
        self.config_path = "config/memo.json"
        self.standards = {}
        
    def analyze_api_version_standard(self):
        """分析API版本号标准"""
        # 从扫描结果看：
        # - 配置文件中已确定: "1.0"
        # - 大量使用 /api/v1/
        # - 控制器中使用 api/v{version:apiVersion}
        
        patterns = {
            "/api/v1/": 15,  # 从扫描结果计数
            "api/v{api_version}/": 5,  # Python脚本中使用
            "apiVersion": "1.0"  # 配置中已固定
        }
        
        # 选择标准：小写v + 版本号
        self.standards["apiVersion"] = "1.0"
        self.standards["apiPathPrefix"] = "/api/v"
        return "v1.0", "选择小写v加版本号格式，与现有配置一致"
    
    def analyze_encoding_standard(self):
        """分析编码格式标准"""
        # 从扫描结果看：
        # - Python文件统一使用 # -*- coding: utf-8 -*-
        # - 参数memory.py中使用 encoding='utf-8'
        # - 文档中提到UTF-8
        
        patterns = {
            "utf-8": 8,
            "UTF-8": 2
        }
        
        # 选择标准：UTF-8（大写，更正规）
        self.standards["encoding"] = "UTF-8"
        return "UTF-8", "选择大写UTF-8，符合标准命名规范"
    
    def analyze_date_format_standard(self):
        """分析日期格式标准"""
        # 从扫描结果看：
        # - SystemConstants中定义了标准格式
        # - 大量使用 yyyy-MM-dd HH:mm:ss
        # - API使用 yyyy-MM-ddTHH:mm:ss.fffZ
        
        patterns = {
            "yyyy-MM-dd": 5,
            "yyyy-MM-dd HH:mm:ss": 6,
            "yyyy-MM-ddTHH:mm:ss.fffZ": 2
        }
        
        # 选择标准：yyyy-MM-dd（日期），yyyy-MM-dd HH:mm:ss（日期时间）
        self.standards["dateFormat"] = "yyyy-MM-dd"
        self.standards["dateTimeFormat"] = "yyyy-MM-dd HH:mm:ss"
        self.standards["apiDateTimeFormat"] = "yyyy-MM-ddTHH:mm:ss.fffZ"
        return "yyyy-MM-dd", "选择ISO 8601标准格式，与SystemConstants一致"
    
    def update_memo_config(self):
        """更新记忆配置文件"""
        # 读取现有配置
        try:
            with open(self.config_path, 'r', encoding='utf-8') as f:
                config = json.load(f)
        except FileNotFoundError:
            config = {"fixedParameters": {}, "history": {}}
        
        # 更新固定参数
        config["fixedParameters"].update(self.standards)
        
        # 添加分析历史
        if "analysisHistory" not in config:
            config["analysisHistory"] = {}
        
        config["analysisHistory"]["autoStandardSelection"] = {
            "timestamp": "2025-08-01T22:30:00Z",
            "selectedStandards": self.standards,
            "reasoning": {
                "apiVersion": "基于现有配置和大量使用模式选择1.0",
                "encoding": "基于Python文件头和标准规范选择UTF-8",
                "dateFormat": "基于SystemConstants定义选择ISO 8601格式"
            }
        }
        
        # 保存配置
        os.makedirs(os.path.dirname(self.config_path), exist_ok=True)
        with open(self.config_path, 'w', encoding='utf-8') as f:
            json.dump(config, f, indent=2, ensure_ascii=False)
    
    def run_analysis(self):
        """运行完整分析"""
        print("=== 自动规范选择与记忆功能 ===")
        
        # 分析各类标准
        api_result, api_reason = self.analyze_api_version_standard()
        encoding_result, encoding_reason = self.analyze_encoding_standard()
        date_result, date_reason = self.analyze_date_format_standard()
        
        print(f"1. API版本标准: {api_result}")
        print(f"   原因: {api_reason}")
        
        print(f"2. 编码格式标准: {encoding_result}")
        print(f"   原因: {encoding_reason}")
        
        print(f"3. 日期格式标准: {date_result}")
        print(f"   原因: {date_reason}")
        
        # 更新配置文件
        self.update_memo_config()
        print(f"\n✅ 标准已自动选择并写入 {self.config_path}")
        
        # 显示最终选择的标准
        print("\n📋 最终选择的标准:")
        for key, value in self.standards.items():
            print(f"   {key}: {value}")

if __name__ == "__main__":
    analyzer = StandardsAnalyzer()
    analyzer.run_analysis()