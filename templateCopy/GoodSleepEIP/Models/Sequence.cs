namespace GoodSleepEIP.Models
{
    /// <summary>
    /// SequenceRule（規則表）
    /// </summary>
    public class SequenceRule
    {
        public required int SequenceRuleId { get; set; }
        public required string RuleName { get; set; }
        public string? Description { get; set; }
        public required string EncodingPattern { get; set; }
        public required string SequenceGroupingPattern { get; set; }
        public string? FixedCode1 { get; set; }
        public string? FixedCode2 { get; set; }
        public string? FixedCode3 { get; set; }
        public required int SequenceLength { get; set; }
    }

    /// <summary>
    /// Sequence（流水號表）
    /// </summary>
    public class Sequence
    {
        public required int SequenceId { get; set; }
        public required int SequenceRuleId { get; set; }
        public required string GroupingValue { get; set; }
        public required int CurrentNumber { get; set; }
        public DateTime UpdateTime { get; set; }
    }
}
