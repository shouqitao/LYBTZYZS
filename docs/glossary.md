# 凌隐宝堂中医诊所管理系统 - 术语表

> 更新时间：2025-01-02  
> 说明：本文档提供系统中使用的专业术语的中英文对照和详细解释

## 一、系统架构术语

| 中文术语 | 英文术语 | 缩写 | 说明 |
|---------|---------|------|------|
| 聚合根 | Aggregate Root | AR | DDD概念，领域模型的根实体，本系统中MedicalCase为聚合根 |
| 分层架构 | Layered Architecture | - | 将系统分为表现层、业务层、数据层的架构模式 |
| 依赖注入 | Dependency Injection | DI | 控制反转的一种实现，用于解耦组件依赖 |
| 仓储模式 | Repository Pattern | - | 数据访问层的抽象，隔离业务逻辑与数据访问 |
| 工作单元 | Unit of Work | UOW | 维护受业务事务影响的对象列表，协调变更写入 |
| 数据传输对象 | Data Transfer Object | DTO | 用于层间数据传输的对象 |
| 视图模型 | View Model | VM | MVVM模式中的视图数据模型 |
| 服务层 | Service Layer | - | 封装业务逻辑的层次 |
| 领域驱动设计 | Domain-Driven Design | DDD | 以领域模型为核心的设计方法 |
| 对象关系映射 | Object-Relational Mapping | ORM | 对象与关系数据库的映射技术 |

## 二、业务领域术语

### 2.1 中医诊疗术语

| 中文术语 | 英文术语 | 拼音码 | 说明 |
|---------|---------|--------|------|
| 病历 | Medical Case | BL | 患者一次就诊的完整记录，系统聚合根 |
| 诊疗 | Consultation | ZL | 医生对患者进行诊断和治疗的过程 |
| 处方 | Prescription | CF | 医生开具的用药方案 |
| 四诊 | Four Examinations | SZ | 望闻问切，中医诊断的四种方法 |
| 望诊 | Inspection | WZ | 观察患者面色、舌苔等外观 |
| 闻诊 | Auscultation & Olfaction | WZ | 听声音、闻气味 |
| 问诊 | Inquiry | WZ | 询问病情、症状 |
| 切诊 | Palpation | QZ | 把脉、触诊 |
| 辨证论治 | Syndrome Differentiation | BZLZ | 根据四诊信息进行中医诊断 |
| 主诉 | Chief Complaint | ZS | 患者的主要症状描述 |
| 现病史 | Present Illness | XBS | 当前疾病的发展过程 |
| 既往史 | Past History | JWS | 过去的疾病史 |
| 过敏史 | Allergy History | GMS | 药物或食物过敏历史 |
| 中医诊断 | TCM Diagnosis | ZYZD | Traditional Chinese Medicine诊断 |
| 治则治法 | Treatment Principle | ZZZF | 治疗原则和方法 |
| 医嘱 | Medical Advice | YZ | 医生的用药和生活指导建议 |

### 2.2 中药相关术语

| 中文术语 | 英文术语 | 拼音码 | 说明 |
|---------|---------|--------|------|
| 中药材 | Herb | ZYC | 中医使用的药材 |
| 方剂 | Formula | FJ | 多味中药的组合配方 |
| 剂量 | Dosage | JL | 药物的用量 |
| 剂数 | Dose Count | JS | 处方的服用次数 |
| 煎服法 | Decoction Method | JSF | 中药的煎煮和服用方法 |
| 适应症 | Indication | SYZ | 药物的适用病症 |
| 药材单价 | Unit Price | YCDJ | 每单位药材的价格 |
| 处方总价 | Total Price | CFZJ | 整个处方的总费用 |
| 折扣 | Discount | ZK | 价格优惠比例 |
| 拼音码 | Pinyin Code | PYM | 用于快速检索的拼音缩写 |
| 用法用量 | Usage & Dosage | YFYL | 药物的使用方法和剂量 |
| 炮制 | Processing | PZ | 中药材的加工处理方法 |

### 2.3 系统功能术语

