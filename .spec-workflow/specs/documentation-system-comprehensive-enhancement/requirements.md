# 文档系统真实完善需求文档

**项目编号**: DOC-001
**创建时间**: 2025-10-15
**创建人**: Claude Code
**状态**: 待审批

## 📋 需求概述

### 背景分析
通过对LYBTZYZS项目实际代码的深度分析，发现文档系统与实际代码严重不匹配：

**实际发现的问题**：
1. **实体架构文档不完整**：LYBT.Entities README存在但缺少实际的数据库设计文档
2. **API文档严重缺失**：12个控制器但文档只有简单列表
3. **认证系统复杂但文档简单**：双轨认证、JWT机制、超级管理员隔离等缺少详细说明
4. **客户端架构复杂但文档不足**：8个WPF模块、Prism框架、MVVM模式缺少实践指南
5. **数据库设计文档空白**：11个实体表、关系映射、迁移策略完全缺失
6. **业务流程文档缺失**：中医四诊、处方流程、医案管理等核心业务流程没有文档

### 真实需求分析
基于代码扫描发现的实际情况：

**Server端实际情况**：
- 8个业务模块：Auth, Users, Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula
- 12个API控制器：AuthController, UsersController, PatientsController, MedicalCaseController等
- 11个核心实体：UserModel, PatientModel, MedicalCaseModel, ConsultationModel等
- 复杂的认证系统：双轨认证、JWT、RefreshToken、超级管理员物理隔离

**Client端实际情况**：
- 8个WPF业务模块对应Server端
- 复杂的MVVM架构：Prism.DryIoc、AutoMapper、Refit HTTP客户端
- 三层架构：Core, Services, Infrastructure, Modules, Workstations
- 丰富的UI组件和现代化设计

## 🎯 核心需求（基于实际代码，严格遵循index.md架构）

## Requirements

### Requirement 1: Level 1 快速参考文档完善

**User Story:** 作为开发人员，我需要完整的快速参考文档，以便快速查找和使用常用信息。

**发现的问题**：
- `quick-reference/api_reference.md` 只包含简单列表，缺少12个实际控制器的详细API示例
- `quick-reference/config_templates.md` 缺少基于实际appsettings.json的配置模板
- `quick-reference/code_patterns.md` 缺少基于8个业务模块的实际代码模式
- `quick-reference/troubleshooting.md` 缺少基于实际错误的问题解决方案

#### Acceptance Criteria

1. WHEN 开发人员需要查找API THEN 系统 SHALL 在`quick-reference/api_reference.md`提供12个控制器的实际API调用示例
2. WHEN 开发人员需要配置 THEN 系统 SHALL 在`quick-reference/config_templates.md`提供基于实际配置文件的完整模板
3. WHEN 开发人员需要代码模式 THEN 系统 SHALL 在`quick-reference/code_patterns.md`提供基于8个业务模块的实际代码模式
4. WHEN 开发人员遇到问题 THEN 系统 SHALL 在`quick-reference/troubleshooting.md`提供基于实际错误的解决方案

### Requirement 2: Level 3 API接口文档完善

**User Story:** 作为开发人员，我需要完整的API接口文档，以便正确调用和使用后端服务。

**发现的问题**：
- 实际有12个API控制器，但`docs/api/README.md`只有简单列表，缺少详细文档
- 缺少基于实际控制器的API端点详细说明
- 缺少基于实际请求/响应的示例和错误处理

#### Acceptance Criteria

1. WHEN 开发人员需要调用API THEN 系统 SHALL 在`docs/api/`提供12个控制器的详细API文档
2. WHEN 需要理解认证机制 THEN 系统 SHALL 在API文档中提供双轨认证的详细说明
3. WHEN 处理API错误 THEN 系统 SHALL 在API文档中提供完整的错误码和处理示例
4. IF 需要集成客户端 THEN 系统 SHALL 在API文档中提供Refit客户端配置示例

### Requirement 3: Level 2 架构文档完善

**User Story:** 作为开发人员，我需要完整的架构文档，以便理解系统设计和开发标准。

**发现的问题**：
- `architecture/server/README.md` 缺少基于实际8个业务模块的架构说明
- `architecture/client/README.md` 缺少基于实际WPF客户端的架构文档
- `architecture/shared/README.md` 缺少基于实际认证系统的共享架构说明

#### Acceptance Criteria

1. WHEN 开发人员需要理解Server架构 THEN 系统 SHALL 在`architecture/server/README.md`提供基于实际8个模块的架构说明
2. WHEN 开发人员需要理解Client架构 THEN 系统 SHALL 在`architecture/client/README.md`提供基于实际WPF客户端的架构文档
3. WHEN 开发人员需要理解共享架构 THEN 系统 SHALL 在`architecture/shared/README.md`提供基于实际认证系统的共享架构说明
4. IF 开发人员需要设计标准 THEN 系统 SHALL 在现有design-standard.md中补充基于实际代码的设计细节

