using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    public class PrescriptionsController : Controller {

        public IActionResult Index() {
            return View();
        }
    }
}