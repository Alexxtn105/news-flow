# NewsFlow — Контекст для AI-ассистента

## Обзор проекта

**NewsFlow** — изолированная (air-gapped, offline) система обработки новостных материалов, построенная на **.NET 8** с использованием **Clean Architecture + DDD**. Обрабатывает полный жизненный цикл новостных материалов: загрузка → перевод → анализ → создание документа → ревью → регистрация → контроль → оценка.

**Ключевые характеристики:**
- ~100 одновременных пользователей
- Конфигурируемые рабочие места через YAML (без изменений кода)
- Blazor Server UI + REST API (оба обслуживаются одним хостом)
- База данных: PostgreSQL или SQLite
- JWT-аутентификация с ролевой моделью доступа

---

## Стек технологий

| Компонент | Технология |
|-----------|------------|
| Runtime | .NET 8 LTS |
| Backend | ASP.NET Core 8 (Minimal APIs) |
| CQRS | MediatR |
| Валидация | FluentValidation |
| ORM | EF Core 8 |
| База данных | PostgreSQL 16+ / SQLite |
| UI | Blazor Server + MudBlazor 7 |
| Аутентификация | JWT (HMAC-SHA256), BCrypt |
| Real-time | SignalR |
| Логирование | Serilog |
| Парсинг YAML | YamlDotNet |

---

## Структура решения

```
news-flow/
├── src/
│   ├── NewsFlow.Domain           # Сущности, value objects, enum, доменные события
│   ├── NewsFlow.Application      # Команды/запросы MediatR, DTO, валидаторы
│   ├── NewsFlow.Infrastructure   # EF Core, JWT, BCrypt, файловое хранилище, репозитории
│   ├── NewsFlow.Infrastructure.Yaml  # Парсинг и валидация YAML-конфигураций
│   ├── NewsFlow.Api              # REST API endpoints, SignalR hub, middleware
│   └── NewsFlow.Web              # Blazor Server: страницы и компоненты
├── config/workspaces/
│   ├── pipelines/
│   │   ├── material_default.yaml   # Жизненный цикл материала
│   │   └── document_default.yaml   # Жизненный цикл документа
│   ├── translator.yaml, analyst.yaml, reviewer.yaml,
│   │   registrar.yaml, controller.yaml, evaluator.yaml
├── tests/
│   ├── NewsFlow.Domain.Tests
│   ├── NewsFlow.Application.Tests
│   ├── NewsFlow.Infrastructure.Tests
│   └── NewsFlow.Api.Tests
├── packages/                     # Локальный кэш NuGet (172 пакета)
├── NuGet.Config                  # Указывает на ./packages для offline-сборки
└── NewsFlow.slnx
```

---

## Команды сборки и запуска

### Предварительные требования
- Установлен .NET 8 SDK
- Все NuGet-пакеты в `./packages/` (настроено через `NuGet.Config`)

### Сборка
```bash
dotnet restore    # Использует локальные ./packages (offline)
dotnet build
dotnet test       # 33 юнит-теста
```

### Запуск (разработка)
```bash
# Blazor Server UI (основной интерфейс)
dotnet run --project src/NewsFlow.Web
# Откроется на https://localhost:5001

# REST API (опционально, для внешнего доступа)
dotnet run --project src/NewsFlow.Api
# Swagger на https://localhost:5002/swagger
```

### Публикация (автономная, продакшен)
```bash
# Windows
dotnet publish src/NewsFlow.Web -c Release -r win-x64 --self-contained -o ./publish/web
./publish/web/NewsFlow.Web.exe

# Linux
dotnet publish src/NewsFlow.Web -c Release -r linux-x64 --self-contained -o ./publish/web
./publish/web/NewsFlow.Web
```

### Конфигурация базы данных

**SQLite (по умолчанию, zero-config):**
```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=newsflow.db"
  }
}
```

