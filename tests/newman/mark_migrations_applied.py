import pymssql

conn = pymssql.connect(server='192.168.190.243', user='sa', password='Shou@850528', database='LYBTDB_Dev')
cursor = conn.cursor()

migrations_to_mark = [
    '20260617124932_AddIdentityTables',
    '20260703002757_AddMedicalCasePrintLog'
]

for migration_id in migrations_to_mark:
    cursor.execute(
        "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (%s, %s)",
        (migration_id, '8.0.0')
    )
    print(f"Marked as applied: {migration_id}")

conn.commit()
print("All pending migrations marked as applied")

# Verify
cursor.execute("SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId")
rows = cursor.fetchall()
print(f"\nTotal applied migrations: {len(rows)}")
for row in rows:
    print(f"  {row[0]}")

conn.close()
