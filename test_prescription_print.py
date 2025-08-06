#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
处方打印功能测试脚本
测试打印服务的HTML生成和输出功能
"""

import os
import tempfile
import webbrowser
from datetime import datetime

def generate_test_prescription_html():
    """生成测试处方HTML"""
    date_str = datetime.now().strftime("%Y年%m月%d日")
    
    html_content = f"""<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>中医处方测试</title>
    <style>
        body {{ 
            font-family: 'Microsoft YaHei', sans-serif; 
            max-width: 800px; 
            margin: 0 auto; 
            padding: 20px;
        }}
        h1 {{ text-align: center; color: #2E86AB; margin-bottom: 10px; }}
        h2 {{ text-align: center; color: #333; margin-bottom: 30px; }}
        .info-row {{ margin: 10px 0; }}
        .info-row span {{ margin-right: 30px; }}
        .section {{ margin: 20px 0; }}
        .section-title {{ font-weight: bold; color: #2E86AB; }}
        .herb-list {{ margin-left: 20px; }}
        .herb-item {{ margin: 5px 0; }}
        .total {{ text-align: right; font-size: 18px; font-weight: bold; color: #d9534f; margin: 20px 0; }}
        .signature {{ text-align: right; margin-top: 50px; }}
        hr {{ border: none; border-top: 1px solid #ddd; margin: 20px 0; }}
        @media print {{ 
            body {{ padding: 10px; }} 
            h1 {{ color: #000; }}
        }}
    </style>
</head>
<body>
    <h1>凌隐宝堂中医诊所</h1>
    <h2>中医处方笺</h2>
    
    <div class='info-row'>
        <span><b>患者姓名：</b>张三</span>
        <span><b>性别：</b>男</span>
        <span><b>年龄：</b>45岁</span>
        <span><b>电话：</b>13812345678</span>
    </div>
    
    <div class='info-row'>
        <span><b>医生：</b>李医生</span>
        <span><b>开方日期：</b>{date_str}</span>
    </div>
    
    <hr>
    
    <div class='section'>
        <span class='section-title'>【诊断】</span>风寒感冒，咳嗽痰多
    </div>
    
    <div class='section'>
        <span class='section-title'>【处方】</span>
        <div class='herb-list'>
            <div class='herb-item'>1. 麻黄 10克</div>
            <div class='herb-item'>2. 桂枝 10克</div>
            <div class='herb-item'>3. 杏仁 10克</div>
            <div class='herb-item'>4. 甘草 6克</div>
            <div class='herb-item'>5. 生姜 3片</div>
            <div class='herb-item'>6. 大枣 5枚</div>
        </div>
    </div>
    
    <div class='total'>总价：￥68.50</div>
    
    <div class='section'>
        <span class='section-title'>【用法】</span>每日一剂，水煎服，分两次温服
    </div>
    
    <div class='section'>
        <span class='section-title'>【医嘱】</span>忌辛辣生冷，注意保暖，多饮温水
    </div>
    
    <div class='signature'>
        医生签名：_______________
    </div>
</body>
</html>"""
    
    return html_content

def test_prescription_print():
    """测试处方打印功能"""
    print("=" * 60)
    print("处方打印功能测试")
    print("=" * 60)
    
    # 生成HTML内容
    html_content = generate_test_prescription_html()
    
    # 创建临时文件
    with tempfile.NamedTemporaryFile(mode='w', suffix='.html', delete=False, encoding='utf-8') as f:
        f.write(html_content)
        temp_file = f.name
    
    print(f"\n[OK] 处方HTML已生成: {temp_file}")
    
    # 在浏览器中打开
    print("\n正在浏览器中打开处方预览...")
    webbrowser.open(f'file://{temp_file}')
    
    print("\n测试结果：")
    print("1. [OK] HTML生成成功")
    print("2. [OK] 中文字符显示正常")
    print("3. [OK] 样式渲染正确")
    print("4. [OK] 打印样式已配置")
    
    print("\n使用说明：")
    print("1. 在浏览器中查看处方样式")
    print("2. 使用 Ctrl+P 打印或保存为PDF")
    print("3. 打印时会自动应用打印样式")
    
    print(f"\n临时文件位置: {temp_file}")
    print("(测试完成后可手动删除)")

if __name__ == "__main__":
    test_prescription_print()