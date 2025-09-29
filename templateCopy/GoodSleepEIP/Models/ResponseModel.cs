using System.Data;

namespace GoodSleepEIP.Models
{
    public class JsonResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public object? data { get; set; }
        public string timestamp { get; set; } = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    }
}
