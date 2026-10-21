using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SurveyApi.Controllers;

public sealed class DashboardController(SurveyDb db) : Controller
{
    [HttpGet("/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(
        await db.Surveys.AsNoTracking().OrderBy(s => s.Title)
            .Select(s => new SurveyBrief(s.Id, s.Title, s.Questions.Count, s.Responses.Count)).ToListAsync(ct));
}
