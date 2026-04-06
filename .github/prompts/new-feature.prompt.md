---
mode: agent
description: Scaffold a new minimal-API feature endpoint with a service, model, and xUnit tests.
---

Create a new feature called `${input:featureName}` in the LaughingBarnacle Web API.

## Steps

1. **Model** – Create a `record` for the request and/or response in `src/LaughingBarnacle/Features/${input:featureName}/`.
2. **Service interface & implementation** – Create `I${input:featureName}Service` and `${input:featureName}Service` in the same folder. Register the service in `Program.cs` with the appropriate lifetime.
3. **Endpoint** – Add a minimal-API endpoint in `src/LaughingBarnacle/Features/${input:featureName}/${input:featureName}Endpoints.cs` and call `app.Map${input:featureName}Endpoints()` from `Program.cs`.
4. **Tests** – Create unit tests for the service and an integration test for the endpoint in `tests/LaughingBarnacle.Tests/Features/${input:featureName}/`.

## Constraints

- Follow all conventions in `.github/copilot-instructions.md` and `.github/instructions/csharp.instructions.md`.
- Tests must follow `.github/instructions/tests.instructions.md`.
- Do **not** add new NuGet packages without asking first.
- Run `dotnet build` and `dotnet test` mentally to verify there are no compile errors before presenting the output.