| 中文术语 | 英文术语 | 说明 |
|---------|---------|------|
| 患者档案 | Patient Archive | 患者的基本信息和病历记录 |
| 诊疗工作台 | Consultation Workstation | 医生进行诊疗的主界面 |
| 系统工作台 | System Workstation | 管理员管理系统的主界面 |
| 快速录入 | Quick Entry | 使用拼音码快速输入处方 |
| 方剂导入 | Formula Import | 从模板导入标准方剂 |
| 历史复制 | History Copy | 复制历史处方进行修改 |
| 批量导入 | Batch Import | Excel批量导入数据 |
| 软删除 | Soft Delete | 标记删除但不物理删除 |
| 当天可改 | Same-day Editable | 当天创建的记录可以修改 |
| 过期锁定 | Expired Locked | 过了当天的记录自动锁定 |

## 三、技术术语

### 3.1 前端技术

| 术语 | 全称 | 说明 |
|-----|------|------|
| WPF | Windows Presentation Foundation | Windows桌面应用开发框架 |
| MVVM | Model-View-ViewModel | 前端架构模式 |
| Prism | - | WPF应用框架，提供模块化、导航等功能 |
| DryIoc | - | 依赖注入容器 |
| Refit | - | REST API客户端库 |
| XAML | Extensible Application Markup Language | WPF界面标记语言 |
| Binding | 数据绑定 | 视图与视图模型的数据同步机制 |
| Command | 命令 | MVVM中的用户交互处理机制 |
| Region | 区域 | Prism中的视图容器概念 |
| Module | 模块 | Prism中的功能模块单元 |

### 3.2 后端技术

| 术语 | 全称 | 说明 |
|-----|------|------|
| ASP.NET Core | - | .NET Web应用开发框架 |
| EF Core | Entity Framework Core | .NET的ORM框架 |
| JWT | JSON Web Token | 身份认证令牌 |
| RefreshToken | 刷新令牌 | 用于刷新JWT的长期令牌 |
| Middleware | 中间件 | 请求处理管道组件 |
| Controller | 控制器 | MVC中处理HTTP请求的组件 |
| Action | 动作 | 控制器中的方法 |
| Filter | 过滤器 | 请求处理的拦截器 |
| Migration | 迁移 | EF Core的数据库版本管理 |
| DbContext | 数据库上下文 | EF Core的数据访问入口 |

### 3.3 数据库术语

| 术语 | 说明 |
|-----|------|
| Primary Key | 主键，唯一标识记录 |
| Foreign Key | 外键，关联其他表 |
| Index | 索引，提高查询性能 |
| Transaction | 事务，保证数据一致性 |
| Stored Procedure | 存储过程，数据库中的程序 |
| View | 视图，虚拟表 |
| Trigger | 触发器，自动执行的数据库程序 |
| Constraint | 约束，数据完整性规则 |

## 四、HTTP状态码

| 状态码 | 英文说明 | 中文说明 | 使用场景 |
|--------|---------|----------|----------|
| 200 | OK | 成功 | 请求成功 |
| 201 | Created | 已创建 | 资源创建成功 |
| 204 | No Content | 无内容 | 删除成功 |
| 400 | Bad Request | 错误请求 | 参数错误 |
| 401 | Unauthorized | 未授权 | 需要登录 |
| 403 | Forbidden | 禁止访问 | 权限不足 |
| 404 | Not Found | 未找到 | 资源不存在 |
| 409 | Conflict | 冲突 | 数据冲突 |
| 500 | Internal Server Error | 服务器错误 | 系统异常 |

## 五、角色权限术语

| 角色 | 英文 | 权限范围 |
|-----|------|----------|
| 管理员 | Admin | 系统所有功能，包括用户管理、系统配置 |
| 医生 | Doctor | 诊疗相关功能，患者管理、处方开具 |
| 护士 | Nurse | 患者接待、基础信息录入 |
| 药剂师 | Pharmacist | 药材管理、处方审核 |

## 六、测试术语

