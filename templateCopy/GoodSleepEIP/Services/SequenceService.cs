using Dapper;
using GoodSleepEIP.Models;

namespace GoodSleepEIP
{
    public class SequenceService
    {
        private readonly IDapperHelper dapper;

        public SequenceService(IDapperHelper _dapper)
        {
            dapper = _dapper;
        }

        /// <summary>
        /// 取得下一個流水號
        /// dynamicValues 是一個 Dictionary，可以傳入額外變數，如：new Dictionary<string, string> { { "DynamicCode", "ABC" } }
        /// 這樣可以在 EncodingPattern 和 SequenceGroupingPattern 替換 {DynamicCode}。
        /// </summary>
        public string GetNextSequence(string ruleName, int? year = null, int? month = null, int? day = null, Dictionary<string, string>? dynamicValues = null)
        {
            try
            {
                // 透過 RuleName 取得 SequenceRuleId
                string sql_rule = $"SELECT * FROM {DBName.Main}.SequenceRule WHERE RuleName = @RuleName";
                var ruleList = dapper.Query<SequenceRule>(sql_rule, new { RuleName = ruleName }).ToList();

                if (ruleList == null || ruleList.Count == 0)
                    throw new Exception($"找不到指定的取號規則: {ruleName}");

                var rule = ruleList[0];  // 取出第一筆匹配的規則
                int sequenceRuleId = rule.SequenceRuleId;

                // 動態計算分組鍵
                string groupingValue = ComputeGroupingValue(rule, year, month, day, dynamicValues);

                // 取得或初始化 Sequence
                string sql_select = @$"SELECT * FROM {DBName.Main}.Sequence 
                                      WHERE SequenceRuleId = @SequenceRuleId AND GroupingValue = @GroupingValue";

                var sequenceList = dapper.Query<Sequence>(sql_select, new
                {
                    SequenceRuleId = sequenceRuleId,
                    GroupingValue = groupingValue
                }).ToList();

                Sequence? sequence = null;

                if (sequenceList == null || sequenceList.Count == 0)
                {
                    sequence = new Sequence
                    {
                        SequenceId = 0,
                        SequenceRuleId = sequenceRuleId,
                        GroupingValue = groupingValue,
                        CurrentNumber = 1
                    };

                    string sql_insert = @$"INSERT INTO {DBName.Main}.Sequence (SequenceRuleId, GroupingValue, CurrentNumber, UpdateTime) 
                                          VALUES (@SequenceRuleId, @GroupingValue, @CurrentNumber, GETDATE());";

                    dapper.Execute(sql_insert, sequence);
                }
                else
                {
                    sequence = sequenceList[0];
                    sequence.CurrentNumber++;
                    string sql_update = @$"UPDATE {DBName.Main}.Sequence 
                                          SET CurrentNumber = @CurrentNumber, UpdateTime = GETDATE() 
                                          WHERE SequenceRuleId = @SequenceRuleId AND GroupingValue = @GroupingValue";

                    dapper.Execute(sql_update, sequence);
                }

                // 產生最終的編碼
                return GenerateFinalSequence(rule, sequence.CurrentNumber, year, month, day, dynamicValues);
            }
            catch (Exception ex)
            {
                throw new Exception($"產生流水號時發生錯誤: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 計算 GroupingValue，根據 SequenceGroupingPattern
        /// </summary>
        private string ComputeGroupingValue(SequenceRule rule, int? year, int? month, int? day, Dictionary<string, string>? dynamicValues)
        {
            string groupingValue = rule.SequenceGroupingPattern
                .Replace("{FixedCode1}", rule.FixedCode1 ?? "")
                .Replace("{FixedCode2}", rule.FixedCode2 ?? "")
                .Replace("{FixedCode3}", rule.FixedCode3 ?? "")
                .Replace("{Year}", year?.ToString() ?? "")
                .Replace("{Month}", month?.ToString("D2") ?? "")
                .Replace("{Day}", day?.ToString("D2") ?? "");  // 加入 {Day} 處理

            // 替換動態變數
            if (dynamicValues != null)
            {
                foreach (var kvp in dynamicValues)
                {
                    groupingValue = groupingValue.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            return groupingValue;
        }

        /// <summary>
        /// 根據 EncodingPattern 產生最終序號
        /// </summary>
        private string GenerateFinalSequence(SequenceRule rule, int currentNumber, int? year, int? month, int? day, Dictionary<string, string>? dynamicValues)
        {
            string formattedSequence = rule.SequenceLength > 0
                ? currentNumber.ToString($"D{rule.SequenceLength}")  // 若 SequenceLength > 0 則補 0
                : currentNumber.ToString();  // 若 SequenceLength = 0，則不補 0

            string finalSequence = rule.EncodingPattern
                .Replace("{FixedCode1}", rule.FixedCode1 ?? "")
                .Replace("{FixedCode2}", rule.FixedCode2 ?? "")
                .Replace("{FixedCode3}", rule.FixedCode3 ?? "")
                .Replace("{Year}", year?.ToString() ?? "")
                .Replace("{Month}", month?.ToString("D2") ?? "")
                .Replace("{Day}", day?.ToString("D2") ?? "")  // 加入 {Day} 替換
                .Replace("{Sequence}", formattedSequence);

            // 替換動態變數
            if (dynamicValues != null)
            {
                foreach (var kvp in dynamicValues)
                {
                    finalSequence = finalSequence.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            return finalSequence;
        }

        /// <summary>
        /// 預覽下一個流水號（不寫入資料庫）
        /// 在新增前預估序號，注意這個方法不會鎖定資料庫資源，所以返回的預覽值可能會與實際產生的序號有所不同
        /// dynamicValues 是一個 Dictionary，可以傳入額外變數，如：new Dictionary<string, string> { { "DynamicCode", "ABC" } }
        /// 這樣可以在 EncodingPattern 和 SequenceGroupingPattern 替換 {DynamicCode}。
        /// </summary>
        public string PreviewNextSequence(string ruleName, int? year = null, int? month = null, int? day = null, Dictionary<string, string>? dynamicValues = null)
        {
            try
            {
                // 透過 RuleName 取得 SequenceRuleId
                string sql_rule = $"SELECT * FROM {DBName.Main}.SequenceRule WHERE RuleName = @RuleName";
                var ruleList = dapper.Query<SequenceRule>(sql_rule, new { RuleName = ruleName }).ToList();

                if (ruleList == null || ruleList.Count == 0)
                    throw new Exception($"找不到指定的取號規則: {ruleName}");

                var rule = ruleList[0];  // 取出第一筆匹配的規則
                int sequenceRuleId = rule.SequenceRuleId;

                // 動態計算分組鍵
                string groupingValue = ComputeGroupingValue(rule, year, month, day, dynamicValues);

                // 只查詢當前序號
                string sql_select = @$"SELECT * FROM {DBName.Main}.Sequence 
                                      WHERE SequenceRuleId = @SequenceRuleId AND GroupingValue = @GroupingValue";

                var sequenceList = dapper.Query<Sequence>(sql_select, new
                {
                    SequenceRuleId = sequenceRuleId,
                    GroupingValue = groupingValue
                }).ToList();

                // 計算下一個序號
                int nextNumber = (sequenceList != null && sequenceList.Count > 0)
                    ? sequenceList[0].CurrentNumber + 1
                    : 1;

                // 產生預覽的編碼
                return GenerateFinalSequence(rule, nextNumber, year, month, day, dynamicValues);
            }
            catch (Exception ex)
            {
                throw new Exception($"預覽流水號時發生錯誤: {ex.Message}", ex);
            }
        }
    }
}
