import pymssql

conn = pymssql.connect(server='192.168.190.243', user='sa', password='Shou@850528', database='LYBTDB_Dev')
cursor = conn.cursor()
cursor.execute("SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId")
rows = cursor.fetchall()
print("Applied migrations:")
for row in rows:
    print(f"  {row[0]}")
conn.close()
