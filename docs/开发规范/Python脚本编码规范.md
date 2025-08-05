# Python 脚本编码规范

## 一、编码格式要求

### 1.1 文件编码

所有 Python 脚本必须使用 **UTF-8** 编码，并在文件开头声明：

```python
# -*- coding: utf-8 -*-
```

或者（Python 3 默认）：

```python
# coding: utf-8
```

### 1.2 输出编码设置

在 Windows 环境下运行 Python 脚本时，需要特别注意控制台输出的编码问题：

```python
import sys
import io

# 设置标准输出编码为 UTF-8
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')
```

或者使用环境变量：

```python
import os
os.environ['PYTHONIOENCODING'] = 'utf-8'
```

### 1.3 文件读写编码

读写文件时必须明确指定编码：

```python
# 读取文件
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 写入文件
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
```

## 二、Windows 环境特殊处理

### 2.1 控制台输出中文

Windows 控制台默认使用 GBK 编码，可能导致中文乱码。解决方案：

```python
import locale
import sys

# 方法1：检测系统编码并相应处理
system_encoding = locale.getpreferredencoding()
print(f"系统默认编码: {system_encoding}")

# 方法2：强制使用 UTF-8（推荐）
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')
```

### 2.2 避免特殊字符

在输出中避免使用可能导致编码错误的特殊字符：

```python
# 避免使用
print("✓ 成功")  # 可能导致编码错误
print("✗ 失败")  # 可能导致编码错误

# 推荐使用
print("[成功] 操作完成")
print("[失败] 操作失败")
print("√ 成功")  # 使用 ASCII 兼容字符
print("× 失败")  # 使用 ASCII 兼容字符
```

## 三、完整脚本模板

### 3.1 基础模板

```python
#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
脚本说明文档
"""

import sys
import io
import os

# Windows 环境编码设置
if sys.platform == 'win32':
    # 设置控制台输出编码
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')
    # 设置环境变量
    os.environ['PYTHONIOENCODING'] = 'utf-8'

def main():
    """主函数"""
    print("开始执行脚本...")
    # 脚本逻辑
    print("脚本执行完成！")

if __name__ == '__main__':
    main()
```

### 3.2 文件处理模板

```python
#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
文件处理脚本模板
"""

import sys
import io
import os
import re

# Windows 环境编码设置
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

def process_file(file_path):
    """处理单个文件"""
    try:
        # 读取文件
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 处理内容
        modified_content = content  # 你的处理逻辑
        
        # 写入文件
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(modified_content)
        
        print(f"[成功] 处理文件: {file_path}")
        return True
        
    except Exception as e:
        print(f"[错误] 处理文件失败 {file_path}: {str(e)}")
        return False

def main():
    """主函数"""
    print("=" * 50)
    print("文件处理脚本")
    print("=" * 50)
    
    # 处理文件
    files_to_process = [
        'file1.txt',
        'file2.txt'
    ]
    
    success_count = 0
    for file_path in files_to_process:
        if os.path.exists(file_path):
            if process_file(file_path):
                success_count += 1
    
    print("=" * 50)
    print(f"处理完成！成功: {success_count}, 总计: {len(files_to_process)}")

if __name__ == '__main__':
    main()
```

## 四、代码重构脚本模板

### 4.1 控制器重构脚本

```python
#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
控制器代码重构脚本
"""

import sys
import io
import os
import re

# Windows 环境编码设置
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

def refactor_controller(file_path, controller_name):
    """重构控制器文件"""
    try:
        # 读取文件内容
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 执行重构
        modified = False
        
        # 示例：替换返回类型
        pattern = r'public async Task<ActionResult<object>>'
        replacement = r'public async Task<ActionResult<ResourceDto>>'
        
        if pattern in content:
            content = re.sub(pattern, replacement, content)
            modified = True
        
        # 保存修改
        if modified:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"[成功] {controller_name} 重构完成")
            return True
        else:
            print(f"[跳过] {controller_name} 无需修改")
            return False
            
    except Exception as e:
        print(f"[错误] 处理 {controller_name} 失败: {str(e)}")
        return False

def main():
    """主函数"""
    print("开始重构控制器...")
    print("=" * 50)
    
    controllers_dir = r'D:\source\repos\LYBTZYZS\src\Backend\Services\LYBT.WebAPI\Controllers'
    
    controllers = [
        'DiagnosisTreatmentController.cs',
        'PrescriptionsController.cs',
        # 添加更多控制器
    ]
    
    success_count = 0
    for controller_file in controllers:
        file_path = os.path.join(controllers_dir, controller_file)
        controller_name = controller_file.replace('.cs', '')
        
        print(f"\n处理 {controller_name}:")
        if os.path.exists(file_path):
            if refactor_controller(file_path, controller_name):
                success_count += 1
        else:
            print(f"[警告] 文件不存在: {file_path}")
    
    print("\n" + "=" * 50)
    print(f"重构完成！成功: {success_count}, 总计: {len(controllers)}")

if __name__ == '__main__':
    main()
```

## 五、常见问题解决

### 5.1 UnicodeEncodeError

错误信息：
```
UnicodeEncodeError: 'gbk' codec can't encode character '\u2713' in position 2
```

解决方案：
1. 确保脚本开头有编码声明
2. 设置标准输出编码为 UTF-8
3. 避免使用不兼容的特殊字符

### 5.2 文件读写乱码

问题：读取或写入中文文件时出现乱码

解决方案：
```python
# 始终明确指定 encoding='utf-8'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()
```

### 5.3 正则表达式中文匹配

```python
import re

# 匹配中文字符
chinese_pattern = r'[\u4e00-\u9fa5]+'

# 在包含中文的字符串中使用
text = "编辑诊疗成功"
if re.search(chinese_pattern, text):
    print("包含中文")
```

## 六、最佳实践

1. **始终使用 UTF-8 编码**
2. **在 Windows 环境下设置控制台编码**
3. **文件操作时明确指定编码**
4. **避免使用可能导致编码错误的特殊字符**
5. **在脚本开始处统一处理编码设置**
6. **捕获并处理编码相关的异常**
7. **测试脚本在不同环境下的兼容性**

## 七、推荐的 VS Code 设置

在项目的 `.vscode/settings.json` 中添加：

```json
{
    "files.encoding": "utf8",
    "python.linting.enabled": true,
    "python.linting.pylintEnabled": true,
    "[python]": {
        "files.encoding": "utf8"
    }
}
```

这样可以确保所有 Python 文件都使用 UTF-8 编码。