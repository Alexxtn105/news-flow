# NewsFlow — Система обработки новостных материалов

## Описание проекта

NewsFlow — изолированная (air-gapped, без доступа к интернету) система для приёма, перевода, анализа и выпуска документов на основе новостных материалов. Обеспечивает полный конвейер обработки: от загрузки сырого материала до выпуска готового аналитического документа, прошедшего ревью, регистрацию, контроль и оценку.

Рассчитана на ~100 одновременных пользователей. Все рабочие места конфигурируются через YAML без изменения кода.

---

## Текущий статус: Phase 1 + Phase 2 — реализованы

Все 12 итераций завершены. Система полностью компилируется и проходит тесты.

| Метрика | Значение |
|---------|----------|
| Исходные файлы (src) | 143 (.cs + .razor) |
| Строк кода (C#) | ~4 700 |
| Строк кода (Razor) | ~1 700 |
| Строк тестов | ~660 |
| Юнит-тестов | 33 (все проходят) |
| Проектов (src) | 6 |
| Проектов (tests) | 4 |
| YAML-конфигураций | 8 |

---

## Стек технологий

| Компонент | Технология |
|-----------|-----------|
| Runtime | .NET 8 LTS |
| Backend API | ASP.NET Core Minimal APIs |
| CQRS | MediatR |
| Валидация | FluentValidation |
| ORM | Entity Framework Core 8 |
| СУБД | PostgreSQL 16+ (или SQLite для автономной работы) |
| UI | Blazor Server + MudBlazor 7 |
| Аутентификация | JWT (HMAC-SHA256), BCrypt |
| Real-time | SignalR |
| Логирование | Serilog |
| Конфигурация | YamlDotNet |
| Файловое хранилище | Локальная файловая система (заглушка для MinIO) |

---

## Архитектура

**Clean Architecture + DDD**, 6 слоёв:

```
NewsFlow.sln
├── src/
│   ├── NewsFlow.Domain           — сущности, value objects, перечисления, интерфейсы
│   ├── NewsFlow.Application      — use cases (команды/запросы MediatR), DTO, валидация
│   ├── NewsFlow.Infrastructure   — EF Core, JWT, BCrypt, файловое хранилище, репозитории
│   ├── NewsFlow.Infrastructure.Yaml — парсинг и валидация YAML-конфигураций
│   ├── NewsFlow.Api              — Minimal API endpoints, SignalR hub, middleware
│   └── NewsFlow.Web              — Blazor Server: страницы, компоненты, сервисы
├── config/
│   └── workspaces/
│       ├── pipelines/            — material_default.yaml, document_default.yaml
│       ├── translator.yaml, analyst.yaml, reviewer.yaml,
│       │   registrar.yaml, controller.yaml, evaluator.yaml
└── tests/
    ├── NewsFlow.Domain.Tests
    ├── NewsFlow.Application.Tests
    ├── NewsFlow.Infrastructure.Tests
    └── NewsFlow.Api.Tests
```

**Ключевое решение:** Blazor Server вызывает MediatR напрямую (in-process) через `ISender`, без HTTP. API-эндпоинты существуют параллельно для внешнего/программного доступа. Один хост обслуживает и API (`/api/*`), и Blazor.

---

## Доменная модель

### Сущности (15 штук)

| Сущность | Тип | Описание |
|----------|-----|----------|
| User | Aggregate Root | Пользователь с ролями, языками, блокировкой |
| Role | Entity | Роль (Administrator, Translator, Analyst, ...) |
| Material | Aggregate Root | Новостной материал с полным автоматом статусов |
| Attachment | Entity | Медиа-файл, прикреплённый к материалу |
| Document | Aggregate Root | Аналитический документ с конвейером обработки |
| DocumentMaterial | Entity | Связь документа с исходными материалами |
| DocumentComment | Entity | Замечание ревьюера/контролёра |
| DocumentVersion | Entity | Снимок версии документа |
| DocumentDistribution | Entity | Рассылка документа адресату |
| Recipient | Entity | Адресат рассылки |
| Source | Entity | Источник информации |
| Language | Entity | Язык (ISO 639-1) |
| Country | Entity | Страна (ISO 3166-1) |
| Tag | Entity | Тег/категория |
| AuditLog | Entity | Запись аудит-лога |

### Конечные автоматы

**Material:**
```
New → InTranslation → Translated → InAnalysis → Processed
│        ↓                │            ↓
│   ReturnedFrom          │     ReturnedFrom
│   Translation           │     Analysis
└─────────┴───────────────┴────────────┴──→ Rejected
```

**Document:**
```
Draft → InReview → Reviewed → InRegistration → Registered →
  ↑       ↓                                        ↓
  │  ReturnedFor                               InControl → Controlled → InEvaluation → Evaluated
  │  Revision                                      ↓
  │                                          ReturnedFor
  │                                          Control
  └── Cancelled (с любого незаблокированного)
```

---

## Роли пользователей (8)

| Роль | Код | Рабочее место |
|------|-----|---------------|
| Администратор | Administrator | Админка (пользователи, справочники, пайплайны, аудит) |
| Оператор загрузки | Operator | Загрузка материалов |
| Переводчик | Translator | `/workspace/translator` — перевод материалов |
| Аналитик | Analyst | `/workspace/analyst` — анализ и создание документов |
| Ревьюер | Reviewer | `/workspace/reviewer` — ревью документов |
| Регистратор | Registrar | `/workspace/registrar` — регистрация документов |
| Контролёр | Controller | `/workspace/controller` — контроль документов |
| Оценщик | Evaluator | `/workspace/evaluator` — оценка документов |

---

## Blazor-страницы (18)

### Общие
- `/` и `/login` — Страница входа
- `/home` — Главная с карточками рабочих мест по ролям

### Рабочие места
- `/workspace/translator` — Переводчик (split-panel: очередь + редактор перевода)
- `/workspace/analyst` — Аналитик (three-panel: материал + документ + оригинал)
- `/workspace/{code}` — GenericWorkspace (ревьюер, регистратор, контролёр, оценщик)

### Админка
- `/admin/users` — CRUD пользователей с ролями и языками
- `/admin/sources` — Справочник источников
- `/admin/languages` — Справочник языков (ISO 639-1)
- `/admin/countries` — Справочник стран (ISO 3166-1)
- `/admin/tags` — Справочник тегов
- `/admin/audit-log` — Журнал аудита с фильтрами
- `/admin/pipelines` — Просмотр конвейеров и рабочих мест

---

## API-эндпоинты (8 групп)

| Группа | Путь | Описание |
|--------|------|----------|
| Auth | `/api/auth/*` | login, refresh, me, logout |
| Users | `/api/admin/users` | CRUD пользователей |
| References | `/api/admin/sources,languages,countries,tags,roles` | Справочники |
| Materials | `/api/materials` | CRUD материалов + файлы |
| Translator | `/api/materials/{id}/take,translate,...` | Действия переводчика |
| Documents | `/api/documents` | CRUD документов + workflow |
| Workspaces | `/api/workspaces/{code}/*` | Универсальные действия через Pipeline Engine |
| Distributions | `/api/documents/{id}/distribute`, `/api/distributions/{id}/evaluate` | Рассылка + оценки |

**SignalR Hub:** `/hubs/notifications`

---

## YAML Pipeline Engine

Система поддерживает конфигурируемые конвейеры и рабочие места через YAML-файлы.

**Конвейеры** (`config/workspaces/pipelines/`) определяют:
- Статусы сущности (initial, intermediate, terminal, returned)
- Переходы между статусами (кто может, какие поля требуются, блокировка)
- Таймауты блокировок

**Рабочие места** (`config/workspaces/`) определяют:
- Фильтры входной очереди
- Доступные действия с лейблами
- Layout интерфейса (split-panel, three-panel)

Новое рабочее место создаётся добавлением YAML-файла без изменения кода.

---

## Итоги реализации по итерациям

| # | Итерация | Что сделано |
|---|----------|-------------|
| 1 | Каркас + домен + БД | Solution, 15 сущностей, 4 enum, EF Core, миграции, seed (8 ролей, 30 языков, admin user) |
| 2 | Аутентификация | JWT login/refresh, BCrypt, CRUD пользователей, политики авторизации |
| 3 | Справочники + аудит | CRUD для Source/Language/Country/Tag, аудит-лог с фильтрами |
| 4 | Материалы + файлы | Агрегат Material, загрузка файлов, локальное хранилище |
| 5 | Переводчик | Очередь → взять → перевести → завершить/вернуть/отбраковать |
| 6 | Аналитик + Document | Агрегат Document с полным workflow, создание документа из материалов |
| 7 | Blazor UI | Login, MainLayout, TranslatorWorkspace, AnalystWorkspace |
| 8 | Админка Blazor | Users, Sources, Languages, Countries, Tags, AuditLog — полный CRUD |
| 9 | YAML Engine | PipelineDefinition, WorkspaceDefinition, парсер, валидатор, провайдеры |
| 10 | Конвейер документов | GenericWorkspace.razor, все этапы (ревью → регистрация → контроль → оценка) |
| 11 | Рассылка + SignalR | Recipient, DocumentDistribution, SignalR NotificationHub, NotificationBell |
| 12 | Тесты + полировка | 33 юнит-теста, ExceptionHandlingMiddleware, Pipelines.razor |

---

## Запуск в изолированной среде

### Предварительные требования

На целевой машине (air-gapped) должны быть предустановлены:

1. **.NET 8 SDK** (или как минимум .NET 8 Runtime + ASP.NET Core Runtime)
2. **PostgreSQL 16+** — база данных (опционально; без PostgreSQL система автоматически использует SQLite)
3. **Все NuGet-пакеты** — скачать заранее на машине с интернетом

### Подготовка на машине с интернетом

```bash
# 1. Клонировать/скопировать репозиторий
# 2. Восстановить пакеты и создать локальный кэш
cd D:\Install\pr
dotnet restore
dotnet nuget locals global-packages --list
# Скопировать содержимое папки global-packages на переносной носитель

# 3. Альтернатива: создать автономную публикацию (self-contained)
dotnet publish src/NewsFlow.Web -c Release -r win-x64 --self-contained -o ./publish/web
dotnet publish src/NewsFlow.Api -c Release -r win-x64 --self-contained -o ./publish/api
# Папка publish/ содержит всё необходимое, .NET SDK не нужен на целевой машине
```

### Выбор СУБД

Система поддерживает два провайдера баз данных, настраиваемых через `appsettings.json`:

#### Вариант A: SQLite (по умолчанию, zero-config)

По умолчанию в `appsettings.json` настроен SQLite — не требует установки СУБД:

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=newsflow.db"
  }
}
```

При запуске автоматически создаётся файл `newsflow.db` рядом с приложением. Подходит для демонстрации и небольшого числа пользователей.

#### Вариант B: PostgreSQL (рекомендуется для продакшена)

```sql
-- Создать базу данных
CREATE DATABASE newsflow;
CREATE USER newsflow_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE newsflow TO newsflow_user;
```

Отредактируйте `appsettings.json` (в `src/NewsFlow.Web/` и `src/NewsFlow.Api/`):

```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=newsflow;Username=newsflow_user;Password=your_secure_password"
  },
  "Jwt": {
    "Secret": "YourSecureSecretKeyAtLeast32BytesLong!"
  }
}
```

### Сборка с локальными пакетами (offline)

В репозитории уже есть всё необходимое для сборки без интернета:

- **`packages/`** — папка со всеми 172 NuGet-зависимостями
- **`NuGet.Config`** — конфигурация, указывающая на `./packages` как единственный источник пакетов

```bash
# Скопируйте весь каталог проекта на целевую машину
# Убедитесь, что .NET 8 SDK установлен

