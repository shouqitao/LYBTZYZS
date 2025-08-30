using System.Windows.Controls;

namespace LYBT.Desktop.Workbench.Admin.Views
{
    /// <summary>
    /// SystemWorkbenchMainView.xaml 的交互逻辑
    /// </summary>
    public partial class SystemWorkbenchMainView : UserControl
    {
        public SystemWorkbenchMainView()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎯 SystemWorkbenchMainView构造函数开始");
                
                InitializeComponent();
                
                System.Diagnostics.Debug.WriteLine("✅ SystemWorkbenchMainView.InitializeComponent()完成");
                System.Diagnostics.Debug.WriteLine($"✅ SystemWorkbenchMainView创建成功 - DataContext: {DataContext?.GetType().Name ?? "null"}");
                
                // 添加Loaded事件处理，确认View是否被加载到可视树中
                this.Loaded += (sender, e) =>
                {
                    System.Diagnostics.Debug.WriteLine("🎯 SystemWorkbenchMainView.Loaded事件触发");
                    System.Diagnostics.Debug.WriteLine($"   Parent: {this.Parent?.GetType().Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"   DataContext: {this.DataContext?.GetType().Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"   IsVisible: {this.IsVisible}");
                    System.Diagnostics.Debug.WriteLine($"   ActualWidth: {this.ActualWidth}");
                    System.Diagnostics.Debug.WriteLine($"   ActualHeight: {this.ActualHeight}");
                };
                
                System.Diagnostics.Debug.WriteLine("🎯 SystemWorkbenchMainView构造函数完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SystemWorkbenchMainView构造失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                throw;
            }
        }
    }
}