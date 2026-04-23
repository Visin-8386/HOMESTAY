using System.ComponentModel.DataAnnotations;

namespace WebHS.ViewModels
{
    public class UserViewModel
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        
        // Helper property để kiểm tra IsHost từ roles
        public bool IsHost => Roles.Contains("Host");
    }

    public class UserDetailsViewModel
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<ClaimViewModel> Claims { get; set; } = new List<ClaimViewModel>();
        
        // Helper property để kiểm tra IsHost từ roles
        public bool IsHost => Roles.Contains("Host");
    }

    public class EditUserViewModel
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }

        [Required(ErrorMessage = "Họ không được để trống")]
        [Display(Name = "Họ")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Tên không được để trống")]
        [Display(Name = "Tên")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? ProfilePicture { get; set; }

        [Display(Name = "Giới thiệu")]
        public string? Bio { get; set; }

        [Display(Name = "Tài khoản hoạt động")]
        public bool IsActive { get; set; }

        public List<RoleViewModel> AllRoles { get; set; } = new List<RoleViewModel>();
        public List<string> SelectedRoles { get; set; } = new List<string>();
        
        // Helper property để kiểm tra IsHost từ roles
        public bool IsHost => SelectedRoles.Contains("Host");
    }

    public class RoleViewModel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CreateRoleViewModel
    {
        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [Display(Name = "Tên vai trò")]
        public string? Name { get; set; }
    }

    public class EditRoleViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [Display(Name = "Tên vai trò")]
        public string? Name { get; set; }
    }

    public class RoleDetailsViewModel
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public List<ClaimViewModel> Claims { get; set; } = new List<ClaimViewModel>();
    }

    public class UserRoleViewModel
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool IsSelected { get; set; }
    }

    public class UsersInRoleViewModel
    {
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public List<UserRoleViewModel> Users { get; set; } = new List<UserRoleViewModel>();
    }
}
