# NewsFlow — Инструкция по сборке и развёртыванию

## Предварительные требования

- **.NET 8 SDK** (для сборки из исходников) или .NET 8 Runtime + ASP.NET Core Runtime (для запуска публикации)
- **PostgreSQL 16+** — только если выбран режим PostgreSQL (опционально)

---

## 1. Сборка проекта (offline, без интернета)

Все NuGet-пакеты уже скачаны в папку `packages/`. Файл `NuGet.Config` настроен на использование только локальных пакетов.

```bash
# Восстановление зависимостей (из локальной папки packages/)
dotnet restore

# Сборка
dotnet build

# Запуск тестов (опционально)
dotnet test
```

> Если папка `packages/` отсутствует, пересоздайте её на машине с интернетом:
> ```bash
> mv NuGet.Config NuGet.Config.bak
> dotnet restore --packages ./packages
> mv NuGet.Config.bak NuGet.Config
> ```

---

## 2. Выбор СУБД

Система поддерживает два провайдера базы данных. Переключение выполняется через параметр `"DatabaseProvider"` в файле `appsettings.json`.

Файлы конфигурации:
- `src/NewsFlow.Web/appsettings.json` — Blazor Server (основной UI)
- `src/NewsFlow.Api/appsettings.json` — REST API (для внешнего доступа)

### Вариант A: SQLite (по умолчанию)

Не требует установки СУБД. База данных создаётся автоматически в виде файла `newsflow.db` рядом с исполняемым файлом.

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=newsflow.db"
  }
}
```

Подходит для:
- Демонстрации и тестирования
- Небольшого числа пользователей (до ~20)
- Быстрого запуска без настройки инфраструктуры

### Вариант B: PostgreSQL (рекомендуется для продакшена)

Требует установленный и запущенный PostgreSQL 16+.

#### Подготовка базы данных

```sql
CREATE DATABASE newsflow;
CREATE USER newsflow_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE newsflow TO newsflow_user;
```

#### Конфигурация приложения

```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=newsflow;Username=newsflow_user;Password=your_secure_password"
  }
}
```

Подходит для:
- Продакшен-окружения (~100 пользователей)
- Надёжного хранения данных с бэкапами
- Параллельной работы множества пользователей

---

## 3. Переключение между конфигурациями

### Через appsettings.json

Отредактируйте файл `appsettings.json` в проекте, который запускаете:

**SQLite → PostgreSQL:**
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=newsflow;Username=postgres;Password=postgres"
  }
}
```

**PostgreSQL → SQLite:**
```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=newsflow.db"
  }
}
```

### Через переменные окружения (без изменения файлов)

```bash
# SQLite
export DatabaseProvider=SQLite
export ConnectionStrings__DefaultConnection="Data Source=newsflow.db"
dotnet run --project src/NewsFlow.Web

# PostgreSQL
export DatabaseProvider=PostgreSQL
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=newsflow;Username=postgres;Password=postgres"
dotnet run --project src/NewsFlow.Web
```

На Windows (PowerShell):
```powershell
# SQLite
$env:DatabaseProvider = "SQLite"
$env:ConnectionStrings__DefaultConnection = "Data Source=newsflow.db"
dotnet run --project src/NewsFlow.Web

# PostgreSQL
$env:DatabaseProvider = "PostgreSQL"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=newsflow;Username=postgres;Password=postgres"
dotnet run --project src/NewsFlow.Web
```

### Через аргументы командной строки

```bash
dotnet run --project src/NewsFlow.Web -- --DatabaseProvider=SQLite
dotnet run --project src/NewsFlow.Web -- --DatabaseProvider=PostgreSQL
```

> **Приоритет конфигурации** (от низшего к высшему):
> `appsettings.json` → `appsettings.{Environment}.json` → переменные окружения → аргументы командной строки

---

## 4. Запуск

### Из исходников (разработка)

```bash
# Blazor Server — основной интерфейс
dotnet run --project src/NewsFlow.Web
# По умолчанию: http://localhost:5000

# REST API — для внешнего/программного доступа (опционально)
dotnet run --project src/NewsFlow.Api
# Swagger: http://localhost:5002/swagger
```

Указать свой порт:
```bash
dotnet run --project src/NewsFlow.Web --urls "http://localhost:8080"
```

### Self-contained публикация (продакшен)

Создание автономной сборки, не требующей .NET SDK на целевой машине:

```bash
# Windows
dotnet publish src/NewsFlow.Web -c Release -r win-x64 --self-contained -o ./publish/web

# Linux
dotnet publish src/NewsFlow.Web -c Release -r linux-x64 --self-contained -o ./publish/web
```

Запуск:
```bash
# Windows
./publish/web/NewsFlow.Web.exe

# Linux
chmod +x ./publish/web/NewsFlow.Web
./publish/web/NewsFlow.Web
```

---

## 5. Первый вход

При первом запуске автоматически:
- Создаётся схема БД (для SQLite — `EnsureCreated`, для PostgreSQL — миграции)
- Заполняются seed-данные: 8 ролей, 30 языков (ISO 639-1), учётная запись администратора

**Учётные данные по умолчанию:**
| Поле | Значение |
|------|----------|
| Логин | `admin` |
| Пароль | `admin123` |

После входа:
1. `/admin/users` — создайте пользователей и назначьте роли
2. `/admin/sources` — заполните справочник источников
3. `/admin/countries` — добавьте страны
4. `/admin/tags` — создайте теги

---

## 6. Сетевой доступ

По умолчанию приложение доступно только на `localhost`. Для доступа с других машин:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://0.0.0.0:5000" },
      "Https": { "Url": "https://0.0.0.0:5001" }
    }
  }
}
```

Или через переменную:
```bash
ASPNETCORE_URLS="http://0.0.0.0:5000" dotnet run --project src/NewsFlow.Web
```

---

## 7. Обслуживание

| Задача | Команда / Действие |
|--------|--------------------|
| Логи | Консоль (Serilog). API также пишет в `logs/newsflow-*.log` |
| Бэкап SQLite | Скопировать файл `newsflow.db` |
| Бэкап PostgreSQL | `pg_dump newsflow > backup.sql` |
| Обновление YAML | Отредактировать файлы в `config/workspaces/`, перезапустить приложение |
| Смена JWT-секрета | Изменить `Jwt:Secret` в `appsettings.json` (мин. 32 символа) |
