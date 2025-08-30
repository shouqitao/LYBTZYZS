#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
凌隐宝堂中医诊所管理系统 - 开发环境启动器
"""

import os
import sys
import subprocess
from pathlib import Path

def main():
    """启动开发服务器"""
    print()
    print("="*52)
    print("   凌隐宝堂中医诊所管理系统 - 开发环境启动器")
    print("="*52)
    print()
    
    # 获取项目根目录
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    webapi_dir = project_root / "src" / "Backend" / "Services" / "LYBT.WebAPI"
    
    # 检查项目目录是否存在
    if not webapi_dir.exists():
        print("❌ 错误: 找不到WebAPI项目目录")
        print(f"   期望路径: {webapi_dir}")
        print()
        input("按任意键退出...")
        sys.exit(1)
    
    print(f"📂 项目根目录: {project_root}")
    print(f"🚀 WebAPI目录: {webapi_dir}")
    print()
    
    # 切换到WebAPI目录
    os.chdir(str(webapi_dir))
    
    print("🔄 正在启动开发服务器...")
    print("💡 提示: 按 Ctrl+C 可以停止服务器")
    print()
    
    try:
        # 启动开发服务器
        subprocess.run(["dotnet", "run"], check=True)
    except KeyboardInterrupt:
        print("\n\n⚠️ 收到中断信号，正在停止服务器...")
    except subprocess.CalledProcessError as e:
        print(f"\n❌ 服务器启动失败: {e}")
    finally:
        print("\n🔚 服务器已停止")
        input("按任意键退出...")

if __name__ == "__main__":
    main()