using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrismDemo.ViewModels {
    /// <summary>
    /// MainWindow对应的视图模型
    /// </summary>
    public class MainWindowViewModel : BindableBase {
        private string _title = "LYBT 应用程序";
        /// <summary>
        /// 窗口标题属性，示例绑定到Window的Title
        /// </summary>
        public string Title {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // 可根据需要在此添加命令、服务等注入 (本示例暂不需额外逻辑)
        public MainWindowViewModel() {
        }
    }
}
