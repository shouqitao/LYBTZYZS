import json

with open('D:/source/repos/LYBTZYZS/tests/newman/exported.json', 'r', encoding='utf-8') as f:
    c = json.load(f)

for v in c.get('variable', []):
    val = str(v.get('value', ''))[:50]
    print(f"{v['key']}: {val}")
