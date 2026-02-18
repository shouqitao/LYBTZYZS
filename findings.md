# Findings

## 任务来源
- 任务 A (信息保护深化): PRD 审查 A6 决策 -- "v1.0 字段级加密，以 nfr.md 为准"
- 任务 B (MedicalCase 同步设计): PRD 审查 E1 决策 -- "MedicalCase 同步必须支持"

---

## 任务 A: 信息保护深化

### 现状分析
1. **NFR-SEC-004 已定义**: SQLite 字段级加密 (IdCardNumber + PhoneNumber)，AES-256 + DPAPI
2. **DPAPI 基础设施已有**: CredentialVault (AutoLoginToken 加密)
3. **范围**: 仅 2 个字段，不整库加密
4. **密钥管理**: DPAPI 绑定 Windows 用户
5. **限制**: 加密字段不支持 SQLite LIKE 搜索

### 确认方案
- 3 级敏感数据分级 (L1高敏感/L2一般敏感/L3普通)
- EF Core Value Converter 实现透明加密/解密
- DPAPI 密钥生命周期: 首次启动生成 → DPAPI 保护 → 丢失则重新同步
- Migration 脚本做明文→加密迁移
- 日志脱敏规则细化
- 产出: 补充到 nfr.md + patients.md 对应章节

---

## 任务 B: MedicalCase 同步设计

### 核心场景
外出看诊离线工作流: 出诊前下载基础数据+历史医案 → 离线看诊创建医案 → 返回后同步

### 已确认设计决策

| # | 决策 | 理由 |
|---|------|------|
| B1 | 全状态双向同步 (Draft/Active/Completed) | 离线创建的任何状态医案都能同步回 Server |
| B2 | 聚合级原子同步 (MC+Consultation+Prescription+Items) | DDD 聚合一致性 |
| B3 | 打印字段不参与同步 | 打印是本地行为 |
| B4 | 自动强制依赖顺序: Herb → Patient → MedicalCase | 用户无需关心顺序 |
| B5 | 患者去重: IdCardNumber 匹配 → PatientId 重映射 | 忘记同步患者时的恢复路径 |
| B6 | CaseNumber/PrescriptionNumber: Server 重新分配 | 保持全局序列一致 |
| B7 | GUID (Id): 保留本地生成的 GUID | GUID 全局唯一无冲突 |
| B8 | BR-001 冲突: 提示医生选择处理方式 | 单活跃医案约束 |
| B9 | Checksum 排除: 审计字段 + 打印字段 + 编号字段 + 冗余字段 | 避免假差异 |

### 6 个关键细节 (已分析完成)
1. 患者去重 (IdCardNumber 匹配 + PatientId 重映射)
2. 聚合原子同步 (单事务写入)
3. 编号重分配 (CaseNumber/PrescriptionNumber)
4. BR-001 冲突处理 (提示选择)
5. 依赖引用完整性校验 (PatientId + HerbId)
6. Checksum 计算范围 (4层实体，排除非业务字段)

### 产出
补充到 sync.md 的 MedicalCase 同步设计节点
