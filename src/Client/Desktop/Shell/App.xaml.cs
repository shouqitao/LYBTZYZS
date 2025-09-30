using System.Windows;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Consultation;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Prescriptions;
using LYBT.Desktop.Shell.Extensions;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Desktop.Shell.Views;
using LYBT.Desktop.Users;
using LYBT.Shared.Models.Enums;
// TODO: Issue #815 Phase 3 - 鎭㈠Workstation寮曠敤
// using LYBT.Desktop.Workstation.Medical;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell;

/// <summary>
/// 搴旂敤绋嬪簭涓诲叆鍙?- WPF搴旂敤绋嬪簭鏍稿績鍚姩鍣?
/// 閲囩敤UltraThink鏋舵瀯鏍囧噯锛屼娇鐢–# 12鐜颁唬鍖栫壒鎬?
/// 鎻愪緵鏅鸿兘妯″潡鍔犺浇銆佽鑹查┍鍔ㄥ垵濮嬪寲鍜屼紒涓氱骇閿欒澶勭悊
/// 闆嗘垚Prism.DryIoc瀹瑰櫒绠＄悊锛屾敮鎸?涓笟鍔℃ā鍧楃殑缁熶竴鍗忚皟
/// 浼樺寲鍚姩鎬ц兘锛屾彁渚涜鑹插熀纭€鐨勬ā鍧楁寜闇€鍔犺浇绛栫暐
/// 閫傞厤灏忓瀷璇婃墍閮ㄧ讲鐜锛岀‘淇濈郴缁熷揩閫熷惎鍔ㄥ拰绋冲畾杩愯
/// </summary>
public partial class App : PrismApplication
{
    private IApplicationBootstrapper? _bootstrapper;

    /// <summary>
    /// 鍒涘缓搴旂敤绋嬪簭涓荤獥浣?
    /// 浠嶥I瀹瑰櫒涓В鏋怣ainWindow瀹炰緥
    /// 娉細杩欐槸Prism妗嗘灦鐨勬爣鍑嗗仛娉曪紝姝ゅ浣跨敤Container.Resolve鏄繀闇€鐨?
    /// </summary>
    /// <returns>搴旂敤绋嬪簭涓荤獥浣撳疄渚?/returns>
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    /// <summary>
    /// 娉ㄥ唽搴旂敤绋嬪簭绫诲瀷鍜屾湇鍔?
    /// 浣跨敤鎵╁睍鏂规硶缁熶竴娉ㄥ唽鎵€鏈変笟鍔℃ā鍧楃殑鏈嶅姟鍜屼緷璧?
    /// </summary>
    /// <param name="containerRegistry">DI瀹瑰櫒娉ㄥ唽鍣?/param>
    /// <exception cref="ArgumentNullException">褰撳鍣ㄦ敞鍐屽櫒涓?null 鏃舵姏鍑?/exception>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry, nameof(containerRegistry));

        // 娉ㄥ唽鍚姩寮曞鏈嶅姟锛堟浛浠ｅ師鏈夌殑鐩存帴Container.Resolve璋冪敤锛?
        containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();

        // 娉ㄥ唽搴旂敤鍒濆鍖栨湇鍔?
        containerRegistry.RegisterSingleton<LYBT.Desktop.Shell.Services.IApplicationInitializationService,
            LYBT.Desktop.Shell.Services.ApplicationInitializationService>();

        // 浣跨敤鎵╁睍鏂规硶缁熶竴娉ㄥ唽鎵€鏈夋湇鍔?
        containerRegistry.RegisterAllServices();