### Requirement 4: Level 2 开发文档完善

**User Story:** 作为开发人员，我需要完整的开发指南文档，以便按照规范进行开发。

**发现的问题**：
- `development/server/README.md` 缺少基于实际Server端代码的开发指南
- `development/client/README.md` 缺少基于实际WPF客户端的开发指南
- `development/shared/README.md` 缺少基于实际共享组件的开发指南

#### Acceptance Criteria

1. WHEN 开发人员需要Server端开发指南 THEN 系统 SHALL 在`development/server/README.md`提供基于实际代码的开发指南
2. WHEN 开发人员需要Client端开发指南 THEN 系统 SHALL 在`development/client/README.md`提供基于实际WPF客户端的开发指南
3. WHEN 开发人员需要共享开发指南 THEN 系统 SHALL 在`development/shared/README.md`提供基于实际共享组件的开发指南
4. IF 开发人员需要测试指南 THEN 系统 SHALL 在现有testing-guide.md中补充基于实际代码的测试示例

### Requirement 5: 数据库设计文档补充

**User Story:** 作为开发人员，我需要完整的数据库设计文档，以便理解数据模型和进行数据操作。

**发现的问题**：
- 现有`src/Server/Core/LYBT.Entities/README.md`存在但缺少实际数据库设计文档
- 11个实体表的关系图、字段约束、索引策略在docs/架构中缺失
- EF Core映射配置、迁移策略在docs/中缺少说明

#### Acceptance Criteria

1. WHEN 开发人员需要了解数据结构 THEN 系统 SHALL 在`architecture/server/`中补充基于11个实体的数据库设计文档
2. WHEN 开发人员进行数据库变更 THEN 系统 SHALL 在`development/server/`中补充EF Core映射配置和迁移指南
3. WHEN 需要理解实体关系 THEN 系统 SHALL 提供UserModel→MedicalCaseModel→ConsultationModel→PrescriptionModel的完整关系链说明
4. IF 需要进行数据库优化 THEN 系统 SHALL 在`development/server/`中补充索引策略和查询性能优化指南

### Requirement 6: 业务流程文档补充

**User Story:** 作为医生或管理员，我需要详细的业务流程说明，以便正确使用系统功能。

**发现的问题**：
- 8个业务模块的实际功能与现有文档不匹配
- 缺少模块间依赖关系和数据流说明
- 中医特色业务流程（四诊合参、辨证论治、处方管理）在docs/中缺失

#### Acceptance Criteria

1. WHEN 医生需要了解诊疗流程 THEN 系统 SHALL 在`architecture/server/`中补充患者登记→四诊合参→辨证论治→处方开具的完整中医诊疗流程
2. WHEN 管理员需要管理用户 THEN 系统 SHALL 在`architecture/shared/`中补充双轨用户管理（普通用户+超级管理员）的详细说明
3. WHEN 开发人员需要理解业务逻辑 THEN 系统 SHALL 在`architecture/server/`中补充8个模块的详细功能说明和接口定义
4. IF 用户遇到业务规则疑问 THEN 系统 SHALL 在`development/shared/`中补充一病历一诊断、当天可改过期锁定等业务规则的详细说明

## Non-Functional Requirements

### Code Architecture and Modularity
- **Single Responsibility Principle**: 每个文档应专注于特定的技术领域或用户群体
- **模块化设计**: 文档系统采用分层架构，便于维护和扩展
- **依赖管理**: 最小化文档间的交叉引用依赖，确保独立性
- **Clear Interfaces**: 定义清晰的文档模板和标准，确保格式一致性

### Performance
- **文档加载性能**: 文档导航系统应在3秒内加载完成
- **搜索效率**: 全文搜索功能应在2秒内返回结果
- **文档大小**: 单个文档文件应控制在200KB以内，确保快速加载

### Security
- **访问控制**: 敏感技术文档应设置适当的访问权限
- **版本管理**: 文档变更应有完整的版本历史记录
- **备份策略**: 重要文档应有定期备份和恢复机制

### Reliability
- **链接完整性**: 所有文档内部链接应保持有效，失效链接应自动检测
- **内容一致性**: 确保文档间信息的一致性，避免矛盾内容
- **更新及时性**: 代码变更后相关文档应在24小时内更新

### Usability
- **导航便捷性**: 用户应在3次点击内找到所需文档
- **搜索友好性**: 支持关键词搜索和分类浏览两种查找方式
- **多设备支持**: 文档应支持桌面、平板和移动设备访问
- **国际化支持**: 关键文档应提供中英文版本

### 文档质量标准
- **完整性**: 覆盖项目所有技术层面和业务场景
- **准确性**: 确保技术细节和业务流程描述的准确性
- **易理解性**: 使用清晰的语言和丰富的图表说明复杂概念
- **实用性**: 提供具体的操作指南和实际案例
- **时效性**: 定期审查和更新，确保文档内容与系统现状同步