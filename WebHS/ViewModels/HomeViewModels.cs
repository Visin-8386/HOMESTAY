using WebHS.Models;
using WebHSUser = WebHS.Models.User;
using WebHSPromotionType = WebHS.Models.PromotionType;
using WebHSPromotion = WebHS.Models.Promotion;
using WebHS.ViewModels;

namespace WebHS.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<HomestayCardViewModel> PopularHomestays { get; set; } = new List<HomestayCardViewModel>();
        public HomestaySearchViewModel SearchForm { get; set; } = new HomestaySearchViewModel();
    }
}