| 术语 | 英文 | 说明 |
|-----|------|------|
| 单元测试 | Unit Test | 测试单个组件或方法 |
| 集成测试 | Integration Test | 测试组件间交互 |
| E2E测试 | End-to-End Test | 端到端的完整流程测试 |
| 测试覆盖率 | Test Coverage | 代码被测试覆盖的比例 |
| Mock | 模拟对象 | 测试中的假对象 |
| Stub | 桩 | 简单的模拟实现 |
| Assert | 断言 | 测试中的验证语句 |
| Arrange-Act-Assert | AAA模式 | 测试的组织模式 |

## 七、Git操作术语

| 术语 | 说明 | 示例 |
|-----|------|------|
| commit | 提交 | `git commit -m "消息"` |
| push | 推送 | `git push origin master` |
| pull | 拉取 | `git pull origin master` |
| branch | 分支 | `git branch feature/xxx` |
| merge | 合并 | `git merge feature/xxx` |
| rebase | 变基 | `git rebase master` |
| stash | 暂存 | `git stash` |
| tag | 标签 | `git tag v1.0.0` |

## 八、缩写对照表

| 缩写 | 全称 | 中文 |
|-----|------|------|
| API | Application Programming Interface | 应用程序接口 |
| CRUD | Create, Read, Update, Delete | 增删改查 |
| DI | Dependency Injection | 依赖注入 |
| DTO | Data Transfer Object | 数据传输对象 |
| IoC | Inversion of Control | 控制反转 |
| JWT | JSON Web Token | JSON网络令牌 |
| MVC | Model-View-Controller | 模型-视图-控制器 |
| MVVM | Model-View-ViewModel | 模型-视图-视图模型 |
| ORM | Object-Relational Mapping | 对象关系映射 |
| REST | Representational State Transfer | 表述性状态转移 |
| SQL | Structured Query Language | 结构化查询语言 |
| TCM | Traditional Chinese Medicine | 中医 |
| UI | User Interface | 用户界面 |
| UX | User Experience | 用户体验 |
| VM | View Model | 视图模型 |

## 九、命名约定

### 9.1 数据库命名

| 类型 | 约定 | 示例 |
|-----|------|------|
| 表名 | 复数形式 | Patients, Prescriptions |
| 字段名 | PascalCase | PatientName, CreatedAt |
| 主键 | Id | Id (GUID类型) |
| 外键 | 实体名+Id | PatientId, MedicalCaseId |

### 9.2 代码命名

| 类型 | 约定 | 示例 |
|-----|------|------|
| 类名 | PascalCase | PatientService |
| 接口 | I+PascalCase | IPatientService |
| 方法名 | PascalCase | GetPatientAsync |
| 属性 | PascalCase | PatientName |
| 私有字段 | _camelCase | _patientRepository |
| 参数 | camelCase | patientId |
| 常量 | UPPER_CASE | MAX_RETRY_COUNT |

### 9.3 文件命名

| 类型 | 约定 | 示例 |
|-----|------|------|
| 实体类 | 实体名+Model.cs | PatientModel.cs |
| 服务类 | 服务名+Service.cs | PatientService.cs |
| 控制器 | 实体名+Controller.cs | PatientController.cs |
| DTO类 | 实体名+Dto.cs | PatientDto.cs |
| 视图 | 功能名+View.xaml | PatientListView.xaml |
| 视图模型 | 功能名+ViewModel.cs | PatientListViewModel.cs |

## 十、业务规则术语

| 规则名称 | 说明 | 实现位置 |
|---------|------|----------|
| 当天可改规则 | 病历、处方等当天创建可修改 | MedicalCase.CanEdit() |
| 过期锁定规则 | 过了创建当天自动锁定 | MedicalCase.IsLocked |
| 管理员例外规则 | 管理员可编辑所有记录 | 权限检查中间件 |
| 一病历一诊断 | 每个病历只有一个诊断 | 实体关系1:1 |
| 一病历一处方 | 每个病历最多一个处方 | 实体关系1:0..1 |
| 处方审核规则 | 处方需要审核才能打印 | Prescription.Status |
| 软删除规则 | 删除操作只标记不物理删除 | BaseEntity.IsDeleted |

---

**维护说明**: 本术语表应随系统发展持续更新，新增术语请按类别添加到相应章节。