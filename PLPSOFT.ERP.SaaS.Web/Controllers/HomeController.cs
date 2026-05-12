using Microsoft.AspNetCore.Mvc;
using PLPSOFT.ERP.SaaS.Web.Models;
using System.Diagnostics;

namespace PLPSOFT.ERP.SaaS.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Bẻ lái (Redirect) thẳng vào trang Danh sách Khuyến mãi của Module 9
            return RedirectToAction("Index", "Campaigns", new { area = "Promotions" });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}