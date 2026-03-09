using LYBT.Desktop.Printing.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LYBT.Desktop.Printing.Services;

/// <summary>
/// PDF 处方笺导出器
/// D1: 使用 QuestPDF 生成处方笺 PDF，布局镜像 XAML 模板
/// </summary>
public static class PrescriptionPdfExporter
{
    private const float LabelFontSize = 9f;
    private const float ValueFontSize = 10f;
    private const float TitleFontSize = 14f;
    private const float ClinicNameFontSize = 12f;
    private const float HerbFontSize = 10f;

    /// <summary>
    /// 导出处方笺为 PDF 文件
    /// </summary>
    public static void Export(PrescriptionPrintModel model, string filePath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginTop(15, Unit.Millimetre);
                page.MarginBottom(10, Unit.Millimetre);
                page.MarginLeft(12, Unit.Millimetre);
                page.MarginRight(12, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Microsoft YaHei").FontSize(ValueFontSize));

                page.Content().Column(col =>
                {
                    // 诊所信息
                    ComposeClinicHeader(col, model);

                    // 标题
                    col.Item().AlignCenter().Text("普通处方笺").FontSize(TitleFontSize).Bold();
                    col.Item().PaddingBottom(4);

                    // 患者信息行1: 姓名/性别/年龄/时间
                    ComposePatientInfoRow1(col, model);

                    // 患者信息行2: 门诊号/科别/电话
                    ComposePatientInfoRow2(col, model);

                    // 住址
                    ComposeFieldRow(col, "住址", model.PatientAddress ?? "");

                    // 诊断
                    ComposeFieldRow(col, "诊断", model.TcmDiagnosis ?? "");

                    // 四诊
                    ComposeFourDiagnosis(col, model);

                    // 诊见
                    ComposeFieldRow(col, "诊见", model.SymptomsText);

                    col.Item().PaddingTop(4);

                    // Rp. + 药材
                    ComposePrescription(col, model);

                    // 弹性空白
                    col.Item().ExtendVertical();

                    // 分隔线
                    col.Item().LineHorizontal(1.5f);
                    col.Item().PaddingBottom(4);

                    // 签名行
                    ComposeSignatureRow(col, model);

                    // 费用行
                    ComposeFeeRow(col, model);
                });

                // D3: 草稿水印
                if (model.IsDraft)
                {
                    page.Foreground()
                        .AlignCenter().AlignMiddle()
                        .Rotate(-35)
                        .Text("草 稿")
                        .FontSize(72).Bold()
                        .FontFamily("Microsoft YaHei")
                        .FontColor(Color.FromHex("#30FF0000"));
                }
            });
        }).GeneratePdf(filePath);
    }

    private static void ComposeClinicHeader(ColumnDescriptor col, PrescriptionPrintModel model)
    {
        col.Item().AlignCenter().Text(model.ClinicName)
            .FontSize(ClinicNameFontSize).Bold();

        var clinicInfo = string.Join("  ",
            new[] { model.ClinicAddress, model.ClinicPhone }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrEmpty(clinicInfo))
        {
            col.Item().AlignCenter().Text(clinicInfo).FontSize(8);
        }
        col.Item().PaddingBottom(2);
    }

    private static void ComposePatientInfoRow1(ColumnDescriptor col, PrescriptionPrintModel model)
    {
        col.Item().Row(row =>
        {
            LabelValue(row, "姓名", model.PatientName, 2);
            LabelValue(row, "性别", model.Gender, 1);
            LabelValue(row, "年龄", $"{model.Age}岁", 1);
            LabelValue(row, "时间", model.ConsultationDate.ToString("yyyy年M月d日"), 2);
        });
        col.Item().PaddingBottom(2);
    }

    private static void ComposePatientInfoRow2(ColumnDescriptor col, PrescriptionPrintModel model)
    {
        col.Item().Row(row =>
        {
            LabelValue(row, "门诊号", model.OutpatientNumber ?? "", 2);
            LabelValue(row, "科别", model.Department, 1);
            LabelValue(row, "电话", model.PatientPhone ?? "", 2);
        });
        col.Item().PaddingBottom(2);
    }

    private static void ComposeFieldRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.AutoItem().Text($"{label}：").FontSize(LabelFontSize);
            row.RelativeItem().BorderBottom(0.5f).BorderColor(Colors.Black)
                .Text(value).FontSize(ValueFontSize);
        });
        col.Item().PaddingBottom(2);
    }

    private static void ComposeFourDiagnosis(ColumnDescriptor col, PrescriptionPrintModel model)
    {
        col.Item().Row(row =>
        {
            LabelValue(row, "望诊", model.InspectionDiagnosis ?? "", 1);
            LabelValue(row, "闻诊", model.AuscultationDiagnosis ?? "", 1);
        });
        col.Item().Row(row =>
        {
            LabelValue(row, "舌诊", model.TongueDiagnosis ?? "", 1);
            LabelValue(row, "脉诊", model.PulseDiagnosis ?? "", 1);
        });
        col.Item().PaddingBottom(2);
    }

    private static void ComposePrescription(ColumnDescriptor col, PrescriptionPrintModel model)
    {
        // Rp.
        col.Item().Text("Rp.").FontSize(12).Bold().Italic();
        col.Item().PaddingBottom(2);

        // 药材列表 (流式布局)
        if (model.Items.Count > 0)
        {
            col.Item().Text(text =>
            {
                foreach (var item in model.Items)
                {
                    text.Span($"{item.HerbName}{item.Dosage:0.##}{item.Unit}")
                        .FontSize(HerbFontSize);
                    text.Span("  ");
                }
            });
            col.Item().PaddingBottom(4);
        }

        // 服法
        col.Item().Text($"{model.DosageCount}剂，{model.Usage}").FontSize(ValueFontSize);

        // 医嘱
        if (!string.IsNullOrWhiteSpace(model.Advice))
        {
            col.Item().PaddingTop(2).Text($"医嘱：{model.Advice}").FontSize(LabelFontSize);
        }
    }

    private static void ComposeSignatureRow(ColumnDescriptor col, PrescriptionPrintModel model)
    {
        col.Item().Row(row =>
        {
            LabelValue(row, "医师签字", model.DoctorName, 1);
            LabelValue(row, "审核", model.Reviewer ?? "", 1);
            LabelValue(row, "调配", model.Dispenser ?? "", 1);
        });
        col.Item().PaddingBottom(2);
    }

    private static void ComposeFeeRow(ColumnDescriptor col, PrescriptionPrintModel model)
    {
        col.Item().Row(row =>
        {
            LabelValue(row, "诊疗费", $"{model.ConsultationFee:F0}", 1);
            LabelValue(row, "药费", $"{model.MedicineFee:F0}", 1);
            LabelValue(row, "折扣", $"{model.Discount:P0}", 1);
            LabelValue(row, "合计", $"{model.TotalPrice:F0}", 1);
        });
    }

    /// <summary>
    /// 标签+值 组合单元，带下划线
    /// </summary>
    private static void LabelValue(RowDescriptor row, string label, string value, int relativeSize)
    {
        row.AutoItem().PaddingRight(2).Text($"{label}：").FontSize(LabelFontSize);
        row.RelativeItem(relativeSize).BorderBottom(0.5f).BorderColor(Colors.Black)
            .Text(value).FontSize(ValueFontSize);
        row.AutoItem().PaddingRight(6).Text("");
    }
}
