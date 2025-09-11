using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Transactions;

// Removed: using LYBT.Infrastructure.Transactions.Steps; - DatabaseTransactionStep now in main namespace
using LYBT.Module.MedicalCase.Transactions.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Transactions
{
    /// <summary>
    /// 开始看诊流程事务定义
    /// 包含创建医疗案例、初始化诊断记录、更新患者状态的完整流程
    /// </summary>
    public class StartConsultationTransaction
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StartConsultationTransaction> _logger;

        public StartConsultationTransaction(
            IServiceProvider serviceProvider,
            ILogger<StartConsultationTransaction> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建看诊开始事务定义
        /// </summary>
        /// <param name="options">事务选项</param>
        /// <returns>事务定义</returns>
        public TransactionDefinition<ConsultationTransactionContext> CreateDefinition(ConsultationTransactionOptions? options = null)
        {
            options ??= new ConsultationTransactionOptions();

            var steps = CreateTransactionSteps(options);

            var definition = new TransactionDefinition<ConsultationTransactionContext>
            {
                Name = "StartConsultation",
                Description = "开始看诊流程：创建医疗案例、初始化诊断记录、更新患者状态",
                Steps = steps,
                Timeout = options.Timeout,
                MaxRetryCount = options.MaxRetryCount,
                EnableAutoCompensation = options.EnableAutoCompensation,
                EnableParallelExecution = false // 步骤之间有依赖关系，必须顺序执行
            };

            _logger.LogDebug("Created StartConsultation transaction definition with {StepCount} steps", steps.Count);
            return definition;
        }

        /// <summary>
        /// 执行看诊开始事务
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="options">事务选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>事务执行结果</returns>
        public async Task<TransactionResult<ConsultationTransactionContext>> ExecuteAsync(
            ConsultationTransactionContext context,
            ConsultationTransactionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // 验证上下文
            var (isValid, errors) = context.ValidateContext();
            if (!isValid)
            {
                _logger.LogError("Transaction context validation failed: {Errors}", string.Join(", ", errors));
                return new TransactionResult<ConsultationTransactionContext>
                {
                    TransactionId = Guid.NewGuid(),
                    Status = TransactionStatus.Failed,
                    Context = context,
                    Message = $"Context validation failed: {string.Join(", ", errors)}",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow
                };
            }

            // 创建事务定义
            var definition = CreateDefinition(options);

            // 获取事务协调器并执行
            var coordinator = _serviceProvider.GetRequiredService<ITransactionCoordinator<ConsultationTransactionContext>>();
            return await coordinator.ExecuteAsync(definition, context, cancellationToken);
        }

        /// <summary>
        /// 创建事务步骤列表
        /// </summary>
        /// <param name="options">事务选项</param>
        /// <returns>事务步骤列表</returns>
        private List<ITransactionStep<ConsultationTransactionContext>> CreateTransactionSteps(ConsultationTransactionOptions options)
        {
            var steps = new List<ITransactionStep<ConsultationTransactionContext>>();

            // 第一步：创建医疗案例
            if (options.IncludeCreateMedicalCase)
            {
                var createMedicalCaseStep = _serviceProvider.GetRequiredService<CreateMedicalCaseStep>();
                steps.Add(createMedicalCaseStep);
            }

            // 第二步：初始化诊断记录
            if (options.IncludeInitializeConsultation)
            {
                var initializeConsultationStep = _serviceProvider.GetRequiredService<InitializeConsultationStep>();
                steps.Add(initializeConsultationStep);
            }

            // 第三步：更新患者状态
            if (options.IncludeUpdatePatientStatus)
            {
                var updatePatientStatusStep = _serviceProvider.GetRequiredService<UpdatePatientStatusStep>();
                steps.Add(updatePatientStatusStep);
            }

            // 可以根据选项添加额外的步骤
            if (options.IncludeBusinessRuleValidation)
            {
                steps.Add(CreateBusinessRuleValidationStep());
            }

            if (options.IncludeNotification)
            {
                steps.Add(CreateNotificationStep());
            }

            return steps;
        }

        /// <summary>
        /// 创建业务规则验证步骤
        /// </summary>
        /// <returns>业务规则验证步骤</returns>
        private ITransactionStep<ConsultationTransactionContext> CreateBusinessRuleValidationStep()
        {
            // 可以创建一个专门的业务规则验证步骤
            // 这里为简化，返回一个条件步骤
            return new BusinessRuleValidationStep(_serviceProvider.GetRequiredService<ILogger<BusinessRuleValidationStep>>());
        }

        /// <summary>
        /// 创建通知步骤
        /// </summary>
        /// <returns>通知步骤</returns>
        private ITransactionStep<ConsultationTransactionContext> CreateNotificationStep()
        {
            // 可以创建一个通知步骤，发送诊疗开始的通知
            return new NotificationStep(_serviceProvider.GetRequiredService<ILogger<NotificationStep>>());
        }
    }

    /// <summary>
    /// 看诊事务选项配置
    /// </summary>
    public class ConsultationTransactionOptions
    {
        /// <summary>
        /// 事务超时时间
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 是否启用自动补偿
        /// </summary>
        public bool EnableAutoCompensation { get; set; } = true;

        /// <summary>
        /// 是否包含创建医疗案例步骤
        /// </summary>
        public bool IncludeCreateMedicalCase { get; set; } = true;

        /// <summary>
        /// 是否包含初始化诊断步骤
        /// </summary>
        public bool IncludeInitializeConsultation { get; set; } = true;

        /// <summary>
        /// 是否包含更新患者状态步骤
        /// </summary>
        public bool IncludeUpdatePatientStatus { get; set; } = true;

        /// <summary>
        /// 是否包含业务规则验证步骤
        /// </summary>
        public bool IncludeBusinessRuleValidation { get; set; } = false;

        /// <summary>
        /// 是否包含通知步骤
        /// </summary>
        public bool IncludeNotification { get; set; } = false;

        /// <summary>
        /// 是否允许覆盖已存在的诊断记录
        /// </summary>
        public bool AllowConsultationOverwrite { get; set; } = false;

        /// <summary>
        /// 是否跳过患者状态检查
        /// </summary>
        public bool SkipPatientStatusCheck { get; set; } = false;

        /// <summary>
        /// 自定义验证规则
        /// </summary>
        public Dictionary<string, object> CustomValidationRules { get; set; } = new();

        /// <summary>
        /// 事务元数据
        /// </summary>
        public Dictionary<string, object> TransactionMetadata { get; set; } = new();

        /// <summary>
        /// 创建默认选项
        /// </summary>
        /// <returns>默认选项实例</returns>
        public static ConsultationTransactionOptions Default()
        {
            return new ConsultationTransactionOptions();
        }

        /// <summary>
        /// 创建快速开始选项（跳过一些非关键验证）
        /// </summary>
        /// <returns>快速开始选项实例</returns>
        public static ConsultationTransactionOptions QuickStart()
        {
            return new ConsultationTransactionOptions
            {
                Timeout = TimeSpan.FromMinutes(2),
                SkipPatientStatusCheck = true,
                IncludeBusinessRuleValidation = false,
                IncludeNotification = false
            };
        }

        /// <summary>
        /// 创建严格验证选项（包含所有验证步骤）
        /// </summary>
        /// <returns>严格验证选项实例</returns>
        public static ConsultationTransactionOptions Strict()
        {
            return new ConsultationTransactionOptions
            {
                Timeout = TimeSpan.FromMinutes(10),
                IncludeBusinessRuleValidation = true,
                IncludeNotification = true,
                AllowConsultationOverwrite = false,
                SkipPatientStatusCheck = false
            };
        }
    }

    /// <summary>
    /// 业务规则验证步骤示例
    /// </summary>
    internal class BusinessRuleValidationStep : ConditionalTransactionStep<ConsultationTransactionContext>
    {
        /// <inheritdoc/>
        public override string StepName => "BusinessRuleValidation";

        /// <inheritdoc/>
        public override int Order => 10;

        public BusinessRuleValidationStep(ILogger logger) : base(logger)
        {
        }

        /// <inheritdoc/>
        protected override Task<bool> EvaluateConditionAsync(ConsultationTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 实现具体的业务规则验证逻辑
            // 例如：检查医生是否有权限为该患者看诊、检查时间段是否可用等
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        protected override Task<TransactionStepResult> ExecuteConditionalOperationAsync(ConsultationTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 执行业务规则验证
            Logger?.LogInformation("Business rule validation passed for consultation: {PatientId}", context.PatientId);
            return Task.FromResult(CreateSuccessResult());
        }
    }

    /// <summary>
    /// 通知步骤示例
    /// </summary>
    internal class NotificationStep : TransactionStepBase<ConsultationTransactionContext>
    {
        private readonly ILogger _logger;

        /// <inheritdoc/>
        public override string StepName => "SendNotification";

        /// <inheritdoc/>
        public override int Order => 20;

        /// <inheritdoc/>
        public override bool SupportsCompensation => false; // 通知通常不需要补偿

        public NotificationStep(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public override Task<TransactionStepResult> ExecuteAsync(ConsultationTransactionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // 发送诊疗开始通知
                _logger.LogInformation(
                    "Sending consultation start notification for patient: {PatientId}, doctor: {DoctorId}",
                    context.PatientId, context.DoctorId);

                // 这里可以集成实际的通知系统
                // 例如：发送短信、邮件、系统内消息等

                return Task.FromResult(CreateSuccessResult(new Dictionary<string, object>
                {
                    ["NotificationType"] = "ConsultationStart",
                    ["Recipients"] = new[] { context.PatientId.ToString(), context.DoctorId.ToString() },
                    ["Timestamp"] = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send consultation notification");
                return Task.FromResult(CreateFailureResult(ex));
            }
        }
    }
}
