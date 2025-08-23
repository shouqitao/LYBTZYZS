#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DI 依赖注入诊断脚本
分析 MainWindowViewModel 的依赖项是否都已正确注册
"""

import re
import os

def analyze_main_window_dependencies():
    """分析MainWindowViewModel的构造函数依赖项"""
    
    # 读取MainWindowViewModel
    main_window_path = "src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs"
    if not os.path.exists(main_window_path):
        print(f"文件不存在: {main_window_path}")
        return
    
    with open(main_window_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 提取构造函数参数
    constructor_match = re.search(r'public MainWindowViewModel\((.*?)\)', content, re.DOTALL)
    if not constructor_match:
        print("找不到MainWindowViewModel构造函数")
        return
    
    constructor_params = constructor_match.group(1)
    print("MainWindowViewModel 构造函数参数:")
    print("=" * 50)
    
    # 解析参数
    params = []
    for line in constructor_params.split('\n'):
        line = line.strip()
        if line and not line.startswith('//'):
            # 移除末尾逗号
            line = line.rstrip(',').strip()
            if ' ' in line:
                parts = line.split()
                if len(parts) >= 2:
                    type_name = parts[-2]  # 倒数第二个是类型
                    param_name = parts[-1]  # 最后一个是参数名
                    params.append((type_name, param_name))
                    print(f"  {len(params)}. {type_name} {param_name}")
    
    print(f"\n总共 {len(params)} 个依赖项")
    
    # 检查ServiceCollectionExtensions
    service_ext_path = "src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs"
    if not os.path.exists(service_ext_path):
        print(f"文件不存在: {service_ext_path}")
        return
    
    with open(service_ext_path, 'r', encoding='utf-8') as f:
        service_content = f.read()
    
    print("\n检查依赖项注册状态:")
    print("=" * 50)
    
    for i, (type_name, param_name) in enumerate(params, 1):
        if check_registration(service_content, type_name):
            print(f"  OK {i}. {type_name} - 已注册")
        else:
            print(f"  !! {i}. {type_name} - 未找到注册")
    
    # 检查Prism内置服务
    prism_services = ['IRegionManager', 'IEventAggregator']
    print(f"\nPrism内置服务 (应自动提供):")
    print("=" * 30)
    for service in prism_services:
        print(f"  PRISM {service} - Prism框架自动提供")

def check_registration(service_content, type_name):
    """检查类型是否在ServiceCollectionExtensions中注册"""
    
    # 检查完整类型名注册
    if f"<{type_name}>" in service_content or f"<{type_name}," in service_content:
        return True
    
    # 检查接口注册模式
    interface_patterns = [
        f"RegisterSingleton<.*{type_name}",
        f"Register<.*{type_name}",
        f"RegisterInstance<.*{type_name}",
        f"{type_name}.*>",
    ]
    
    for pattern in interface_patterns:
        if re.search(pattern, service_content):
            return True
    
    return False

if __name__ == "__main__":
    print("DI 依赖注入诊断分析")
    print("=" * 60)
    analyze_main_window_dependencies()