#!/bin/bash
# 凌隐宝堂数据库容器启动脚本 - UltraThink重构数据库容器化

set -e

# 启动SQL Server在后台
echo "启动SQL Server..."
/opt/mssql/bin/sqlservr &

# 等待SQL Server启动
echo "等待SQL Server启动..."
sleep 30

# 检查SQL Server是否启动成功
echo "检查SQL Server状态..."
counter=1
while [ $counter -le 30 ]
do
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1
    if [ $? -eq 0 ]
    then
        echo "SQL Server已启动成功"
        break
    else
        echo "等待SQL Server启动... ($counter/30)"
        sleep 2
        counter=$((counter+1))
    fi
done

if [ $counter -gt 30 ]
then
    echo "错误: SQL Server启动超时"
    exit 1
fi

# 执行数据库初始化脚本
echo "执行数据库初始化..."
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -i /var/opt/mssql/scripts/init-database.sql

if [ $? -eq 0 ]
then
    echo "数据库初始化完成"
else
    echo "错误: 数据库初始化失败"
    exit 1
fi

# 创建备份目录
echo "创建备份目录..."
mkdir -p /var/opt/mssql/backup
chmod 777 /var/opt/mssql/backup

# 设置数据库备份计划（可选）
echo "配置数据库维护..."
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "
-- 创建备份计划
BACKUP DATABASE [LYBTDB] TO DISK = N'/var/opt/mssql/backup/LYBTDB_Initial.bak' 
WITH FORMAT, INIT, NAME = N'LYBTDB-Initial Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10;

-- 更新统计信息
USE [LYBTDB];
UPDATE STATISTICS;
"

echo "数据库配置完成"

# 显示数据库信息
echo "数据库信息:"
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "
SELECT 
    name as DatabaseName,
    database_id as ID,
    create_date as CreatedDate,
    collation_name as Collation
FROM sys.databases 
WHERE name = 'LYBTDB';
"

echo "用户信息:"
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "
USE [LYBTDB];
SELECT 
    dp.name AS UserName,
    dp.type_desc AS UserType,
    dp.create_date AS CreatedDate
FROM sys.database_principals dp 
WHERE dp.type IN ('U', 'S') 
AND dp.name NOT IN ('guest', 'dbo', 'information_schema', 'sys');
"

echo "==============================================="
echo "凌隐宝堂数据库容器启动完成！"
echo "数据库: LYBTDB"
echo "管理员账户: sa / $SA_PASSWORD"
echo "应用账户: lybt_app_user / LybtApp@2024!"
echo "默认系统管理员: sysadmin / Admin@123456"
echo "==============================================="

# 保持SQL Server运行
wait