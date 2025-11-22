# 中医诊断常见问题解决指南

## 概述

本文档提供中医诊断模块日常使用中的常见问题解决方案，帮助医师快速解决诊断过程中遇到的技术和业务问题，确保诊断数据的准确性和完整性。

## 目录

- [四诊信息采集问题](#四诊信息采集问题)
- [舌诊图像识别问题](#舌诊图像识别问题)
- [脉诊数据记录问题](#脉诊数据记录问题)
- [辨证分析准确性问题](#辨证分析准确性问题)
- [数据同步和保存问题](#数据同步和保存问题)
- [权限和协作问题](#权限和协作问题)
- [诊断报告生成问题](#诊断报告生成问题)
- [历史诊断记录问题](#历史诊断记录问题)

---

## 四诊信息采集问题

### 问题1：望诊信息记录不完整

**症状**: 望诊信息字段填写不完整，缺少关键体征描述

**原因分析**:
- 望诊标准不明确
- 字段设计不够直观
- 缺少望诊要点提示

**解决方案**:

#### 1.1 建立望诊检查清单

```csharp
public class InspectionChecklistService
{
    private readonly Dictionary<string, string[]> _inspectionCategories = new()
    {
        ["神色"] = new[] { "精神状态", "面色", "表情", "姿态" },
        ["形态"] = new[] { "体型", "发育", "营养", "姿态" },
        ["皮肤"] = new[] { "色泽", "湿润度", "皮疹", "瘢痕" },
        ["毛发"] = new[] { "光泽", "分布", "脱落", "颜色" },
        ["五官"] = new[] { "眼", "耳", "鼻", "口", "咽喉" }
    };

    public InspectionChecklistViewModel GenerateInspectionChecklist()
    {
        var checklist = new InspectionChecklistViewModel();

        foreach (var category in _inspectionCategories)
        {
            checklist.Categories.Add(new InspectionCategoryViewModel
            {
                Name = category.Key,
                Items = category.Value.Select(item => new InspectionItemViewModel
                {
                    Name = item,
                    Required = item switch
                    {
                        "精神状态" => true,
                        "面色" => true,
                        "表情" => true,
                        _ => false
                    }
                }).ToList()
            });
        }

        return checklist;
    }
}
```

#### 1.2 智能望诊提示系统

```csharp
public class InspectionPromptService
{
    public List<string> GetInspectionPrompts(string chiefComplaint)
    {
        var prompts = new List<string>();

        // 根据主诉生成相应的望诊提示
        if (chiefComplaint.Contains("发热"))
        {
            prompts.AddRange(new[]
            {
                "面色：是否红赤、潮红或苍白",
                "皮肤：是否有皮疹或出血点",
                "眼结膜：是否充血或苍白",
                "唇色：是否红紫或苍白"
            });
        }

        if (chiefComplaint.Contains("咳嗽"))
        {
            prompts.AddRange(new[]
            {
                "咽喉：是否充血红肿",
                "面色：是否红赤或晦暗",
                "鼻翼：是否有煽动",
                "胸廓：形态是否正常"
            });
        }

        return prompts;
    }

    public bool ValidateInspectionCompleteness(InspectionData inspection)
    {
        var requiredFields = new[]
        {
            inspection.GeneralAppearance,
            inspection.FacialColor,
            inspection.FacialExpression
        };

        return requiredFields.All(field => !string.IsNullOrWhiteSpace(field?.Trim()));
    }
}
```

#### 1.3 XAML界面优化

```xml
<!-- 望诊信息录入界面 -->
<StackPanel>
    <TextBlock Text="望诊检查清单"
               Style="{StaticResource HeaderTextBlockStyle}" />

    <ItemsControl ItemsSource="{Binding InspectionChecklist.Categories}">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border Style="{StaticResource CategoryBorderStyle}">
                    <StackPanel>
                        <TextBlock Text="{Binding Name}"
                                   Style="{StaticResource CategoryHeaderStyle}" />
                        <ItemsControl ItemsSource="{Binding Items}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Grid Margin="0,2">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="Auto" />
                                            <ColumnDefinition Width="*" />
                                            <ColumnDefinition Width="Auto" />
                                        </Grid.ColumnDefinitions>

                                        <CheckBox Grid.Column="0"
                                                  IsChecked="{Binding IsChecked}" />
                                        <TextBlock Grid.Column="1"
                                                   Text="{Binding Name}"
                                                   Margin="8,0" />
                                        <TextBlock Grid.Column="2"
                                                   Text="*"
                                                   Foreground="Red"
                                                   Visibility="{Binding Required, Converter={StaticResource BooleanToVisibilityConverter}}" />
                                    </Grid>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</StackPanel>
```

### 问题2：闻诊信息过于简单

**症状**: 闻诊记录过于简单，缺少气味、声音等详细信息

**解决方案**:

#### 2.1 闻诊信息结构化

```csharp
public class AuscultationOlfactionData
{
    // 声音闻诊
    public VoiceCharacteristics Voice { get; set; }
    public RespiratorySounds Respiratory { get; set; }
    public GastrointestinalSounds GI { get; set; }

    // 气味闻诊
    public BodyOdor BodyOdor { get; set; }
    public BreathOdor BreathOdor { get; set; }
    public ExcretionOdor ExcretionOdor { get; set; }
}

public class VoiceCharacteristics
{
    public string Volume { get; set; }        // 高亢、低沉、正常
    public string Strength { get; set; }      // 有力、无力、嘶哑
    public string Clarity { get; set; }      // 清晰、含糊、断续
    public string Tone { get; set; }         // 调高、调低
}

public class RespiratorySounds
{
    public string BreathingSound { get; set; }     // 呼吸音
    public string CoughSound { get; set; }         // 咳嗽声
    public string SputumSound { get; set; }        // 痰声
}
```

#### 2.2 智能闻诊辅助

```csharp
public class AuscultationHelper
{
    public List<string> GetVoicePrompts()
    {
        return new List<string>
        {
            "声音：高亢洪亮（实证）、低沉无力（虚证）",
            "语音：清晰流利、含糊不清、语言謇涩",
            "呼吸：呼吸平稳、气粗、气短、喘息",
            "咳嗽：干咳、湿咳、顿咳、夜间咳嗽"
        };
    }

    public List<string> GetOdorPrompts()
    {
        return new List<string>
        {
            "口气：正常、口臭、酸臭、腥臭",
            "汗味：正常汗味、汗臭、无汗",
            "排泄物：大便、小便、呕吐物气味",
            "体味：正常、异常体味、特殊气味"
        };
    }
}
```

### 问题3：问诊信息系统性不足

**症状**: 问诊缺乏系统性，重要病史信息遗漏

**解决方案**:

#### 3.1 十问歌系统化问诊

```csharp
public class SystematicInquiryService
{
    public class InquirySections
    {
        public InquirySection 寒热 { get; set; }
        public InquirySection 汗 { get; set; }
        public InquirySection 头身 { get; set; }
        public InquirySection 便 { get; set; }
        public InquirySection 食 { get; set; }
        public InquirySection 胸腹 { get; set; }
        public InquirySection 耳 { get; set; }
        public InquirySection 渴 { get; set; }
    }

    public InquirySections GenerateSystematicInquiry()
    {
        return new InquirySections
        {
            寒热 = new InquirySection
            {
                Title = "寒热",
                Questions = new[]
                {
                    "有无恶寒发热",
                    "寒热往来的时间",
                    "怕冷还是怕热",
                    "手足温度如何"
                }
            },
            汗 = new InquirySection
            {
                Title = "汗",
                Questions = new[]
                {
                    "有无汗出",
                    "汗出的时间和部位",
                    "汗的性质（自汗、盗汗、无汗）",
                    "汗的量和颜色"
                }
            },
            // ... 其他问诊项目
        };
    }
}
```

#### 3.2 问诊信息智能关联

```csharp
public class InquiryCorrelationService
{
    public List<string> GetFollowUpQuestions(List<string> initialAnswers)
    {
        var followUpQuestions = new List<string>();

        foreach (var answer in initialAnswers)
        {
            switch (answer.ToLower())
            {
                case var s when s.Contains("发热"):
                    followUpQuestions.AddRange(new[]
                    {
                        "发热的具体温度",
                        "发热的时间规律",
                        "伴随的寒战情况",
                        "发热时的汗出情况"
                    });
                    break;

                case var s when s.Contains("咳嗽"):
                    followUpQuestions.AddRange(new[]
                    {
                        "咳嗽的性质（干咳、湿咳）",
                        "咳嗽的时间规律",
                        "痰的颜色和量",
                        "有无胸痛气促"
                    });
                    break;
            }
        }

        return followUpQuestions;
    }
}
```

---

## 舌诊图像识别问题

### 问题4：舌诊图像质量不佳

**症状**: 舌诊图像模糊、光线不均、色彩失真

**解决方案**:

#### 4.1 舌诊图像采集标准

```csharp
public class TongueImageQualityService
{
    public class ImageQualityStandards
    {
        public int MinResolution { get; } = 1920;  // 最小分辨率
        public int MaxResolution { get; } = 4096;  // 最大分辨率
        public double MinBrightness { get; } = 0.4;  // 最小亮度
        public double MaxBrightness { get; } = 0.8;  // 最大亮度
        public double MinContrast { get; } = 0.3;   // 最小对比度
        public double MinSharpness { get; } = 0.5;  // 最小清晰度
    }

    public ImageQualityResult ValidateImageQuality(byte[] imageData)
    {
        var result = new ImageQualityResult();

        using var image = Image.Load<Rgba32>(imageData);

        // 检查分辨率
        result.Resolution = Math.Max(image.Width, image.Height);
        result.IsResolutionValid = result.Resolution >= _standards.MinResolution &&
                                  result.Resolution <= _standards.MaxResolution;

        // 检查亮度和对比度
        var brightness = CalculateBrightness(image);
        result.Brightness = brightness;
        result.IsBrightnessValid = brightness >= _standards.MinBrightness &&
                                  brightness <= _standards.MaxBrightness;

        var contrast = CalculateContrast(image);
        result.Contrast = contrast;
        result.IsContrastValid = contrast >= _standards.MinContrast;

        // 检查清晰度
        var sharpness = CalculateSharpness(image);
        result.Sharpness = sharpness;
        result.IsSharpnessValid = sharpness >= _standards.MinSharpness;

        result.IsValid = result.IsResolutionValid &&
                        result.IsBrightnessValid &&
                        result.IsContrastValid &&
                        result.IsSharpnessValid;

        return result;
    }

    private double CalculateBrightness(Image<Rgba32> image)
    {
        long totalBrightness = 0;
        int pixelCount = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = row[x];
                    // 使用标准亮度计算公式
                    var brightness = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;
                    totalBrightness += brightness;
                    pixelCount++;
                }
            }
        });

        return totalBrightness / (pixelCount * 255.0);
    }
}
```

#### 4.2 舌诊图像增强处理

```csharp
public class TongueImageEnhancementService
{
    public byte[] EnhanceTongueImage(byte[] originalImage)
    {
        using var image = Image.Load<Rgba32>(originalImage);

        // 1. 色彩校正
        CorrectColorBalance(image);

        // 2. 对比度增强
        EnhanceContrast(image);

        // 3. 锐化处理
        SharpenImage(image);

        // 4. 降噪处理
        DenoiseImage(image);

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private void CorrectColorBalance(Image<Rgba32> image)
    {
        // 自动白平衡
        var avgR = 0.0;
        var avgG = 0.0;
        var avgB = 0.0;
        int pixelCount = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = row[x];
                    avgR += pixel.R;
                    avgG += pixel.G;
                    avgB += pixel.B;
                    pixelCount++;
                }
            }
        });

        avgR /= pixelCount;
        avgG /= pixelCount;
        avgB /= pixelCount;

        var grayValue = (avgR + avgG + avgB) / 3.0;
        var rScale = grayValue / avgR;
        var gScale = grayValue / avgG;
        var bScale = grayValue / avgB;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = row[x];
                    row[x] = new Rgba32(
                        (byte)Math.Clamp(pixel.R * rScale, 0, 255),
                        (byte)Math.Clamp(pixel.G * gScale, 0, 255),
                        (byte)Math.Clamp(pixel.B * bScale, 0, 255),
                        pixel.A
                    );
                }
            }
        });
    }
}
```

### 问题5：舌象分析结果不准确

**症状**: 系统自动分析的舌象结果与实际观察不符

**解决方案**:

#### 5.1 舌象分析算法优化

```csharp
public class TongueAnalysisService
{
    public TongueAnalysisResult AnalyzeTongue(byte[] tongueImage)
    {
        var result = new TongueAnalysisResult();

        using var image = Image.Load<Rgba32>(tongueImage);

        // 1. 舌体分割
        var tongueRegion = SegmentTongue(image);

        // 2. 舌质分析
        result.TongueBody = AnalyzeTongueBody(image, tongueRegion);

        // 3. 舌苔分析
        result.TongueCoating = AnalyzeTongueCoating(image, tongueRegion);

        // 4. 舌形分析
        result.TongueShape = AnalyzeTongueShape(image, tongueRegion);

        // 5. 舌下络脉分析
        result.SublingualVeins = AnalyzeSublingualVeins(image);

        // 6. 生成中医诊断建议
        result.TCMDiagnosis = GenerateTCMDiagnosis(result);

        return result;
    }

    private TongueBodyAnalysis AnalyzeTongueBody(Image<Rgba32> image, Rectangle tongueRegion)
    {
        var analysis = new TongueBodyAnalysis();

        // 提取舌体区域
        var tongueBodyArea = ExtractTongueBodyArea(image, tongueRegion);

        // 颜色分析
        var dominantColors = ExtractDominantColors(tongueBodyArea);
        analysis.Color = AnalyzeTongueColor(dominantColors);

        // 质地分析
        var textureFeatures = ExtractTextureFeatures(tongueBodyArea);
        analysis.Texture = AnalyzeTongueTexture(textureFeatures);

        // 动态分析（如果有视频）
        analysis.Mobility = AnalyzeTongueMobility(image);

        return analysis;
    }

    private TongueColor AnalyzeTongueColor(List<Color> dominantColors)
    {
        var avgColor = CalculateAverageColor(dominantColors);

        // 基于HSV颜色空间分析
        var hsv = ColorToHsv(avgColor);

        return hsv.Hue switch
        {
            < 10 or > 350 => TongueColor.淡红,    // 正常舌色
            >= 10 and < 25 => TongueColor.红,       // 红舌
            >= 25 and < 45 => TongueColor.暗红,     // 暗红舌
            >= 45 and < 65 => TongueColor.紫,       // 紫舌
            >= 65 and < 200 => TongueColor.淡白,    // 淡白舌
            _ => TongueColor.正常
        };
    }
}
```

#### 5.2 舌象验证和校正

```csharp
public class TongueAnalysisValidationService
{
    public ValidationResult ValidateTongueAnalysis(TongueAnalysisResult analysis,
                                                   string clinicalObservation)
    {
        var result = new ValidationResult();

        // 验证舌色
        var colorValidation = ValidateTongueColor(analysis.TongueBody.Color, clinicalObservation);
        result.ColorValidation = colorValidation;

        // 验证舌苔
        var coatingValidation = ValidateTongueCoating(analysis.TongueCoating, clinicalObservation);
        result.CoatingValidation = coatingValidation;

        // 验证舌形
        var shapeValidation = ValidateTongueShape(analysis.TongueShape, clinicalObservation);
        result.ShapeValidation = shapeValidation;

        result.IsValid = colorValidation.IsValid &&
                        coatingValidation.IsValid &&
                        shapeValidation.IsValid;

        return result;
    }

    public TongueAnalysisResult CorrectAnalysis(TongueAnalysisResult original,
                                              ValidationResult validation,
                                              string physicianCorrection)
    {
        var corrected = original.Clone();

        // 根据医生校正调整分析结果
        if (validation.ColorValidation.NeedsCorrection)
        {
            corrected.TongueBody.Color = ParseColorFromText(physicianCorrection);
        }

        if (validation.CoatingValidation.NeedsCorrection)
        {
            corrected.TongueCoating = ParseCoatingFromText(physicianCorrection);
        }

        // 重新学习校正模式
        LearnCorrectionPattern(original, corrected, physicianCorrection);

        return corrected;
    }

    private void LearnCorrectionPattern(TongueAnalysisResult original,
                                      TongueAnalysisResult corrected,
                                      string correction)
    {
        // 记录校正模式用于机器学习改进
        var correctionRecord = new TongueAnalysisCorrection
        {
            OriginalResult = original,
            CorrectedResult = corrected,
            PhysicianNote = correction,
            Timestamp = DateTime.UtcNow
        };

        // 保存到训练数据集
        _trainingDataService.AddCorrectionRecord(correctionRecord);
    }
}
```

---

## 脉诊数据记录问题

### 问题6：脉诊记录标准化不足

**症状**: 脉诊描述不规范，缺少量化标准

**解决方案**:

#### 6.1 脉诊量化标准

```csharp
public class PulseQuantificationService
{
    public class PulseCharacteristics
    {
        // 脉位
        public PulsePosition Position { get; set; }    // 浮、中、沉
        public float PositionValue { get; set; }       // 0-1量化值

        // 脉率
        public int Rate { get; set; }                  // 次/分钟
        public PulseRhythm Rhythm { get; set; }        // 结、代、促、缓、数

        // 脉力
        public PulseStrength Strength { get; set; }    // 无力、有力、实脉
        public float StrengthValue { get; set; }       // 0-1量化值

        // 脉形
        public PulseShape Shape { get; set; }          // 弦、滑、涩、紧、濡
        public float Tension { get; set; }             // 紧张度 0-1

        // 脉势
        public PulseTendency Tendency { get; set; }    // 流利度、充实度
    }

    public PulseCharacteristics QuantifyPulse(PulseInputData pulseData)
    {
        var result = new PulseCharacteristics();

        // 脉位分析
        result.Position = AnalyzePulsePosition(pulseData.PressureLevels);
        result.PositionValue = pulseData.PressureLevels.Average();

        // 脉率计算
        result.Rate = CalculatePulseRate(pulseData.HeartbeatIntervals);
        result.Rhythm = AnalyzePulseRhythm(pulseData.HeartbeatIntervals);

        // 脉力分析
        result.Strength = AnalyzePulseStrength(pulseData.PulseAmplitude);
        result.StrengthValue = pulseData.PulseAmplitude.Average();

        // 脉形分析
        result.Shape = AnalyzePulseShape(pulseData.Waveform);
        result.Tension = CalculateTension(pulseData.Waveform);

        // 脉势分析
        result.Tendency = AnalyzePulseTendency(pulseData);

        return result;
    }

    private PulsePosition AnalyzePulsePosition(PressureLevelData pressureLevels)
    {
        var surfaceLevel = pressureLevels.SurfacePressure;
        var middleLevel = pressureLevels.MiddlePressure;
        var deepLevel = pressureLevels.DeepPressure;

        if (surfaceLevel > middleLevel && surfaceLevel > deepLevel)
            return PulsePosition.浮;
        else if (deepLevel > surfaceLevel && deepLevel > middleLevel)
            return PulsePosition.沉;
        else
            return PulsePosition.中;
    }
}
```

#### 6.2 脉诊辅助设备集成

```csharp
public class PulseDeviceIntegrationService
{
    public async Task<PulseCharacteristics> ReadFromPulseDeviceAsync(string deviceId)
    {
        try
        {
            // 连接脉诊设备
            var device = await ConnectToPulseDevice(deviceId);

            // 采集脉诊数据
            var rawData = await device.CollectPulseDataAsync(TimeSpan.FromSeconds(30));

            // 数据预处理
            var processedData = PreprocessPulseData(rawData);

            // 特征提取
            var features = ExtractPulseFeatures(processedData);

            // 脉象分析
            var analysis = AnalyzePulseFeatures(features);

            return analysis;
        }
        catch (DeviceConnectionException ex)
        {
            _logger.LogError(ex, "脉诊设备连接失败: {DeviceId}", deviceId);
            throw new PulseDiagnosticException("无法连接脉诊设备", ex);
        }
    }

    private ProcessedPulseData PreprocessPulseData(RawPulseData rawData)
    {
        var processed = new ProcessedPulseData();

        // 滤波去噪
        processed.FilteredWaveform = ApplyBandPassFilter(rawData.Waveform);

        // 基线校正
        processed.CorrectedWaveform = CorrectBaseline(processed.FilteredWaveform);

        // 心跳检测
        processed.Heartbeats = DetectHeartbeats(processed.CorrectedWaveform);

        // 脉率计算
        processed.HeartRate = CalculateHeartRate(processed.Heartbeats);

        return processed;
    }
}
```

### 问题7：脉诊数据与临床不符

**症状**: 系统分析的脉象与医师实际感受不符

**解决方案**:

#### 7.1 脉诊对比学习

```csharp
public class PulseLearningService
{
    public void LearnPulsePattern(PulseCharacteristics systemResult,
                                 PulseCharacteristics physicianResult,
                                 string caseContext)
    {
        var learningRecord = new PulseLearningRecord
        {
            SystemResult = systemResult,
            PhysicianResult = physicianResult,
            CaseContext = caseContext,
            Timestamp = DateTime.UtcNow,
            Confidence = CalculateLearningConfidence(systemResult, physicianResult)
        };

        // 更新脉诊模型
        UpdatePulseModel(learningRecord);
    }

    private float CalculateLearningConfidence(PulseCharacteristics system, PulseCharacteristics physician)
    {
        var positionDiff = Math.Abs((float)system.Position - (float)physician.Position);
        var strengthDiff = Math.Abs(system.StrengthValue - physician.StrengthValue);
        var rateDiff = Math.Abs(system.Rate - physician.Rate) / 60.0f;

        // 计算相似度
        var similarity = 1.0f - (positionDiff + strengthDiff + rateDiff) / 3.0f;

        return Math.Clamp(similarity, 0, 1);
    }
}
```

---

## 辨证分析准确性问题

### 问题8：八纲辨证结果不准确

**症状**: 系统生成的八纲辨证与中医理论不符

**解决方案**:

#### 8.1 八纲辨证规则引擎

```csharp
public class EightPrincipleSyndromeService
{
    public EightPrincipleSyndrome AnalyzeEightPrinciples(DiagnosticData diagnosticData)
    {
        var syndrome = new EightPrincipleSyndrome();

        // 表里辨证
        syndrome.ExteriorInterior = AnalyzeExteriorInterior(diagnosticData);

        // 寒热辨证
        syndrome.ColdHeat = AnalyzeColdHeat(diagnosticData);

        // 虚实辨证
        syndrome.DeficiencyExcess = AnalyzeDeficiencyExcess(diagnosticData);

        // 阴阳辨证（综合判断）
        syndrome.YinYang = AnalyzeYinYang(syndrome);

        return syndrome;
    }

    private ExteriorInteriorSyndrome AnalyzeExteriorInterior(DiagnosticData data)
    {
        var score = new ExteriorInteriorScore();

        // 病程评分
        score.DurationScore = data.DurationDays switch
        {
            <= 3 => 1.0f,   // 表证
            <= 7 => 0.5f,   // 半表半里
            > 7 => -1.0f    // 里证
        };

        // 症状评分
        score.SymptomScore = CalculateExteriorInteriorSymptomScore(data.Symptoms);

        // 舌脉评分
        score.TonguePulseScore = CalculateExteriorInteriorTonguePulseScore(data.Tongue, data.Pulse);

        // 综合判断
        var totalScore = score.DurationScore + score.SymptomScore + score.TonguePulseScore;

        return totalScore switch
        {
            > 1.0 => ExteriorInteriorSyndrome.表证,
            < -1.0 => ExteriorInteriorSyndrome.里证,
            _ => ExteriorInteriorSyndrome.半表半里证
        };
    }

    private ColdHeatSyndrome AnalyzeColdHeat(DiagnosticData data)
    {
        var score = new ColdHeatScore();

        // 症状评分
        if (data.Symptoms.Contains("恶寒") && !data.Symptoms.Contains("发热"))
            score.SymptomScore -= 2.0f;  // 寒证
        else if (data.Symptoms.Contains("发热") && !data.Symptoms.Contains("恶寒"))
            score.SymptomScore += 2.0f;  // 热证

        // 舌象评分
        score.TongueScore = data.Tongue.BodyColor switch
        {
            TongueColor.淡白 or TongueColor.淡红 => -1.0f,  // 寒证
            TongueColor.红 or TongueColor.暗红 => 1.0f,     // 热证
            _ => 0
        };

        // 脉象评分
        score.PulseScore = data.Pulse.Rate switch
        {
            < 60 => -1.0f,   // 迟脉（寒）
            > 90 => 1.0f,    // 数脉（热）
            _ => 0
        };

        var totalScore = score.SymptomScore + score.TongueScore + score.PulseScore;

        return totalScore switch
        {
            > 1.0 => ColdHeatSyndrome.热证,
            < -1.0 => ColdHeatSyndrome.寒证,
            _ => ColdHeatSyndrome.寒热错杂
        };
    }
}
```

#### 8.2 脏腑辨证优化

```csharp
public class OrganSyndromeService
{
    public OrganSyndrome AnalyzeOrganSyndrome(DiagnosticData diagnosticData)
    {
        var result = new OrganSyndrome();

        // 心系辨证
        result.HeartSyndrome = AnalyzeHeartSyndrome(diagnosticData);

        // 肝系辨证
        result.LiverSyndrome = AnalyzeLiverSyndrome(diagnosticData);

        // 脾系辨证
        result.SpleenSyndrome = AnalyzeSpleenSyndrome(diagnosticData);

        // 肺系辨证
        result.LungSyndrome = AnalyzeLungSyndrome(diagnosticData);

        // 肾系辨证
        result.KidneySyndrome = AnalyzeKidneySyndrome(diagnosticData);

        // 胆系辨证
        result.GallbladderSyndrome = AnalyzeGallbladderSyndrome(diagnosticData);

        // 胃系辨证
        result.StomachSyndrome = AnalyzeStomachSyndrome(diagnosticData);

        // 肠系辨证
        result.IntestineSyndrome = AnalyzeIntestineSyndrome(diagnosticData);

        return result;
    }

    private HeartSyndrome AnalyzeHeartSyndrome(DiagnosticData data)
    {
        var syndrome = new HeartSyndrome();

        // 心气虚证
        if (CheckHeartQiDeficiency(data))
            syndrome.HeartQiDeficiency = true;

        // 心阳虚证
        if (CheckHeartYangDeficiency(data))
            syndrome.HeartYangDeficiency = true;

        // 心血虚证
        if (CheckHeartBloodDeficiency(data))
            syndrome.HeartBloodDeficiency = true;

        // 心阴虚证
        if (CheckHeartYinDeficiency(data))
            syndrome.HeartYinDeficiency = true;

        // 心火亢盛证
        if (CheckHeartFireExcess(data))
            syndrome.HeartFireExcess = true;

        // 心脉痹阻证
        if (CheckHeartBloodStasis(data))
            syndrome.HeartBloodStasis = true;

        return syndrome;
    }

    private bool CheckHeartQiDeficiency(DiagnosticData data)
    {
        var symptoms = new[]
        {
            "心悸", "气短", "自汗", "活动后加重", "乏力"
        };

        var tongueSigns = new[]
        {
            TongueColor.淡白, TongueShape.胖大
        };

        var pulseSigns = new[]
        {
            PulseStrength.无力, PulseShape.结脉, PulseShape.代脉
        };

        return symptoms.Count(s => data.Symptoms.Contains(s)) >= 2 &&
               tongueSigns.Contains(data.Tongue.BodyColor) &&
               pulseSigns.Contains(data.Pulse.Strength);
    }
}
```

---

## 数据同步和保存问题

### 问题9：诊断数据同步失败

**症状**: 离线诊断数据无法正常同步到服务器

**解决方案**:

#### 9.1 离线数据缓存策略

```csharp
public class OfflineDiagnosticDataService
{
    private readonly IOfflineDataCache _offlineCache;
    private readonly IDiagnosticDataRepository _onlineRepository;

    public async Task SaveDiagnosticDataAsync(DiagnosticData data)
    {
        try
        {
            // 尝试在线保存
            if (await IsOnlineAsync())
            {
                await _onlineRepository.SaveAsync(data);
                // 清除本地缓存
                await _offlineCache.RemoveAsync(data.Id);
            }
            else
            {
                // 离线保存到本地
                await _offlineCache.SaveAsync(data);
            }
        }
        catch (NetworkException ex)
        {
            _logger.LogWarning(ex, "网络异常，数据已保存到本地缓存");
            await _offlineCache.SaveAsync(data);
        }
    }

    public async Task SyncOfflineDataAsync()
    {
        if (!await IsOnlineAsync())
        {
            _logger.LogInformation("当前离线状态，跳过同步");
            return;
        }

        try
        {
            var offlineData = await _offlineCache.GetAllAsync();
            var syncResults = new List<SyncResult>();

            foreach (var data in offlineData)
            {
                try
                {
                    await _onlineRepository.SaveAsync(data);
                    await _offlineCache.RemoveAsync(data.Id);
                    syncResults.Add(new SyncResult
                    {
                        DataId = data.Id,
                        Status = SyncStatus.Success,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "同步诊断数据失败: {DataId}", data.Id);
                    syncResults.Add(new SyncResult
                    {
                        DataId = data.Id,
                        Status = SyncStatus.Failed,
                        ErrorMessage = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            await ReportSyncResults(syncResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "离线数据同步过程发生异常");
            throw;
        }
    }
}
```

#### 9.2 数据冲突解决

```csharp
public class DiagnosticDataConflictResolver
{
    public async Task<ConflictResolution> ResolveConflictAsync(
        DiagnosticData localData,
        DiagnosticData serverData,
        ConflictType conflictType)
    {
        switch (conflictType)
        {
            case ConflictType.BothModified:
                return await ResolveBothModified(localData, serverData);

            case ConflictType.ServerModified:
                return await ResolveServerModified(localData, serverData);

            case ConflictType.LocalModified:
                return await ResolveLocalModified(localData, serverData);

            default:
                return ConflictResolution.UseLocal;
        }
    }

    private async Task<ConflictResolution> ResolveBothModified(
        DiagnosticData localData,
        DiagnosticData serverData)
    {
        // 智能合并策略
        var mergedData = await MergeDiagnosticData(localData, serverData);

        // 如果可以自动合并
        if (mergedData != null)
        {
            return new ConflictResolution
            {
                Action = ConflictAction.Merge,
                MergedData = mergedData
            };
        }

        // 需要手动解决冲突
        return new ConflictResolution
        {
            Action = ConflictAction.Manual,
            ConflictDescription = GenerateConflictDescription(localData, serverData)
        };
    }

    private async Task<DiagnosticData> MergeDiagnosticData(
        DiagnosticData localData,
        DiagnosticData serverData)
    {
        var merged = localData.Clone();

        // 合并策略：以最新修改的字段为准
        if (serverData.LastModified > localData.LastModified)
        {
            merged.ChiefComplaint = serverData.ChiefComplaint ?? localData.ChiefComplaint;
            merged.PresentIllness = serverData.PresentIllness ?? localData.PresentIllness;
            merged.TCMDiagnosis = serverData.TCMDiagnosis ?? localData.TCMDiagnosis;
        }

        // 特殊字段需要手动处理
        if (HasConflictingTongueData(localData, serverData))
        {
            return null; // 需要手动解决
        }

        merged.LastModified = DateTime.UtcNow;
        merged.ModifiedBy = "System Merge";

        return merged;
    }
}
```

---

## 权限和协作问题

### 问题10：诊断权限控制不严格

**症状**: 不同权限医师可以查看和修改不该访问的诊断数据

**解决方案**:

#### 10.1 细粒度权限控制

```csharp
public class DiagnosticPermissionService
{
    public async Task<bool> CanAccessDiagnosticDataAsync(
        Guid userId,
        Guid diagnosticDataId,
        DiagnosticOperation operation)
    {
        var user = await _userService.GetByIdAsync(userId);
        var diagnosticData = await _diagnosticRepository.GetByIdAsync(diagnosticDataId);

        return operation switch
        {
            DiagnosticOperation.View => await CanViewDiagnosticData(user, diagnosticData),
            DiagnosticOperation.Edit => await CanEditDiagnosticData(user, diagnosticData),
            DiagnosticOperation.Delete => await CanDeleteDiagnosticData(user, diagnosticData),
            DiagnosticOperation.Share => await CanShareDiagnosticData(user, diagnosticData),
            _ => false
        };
    }

    private async Task<bool> CanViewDiagnosticData(User user, DiagnosticData diagnosticData)
    {
        // 1. 自己的诊断数据
        if (diagnosticData.DoctorId == user.Id)
            return true;

        // 2. 同科室医师（根据科室设置）
        if (await AreInSameDepartment(user.Id, diagnosticData.DoctorId))
            return true;

        // 3. 上级医师
        if (await IsSupervisor(user.Id, diagnosticData.DoctorId))
            return true;

        // 4. 特殊权限
        var specialPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);
        return specialPermissions.Contains("diagnostic.view.all");
    }

    private async Task<bool> CanEditDiagnosticData(User user, DiagnosticData diagnosticData)
    {
        // 当日编辑规则
        if (diagnosticData.CreatedAt.Date != DateTime.UtcNow.Date)
        {
            // 只有特殊权限才能编辑历史数据
            var specialPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);
            return specialPermissions.Contains("diagnostic.edit.historical");
        }

        // 1. 原诊断医师
        if (diagnosticData.DoctorId == user.Id)
            return true;

        // 2. 上级医师
        if (await IsSupervisor(user.Id, diagnosticData.DoctorId))
            return true;

        return false;
    }
}
```

#### 10.2 诊断协作机制

```csharp
public class DiagnosticCollaborationService
{
    public async Task<DiagnosticConsultation> RequestConsultationAsync(
        Guid diagnosticDataId,
        Guid requestedConsultantId,
        string consultationReason)
    {
        var consultation = new DiagnosticConsultation
        {
            Id = Guid.NewGuid(),
            DiagnosticDataId = diagnosticDataId,
            RequestedConsultantId = requestedConsultantId,
            RequesterId = _currentUser.Id,
            Reason = consultationReason,
            Status = ConsultationStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        // 保存会诊请求
        await _consultationRepository.AddAsync(consultation);

        // 发送通知
        await _notificationService.SendConsultationNotificationAsync(consultation);

        return consultation;
    }

    public async Task<ConsultationResponse> ProvideConsultationAsync(
        Guid consultationId,
        string consultationNotes,
        List<string> recommendations)
    {
        var consultation = await _consultationRepository.GetByIdAsync(consultationId);
        if (consultation == null)
            throw new ConsultationNotFoundException("会诊请求不存在");

        var response = new ConsultationResponse
        {
            Id = Guid.NewGuid(),
            ConsultationId = consultationId,
            ConsultantId = _currentUser.Id,
            Notes = consultationNotes,
            Recommendations = recommendations,
            ProvidedAt = DateTime.UtcNow
        };

        // 更新会诊状态
        consultation.Status = ConsultationStatus.Completed;
        consultation.ResponseId = response.Id;

        await _consultationRepository.UpdateAsync(consultation);
        await _responseRepository.AddAsync(response);

        // 通知原诊断医师
        await _notificationService.SendConsultationCompletedNotificationAsync(
            consultation.RequesterId, response);

        return response;
    }
}
```

---

## 诊断报告生成问题

### 问题11：诊断报告格式不统一

**症状**: 不同医师生成的诊断报告格式差异很大

**解决方案**:

#### 11.1 标准化诊断报告模板

```csharp
public class DiagnosticReportTemplateService
{
    public class StandardReportTemplate
    {
        public string PatientInfo { get; set; }
        public string ConsultationDate { get; set; }
        public string ChiefComplaint { get; set; }
        public string PresentIllness { get; set; }
        public string FourDiagnosticMethods { get; set; }
        public string TongueDiagnosis { get; set; }
        public string PulseDiagnosis { get; set; }
        public string TCMDiagnosis { get; set; }
        public string TreatmentPrinciple { get; set; }
        public string Suggestions { get; set; }
        public string DoctorSignature { get; set; }
    }

    public StandardReportTemplate GenerateReportTemplate(DiagnosticData diagnosticData)
    {
        return new StandardReportTemplate
        {
            PatientInfo = FormatPatientInfo(diagnosticData.Patient),
            ConsultationDate = diagnosticData.CreatedAt.ToString("yyyy年MM月dd日"),
            ChiefComplaint = diagnosticData.ChiefComplaint,
            PresentIllness = FormatPresentIllness(diagnosticData.PresentIllness),
            FourDiagnosticMethods = FormatFourDiagnosticMethods(diagnosticData),
            TongueDiagnosis = FormatTongueDiagnosis(diagnosticData.Tongue),
            PulseDiagnosis = FormatPulseDiagnosis(diagnosticData.Pulse),
            TCMDiagnosis = FormatTCMDiagnosis(diagnosticData.TCMDiagnosis),
            TreatmentPrinciple = diagnosticData.TreatmentPrinciple,
            Suggestions = GenerateSuggestions(diagnosticData),
            DoctorSignature = _currentUser.Name
        };
    }

    private string FormatFourDiagnosticMethods(DiagnosticData diagnosticData)
    {
        var sections = new List<string>();

        // 望诊
        if (!string.IsNullOrWhiteSpace(diagnosticData.Inspection))
        {
            sections.Add($"**望诊：**{diagnosticData.Inspection}");
        }

        // 闻诊
        if (!string.IsNullOrWhiteSpace(diagnosticData.AuscultationOlfaction))
        {
            sections.Add($"**闻诊：**{diagnosticData.AuscultationOlfaction}");
        }

        // 问诊
        if (!string.IsNullOrWhiteSpace(diagnosticData.Inquiry))
        {
            sections.Add($"**问诊：**{diagnosticData.Inquiry}");
        }

        // 切诊
        if (!string.IsNullOrWhiteSpace(diagnosticData.Palpation))
        {
            sections.Add($"**切诊：**{diagnosticData.Palpation}");
        }

        return string.Join("\n\n", sections);
    }
}
```

#### 11.2 报告导出功能

```csharp
public class DiagnosticReportExportService
{
    public async Task<byte[]> ExportToPdfAsync(Guid diagnosticDataId)
    {
        var diagnosticData = await _diagnosticRepository.GetByIdAsync(diagnosticDataId);
        var template = _templateService.GenerateReportTemplate(diagnosticData);

        using var document = new Document();
        var pdfWriter = new PdfWriter(new MemoryStream());
        var pdf = new PdfDocument(pdfWriter);
        document.SetPdfDocument(pdf);

        // 设置页面
        document.SetPageSize(PageSize.A4);
        var page = document.AddNewPage();

        using var canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdf);

        // 绘制报告内容
        await DrawReportHeader(canvas, diagnosticData);
        await DrawReportContent(canvas, template);
        await DrawReportFooter(canvas);

        document.Close();

        return ((MemoryStream)pdfWriter.Stream).ToArray();
    }

    public async Task<byte[]> ExportToWordAsync(Guid diagnosticDataId)
    {
        var diagnosticData = await _diagnosticRepository.GetByIdAsync(diagnosticDataId);
        var template = _templateService.GenerateReportTemplate(diagnosticData);

        using var document = WordDocument.CreateNew();

        // 添加标题
        document.AddParagraph("中医诊断报告")
                .Bold()
                .FontSize(16)
                .Alignment = ParagraphAlignment.Center;

        // 添加患者信息
        document.AddParagraph("患者信息")
                .Bold()
                .FontSize(14);
        document.AddParagraph(template.PatientInfo);

        // 添加诊断内容
        foreach (var section in GetReportSections(template))
        {
            document.AddParagraph(section.Title)
                    .Bold()
                    .FontSize(12);
            document.AddParagraph(section.Content);
        }

        return document.SaveToArray();
    }
}
```

---

## 历史诊断记录问题

### 问题12：历史诊断查询效率低

**症状**: 查询患者历史诊断记录响应缓慢

**解决方案**:

#### 12.1 诊断数据索引优化

```csharp
public class DiagnosticDataIndexService
{
    public async Task CreateDiagnosticIndexesAsync()
    {
        // 创建患者诊断历史索引
        await _indexService.CreateIndexAsync("patient_diagnostic_history", new IndexDefinition
        {
            Fields = new[]
            {
                new IndexField { Name = "PatientId", Type = FieldType.Keyword },
                new IndexField { Name = "DoctorId", Type = FieldType.Keyword },
                new IndexField { Name = "DiagnosticDate", Type = FieldType.Date },
                new IndexField { Name = "TCMDiagnosis", Type = FieldType.Text },
                new IndexField { Name = "ChiefComplaint", Type = FieldType.Text },
                new IndexField { Name = "Status", Type = FieldType.Keyword }
            }
        });

        // 创建舌诊特征索引
        await _indexService.CreateIndexAsync("tongue_features", new IndexDefinition
        {
            Fields = new[]
            {
                new IndexField { Name = "TongueColor", Type = FieldType.Keyword },
                new IndexField { Name = "TongueShape", Type = FieldType.Keyword },
                new IndexField { Name = "CoatingColor", Type = FieldType.Keyword },
                new IndexField { Name = "CoatingThickness", Type = FieldType.Keyword }
            }
        });

        // 创建脉诊特征索引
        await _indexService.CreateIndexAsync("pulse_features", new IndexDefinition
        {
            Fields = new[]
            {
                new IndexField { Name = "PulsePosition", Type = FieldType.Keyword },
                new IndexField { Name = "PulseRate", Type = FieldType.Integer },
                new IndexField { Name = "PulseStrength", Type = FieldType.Keyword },
                new IndexField { Name = "PulseShape", Type = FieldType.Keyword }
            }
        });
    }

    public async Task<List<DiagnosticData>> SearchPatientHistoryAsync(
        Guid patientId,
        DiagnosticSearchCriteria criteria)
    {
        var searchQuery = new SearchQuery
        {
            Filter = new TermFilter("PatientId", patientId.ToString()),
            Sort = new[]
            {
                new SortField { Field = "DiagnosticDate", Order = SortOrder.Descending }
            }
        };

        // 添加搜索条件
        if (!string.IsNullOrWhiteSpace(criteria.TCMDiagnosis))
        {
            searchQuery.MustQueries.Add(new MatchQuery("TCMDiagnosis", criteria.TCMDiagnosis));
        }

        if (criteria.StartDate.HasValue)
        {
            searchQuery.Filter = new RangeFilter("DiagnosticDate",
                new DateTimeRange { Gte = criteria.StartDate.Value });
        }

        if (criteria.EndDate.HasValue)
        {
            searchQuery.Filter = new RangeFilter("DiagnosticDate",
                new DateTimeRange { Lte = criteria.EndDate.Value });
        }

        var searchResult = await _searchService.SearchAsync("patient_diagnostic_history", searchQuery);

        return await _diagnosticRepository.GetByIdsAsync(
            searchResult.Hits.Select(h => Guid.Parse(h.Id)).ToList());
    }
}
```

#### 12.2 诊断趋势分析

```csharp
public class DiagnosticTrendAnalysisService
{
    public async Task<DiagnosticTrendAnalysis> AnalyzePatientDiagnosticTrendAsync(
        Guid patientId,
        int months = 12)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-months);

        var diagnosticHistory = await _diagnosticRepository.GetPatientHistoryAsync(
            patientId, startDate, endDate);

        var analysis = new DiagnosticTrendAnalysis
        {
            PatientId = patientId,
            AnalysisPeriod = new DateRange(startDate, endDate),
            TotalVisits = diagnosticHistory.Count
        };

        // 辨证趋势分析
        analysis.SyndromeTrends = AnalyzeSyndromeTrends(diagnosticHistory);

        // 舌象趋势分析
        analysis.TongueTrends = AnalyzeTongueTrends(diagnosticHistory);

        // 脉象趋势分析
        analysis.PulseTrends = AnalyzePulseTrends(diagnosticHistory);

        // 治疗效果分析
        analysis.TreatmentEffectiveness = AnalyzeTreatmentEffectiveness(diagnosticHistory);

        return analysis;
    }

    private List<SyndromeTrend> AnalyzeSyndromeTrends(List<DiagnosticData> history)
    {
        var trends = new List<SyndromeTrend>();

        // 按时间分组分析
        var groupedByPeriod = history
            .GroupBy(d => new DateTime(d.CreatedAt.Year, d.CreatedAt.Month, 1))
            .OrderBy(g => g.Key);

        foreach (var group in groupedByPeriod)
        {
            var trend = new SyndromeTrend
            {
                Period = group.Key,
                VisitCount = group.Count()
            };

            // 统计辨证类型
            var syndromeTypes = group
                .SelectMany(d => ParseSyndromeTypes(d.TCMDiagnosis))
                .GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count());

            trend.DominantSyndromes = syndromeTypes
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => new SyndromeFrequency
                {
                    Syndrome = kvp.Key,
                    Frequency = (double)kvp.Value / group.Count()
                })
                .ToList();

            trends.Add(trend);
        }

        return trends;
    }
}
```

---

## 常见错误码和解决方案

| 错误码 | 错误描述 | 解决方案 |
|--------|----------|----------|
| CONS001 | 望诊信息不完整 | 检查望诊清单，确保必填项完整 |
| CONS002 | 舌诊图像质量不合格 | 重新采集舌诊图像，确保光线和清晰度 |
| CONS003 | 脉诊数据异常 | 检查脉诊设备连接，重新采集数据 |
| CONS004 | 辨证分析失败 | 检查四诊信息是否完整和准确 |
| CONS005 | 诊断权限不足 | 联系系统管理员分配相应权限 |
| CONS006 | 数据同步失败 | 检查网络连接，尝试手动同步 |
| CONS007 | 报告生成失败 | 检查诊断数据完整性，重试生成 |

## 最佳实践

### 1. 四诊信息采集
- 使用结构化录入界面，确保信息完整性
- 定期校验和更新望诊标准
- 建立闻诊信息标准化词汇库
- 采用系统化问诊流程（十问歌）

### 2. 舌诊图像管理
- 严格按照采集标准获取舌诊图像
- 定期校准图像质量检测算法
- 建立舌象分析结果的验证机制
- 持续收集校正数据改进算法

### 3. 脉诊数据处理
- 使用标准化脉诊量化指标
- 定期校准脉诊设备
- 建立脉诊与临床症状的关联分析
- 记录和校正系统分析偏差

### 4. 辨证分析
- 综合运用多种辨证方法
- 定期更新辨证规则库
- 建立辨证结果的交叉验证机制
- 收集专家经验优化分析算法

### 5. 数据管理
- 实施严格的权限控制
- 建立完善的数据备份机制
- 定期进行数据同步验证
- 优化数据查询性能

通过遵循这些指南和最佳实践，可以有效解决中医诊断过程中的常见问题，提高诊断数据的准确性和系统的可靠性。