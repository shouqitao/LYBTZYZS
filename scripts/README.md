# LYBTZYZS 脚本目录

本目录包含项目的所有管理脚本，包括 Python 脚本和批处理脚本。

## 🚀 快速开始 - 编译管理

最常用的编译命令：
```batch
# 主编译管理器（推荐）
scripts\build.bat

# 快速编译检查
scripts\build-check.bat
```

## 核心脚本列表

### 1. 编译管理脚本（批处理）

#### build.bat - 🎯 主编译管理器
统一的编译工具入口，提供图形化菜单界面
- **功能**：快速编译、错误分析、自动修复、清理重建
- **使用**：`scripts\build.bat`

#### build-check.bat - ⚡ 快速编译检查
日常开发最常用的编译工具
- **功能**：选择性编译（后端/前端/全部）、错误统计
- **使用**：
  ```batch
  scripts\build-check.bat      # 交互式
  scripts\build-check.bat 1    # 编译后端
  scripts\build-check.bat 2    # 编译前端
  scripts\build-check.bat 3    # 编译全部
  ```

#### build-analyze.bat - 🔍 深度错误分析
详细分析编译错误并生成报告
- **功能**：错误分类、统计分析、生成报告
- **输出**：`build-report.txt`、`temp\*.log`
- **使用**：`scripts\build-analyze.bat`

#### quick-fix.bat - 🔧 自动修复工具
自动修复常见编译错误
- **功能**：
  1. 修复属性名不匹配
  2. 修复中文编码问题
  3. 修复命名空间引用
  4. 清理和重建
- **使用**：`scripts\quick-fix.bat`

### 2. 开发环境管理（Python）

#### start_dev.py
启动开发服务器
```bash
python start_dev.py
```

#### dev_manager.py
开发环境综合管理工具，提供以下功能：
- 重启开发环境
- 停止所有进程
- 重新编译项目
- 启动服务
- 深度清理
```bash
python dev_manager.py
```

### 2. 数据库管理

#### database_manager.py
数据库综合管理工具，提供以下功能：
- 查看数据库状态
- 应用待处理的迁移
- 创建新的迁移
- 回滚迁移
- 重建数据库
- 备份数据库
- 生成数据库脚本
```bash
python database_manager.py
```

### 3. 发布部署

#### publish_production.py
生产环境发布工具，用于：
- 编译Release版本
- 发布到指定目录
- 创建启动脚本
- 生成配置模板
```bash
python publish_production.py
```

## 使用说明

1. **Python 环境要求**
   - Python 3.7 或更高版本
   - 需要安装 psutil 库（dev_manager.py 使用）
   ```bash
   pip install psutil
   ```

2. **脚本执行**
   - 所有脚本都可以直接使用 Python 运行
   - 脚本会自动检测项目路径
   - 提供交互式菜单界面

3. **注意事项**
   - 临时脚本使用后会自动删除
   - 所有脚本都使用 UTF-8 编码
   - 脚本运行时会显示中文提示信息

## SQL 脚本

sql 子目录包含数据库初始化和维护脚本：
- 用户创建脚本
- 表结构修改脚本
- 测试数据插入脚本

## 维护指南

1. **添加新脚本**
   - 使用 Python 3 编写
   - 添加完整的文档字符串
   - 使用 UTF-8 编码
   - 遵循 PEP 8 规范

2. **脚本命名**
   - 使用小写字母和下划线
   - 名称要清晰表达功能
   - 例：`build_project.py`、`clean_cache.py`

3. **错误处理**
   - 捕获并处理异常
   - 提供清晰的错误信息
   - 支持 Ctrl+C 中断