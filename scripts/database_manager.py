#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
凌隐宝堂中医诊所管理系统 - 数据库管理工具
"""

import os
import sys
import subprocess
import shutil
from pathlib import Path
from datetime import datetime

class DatabaseManager:
    def __init__(self):
        # 获取项目路径
        self.script_dir = Path(__file__).parent
        self.project_root = self.script_dir.parent
        self.webapi_dir = self.project_root / "src" / "Backend" / "Services" / "LYBT.WebAPI"
        self.infrastructure_dir = self.project_root / "src" / "Backend" / "Core" / "LYBT.Infrastructure"
        
    def run_command(self, cmd, check=True):
        """运行命令并返回结果"""
        try:
            result = subprocess.run(cmd, shell=True, check=check, capture_output=True, text=True, encoding='utf-8')
            print(result.stdout)
            if result.stderr:
                print(result.stderr)
            return result.returncode == 0
        except subprocess.CalledProcessError as e:
            print(f"❌ 命令执行失败: {e}")
            if e.stdout:
                print(e.stdout)
            if e.stderr:
                print(e.stderr)
            return False
    
    def ef_command(self, command):
        """执行Entity Framework命令"""
        os.chdir(str(self.webapi_dir))
        full_command = f'dotnet ef {command} --project "{self.infrastructure_dir}" --startup-project "{self.webapi_dir}"'
        return self.run_command(full_command)
    
    def check_database(self):
        """检查数据库状态"""
        print("\n🔍 检查数据库状态...")
        self.ef_command("database list")
        print("\n📊 迁移历史:")
        self.ef_command("migrations list")
        input("\n按任意键继续...")
    
    def update_database(self):
        """应用待处理的迁移"""
        print("\n🔄 应用待处理的迁移...")
        if self.ef_command("database update"):
            print("✅ 迁移应用成功！")
        else:
            print("❌ 迁移应用失败！")
        input("\n按任意键继续...")
    
    def add_migration(self):
        """创建新的迁移"""
        print()
        migration_name = input("请输入迁移名称: ").strip()
        if not migration_name:
            print("迁移名称不能为空")
            return
        
        print(f"\n📝 创建迁移: {migration_name}")
        if self.ef_command(f'migrations add "{migration_name}"'):
            print("✅ 迁移创建成功！")
            print("💡 使用选项2应用此迁移到数据库")
        else:
            print("❌ 迁移创建失败！")
        input("\n按任意键继续...")
    
    def rollback_migration(self):
        """回滚到上一个迁移"""
        print("\n⚠️  警告: 回滚迁移可能会导致数据丢失！")
        confirm = input("确定要回滚吗? (Y/N): ").strip().upper()
        if confirm != 'Y':
            return
        
        print("\n📋 当前迁移列表:")
        self.ef_command("migrations list")
        print()
        target_migration = input("请输入要回滚到的迁移名称 (留空回滚到上一个): ").strip()
        
        if target_migration:
            success = self.ef_command(f'database update "{target_migration}"')
        else:
            success = self.ef_command("database update")
        
        if success:
            print("✅ 数据库回滚成功！")
        else:
            print("❌ 数据库回滚失败！")
        input("\n按任意键继续...")
    
    def rebuild_database(self):
        """完全重建数据库"""
        print("\n⚠️⚠️⚠️  警告 ⚠️⚠️⚠️")
        print("此操作将:")
        print("1. 删除整个数据库")
        print("2. 删除所有迁移文件")
        print("3. 重新创建初始迁移")
        print("4. 重建数据库")
        print("\n所有数据将永久丢失！\n")
        
        confirm1 = input("确定要继续吗? (输入 YES 确认): ").strip()
        if confirm1 != "YES":
            return
        
        confirm2 = input("最后确认: 真的要删除所有数据吗? (输入 DELETE 确认): ").strip()
        if confirm2 != "DELETE":
            return
        
        print("\n🗑️  步骤1: 删除数据库...")
        self.ef_command("database drop --force")
        
        print("📁 步骤2: 删除迁移文件...")
        migrations_dir = self.infrastructure_dir / "Migrations"
        if migrations_dir.exists():
            shutil.rmtree(migrations_dir)
            print("迁移文件已删除")
        
        print("📝 步骤3: 创建初始迁移...")
        self.ef_command("migrations add InitialCreate")
        
        print("🔄 步骤4: 创建数据库...")
        if self.ef_command("database update"):
            print("✅ 数据库重建完成！")
        else:
            print("❌ 数据库重建失败！")
        input("\n按任意键继续...")
    
    def backup_database(self):
        """备份数据库"""
        print("\n💾 数据库备份功能...")
        print("💡 提示: 请使用SQL Server Management Studio或sqlcmd进行数据库备份")
        print("\n示例备份命令:")
        backup_date = datetime.now().strftime("%Y%m%d")
        print(f'sqlcmd -S localhost -Q "BACKUP DATABASE [LYBTDB] TO DISK = \'C:\\Backup\\LYBTDB_{backup_date}.bak\'"')
        input("\n按任意键继续...")
    
    def generate_script(self):
        """生成数据库脚本"""
        print("\n📜 生成数据库脚本...")
        from_migration = input("起始迁移 (留空表示从头开始): ").strip()
        to_migration = input("结束迁移 (留空表示到最新): ").strip()
        
        if from_migration or to_migration:
            cmd = f'migrations script "{from_migration}" "{to_migration}" --output "database-script.sql"'
        else:
            cmd = 'migrations script --output "database-script.sql"'
        
        if self.ef_command(cmd):
            print("✅ 脚本生成成功: database-script.sql")
        else:
            print("❌ 脚本生成失败！")
        input("\n按任意键继续...")
    
    def show_menu(self):
        """显示菜单"""
        while True:
            print("\n" + "="*52)
            print("   凌隐宝堂中医诊所管理系统 - 数据库管理工具")
            print("="*52)
            print("\n请选择操作:\n")
            print("1. 查看数据库状态")
            print("2. 应用待处理的迁移")
            print("3. 创建新的迁移")
            print("4. 回滚到上一个迁移")
            print("5. 完全重建数据库 (⚠️  会删除所有数据)")
            print("6. 备份数据库")
            print("7. 生成数据库脚本")
            print("8. 退出")
            print()
            
            choice = input("请输入选项 (1-8): ").strip()
            
            if choice == '1':
                self.check_database()
            elif choice == '2':
                self.update_database()
            elif choice == '3':
                self.add_migration()
            elif choice == '4':
                self.rollback_migration()
            elif choice == '5':
                self.rebuild_database()
            elif choice == '6':
                self.backup_database()
            elif choice == '7':
                self.generate_script()
            elif choice == '8':
                print("\n👋 再见！")
                break
            else:
                print("无效选项，请重新选择")

def main():
    """主函数"""
    try:
        manager = DatabaseManager()
        manager.show_menu()
    except KeyboardInterrupt:
        print("\n\n⚠️ 收到中断信号，正在退出...")
    except Exception as e:
        print(f"\n❌ 发生错误: {e}")
        input("按任意键退出...")

if __name__ == "__main__":
    main()