**PostgreSQL (продакшен):**
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=newsflow;Username=newsflow_user;Password=..."
  }
}
```

---

## Доменная модель

### Корни агрегатов (5)

| Сущность | Описание |
|----------|----------|
| **User** | Пользователи с ролями, языками, блокировкой входа |
| **Material** | Новостные материалы с автоматом статусов (New → Translated → Processed) |
| **Document** | Аналитические документы с полным workflow (Draft → Evaluated) |
| **Workspace** | Конфигурируемые определения рабочих мест (YAML) |
| **Pipeline** | Определения жизненных циклов для сущностей |

### Сущности (10)

- `Role` — Роли пользователей (8 типов)
- `Attachment` — Медиафайлы, прикреплённые к материалам
- `DocumentMaterial` — Связь документов с исходными материалами
- `DocumentComment` — Комментарии ревьюера/контролёра
- `DocumentVersion` — Снимки версий документов
- `DocumentDistribution` — Записи о рассылке документов
- `Recipient` — Получатели рассылки
- `Source` — Источники информации
- `Language` — Языки ISO 639-1
- `Country` — Страны ISO 3166-1
- `Tag` — Категории/теги
- `AuditLog` — Записи аудита

### Автоматы статусов

**Material:**
```
New → InTranslation → Translated → InAnalysis → Processed
 │        ↓                │            ↓
 └────────┴────────────────┴────────────┴──→ Rejected
```

**Document:**
```
Draft → InReview → Reviewed → InRegistration → Registered → InControl → Controlled → InEvaluation → Evaluated
           ↓                                                    ↓
    ReturnedForRevision                                  ReturnedForControl
```

---

## Роли пользователей (8)

| Роль | Код | Рабочее место |
|------|-----|---------------|
| Администратор | Administrator | Админка (пользователи, справочники, пайплайны, аудит) |
| Оператор загрузки | Operator | Загрузка материалов |
| Переводчик | Translator | `/workspace/translator` |
| Аналитик | Analyst | `/workspace/analyst` |
| Ревьюер | Reviewer | `/workspace/reviewer` |
| Регистратор | Registrar | `/workspace/registrar` |
| Контролёр | Controller | `/workspace/controller` |
| Оценщик | Evaluator | `/workspace/evaluator` |

---

## Ключевые архитектурные решения

1. **Blazor Server + MediatR (in-process):** Blazor вызывает `ISender` напрямую, без HTTP
2. **YAML Pipeline Engine:** Рабочие места и пайплайны определяются в YAML, загружаются при старте
3. **Двойная поддержка БД:** SQLite (по умолчанию) или PostgreSQL через переключатель в конфиге
4. **Offline-first:** Все NuGet-пакеты кэшированы локально, интернет не требуется
5. **Clean Architecture:** Строгое разделение слоёв (Domain → Application → Infrastructure → Api/Web)

---

## Конвенции разработки

### Стиль кода
- C# 12 с включёнными nullable reference types
- Minimal APIs для эндпоинтов
- Паттерн MediatR для бизнес-логики
- FluentValidation для валидации команд/запросов

### Тестирование
- xUnit для юнит-тестов
- 33 теста покрывают доменную логику, обработчики приложений, инфраструктуру
- Тесты в `tests/` зеркалят структуру `src/`

### Конфигурация
- YAML-файлы в `config/workspaces/` для определений рабочих мест/пайплайнов
- `appsettings.json` для runtime-конфигурации (БД, JWT и т.д.)
- Переменные окружения переопределяют appsettings

---

## Учётные данные по умолчанию

После первого запуска (автоматически создаются):
- **Логин:** `admin`
- **Пароль:** `admin123`

---

## Ключевые файлы

| Файл | Назначение |
|------|------------|
| `NuGet.Config` | Конфигурация локального источника NuGet (offline-сборка) |
| `PRD_NewsProcessingSystem.md` | Документ требований продукта (3300+ строк) |
| `PROJECT_STATUS.md` | Текущий статус проекта, архитектура, TODO |
| `HOW_TO_DEPLOY.md` | Инструкция по развёртыванию |
| `config/workspaces/pipelines/*.yaml` | Определения жизненных циклов |
| `config/workspaces/*.yaml` | Определения UI/действий рабочих мест |

---

## Типовые задачи

### Добавить новое рабочее место
1. Создать YAML-файл в `config/workspaces/`
2. Определить `input_queue`, `actions`, `work_layout`
3. Перезапустить приложение

### Добавить новый статус в пайплайн
1. Отредактировать `config/workspaces/pipelines/{entity}_default.yaml`
2. Добавить статус в массив `statuses`
3. Определить переходы к/из нового статуса
4. Перезапустить приложение

### Отладка обработчика MediatR
Обработчики находятся в `src/NewsFlow.Application/Features/{Feature}/`

### Проверить покрытие тестами
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Известные ограничения / TODO

- Интеграция MinIO (сейчас локальное файловое хранилище)
- RS256 JWT (сейчас HMAC-SHA256)
- Сохранение RefreshToken в БД
- RTL-поддержка для арабских текстов
- YAML-редактор в админке
- Интеграционные тесты (API + EF)
- UI загрузки материалов для оператора
- PostGIS для геоданных
