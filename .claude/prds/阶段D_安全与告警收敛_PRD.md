# 阶段 D：安全与告警收敛 PRD

## 目标
- 消除 .NET 8 SYSLIB0053 告警：升级 AES‑GCM 的使用方式，明确 tag/nonce 尺寸，保证兼容性与安全性。

## 范围
- In Scope：`SecureConfigurationService` 中对 AES‑GCM 的加解密实现；相关单元测试新增。
- Out of Scope：密钥来源策略更改（仍使用当前派生/保护方式）。

## 交付物
- 使用新构造器的 AES‑GCM 实现（12 字节 nonce，16 字节 tag）；文档说明安全假设与限制。
- 针对样本明文的加解密单测，验证往返正确与异常路径（tag 验证失败）。

## 验收标准
- 无 SYSLIB0053 告警；单测通过；编译通过。

## 里程碑
1. 重构 `EncryptData/DecryptData`：明确分配 nonce/tag/ciphertext 布局，传入新构造器。
2. 新增 `SecureConfigurationServiceTests` 覆盖成功/失败场景。
3. 文档更新：风险与最佳实践（随机 nonce、不可重复使用等）。

## 风险与缓解
- 风险：与历史密文不兼容。缓解：保留老版本读取兼容分支（尝试旧布局失败后再试新布局），并带开关迁移。

## 依赖
- System.Security.Cryptography (net8.0)

## 回滚方案
- 恢复旧实现并用 `#pragma warning disable SYSLIB0053` 局部抑制，保业务连续性。

## 度量
- 告警数量 = 0；新增测试用例数 ≥ 4。

## 测试计划
- 单元测试：正常/错误 tag/错误 key/损坏密文。

## 受影响文件（示例）
- `src/Client/Desktop/Core/Services/Configuration/SecureConfigurationService.cs`
- `tests/UnitTests/*`（新增）

