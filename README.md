# C# Survey API - ASP.NET Core and Entity Framework

A small survey service for anonymous team feedback. Create questionnaires,
submit one score per question and inspect aggregated results. Includes an
ASP.NET MVC overview at `/dashboard`.

New learning project, October 2026. Uses fictional questions and
sample responses; it is not a previous employer's system.

## What the code demonstrates

- C# records, classes, validation and dependency injection.
- ASP.NET Core REST endpoints and an MVC controller with a Razor view.
- Entity Framework Core relationships, a unique index and SQLite persistence.
- HTTP validation errors, missing resources and aggregated survey results.
- Unit tests and HTTP integration tests against a temporary relational database.

SQLite makes this example runnable without installing a database server. SQL
Server, PostgreSQL and MongoDB examples live in the separate `sql-mongodb-lab`.

## Run

Requires the .NET 10 SDK.

```sh
dotnet run --project src/SurveyApi --urls http://localhost:5080
```

Open `http://localhost:5080/dashboard` or `http://localhost:5080/api/surveys`.
The first run creates `surveys.db` and one sample survey. Override the database
using `ConnectionStrings__Surveys='Data Source=/path/to/demo.db'`.

```sh
curl -X POST http://localhost:5080/api/surveys \
  -H 'Content-Type: application/json' \
  -d '{"title":"Weekly check-in","questions":["How clear are your priorities?","How manageable is the workload?"]}'
```

Use the returned question IDs in a submission:

```json
{"answers":[{"questionId":"<first-question-id>","score":4},{"questionId":"<second-question-id>","score":3}]}
```

| Method | Endpoint | Behaviour |
| --- | --- | --- |
| GET | `/api/surveys` | List questionnaires |
| POST | `/api/surveys` | Validate and create a questionnaire |
| GET | `/api/surveys/{id}` | Read a questionnaire and its question IDs |
| PUT | `/api/surveys/{id}` | Rename with `{"title":"New title"}` |
| DELETE | `/api/surveys/{id}` | Delete a questionnaire that has no responses |
| POST | `/api/surveys/{id}/responses` | Submit exactly one score, 1-5, per question |
| GET | `/api/surveys/{id}/results` | Response count and mean score per question |

## Test

```sh
dotnet test tests/SurveyApi.Tests
```

This is a local demonstration with no authentication or abuse protection. Run
it on localhost. An actual deployment would need those features and migrations.
