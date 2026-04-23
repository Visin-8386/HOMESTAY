using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebHS.Models;
using WebHSUser = WebHS.Models.User;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHS.Services;
using WebHS.ViewModels;

namespace WebHS.Controllers
{    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomestayService _homestayService;

        public HomeController(ILogger<HomeController> logger, IHomestayService homestayService)
        {
            _logger = logger;
            _homestayService = homestayService;
        }

        public async Task<IActionResult> Index()
        {
            var popularHomestays = await _homestayService.GetPopularHomestaysAsync(8);
            
            var model = new HomeViewModel
            {
                PopularHomestays = popularHomestays,
                SearchForm = new HomestaySearchViewModel()
            };

            return View(model);
        }        [HttpPost]
        public IActionResult Search(HomestaySearchViewModel model)
        {
            // Kiểm tra và đặt giá trị mặc định nếu cần
            if (model.CheckInDate == null)
                model.CheckInDate = DateTime.Today;
            
            if (model.CheckOutDate == null)
                model.CheckOutDate = DateTime.Today.AddDays(1);
            
            // Đảm bảo ngày nhận phòng không trước ngày hiện tại
            if (model.CheckInDate < DateTime.Today)
                model.CheckInDate = DateTime.Today;
            
            // Đảm bảo ngày trả phòng sau ngày nhận phòng ít nhất 1 ngày
            if (model.CheckOutDate <= model.CheckInDate)
                model.CheckOutDate = model.CheckInDate.Value.AddDays(1);
            
            // Đảm bảo số khách hợp lệ
            if (model.Guests <= 0)
                model.Guests = 1;
                
            return RedirectToAction("Index", "Homestay", model);
        }        public IActionResult Privacy()
        {
            return View();
        }        public IActionResult About()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult DeleteData()
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

