namespace GoodSleepEIP.Models
{
    public class LineBindRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string LineIdToken { get; set; }
        public string? CaptchaId { get; set; }
        public string? CaptchaAnswer { get; set; }
    }

    // 用於接收 LINE 驗證回應的 Model
    public class LineVerifyIdTokenResponse
    {
        public string Iss { get; set; } = string.Empty;
        public string Sub { get; set; } = string.Empty; // 這就是 Line User ID
        public string Aud { get; set; } = string.Empty;
        public long Exp { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
        public string? Email { get; set; }
    }
}
