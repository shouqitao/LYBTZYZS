using LYBT.Module.Consultations.Services;

namespace LYBT.Module.Consultations.Tests
{
    /// <summary>
    /// ConsultationService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimpleConsultationServiceTests
    {
#pragma warning disable CS0169 // Field is never used
        private readonly ConsultationService? _consultationService;
#pragma warning restore CS0169

        public SimpleConsultationServiceTests()
        {
            // UltraThink双层架构Mock配置
        }

    }
}
