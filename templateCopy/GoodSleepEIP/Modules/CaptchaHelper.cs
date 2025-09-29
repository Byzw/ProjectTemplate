using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace GoodSleepEIP
{
    public class CaptchaResult
    {
        public string CaptchaCode { get; set; } = string.Empty;
        public byte[] CaptchaByteData { get; set; } = Array.Empty<byte>();
        public string CaptchBase64Data => Convert.ToBase64String(CaptchaByteData);
        public DateTime Timestamp { get; set; }
    }

    public static class Captcha
    {
        // 字母最好不要有下列(有些字很難區分): Number 0 and English O、Number 5 and English S、Number 1 and English I
        //const string Letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ";
        const string Letters = "1234567890";
        const int CodeLength = 4;

        public static string GenerateCaptchaCode()
        {
            Random rand = new Random();
            int maxRand = Letters.Length - 1;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < CodeLength; i++)
            {
                int index = rand.Next(maxRand);
                sb.Append(Letters[index]);
            }

            return sb.ToString();
        }

        public static bool ValidateCaptchaCode(string userInputCaptcha, HttpContext context)
        {
            var isValid = userInputCaptcha == context.Session.GetString("CaptchaCode");
            context.Session.Remove("CaptchaCode");
            return isValid;
        }

        public static CaptchaResult GetImage(int width, int height, string captchaCode, string fontName, bool isDarkTheme = false, FontStyle fontStyle = FontStyle.Bold)
        {
            using var ms = new MemoryStream();
            var rand = new Random();

            using (var imgText = new Image<Rgba32>(width, height))
            {
                // characters layer
                float position = 0;
                var averageSize = width / captchaCode.Length;
                var fontSize = Convert.ToInt32(averageSize);

                Font font = SystemFonts.CreateFont(fontName, fontSize, fontStyle);

                foreach (char c in captchaCode)
                {
                    //var x = rand.Next(5, 10);     數值越高 產生的 位置越右
                    //var y = rand.Next(10, 13);    數值越高 產生的 位置越低
                    var x = rand.Next(20, 25);
                    var y = rand.Next(2, 5);

                    var location = new PointF(x + position, y);
                    imgText.Mutate(ctx => ctx.DrawText(c.ToString(), font, GetRandomDeepColor(isDarkTheme), location));
                    position += TextMeasurer.MeasureAdvance(c.ToString(), new(font)).Width;
                }

                Random random = new Random();
                var builder = new AffineTransformBuilder();
                var rWidth = random.Next(10, width);
                var rHeight = random.Next(10, height);
                var pointF = new PointF(rWidth, rHeight);
                var degrees = random.Next(0, 10) * (random.Next(-10, 10) > 0 ? 1 : -1);
                var rotation = builder.PrependRotationDegrees(degrees, pointF);
                imgText.Mutate(ctx => ctx.Transform(rotation));

                // background layer
                int low = 180, high = 255;
                var nRend = rand.Next(high) % (high - low) + low;
                var nGreen = rand.Next(high) % (high - low) + low;
                var nBlue = rand.Next(high) % (high - low) + low;
                //var backColor = Color.FromRgb((byte)nRend, (byte)nGreen, (byte)nBlue);
                Color backColor = isDarkTheme ? Color.Black : Color.FromRgb((byte)nRend, (byte)nGreen, (byte)nBlue);


                var img = new Image<Rgba32>(width, height);
                img.Mutate(ctx => ctx.BackgroundColor(backColor));

                // lines
                for (var i = 0; i < rand.Next(3, 7); i++)
                {
                    var color = GetRandomDeepColor(isDarkTheme);
                    var startPoint = new PointF(rand.Next(0, width), rand.Next(0, height));
                    var endPoint = new PointF(rand.Next(0, width), rand.Next(0, height));
                    img.Mutate(ctx => ctx.DrawLine(color, 1, startPoint, endPoint));
                }

                // merge layers
                img.Mutate(ctx => ctx.DrawImage(imgText, 1f));
                img.SaveAsPng(ms);
            }

            return new()
            {
                CaptchaCode = captchaCode,
                CaptchaByteData = ms.ToArray(),
                Timestamp = DateTime.UtcNow
            };

            Color GetRandomDeepColor(bool isDarkTheme = false)
            {
                if (isDarkTheme) return Color.FromRgb((byte)rand.Next(200, 255), (byte)rand.Next(200, 255), (byte)rand.Next(200, 255)); // 暗色風、提高亮度

                int redlow = 160, greenLow = 100, blueLow = 160;
                return Color.FromRgb((byte)rand.Next(redlow), (byte)rand.Next(greenLow), (byte)rand.Next(blueLow));
            }
        }
    }

}
