namespace WebHS.Models
{
    public static class WebHSClaimTypes
    {
        // User-specific claims
        public const string DateOfBirth = "birthdate";
        public const string Address = "address";
        public const string PreferredLanguage = "language";
        public const string PhoneVerified = "phone_verified";
        public const string AccountLevel = "account_level";
        public const string LastPasswordChange = "last_password_change";
        public const string LoginMethod = "login_method";
        public const string TwoFactorEnabled = "2fa_enabled";
        public const string ProfileCompleted = "profile_completed";
        
        // Host-specific claims
        public const string HostVerified = "host_verified";
        public const string HostRating = "host_rating";
        public const string HostSince = "host_since";
        public const string IdentityVerified = "identity_verified";
        public const string PropertyCount = "property_count";
        public const string SuperHost = "super_host";
        
        // Permission claims
        public const string CanManageUsers = "can_manage_users";
        public const string CanManageRoles = "can_manage_roles";
        public const string CanViewReports = "can_view_reports";
        public const string CanManageProperties = "can_manage_properties";
        public const string CanApproveListings = "can_approve_listings";
        public const string CanModerateReviews = "can_moderate_reviews";
        public const string CanManagePromotions = "can_manage_promotions";
        public const string CanAccessApiKeys = "can_access_api_keys";
    }
}
