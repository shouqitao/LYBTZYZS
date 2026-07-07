import subprocess

result = subprocess.run([
    'sqlcmd', '-S', '192.168.190.243', '-U', 'sa', '-P', 'Shou@850528',
    '-d', 'LYBTDB_Dev', '-Q',
    "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AspNetUsers' AND COLUMN_NAME IN ('Role','Status')"
], capture_output=True, text=True, timeout=10)

print(result.stdout)
if result.stderr:
    print("STDERR:", result.stderr)
