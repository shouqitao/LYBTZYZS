using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

public sealed class ConnectionModeProvider : IConnectionModeProvider
{
    private readonly ILogger<ConnectionModeProvider> _logger;
    private readonly IModeSwitchValidator _validator;
    private readonly IActiveConsultationService _activeConsultation;
    private readonly INavigationCoordinator _navigation;
    private ConnectionMode _currentMode;
    private bool _isSwitching;

    public ConnectionModeProvider(
        ConnectionMode initialMode,
        ILogger<ConnectionModeProvider> logger,
        IModeSwitchValidator validator,
        IActiveConsultationService activeConsultation,
        INavigationCoordinator navigation)
    {
        _currentMode = initialMode;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _activeConsultation = activeConsultation ?? throw new ArgumentNullException(nameof(activeConsultation));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

        _logger.LogInformation(
            "[ConnectionModeProvider] Initialized with mode: {Mode}", _currentMode);
    }

    public ConnectionMode CurrentMode => _currentMode;

    public bool IsSwitching => _isSwitching;

    public event EventHandler<ConnectionModeChangedEventArgs>? ModeChanged;

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

            if (_activeConsultation.HasActiveConsultation)
            {
                _logger.LogWarning("[ConnectionModeProvider] Switch blocked: active consultation exists");
                return ModeSwitchResult.Failed("当前有活跃医案，请先完成或关闭后再切换模式");
            }

            var validation = _currentMode == ConnectionMode.Local
                ? await _validator.ValidateLocalToRemoteSwitchAsync(ct)
                : await _validator.ValidateRemoteToLocalSwitchAsync(ct);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "[ConnectionModeProvider] Switch blocked by validator: {Error}",
                    validation.ErrorMessage);
                return ModeSwitchResult.Failed(validation.ErrorMessage!);
            }

            _navigation.ClearContentRegion();
            _navigation.ClearHistory();

            _logger.LogDebug("[ConnectionModeProvider] UI state cleared");

            var previousMode = _currentMode;
            _currentMode = targetMode;

            _logger.LogInformation(
                "[ConnectionModeProvider] Mode switched: {Previous} -> {Current}",
                previousMode, _currentMode);

            ModeChanged?.Invoke(this, new ConnectionModeChangedEventArgs(previousMode, _currentMode));

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
