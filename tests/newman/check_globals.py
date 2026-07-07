import json

with open('D:/source/repos/LYBTZYZS/tests/newman/globals.json', 'r', encoding='utf-8') as f:
    g = json.load(f)

for v in g.get('values', []):
    val = v.get('value', '')
    print(f"{v['key']}: {val[:50]}...")
