# 患者管理功能完善需求分析

**文档版本**: v1.0
**创建日期**: 2025-11-09
**状态**: 📝 需求讨论
**相关模块**: Patients（患者管理）
**参考模块**: Users（用户管理）、Formula（验方管理）

---

## 📋 目录

- [1. 需求概述](#1-需求概述)
- [2. 功能性需求](#2-功能性需求)
- [3. 非功能性需求](#3-非功能性需求)
- [4. 业务规则](#4-业务规则)
- [5. 技术约束](#5-技术约束)
- [6. 参考实现分析](#6-参考实现分析)
- [7. 开放问题](#7-开放问题)

---

## 1. 需求概述

### 1.1 业务目标

完善患者管理模块功能，参考用户管理和验方管理的最佳实践，提供完整的CRUD操作、批量导入导出、优化列表UI体验，确保功能完整性和一致性。

### 1.2 目标用户

- **管理员**: 批量导入患者数据、数据导出备份、管理患者档案
- **医生**: 查看患者信息、编辑患者档案、管理就诊记录

### 1.3 核心场景

1. **批量导入**: 从Excel文件批量导入患者基本信息（新开诊所初始化数据）
2. **数据导出**: 导出患者档案到Excel文件（数据备份、报表生成）
3. **完整CRUD**: 查看患者详情、编辑患者信息、删除患者档案
4. **优化列表UI**: 操作列右对齐、数据列自适应宽度，参考验方列表实现

### 1.4 当前状态与差距分析

| 功能模块 | 当前状态 | 目标状态 | 差距 |
|---------|---------|---------|------|
| **患者列表UI** | ✅ 已实现（操作列右对齐） | ✅ 优化列宽自适应 | 🟡 部分优化 |
| **新增患者** | ✅ 已实现 | ✅ 保持现状 | ✅ 无差距 |
| **查看患者** | ❌ 未实现 | ✅ 需实现 | 🔴 缺失 |
| **编辑患者** | ❌ 未实现 | ✅ 需实现 | 🔴 缺失 |
| **删除患者** | ✅ 已实现 | ✅ 保持现状 | ✅ 无差距 |
| **批量导入** | ❌ 未实现 | ✅ 需实现 | 🔴 缺失 |
| **批量导出** | ❌ 未实现 | ✅ 需实现 | 🔴 缺失 |
| **分页功能** | ✅ 已实现 | ✅ 保持现状 | ✅ 无差距 |

---

## 2. 功能性需求

### FR-001: 批量导入患者数据

**描述**: 管理员可以从Excel文件批量导入患者基本信息

**User Story**:
```
作为 管理员
我想要 从Excel文件批量导入患者数据
以便 快速初始化患者档案库，减少手工录入工作量
```

**验收标准**:
- [x] 支持Excel文件格式（.xlsx）
- [x] Excel模板包含必填列：姓名、性别、出生日期、手机号
- [x] Excel模板包含可选列：身份证号、地址、过敏史、既往病史
- [x] 导入前预览数据（显示前10条记录）
- [x] 数据验证：
  - 姓名非空（2-20字符）
  - 性别值必须为"男"或"女"
  - 手机号格式验证（11位数字）
  - 身份证号格式验证（18位或15位）
  - 出生日期合法性验证（不早于1900年，不晚于今天）
- [x] 重复性检查：手机号已存在时提示（可选择跳过或更新）
- [x] 导入结果统计：成功X条、失败Y条、跳过Z条
- [x] **失败数据快速修复机制**（⭐ 核心体验）：
  - **自动导出失败数据Excel**：
    - 文件名：`导入失败数据_YYYYMMDD_HHmmss.xlsx`
    - 包含列：原始行号 + 所有原始数据列 + 失败原因列
    - 示例：第5行 | 张三 | 男 | ... | ❌ 手机号格式错误（需11位数字）
    - 导出后自动打开文件所在目录
  - **失败原因详细说明**：
    - 每条失败记录显示具体错误（如"第5行：手机号格式错误，当前值'1380013800'仅10位"）
    - 提供修复建议（如"请修改为11位数字"）
  - **增量导入支持**：
    - 用户修复失败的Excel后，可以直接导入（仅30条）
    - 系统通过手机号识别已存在记录
    - 提供选项：跳过 or 更新现有记录
  - **导入历史记录**（导入结果对话框）：
    - 显示本次导入的详细结果列表（可滚动查看所有失败记录）
    - 每条失败记录可点击查看完整信息
    - 提供"导出失败数据"按钮一键导出
- [x] 导入进度条显示（导入过程中禁止关闭窗口）

**技术实现要点**:
- **Excel处理库**: EPPlus（MIT许可，.NET生态成熟）
- **架构层分配**:
  - Server端: 提供批量添加API `POST /api/patients/batch`
  - Desktop端: Excel读取、数据转换、文件对话框、进度显示
- **API兼容性**: 新增批量API，保持现有单个添加API不变

---

### FR-002: 批量导出患者数据

**描述**: 管理员可以将患者数据导出到Excel文件

**User Story**:
```
作为 管理员
我想要 将患者数据导出到Excel文件
以便 进行数据备份、报表分析、数据迁移
```

**验收标准**:
- [x] 支持导出当前筛选条件下的所有患者数据
- [x] 支持导出全部患者数据（忽略筛选）
- [x] Excel文件包含列：
  - 基本信息：姓名、性别、出生日期、年龄、手机号、身份证号
  - 扩展信息：地址、过敏史、既往病史
  - 统计信息：就诊次数、创建时间、最后更新时间
- [x] 文件命名规范：`患者档案_YYYYMMDD_HHmmss.xlsx`
- [x] 导出进度条显示（大数据量场景）
- [x] 导出成功后自动打开文件所在目录
- [x] 数据脱敏选项（可选）：
  - 手机号中间4位显示为`****`
  - 身份证号中间10位显示为`****`

**技术实现要点**:
- **Excel生成**: EPPlus（Desktop端生成）
- **数据获取**: 通过现有API `GET /api/patients?pageSize=999999`获取全部数据
- **导出策略**: Desktop端处理（不在Server端生成Excel）

---

### FR-003: 查看患者详情

**描述**: 医生/管理员可以查看患者的完整档案信息

**User Story**:
```
作为 医生
我想要 查看患者的完整档案信息
以便 了解患者病史、过敏史、就诊记录等详细信息
```

**验收标准**:
- [x] 列表中点击"查看"按钮打开患者详情对话框
- [x] 详情对话框显示：
  - 基本信息：姓名、性别、出生日期、年龄、手机号、身份证号
  - 扩展信息：地址、过敏史、既往病史、备注
  - 统计信息：就诊次数、创建时间、最后更新时间
  - 关联数据：历史就诊记录列表（最近10条）
- [x] 详情对话框为只读模式（不可编辑）
- [x] 提供"编辑"按钮切换到编辑模式
- [x] 提供"关闭"按钮关闭对话框

**技术实现要点**:
- **UI组件**: PatientDetailView（新增）
- **ViewModel**: PatientDetailViewModel（新增）
- **API调用**: `GET /api/patients/{id}`（现有API）

---

### FR-004: 编辑患者信息

**描述**: 医生/管理员可以编辑患者的基本信息和扩展信息

**User Story**:
```
作为 医生
我想要 编辑患者的基本信息和病史
以便 更新患者档案，保持信息准确性
```

**验收标准**:
- [x] 列表中点击"编辑"按钮打开编辑对话框
- [x] 或在详情对话框中点击"编辑"按钮切换到编辑模式
- [x] 可编辑字段：
  - 基本信息：姓名、性别、出生日期、手机号、身份证号
  - 扩展信息：地址、过敏史、既往病史、备注
- [x] 不可编辑字段：就诊次数、创建时间、最后更新时间
- [x] 数据验证：
  - 姓名非空（2-20字符）
  - 手机号格式验证
  - 身份证号格式验证
  - 出生日期合法性验证
- [x] 保存成功后刷新列表
- [x] 保存失败时显示错误信息

**技术实现要点**:
- **UI组件**: EditPatientDialog（新增，或复用QuickCreatePatientDialog）
- **ViewModel**: EditPatientDialogViewModel（新增）
- **API调用**: `PUT /api/patients/{id}`（需新增Server API）

---

### FR-005: 列表UI优化

**描述**: 优化患者列表UI，参考验方列表实现，操作列右对齐，数据列自适应宽度

**User Story**:
```
作为 医生
我想要 患者列表界面更加美观和实用
以便 提高工作效率，快速定位所需信息
```

**验收标准**:
- [x] **操作列右对齐**（✅ 已实现）:
  - 操作列固定宽度：`Width="200"`（容纳"查看"+"编辑"+"删除"3个按钮）
  - 操作按钮右对齐：`HorizontalAlignment="Right"`
  - 按钮与右边框保持20px间距：`Margin="0,0,20,0"`
- [x] **数据列自适应宽度**（🟡 需优化）:
  - 姓名列：`Width="120"`（固定，避免过长姓名挤占空间）
  - 性别列：`Width="80"`（固定，内容简短）
  - 年龄列：`Width="80"`（固定，内容简短）
  - 手机号列：`Width="*"`（⭐ 自适应，重要信息）
  - 身份证号列：`Width="*"`（⭐ 自适应，重要信息）
  - 就诊次数列：`Width="100"`（固定，内容简短）
- [x] **参考验方列表样式**:
  - 使用UnifiedManagementTable统一表格组件（✅ 已使用）
  - 使用UnifiedStatusBadge统一状态徽章（🟡 可选，患者无状态字段）
  - 按钮样式统一：查看（InfoButton）、编辑（SuccessButton）、删除（DangerButton）

**修改对比**:

**当前实现**（PatientManagementView.xaml Line 57-106）:
```xaml
<!-- 手机号 - 固定宽度 -->
<DataGridTextColumn Header="手机号"
                    Binding="{Binding PhoneNumber}"
                    Width="140" />

<!-- 身份证号 - 固定宽度 -->
<DataGridTextColumn Header="身份证号"
                    Binding="{Binding IdNumber}"
                    Width="180" />

<!-- 操作列 - 固定宽度120，右对齐 ✅ -->
<DataGridTemplateColumn Header="操作" Width="120">
    <DataGridTemplateColumn.CellStyle>
        <Style TargetType="DataGridCell">
            <Setter Property="HorizontalContentAlignment" Value="Right" />
        </Style>
    </DataGridTemplateColumn.CellStyle>
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,0,20,0">
                <!-- Phase 2: 仅保留删除按钮 -->
                <Button Content="删除" ... />
            </StackPanel>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**优化后实现**:
```xaml
<!-- 手机号 - 自适应宽度 ⭐ -->
<DataGridTextColumn Header="手机号"
                    Binding="{Binding PhoneNumber}"
                    Width="*" />

<!-- 身份证号 - 自适应宽度 ⭐ -->
<DataGridTextColumn Header="身份证号"
                    Binding="{Binding IdNumber}"
                    Width="*" />

<!-- 操作列 - 固定宽度200，右对齐，增加查看/编辑按钮 -->
<DataGridTemplateColumn Header="操作" Width="200">
    <DataGridTemplateColumn.CellStyle>
        <Style TargetType="DataGridCell">
            <Setter Property="HorizontalContentAlignment" Value="Right" />
        </Style>
    </DataGridTemplateColumn.CellStyle>
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,0,20,0">
                <Button Content="查看"
                        Style="{StaticResource InfoButton}"
                        Padding="8,4"
                        FontSize="12"
                        Margin="2"
                        Command="{Binding DataContext.ViewDetailsCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        CommandParameter="{Binding}" />
                <Button Content="编辑"
                        Style="{StaticResource SuccessButton}"
                        Padding="8,4"
                        FontSize="12"
                        Margin="2"
                        Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        CommandParameter="{Binding}" />
                <Button Content="删除"
                        Style="{StaticResource DangerButton}"
                        Padding="8,4"
                        FontSize="12"
                        Margin="2"
                        Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        CommandParameter="{Binding}" />
            </StackPanel>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

---

### FR-006: 导入/导出功能入口

**描述**: 在患者管理工具栏中添加导入/导出按钮

**验收标准**:
- [x] 工具栏添加按钮：
  - "📥 导入患者"按钮（SecondaryButton样式）
  - "📄 下载模板"按钮（InfoButton样式）
  - "📤 导出患者"按钮（WarningButton样式）
- [x] 按钮位置：搜索框右侧，"新增患者"按钮左侧
- [x] 按钮提示：ToolTip显示功能说明
- [x] 权限控制：仅管理员可见导入/导出按钮（医生角色隐藏）

**修改位置**: PatientManagementView.xaml Line 27-46（工具栏ActionButtons区域）

**参考实现**: FormulaManagementView.xaml Line 40-54（验方管理工具栏）

---

## 3. 非功能性需求

### NFR-001: 性能要求

| 场景 | 性能指标 | 备注 |
|------|---------|------|
| 患者列表加载 | < 500ms | 100条数据以内 |
| 单个患者查看 | < 300ms | 包含关联就诊记录 |
| 单个患者编辑保存 | < 300ms | 数据验证+保存 |
| 批量导入（100条） | < 5s | 含数据验证 |
| 批量导入（1000条） | < 30s | 含数据验证 |
| 批量导出（1000条） | < 10s | 生成Excel文件 |

### NFR-002: 安全要求

- **数据脱敏**: 导出时可选脱敏（手机号、身份证号）
- **权限控制**:
  - 管理员：CRUD + 导入/导出
  - 医生：查看 + 编辑（本人创建的患者）
  - 护士：查看（只读）
- **审计日志**: 记录批量导入/导出操作（操作人、时间、记录数）
- **数据验证**: 所有输入数据必须经过服务端验证

### NFR-003: 可用性要求

- **导入容错**: 部分失败不影响成功数据，提供失败原因导出
- **Excel模板**: 提供标准模板下载（含示例数据和列说明）
- **进度反馈**: 批量操作显示进度条和剩余时间估算
- **操作提示**: 删除/导入前显示确认对话框
- **错误提示**: 错误信息清晰明确，提供解决建议

### NFR-004: 兼容性要求

- **Excel格式**: 支持.xlsx格式（Office 2007+）
- **文件编码**: UTF-8（避免中文乱码）
- **操作系统**: Windows 10/11（WPF应用）
- **数据库**: SQL Server 2022（已有Patients表）

---

## 4. 业务规则

### BR-001: 批量导入数据验证规则

| 字段 | 验证规则 | 失败处理 |
|------|---------|---------|
| **姓名** | 非空，2-20字符 | 跳过该行，记录失败原因 |
| **性别** | 必须为"男"或"女" | 跳过该行，记录失败原因 |
| **出生日期** | 格式：YYYY-MM-DD，1900-01-01 ~ 今天 | 跳过该行，记录失败原因 |
| **手机号** | 11位数字，正则验证 | 跳过该行，记录失败原因 |
| **身份证号** | 18位或15位，校验位验证 | 跳过该行，记录失败原因 |
| **手机号重复** | 数据库已存在 | 可选：跳过 or 更新现有记录 |

**理由**: 保证数据质量，避免脏数据入库

**实现**: Desktop端预验证 + Server端最终验证（双重保障）

---

### BR-002: 批量导入失败策略与快速修复流程

**策略**: **部分成功模式 + 失败数据快速修复**（推荐）

**完整流程示例**（100条数据，30条失败）：

```
步骤1: 首次导入
  ├─ 用户选择Excel文件（100条患者数据）
  ├─ 系统验证数据
  ├─ 结果：70条成功，30条失败
  └─ 显示导入结果对话框

步骤2: 查看失败详情
  ├─ 导入结果对话框显示：
  │   ├─ 成功：70条 ✅
  │   ├─ 失败：30条 ❌
  │   └─ 失败记录列表（可滚动查看）：
  │       ├─ 第5行：张三 - 手机号格式错误（当前'1380013800'仅10位，需11位）
  │       ├─ 第12行：李四 - 身份证号校验位错误
  │       ├─ 第18行：王五 - 手机号已存在（138****1234，患者ID: 1025）
  │       └─ ... （共30条）
  └─ 点击"导出失败数据"按钮

步骤3: 自动导出失败数据
  ├─ 生成Excel文件：`导入失败数据_20251109_143025.xlsx`
  ├─ Excel内容：
  │   ┌──────┬──────┬──────┬─────────────┬────────────────────────────────┐
  │   │ 行号 │ 姓名 │ 性别 │   手机号    │          失败原因              │
  │   ├──────┼──────┼──────┼─────────────┼────────────────────────────────┤
  │   │  5   │ 张三 │  男  │ 1380013800  │ 手机号格式错误（需11位数字）    │
  │   │  12  │ 李四 │  女  │ 13900139000 │ 身份证号校验位错误              │
  │   │  18  │ 王五 │  男  │ 13800138000 │ 手机号已存在（患者ID: 1025）    │
  │   │  ... │ ...  │ ...  │    ...      │              ...                │
  │   └──────┴──────┴──────┴─────────────┴────────────────────────────────┘
  └─ 自动打开文件所在目录

步骤4: 用户修复数据
  ├─ 打开失败数据Excel
  ├─ 根据"失败原因"列修复数据：
  │   ├─ 第5行：修改手机号为 13800138000（补齐1位）
  │   ├─ 第12行：修改身份证号校验位
  │   └─ 第18行：决定跳过（已存在）或修改手机号
  ├─ 删除"失败原因"列（可选，系统会忽略此列）
  └─ 保存Excel文件

步骤5: 增量导入修复后的数据
  ├─ 重新打开"导入患者"功能
  ├─ 选择修复后的Excel（30条数据）
  ├─ 系统验证数据
  ├─ 遇到重复手机号时提示：
  │   ├─ 手机号 13800138000 已存在
  │   ├─ 现有患者：王五（ID: 1025）
  │   └─ 选项：⭕ 跳过  ⭕ 更新现有记录
  ├─ 用户选择"跳过"
  └─ 结果：29条成功，1条跳过

步骤6: 最终结果
  ├─ 第一次导入：70条成功
  ├─ 第二次导入：29条成功
  └─ 总计：99条成功，1条跳过（因已存在）
```

**核心特性**:
- ✅ **原始行号保留**: 失败数据包含原始Excel行号，用户可快速定位
- ✅ **详细失败原因**: 每条失败记录显示具体错误和修复建议
- ✅ **一键导出**: 自动生成失败数据Excel，无需手动筛选
- ✅ **增量导入**: 修复后可单独导入失败的数据，无需重新导入全部
- ✅ **重复检测**: 通过手机号识别已存在记录，提供跳过/更新选项

**替代方案**: 全部成功或全部回滚（不推荐，MVP阶段过于复杂）

**理由**: 用户友好，最小化手工操作，快速定位和修复错误

---

### BR-003: 批量导出权限控制

**规则**:
- 管理员：可导出全部患者数据
- 医生：仅可导出本人创建的患者数据
- 护士：无导出权限

**理由**: 保护患者隐私，符合数据安全规范

**实现**: Server端API根据当前用户角色过滤数据

---

### BR-004: Excel模板列定义

**必填列**（红色标题，带*号）:
1. 姓名*
2. 性别*（男/女）
3. 出生日期*（格式：YYYY-MM-DD）
4. 手机号*（11位数字）

**可选列**（黑色标题）:
5. 身份证号（18位或15位）
6. 地址
7. 过敏史
8. 既往病史
9. 备注

**模板示例数据**（第2-3行）:
```
姓名*    | 性别* | 出生日期*    | 手机号*       | 身份证号          | 地址        | 过敏史   | 既往病史 | 备注
张三     | 男    | 1980-05-15   | 13800138000  | 110101198005151234 | 北京市朝阳区  | 青霉素   | 高血压   | 示例数据
李四     | 女    | 1992-08-20   | 13900139000  | 110102199208201234 | 北京市海淀区  | 无       | 无       | 示例数据
```

---

### BR-005: 列表UI自适应规则（通用规则，适用于所有管理模块）

**核心原则**:
- 短内容列（性别、年龄、状态）→ 固定宽度（60-100px）
- 长内容列（姓名、手机号、身份证号）→ 自适应宽度（Width="*"）
- 操作列 → 固定宽度（根据按钮数量：2按钮=150px，3按钮=200px，4按钮=280px）
- 操作列 → 右对齐（HorizontalAlignment="Right"）

**应用场景**:
- ✅ 患者管理（本需求）
- ✅ 验方管理（已实现，参考标准）
- ⚠️ 用户管理（待优化）
- ⚠️ 其他管理模块（待统一）

**技术实现**: 修改XAML列定义，Width属性从固定值改为"*"

---

### BR-006: 调用链完整性验证（从用户管理经验中学习）

**背景**: 用户管理Epic #1911发现的调用链断裂问题：

```
UI完整 ✅
  ↓
ViewModel.Method() ❌ Mock实现
  ↓
CommandHandler.Method() ❌ 返回"功能开发中"
  ↓
IRepository.Method() ❌ 接口未定义
  ↓
Repository.Method() ❌ 未实现
  ↓
IApi.Method() ❌ 接口未定义
  ↓
Api (Refit) ❌ 需添加接口定义
  ↓
Server API ✅ 已实现
```

**验证规则**: 任何新功能必须验证完整调用链

**实施检查清单**:
- [ ] Server API端点已实现并测试
- [ ] IUserApi接口已定义（Shared层）
- [ ] UserApi (Refit)已实现（Client层）
- [ ] IUserRepository接口已定义
- [ ] UserRepository已实现
- [ ] CommandHandler已实现（非Mock）
- [ ] ViewModel已实现调用
- [ ] UI已绑定Command

**理由**: 避免出现"UI完整但功能不可用"的问题

**应用**: 患者管理的查看/编辑功能必须遵循此检查清单

---

## 5. 技术约束

### 5.1 MVP技术栈限制

**✅ 允许的技术**:
- EPPlus（Excel处理，MIT许可）
- Entity Framework Core 8.0
- ASP.NET Core 8.0
- WPF + Prism 8.x
- SQL Server 2022

**❌ 禁止的技术**（MVP阶段）:
- Redis缓存
- RabbitMQ/Kafka消息队列
- Docker容器化
- 微服务架构
- CQRS模式
- MediatR
- 事件溯源

### 5.2 架构层分配

| 功能 | Server端 | Desktop端 | Shared端 |
|------|---------|-----------|---------|
| **批量导入** | POST /api/patients/batch | Excel读取、数据转换、进度显示 | CreatePatientDto[] |
| **批量导出** | GET /api/patients?pageSize=999999 | Excel生成、文件保存 | PatientDto[] |
| **查看详情** | GET /api/patients/{id} | 详情对话框UI | PatientDto |
| **编辑患者** | PUT /api/patients/{id} | 编辑对话框UI | UpdatePatientDto |

### 5.3 数据库约束

**现有Patients表结构**（无需修改）:
```sql
CREATE TABLE Patients (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Gender NVARCHAR(10) NOT NULL,
    BirthDate DATE NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    IdNumber NVARCHAR(20) NULL,
    Address NVARCHAR(500) NULL,
    AllergyHistory NVARCHAR(1000) NULL,
    MedicalHistory NVARCHAR(2000) NULL,
    Notes NVARCHAR(2000) NULL,
    VisitCount INT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    IsDeleted BIT DEFAULT 0
)
```

**索引优化建议**（性能优化）:
```sql
-- 手机号唯一索引（防止重复）
CREATE UNIQUE INDEX IX_Patients_PhoneNumber ON Patients(PhoneNumber)
    WHERE IsDeleted = 0;

-- 姓名索引（搜索优化）
CREATE INDEX IX_Patients_Name ON Patients(Name);

-- 身份证号索引（查询优化）
CREATE INDEX IX_Patients_IdNumber ON Patients(IdNumber)
    WHERE IdNumber IS NOT NULL;
```

---

## 6. 参考实现分析

### 6.1 用户管理模块参考

**优点**（值得学习）:
- ✅ 完整的CRUD操作（Create、Read、Update、Delete）
- ✅ 角色筛选和状态筛选功能
- ✅ 重置密码功能（独立对话框）
- ✅ 详情查看功能（ViewDetailsCommand）
- ✅ 统一的UnifiedListViewModelBase基类（分页、搜索、刷新）

**问题**（需要避免）:
- ⚠️ **调用链断裂问题**（Epic #1911）:
  - UI完整但后端调用链未打通
  - 导致功能看似完整实则不可用
  - 需要修改6个文件才能修复
- ⚠️ **无导入/导出功能**（根据Q3决策，用户管理不需要）

**经验教训**:
1. **调用链完整性验证**: 任何新功能必须验证完整调用链（见BR-006）
2. **增量开发**: 先实现核心功能，再扩展次要功能
3. **单元测试**: 关键业务逻辑必须有单元测试覆盖

---

### 6.2 验方管理模块参考

**优点**（直接复用）:
- ✅ **批量导入/导出功能**:
  - ImportFormulasCommand（📥 导入模板）
  - ExportTemplateCommand（📄 导出模板）
  - ExportFormulasCommand（📤 导出验方）
  - 完整的调用链实现
- ✅ **列表UI优化**:
  - 操作列固定宽度：Width="280"
  - 其他列自适应：Width="*"
  - 操作按钮居中对齐：HorizontalAlignment="Center"（❗ 但患者管理要求右对齐）
- ✅ **完整的CRUD按钮**:
  - 查看（ViewDetailsCommand）
  - 编辑（EditCommand）
  - 复制（CopyCommand）
  - 删除（DeleteCommand）

**可复用代码**:
1. **Excel导入ViewModel模式**:
```csharp
// FormulaManagementViewModel.cs
private async Task ExecuteImportFormulasAsync()
{
    // 1. 打开文件选择对话框
    var dialog = new OpenFileDialog { Filter = "Excel Files|*.xlsx" };
    if (dialog.ShowDialog() != true) return;

    // 2. 读取Excel文件
    var formulas = await ReadFormulasFromExcelAsync(dialog.FileName);

    // 3. 数据验证
    var validationResults = ValidateFormulas(formulas);

    // 4. 预览对话框（显示前10条）
    var preview = new ImportPreviewDialog(validationResults);
    if (preview.ShowDialog() != true) return;

    // 5. 调用API批量导入
    var result = await _repository.BatchAddAsync(formulas);

    // 6. 显示导入结果
    ShowImportResult(result);
}
```

2. **Excel导出ViewModel模式**:
```csharp
// FormulaManagementViewModel.cs
private async Task ExecuteExportFormulasAsync()
{
    // 1. 获取数据
    var formulas = await _repository.GetAllAsync();

    // 2. 生成Excel文件
    var filePath = await GenerateExcelFileAsync(formulas);

    // 3. 打开文件所在目录
    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
}
```

**差异点**:
- 验方管理：操作按钮**居中对齐**
- 患者管理：操作按钮**右对齐**（用户要求）

---

### 6.3 患者管理现状分析

**已实现功能** ✅:
- 新增患者（AddCommand）
- 删除患者（DeleteCommand）
- 分页功能（PreviousPageCommand、NextPageCommand）
- 搜索功能（SearchCommand）
- 刷新功能（RefreshCommand）
- 操作列右对齐（HorizontalContentAlignment="Right"）

**缺失功能** ❌:
- 查看患者详情（ViewDetailsCommand）
- 编辑患者信息（EditCommand）
- 批量导入（ImportPatientsCommand）
- 批量导出（ExportPatientsCommand）
- 下载模板（ExportTemplateCommand）

**UI优化点** 🟡:
- 列宽度：目前全部固定宽度，需改为部分自适应（Width="*"）
- 操作按钮：目前仅1个（删除），需增加至3个（查看、编辑、删除）
- 操作列宽度：目前120px，需扩展至200px（容纳3个按钮）

---

## 7. 开放问题

### Q1: 批量导入失败策略

**问题**: 批量导入时，如果部分数据验证失败，应该采取什么策略？

**选项**:
- **A. 部分成功模式**（推荐）:
  - 验证通过的数据正常导入
  - 验证失败的数据跳过，记录失败原因
  - 提供失败数据Excel下载
  - 优点：用户友好，允许修正后重新导入
  - 缺点：数据可能不完整
- **B. 全部成功或全部回滚**:
  - 任何一条数据失败，整个批次回滚
  - 优点：数据一致性强
  - 缺点：用户体验差，需要修正所有错误后重新导入

**建议**: 选择 **A（部分成功模式）** - 符合MVP原则（够用即好），用户体验更佳

---

### Q2: Excel模板是否需要示例数据？

**问题**: 下载的Excel模板是否包含示例数据？

**选项**:
- **A. 包含示例数据**（推荐）:
  - 第2-3行包含示例数据（如"张三"、"李四"）
  - 用户可直接修改示例数据
  - 优点：降低学习成本，用户更容易理解格式
  - 缺点：需要提示用户删除示例数据
- **B. 仅包含列头**:
  - 只有第1行列头，无示例数据
  - 优点：避免用户忘记删除示例数据
  - 缺点：用户需要查看文档才能理解格式

**建议**: 选择 **A（包含示例数据）** - 降低学习成本，提升用户体验

---

### Q3: 导入数据量限制

**问题**: 单次批量导入最多支持多少条患者数据？

**选项**:
- **A. 无限制**（简单）:
  - 不设置上限
  - 优点：实现简单
  - 缺点：大数据量可能导致性能问题
- **B. 限制1000条**（推荐）:
  - 单次最多导入1000条
  - 超过提示分批导入
  - 优点：避免性能问题，1000条对MVP阶段足够
  - 缺点：大诊所可能需要分批导入
- **C. 限制5000条**（宽松）:
  - 单次最多导入5000条
  - 优点：满足大部分场景
  - 缺点：性能测试工作量增加

**建议**: 选择 **B（限制1000条）** - 平衡性能和实用性，符合MVP原则

---

### Q4: 是否需要导入/导出历史记录？

**问题**: 是否需要记录每次导入/导出操作的历史记录？

**选项**:
- **A. 不记录历史**（推荐）:
  - 仅记录审计日志（操作人、时间、记录数）
  - 不保存导入/导出文件副本
  - 优点：简单，存储空间小
  - 缺点：无法追溯历史导入数据
- **B. 记录历史**（复杂）:
  - 保存每次导入/导出的Excel文件副本
  - 提供历史记录查询界面
  - 优点：可追溯，支持数据回滚
  - 缺点：存储空间占用大，实现复杂

**建议**: 选择 **A（不记录历史）** - MVP阶段保持简单，审计日志已满足合规要求

---

### Q5: 列表UI自适应规则是否作为全局标准？

**问题**: BR-005中定义的列表UI自适应规则，是否推广为所有管理模块的全局标准？

**选项**:
- **A. 仅应用于患者管理**（保守）:
  - 本次仅优化患者管理
  - 其他模块保持现状
  - 优点：改动范围小，风险低
  - 缺点：模块间UI不一致
- **B. 作为全局标准推广**（推荐）:
  - 定义为全局UI规范
  - 逐步优化所有管理模块（用户、验方、处方等）
  - 优点：UI一致性强，用户体验统一
  - 缺点：需要修改多个模块

**建议**: 选择 **B（作为全局标准推广）** - 但采取渐进式优化策略：
1. 本次优先优化患者管理（验证效果）
2. 将规则文档化（写入CLAUDE.md或UI规范文档）
3. 后续迭代逐步优化其他模块（用户、验方、处方等）

---

## 📎 附录

### A. 参考文档

- [用户管理技术设计](./architecture/shared/user-management-technical-design.md)
- [验方管理实现](../src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaManagementView.xaml)
- [患者管理现有实现](../src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientManagementView.xaml)
- [Epic #1911: 用户管理调用链修复](https://github.com/shouqitao/LYBTZYZS/issues/1911)
- [三层架构指南](./architecture/README.md)
- [MVP Constitution](./.spec-workflow/steering/constitution.md)

### B. Excel模板列说明

| 列名 | 是否必填 | 数据类型 | 格式/范围 | 示例 | 说明 |
|------|---------|---------|----------|------|------|
| 姓名* | ✅ 必填 | 文本 | 2-20字符 | 张三 | 患者真实姓名 |
| 性别* | ✅ 必填 | 枚举 | 男/女 | 男 | 仅支持"男"或"女" |
| 出生日期* | ✅ 必填 | 日期 | YYYY-MM-DD | 1980-05-15 | 1900-01-01 ~ 今天 |
| 手机号* | ✅ 必填 | 文本 | 11位数字 | 13800138000 | 用于联系和识别患者 |
| 身份证号 | 可选 | 文本 | 18位或15位 | 110101198005151234 | 需通过校验位验证 |
| 地址 | 可选 | 文本 | ≤500字符 | 北京市朝阳区 | 患者居住地址 |
| 过敏史 | 可选 | 文本 | ≤1000字符 | 青霉素 | 药物/食物过敏史 |
| 既往病史 | 可选 | 文本 | ≤2000字符 | 高血压 | 重要病史记录 |
| 备注 | 可选 | 文本 | ≤2000字符 | 示例数据 | 其他补充信息 |

### C. API端点设计草案

**新增Server API端点**:

```csharp
// POST /api/patients/batch - 批量添加患者
[HttpPost("batch")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<BatchImportResultDto>> BatchCreate(
    [FromBody] CreatePatientDto[] patients)
{
    var result = await _patientService.BatchCreateAsync(patients);
    return Ok(result);
}

// PUT /api/patients/{id} - 更新患者信息
[HttpPut("{id}")]
[Authorize(Roles = "Admin,Doctor")]
public async Task<ActionResult<PatientDto>> Update(
    int id,
    [FromBody] UpdatePatientDto dto)
{
    var patient = await _patientService.UpdateAsync(id, dto);
    return Ok(patient);
}
```

**新增DTO定义**:

```csharp
// Shared/Dtos/Patients/UpdatePatientDto.cs
public class UpdatePatientDto
{
    public string Name { get; set; }
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string PhoneNumber { get; set; }
    public string? IdNumber { get; set; }
    public string? Address { get; set; }
    public string? AllergyHistory { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Notes { get; set; }
}

// Shared/Dtos/Patients/BatchImportResultDto.cs
public class BatchImportResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int SkippedCount { get; set; }
    public List<ImportFailureDetail> Failures { get; set; }
}

public class ImportFailureDetail
{
    public int RowNumber { get; set; }
    public string Name { get; set; }
    public string FailureReason { get; set; }
}
```

---

**下一步**:
1. ✅ 用户确认需求（特别是5个开放问题Q1-Q5）
2. ⏭️ 调用 `lybtzyzs-design-generator` 生成技术设计文档
3. ⏭️ 调用 `lybtzyzs-task-breakdown` 拆分实施任务
4. ⏭️ 调用 `lybtzyzs-issue-template` 批量创建GitHub Issues

---

**文档维护**: Claude Code
**最后更新**: 2025-11-09
