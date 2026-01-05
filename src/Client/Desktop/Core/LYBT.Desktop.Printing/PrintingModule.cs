using LYBT.Desktop.Printing.Interfaces;
using LYBT.Desktop.Printing.Models;
using LYBT.Desktop.Printing.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Printing
{
    /// <summary>
    /// 打印服务模块
    /// OpenSpec: create-printing-module
    /// 提供独立的打印、预览、导出功能
    /// </summary>
    [Module(ModuleName = nameof(PrintingModule))]
    public class PrintingModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册处方打印服务
            containerRegistry.RegisterSingleton<IPrintService<PrescriptionPrintModel>, PrescriptionPrintService>();
        }
    }
}