# Восстановление пакетов из локальной папки (NuGet.Config уже настроен)
dotnet restore

# Сборка
dotnet build

# Запуск тестов (опционально)
dotnet test
```

> **Важно:** Файл `NuGet.Config` в корне проекта содержит `<clear />` и единственный источник `./packages`. Это гарантирует, что `dotnet restore` не будет пытаться обращаться к nuget.org. Если нужно вернуть доступ к публичному NuGet (на машине с интернетом), удалите или переименуйте `NuGet.Config`.

Если папка `packages/` была потеряна или нужно обновить зависимости, пересоздайте её на машине с интернетом:

```bash
# Удалить NuGet.Config или временно переименовать
mv NuGet.Config NuGet.Config.bak

# Скачать все пакеты в локальную папку
dotnet restore --packages ./packages

# Вернуть NuGet.Config
mv NuGet.Config.bak NuGet.Config
```

### Запуск (вариант 1: из исходников)

```bash
# Сборка (NuGet.Config автоматически берёт пакеты из ./packages)
dotnet build

# Запуск тестов (опционально)
dotnet test

# Запуск Web-приложения (Blazor Server — основной интерфейс)
dotnet run --project src/NewsFlow.Web
# Откроется на https://localhost:5001

# Запуск API (если нужен отдельно для внешнего доступа)
dotnet run --project src/NewsFlow.Api
# Swagger доступен на https://localhost:5002/swagger
```

### Запуск (вариант 2: из публикации, self-contained)

```bash
# Скопировать папку publish/ на целевую машину

