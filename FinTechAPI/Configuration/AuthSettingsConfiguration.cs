// AuthSettings.cs
namespace FinTechAPI.Configuration
{
    public class AuthSettings
    {
        public string SecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationInMinutes { get; set; }
    }
}