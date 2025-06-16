using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 处方页面控制器（示例）
    /// </summary>
    public class PrescriptionsController : Controller {

        /// <summary>
        /// 默认视图
        /// </summary>
        public IActionResult Index() {
            return View();
        }
    }
}