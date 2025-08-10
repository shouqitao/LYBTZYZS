using System;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.WPF.Client.Shell.Views
{
    /// <summary>
    /// DiagnosticHomeView.xaml 的交互逻辑
    /// </summary>
    public partial class DiagnosticHomeView : UserControl
    {
        public DiagnosticHomeView()
        {
            InitializeComponent();
            
            Loaded += OnLoaded;
            DataContextChanged += OnDataContextChanged;
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DiagnosticInfo.Text = "View已加载，正在检查DataContext...";
            CheckDataContext();
        }
        
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CheckDataContext();
        }
        
        private void CheckDataContext()
        {
            if (DataContext == null)
            {
                DataContextInfo.Text = "❌ DataContext为NULL - ViewModelLocator失败！";
                ViewModelType.Text = "无";
                DiagnosticInfo.Text = "诊断结果：ViewModelLocator未能自动装配ViewModel";
            }
            else
            {
                var typeName = DataContext.GetType().FullName;
                DataContextInfo.Text = $"✅ DataContext已绑定";
                ViewModelType.Text = typeName ?? "未知类型";
                DiagnosticInfo.Text = $"诊断结果：ViewModel成功绑定 - {DataContext.GetType().Name}";
            }
        }
    }
}