using System.ComponentModel.DataAnnotations;

namespace WebHS.ViewModels
{
    // ViewModel cho một claim
    public class ClaimViewModel
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }

        [Required(ErrorMessage = "Loại claim không được để trống")]
        [StringLength(255, ErrorMessage = "Loại claim không được quá 255 ký tự")]
        [Display(Name = "Loại claim")]
        public string ClaimType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá trị claim không được để trống")]
        [StringLength(255, ErrorMessage = "Giá trị claim không được quá 255 ký tự")]
        [Display(Name = "Giá trị claim")]
        public string ClaimValue { get; set; } = string.Empty;
    }

    // ViewModel cho danh sách claims của user
    public class UserClaimsViewModel
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public List<ClaimViewModel> Claims { get; set; } = new List<ClaimViewModel>();
    }

    // ViewModel cho danh sách claims của role
    public class RoleClaimsViewModel
    {
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public List<ClaimViewModel> Claims { get; set; } = new List<ClaimViewModel>();
    }
}
