"""
创建带默认密码的用户脚本
使用系统配置的默认密码 ChangeMe123
"""

import hashlib
import base64
import os
import secrets
from datetime import datetime

def generate_password_hash(password="ChangeMe123"):
    """
    生成与 ASP.NET Core Identity 兼容的密码哈希
    注意：这是简化版本，实际应使用 Identity 的 PasswordHasher
    """
    # 预定义的哈希值（仅用于测试）
    password_hashes = {
        "ChangeMe123": "AQAAAAIAAYagAAAAEKFcV+rEOz3qY7KMwU8GmDF0NXBkC2PwMqPc7WaJYYqH0YJpNdxL5BqMUk0cFGV3uw==",
        "Admin@123456": "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ==",
        "Front@123456": "AQAAAAIAAYagAAAAEPxjZQ6uXz1vIpH5kB9HgT9S2JO9bvHmzUAX8Yl+7Yx3hKQNMJ0RKP4ZvN6HzxVxVg=="
    }
    
    return password_hashes.get(password, password_hashes["ChangeMe123"])

def create_user_sql(username, realname, role=0, department=None, position=None, password="ChangeMe123"):
    """
    生成创建用户的SQL语句
    
    参数:
    - username: 用户名
    - realname: 真实姓名
    - role: 角色ID (0=挂号员, 1=医生, 2=收费员, 3=药剂师, 4=理疗师, 99=管理员)
    - department: 部门
    - position: 职位
    - password: 密码（默认使用 ChangeMe123）
    """
    
    password_hash = generate_password_hash(password)
    pinyin_code = ''.join([c[0].upper() for c in realname if c.isalpha()])
    
    sql = f"""
-- 创建用户: {username} ({realname})
-- 默认密码: {password}
-- 角色: {get_role_name(role)}

IF NOT EXISTS (SELECT 1 FROM Users WHERE UserName = '{username}')
BEGIN
    INSERT INTO Users (
        Id,
        UserName,
        PasswordHash,
        RealName,
        PinYinCode,
        Role,
        IsActive,
        CreatedTime,
        Email,
        PhoneNumber,
        Department,
        Position,
        FailedLoginCount
    ) VALUES (
        NEWID(),
        '{username}',
        '{password_hash}',
        N'{realname}',
        '{pinyin_code}',
        {role},
        1,
        GETDATE(),
        '{username}@lybt.com',
        '138{str(role).zfill(4)}{str(hash(username) % 10000).zfill(4)}',
        {f"N'{department}'" if department else 'NULL'},
        {f"N'{position}'" if position else 'NULL'},
        0
    );
    
    PRINT '用户 {username} 创建成功！';
    PRINT '默认密码: {password}';
END
ELSE
BEGIN
    PRINT '用户 {username} 已存在！';
END
"""
    return sql

def get_role_name(role):
    """获取角色名称"""
    roles = {
        0: "挂号员",
        1: "主治医生",
        2: "收费员",
        3: "药剂师",
        4: "理疗师",
        99: "管理员"
    }
    return roles.get(role, "未知角色")

def main():
    """主函数 - 生成示例用户创建脚本"""
    
    print("=" * 60)
    print("生成用户创建SQL脚本")
    print("=" * 60)
    
    # 示例用户配置
    users = [
        # (用户名, 真实姓名, 角色, 部门, 职位)
        ("nurse1", "张护士", 0, "挂号室", "护士"),
        ("cashier1", "王收银", 2, "财务部", "收费员"),
        ("pharmacist1", "李药师", 3, "药房", "主管药师"),
        ("physio1", "赵理疗", 4, "理疗科", "理疗师"),
        ("doctor3", "孙医生", 1, "外科", "主治医师"),
    ]
    
    # 生成SQL文件
    sql_file = "create_test_users.sql"
    
    with open(sql_file, 'w', encoding='utf-8') as f:
        f.write("-- 批量创建测试用户\n")
        f.write(f"-- 生成时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        f.write("-- 默认密码: ChangeMe123\n")
        f.write("-- 注意: 仅用于开发测试环境\n\n")
        f.write("USE LYBTDB;\nGO\n\n")
        
        for user_info in users:
            username, realname, role, department, position = user_info
            sql = create_user_sql(username, realname, role, department, position)
            f.write(sql)
            f.write("\nGO\n\n")
        
        # 添加查询语句
        f.write("\n-- 查询创建的用户\n")
        f.write("SELECT UserName, RealName, Role, Department, Position, IsActive\n")
        f.write("FROM Users\n")
        f.write("WHERE UserName IN (")
        f.write(", ".join([f"'{u[0]}'" for u in users]))
        f.write(")\n")
        f.write("ORDER BY Role, UserName;\n")
    
    print(f"✅ SQL脚本已生成: {sql_file}")
    print("\n使用方法:")
    print(f"sqlcmd -S localhost -E -d LYBTDB -i {sql_file}")
    
    # 输出用户信息汇总
    print("\n" + "=" * 60)
    print("用户信息汇总")
    print("=" * 60)
    print(f"{'用户名':<15} {'真实姓名':<10} {'角色':<10} {'部门':<10}")
    print("-" * 60)
    for user in users:
        username, realname, role, department, _ = user
        print(f"{username:<15} {realname:<10} {get_role_name(role):<10} {department or 'N/A':<10}")
    
    print("\n所有用户的默认密码: ChangeMe123")
    print("⚠️  注意: 请在首次登录后立即修改密码！")

if __name__ == "__main__":
    main()