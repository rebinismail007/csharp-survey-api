using SurveyApi;
using Xunit;

public class RulesTests
{
    [Fact] public void RejectsBlankTitle() => Assert.Contains("title", SurveyRules.Validate(new(" ", ["Question"])));
    [Fact] public void RejectsEmptyQuestionList() => Assert.Contains("questions", SurveyRules.Validate(new("Check-in", [])));
    [Theory] [InlineData(0)] [InlineData(6)]
    public void RejectsOutOfRangeScore(int score)
    {
        var id = Guid.NewGuid();
        Assert.NotEmpty(SurveyRules.ValidateAnswers([id], [new(id, score)]));
    }
    [Fact] public void RejectsDuplicateQuestionAnswers()
    {
        var id = Guid.NewGuid();
        Assert.NotEmpty(SurveyRules.ValidateAnswers([id, Guid.NewGuid()], [new(id, 4), new(id, 5)]));
    }
    [Fact] public void AcceptsCompleteAnswers()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        Assert.Empty(SurveyRules.ValidateAnswers([first, second], [new(second, 3), new(first, 5)]));
    }
}
