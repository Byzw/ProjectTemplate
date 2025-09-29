using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoodSleepEIP
{
    /// <summary>
    /// 自定義 DateTime? 轉換器，處理 null 值、空字串和無效日期格式
    /// </summary>
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                // 如果是空字串或空白字串，返回 null
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                // 嘗試解析日期時間
                if (DateTime.TryParse(stringValue, out DateTime dateTime))
                {
                    return dateTime;
                }

                // 如果解析失敗，返回 null 而不是拋出異常
                return null;
            }

            // 對於其他類型，嘗試使用預設行為
            try
            {
                return JsonSerializer.Deserialize<DateTime?>(ref reader, options);
            }
            catch
            {
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}