# Запуск Web (Blazor Server)
./publish/web/NewsFlow.Web.exe
# или на Linux:
./publish/web/NewsFlow.Web

# Запуск API (опционально, если нужен отдельный API-сервер)
./publish/api/NewsFlow.Api.exe
```

### Первый вход

При первом запуске автоматически:
- Выполняется миграция БД (создаются все таблицы)
- Создаются seed-данные: 8 ролей, 30 языков (ISO 639-1), администратор

**Учётная запись по умолчанию:**
- Логин: `admin`
- Пароль: `admin123`

После входа как администратор:
1. Создайте пользователей через `/admin/users`
2. Назначьте им роли (Translator, Analyst, Reviewer, ...)
3. Переводчикам назначьте языки
4. Заполните справочники: источники, страны, теги

### Сетевая конфигурация

По умолчанию приложение слушает на `localhost`. Для доступа с других машин в сети, добавьте в `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      },
      "Https": {
        "Url": "https://0.0.0.0:5001"
      }
    }
  }
}
```

Или через переменную окружения:
```bash
ASPNETCORE_URLS=http://0.0.0.0:5000 dotnet run --project src/NewsFlow.Web
```

### Обслуживание

- **Логи** пишутся в консоль (Serilog). Для API также в `logs/newsflow-*.log`.
- **Бэкап БД**: стандартный `pg_dump newsflow > backup.sql`
- **Обновление YAML-конфигураций**: отредактируйте файлы в `config/workspaces/`, перезапустите приложение.

---

## Что не реализовано / TODO

- [ ] MinIO для объектного хранилища (сейчас локальная файловая система)
- [ ] RS256 для JWT (сейчас HMAC-SHA256)
- [ ] RefreshToken в БД (сейчас только access token)
- [ ] RTL-поддержка (`dir="auto"`) для арабских текстов
- [ ] Traefik reverse proxy конфигурация
- [ ] FluentValidation правила для всех команд
- [ ] Интеграционные тесты (API + EF)
- [ ] Фильтр очереди переводчика по его языкам
- [ ] YAML-редактор в админке (сейчас только просмотр)
- [ ] Docker / docker-compose для деплоя
- [ ] Загрузка материалов (UI-страница оператора)
- [ ] PostGIS для геоданных
