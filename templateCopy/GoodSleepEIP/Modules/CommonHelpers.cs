using Newtonsoft.Json;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using GoodSleepEIP.Models;

namespace GoodSleepEIP
{
    public static class ResponseMsg
    {
        public static IActionResult Ok(bool success, string? message, object? data)
        {
            JsonResponse result = new JsonResponse
            {
                success = success,
                message = message,
                data = data ?? new List<string> { }
            };

            return new JsonResult(result);
        }

        public static IActionResult Ok(bool success, string? message)
        {
            JsonResponse result = new JsonResponse
            {
                success = success,
                message = message,
                data = new List<string>()
            };

            return new JsonResult(result);
        }

        /// <summary>
        /// 產生 `JsonResponse` 物件，給非 `Controller` 類別使用
        /// </summary>
        public static JsonResponse Create(bool success, string? message, object? data = null)
        {
            return new JsonResponse
            {
                success = success,
                message = message,
                data = data ?? new List<string>()
            };
        }
    }

    // Global Data
    public static class GlobalData
    {
        public static Dictionary<string, string> Config = new Dictionary<string, string>();
        public static int DocumentCategoryLevel1Selected = 0;
        public static int ShowProjectPhase04Only = 0;
    }

    public static class DBName
    {
        private static readonly IConfiguration _configuration;
        static DBName()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
        public static string Main => $"[{_configuration["DBName:Main"] ?? ""}].dbo";
        public static string ERP => $"[{_configuration["DBName:ERP"] ?? ""}].dbo";
    }

    public class ConstantValue
    {
        public string GetValue(Dictionary<string, string> dic, string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            dic.TryGetValue(key, out string? result);
            return result ?? string.Empty;
        }

        public string GetValue(Dictionary<int, string> dic, int key)
        {
            dic.TryGetValue(key, out string? result);
            return result ?? string.Empty;
        }

        public Dictionary<string, string> users_role = new Dictionary<string, string>
        {
            {"1", "系統管理員"}, {"3", "採購人員"}, {"5", "供應商"}
        };
        public Dictionary<string, string> tax_code = new Dictionary<string, string>
        {
            {"01", "應稅"}, {"02", "零稅率"}, {"03", "免稅"}
        };
    }

    public static class Func
    {
        public static bool IsValidEmail(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static string NumToChinese(int num)
        {
            string[] units = { "", "十", "百", "千", "萬", "十", "百", "千", "億", "十", "百", "千", "萬億" };
            string[] chars = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

            string str = num.ToString();
            int len = str.Length;
            StringBuilder result = new StringBuilder();
            bool zeroFlag = false;

            for (int i = 0; i < len; i++)
            {
                char c = str[i];
                int pos = len - i - 1;

                if (c == '0')
                {
                    zeroFlag = true;
                }
                else
                {
                    if (zeroFlag)
                    {
                        result.Append(chars[0]);
                        zeroFlag = false;
                    }
                    result.Append(chars[int.Parse(c.ToString())]);
                    result.Append(units[pos]);
                }
            }

            // 處理特殊情況，例如 "一十" 開頭的數字
            if (result.ToString().StartsWith("一十"))
            {
                result = new StringBuilder(result.ToString().Replace("一十", "十"));
            }

            // 處理多餘的零
            string resultStr = result.ToString();
            resultStr = resultStr.Replace("零零", "零");
            // 去掉末尾的零
            resultStr = resultStr.TrimEnd('零');

            return resultStr;
        }

        public static string GetROCDateString(DateTime? date = null)
        {
            DateTime actualDate = date ?? DateTime.Now;
            int rocYear = actualDate.Year - 1911;
            if (rocYear <= 0)
            {
                throw new ArgumentOutOfRangeException("Year must be greater than 1911");
            }

            return $"{rocYear}年{actualDate.Month}月{actualDate.Day}日";
        }

        public static string ReplaceNewLinesWithSymbol(string input, string symbol)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            // 先替換 Windows 的換行符
            string result = input.Replace("\r\n", symbol);
            // 再替換 Unix 的換行符
            result = result.Replace("\n", symbol);

            return result;
        }
    }
    
    // 雜湊相關
    public static class HashHelper
    {
        /// <summary>
        /// 將字串組合後轉為 MD5 雜湊，輸出為大寫十六進位字串
        /// </summary>
        public static string ComputeMd5Upper(params string[] inputs)
        {
            var combined = string.Concat(inputs);
            var bytes = Encoding.UTF8.GetBytes(combined);
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hashBytes = md5.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
        }
    }
}
