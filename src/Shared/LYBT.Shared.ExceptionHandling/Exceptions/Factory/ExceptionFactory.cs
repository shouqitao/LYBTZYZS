using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Shared.ExceptionHandling.Exceptions;

/// <summary>
/// 异常工厂 - 提供便捷的异常创建方法
/// consolidate-exception-handling: 从LYBT.Shared.Models迁移
/// </summary>
public static class ExceptionFactory
{
    /// <summary>
    /// 用户相关异常
    /// </summary>
    public static class User
    {
        public static NotFoundException NotFound(Guid userId) =>
            NotFoundException.User(userId);

        public static ConflictException UserNameExists(string userName) =>
            ConflictException.Duplicate("用户", "用户名", userName);

        public static ConflictException EmailExists(string email) =>
            ConflictException.Duplicate("用户", "邮箱", email);

        public static UnauthorizedException InvalidPassword() =>
            UnauthorizedException.InvalidPassword();

        public static UnauthorizedException Disabled() =>
            UnauthorizedException.UserDisabled();

        public static UnauthorizedException Locked() =>
            UnauthorizedException.UserLocked();

        public static BusinessException CannotDeleteSysAdmin() =>
            new(EC.CannotDeleteSysAdmin, "无法删除系统管理员");
    }

    /// <summary>
    /// 患者相关异常
    /// </summary>
    public static class Patient
    {
        public static NotFoundException NotFound(Guid patientId) =>
            NotFoundException.Patient(patientId);

        public static ConflictException IdCardExists(string idCard) =>
            ConflictException.Duplicate("患者", "身份证号", idCard);

        public static ConflictException PhoneExists(string phone) =>
            ConflictException.Duplicate("患者", "手机号", phone);

        public static BusinessException HasActiveCases(Guid patientId) =>
            new(EC.PatientHasActiveCases, $"患者 (ID: {patientId}) 有关联的医案");
    }

    /// <summary>
    /// 药材相关异常
    /// </summary>
    public static class Herb
    {
        public static NotFoundException NotFound(Guid herbId) =>
            NotFoundException.Herb(herbId);

        public static ConflictException NameExists(string herbName) =>
            ConflictException.Duplicate("药材", "名称", herbName);
    }

    /// <summary>
    /// 医案相关异常
    /// </summary>
    public static class MedicalCase
    {
        public static NotFoundException NotFound(Guid caseId) =>
            NotFoundException.MedicalCase(caseId);

        public static BusinessException InvalidState(Guid caseId, string currentState, string expectedState) =>
            new(EC.InvalidMedicalCaseState,
                $"医案 (ID: {caseId}) 状态无效，当前: {currentState}，期望: {expectedState}");

        public static ConflictException VersionConflict(Guid caseId, int expectedVersion, int currentVersion) =>
            ConflictException.MedicalCaseVersion(caseId, expectedVersion, currentVersion);

        public static ConflictException Locked(Guid caseId, string? lockedBy = null) =>
            ConflictException.MedicalCaseLocked(caseId, lockedBy);
    }

    /// <summary>
    /// 方剂相关异常
    /// </summary>
    public static class Formula
    {
        public static NotFoundException NotFound(Guid formulaId) =>
            NotFoundException.Formula(formulaId);

        public static ConflictException NameExists(string formulaName) =>
            ConflictException.Duplicate("方剂", "名称", formulaName);
    }

}
