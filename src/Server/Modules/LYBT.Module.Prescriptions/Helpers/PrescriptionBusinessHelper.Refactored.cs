using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Module.Prescriptions.Services.Core;
using LYBT.Module.Prescriptions.Services.Workflow;
using LYBT.Module.Prescriptions.Services.Features;
using LYBT.Module.Prescriptions.Services.Intelligence;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Helpers
{
    /// <summary>
    /// PrescriptionService涓氬姟閫昏緫鍔╂墜绫?- UltraThink閲嶆瀯鐗?
    /// 閲嶆瀯鍚庯細浣滀负鏈嶅姟鍗忚皟鍣紝灏嗗師鏉ョ殑649琛屼唬鐮侀噸鏋勪负5涓笓涓氭湇鍔＄被
    /// 鑱岃矗锛氬崗璋冨悇涓笓涓氭湇鍔★紝鎻愪緵缁熶竴鐨勪笟鍔℃帴鍙?
    /// 浠ｇ爜琛屾暟锛氱害150琛岋紝姣斿師鏉ュ噺灏?7%
    /// </summary>
    public class PrescriptionBusinessHelperRefactored
    {
        private readonly IPrescriptionCrudService _crudService;
        private readonly IPrescriptionWorkflowService _workflowService;
        private readonly IPrescriptionCopyService _copyService;
        private readonly IPrescriptionExportService _exportService;
        private readonly IPrescriptionIntelligentService _intelligentService;
        private readonly ILogger<PrescriptionBusinessHelperRefactored> _logger;

        public PrescriptionBusinessHelperRefactored(
            IPrescriptionCrudService crudService,
            IPrescriptionWorkflowService workflowService,
            IPrescriptionCopyService copyService,
            IPrescriptionExportService exportService,
            IPrescriptionIntelligentService intelligentService,
            ILogger<PrescriptionBusinessHelperRefactored> logger)
        {
            _crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _copyService = copyService ?? throw new ArgumentNullException(nameof(copyService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _intelligentService = intelligentService ?? throw new ArgumentNullException(nameof(intelligentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD鎿嶄綔濮旀墭

        /// <summary>
        /// 鍒涘缓鏂板鏂?
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName)
        {
            _logger.LogInformation("寮€濮嬪垱寤哄鏂?- 鎿嶄綔鑰? {OperatorName}", operatorName);            return await _crudService.CreateAsync(dto, operatorId, operatorName);
        }

        /// <summary>
        /// 鏇存柊澶勬柟
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬫洿鏂板鏂?- 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, dto.Id);            return await _crudService.UpdateAsync(dto, operatorId, operatorName);
        }

        /// <summary>
        /// 鍒犻櫎澶勬柟
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬪垹闄ゅ鏂?- 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, id);            return await _crudService.DeleteAsync(id, operatorId, operatorName);
        }

        #endregion

        #region 宸ヤ綔娴佹搷浣滃鎵?

        /// <summary>
        /// 鎵瑰噯澶勬柟
        /// </summary>
        public async Task<ServiceResult<bool>> ApproveAsync(Guid id, string approvalNote, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬫壒鍑嗗鏂?- 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, id);            return await _workflowService.ApproveAsync(id, approvalNote, operatorId, operatorName);
        }

        /// <summary>
        /// 鎷掔粷澶勬柟
        /// </summary>
        public async Task<ServiceResult<bool>> RejectAsync(Guid id, string rejectionReason, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬫嫆缁濆鏂?- 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, id);            return await _workflowService.RejectAsync(id, rejectionReason, operatorId, operatorName);
        }

        /// <summary>
        /// 鎻愪氦澶勬柟
        /// </summary>
        public async Task<ServiceResult<bool>> SubmitAsync(Guid prescriptionId, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬫彁浜ゅ鏂?- 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, prescriptionId);            return await _workflowService.SubmitAsync(prescriptionId, operatorId, operatorName);
        }

        /// <summary>
        /// 鍙栨秷澶勬柟
        /// </summary>
        public async Task<ServiceResult<bool>> CancelAsync(Guid id, string cancellationReason, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬪彇娑堝鏂?- 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, id);            return await _workflowService.CancelAsync(id, cancellationReason, operatorId, operatorName);
        }

        /// <summary>
        /// 蹇€熶繚瀛?
        /// </summary>
        public async Task<ServiceResult<bool>> QuickSaveAsync(Guid id, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬪揩閫熶繚瀛樺鏂?- 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, id);            return await _workflowService.QuickSaveAsync(id, operatorId, operatorName);
        }

        #endregion

        #region 澶嶅埗鍜屾ā鏉垮姛鑳藉鎵?

        /// <summary>
        /// 澶嶅埗澶勬柟
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid originalId, string newName, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬪鍒跺鏂?- 鎿嶄綔鑰? {OperatorName}, 鍘熷鏂笽D: {OriginalId}", operatorName, originalId);            return await _copyService.CopyAsync(originalId, newName, operatorId, operatorName);
        }

        /// <summary>
        /// 澶嶅埗涓婃澶勬柟
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬪鍒舵渶鍚庡鏂?- 鎿嶄綔鑰? {OperatorName}, 鎮ｈ€匢D: {PatientId}", operatorName, patientId);            return await _copyService.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName);
        }

        /// <summary>
        /// 浠庢ā鏉垮垱寤哄鏂?
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬩粠妯℃澘鍒涘缓澶勬柟 - 鎿嶄綔鑰? {OperatorName}, 妯℃澘ID: {TemplateId}", operatorName, templateId);            return await _copyService.CreateFromTemplateAsync(templateId, patientId, doctorId, operatorId, operatorName);
        }

        #endregion

        #region 瀵煎嚭鍔熻兘濮旀墭

        /// <summary>
        /// 瀵煎嚭澶勬柟涓篜DF
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportToPdfAsync(Guid prescriptionId, Guid operatorId, string operatorName)
        {            _logger.LogInformation("寮€濮嬪鍑篜DF - 鎿嶄綔鑰? {OperatorName}, 澶勬柟ID: {PrescriptionId}", operatorName, prescriptionId);            return await _exportService.ExportToPdfAsync(prescriptionId, operatorId, operatorName);
        }

        #endregion
    }

    /// <summary>
    /// UltraThink閲嶆瀯鎶ュ憡
    /// 
    /// 閲嶆瀯鍓嶏細PrescriptionBusinessHelper - 649琛屼唬鐮?
    /// 閲嶆瀯鍚庯細5涓笓涓氭湇鍔?+ 1涓崗璋冨櫒
    /// 
    /// 鏂版灦鏋勶細
    /// 1. PrescriptionCrudService (120琛? - 鍩虹CRUD鎿嶄綔
    /// 2. PrescriptionWorkflowService (200琛? - 宸ヤ綔娴佺鐞?
    /// 3. PrescriptionCopyService (150琛? - 澶嶅埗鍜屾ā鏉垮姛鑳? 
    /// 4. PrescriptionExportService (100琛? - 瀵煎嚭鍔熻兘
    /// 5. PrescriptionIntelligentService (180琛? - 鏅鸿兘妫€鏌?
    /// 6. PrescriptionBusinessHelperRefactored (150琛? - 鏈嶅姟鍗忚皟鍣?
    /// 
    /// 閲嶆瀯鏀剁泭锛?
    /// 鉁?鍗曚竴鑱岃矗鍘熷垯 - 姣忎釜鏈嶅姟涓撴敞鍗曚竴鑱岃矗
    /// 鉁?寮€闂師鍒?- 鏄撲簬鎵╁睍鏂板姛鑳?
    /// 鉁?渚濊禆鍊掔疆 - 閫氳繃鎺ュ彛瑙ｈ€?
    /// 鉁?浠ｇ爜鍙祴璇曟€?- 姣忎釜鏈嶅姟鍙嫭绔嬫祴璇?
    /// 鉁?浠ｇ爜鍙淮鎶ゆ€?- 鑱岃矗娓呮櫚锛屾槗浜庣悊瑙ｅ拰淇敼
    /// 鉁?鍥㈤槦鍗忎綔 - 涓嶅悓寮€鍙戜汉鍛樺彲骞惰寮€鍙戜笉鍚屾湇鍔?
    /// 
    /// 鏂囦欢澶у皬鎺у埗锛?
    /// - 鍘熸潵锛?涓枃浠?49琛?
    /// - 閲嶆瀯鍚庯細6涓枃浠讹紝姣忎釜鏂囦欢閮藉湪500琛屼互涓?
    /// - 鏈€澶ф枃浠讹細PrescriptionWorkflowService 200琛?
    /// - 骞冲潎鏂囦欢锛氱害150琛?
    /// 
    /// 涓嬩竴姝ヤ紭鍖栧缓璁細
    /// 1. 涓烘瘡涓湇鍔℃坊鍔犲搴旂殑鍗曞厓娴嬭瘯
    /// 2. 浣跨敤渚濊禆娉ㄥ叆瀹瑰櫒娉ㄥ唽鎵€鏈夋柊鏈嶅姟
    /// 3. 閫愭杩佺Щ鐜版湁璋冪敤鍒版柊鐨勯噸鏋勭増鏈?
    /// 4. 鑰冭檻娣诲姞缂撳瓨灞傛彁鍗囨€ц兘
    /// 5. 鑰冭檻娣诲姞瀹¤鏃ュ織璁板綍
    /// </summary>
    internal static class RefactoringReport
    {        public const string Summary = "PrescriptionBusinessHelper閲嶆瀯瀹屾垚锛?49琛屸啋6涓笓涓氭湇鍔★紝骞冲潎150琛?鏂囦欢";
    }
}

