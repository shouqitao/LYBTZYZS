# LYBTZYZS 脚本目录

本目录包含项目的所有管理脚本，统一使用 Python 编写。

## 核心脚本列表

### 1. 开发环境管理

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