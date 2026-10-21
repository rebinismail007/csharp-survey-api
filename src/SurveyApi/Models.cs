namespace SurveyApi;

public sealed class Survey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<Question> Questions { get; set; } = [];
    public List<SurveyResponse> Responses { get; set; } = [];
}

public sealed class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SurveyId { get; set; }
    public string Text { get; set; } = "";
    public int Position { get; set; }
}

public sealed class SurveyResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SurveyId { get; set; }
    public List<Answer> Answers { get; set; } = [];
}

public sealed class Answer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResponseId { get; set; }
    public SurveyResponse Response { get; set; } = null!;
    public Guid QuestionId { get; set; }
    public int Score { get; set; }
}

public sealed record CreateSurvey(string? Title, List<string?>? Questions);
public sealed record RenameSurvey(string? Title);
public sealed record AnswerInput(Guid QuestionId, int Score);
public sealed record SubmitResponse(List<AnswerInput>? Answers);
public sealed record SurveyBrief(Guid Id, string Title, int QuestionCount, int ResponseCount);

public static class SurveyRules
{
    public static bool ValidTitle(string? title) => !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= 120;

    public static Dictionary<string, string[]> Validate(CreateSurvey input)
    {
        var errors = new Dictionary<string, string[]>();
        if (!ValidTitle(input.Title))
            errors["title"] = ["Use a title of 1-120 characters."];
        if (input.Questions is not { Count: >= 1 and <= 10 } ||
            input.Questions.Any(q => string.IsNullOrWhiteSpace(q) || q.Trim().Length > 200))
            errors["questions"] = ["Provide 1-10 questions, each 1-200 characters."];
        return errors;
    }

    public static Dictionary<string, string[]> ValidateAnswers(
        IEnumerable<Guid> questionIds, List<AnswerInput>? answers)
    {
        var expected = questionIds.ToHashSet();
        if (answers is null || answers.Count != expected.Count ||
            answers.Select(a => a.QuestionId).Distinct().Count() != expected.Count ||
            answers.Any(a => !expected.Contains(a.QuestionId) || a.Score is < 1 or > 5))
            return new() { ["answers"] = ["Submit exactly one score from 1 to 5 for every question."] };
        return [];
    }
}