        // 鏄惧紡閰嶇疆ViewModelLocator鏄犲皠
        ConfigureViewModelLocator();
    }

    /// <summary>
    /// 閰嶇疆ViewModel瀹氫綅鍣?
    /// 鏄惧紡娉ㄥ唽View鍜孷iewModel鐨勬槧灏勫叧绯伙紝纭繚渚濊禆娉ㄥ叆姝ｇ‘宸ヤ綔
    /// </summary>
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        // Prism 8.x鏈€浣冲疄璺碉細鐩存帴浣跨敤瀹瑰櫒瑙ｆ瀽锛屾棤闇€宸ュ巶鏂规硶
        // Prism 8.x鏈€浣冲疄璺碉細浣跨敤绫诲瀷鏄犲皠閬垮厤Container.Resolve
        // 閫氳繃娉涘瀷閲嶈浇璁╂鏋惰嚜鍔ㄨВ鏋愪緷璧栵紝鑰屼笉鏄墜鍔ㄨ皟鐢ㄥ鍣?
        ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        ViewModelLocationProvider.Register<HomeView, HomeViewModel>();

        // Note: 鍏朵粬View-ViewModel鏄犲皠閫氳繃Prism鑷姩鍙戠幇鏈哄埗澶勭悊
    }

    /// <summary>
    /// 搴旂敤绋嬪簭鍒濆鍖栧畬鎴愬悗鐨勫洖璋?
    /// 浣跨敤娉ㄥ叆鐨凙pplicationBootstrapper鏈嶅姟锛岄伩鍏峉ervice Locator鍙嶆ā寮?
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 浣跨敤娉ㄥ叆鐨勫惎鍔ㄥ紩瀵兼湇鍔★紙閬垮厤Container.Resolve锛?
        try
        {
            // 鑾峰彇鍚姩寮曞鏈嶅姟
            // 娉細姝ゅContainer.Resolve鏄彲鎺ュ彈鐨勶紝鍥犱负锛?
            // 1. 浣嶄簬缁勫悎鏍?App.xaml.cs)
            // 2. OnInitialized鏄噸鍐欐柟娉曪紝鏃犳硶浣跨敤鏋勯€犲嚱鏁版敞鍏?
            // 3. 浠呭湪搴旂敤鍚姩鏃惰皟鐢ㄤ竴娆?
            _bootstrapper = Container.Resolve<IApplicationBootstrapper>();

            // 鍒濆鍖栭敊璇鐞嗭紙鍚屾鎿嶄綔锛?
            _bootstrapper.InitializeErrorHandlingService();

            // 鍒濆鍖栨ā鍧楀崗璋冨櫒
            _bootstrapper.InitializeSimplifiedModuleCoordinator();

            // 寮傛鍒濆鍖栨牳蹇冩湇鍔?
            _ = Task.Run(async () =>
            {
                await _bootstrapper.InitializeCoreServicesAsync();
                await _bootstrapper.InitializeApplicationWarmupAsync();
            });
        }
        catch (Exception ex)
        {
            // 闄嶇骇澶勭悊锛氬鏋滃垵濮嬪寲鏈嶅姟鏈纭敞鍐岋紝璁板綍閿欒浣嗙户缁惎鍔?
            System.Diagnostics.Debug.WriteLine($"搴旂敤鍒濆鍖栧け璐? {ex.Message}");
            System.Windows.MessageBox.Show(
                $"搴旂敤鍒濆鍖栧け璐? {ex.Message}",
                "鍑岄殣瀹濆爞 - 绯荤粺閿欒",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }









    /// <summary>
    /// 閰嶇疆妯″潡鐩綍
    /// 鍩轰簬瑙掕壊鐨勬櫤鑳芥ā鍧楀姞杞界瓥鐣ワ紝鏄捐憲鎻愬崌鍚姩鎬ц兘
    /// 浼樺厛鍔犺浇鏍稿績妯″潡锛屼笓涓氭ā鍧楁寜闇€鍔犺浇
    /// </summary>
    /// <param name="moduleCatalog">妯″潡鐩綍</param>
    /// <exception cref="ArgumentNullException">褰撴ā鍧楃洰褰曚负 null 鏃舵姏鍑?/exception>
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        ArgumentNullException.ThrowIfNull(moduleCatalog, nameof(moduleCatalog));

        // ========== 鏍稿績妯″潡 - 绔嬪嵆鍔犺浇 ==========
        // 璁よ瘉妯″潡 - 鎵€鏈夊姛鑳界殑鍩虹
        moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);

        // 鐢ㄦ埛妯″潡 - 鍩虹鏉冮檺绠＄悊
        moduleCatalog.AddModule<UsersModule>(InitializationMode.WhenAvailable);

        // ========== 鍩虹涓氬姟妯″潡 - 鐧诲綍鍚庡姞杞?==========
        // 鎮ｈ€呯鐞?- 澶氭暟涓氬姟鐨勫熀纭€
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.OnDemand);

        // ========== 鍔熻兘妯″潡 - 鎸夐渶鍔犺浇 ==========
        // 鑽潗绠＄悊 - 鐙珛鍔熻兘锛屽彲寤惰繜鍔犺浇
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);

        // 鏂瑰墏绠＄悊 - 渚濊禆鑽潗
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);

        // 璇婄枟绠＄悊 - 渚濊禆鎮ｈ€?
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.OnDemand);

        // 鐥呭巻绠＄悊 - 澶嶆潅渚濊禆
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);

        // 澶勬柟绠＄悊 - 鏈€澶嶆潅渚濊禆
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.OnDemand);

        // ========== 宸ヤ綔鍙版ā鍧?- 鐢ㄦ埛瑙﹀彂鍔犺浇 ==========
        // 璇婄枟宸ヤ綔鍙?- 椤跺眰闆嗘垚妯″潡
        // TODO: Issue #815 Phase 3 - 鎭㈠璇婄枟宸ヤ綔鍙版ā鍧?
        // moduleCatalog.AddModule<MedicalWorkstationModule>(InitializationMode.OnDemand);

        // 绠＄悊宸ヤ綔鍙?- 绠＄悊鍛樿鑹蹭娇鐢?
        moduleCatalog.AddModule<AdminWorkstation.AdminWorkstationModule>(InitializationMode.OnDemand);

        // 璇婄枟宸ヤ綔鍙?- 鍖荤敓瑙掕壊浣跨敤
        moduleCatalog.AddModule<ClinicalWorkstation.ClinicalWorkstationModule>(InitializationMode.OnDemand);

        base.ConfigureModuleCatalog(moduleCatalog);
    }

    /// <summary>
    /// 娣诲姞鏍稿績妯″潡
    /// 鏍稿績妯″潡鍦ㄥ簲鐢ㄥ惎鍔ㄦ椂绔嬪嵆鍔犺浇
    /// </summary>
    /// <param name="moduleCatalog">妯″潡鐩綍</param>
    /// <param name="moduleName">妯″潡鍚嶇О</param>
    /// <param name="moduleType">妯″潡绫诲瀷</param>
    private static void AddCoreModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType)
    {
        moduleCatalog.AddModule(new ModuleInfo
        {
            ModuleName = moduleName,
            ModuleType = moduleType.AssemblyQualifiedName,
            InitializationMode = InitializationMode.WhenAvailable
        });
    }

    /// <summary>
    /// 娣诲姞鍩轰簬瑙掕壊鐨勬櫤鑳芥ā鍧楅厤缃?
    /// 鏍规嵁鐢ㄦ埛瑙掕壊鍐冲畾妯″潡鍔犺浇鏃舵満锛屾彁鍗囧惎鍔ㄦ€ц兘
    /// </summary>
    /// <param name="moduleCatalog">妯″潡鐩綍</param>
    /// <param name="moduleName">妯″潡鍚嶇О</param>
    /// <param name="moduleType">妯″潡绫诲瀷</param>
    /// <param name="requiredRoles">鎵€闇€瑙掕壊鏁扮粍</param>
    private static void AddRoleBasedModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType, string[] requiredRoles)
    {
        var moduleInfo = new ModuleInfo
        {
            ModuleName = moduleName,
            ModuleType = moduleType.AssemblyQualifiedName,

            // 璁句负鎸夐渶鍔犺浇锛岀櫥褰曞悗鏍规嵁瑙掕壊鍐冲畾鏄惁绔嬪嵆鍔犺浇
            InitializationMode = InitializationMode.OnDemand
        };

        // 璁板綍妯″潡瑙掕壊淇℃伅锛堢畝鍖栧鐞嗭紝褰撳墠涓嶉檺鍒惰鑹茶闂級
        moduleCatalog.AddModule(moduleInfo);
    }

    /// <summary>
    /// 鐢ㄦ埛鐧诲綍鍚庣殑瑙掕壊椹卞姩妯″潡鍔犺浇
    /// 鏍规嵁鐢ㄦ埛瑙掕壊鏅鸿兘鍔犺浇鎵€闇€妯″潡锛岄伩鍏嶄笉蹇呰鐨勮祫婧愭秷鑰?
    /// </summary>
    /// <param name="userRole">鐢ㄦ埛瑙掕壊</param>
    /// <returns>妯″潡鍔犺浇浠诲姟</returns>
    /// <exception cref="ArgumentException">褰撶敤鎴疯鑹蹭负绌烘椂鎶涘嚭</exception>
    public async Task LoadRoleBasedModulesAsync(string userRole)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userRole, nameof(userRole));

        try
        {
            // 纭繚鍚姩寮曞鏈嶅姟宸插垵濮嬪寲
            if (_bootstrapper == null)
            {
                throw new InvalidOperationException("搴旂敤绋嬪簭鍚姩寮曞鏈嶅姟鏈垵濮嬪寲");
            }

            // 灏嗗瓧绗︿覆瑙掕壊杞崲涓烘灇涓?
            if (Enum.TryParse<UserRole>(userRole, out var role))
            {
                await _bootstrapper.LoadModulesForRoleAsync(role);
            }
            else
            {
                throw new ArgumentException($"鏃犳晥鐨勭敤鎴疯鑹? {userRole}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"瑙掕壊椹卞姩妯″潡鍔犺浇寮傚父: {ex.Message}");
            throw;
        }
    }
}
