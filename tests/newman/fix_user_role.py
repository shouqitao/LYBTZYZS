import pymssql

conn = pymssql.connect(server='192.168.190.243', user='sa', password='Shou@850528', database='LYBTDB_Dev')
cursor = conn.cursor()

# Update newadmin's role from Doctor(1) to Admin(10)
cursor.execute("UPDATE Users SET Role = 10 WHERE UserName = 'newadmin'")
print(f"Updated {cursor.rowcount} rows")

conn.commit()

# Verify
cursor.execute("SELECT UserName, Role FROM Users WHERE UserName = 'newadmin'")
row = cursor.fetchone()
print(f"newadmin role: {row[1]} (expected 10)")

conn.close()
