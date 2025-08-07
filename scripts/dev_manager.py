#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LYBT医疗系统 - 开发环境管理器
"""

import os
import sys
import subprocess
import time
import shutil
from pathlib import Path
import psutil

class DevManager:
    def __init__(self):
        self.script_dir = Path(__file__).parent
        self.project_root = self.script_dir.parent
        self.webapi_dir = self.project_root / "src" / "Backend" / "Services" / "LYBT.WebAPI"
        self.wpf_exe = self.project_root / "BIN" / "net8.0-windows" / "LYBT.WPF.Client.Shell.exe"
        
    def clear_screen(self):
        """清屏"""
        os.system('cls' if os.name == 'nt' else 'clear')
    
    def run_command(self, cmd, show_output=True, check=True):
        """运行命令"""
        try:
            if show_output:
                result = subprocess.run(cmd, shell=True, check=check)
            else:
                result = subprocess.run(cmd, shell=True, check=check, 
                                     capture_output=True, text=True)
            return result.returncode == 0
        except subprocess.CalledProcessError:
            return False
    
    def stop_processes(self, silent=False):
        """停止所有LYBT进程"""
        if not silent:
            print("\n📛 正在停止所有LYBT进程...")
        
        print("  - 停止WebAPI进程...")
        # 使用psutil查找并终止进程
        for proc in psutil.process_iter(['pid', 'name']):
            try:
                if 'LYBT.WebAPI' in proc.info['name']:
                    proc.terminate()
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass
        
        print("  - 停止WPF客户端...")
        for proc in psutil.process_iter(['pid', 'name']):
            try:
                if 'LYBT.WPF.Client.Shell' in proc.info['name']:
                    proc.terminate()
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass
        
        print("  - 停止其他LYBT进程...")
        for proc in psutil.process_iter(['pid', 'name']):
            try:
                if 'LYBT' in proc.info['name']:
                    proc.terminate()
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass
        
        time.sleep(1)
        if not silent:
            print("✅ 所有进程已停止")
    
    def build_project(self):
        """编译项目"""
        print("  - 清理编译输出...")
        self.run_command("dotnet clean", show_output=False)
        
        print("  - 正在编译解决方案...")
        return self.run_command("dotnet build")
    
    def start_services(self, silent=False):
        """启动服务"""
        if not silent:
            print("\n🚀 正在启动服务...")
        
        print("  - 启动WebAPI服务...")
        # 在新窗口中启动WebAPI
        os.chdir(str(self.webapi_dir))
        subprocess.Popen(["cmd", "/c", "start", "LYBT WebAPI", "/MIN", "cmd", "/c", "dotnet run"])
        
        print("  - 等待服务初始化...")
        time.sleep(3)
        
        print("  - 启动WPF客户端...")
        if self.wpf_exe.exists():
            subprocess.Popen([str(self.wpf_exe)])
        else:
            print("    ⚠️ WPF客户端未找到，请先编译项目")
        
        if not silent:
            print("✅ 服务启动完成")
    
    def restart_all(self):
        """重启开发环境"""
        print("\n🔄 正在重启LYBT开发环境...")
        print("="*32)
        
        self.stop_processes(silent=True)
        
        if self.build_project():
            self.start_services(silent=True)
            print("✅ 开发环境重启完成！")
        else:
            print("\n❌ 编译失败！")
            print("请检查编译错误信息，修复后重试。")
            input("\n按任意键继续...")
    
    def deep_clean(self):
        """深度清理"""
        print("\n🧹 正在进行深度清理...")
        
        print("  - 停止所有进程...")
        self.stop_processes(silent=True)
        
        print("  - 删除BIN目录...")
        bin_dir = self.project_root / "BIN"
        if bin_dir.exists():
            shutil.rmtree(bin_dir, ignore_errors=True)
        
        print("  - 删除obj目录...")
        for obj_dir in self.project_root.rglob("obj"):
            shutil.rmtree(obj_dir, ignore_errors=True)
        
        print("  - 删除bin目录...")
        for bin_dir in self.project_root.rglob("bin"):
            if bin_dir.parent.name != "scripts":  # 保留scripts下的bin
                shutil.rmtree(bin_dir, ignore_errors=True)
        
        print("  - 清理NuGet缓存...")
        self.run_command("dotnet nuget locals all --clear", show_output=False)
        
        print("  - 重新还原包...")
        self.run_command("dotnet restore", show_output=False)
        
        print("✅ 深度清理完成")
    
    def show_menu(self):
        """显示菜单"""
        while True:
            print("\n ╔══════════════════════════════════════╗")
            print(" ║     LYBT医疗系统 - 开发环境管理器     ║")
            print(" ╚══════════════════════════════════════╝")
            print("\n 请选择操作：")
            print(" [1] 🔄 重启开发环境 (推荐)")
            print(" [2] 📛 仅停止所有进程")
            print(" [3] 🔨 仅重新编译")
            print(" [4] 🚀 启动服务")
            print(" [5] 🧹 深度清理")
            print(" [0] 退出")
            print()
            
            choice = input("请输入选项 (0-5): ").strip()
            
            if choice == '1':
                self.restart_all()
            elif choice == '2':
                self.stop_processes()
            elif choice == '3':
                print("\n🔨 正在编译项目...")
                if self.build_project():
                    print("✅ 编译完成")
                else:
                    print("❌ 编译失败")
                    input("\n按任意键继续...")
            elif choice == '4':
                self.start_services()
            elif choice == '5':
                self.deep_clean()
            elif choice == '0':
                print("\n👋 再见！开发愉快！")
                time.sleep(2)
                break
            else:
                print("无效选项，请重新选择")

def main():
    """主函数"""
    # 检查依赖
    try:
        import psutil
    except ImportError:
        print("需要安装 psutil 库，正在安装...")
        subprocess.run([sys.executable, "-m", "pip", "install", "psutil"])
        print("安装完成，请重新运行脚本")
        sys.exit(1)
    
    try:
        manager = DevManager()
        manager.show_menu()
    except KeyboardInterrupt:
        print("\n\n⚠️ 收到中断信号，正在退出...")
    except Exception as e:
        print(f"\n❌ 发生错误: {e}")
        input("按任意键退出...")

if __name__ == "__main__":
    main()