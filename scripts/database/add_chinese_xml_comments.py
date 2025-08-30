import os
import re
import urllib.parse
import json
import requests


def translate(text):
    url = 'https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=zh-CN&dt=t&q=' + urllib.parse.quote(text)
    try:
        r = requests.get(url, timeout=10)
        if r.status_code == 200:
            data = json.loads(r.text)
            return ''.join([item[0] for item in data[0]])
    except Exception:
        pass
    return text


def is_english(text):
    return re.match(r'^[A-Za-z0-9\s,.;:]+$', text) is not None


def process_file(path):
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.read().splitlines()

    changed = False
    new_lines = []
    i = 0
    while i < len(lines):
        line = lines[i]
        if line.strip().startswith('///'):
            m = re.search(r'>([^<>]+)<', line)
            if m:
                txt = m.group(1).strip()
                if txt and is_english(txt):
                    zh = translate(txt)
                    line = line.replace(m.group(1), zh)
                    changed = True
            new_lines.append(line)
            i += 1
            continue

        class_match = re.match(r'\s*(public|internal|private|protected)?\s*(partial\s+)?(class|interface|enum|struct|record)\s+(\w+)', line)
        if class_match:
            if not new_lines or not new_lines[-1].strip().startswith('///'):
                name = class_match.group(4)
                comment = [
                    '/// <summary>',
                    f'/// 表示{name}。',
                    '/// </summary>'
                ]
                new_lines.extend(comment)
                changed = True
            new_lines.append(line)
            i += 1
            continue

        method_match = re.match(r'\s*(public|private|internal|protected)\s+[^=;]+\s+(\w+)\s*\(([^)]*)\)', line)
        if method_match and ('{' in line or (i + 1 < len(lines) and '{' in lines[i+1])):
            if not new_lines or not new_lines[-1].strip().startswith('///'):
                name = method_match.group(2)
                params = [p.strip() for p in method_match.group(3).split(',') if p.strip()]
                comment = ['/// <summary>', f'/// 执行{name}操作。', '/// </summary>']
                for p in params:
                    pname = p.split()[-1]
                    comment.append(f'/// <param name="{pname}">参数{pname}</param>')
                if ' void ' not in line:
                    comment.append('/// <returns>返回值</returns>')
                new_lines.extend(comment)
                changed = True
            new_lines.append(line)
            i += 1
            continue

        property_match = re.match(r'\s*(public|private|internal|protected)\s+[^=]+\s+(\w+)\s*{', line)
        if property_match and (' get;' in line or ' set;' in line or ' get;}' in line or ' set;}' in line):
            if not new_lines or not new_lines[-1].strip().startswith('///'):
                name = property_match.group(2)
                comment = [
                    '/// <summary>',
                    f'/// {name} 属性。',
                    '/// </summary>'
                ]
                new_lines.extend(comment)
                changed = True
            new_lines.append(line)
            i += 1
            continue

        new_lines.append(line)
        i += 1

    if changed:
        with open(path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(new_lines) + '\n')
    return changed


def main():
    for root, _, files in os.walk('.'):
        for file in files:
            if file.endswith('.cs'):
                path = os.path.join(root, file)
                process_file(path)


if __name__ == '__main__':
    main()
