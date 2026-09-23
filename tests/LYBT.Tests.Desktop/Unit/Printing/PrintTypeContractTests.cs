using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.Unit.Printing;

/// <summary>
/// <see cref="PrintType"/> 持久化取值契约守卫（F-06 Item B）。
/// </summary>
/// <remarks>
/// <c>MedicalCasePrintLog.PrintType</c>（DB 列）与 <c>RecordPrintRequest.PrintType</c>（API 契约）均为 <c>int</c>：
/// 枚举重排/改值会静默改变既有打印日志的含义（0 = 处方笺），因此锁定取值。
/// 需求依据：US-PRINT-004 业务规则 2（<c>Prescription</c>(0) 已实现；<c>Formula</c>(1) 预留）。
/// </remarks>
public class PrintTypeContractTests
{
    [Fact]
    public void PersistedValues_AreStable()
    {
        ((int)PrintType.Prescription).Should().Be(0, "既有 MedicalCasePrintLog 记录以 0 表示处方笺");
        ((int)PrintType.Formula).Should().Be(1, "验方打印为预留取值 1");
    }
}
