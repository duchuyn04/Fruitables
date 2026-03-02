namespace Fruitables.Constants
{
    /// <summary>
    /// Constants for setting keys used throughout the application
    /// </summary>
    public static class SettingKeys
    {
        // General Settings
        public const string SiteName = "site_name";
        public const string Logo = "logo";
        public const string Favicon = "favicon";

        // SEO Settings
        public const string MetaTitle = "meta_title";
        public const string MetaDescription = "meta_description";
        public const string MetaKeywords = "meta_keywords";
        public const string GoogleAnalyticsId = "google_analytics_id";

        // Contact Settings
        public const string ContactAddress = "contact_address";
        public const string ContactPhone = "contact_phone";
        public const string ContactEmail = "contact_email";
        public const string ContactWorkingHours = "contact_working_hours";
        public const string ContactMapEmbed = "contact_map_embed";

        // Social Settings
        public const string SocialFacebook = "social_facebook";
        public const string SocialTwitter = "social_twitter";
        public const string SocialInstagram = "social_instagram";
        public const string SocialYoutube = "social_youtube";
        public const string SocialLinkedIn = "social_linkedin";

        // Banner Settings
        public const string Banner1Title = "banner1_title";
        public const string Banner1Subtitle = "banner1_subtitle";
        public const string Banner1Image = "banner1_image";
        public const string Banner1Link = "banner1_link";
        public const string Banner2Title = "banner2_title";
        public const string Banner2Subtitle = "banner2_subtitle";
        public const string Banner2Image = "banner2_image";
        public const string Banner2Link = "banner2_link";
        public const string Banner3Title = "banner3_title";
        public const string Banner3Subtitle = "banner3_subtitle";
        public const string Banner3Image = "banner3_image";
        public const string Banner3Link = "banner3_link";

        // Shipping Settings - Phí theo khu vực
        public const string ShippingFeeZone1 = "shipping_fee_zone1";      // Nội thành
        public const string ShippingFeeZone2 = "shipping_fee_zone2";      // Ngoại thành
        public const string ShippingFeeZone3 = "shipping_fee_zone3";      // Vùng xa
        public const string FreeShippingThreshold = "free_shipping_threshold";
        public const string ReducedShippingFeeZone3 = "reduced_shipping_fee_zone3"; // Phí giảm vùng xa

        // Danh sách quận/huyện theo khu vực (JSON array)
        public const string Zone1Districts = "zone1_districts";
        public const string Zone2Districts = "zone2_districts";

        // SMTP Email Settings
        public const string SmtpHost = "smtp_host";
        public const string SmtpPort = "smtp_port";
        public const string SmtpUsername = "smtp_username";
        public const string SmtpPassword = "smtp_password";
        public const string SmtpEnableSsl = "smtp_enable_ssl";
        public const string SmtpSenderName = "smtp_sender_name";
        public const string SmtpSenderEmail = "smtp_sender_email";

        // Google OAuth Settings
        public const string GoogleAuthClientId = "google_auth_client_id";
        public const string GoogleAuthClientSecret = "google_auth_client_secret";
        public const string GoogleAuthIsEnabled = "google_auth_is_enabled";
    }

    /// <summary>
    /// Constants for setting groups
    /// </summary>
    public static class SettingGroups
    {
        public const string General = "General";
        public const string Seo = "SEO";
        public const string Contact = "Contact";
        public const string Social = "Social";
        public const string Banner = "Banner";
        public const string Shipping = "Shipping";
        public const string Smtp = "Smtp";
        public const string GoogleAuth = "GoogleAuth";
    }
}
