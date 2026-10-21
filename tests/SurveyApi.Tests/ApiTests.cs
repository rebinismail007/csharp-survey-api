using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class ApiTests : IDisposable
{
    private readonly string dbPath = Path.Combine(Path.GetTempPath(), $"survey-{Guid.NewGuid()}.db");
    private readonly WebApplicationFactory<Program> factory;

    public ApiTests() => factory = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        web.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Surveys"] = $"Data Source={dbPath}" })));

    [Fact]
    public async Task QuestionnaireSubmissionAndResultsRoundTrip()
    {
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/surveys", new { title = "Demo", questions = new[] { "Clarity?" } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/surveys/{id}");
        var question = detail.GetProperty("questions")[0].GetProperty("id").GetGuid();
        foreach (var score in new[] { 3, 5 })
        {
            var response = await client.PostAsJsonAsync($"/api/surveys/{id}/responses", new { answers = new[] { new { questionId = question, score } } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var results = await client.GetFromJsonAsync<JsonElement>($"/api/surveys/{id}/results");
        Assert.Equal(2, results.GetProperty("responseCount").GetInt32());
        Assert.Equal(4, results.GetProperty("questions")[0].GetProperty("average").GetDouble());
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/surveys/{id}")).StatusCode);
    }

    [Fact]
    public async Task RenameAndDeleteAnUnansweredSurvey()
    {
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/surveys", new { title = "Original", questions = new[] { "Clarity?" } });
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/surveys/{id}", new { title = "Updated" })).StatusCode);
        var updated = await client.GetFromJsonAsync<JsonElement>($"/api/surveys/{id}");
        Assert.Equal("Updated", updated.GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/surveys/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/surveys/{id}")).StatusCode);
    }

    [Fact]
    public async Task InvalidDraftReturnsValidationProblem()
    {
        using var client = factory.CreateClient();
        var result = await client.PostAsJsonAsync("/api/surveys", new { title = "", questions = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UnknownSurveyReturns404()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/surveys/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task MvcOverviewRenders()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/dashboard");
        Assert.Contains("Team surveys", html);
        Assert.Contains("Demo team check-in", html);
    }

    public void Dispose()
    {
        factory.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" }) File.Delete(dbPath + suffix);
    }
}
