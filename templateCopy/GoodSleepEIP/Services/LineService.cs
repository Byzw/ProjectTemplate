using Dapper;
using isRock.LineBot;
using GoodSleepEIP.Models;
using System.Text;
using System.Security.Cryptography;

namespace GoodSleepEIP
{
    public class LineService
    {
        private readonly IDapperHelper dapper;
        private readonly string channelAccessToken;
        private readonly string channelSecret;
        private readonly string LiffIdLineLogin;
        private readonly Bot bot;

        public LineService(IDapperHelper dapper, IConfiguration configuration)
        {
            this.dapper = dapper;
            channelAccessToken = configuration["LineBot:ChannelAccessToken"] ?? "";
            channelSecret = configuration["LineBot:ChannelSecret"] ?? "";
            LiffIdLineLogin = configuration["LineBot:LiffIdLineLogin"] ?? "";
            bot = new Bot(channelAccessToken);
        }

        public void HandleEvent(Event evt)
        {
            if (evt.source?.userId == null)
                return;

            var user = GetUserDataOnlyByLineId(evt.source.userId);
            if (user == null)
            {
                SendBindingMessage(evt.replyToken);
                return;
            }

            switch (evt.type)
            {
                case "message":
                    HandleMessage(evt, user);
                    break;
                case "postback":
                    HandlePostback(evt, user);
                    break;
                case "follow":
                    
                    break;
            }
        }

        public void SendBindingMessage(string replyToken)
        {
            try
            {
                var ButtonTempMsg = new ButtonsTemplate()
                {
                    title = "帳號綁定",
                    text = "您還沒綁定系統喔，請點擊此處綁定您的帳號。",
                    actions = new List<TemplateActionBase>() {
                        new UriAction() {
                            label = "綁定帳號",
                            uri = new Uri($"https://liff.line.me/{LiffIdLineLogin}")
                        }
                    }
                };

                bot.ReplyMessage(replyToken, new TemplateMessage(ButtonTempMsg));
            }
            catch (Exception ex)
            {
                bot.ReplyMessage(replyToken, $"[系統錯誤] {ex.Message}");
            }
        }

        private void HandleMessage(Event evt, Users user)
        {
            try
            {
                evt.message.text = evt.message.text?.Trim().ToLower();
                if (evt.message.text == "help")
                {
                    bot.ReplyMessage(evt.replyToken, $"{user.UserDescription} 您好，請問有什麼需要幫忙的嗎？");
                }
                else
                {
                    bot.ReplyMessage(evt.replyToken, $"{user.UserDescription}，我們已收到您的訊息");
                }
            }
            catch (Exception ex)
            {
                bot.ReplyMessage(evt.replyToken, $"[系統錯誤] {ex.Message}");
            }
        }

        private void HandlePostback(Event evt, Users user)
        {
            try
            {
                bot.ReplyMessage(evt.replyToken, $"{user.UserDescription}，我們已收到您的回應");
            }
            catch (Exception ex)
            {
                bot.ReplyMessage(evt.replyToken, $"[系統錯誤] {ex.Message}");
            }
        }

        public bool ValidateSignature(string requestBody, string signature)
        {
            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(requestBody)) return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(channelSecret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            var computedSignature = Convert.ToBase64String(hashBytes);

            return computedSignature.Equals(signature, StringComparison.Ordinal);
        }

        // 只取資料不做登入 token 產生等動作
        public Users? GetUserDataOnlyByLineId(string lineUserId)
        {
            string sqlstr = @$"SELECT * FROM {DBName.Main}.Users WHERE LineUserId = @LineUserId";
            var qresult = (List<Users>)dapper.Query<Users>(sqlstr, new { LineUserId = lineUserId });
            if (qresult.Count() >= 1) return qresult[0];
            else return null;
        }
    }
}
