using Microsoft.EntityFrameworkCore;
using SurveyApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SurveyDb>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Surveys") ?? "Data Source=surveys.db"));
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SurveyDb>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Surveys.AnyAsync())
    {
        db.Surveys.Add(new Survey
        {
            Title = "Demo team check-in",
            Questions = [new Question { Text = "How clear are your priorities?", Position = 0 },
                         new Question { Text = "How supported do you feel?", Position = 1 }]
        });
        await db.SaveChangesAsync();
    }
}

app.MapGet("/", () => Results.Redirect("/dashboard"));
app.MapGet("/api/surveys", async (SurveyDb db, CancellationToken ct) =>
    await db.Surveys.AsNoTracking().OrderBy(s => s.Title)
        .Select(s => new SurveyBrief(s.Id, s.Title, s.Questions.Count, s.Responses.Count)).ToListAsync(ct));

app.MapPost("/api/surveys", async (CreateSurvey input, SurveyDb db, CancellationToken ct) =>
{
    var errors = SurveyRules.Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var survey = new Survey
    {
        Title = input.Title!.Trim(),
        Questions = input.Questions!.Select((text, position) =>
            new Question { Text = text!.Trim(), Position = position }).ToList()
    };
    db.Surveys.Add(survey);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/surveys/{survey.Id}", new { survey.Id, survey.Title });
});

app.MapGet("/api/surveys/{id:guid}", async (Guid id, SurveyDb db, CancellationToken ct) =>
{
    var survey = await db.Surveys.AsNoTracking().Include(s => s.Questions).SingleOrDefaultAsync(s => s.Id == id, ct);
    return survey is null ? Results.NotFound() : Results.Ok(new
    {
        survey.Id, survey.Title,
        Questions = survey.Questions.OrderBy(q => q.Position).Select(q => new { q.Id, q.Text })
    });
});

app.MapPut("/api/surveys/{id:guid}", async (Guid id, RenameSurvey input, SurveyDb db, CancellationToken ct) =>
{
    if (!SurveyRules.ValidTitle(input.Title))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Use a title of 1-120 characters."] });
    var survey = await db.Surveys.FindAsync([id], ct);
    if (survey is null) return Results.NotFound();
    survey.Title = input.Title!.Trim();
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

app.MapDelete("/api/surveys/{id:guid}", async (Guid id, SurveyDb db, CancellationToken ct) =>
{
    var survey = await db.Surveys.FindAsync([id], ct);
    if (survey is null) return Results.NotFound();
    if (await db.Responses.AnyAsync(r => r.SurveyId == id, ct))
        return Results.Conflict(new { error = "Surveys with responses are retained to preserve their results." });
    db.Surveys.Remove(survey);
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

app.MapPost("/api/surveys/{id:guid}/responses", async (
    Guid id, SubmitResponse input, SurveyDb db, CancellationToken ct) =>
{
    var survey = await db.Surveys.Include(s => s.Questions).SingleOrDefaultAsync(s => s.Id == id, ct);
    if (survey is null) return Results.NotFound();
    var errors = SurveyRules.ValidateAnswers(survey.Questions.Select(q => q.Id), input.Answers);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var response = new SurveyResponse
    {
        SurveyId = id,
        Answers = input.Answers!.Select(a => new Answer { QuestionId = a.QuestionId, Score = a.Score }).ToList()
    };
    db.Responses.Add(response);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { response.Id });
});

app.MapGet("/api/surveys/{id:guid}/results", async (Guid id, SurveyDb db, CancellationToken ct) =>
{
    var survey = await db.Surveys.AsNoTracking().Include(s => s.Questions).SingleOrDefaultAsync(s => s.Id == id, ct);
    if (survey is null) return Results.NotFound();
    var answers = await db.Answers.AsNoTracking().Where(a => a.Response.SurveyId == id).ToListAsync(ct);
    var scores = answers.GroupBy(a => a.QuestionId).ToDictionary(g => g.Key, g => Math.Round(g.Average(a => a.Score), 2));
    return Results.Ok(new
    {
        survey.Id,
        ResponseCount = await db.Responses.CountAsync(r => r.SurveyId == id, ct),
        Questions = survey.Questions.OrderBy(q => q.Position).Select(q => new
        {
            q.Id, q.Text, Average = scores.TryGetValue(q.Id, out var average) ? (double?)average : null
        })
    });
});

app.MapControllers();
app.Run();
public partial class Program { }
