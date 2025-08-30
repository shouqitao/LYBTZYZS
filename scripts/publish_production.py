#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
凌隐宝堂中医诊所管理系统 - 生产发布器
"""

import os
import sys
import subprocess
import shutil
from pathlib import Path
from datetime import datetime

class ProductionPublisher:
    def __init__(self):
        self.script_dir = Path(__file__).parent
        self.project_root = self.script_dir.parent
        self.webapi_dir = self.project_root / "src" / "Backend" / "Services" / "LYBT.WebAPI"
        self.publish_dir = self.project_root / "publish"
        self.datetime_str = datetime.now().strftime("%Y%m%d_%H%M%S")
        
    def clean_publish_dir(self):
        """清理发布目录"""
        print("🧹 清理旧的发布文件...")
        if self.publish_dir.exists():
            # 删除特定文件类型
            for pattern in ["*.dll", "*.exe", "*.json", "*.pdb"]:
                for file in self.publish_dir.glob(pattern):
                    file.unlink()
            
            # 删除wwwroot目录
            wwwroot_dir = self.publish_dir / "wwwroot"
            if wwwroot_dir.exists():
                shutil.rmtree(wwwroot_dir)
    
    def publish_application(self):
        """发布应用程序"""
        print("\n🔄 开始发布应用程序...")
        print("⏳ 这可能需要几分钟时间，请耐心等待...\n")
        
        cmd = [
            "dotnet", "publish", str(self.webapi_dir),
            "--configuration", "Release",
            "--output", str(self.publish_dir),
            "--self-contained", "false",
            "--runtime", "win-x64",
            "--verbosity", "minimal"
        ]
        
        result = subprocess.run(cmd)
        return result.returncode == 0
    
    def create_startup_script(self):
        """创建启动脚本"""
        print("📝 创建生产环境启动脚本...")
        
        startup_script = self.publish_dir / "start-production.bat"
        content = """@echo off
chcp 65001 >nul
title 凌隐宝堂中医诊所管理系统 - 生产环境

echo ====================================================
echo    凌隐宝堂中医诊所管理系统 - 生产环境
echo ====================================================
echo.
echo 🚀 正在启动服务器...
echo 💡 提示: 按 Ctrl+C 可以停止服务器
echo 📖 Swagger文档: http://localhost:5000/swagger
echo.

:: 设置生产环境变量
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://localhost:5000

:: 启动应用程序
LYBT.WebAPI.exe

pause
"""
        
        with open(startup_script, 'w', encoding='utf-8') as f:
            f.write(content)
    
    def create_python_startup_script(self):
        """创建Python启动脚本"""
        print("📝 创建Python生产环境启动脚本...")
        
        py_script = self.publish_dir / "start_production.py"
        content = '''#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生产环境启动脚本"""

import os
import subprocess
import sys

def main():
    print("="*52)
    print("   凌隐宝堂中医诊所管理系统 - 生产环境")
    print("="*52)
    print()
    print("🚀 正在启动服务器...")
    print("💡 提示: 按 Ctrl+C 可以停止服务器")
    print("📖 Swagger文档: http://localhost:5000/swagger")
    print()
    
    # 设置环境变量
    os.environ["ASPNETCORE_ENVIRONMENT"] = "Production"
    os.environ["ASPNETCORE_URLS"] = "http://localhost:5000"
    
    try:
        # 启动应用程序
        subprocess.run(["LYBT.WebAPI.exe"])
    except KeyboardInterrupt:
        print("\\n\\n⚠️ 服务器已停止")
    except Exception as e:
        print(f"\\n❌ 启动失败: {e}")
    
    input("\\n按任意键退出...")

if __name__ == "__main__":
    main()
'''
        
        with open(py_script, 'w', encoding='utf-8') as f:
            f.write(content)
    
    def copy_config_template(self):
        """复制配置文件模板"""
        print("📋 创建配置文件模板...")
        
        source_config = self.webapi_dir / "appsettings.json"
        if source_config.exists():
            dest_config = self.publish_dir / "appsettings.Production.json"
            shutil.copy2(source_config, dest_config)
    
    def run(self):
        """执行发布流程"""
        print()
        print("="*52)
        print("   凌隐宝堂中医诊所管理系统 - 生产发布器")
        print("="*52)
        print()
        print(f"📂 项目根目录: {self.project_root}")
        print(f"🎯 发布目录: {self.publish_dir}")
        print(f"📅 发布时间: {self.datetime_str}")
        print()
        
        # 检查项目目录
        if not self.webapi_dir.exists():
            print("❌ 错误: 找不到WebAPI项目目录")
            print(f"   期望路径: {self.webapi_dir}")
            input("按任意键退出...")
            return False
        
        # 创建发布目录
        if not self.publish_dir.exists():
            print("📁 创建发布目录...")
            self.publish_dir.mkdir(parents=True)
        
        # 清理旧文件
        self.clean_publish_dir()
        
        # 发布应用
        if not self.publish_application():
            print("\n❌ 发布失败！")
            print("💡 请检查项目是否可以正常编译")
            input("按任意键退出...")
            return False
        
        print("\n✅ 发布完成！\n")
        print(f"📁 发布文件位置: {self.publish_dir}")
        print(f"🚀 可执行文件: {self.publish_dir / 'LYBT.WebAPI.exe'}\n")
        
        # 创建脚本和配置
        self.create_startup_script()
        self.create_python_startup_script()
        self.copy_config_template()
        
        print("\n🎉 发布完成！\n")
        print("📁 文件清单:")
        print("   ├─ LYBT.WebAPI.exe (主程序)")
        print("   ├─ start-production.bat (批处理启动脚本)")
        print("   ├─ start_production.py (Python启动脚本)")
        print("   ├─ appsettings.Production.json (生产配置)")
        print("   └─ ... (其他依赖文件)\n")
        print("🔧 使用说明:")
        print("   1. 编辑 appsettings.Production.json 配置数据库连接")
        print("   2. 双击 start-production.bat 或运行 start_production.py 启动服务器")
        print("   3. 访问 http://localhost:5000/swagger 查看API文档\n")
        
        # 询问是否打开文件夹
        open_folder = input("是否打开发布文件夹? (Y/N): ").strip().upper()
        if open_folder == 'Y':
            os.startfile(str(self.publish_dir))
        
        return True

def main():
    """主函数"""
    try:
        publisher = ProductionPublisher()
        publisher.run()
    except KeyboardInterrupt:
        print("\n\n⚠️ 发布已取消")
    except Exception as e:
        print(f"\n❌ 发生错误: {e}")
        input("按任意键退出...")

if __name__ == "__main__":
    main()