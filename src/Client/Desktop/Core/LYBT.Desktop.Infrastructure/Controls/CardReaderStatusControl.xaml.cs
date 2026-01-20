using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls;

/// <summary>
/// 读卡器状态控件
/// OpenSpec: integrate-cardreader-module - 身份证读卡器UI集成
/// 提供读卡器连接状态显示、手动读卡按钮、自动读卡开关
/// </summary>
public partial class CardReaderStatusControl : UserControl
{
    #region 依赖属性

    /// <summary>是否已连接读卡器</summary>
    public static readonly DependencyProperty IsConnectedProperty =
        DependencyProperty.Register(
            nameof(IsConnected),
            typeof(bool),
            typeof(CardReaderStatusControl),
            new PropertyMetadata(false, OnConnectionStateChanged));

    /// <summary>是否启用自动读卡</summary>
    public static readonly DependencyProperty IsAutoReadEnabledProperty =
        DependencyProperty.Register(
            nameof(IsAutoReadEnabled),
            typeof(bool),
            typeof(CardReaderStatusControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>是否正在读卡</summary>
    public static readonly DependencyProperty IsReadingProperty =
        DependencyProperty.Register(
            nameof(IsReading),
            typeof(bool),
            typeof(CardReaderStatusControl),
            new PropertyMetadata(false, OnReadingStateChanged));

    /// <summary>状态信息</summary>
    public static readonly DependencyProperty StatusMessageProperty =
        DependencyProperty.Register(
            nameof(StatusMessage),
            typeof(string),
            typeof(CardReaderStatusControl),
            new PropertyMetadata("读卡器未连接"));

    /// <summary>读卡命令</summary>
    public static readonly DependencyProperty ReadCardCommandProperty =
        DependencyProperty.Register(
            nameof(ReadCardCommand),
            typeof(ICommand),
            typeof(CardReaderStatusControl),
            new PropertyMetadata(null));

    /// <summary>切换自动读卡命令</summary>
    public static readonly DependencyProperty ToggleAutoReadCommandProperty =
        DependencyProperty.Register(
            nameof(ToggleAutoReadCommand),
            typeof(ICommand),
            typeof(CardReaderStatusControl),
            new PropertyMetadata(null));

    /// <summary>是否可以手动读卡（只读，根据状态自动计算）</summary>
    public static readonly DependencyProperty CanManualReadProperty =
        DependencyProperty.Register(
            nameof(CanManualRead),
            typeof(bool),
            typeof(CardReaderStatusControl),
            new PropertyMetadata(false));

    #endregion

    #region 属性

    /// <summary>
    /// 是否已连接读卡器
    /// </summary>
    public bool IsConnected
    {
        get => (bool)GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    /// <summary>
    /// 是否启用自动读卡
    /// </summary>
    public bool IsAutoReadEnabled
    {
        get => (bool)GetValue(IsAutoReadEnabledProperty);
        set => SetValue(IsAutoReadEnabledProperty, value);
    }

    /// <summary>
    /// 是否正在读卡
    /// </summary>
    public bool IsReading
    {
        get => (bool)GetValue(IsReadingProperty);
        set => SetValue(IsReadingProperty, value);
    }

    /// <summary>
    /// 状态信息
    /// </summary>
    public string StatusMessage
    {
        get => (string)GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    /// <summary>
    /// 读卡命令
    /// </summary>
    public ICommand? ReadCardCommand
    {
        get => (ICommand?)GetValue(ReadCardCommandProperty);
        set => SetValue(ReadCardCommandProperty, value);
    }

    /// <summary>
    /// 切换自动读卡命令
    /// </summary>
    public ICommand? ToggleAutoReadCommand
    {
        get => (ICommand?)GetValue(ToggleAutoReadCommandProperty);
        set => SetValue(ToggleAutoReadCommandProperty, value);
    }

    /// <summary>
    /// 是否可以手动读卡
    /// </summary>
    public bool CanManualRead
    {
        get => (bool)GetValue(CanManualReadProperty);
        private set => SetValue(CanManualReadProperty, value);
    }

    #endregion

    #region 构造函数

    public CardReaderStatusControl()
    {
        InitializeComponent();
        UpdateCanManualRead();
    }

    #endregion

    #region 私有方法

    private static void OnConnectionStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CardReaderStatusControl control)
        {
            control.UpdateCanManualRead();
            control.UpdateDefaultStatusMessage();
        }
    }

    private static void OnReadingStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CardReaderStatusControl control)
        {
            control.UpdateCanManualRead();
        }
    }

    private void UpdateCanManualRead()
    {
        // 已连接且未在读卡中时可以手动读卡
        CanManualRead = IsConnected && !IsReading;
    }

    private void UpdateDefaultStatusMessage()
    {
        // 如果状态信息未被外部设置，使用默认值
        if (string.IsNullOrEmpty(StatusMessage) || StatusMessage == "读卡器未连接" || StatusMessage == "读卡器已就绪")
        {
            StatusMessage = IsConnected ? "读卡器已就绪" : "读卡器未连接";
        }
    }

    #endregion
}
