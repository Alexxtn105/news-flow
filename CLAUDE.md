# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NewsFlow — система обработки новостных материалов с ролевым доступом и конвейерным workflow. .NET 8, Clean Architecture + DDD.

Два основных агрегата: **Material** (перевод и анализ) и **Document** (рецензирование, регистрация, контроль, оценка). Каждый имеет конечный автомат статусов с бизнес-правилами переходов в доменных сущностях.

## Build & Run Commands

```bash
# Сборка (пакеты берутся из локальной папки ./packages, интернет не нужен)
dotnet build

# Запуск Blazor Web UI (основной интерфейс, http://localhost:5000)
dotnet run --project src/NewsFlow.Web

# Запуск REST API (Swagger на http://localhost:5002/swagger)
dotnet run --project src/NewsFlow.Api

# Тесты
dotnet test                                          # все тесты
dotnet test tests/NewsFlow.Domain.Tests              # один проект
dotnet test --filter "FullyQualifiedName~ClassName"  # один тест/класс
```

## Database Configuration

В `appsettings.json` обоих приложений (Web и Api):

- **SQLite** (по умолчанию): `"DatabaseProvider": "SQLite"`, создание через `EnsureCreated`
- **PostgreSQL** (production): `"DatabaseProvider": "PostgreSQL"`, применяются миграции (`MigrateAsync`)

При первом запуске автоматически создаются роли, языки и пользователь `admin` / `admin123` (см. `SeedData.cs`).

## Architecture

```
NewsFlow.Web (Blazor Server, MudBlazor)  ──┐
NewsFlow.Api (Minimal API, JWT, SignalR)  ──┤── Presentation
                                            │
NewsFlow.Application (MediatR CQRS, FluentValidation, DTOs) ── Application
                                            │
NewsFlow.Domain (Entities, Aggregates, Enums, Interfaces) ── Domain
                                            │
NewsFlow.Infrastructure (EF Core, Repositories, JWT, BCrypt) ── Infrastructure
NewsFlow.Infrastructure.Yaml (YAML-конфигурация пайплайнов)  ──┘
```

**Два способа доступа к одному Application-слою:**
- **Blazor Web** вызывает MediatR напрямую (in-process) через `ISender.Send()`, без HTTP.
- **REST API** — Minimal API с JWT-аутентификацией, вызывает те же MediatR-команды через `ISender`.
- Оба приложения регистрируют сервисы через `AddApplicationServices()`, `AddInfrastructureServices()`, `AddYamlConfiguration()`.

## Domain State Machines

### Material Status Flow
```
New → InTranslation → Translated → InAnalysis → Processed (terminal)
                ↓                        ↓
  ReturnedFromTranslation    ReturnedFromAnalysis
                                         ↓
                          Rejected / NotOfInterest / Distorted (terminal)
```

### Document Status Flow
```
Draft → InReview → Reviewed → InRegistration → Registered
  → InControl → Controlled → InEvaluation → Evaluated (terminal)
  Альтернативы: ReturnedForRevision, ReturnedForControl, Cancelled
```

Бизнес-правила переходов инкапсулированы в доменных методах (`Material.TakeForTranslation()`, `Document.TakeForReview()` и т.д.) — они валидируют текущий статус и назначение.

## Key Patterns

### CQRS (Feature-folder per aggregate)
```
Application/{Feature}/
├── Commands/       # record + handler в одном файле
├── Queries/        # record + handler в одном файле
└── DTOs/           # record-типы для контрактов
```
Связанные команды группируются в один файл (`TranslationCommands.cs`, `AnalysisCommands.cs`, `DocumentWorkflowCommands.cs`). MediatR + FluentValidation авто-регистрируются из Assembly (`ApplicationServiceRegistration.cs`).

### Domain Method Encapsulation
Бизнес-логика живёт в доменных сущностях, не в хендлерах. Хендлер загружает сущность, вызывает доменный метод, сохраняет.

### Repository + Unit of Work
`GenericRepository<T>` — единый репозиторий для всех сущностей. `NewsFlowDbContext` реализует `IUnitOfWork` и `IApplicationDbContext`. Хендлеры обычно работают через `IApplicationDbContext` (доступ к DbSet напрямую), а не через `IRepository<T>`.

### DTO Mapping
Маппинг entity→DTO — статические методы `ToDto()` в хендлерах (`CreateMaterialHandler.ToDto()`), переиспользуемые другими хендлерами того же агрегата.

### YAML-Driven Workspaces
Конфигурация workflow и UI лежит в `config/workspaces/`:
- `pipelines/*.yaml` — определяют статусы и правила переходов (action, required_role, lock_to_user, requires_fields)
- `*.yaml` (корень) — определяют workspace для каждой роли (input_queue фильтр, actions, work_layout: split-panel / three-panel)

Загрузка через `IPipelineProvider` / `IWorkspaceProvider` (singleton, кэшируются в памяти).

## Blazor Web Specifics

- **MudBlazor 7.15.0**: использует `MudDialogInstance` (не `IMudDialogInstance`)
- **AuthStateService** (scoped) — хранит текущего пользователя в памяти сессии. `BlazorCurrentUserService` реализует `ICurrentUserService` через `AuthStateService` вместо `HttpContext`.
- **Тёмная тема** (Adobe Audition style) — настроена в `MainLayout.razor` через `PaletteDark`
- Навигация фильтруется по ролям: `Auth.HasRole("Translator")`, `Auth.HasRole("Administrator")` и т.д.
- Blazor-компоненты в `Components/` (не `Pages/`): `Components/Pages/Workspaces/`, `Components/Pages/Admin/`, `Components/Layout/`
- Workspace-страницы: `GenericWorkspace.razor` (динамическая из YAML), `TranslatorWorkspace.razor` и `AnalystWorkspace.razor` (кастомные)
- Админка Pipeline/Workspace: визуальные редакторы (`PipelineEditor.razor`, `WorkspaceEditor.razor`) с диаграммой состояний (`PipelineStateDiagram.razor`)

## REST API Specifics

Minimal API endpoints в `Endpoints/*.cs`. Паттерн:
```csharp
public static void MapXxxEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/api/xxx").WithTags("Xxx").RequireAuthorization();
    group.MapGet("/", async (..., ISender sender) => Results.Ok(await sender.Send(new Query(...))));
}
```
JWT Bearer authentication, Swagger, SignalR hub на `/hubs/notifications`, `ExceptionHandlingMiddleware`.

## Roles

Administrator, Operator, Translator, Analyst, Reviewer, Registrar, Controller, Evaluator. Привязка к workspace через YAML.

## Tests

xUnit, паттерн AAA. 4 тестовых проекта по слоям. Домен-тесты проверяют state machine переходы без моков.

## Offline / Air-gapped

Все NuGet-пакеты в `./packages/`. `NuGet.Config` указывает только на локальный источник. При добавлении новых зависимостей — скачать `.nupkg` + все транзитивные зависимости в `./packages/`.

## Solution File

`NewsFlow.slnx` — modern XML solution format (.NET 8+).
