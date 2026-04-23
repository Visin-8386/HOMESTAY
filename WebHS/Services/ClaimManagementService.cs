using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WebHS.Models;

namespace WebHS.Services
{
    public interface IClaimManagementService
    {
        Task<IList<Claim>> GetUserClaimsAsync(User user);
        Task AddUserClaimAsync(User user, string claimType, string claimValue);
        Task UpdateUserClaimAsync(User user, string claimType, string oldValue, string newValue);
        Task RemoveUserClaimAsync(User user, string claimType, string claimValue);
        Task<IList<Claim>> GetRoleClaimsAsync(string roleName);
        Task AddRoleClaimAsync(string roleName, string claimType, string claimValue);
        Task RemoveRoleClaimAsync(string roleName, string claimType, string claimValue);
    }

    public class ClaimManagementService : IClaimManagementService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ClaimManagementService(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Lấy tất cả claims của một user
        public async Task<IList<Claim>> GetUserClaimsAsync(User user)
        {
            return await _userManager.GetClaimsAsync(user);
        }

        // Thêm một claim mới cho user
        public async Task AddUserClaimAsync(User user, string claimType, string claimValue)
        {
            await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        }

        // Cập nhật giá trị của một claim của user
        public async Task UpdateUserClaimAsync(User user, string claimType, string oldValue, string newValue)
        {
            await _userManager.ReplaceClaimAsync(
                user,
                new Claim(claimType, oldValue),
                new Claim(claimType, newValue)
            );
        }

        // Xóa một claim của user
        public async Task RemoveUserClaimAsync(User user, string claimType, string claimValue)
        {
            await _userManager.RemoveClaimAsync(user, new Claim(claimType, claimValue));
        }

        // Lấy tất cả claims của một role
        public async Task<IList<Claim>> GetRoleClaimsAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                return new List<Claim>();

            return await _roleManager.GetClaimsAsync(role);
        }

        // Thêm một claim mới cho role
        public async Task AddRoleClaimAsync(string roleName, string claimType, string claimValue)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                await _roleManager.AddClaimAsync(role, new Claim(claimType, claimValue));
            }
        }

        // Xóa một claim của role
        public async Task RemoveRoleClaimAsync(string roleName, string claimType, string claimValue)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                await _roleManager.RemoveClaimAsync(role, new Claim(claimType, claimValue));
            }
        }
    }
}
