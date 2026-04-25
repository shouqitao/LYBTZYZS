using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Initialization;
using LYBT.Desktop.Contracts.Services;
using LYBT.LocalWebAPI;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

public sealed class ConnectionModeProvider : IConnectionModeProvider
{
    private readonly ILogger<ConnectionModeProvider> _logger;
    private readonly IModeSwitchValidator _validator;
    private readonly IActiveConsultationService _activeConsultation;
    private readonly INavigationCoordinator _navigation;
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly LocalWebApiHost? _localWebApiHost;
    private ConnectionMode _currentMode;
    private bool _isSwitching;

    public ConnectionModeProvider(
        ConnectionMode initialMode,
        ILogger<ConnectionModeProvider> logger,
        IModeSwitchValidator validator,
        IActiveConsultationService activeConsultation,
        INavigationCoordinator navigation,
        IDatabaseInitializer databaseInitializer,
        LocalWebApiHost? localWebApiHost = null)
    {
        _currentMode = initialMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _activeConsultation = activeConsultation ?? throw new ArgumentNullException(nameof(activeConsultation));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
        _localWebApiHost = localWebApiHost;

        _logger.LogInformation(
            "[ConnectionModeProvider] Initialized with mode: {Mode}", _currentMode);
    }

    /// <inheritdoc />
    public ConnectionMode CurrentMode => _currentMode;

    /// <inheritdoc />
    public bool IsSwitching => _isSwitching;

    /// <inheritdoc />
    public event EventHandler<ConnectionModeChangedEventArgs>? ModeChanged;

    /// <inheritdoc />
    public async Task<ModeSwitchResult> SwitchModeAsync(ConnectionMode targetMode, CancellationToken ct = default)
    {
        if (_isSwitching)
            return ModeSwitchResult.Failed("模式切换正在进行中，请稍候");

        if (_currentMode == targetMode)
            return ModeSwitchResult.Succeeded();

        _logger.LogInformation(
            "[ConnectionModeProvider] SwitchMode requested: {Current} -> {Target}",
            _currentMode, targetMode);

        try
        {
            _isSwitching = true;

            // Step 1: 检查活跃医案 (SYNC-D03: Gemini 审核 #3)
            if (_activeConsultation.HasActiveConsultation)
            {
                _logger.LogWarning("[ConnectionModeProvider] Switch blocked: active consultation exists");
                return ModeSwitchResult.Failed("当前有活跃医案，请先完成或关闭后再切换模式");
            }

            // Step 2: 前置验证 (SYNC-D01: 未完成医案检查 / LocalDB 可用性)
            // 兼容新加入的 LocalWebAPI 模式，将 LocalWebAPI 视为本地模式变体，执行本地前置校验。
            var isLocalLikeTarget = _currentMode == ConnectionMode.Local || targetMode == ConnectionMode.LocalWebAPI;
            var validation = isLocalLikeTarget
                ? await _validator.ValidateLocalToRemoteSwitchAsync(ct)
                : await _validator.ValidateRemoteToLocalSwitchAsync(ct);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "[ConnectionModeProvider] Switch blocked by validator: {Error}",
                    validation.ErrorMessage);
                return ModeSwitchResult.Failed(validation.ErrorMessage!);
            }

            // Step 3: 清理 UI 状态 (SYNC-D03: Region 清理 + 导航历史)
            _navigation.ClearContentRegion();
            _navigation.ClearHistory();

            _logger.LogDebug("[ConnectionModeProvider] UI state cleared");

            // Step 4: 执行切换
            var previousMode = _currentMode;
            _currentMode = targetMode;

            _logger.LogInformation(
                "[ConnectionModeProvider] Mode switched: {Previous} -> {Current}",
                previousMode, _currentMode);

            // Step 4.5: 切换到本地模式时，延迟初始化数据库
            if (_currentMode == ConnectionMode.Local)
            {
                try
                {
                    await _databaseInitializer.EnsureInitializedAsync(ct);
                    _logger.LogInformation("[ConnectionModeProvider] 本地数据库初始化完成");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ConnectionModeProvider] 本地数据库初始化失败");
                    _currentMode = previousMode;
                    return ModeSwitchResult.Failed($"本地数据库初始化失败: {ex.Message}");
                }
            }

            // Step 4.6: 切换到 LocalWebAPI 模式时，启动嵌入式 WebAPI
            if (_currentMode == ConnectionMode.LocalWebAPI && _localWebApiHost != null)
            {
                try
                {
                    await _localWebApiHost.StartAsync(ct);
                    _logger.LogInformation("[ConnectionModeProvider] LocalWebApiHost started on port {Port}", _localWebApiHost.Port);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ConnectionModeProvider] LocalWebApiHost start failed");
                    _currentMode = previousMode;
                    return ModeSwitchResult.Failed($"嵌入式 WebAPI 启动失败: {ex.Message}");
                }
            }

            // Step 4.7: 从 LocalWebAPI 模式切换出去时，停止嵌入式 WebAPI
            if (previousMode == ConnectionMode.LocalWebAPI && _localWebApiHost != null)
            {
                try
                {
                    await _localWebApiHost.StopAsync(ct);
                    _logger.LogInformation("[ConnectionModeProvider] LocalWebApiHost stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ConnectionModeProvider] LocalWebApiHost stop error");
                }
            }

            // Step 5: 通知所有订阅者
            ModeChanged?.Invoke(this, new ConnectionModeChangedEventArgs(previousMode, _currentMode));

            // Step 6: 导航到首页
            _navigation.NavigateToHome();

            return ModeSwitchResult.Succeeded();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[ConnectionModeProvider] SwitchMode cancelled");
            return ModeSwitchResult.Failed("模式切换已取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ConnectionModeProvider] SwitchMode failed");
            return ModeSwitchResult.Failed($"模式切换失败: {ex.Message}");
        }
        finally
        {
            _isSwitching = false;
        }
    }
}
