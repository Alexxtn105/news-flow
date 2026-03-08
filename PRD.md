# PRD: Система обработки новостных материалов (NewsFlow)

**Версия документа:** 0.1 (черновик)
**Дата:** 2026-03-01
**Статус:** В разработке

---

## 1. Обзор продукта

### 1.1 Назначение

NewsFlow — изолированная (без доступа к интернету) система для приёма, перевода, анализа и выпуска документов на основе новостных материалов. Система обеспечивает полный цикл обработки: от загрузки сырого материала до выпуска готового аналитического документа, прошедшего ревью и регистрацию.

### 1.2 Ключевые характеристики

| Параметр | Значение |
|---|---|
| Среда развёртывания | Изолированная сеть (air-gapped), без доступа в интернет |
| Кол-во пользователей | ~100 одновременных |
| База данных | PostgreSQL 16+ |
| Backend | C# / ASP.NET Core 8+ |
| Архитектура | Clean Architecture, DDD |
| Аутентификация | JWT (логин + пароль) |

### 1.3 Основные роли

| Роль | Описание |
|---|---|
| **Администратор** | Управление пользователями, справочниками, конфигурацией рабочих мест |
| **Оператор загрузки** | Загрузка и первичная разметка материалов |
| **Переводчик** | Перевод иноязычных материалов |
| **Аналитик** | Анализ материалов и подготовка документов |
| **Ревьюер** | Проверка качества подготовленных документов |
| **Регистратор** | Регистрация и присвоение номеров документам |
| **Контролёр** | Финальный контроль и оценка документов |

---

## 2. Пользовательские сценарии (User Stories)

### US-1: Аутентификация
> Как пользователь, я хочу входить в систему по логину и паролю, чтобы получить доступ к своему рабочему месту.

**Критерии приёмки:**
- Форма ввода логина и пароля
- JWT-токен выдаётся при успешной аутентификации (access + refresh)
- Access-токен — срок жизни 30 мин, refresh — 8 часов (настраивается)
- После входа пользователь видит только те рабочие места, которые соответствуют его ролям
- Блокировка учётной записи после N неудачных попыток (настраивается)

### US-2: Загрузка материалов
> Как оператор загрузки, я хочу загружать новостные материалы (текст, аудио, видео) и заполнять метаданные, чтобы материалы были доступны для дальнейшей обработки.

**Критерии приёмки:**
- Форма загрузки с полями метаданных (см. раздел 3.1)
- Поддержка текстовых файлов, аудио (mp3, wav, ogg), видео (mp4, avi, mkv)
- Ограничение размера файла — настраивается (по умолчанию 500 МБ)
- После загрузки материал получает статус `New`
- Валидация обязательных полей перед сохранением

### US-3: Перевод материалов
> Как переводчик, я хочу получать материалы на иностранном языке и вводить перевод, чтобы аналитики могли работать с переведённым текстом.

**Критерии приёмки:**
- Переводчик видит очередь материалов, требующих перевода, отфильтрованную по его языкам
- При взятии материала в работу он блокируется для других переводчиков (статус `InTranslation`)
- Переводчик видит оригинал и поле для ввода перевода (side-by-side)
- Возможность сохранить черновик перевода
- После завершения перевода материал переходит в статус `Translated`
- Возможность вернуть материал в очередь (отказ от перевода)

### US-4: Анализ и подготовка документов
> Как аналитик, я хочу получать переведённые материалы и на их основе создавать аналитические документы.

**Критерии приёмки:**
- Аналитик видит очередь переведённых материалов
- При взятии материала в работу он блокируется для других аналитиков (статус `InAnalysis`)
- Аналитик может создать документ на основе одного или нескольких материалов
- Документ содержит структурированный текст, ссылки на исходные материалы
- После завершения документ переходит в статус `Draft`

### US-5: Ревью документов
> Как ревьюер, я хочу проверять подготовленные документы и возвращать их на доработку или одобрять.

**Критерии приёмки:**
- Очередь документов со статусом `Draft`
- Блокировка документа при взятии в ревью (`InReview`)
- Возможность добавить замечания
- Одобрение → статус `Reviewed` / Возврат → статус `ReturnedForRevision`

### US-6: Регистрация, контроль, оценка
> Как регистратор/контролёр, я хочу обрабатывать документы на своём этапе конвейера.

**Критерии приёмки:**
- Каждый этап: регистрация (`Registered`), контроль (`Controlled`), оценка (`Evaluated`)
- На каждом этапе — блокировка, обработка, переход к следующему статусу или возврат
- Все этапы конфигурируемы (см. раздел 4)

### US-7: Администрирование
> Как администратор, я хочу управлять пользователями, справочниками и конфигурацией рабочих мест.

**Критерии приёмки:**
- CRUD пользователей с назначением ролей
- Редактирование справочников (источники, страны, языки и т.д.)
- Конфигурирование рабочих мест через UI и YAML-файлы
- Создание новых рабочих мест без изменения кода

---

## 3. Доменная модель (DDD)

### 3.1 Агрегат: Material (Материал)

```
Material (Aggregate Root)
├── Id: Guid
├── Title: string                    — заголовок
├── OriginalText: string             — исходный текст
├── TranslatedText: string?          — переведённый текст
├── OriginalLanguage: Language        — язык оригинала
├── Source: Source                     — источник информации
├── Country: Country                  — страна
├── Location: string?                 — место (город/регион)
├── ReceivedAt: DateTime              — дата/время поступления
├── EventDate: DateTime?              — дата события в новости
├── Status: MaterialStatus            — текущий статус
├── AssignedTo: UserId?               — кому назначен (блокировка)
├── AssignedAt: DateTime?             — когда назначен
├── CreatedBy: UserId                 — кто загрузил
├── CreatedAt: DateTime               — когда загружен
├── Attachments: List<Attachment>     — медиафайлы
├── Priority: Priority                — приоритет (Normal, High, Urgent)
└── Tags: List<Tag>                   — тэги/категории
```

**MaterialStatus (конечный автомат):**

```
New → InTranslation → Translated → InAnalysis → Processed
 │        ↓                │            ↓
 │   ReturnedFrom          │     ReturnedFrom
 │   Translation           │     Analysis
 │        │                │            │
 └────────┴────────────────┴────────────┴──→ Rejected (отбраковка с любого этапа)
```

> **Отбраковка:** Материал может быть отбракован (статус `Rejected`) на любом этапе обработки — переводчиком, аналитиком или оператором. При отбраковке обязателен комментарий с причиной. Отбракованные материалы не попадают в документы.

### 3.2 Агрегат: Document (Документ)

```
Document (Aggregate Root)
├── Id: Guid
├── RegistrationNumber: string?       — регистрационный номер
├── Title: string                     — заголовок
├── Content: string                   — текст документа
├── Status: DocumentStatus            — текущий статус
├── AssignedTo: UserId?               — кому назначен
├── AssignedAt: DateTime?
├── CreatedBy: UserId                 — автор (аналитик)
├── CreatedAt: DateTime
├── SourceMaterials: List<MaterialRef> — ссылки на исходные материалы
├── ReviewComments: List<Comment>     — замечания ревьюера
├── Evaluation: Evaluation?           — оценка
└── Priority: Priority
```

**DocumentStatus (конечный автомат):**

```
Draft → InReview → Reviewed → InRegistration → Registered → InControl → Controlled → InEvaluation → Evaluated
           ↓                                                    ↓
    ReturnedForRevision                                  ReturnedForControl
```

### 3.3 Value Objects

```
Source          { Id, Name, Description, Type }
Language        { Id, Code, Name }           — ISO 639-1 (ru, en, ar...)
Country         { Id, Code, Name }           — ISO 3166-1
Tag             { Id, Name, Category }
Attachment      { Id, FileName, ContentType, Size, BucketName, ObjectKey }
Comment         { Id, AuthorId, Text, CreatedAt }
Evaluation      { Score, Commentary, EvaluatedBy, EvaluatedAt }
```

### 3.4 Агрегат: User (Пользователь)

```
User (Aggregate Root)
├── Id: Guid
├── Username: string
├── PasswordHash: string
├── FullName: string
├── Roles: List<Role>
├── Languages: List<Language>         — языки (для переводчиков)
├── IsActive: bool
├── FailedLoginAttempts: int
├── LockedUntil: DateTime?
├── CreatedAt: DateTime
└── LastLoginAt: DateTime?
```

### 3.5 Агрегат: Workspace (Рабочее место)

```
Workspace (Aggregate Root)
├── Id: Guid
├── Code: string                      — уникальный код (translator, analyst...)
├── Name: string                      — отображаемое имя
├── Description: string?
├── RequiredRole: Role                — требуемая роль пользователя
├── InputFilter: FilterConfig         — фильтр входных данных
├── Actions: List<ActionConfig>       — доступные действия
├── OutputTransitions: List<Transition> — переходы статусов
├── UILayout: LayoutConfig            — конфигурация интерфейса
├── IsActive: bool
└── CreatedAt: DateTime
```

---

## 4. Конфигурируемые рабочие места (YAML)

### 4.1 Концепция

Каждое рабочее место описывается YAML-файлом, который определяет:
- **Какие данные** показывать (фильтры/запросы на вход)
- **Какие действия** доступны (кнопки, формы, переходы)
- **Какие обновления** выполнять в БД при каждом действии
- **Как выглядит** интерфейс (layout)

### 4.2 Пример: Рабочее место переводчика

```yaml
workspace:
  code: "translator"
  name: "Рабочее место переводчика"
  description: "Перевод иноязычных материалов"
  required_role: "Translator"

  # Фильтр входной очереди
  input_queue:
    entity: "Material"
    filter:
      status: "New"
      original_language: "!= ru"  # не русский
      assigned_to: null            # не назначен никому
    sort:
      - field: "priority"
        direction: "desc"
      - field: "received_at"
        direction: "asc"
    display_columns:
      - { field: "title", label: "Заголовок", width: "30%" }
      - { field: "source.name", label: "Источник", width: "15%" }
      - { field: "original_language.name", label: "Язык", width: "10%" }
      - { field: "received_at", label: "Поступил", width: "15%", format: "datetime" }
      - { field: "priority", label: "Приоритет", width: "10%" }

  # Действия при взятии в работу
  actions:
    take:
      label: "Взять в работу"
      update:
        status: "InTranslation"
        assigned_to: "$current_user"
        assigned_at: "$now"
      log_message: "Материал взят в перевод"

    save_draft:
      label: "Сохранить черновик"
      update:
        translated_text: "$input.translated_text"
      log_message: "Черновик перевода сохранён"

    complete:
      label: "Завершить перевод"
      requires_fields: ["translated_text"]
      update:
        translated_text: "$input.translated_text"
        status: "Translated"
        assigned_to: null
        assigned_at: null
      log_message: "Перевод завершён"

    release:
      label: "Вернуть в очередь"
      update:
        status: "New"
        assigned_to: null
        assigned_at: null
      log_message: "Материал возвращён в очередь перевода"

  # Рабочая форма (layout)
  work_layout:
    type: "split-panel"
    left:
      title: "Оригинал"
      fields:
        - { field: "title", readonly: true }
        - { field: "source.name", readonly: true }
        - { field: "original_language.name", readonly: true }
        - { field: "original_text", type: "textarea", readonly: true }
        - { field: "attachments", type: "media-player", readonly: true }
    right:
      title: "Перевод"
      fields:
        - { field: "translated_text", type: "textarea", required: true, placeholder: "Введите перевод..." }
```

### 4.3 Пример: Рабочее место аналитика

```yaml
workspace:
  code: "analyst"
  name: "Рабочее место аналитика"
  description: "Анализ материалов и подготовка документов"
  required_role: "Analyst"

  input_queue:
    entity: "Material"
    filter:
      status: "Translated"
      assigned_to: null
    sort:
      - field: "priority"
        direction: "desc"
      - field: "received_at"
        direction: "asc"
    display_columns:
      - { field: "title", label: "Заголовок", width: "25%" }
      - { field: "source.name", label: "Источник", width: "15%" }
      - { field: "country.name", label: "Страна", width: "10%" }
      - { field: "translated_text", label: "Перевод (превью)", width: "30%", truncate: 100 }
      - { field: "priority", label: "Приоритет", width: "10%" }

  actions:
    take:
      label: "Взять в анализ"
      update:
        status: "InAnalysis"
        assigned_to: "$current_user"
        assigned_at: "$now"
      log_message: "Материал взят в анализ"

    create_document:
      label: "Создать документ"
      creates_entity: "Document"
      mapping:
        title: "$input.document_title"
        content: "$input.document_content"
        source_materials: ["$current_entity.id"]
        status: "Draft"
        created_by: "$current_user"
      post_update:
        status: "Processed"
        assigned_to: null
      log_message: "На основе материала создан документ"

    release:
      label: "Вернуть в очередь"
      update:
        status: "Translated"
        assigned_to: null
        assigned_at: null
      log_message: "Материал возвращён в очередь анализа"

  work_layout:
    type: "three-panel"
    left:
      title: "Материал"
      fields:
        - { field: "title", readonly: true }
        - { field: "source.name", readonly: true }
        - { field: "country.name", readonly: true }
        - { field: "translated_text", type: "textarea", readonly: true }
        - { field: "attachments", type: "media-player", readonly: true }
    center:
      title: "Новый документ"
      fields:
        - { field: "document_title", type: "text", required: true, label: "Заголовок документа" }
        - { field: "document_content", type: "rich-textarea", required: true, label: "Текст документа" }
    right:
      title: "Справочная информация"
      fields:
        - { field: "original_text", type: "textarea", readonly: true, label: "Оригинал" }
```

### 4.4 Пример: Рабочее место ревьюера

```yaml
workspace:
  code: "reviewer"
  name: "Рабочее место ревьюера"
  required_role: "Reviewer"

  input_queue:
    entity: "Document"
    filter:
      status: "Draft"
      assigned_to: null

  actions:
    take:
      label: "Взять на ревью"
      update:
        status: "InReview"
        assigned_to: "$current_user"
        assigned_at: "$now"
      log_message: "Документ взят на ревью"

    approve:
      label: "Одобрить"
      update:
        status: "Reviewed"
        assigned_to: null
      log_message: "Документ одобрен"

    return:
      label: "Вернуть на доработку"
      requires_fields: ["comment"]
      update:
        status: "ReturnedForRevision"
        assigned_to: null
      creates_child:
        entity: "Comment"
        mapping:
          text: "$input.comment"
          author_id: "$current_user"
      log_message: "Документ возвращён на доработку"
```

### 4.5 Конфигурируемый конвейер обработки (Pipeline)

#### 4.5.1 Концепция

Конвейер — это описание **жизненного цикла** сущности (Material или Document): какие статусы существуют, какие переходы между ними разрешены, кто может выполнять переходы, и какие действия происходят при переходе.

Конвейеры описываются в отдельных YAML-файлах и загружаются при старте приложения. Администратор может создавать новые конвейеры и модифицировать существующие через админку (с валидацией).

#### 4.5.2 Структура YAML-конвейера

```yaml
pipeline:
  code: "document_default"          # уникальный код конвейера
  name: "Стандартный конвейер документа"
  description: "Полный цикл: черновик → ревью → регистрация → контроль → оценка"
  entity: "Document"                 # к какой сущности применяется
  version: 1                         # версия конфигурации

  # Определение всех статусов
  statuses:
    - code: "Draft"
      name: "Черновик"
      type: "initial"                # initial | intermediate | terminal | returned
      color: "#2196F3"

    - code: "InReview"
      name: "На ревью"
      type: "intermediate"
      is_locked: true                # сущность заблокирована за исполнителем
      color: "#FF9800"

    - code: "ReturnedForRevision"
      name: "Возвращён на доработку"
      type: "returned"
      color: "#F44336"

    - code: "Reviewed"
      name: "Проверен"
      type: "intermediate"
      color: "#4CAF50"

    - code: "InRegistration"
      name: "На регистрации"
      type: "intermediate"
      is_locked: true
      color: "#FF9800"

    - code: "Registered"
      name: "Зарегистрирован"
      type: "intermediate"
      color: "#4CAF50"

    - code: "InControl"
      name: "На контроле"
      type: "intermediate"
      is_locked: true
      color: "#FF9800"

    - code: "ReturnedForControl"
      name: "Возвращён на доработку (контроль)"
      type: "returned"
      color: "#F44336"

    - code: "Controlled"
      name: "Проконтролирован"
      type: "intermediate"
      color: "#4CAF50"

    - code: "InEvaluation"
      name: "На оценке"
      type: "intermediate"
      is_locked: true
      color: "#FF9800"

    - code: "Evaluated"
      name: "Оценён"
      type: "terminal"               # конечный статус
      color: "#009688"

    - code: "Cancelled"
      name: "Отменён"
      type: "terminal"
      color: "#9E9E9E"

  # Определение переходов
  transitions:
    # --- Ревью ---
    - from: "Draft"
      to: "InReview"
      action: "take"
      label: "Взять на ревью"
      required_role: "Reviewer"
      lock_to_user: true
      log_message: "Документ взят на ревью"

    - from: "InReview"
      to: "Reviewed"
      action: "approve"
      label: "Одобрить"
      required_role: "Reviewer"
      only_assigned_user: true       # только тот, кто взял
      unlock: true
      log_message: "Документ одобрен ревьюером"

    - from: "InReview"
      to: "ReturnedForRevision"
      action: "return"
      label: "Вернуть на доработку"
      required_role: "Reviewer"
      only_assigned_user: true
      unlock: true
      requires_fields: ["comment"]   # обязательный комментарий
      creates_child:
        entity: "Comment"
        mapping:
          text: "$input.comment"
          author_id: "$current_user"
      log_message: "Документ возвращён на доработку"

    - from: "ReturnedForRevision"
      to: "Draft"
      action: "revise"
      label: "Исправить и отправить заново"
      required_role: "Analyst"
      creates_version: true          # создать снимок перед изменением
      log_message: "Документ исправлен и отправлен повторно"

    # --- Регистрация ---
    - from: "Reviewed"
      to: "InRegistration"
      action: "take"
      label: "Взять на регистрацию"
      required_role: "Registrar"
      lock_to_user: true
      log_message: "Документ взят на регистрацию"

    - from: "InRegistration"
      to: "Registered"
      action: "register"
      label: "Зарегистрировать"
      required_role: "Registrar"
      only_assigned_user: true
      unlock: true
      requires_fields: ["registration_number"]
      update:
        registration_number: "$input.registration_number"
      creates_version: true
      log_message: "Документу присвоен номер {registration_number}"

    # --- Контроль ---
    - from: "Registered"
      to: "InControl"
      action: "take"
      label: "Взять на контроль"
      required_role: "Controller"
      lock_to_user: true
      log_message: "Документ взят на контроль"

    - from: "InControl"
      to: "Controlled"
      action: "approve"
      label: "Утвердить"
      required_role: "Controller"
      only_assigned_user: true
      unlock: true
      creates_version: true
      log_message: "Документ прошёл контроль"

    - from: "InControl"
      to: "ReturnedForControl"
      action: "return"
      label: "Вернуть"
      required_role: "Controller"
      only_assigned_user: true
      unlock: true
      requires_fields: ["comment"]
      creates_child:
        entity: "Comment"
        mapping:
          text: "$input.comment"
          author_id: "$current_user"
      log_message: "Документ возвращён контролёром"

    - from: "ReturnedForControl"
      to: "Registered"
      action: "fix"
      label: "Исправить"
      required_role: "Registrar"
      creates_version: true
      log_message: "Документ исправлен после контроля"

    # --- Оценка ---
    - from: "Controlled"
      to: "InEvaluation"
      action: "take"
      label: "Взять на оценку"
      required_role: "Evaluator"
      lock_to_user: true
      log_message: "Документ взят на оценку"

    - from: "InEvaluation"
      to: "Evaluated"
      action: "evaluate"
      label: "Оценить"
      required_role: "Evaluator"
      only_assigned_user: true
      unlock: true
      requires_fields: ["evaluation_score"]
      optional_fields: ["evaluation_comment"]
      update:
        evaluation_score: "$input.evaluation_score"
        evaluation_comment: "$input.evaluation_comment"
        evaluated_by: "$current_user"
        evaluated_at: "$now"
      creates_version: true
      log_message: "Документ оценён (балл: {evaluation_score})"

    # --- Отмена (доступна с любого незаблокированного статуса) ---
    - from: ["Draft", "Reviewed", "Registered", "Controlled", "ReturnedForRevision", "ReturnedForControl"]
      to: "Cancelled"
      action: "cancel"
      label: "Отменить"
      required_role: "Admin"
      requires_fields: ["comment"]
      log_message: "Документ отменён"

  # Таймауты блокировок
  lock_timeout:
    duration_hours: 4                # через 4 часа заблокированный документ возвращается в очередь
    fallback_status: "$previous"     # вернуть в предыдущий статус
    notify: true                     # уведомить пользователя
```

#### 4.5.3 Пример: Конвейер материала

```yaml
pipeline:
  code: "material_default"
  name: "Стандартный конвейер материала"
  entity: "Material"
  version: 1

  statuses:
    - code: "New"
      name: "Новый"
      type: "initial"
      color: "#2196F3"

    - code: "InTranslation"
      name: "На переводе"
      type: "intermediate"
      is_locked: true
      color: "#FF9800"

    - code: "Translated"
      name: "Переведён"
      type: "intermediate"
      color: "#4CAF50"

    - code: "InAnalysis"
      name: "В анализе"
      type: "intermediate"
      is_locked: true
      color: "#FF9800"

    - code: "Processed"
      name: "Обработан"
      type: "terminal"
      color: "#009688"

    - code: "Skipped"
      name: "Пропущен"
      type: "terminal"
      color: "#9E9E9E"

    - code: "Rejected"
      name: "Отбракован"
      type: "terminal"
      color: "#F44336"

  transitions:
    - from: "New"
      to: "InTranslation"
      action: "take"
      label: "Взять в перевод"
      required_role: "Translator"
      lock_to_user: true
      filter:                          # дополнительный фильтр на пользователя
        user_languages: "original_language"  # язык материала ∈ языки переводчика
      log_message: "Материал взят в перевод"

    - from: "New"
      to: "Translated"
      action: "mark_no_translation"
      label: "Перевод не требуется"
      required_role: "Translator"
      condition:                        # условие доступности перехода
        field: "original_language"
        operator: "eq"
        value: "ru"
      log_message: "Перевод не требуется (материал на русском)"

    - from: "New"
      to: "Skipped"
      action: "skip"
      label: "Пропустить"
      required_role: ["Translator", "Admin"]
      requires_fields: ["comment"]
      log_message: "Материал пропущен"

    - from: "InTranslation"
      to: "Translated"
      action: "complete"
      label: "Завершить перевод"
      required_role: "Translator"
      only_assigned_user: true
      unlock: true
      requires_fields: ["translated_text"]
      update:
        translated_text: "$input.translated_text"
      log_message: "Перевод завершён"

    - from: "InTranslation"
      to: "New"
      action: "release"
      label: "Вернуть в очередь"
      required_role: "Translator"
      only_assigned_user: true
      unlock: true
      log_message: "Материал возвращён в очередь перевода"

    - from: "Translated"
      to: "InAnalysis"
      action: "take"
      label: "Взять в анализ"
      required_role: "Analyst"
      lock_to_user: true
      log_message: "Материал взят в анализ"

    - from: "InAnalysis"
      to: "Processed"
      action: "complete"
      label: "Завершить анализ"
      required_role: "Analyst"
      only_assigned_user: true
      unlock: true
      creates_entity:                    # создание новой сущности
        entity: "Document"
        requires_fields: ["document_title", "document_content"]
        mapping:
          title: "$input.document_title"
          content: "$input.document_content"
          source_materials: ["$current_entity.id"]
          created_by: "$current_user"
          status: "Draft"
          pipeline_code: "document_default"
      log_message: "Анализ завершён, создан документ"

    - from: "InAnalysis"
      to: "Translated"
      action: "release"
      label: "Вернуть в очередь"
      required_role: "Analyst"
      only_assigned_user: true
      unlock: true
      log_message: "Материал возвращён в очередь анализа"

    # --- Отбраковка (доступна с любого не-терминального статуса) ---
    - from: ["New", "InTranslation", "Translated", "InAnalysis"]
      to: "Rejected"
      action: "reject"
      label: "Отбраковать"
      required_role: ["Translator", "Analyst", "Admin"]
      requires_fields: ["comment"]
      unlock: true                       # снять блокировку, если была
      creates_child:
        entity: "Comment"
        mapping:
          text: "$input.comment"
          author_id: "$current_user"
      log_message: "Материал отбракован: {comment}"

  lock_timeout:
    duration_hours: 4
    fallback_status: "$previous"
    notify: true
```

#### 4.5.4 Связь конвейера и рабочего места

Рабочее место (раздел 4.1–4.4) ссылается на конвейер:

```yaml
workspace:
  code: "translator"
  pipeline: "material_default"       # какой конвейер используется
  # ...остальная конфигурация
```

Движок рабочего места при отображении очереди и доступных действий **автоматически** подтягивает из конвейера:
- Какие статусы являются входными для этого рабочего места
- Какие переходы доступны текущему пользователю
- Какие поля обязательны для каждого перехода

#### 4.5.5 Визуализация конвейера

Система автоматически строит визуальную схему конвейера на основе YAML:

```
  ┌───────┐     ┌───────────────┐     ┌────────────┐     ┌────────────┐
  │  New  │────→│ InTranslation │────→│ Translated │────→│ InAnalysis │────→ Processed
  └───────┘     └───────────────┘     └────────────┘     └────────────┘
      │              │                                        │
      │              └──── release ───→ New                   └──── release ───→ Translated
      │
      └──── skip ───→ Skipped
```

Администратор видит эту схему в админке при редактировании конвейера для наглядной проверки корректности.

#### 4.5.6 Валидация YAML-конвейера

При загрузке/сохранении конвейера система проверяет:

| Правило | Описание |
|---|---|
| Ровно один `initial` | Должен быть один начальный статус |
| Хотя бы один `terminal` | Должен быть хотя бы один конечный статус |
| Достижимость | Все статусы достижимы из начального |
| Нет тупиков | Из каждого не-терминального статуса есть хотя бы один переход |
| Уникальность кодов | Коды статусов и действий уникальны в пределах конвейера |
| Корректность ролей | Все `required_role` существуют в справочнике ролей |
| Корректность полей | Все `requires_fields` и `update` ссылаются на реальные поля сущности |
| Нет циклов блокировок | Невозможна ситуация, когда документ навечно застревает в цикле |

#### 4.5.7 Движок конвейера (Pipeline Engine)

```
┌─────────────────────────────────────────────────────┐
│                  PipelineEngine                      │
│                                                     │
│  1. Загрузка YAML → PipelineDefinition              │
│  2. Валидация (правила из 4.5.6)                    │
│  3. Хранение в памяти (кэш) + в БД (workspaces)     │
│                                                     │
│  При запросе перехода:                               │
│  ┌───────────────────────────────────────────┐      │
│  │ a) Проверить текущий статус сущности      │      │
│  │ b) Найти transition (from → to)           │      │
│  │ c) Проверить роль пользователя            │      │
│  │ d) Проверить блокировку (only_assigned)   │      │
│  │ e) Проверить обязательные поля            │      │
│  │ f) Проверить conditions/filters           │      │
│  │ g) Создать снимок версии (если указано)   │      │
│  │ h) Выполнить update полей                 │      │
│  │ i) Создать дочернюю сущность (если указ.) │      │
│  │ j) Записать аудит-лог                     │      │
│  │ k) Отправить уведомление (SignalR)        │      │
│  └───────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────┘
```

#### 4.5.8 Хранение конвейеров

```sql
CREATE TABLE pipelines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,      -- Material / Document
    config_yaml TEXT NOT NULL,
    version INT NOT NULL DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Снимки версий документов (при смене статуса)
CREATE TABLE document_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES documents(id) NOT NULL,
    version_number INT NOT NULL,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    status VARCHAR(50) NOT NULL,
    transition_action VARCHAR(100),         -- какое действие вызвало снимок
    created_by UUID REFERENCES users(id) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_doc_versions ON document_versions(document_id, version_number);
```

---

## 5. Архитектура системы

### 5.1 Общая схема (Clean Architecture)

```
┌──────────────────────────────────────────────────────────┐
│                     Presentation                         │
│          (Minimal APIs / Endpoints / SignalR)             │
├──────────────────────────────────────────────────────────┤
│                     Application                          │
│         (Use Cases / Commands / Queries / DTOs)           │
│              MediatR (CQRS pattern)                      │
├──────────────────────────────────────────────────────────┤
│                       Domain                             │
│     (Entities, Value Objects, Domain Events,             │
│      Aggregates, Interfaces, Specifications)             │
├──────────────────────────────────────────────────────────┤
│                   Infrastructure                         │
│  (EF Core + PostgreSQL, File Storage, YAML Parser,       │
│   JWT Auth, Logging)                                     │
└──────────────────────────────────────────────────────────┘
```

### 5.2 Структура проектов (Solution)

```
NewsFlow.sln
│
├── src/
│   ├── NewsFlow.Domain/                 — Ядро: сущности, VO, доменные события, интерфейсы
│   ├── NewsFlow.Application/            — Use Cases, CQRS (MediatR), валидация (FluentValidation)
│   ├── NewsFlow.Infrastructure/         — EF Core, репозитории, JWT, MinIO (S3), файловое хранилище
│   ├── NewsFlow.Infrastructure.Yaml/    — Парсинг и валидация YAML-конфигураций рабочих мест
│   ├── NewsFlow.Api/                    — ASP.NET Core Minimal APIs, endpoint-группы, middleware
│   └── NewsFlow.Web/                    — Фронтенд (см. раздел 6)
│
├── config/
│   └── workspaces/                      — YAML-файлы рабочих мест
│       ├── translator.yaml
│       ├── analyst.yaml
│       ├── reviewer.yaml
│       ├── registrar.yaml
│       ├── controller.yaml
│       └── evaluator.yaml
│
├── tests/
│   ├── NewsFlow.Domain.Tests/
│   ├── NewsFlow.Application.Tests/
│   ├── NewsFlow.Infrastructure.Tests/
│   └── NewsFlow.Api.Tests/
│
└── docs/
```

### 5.3 Технологический стек

| Компонент | Технология | Обоснование |
|---|---|---|
| Runtime | .NET 8 LTS | Долгосрочная поддержка, производительность |
| Web Framework | ASP.NET Core 8 (Minimal APIs) | Минимальный boilerplate, высокая производительность |
| ORM | EF Core 8 + Npgsql | Нативная поддержка PostgreSQL |
| CQRS/Mediator | MediatR | Разделение команд и запросов |
| Валидация | FluentValidation | Декларативная валидация |
| Маппинг | Mapster или AutoMapper | DTO ↔ Domain маппинг |
| Аутентификация | ASP.NET Identity + JWT | Встроенный механизм |
| YAML-парсинг | YamlDotNet | Стандартная библиотека для .NET |
| Логирование | Serilog + PostgreSQL sink | Структурное логирование в БД |
| Real-time | SignalR | Уведомления о новых материалах в очереди |
| Файловое хранилище | **MinIO** (S3-совместимое) | Локальное развёртывание, S3 API, масштабируемость, веб-консоль |
| Миграции БД | EF Core Migrations + FluentMigrator | Версионирование схемы |
| API-документация | Swagger / OpenAPI | Документирование эндпоинтов |
| Тестирование | xUnit + Moq + Testcontainers | Стандарт для .NET |
| ГИС / Карты | PostGIS + Leaflet.js (JS Interop) | Пространственные запросы + офлайн-карта |
| Тайловый сервер | Martin (Rust) | Один бинарник, MBTiles/PMTiles, без Node |
| Видеоплеер | Video.js (JS Interop) | Бесплатный, стриминг, все форматы |
| Аудиоплеер | WaveSurfer.js (JS Interop) | Волновая форма, loop A-B |
| Транскодирование | FFmpeg (серверный) | Конвертация в web-форматы |
| Генерация DOCX | Open XML SDK / DocX | Без зависимости от MS Office |
| Генерация XLSX | ClosedXML | Таблицы, графики, формулы |
| Графики в отчётах | ScottPlot | Генерация PNG-графиков на сервере |
| Планировщик | Hangfire | Автогенерация отчётов по расписанию |

---

## 6. Фронтенд: рекомендации

### 6.1 Сравнение вариантов

| Вариант | Плюсы | Минусы | Рекомендация |
|---|---|---|---|
| **Blazor Server** | Один язык (C#), простая разработка, нет JS-сборки, тонкий клиент | Зависимость от постоянного SignalR-соединения, масштабируемость ~100 — ОК | **Рекомендуется** |
| **Blazor WASM** | Работает офлайн, снижает нагрузку на сервер | Большой начальный бандл, сложнее отладка | Подходит |
| **React/Vue + API** | Богатая экосистема, гибкость | Два языка (C# + JS/TS), сложнее стек, нужны npm-пакеты в изолированной среде | Избыточно |
| **Razor Pages / MVC** | Простота, серверный рендеринг | Менее интерактивно, не SPA | Для админки — ОК |

### 6.2 Рекомендуемый выбор: Blazor Server

**Почему Blazor Server подходит лучше всего:**

1. **Изолированная среда** — не нужно тащить npm/node экосистему в air-gapped сеть
2. **Единый язык** — вся команда пишет на C#, снижается порог входа
3. **100 пользователей** — Blazor Server прекрасно справляется с такой нагрузкой
4. **Тонкий клиент** — браузер получает только дифф DOM, не нужно загружать тяжёлый JS-бандл
5. **SignalR уже нужен** — для real-time уведомлений о новых материалах в очереди
6. **Богатые UI-компоненты** — MudBlazor или Radzen Blazor (можно закачать NuGet заранее)

**UI-библиотека:** MudBlazor (бесплатная, Material Design, полный набор компонентов)

### 6.3 Альтернативный вариант: гибрид

- **Blazor Server** для рабочих мест (интерактивность, real-time)
- **Razor Pages** для админки (простота, серверный рендеринг)

---

## 7. Схема базы данных (основные таблицы)

```sql
-- Справочники
CREATE TABLE sources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    source_type VARCHAR(50),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE languages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(10) NOT NULL UNIQUE,     -- ISO 639-1
    name VARCHAR(100) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE countries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(10) NOT NULL UNIQUE,     -- ISO 3166-1
    name VARCHAR(200) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    category VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE
);

-- Пользователи
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    full_name VARCHAR(255) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    failed_login_attempts INT DEFAULT 0,
    locked_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_login_at TIMESTAMPTZ
);

CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE user_roles (
    user_id UUID REFERENCES users(id),
    role_id UUID REFERENCES roles(id),
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE user_languages (
    user_id UUID REFERENCES users(id),
    language_id UUID REFERENCES languages(id),
    PRIMARY KEY (user_id, language_id)
);

-- Материалы
CREATE TABLE materials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(500) NOT NULL,
    original_text TEXT,
    translated_text TEXT,
    original_language_id UUID REFERENCES languages(id),
    source_id UUID REFERENCES sources(id),
    country_id UUID REFERENCES countries(id),
    location VARCHAR(255),
    received_at TIMESTAMPTZ NOT NULL,
    event_date TIMESTAMPTZ,
    status VARCHAR(50) NOT NULL DEFAULT 'New',
    priority VARCHAR(20) NOT NULL DEFAULT 'Normal',
    assigned_to UUID REFERENCES users(id),
    assigned_at TIMESTAMPTZ,
    created_by UUID REFERENCES users(id) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE material_tags (
    material_id UUID REFERENCES materials(id),
    tag_id UUID REFERENCES tags(id),
    PRIMARY KEY (material_id, tag_id)
);

CREATE TABLE attachments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    material_id UUID REFERENCES materials(id) NOT NULL,
    file_name VARCHAR(500) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,
    bucket_name VARCHAR(100) NOT NULL DEFAULT 'attachments',
    object_key VARCHAR(1000) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Документы
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    registration_number VARCHAR(100),
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    priority VARCHAR(20) NOT NULL DEFAULT 'Normal',
    assigned_to UUID REFERENCES users(id),
    assigned_at TIMESTAMPTZ,
    created_by UUID REFERENCES users(id) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    evaluation_score INT,
    evaluation_comment TEXT,
    evaluated_by UUID REFERENCES users(id),
    evaluated_at TIMESTAMPTZ
);

CREATE TABLE document_materials (
    document_id UUID REFERENCES documents(id),
    material_id UUID REFERENCES materials(id),
    PRIMARY KEY (document_id, material_id)
);

CREATE TABLE document_comments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES documents(id) NOT NULL,
    author_id UUID REFERENCES users(id) NOT NULL,
    text TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Конфигурация рабочих мест
CREATE TABLE workspaces (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    required_role_id UUID REFERENCES roles(id),
    config_yaml TEXT NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Аудит-лог
CREATE TABLE audit_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    username VARCHAR(100) NOT NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID NOT NULL,
    old_values JSONB,
    new_values JSONB,
    details TEXT,
    workspace_code VARCHAR(100),
    ip_address VARCHAR(45),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Индексы
CREATE INDEX idx_materials_status ON materials(status);
CREATE INDEX idx_materials_assigned ON materials(assigned_to);
CREATE INDEX idx_materials_language ON materials(original_language_id);
CREATE INDEX idx_materials_priority ON materials(priority, received_at);
CREATE INDEX idx_documents_status ON documents(status);
CREATE INDEX idx_documents_assigned ON documents(assigned_to);
CREATE INDEX idx_audit_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_user ON audit_logs(user_id);
CREATE INDEX idx_audit_created ON audit_logs(created_at);
```

---

## 8. API (основные эндпоинты)

### 8.1 Аутентификация

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/auth/login` | Вход (логин + пароль → JWT) |
| POST | `/api/auth/refresh` | Обновление access-токена |
| POST | `/api/auth/logout` | Выход (инвалидация refresh-токена) |
| GET | `/api/auth/me` | Информация о текущем пользователе |

### 8.2 Материалы

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/materials` | Список (с фильтрацией и пагинацией) |
| GET | `/api/materials/{id}` | Детали материала |
| POST | `/api/materials` | Создание (multipart: метаданные + файлы) |
| PUT | `/api/materials/{id}` | Обновление метаданных |
| POST | `/api/materials/{id}/take` | Взять в работу (блокировка) |
| POST | `/api/materials/{id}/release` | Вернуть в очередь |
| PUT | `/api/materials/{id}/translate` | Сохранить перевод |
| POST | `/api/materials/{id}/complete-translation` | Завершить перевод |

### 8.3 Документы

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/documents` | Список |
| GET | `/api/documents/{id}` | Детали |
| POST | `/api/documents` | Создать документ |
| PUT | `/api/documents/{id}` | Обновить |
| POST | `/api/documents/{id}/take` | Взять в работу |
| POST | `/api/documents/{id}/release` | Вернуть в очередь |
| POST | `/api/documents/{id}/transition` | Перевод в следующий статус |
| POST | `/api/documents/{id}/return` | Возврат на предыдущий этап |

### 8.4 Рабочие места

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/workspaces` | Список рабочих мест (для текущего пользователя) |
| GET | `/api/workspaces/{code}` | Конфигурация рабочего места |
| GET | `/api/workspaces/{code}/queue` | Очередь входных данных |
| POST | `/api/workspaces/{code}/action/{actionCode}` | Выполнить действие |

### 8.5 Администрирование

| Метод | Путь | Описание |
|---|---|---|
| CRUD | `/api/admin/users` | Управление пользователями |
| CRUD | `/api/admin/roles` | Управление ролями |
| CRUD | `/api/admin/sources` | Справочник источников |
| CRUD | `/api/admin/languages` | Справочник языков |
| CRUD | `/api/admin/countries` | Справочник стран |
| CRUD | `/api/admin/tags` | Справочник тэгов |
| CRUD | `/api/admin/workspaces` | Управление рабочими местами |
| GET | `/api/admin/audit-logs` | Журнал аудита |

---

## 9. Механизм блокировки (Concurrency)

### 9.1 Принцип

Когда пользователь берёт материал/документ в работу:

1. Устанавливается `assigned_to = userId`, `assigned_at = now`
2. Статус меняется на `InTranslation` / `InAnalysis` / `InReview` и т.д.
3. Запись фильтруется из очереди других пользователей
4. Операция атомарна (optimistic concurrency через `xmin` PostgreSQL или `RowVersion`)

### 9.2 Таймаут блокировки

- Если пользователь не завершает работу в течение N часов (настраивается), материал автоматически возвращается в очередь
- Фоновый сервис `StaleAssignmentCleanupService` (Hosted Service) проверяет зависшие назначения каждые 15 мин

### 9.3 Race Condition Protection

```csharp
// Атомарное взятие в работу
UPDATE materials
SET status = 'InTranslation',
    assigned_to = @userId,
    assigned_at = NOW()
WHERE id = @materialId
  AND status = 'New'
  AND assigned_to IS NULL
RETURNING id;
-- Если RETURNING вернул 0 строк — материал уже занят
```

---

## 10. Логирование и аудит

### 10.1 Что логируется

| Событие | Пример |
|---|---|
| Авторизация | Вход, выход, неудачные попытки |
| Загрузка материала | Кто, когда, какой файл |
| Смена статуса | Материал/документ: старый → новый статус, кем |
| Блокировка/разблокировка | Кто взял/вернул материал |
| Перевод | Кто перевёл, когда |
| Создание документа | Кто создал, из каких материалов |
| Ревью | Одобрение/возврат, замечания |
| Административные действия | CRUD справочников, пользователей, рабочих мест |

### 10.2 Структура лога

```json
{
  "timestamp": "2026-03-01T10:30:00Z",
  "userId": "guid",
  "username": "ivanov",
  "action": "CompleteTranslation",
  "entityType": "Material",
  "entityId": "guid",
  "workspaceCode": "translator",
  "oldValues": { "status": "InTranslation" },
  "newValues": { "status": "Translated", "translatedText": "..." },
  "ipAddress": "192.168.1.100"
}
```

---

## 11. Хранение медиафайлов (MinIO S3)

### 11.1 Стратегия

Файлы хранятся в **MinIO** — S3-совместимом объектном хранилище, развёрнутом локально в изолированной сети.

**Почему MinIO:**
- Полностью совместим с Amazon S3 API — стандартные SDK, миграция при необходимости
- Работает изолированно, без интернета
- Веб-консоль для администрирования
- Поддержка multipart upload для больших файлов
- Версионирование объектов, lifecycle policies
- Один бинарник, простое развёртывание на Windows

### 11.2 Структура бакетов

| Бакет | Назначение |
|---|---|
| `attachments` | Медиафайлы материалов (аудио, видео, текст) |
| `attachments-web` | Транскодированные web-версии (mp4, ogg) |
| `tiles` | Картографические тайлы (MBTiles/PMTiles) |
| `report-templates` | Шаблоны отчётов (DOCX, XLSX) |
| `generated-reports` | Сгенерированные отчёты |
| `geo-imports` | Импортированные геоданные (GeoJSON, KML, Shapefile) |
| `exports` | Экспортированные документы |

**Структура ключей объектов (object key):**

```
attachments/
├── 2026/03/01/{material_id}/original_video.mp4
├── 2026/03/01/{material_id}/audio_record.mp3
└── 2026/03/01/{material_id}/document.pdf

attachments-web/
├── 2026/03/01/{material_id}/original_video.mp4.web.mp4
└── 2026/03/01/{material_id}/audio_record.mp3.web.ogg

generated-reports/
├── 2026/03/weekly_summary_2026-03-01.docx
└── 2026/03/weekly_summary_2026-03-01.xlsx
```

### 11.3 Конфигурация MinIO

```json
{
  "MinIO": {
    "Endpoint": "minio.local:9000",
    "AccessKey": "newsflow-admin",
    "SecretKey": "***",
    "UseSSL": false,
    "DefaultBuckets": [
      "attachments",
      "attachments-web",
      "tiles",
      "report-templates",
      "generated-reports",
      "geo-imports",
      "exports"
    ],
    "MaxFileSize": 524288000,
    "MultipartThreshold": 104857600
  }
}
```

### 11.4 Интеграция с .NET

```
NuGet-пакет: AWSSDK.S3 или Minio (.NET SDK)

Абстракция:
  IFileStorage
  ├── UploadAsync(bucket, key, stream, contentType)
  ├── DownloadAsync(bucket, key) → Stream
  ├── GetPresignedUrlAsync(bucket, key, expiry) → string
  ├── DeleteAsync(bucket, key)
  ├── ExistsAsync(bucket, key) → bool
  └── ListAsync(bucket, prefix) → List<ObjectInfo>

Реализация: MinioFileStorage : IFileStorage
```

### 11.5 Загрузка и отдача файлов

**Загрузка (upload):**
- Файлы до 100 МБ — обычный PUT
- Файлы > 100 МБ — multipart upload (чанки по 10 МБ)
- Прогресс-бар на клиенте (Blazor)
- После загрузки — запись метаданных в таблицу `attachments`

**Отдача (download/stream):**
- Presigned URL (временная подписанная ссылка) для прямого доступа из браузера
- Или проксирование через API с Range Requests для стриминга видео/аудио
- TTL presigned URL — 1 час (настраивается)

### 11.6 Ограничения

- Максимальный размер файла: 500 МБ (настраивается)
- Поддерживаемые форматы:
  - Текст: `.txt`, `.doc`, `.docx`, `.pdf`, `.rtf`
  - Аудио: `.mp3`, `.wav`, `.ogg`, `.flac`
  - Аудио: `.mp4`, `.avi`, `.mkv`, `.mov`
  - Видео: `.mp4`, `.avi`, `.mkv`, `.mov`
- Стриминг видео/аудио через presigned URL или API proxy (Range Requests)
- Lifecycle policy: автоудаление из `generated-reports` старше 90 дней (настраивается)

### 11.7 Развёртывание MinIO

```
Установка:
  - Один бинарник minio.exe (скачивается заранее, переносится на носителе)
  - Запуск как Windows-служба или через NSSM

Пример запуска:
  minio.exe server D:\data\minio --console-address ":9001"

Консоль администратора:
  http://minio.local:9001

Минимальные требования:
  - CPU: 2 ядра
  - RAM: 4 ГБ
  - Диск: зависит от объёма медиа (рекомендуется отдельный том)
```

---

## 12. Картография и ГИС

### 12.1 Назначение

Электронная карта — инструмент аналитика для пространственной привязки событий, визуального анализа географии новостей и подготовки картографических материалов для включения в документы.

### 12.2 Архитектура (офлайн)

Поскольку система изолирована, вся картографическая инфраструктура разворачивается локально:

```
┌──────────────────────────────────────────────────────────────┐
│                       Клиент (Blazor)                        │
│                                                              │
│   Leaflet.js (через JS Interop) + плагины                    │
│   ├── Leaflet.Draw — рисование полигонов/линий               │
│   ├── Leaflet.MarkerCluster — кластеризация точек            │
│   ├── Leaflet.Heat — тепловые карты                          │
│   └── Leaflet.Measure — измерения расстояний/площадей         │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                       Сервер                                 │
│                                                              │
│   ┌──────────────┐    ┌────────────────┐    ┌─────────────┐ │
│   │ Tile Server   │    │ PostGIS        │    │ GeoData API │ │
│   │ (локальный)   │    │ (пространств.  │    │ (REST)      │ │
│   │               │    │  запросы)      │    │             │ │
│   └──────────────┘    └────────────────┘    └─────────────┘ │
│         ▲                                                    │
│   ┌─────┴────────┐                                           │
│   │ MBTiles /     │  ← заранее подготовленные тайлы          │
│   │ PMTiles файлы │    загружаются с носителя                 │
│   └──────────────┘                                           │
└──────────────────────────────────────────────────────────────┘
```

### 12.3 Тайловый сервер (офлайн-карты)

| Компонент | Технология | Обоснование |
|---|---|---|
| Формат тайлов | **MBTiles** или **PMTiles** | Компактный, один файл на регион |
| Тайловый сервер | **TileServer GL** (Node.js) или **Martin** (Rust) | Martin — без Node, один бинарник, рекомендуется |
| Источник данных | OpenStreetMap (экспорт заранее) | Бесплатно, полное покрытие |
| Стиль карты | MapLibre Style Spec | Офлайн-стили без внешних зависимостей |

**Подготовка тайлов (вне изолированной сети):**

1. Скачать данные OpenStreetMap для нужных регионов (через Geofabrik / planet.osm)
2. Сгенерировать тайлы нужных zoom-уровней с помощью `tilemaker` или `OpenMapTiles`
3. Упаковать в MBTiles/PMTiles
4. Перенести на носителе в изолированную сеть
5. Загрузить через админку или разместить в каталоге тайлов

**Хранение тайлов в MinIO (бакет `tiles`):**

```
tiles/
├── world_z0-z6.mbtiles              — весь мир, обзорный масштаб
├── middle_east_z7-z14.mbtiles       — Ближний Восток, детально
├── europe_z7-z14.mbtiles            — Европа, детально
└── custom/
    └── region_x.mbtiles             — пользовательские тайлы
```

### 12.4 Пространственные данные (PostGIS)

PostgreSQL расширяется модулем **PostGIS** для работы с геоданными:

```sql
-- Расширение
CREATE EXTENSION IF NOT EXISTS postgis;

-- Добавление геоколонок к материалам
ALTER TABLE materials ADD COLUMN coordinates GEOMETRY(Point, 4326);
ALTER TABLE materials ADD COLUMN geo_area GEOMETRY(Polygon, 4326);

CREATE INDEX idx_materials_geo ON materials USING GIST(coordinates);
CREATE INDEX idx_materials_area ON materials USING GIST(geo_area);

-- Пользовательские геообъекты (слои аналитика)
CREATE TABLE geo_layers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    layer_type VARCHAR(50) NOT NULL,       -- markers | polygons | lines | heatmap
    style_json JSONB,                       -- цвет, толщина, прозрачность
    is_shared BOOLEAN DEFAULT FALSE,        -- доступен другим пользователям
    created_by UUID REFERENCES users(id) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE geo_features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    layer_id UUID REFERENCES geo_layers(id) ON DELETE CASCADE NOT NULL,
    name VARCHAR(255),
    description TEXT,
    geometry GEOMETRY NOT NULL,             -- Point, LineString, Polygon, MultiPolygon
    properties JSONB,                       -- произвольные атрибуты
    material_id UUID REFERENCES materials(id),   -- привязка к материалу (опционально)
    document_id UUID REFERENCES documents(id),   -- привязка к документу (опционально)
    created_by UUID REFERENCES users(id) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_geo_features_geom ON geo_features USING GIST(geometry);
CREATE INDEX idx_geo_features_layer ON geo_features(layer_id);
```

### 12.5 Функции карты

#### Базовые

| Функция | Описание |
|---|---|
| Просмотр карты | Масштабирование, перемещение, переключение слоёв |
| Маркеры событий | Автоматическое отображение материалов с координатами |
| Кластеризация | При большом количестве точек — группировка в кластеры |
| Popup-информация | Клик на маркер → краткая карточка материала, ссылка на полный текст |
| Фильтр по области | Выделение прямоугольника/полигона → показать только материалы из этой области |
| Фильтр по времени | Слайдер временного диапазона → анимация событий на карте |

#### Рисование и аннотации

| Функция | Описание |
|---|---|
| Точки | Установка маркеров вручную |
| Линии | Рисование маршрутов, границ |
| Полигоны | Выделение областей/зон |
| Окружности | Зона радиуса от точки |
| Текстовые метки | Подписи на карте |
| Редактирование | Перемещение, изменение фигур, удаление |

#### Слои

| Функция | Описание |
|---|---|
| Пользовательские слои | Каждый аналитик может создать свои слои с фигурами |
| Общие слои | Администратор/аналитик может расшарить слой для всех |
| Импорт слоёв | Загрузка GeoJSON, KML, KMZ, Shapefile (через админку или рабочее место) |
| Тепловые карты | Heatmap по плотности событий |
| Переключение слоёв | Галочки для включения/отключения каждого слоя |

#### Измерения

| Функция | Описание |
|---|---|
| Расстояние | Измерение по прямой и по ломаной |
| Площадь | Площадь выделенного полигона |
| Радиус | Определение объектов в радиусе от точки |

#### Экспорт

| Функция | Описание |
|---|---|
| Скриншот карты | Сохранение текущего вида как PNG для вставки в документ |
| Экспорт слоя | GeoJSON / KML для передачи в другие системы |
| Печать | Подготовка картографического материала для печати |

### 12.6 Пространственные запросы (API)

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/geo/materials?bbox=...` | Материалы в прямоугольной области |
| GET | `/api/geo/materials?point=...&radius=...` | Материалы в радиусе от точки |
| POST | `/api/geo/materials/within-polygon` | Материалы внутри произвольного полигона |
| GET | `/api/geo/materials/heatmap?bbox=...` | Данные для тепловой карты |
| GET | `/api/geo/materials/cluster?bbox=...&zoom=...` | Кластеры маркеров для текущего зума |
| CRUD | `/api/geo/layers` | Управление слоями |
| CRUD | `/api/geo/layers/{id}/features` | Управление объектами слоя |
| POST | `/api/geo/layers/import` | Импорт GeoJSON/KML/Shapefile |
| GET | `/api/geo/layers/{id}/export?format=geojson` | Экспорт слоя |
| GET | `/api/tiles/{source}/{z}/{x}/{y}.pbf` | Проксирование тайлов (или напрямую с Martin) |

### 12.7 Привязка к рабочим местам

Карта интегрируется в рабочие места через YAML:

```yaml
workspace:
  code: "analyst"
  # ...

  work_layout:
    type: "three-panel"
    left:
      title: "Материал"
      fields:
        - { field: "translated_text", type: "textarea", readonly: true }
        - { field: "attachments", type: "media-player", readonly: true }
    center:
      title: "Документ"
      fields:
        - { field: "document_content", type: "rich-textarea" }
    right:
      title: "Карта"
      fields:
        - field: "map"
          type: "geo-map"
          config:
            center: [55.75, 37.62]       # начальный центр (Москва)
            zoom: 4
            show_material_marker: true    # показать маркер текущего материала
            editable: true               # разрешить рисование
            layers:
              - "events"                  # слой событий
              - "user_custom"             # пользовательские слои
            tools:
              - "draw_marker"
              - "draw_polygon"
              - "draw_line"
              - "measure_distance"
              - "measure_area"
              - "screenshot"
```

### 12.8 Управление тайлами (админка)

| Функция | Описание |
|---|---|
| Список тайлов | Просмотр загруженных MBTiles/PMTiles, регион, zoom-уровни, размер |
| Загрузка | Загрузка нового файла тайлов через UI |
| Удаление | Удаление ненужных тайлов для освобождения места |
| Предпросмотр | Просмотр покрытия загруженного файла на карте |
| Стили | Настройка стилей отображения карты |

---

## 13. Медиаплеер (аудио/видео)

### 13.1 Назначение

Встроенный медиаплеер позволяет переводчикам и аналитикам просматривать видео и прослушивать аудио непосредственно в рабочем месте, без необходимости скачивать файлы и открывать внешними программами.

### 13.2 Технологии

| Компонент | Технология | Обоснование |
|---|---|---|
| Видеоплеер | **Video.js** (через Blazor JS Interop) | Бесплатный, расширяемый, все форматы |
| Аудиоплеер | **WaveSurfer.js** | Визуализация волны, удобно для длинных записей |
| Стриминг | ASP.NET Core Range Requests | Перемотка без полной загрузки файла |
| Транскодирование | **FFmpeg** (опционально, на сервере) | Конвертация в web-совместимые форматы |

### 13.3 Функции видеоплеера

| Функция | Описание |
|---|---|
| Воспроизведение | Play / Pause / Stop |
| Перемотка | Временная шкала, перемотка вперёд/назад на N сек |
| Скорость | 0.5x, 0.75x, 1x, 1.25x, 1.5x, 2x |
| Полный экран | Разворот на весь экран |
| Громкость | Регулировка + мут |
| Субтитры | Отображение переведённого текста в виде субтитров (если размечены таймкоды) |
| Скриншот кадра | Сохранение текущего кадра как изображение для вставки в документ |
| Закладки | Отметки на таймлайне (переводчик может пометить «важный момент на 2:35») |
| Форматы | MP4 (H.264), WebM (VP9), AVI*, MKV* (*через серверное транскодирование) |

### 13.4 Функции аудиоплеера

| Функция | Описание |
|---|---|
| Воспроизведение | Play / Pause / Stop |
| Волновая форма | Визуализация аудиодорожки (waveform) |
| Перемотка | Клик по волне, кнопки ±5/±15 сек |
| Скорость | 0.5x–2x |
| Зацикливание | Повторение выделенного фрагмента (loop A-B) |
| Закладки | Отметки на таймлайне с комментариями |
| Форматы | MP3, WAV, OGG, FLAC, AAC |

### 13.5 Интеграция с рабочим местом

Медиаплеер встраивается в layout рабочего места:

```yaml
workspace:
  code: "translator"
  # ...

  work_layout:
    type: "split-panel"
    left:
      title: "Оригинал"
      fields:
        - { field: "original_text", type: "textarea", readonly: true }
        - field: "attachments"
          type: "media-player"
          config:
            position: "bottom"             # bottom | right | floating
            auto_play: false
            default_speed: 1.0
            enable_bookmarks: true
            enable_loop: true              # зацикливание фрагмента
            enable_screenshot: true        # скриншот кадра видео
            enable_subtitles: true
            keyboard_shortcuts:
              play_pause: "Space"
              skip_forward: "ArrowRight"   # +5 сек
              skip_backward: "ArrowLeft"   # -5 сек
              speed_up: "Shift+ArrowRight"
              speed_down: "Shift+ArrowLeft"
    right:
      title: "Перевод"
      fields:
        - { field: "translated_text", type: "textarea", required: true }
```

### 13.6 Серверная часть

```
API Endpoint: GET /api/materials/{id}/attachments/{attachmentId}/stream

Заголовки:
  Accept-Ranges: bytes
  Content-Range: bytes 0-1048575/52428800
  Content-Type: video/mp4

Поведение:
  - Поддержка Range Requests для перемотки без полной загрузки
  - Буферизация: отдаёт файл чанками (1 МБ)
  - Авторизация: проверка JWT + доступ к материалу
```

### 13.7 Транскодирование (опционально)

Для форматов, не поддерживаемых браузером нативно (AVI, MKV, FLAC):

```
Hosted Service: MediaTranscodingService
  - При загрузке материала проверяет формат
  - Если формат не web-совместимый → ставит задачу в очередь
  - FFmpeg конвертирует: AVI/MKV → MP4 (H.264), FLAC → OGG
  - Оригинал сохраняется, web-версия создаётся рядом
  - Статус конвертации отображается в UI (прогресс-бар)
```

Хранение в MinIO:

```
Бакет: attachments/
  2026/03/01/{material_id}/original_video.avi        — оригинал
  2026/03/01/{material_id}/audio_record.flac          — оригинал

Бакет: attachments-web/
  2026/03/01/{material_id}/original_video.avi.web.mp4 — web-версия
  2026/03/01/{material_id}/audio_record.flac.web.ogg  — web-версия
```

---

## 14. Система отчётов

### 14.1 Назначение

Подсистема формирования статистических и аналитических отчётов. Отчёты генерируются по запросу пользователя или по расписанию и выгружаются в форматах Word (DOCX), Excel (XLSX) и текстовом (TXT/CSV).

### 14.2 Технологии

| Компонент | Технология | Обоснование |
|---|---|---|
| Генерация DOCX | **TemplateEngine + Open XML SDK** или **DocX** (ClosedXML.Report) | Без зависимости от MS Office, шаблонный подход |
| Генерация XLSX | **ClosedXML** | Бесплатная, шаблоны, стили, формулы |
| Генерация CSV/TXT | Встроенные средства .NET | Никаких зависимостей |
| Шаблоны отчётов | DOCX/XLSX-файлы с плейсхолдерами | Пользователи могут редактировать шаблоны в Word/Excel |
| Планировщик | **Hangfire** или Hosted Service + Cron-выражения | Генерация отчётов по расписанию |

### 14.3 Типы отчётов

#### Статистические

| Отчёт | Описание | Форматы |
|---|---|---|
| Объём загрузки | Кол-во материалов за период по источникам, странам, языкам | XLSX, CSV |
| Производительность переводчиков | Кол-во переведённых материалов, среднее время перевода, по каждому переводчику | XLSX |
| Производительность аналитиков | Кол-во подготовленных документов, среднее время обработки | XLSX |
| Статусы конвейера | Текущее распределение материалов/документов по статусам | XLSX, TXT |
| Нагрузка по периодам | Динамика поступления и обработки за день/неделю/месяц (графики) | XLSX |
| Просроченные блокировки | Материалы/документы, висящие в обработке дольше нормы | XLSX, CSV |

#### Аналитические

| Отчёт | Описание | Форматы |
|---|---|---|
| Сводка за период | Перечень обработанных документов с аннотациями | DOCX, TXT |
| Отчёт по стране/региону | Все документы, связанные с выбранной страной/регионом | DOCX |
| Оценка документов | Документы с оценками за период, средний балл по аналитикам | XLSX |
| Журнал действий | Полный аудит-лог за период (кто, что, когда) | XLSX, CSV |

#### Пользовательские (конфигурируемые)

Администратор может создавать свои отчёты через YAML + SQL:

```yaml
report:
  code: "weekly_summary"
  name: "Еженедельная сводка"
  description: "Статистика за прошедшую неделю"
  output_formats: ["docx", "xlsx"]

  # Шаблон Word
  docx_template: "templates/weekly_summary.docx"

  # Параметры, запрашиваемые у пользователя
  parameters:
    - name: "date_from"
      type: "date"
      label: "Дата начала"
      default: "$week_start"
    - name: "date_to"
      type: "date"
      label: "Дата окончания"
      default: "$today"
    - name: "country_id"
      type: "reference"
      entity: "Country"
      label: "Страна"
      required: false

  # Источники данных (SQL-запросы)
  data_sources:
    summary:
      query: |
        SELECT
          COUNT(*) FILTER (WHERE status = 'New') AS new_count,
          COUNT(*) FILTER (WHERE status = 'Translated') AS translated_count,
          COUNT(*) FILTER (WHERE status = 'Processed') AS processed_count
        FROM materials
        WHERE received_at BETWEEN @date_from AND @date_to
          AND (@country_id IS NULL OR country_id = @country_id)

    by_source:
      query: |
        SELECT s.name AS source_name, COUNT(*) AS material_count
        FROM materials m
        JOIN sources s ON s.id = m.source_id
        WHERE m.received_at BETWEEN @date_from AND @date_to
        GROUP BY s.name
        ORDER BY material_count DESC

    documents_list:
      query: |
        SELECT d.registration_number, d.title, d.status,
               u.full_name AS author, d.created_at
        FROM documents d
        JOIN users u ON u.id = d.created_by
        WHERE d.created_at BETWEEN @date_from AND @date_to
        ORDER BY d.created_at DESC

  # Маппинг данных в шаблон Word
  docx_mapping:
    placeholders:
      "{{period}}": "$date_from — $date_to"
      "{{generated_at}}": "$now"
      "{{new_count}}": "summary.new_count"
      "{{translated_count}}": "summary.translated_count"
    tables:
      - placeholder: "{{sources_table}}"
        data_source: "by_source"
        columns: ["source_name", "material_count"]
      - placeholder: "{{documents_table}}"
        data_source: "documents_list"

  # Маппинг данных в Excel
  xlsx_mapping:
    sheets:
      - name: "Сводка"
        data_source: "summary"
      - name: "По источникам"
        data_source: "by_source"
      - name: "Документы"
        data_source: "documents_list"

  # Расписание автогенерации (опционально)
  schedule:
    cron: "0 8 * * MON"                # каждый понедельник в 08:00
    auto_parameters:
      date_from: "$prev_week_start"
      date_to: "$prev_week_end"
    notify_roles: ["Analyst", "Admin"]  # уведомить о готовности
```

### 14.4 Шаблоны отчётов

Шаблоны — обычные DOCX/XLSX-файлы с плейсхолдерами:

**Word-шаблон (weekly_summary.docx):**
- Создаётся в обычном MS Word
- Плейсхолдеры вида `{{period}}`, `{{new_count}}`
- Таблицы с маркером `{{sources_table}}` — заполняются строками из SQL-запроса
- Можно использовать стили, колонтитулы, нумерацию — всё сохраняется

**Excel-шаблон:**
- Заголовки столбцов, формулы, условное форматирование
- Данные вставляются начиная с указанной строки
- Поддержка нескольких листов

**Хранение в MinIO:**

```
Бакет: report-templates/
  weekly_summary.docx
  monthly_stats.xlsx
  audit_log.xlsx
  custom/...

Бакет: generated-reports/
  2026/03/weekly_summary_2026-03-01.docx
  2026/03/weekly_summary_2026-03-01.xlsx
```

### 14.5 API отчётов

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/reports` | Список доступных отчётов |
| GET | `/api/reports/{code}/parameters` | Параметры отчёта (для формы) |
| POST | `/api/reports/{code}/generate` | Генерация отчёта (body: параметры) |
| GET | `/api/reports/{code}/download/{id}` | Скачивание сгенерированного отчёта |
| GET | `/api/reports/generated` | История сгенерированных отчётов |
| DELETE | `/api/reports/generated/{id}` | Удаление старого отчёта |
| CRUD | `/api/admin/reports` | Управление шаблонами отчётов (админ) |
| POST | `/api/admin/reports/{code}/template` | Загрузка шаблона DOCX/XLSX |

### 14.6 Хранение в БД

```sql
CREATE TABLE report_definitions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    config_yaml TEXT NOT NULL,
    output_formats VARCHAR(50)[] NOT NULL,    -- {docx, xlsx, csv, txt}
    available_to_roles VARCHAR(50)[],          -- какие роли видят отчёт
    schedule_cron VARCHAR(100),                -- cron-выражение (или NULL)
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE generated_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    report_code VARCHAR(100) NOT NULL,
    parameters JSONB,                          -- с какими параметрами сгенерирован
    output_format VARCHAR(20) NOT NULL,
    file_path VARCHAR(1000) NOT NULL,
    file_size BIGINT,
    generated_by UUID REFERENCES users(id),    -- NULL если по расписанию
    generated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_gen_reports_code ON generated_reports(report_code, generated_at DESC);
```

### 14.7 Диаграммы и графики

Отчёты могут содержать визуализации — диаграммы, графики и таблицы.

**В Excel (XLSX):**
- ClosedXML.Report + ClosedXML.Extensions поддерживают встраивание графиков (Chart) в Excel
- Типы: столбчатые, линейные, круговые, комбинированные
- Графики строятся на основе данных из SQL-запросов отчёта
- Автоматическое обновление при перегенерации

**В Word (DOCX):**
- Графики генерируются как PNG-изображения на сервере и вставляются в шаблон
- Библиотека: **ScottPlot** (бесплатная, .NET, без GUI-зависимостей) или **OxyPlot**
- Типы визуализаций:

| Тип | Применение |
|---|---|
| Столбчатая диаграмма | Количество материалов по источникам, странам |
| Линейный график | Динамика поступления по дням/неделям |
| Круговая диаграмма | Распределение по статусам, языкам |
| Стековая диаграмма | Разбивка по категориям с накоплением |
| Таблица | Детализированные данные |
| Тепловая матрица | Нагрузка по дням недели и часам |

**Описание визуализации в YAML:**

```yaml
report:
  code: "monthly_stats"
  # ...
  data_sources:
    daily_volume:
      query: |
        SELECT DATE(received_at) AS day, COUNT(*) AS cnt
        FROM materials
        WHERE received_at BETWEEN @date_from AND @date_to
        GROUP BY DATE(received_at) ORDER BY day

    by_country:
      query: |
        SELECT c.name, COUNT(*) AS cnt
        FROM materials m JOIN countries c ON c.id = m.country_id
        WHERE m.received_at BETWEEN @date_from AND @date_to
        GROUP BY c.name ORDER BY cnt DESC LIMIT 15

  # Визуализации
  charts:
    - code: "daily_chart"
      type: "line"                          # line | bar | pie | stacked_bar | heatmap
      title: "Динамика поступления материалов"
      data_source: "daily_volume"
      x_field: "day"
      y_field: "cnt"
      x_label: "Дата"
      y_label: "Кол-во"
      width: 800
      height: 400

    - code: "country_pie"
      type: "pie"
      title: "Распределение по странам"
      data_source: "by_country"
      label_field: "name"
      value_field: "cnt"
      width: 600
      height: 400

  # Куда вставлять в Word-шаблон
  docx_mapping:
    images:
      "{{daily_chart}}": "daily_chart"       # плейсхолдер → код графика
      "{{country_pie}}": "country_pie"

  # Куда вставлять в Excel
  xlsx_mapping:
    sheets:
      - name: "Графики"
        charts:
          - code: "daily_chart"
            cell: "A1"
          - code: "country_pie"
            cell: "A25"
      - name: "Детали"
        data_source: "daily_volume"
```

### 14.8 Конструктор отчётов (UI)

Визуальный конструктор позволяет пользователю создавать отчёты без редактирования YAML вручную.

#### Интерфейс конструктора

```
┌──────────────────────────────────────────────────────────────────┐
│  Конструктор отчётов                                    [Сохранить] │
├──────────────┬───────────────────────────────────────────────────┤
│              │  1. ОБЩИЕ НАСТРОЙКИ                               │
│  Шаги:       │  ┌─────────────────────────────────────────────┐  │
│              │  │ Название: [Еженедельная сводка            ] │  │
│  ● Общие     │  │ Код:      [weekly_summary                 ] │  │
│  ○ Параметры │  │ Описание: [Статистика за неделю            ] │  │
│  ○ Данные    │  │ Форматы:  ☑ Word  ☑ Excel  ☐ CSV  ☐ TXT   │  │
│  ○ Графики   │  │ Доступен: ☑ Analyst ☑ Admin ☐ Translator   │  │
│  ○ Шаблон    │  │ Расписание: [Каждый пн в 08:00        ] ▼  │  │
│  ○ Просмотр  │  └─────────────────────────────────────────────┘  │
│              │                                                   │
├──────────────┴───────────────────────────────────────────────────┤
│  [◄ Назад]                                        [Далее ►]     │
└──────────────────────────────────────────────────────────────────┘
```

#### Шаги конструктора

**Шаг 1 — Общие настройки:**
- Название, описание, код отчёта
- Форматы выгрузки (Word, Excel, CSV, TXT)
- Роли, которым доступен отчёт
- Расписание автогенерации (опционально)

**Шаг 2 — Параметры:**
- Визуальное добавление входных параметров (дата, справочник, текст, число)
- Настройка значений по умолчанию
- Пометка обязательных/опциональных

```
┌──────────────────────────────────────────────────────┐
│  ПАРАМЕТРЫ                            [+ Добавить]   │
│                                                      │
│  ┌────────┬──────────┬─────────┬────────┬─────────┐ │
│  │ Имя    │ Тип      │ Метка   │ По умолч│ Обязат. │ │
│  ├────────┼──────────┼─────────┼────────┼─────────┤ │
│  │date_from│ Дата     │Начало   │$week_  │ Да      │ │
│  │date_to │ Дата     │Конец    │$today  │ Да      │ │
│  │country │ Справочн.│Страна   │ —      │ Нет     │ │
│  └────────┴──────────┴─────────┴────────┴─────────┘ │
└──────────────────────────────────────────────────────┘
```

**Шаг 3 — Источники данных:**
- SQL-редактор с подсветкой синтаксиса
- Подсказка доступных таблиц и полей (автокомплит из схемы БД)
- Кнопка «Тест» — выполнить запрос с тестовыми параметрами и показать результат
- Можно добавить несколько источников (каждый — отдельный SQL-запрос)

```
┌──────────────────────────────────────────────────────┐
│  ИСТОЧНИКИ ДАННЫХ                     [+ Добавить]   │
│                                                      │
│  ▼ summary                                           │
│  ┌──────────────────────────────────────────────────┐│
│  │ SELECT COUNT(*) FILTER (WHERE status = 'New')    ││
│  │   AS new_count,                                  ││
│  │ COUNT(*) FILTER (WHERE status = 'Translated')    ││
│  │   AS translated_count                            ││
│  │ FROM materials                                   ││
│  │ WHERE received_at BETWEEN @date_from AND @date_to││
│  └──────────────────────────────────────────────────┘│
│  [▶ Тест]  Результат: new_count=42, translated=38   │
│                                                      │
│  ▶ by_source  (свёрнуто)                             │
│  ▶ documents  (свёрнуто)                             │
└──────────────────────────────────────────────────────┘
```

**Шаг 4 — Графики и визуализации:**
- Визуальный выбор типа графика (иконки)
- Привязка к источнику данных
- Выбор полей для осей X/Y, меток, значений
- Предпросмотр графика на реальных данных

```
┌──────────────────────────────────────────────────────┐
│  ГРАФИКИ                              [+ Добавить]   │
│                                                      │
│  ┌──────────────────────┐  ┌───────────────────────┐ │
│  │ 📊 daily_chart       │  │  Предпросмотр:        │ │
│  │ Тип: Линейный       ▼│  │                       │ │
│  │ Источник: daily_vol ▼│  │  ╭─────╮              │ │
│  │ Ось X: day           │  │  │ ╱  ╲ ╱╲            │ │
│  │ Ось Y: cnt           │  │  │╱    ╲╱  ╲          │ │
│  │ Размер: 800x400      │  │  ╰──────────╯         │ │
│  └──────────────────────┘  └───────────────────────┘ │
│                                                      │
│  ┌──────────────────────┐  ┌───────────────────────┐ │
│  │ 🥧 country_pie       │  │  Предпросмотр:        │ │
│  │ Тип: Круговая       ▼│  │    ╭──────╮           │ │
│  │ Источник: by_coun.  ▼│  │   ╱ 35%  ╲           │ │
│  │ Метка: name          │  │  │  20% 45%│          │ │
│  │ Значение: cnt        │  │   ╲      ╱           │ │
│  └──────────────────────┘  └───────────────────────┘ │
└──────────────────────────────────────────────────────┘
```

**Шаг 5 — Шаблон:**
- Для Word: загрузка DOCX-шаблона + маппинг плейсхолдеров к данным и графикам (drag-and-drop)
- Для Excel: маппинг источников данных на листы, размещение графиков
- Для CSV/TXT: выбор источника данных и разделителя

**Шаг 6 — Предпросмотр и тест:**
- Генерация отчёта с тестовыми параметрами
- Просмотр результата прямо в браузере
- Скачивание тестовой версии
- Кнопка «Сохранить и активировать»

#### Безопасность конструктора

| Мера | Описание |
|---|---|
| SQL-песочница | Запросы выполняются через `READ ONLY`-транзакцию, запрещены INSERT/UPDATE/DELETE |
| Таймаут | Максимальное время выполнения запроса — 30 сек (настраивается) |
| Белый список таблиц | Администратор определяет, какие таблицы/вьюхи доступны в конструкторе |
| Ролевой доступ | Конструктор доступен только ролям Admin и Analyst (настраивается) |
| Валидация SQL | Парсинг запроса перед выполнением, блокировка DDL/DML-операций |

---

## 15. Дашборды и статистика

### 15.1 Назначение

Интерактивные дашборды дают пользователям real-time картину состояния системы: загрузка конвейера, производительность персонала, узкие места. Дашборды обновляются через SignalR без перезагрузки страницы.

### 15.2 Типы дашбордов

#### 15.2.1 Операционный дашборд (главный экран)

Доступен всем авторизованным пользователям. Показывает текущее состояние конвейера.

```
┌──────────────────────────────────────────────────────────────────────┐
│  NewsFlow — Оперативная обстановка              01.03.2026 14:35:12 │
├────────────┬────────────┬─────────────┬────────────┬────────────────┤
│  НОВЫЕ     │ НА ПЕРЕВОДЕ│ ПЕРЕВЕДЕНЫ  │ В АНАЛИЗЕ  │  ОБРАБОТАНЫ   │
│    47      │     12     │     23      │     8      │     1 204     │
│   ▲ +5     │            │   ▲ +3      │            │    за месяц   │
├────────────┴────────────┴─────────────┴────────────┴────────────────┤
│                                                                      │
│  ДОКУМЕНТЫ                                                           │
│  ┌──────┬────────┬──────────┬───────────┬──────────┬──────────────┐ │
│  │Черно-│На ревью│Зарегистр.│На контроле│ Оценены  │  Отменены    │ │
│  │вики  │        │          │           │          │              │ │
│  │  14  │   6    │    3     │     2     │   892    │     18       │ │
│  └──────┴────────┴──────────┴───────────┴──────────┴──────────────┘ │
│                                                                      │
│  ┌─────────────────────────────────┐  ┌────────────────────────────┐ │
│  │ Поступление за 7 дней           │  │ По источникам (топ-5)      │ │
│  │                                 │  │                            │ │
│  │  50│    ╭╮                      │  │ Reuters      ████████ 34  │ │
│  │  40│ ╭──╯╰╮  ╭╮               │  │ AP           ██████   28  │ │
│  │  30│╭╯    ╰──╯╰╮╭╮           │  │ Al Jazeera   █████    21  │ │
│  │  20│╯           ╰╯╰─          │  │ ТАСС         ████     17  │ │
│  │  10│                           │  │ Xinhua       ███      12  │ │
│  │    └────────────────────       │  │                            │ │
│  │    Пн Вт Ср Чт Пт Сб Вс      │  │                            │ │
│  └─────────────────────────────────┘  └────────────────────────────┘ │
│                                                                      │
│  ┌─────────────────────────────────┐  ┌────────────────────────────┐ │
│  │ По странам (карта)              │  │ По языкам                  │ │
│  │                                 │  │                            │ │
│  │   ┌──────────────────┐          │  │ Арабский     ████████ 42% │ │
│  │   │  •  •            │          │  │ Английский   █████    31% │ │
│  │   │    •• •          │          │  │ Фарси        ██       12% │ │
│  │   │      ••          │          │  │ Турецкий     █         8% │ │
│  │   └──────────────────┘          │  │ Прочие       █         7% │ │
│  └─────────────────────────────────┘  └────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

**Виджеты операционного дашборда:**

| Виджет | Тип | Описание |
|---|---|---|
| Счётчики статусов (материалы) | KPI-карточки | Текущее кол-во материалов в каждом статусе, дельта за сутки |
| Счётчики статусов (документы) | KPI-карточки | Аналогично для документов |
| Поступление за N дней | Линейный график | Динамика загрузки материалов |
| Топ источников | Горизонтальная гистограмма | Самые активные источники за период |
| По странам | Мини-карта с точками | Географическое распределение |
| По языкам | Круговая / кольцевая | Распределение по языкам оригинала |
| Срочные материалы | Список | Материалы с приоритетом Urgent, ожидающие обработки |
| Просроченные блокировки | Список | Материалы/документы, зависшие в обработке |

#### 15.2.2 Дашборд руководителя

Доступен ролям Admin и аналогичным управленческим ролям. Акцент на производительности и метриках.

```
┌──────────────────────────────────────────────────────────────────────┐
│  Управление — Статистика за март 2026                                │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ПРОИЗВОДИТЕЛЬНОСТЬ ПЕРЕВОДЧИКОВ                                     │
│  ┌──────────────────┬───────────┬───────────┬──────────┬──────────┐ │
│  │ Переводчик       │ Переведено│ Ср. время │ В работе │ Языки    │ │
│  ├──────────────────┼───────────┼───────────┼──────────┼──────────┤ │
│  │ Иванов А.П.      │    87     │  45 мин   │    2     │ AR, FA   │ │
│  │ Петрова Е.С.     │    72     │  52 мин   │    1     │ EN, TR   │ │
│  │ Сидоров К.М.     │    65     │  38 мин   │    1     │ AR       │ │
│  └──────────────────┴───────────┴───────────┴──────────┴──────────┘ │
│                                                                      │
│  ПРОИЗВОДИТЕЛЬНОСТЬ АНАЛИТИКОВ                                       │
│  ┌──────────────────┬───────────┬───────────┬──────────┬──────────┐ │
│  │ Аналитик         │ Документов│ Ср. время │ Ср. оценка│В работе │ │
│  ├──────────────────┼───────────┼───────────┼──────────┼──────────┤ │
│  │ Козлов Д.В.      │    32     │  2.1 ч    │   4.5    │    1     │ │
│  │ Морозова Л.Н.    │    28     │  1.8 ч    │   4.8    │    2     │ │
│  └──────────────────┴───────────┴───────────┴──────────┴──────────┘ │
│                                                                      │
│  ┌─────────────────────────────────┐  ┌────────────────────────────┐ │
│  │ Пропускная способность конвейера│  │ Среднее время на этапе     │ │
│  │ (материалов/день)               │  │                            │ │
│  │                                 │  │ Перевод     ██████  48мин │ │
│  │  80│         ╭──╮              │  │ Анализ      █████████ 2.1ч│ │
│  │  60│  ╭──────╯  ╰──╮          │  │ Ревью       ████     35мин│ │
│  │  40│──╯             ╰───       │  │ Регистрация █        10мин│ │
│  │  20│                           │  │ Контроль    ███      25мин│ │
│  │    └────────────────────       │  │ Оценка      ██       15мин│ │
│  └─────────────────────────────────┘  └────────────────────────────┘ │
│                                                                      │
│  ┌─────────────────────────────────┐  ┌────────────────────────────┐ │
│  │ Возвраты на доработку           │  │ Узкие места конвейера      │ │
│  │                                 │  │                            │ │
│  │  15│ ╭╮                         │  │ ⚠ Перевод AR: 23 в очереди│ │
│  │  10│╭╯╰╮    ╭╮                 │  │ ⚠ Анализ: 15 в очереди    │ │
│  │   5│╯  ╰────╯╰──              │  │ ✓ Ревью: очередь пуста     │ │
│  │    └────────────────────       │  │ ✓ Регистрация: 2 в очереди │ │
│  │    Пн Вт Ср Чт Пт Сб Вс      │  │                            │ │
│  └─────────────────────────────────┘  └────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

**Виджеты дашборда руководителя:**

| Виджет | Тип | Описание |
|---|---|---|
| Таблица переводчиков | Таблица + спарклайны | Производительность за период: кол-во, ср. время, тренд |
| Таблица аналитиков | Таблица + спарклайны | Аналогично + средняя оценка документов |
| Пропускная способность | Линейный график | Кол-во обработанных материалов/документов в день |
| Среднее время на этапе | Горизонтальная гистограмма | Сколько в среднем занимает каждый этап конвейера |
| Возвраты на доработку | Линейный / столбчатый | Динамика количества возвратов (индикатор качества) |
| Узкие места | Список с индикаторами | Этапы, где накапливается очередь (порог — настраивается) |
| SLA-контроль | KPI-карточки | % материалов, обработанных в пределах нормативного времени |
| Воронка конвейера | Диаграмма воронки | Сколько материалов прошло каждый этап за период |

#### 15.2.3 Персональный дашборд

Доступен каждому пользователю. Показывает личную статистику.

| Виджет | Описание |
|---|---|
| Мои задачи | Материалы/документы, назначенные текущему пользователю |
| Моя статистика за сегодня/неделю/месяц | Сколько обработано, ср. время |
| Моя очередь | Кол-во доступных для взятия элементов (по рабочим местам пользователя) |
| Уведомления | Последние уведомления (возвраты, срочные материалы) |
| Мой тренд | Мини-график производительности за последние 30 дней |

### 15.3 Технологии визуализации

| Компонент | Технология | Обоснование |
|---|---|---|
| Библиотека графиков | **ApexCharts** (через Blazor-Apexcharts) | Бесплатная, 20+ типов графиков, анимации, отзывчивость |
| Real-time обновление | SignalR | Дашборд обновляется push-уведомлениями при изменении данных |
| Мини-карта | Leaflet.js (переиспользование из ГИС) | Компактный виджет с точками событий |
| Layout сетка | CSS Grid / MudBlazor Grid | Адаптивное размещение виджетов |

### 15.4 Конфигурируемые дашборды (YAML)

Администратор может создавать и редактировать дашборды через YAML-конфигурацию.

```yaml
dashboard:
  code: "operational"
  name: "Оперативная обстановка"
  description: "Текущее состояние конвейера обработки"
  available_to_roles: ["Admin", "Analyst", "Translator", "Reviewer"]
  auto_refresh_seconds: 30           # интервал фонового обновления данных

  # Глобальные фильтры (отображаются в шапке дашборда)
  global_filters:
    - name: "period"
      type: "date_range"
      label: "Период"
      default: "last_7_days"          # last_24h | last_7_days | last_30_days | custom
    - name: "country_id"
      type: "reference"
      entity: "Country"
      label: "Страна"
      required: false
    - name: "source_id"
      type: "reference"
      entity: "Source"
      label: "Источник"
      required: false

  # Сетка виджетов (12-колоночная)
  layout:
    rows:
      # Ряд 1: KPI-карточки
      - height: "120px"
        widgets:
          - { ref: "material_status_cards", col_span: 12 }

      # Ряд 2: KPI документов
      - height: "120px"
        widgets:
          - { ref: "document_status_cards", col_span: 12 }

      # Ряд 3: Графики
      - height: "350px"
        widgets:
          - { ref: "intake_trend", col_span: 6 }
          - { ref: "top_sources", col_span: 6 }

      # Ряд 4: Карта + языки
      - height: "350px"
        widgets:
          - { ref: "geo_overview", col_span: 6 }
          - { ref: "language_distribution", col_span: 6 }

      # Ряд 5: Списки
      - height: "300px"
        widgets:
          - { ref: "urgent_materials", col_span: 6 }
          - { ref: "stale_locks", col_span: 6 }

  # Определения виджетов
  widgets:
    material_status_cards:
      type: "kpi_cards"
      title: "Материалы"
      data_source:
        query: |
          SELECT
            COUNT(*) FILTER (WHERE status = 'New') AS new,
            COUNT(*) FILTER (WHERE status = 'InTranslation') AS in_translation,
            COUNT(*) FILTER (WHERE status = 'Translated') AS translated,
            COUNT(*) FILTER (WHERE status = 'InAnalysis') AS in_analysis,
            COUNT(*) FILTER (WHERE received_at >= NOW() - INTERVAL '30 days'
                            AND status = 'Processed') AS processed_month
          FROM materials
          WHERE (@country_id IS NULL OR country_id = @country_id)
            AND (@source_id IS NULL OR source_id = @source_id)
      cards:
        - { field: "new", label: "Новые", color: "#2196F3", icon: "inbox" }
        - { field: "in_translation", label: "На переводе", color: "#FF9800", icon: "translate" }
        - { field: "translated", label: "Переведены", color: "#4CAF50", icon: "check" }
        - { field: "in_analysis", label: "В анализе", color: "#FF9800", icon: "analytics" }
        - { field: "processed_month", label: "Обработаны (мес)", color: "#009688", icon: "done_all" }
      show_delta: true                 # показывать дельту по сравнению с предыдущим периодом

    intake_trend:
      type: "line_chart"
      title: "Поступление материалов"
      data_source:
        query: |
          SELECT DATE(received_at) AS day, COUNT(*) AS count
          FROM materials
          WHERE received_at BETWEEN @period_start AND @period_end
            AND (@country_id IS NULL OR country_id = @country_id)
          GROUP BY DATE(received_at)
          ORDER BY day
      chart:
        x_field: "day"
        y_field: "count"
        x_label: "Дата"
        y_label: "Кол-во"
        color: "#2196F3"
        show_area: true

    top_sources:
      type: "bar_chart"
      title: "Топ источников"
      data_source:
        query: |
          SELECT s.name, COUNT(*) AS count
          FROM materials m
          JOIN sources s ON s.id = m.source_id
          WHERE m.received_at BETWEEN @period_start AND @period_end
          GROUP BY s.name
          ORDER BY count DESC
          LIMIT 10
      chart:
        direction: "horizontal"
        label_field: "name"
        value_field: "count"
        color: "#FF9800"

    language_distribution:
      type: "donut_chart"
      title: "Распределение по языкам"
      data_source:
        query: |
          SELECT l.name, COUNT(*) AS count
          FROM materials m
          JOIN languages l ON l.id = m.original_language_id
          WHERE m.received_at BETWEEN @period_start AND @period_end
          GROUP BY l.name
          ORDER BY count DESC
      chart:
        label_field: "name"
        value_field: "count"

    geo_overview:
      type: "mini_map"
      title: "География событий"
      data_source:
        query: |
          SELECT ST_X(coordinates) AS lng, ST_Y(coordinates) AS lat,
                 title, priority
          FROM materials
          WHERE coordinates IS NOT NULL
            AND received_at BETWEEN @period_start AND @period_end
      map:
        center: [30, 45]
        zoom: 2
        cluster: true
        color_field: "priority"

    urgent_materials:
      type: "table"
      title: "Срочные материалы"
      data_source:
        query: |
          SELECT m.title, s.name AS source, l.name AS language,
                 m.status, m.received_at
          FROM materials m
          JOIN sources s ON s.id = m.source_id
          JOIN languages l ON l.id = m.original_language_id
          WHERE m.priority = 'Urgent'
            AND m.status NOT IN ('Processed', 'Skipped')
          ORDER BY m.received_at ASC
          LIMIT 20
      columns:
        - { field: "title", label: "Заголовок", width: "35%" }
        - { field: "source", label: "Источник", width: "15%" }
        - { field: "language", label: "Язык", width: "10%" }
        - { field: "status", label: "Статус", width: "15%", badge: true }
        - { field: "received_at", label: "Поступил", width: "15%", format: "relative" }
      row_click: "navigate_to_material"

    stale_locks:
      type: "table"
      title: "Просроченные блокировки"
      data_source:
        query: |
          SELECT 'Material' AS entity_type, m.id, m.title,
                 u.full_name AS locked_by, m.assigned_at,
                 EXTRACT(EPOCH FROM (NOW() - m.assigned_at))/3600 AS hours_locked
          FROM materials m
          JOIN users u ON u.id = m.assigned_to
          WHERE m.assigned_to IS NOT NULL
            AND m.assigned_at < NOW() - INTERVAL '4 hours'
          UNION ALL
          SELECT 'Document', d.id, d.title,
                 u.full_name, d.assigned_at,
                 EXTRACT(EPOCH FROM (NOW() - d.assigned_at))/3600
          FROM documents d
          JOIN users u ON u.id = d.assigned_to
          WHERE d.assigned_to IS NOT NULL
            AND d.assigned_at < NOW() - INTERVAL '4 hours'
          ORDER BY hours_locked DESC
      columns:
        - { field: "entity_type", label: "Тип", width: "10%" }
        - { field: "title", label: "Заголовок", width: "35%" }
        - { field: "locked_by", label: "Заблокировал", width: "20%" }
        - { field: "hours_locked", label: "Часов", width: "10%", format: "decimal_1" }
      highlight_rule:
        field: "hours_locked"
        thresholds: [{ value: 8, color: "#F44336" }, { value: 4, color: "#FF9800" }]
```

### 15.5 Типы виджетов

| Тип (`type`) | Описание | Параметры |
|---|---|---|
| `kpi_cards` | Ряд карточек с числовыми значениями и дельтой | cards[], show_delta |
| `line_chart` | Линейный график (с заливкой или без) | x_field, y_field, show_area, multi_series |
| `bar_chart` | Столбчатая/горизонтальная диаграмма | direction, label_field, value_field, stacked |
| `donut_chart` | Кольцевая / круговая диаграмма | label_field, value_field |
| `pie_chart` | Круговая диаграмма | label_field, value_field |
| `stacked_area` | Областная диаграмма с накоплением | x_field, series_field, value_field |
| `heatmap` | Тепловая матрица (день недели × час) | x_field, y_field, value_field |
| `funnel` | Воронка конвейера | stages[], value_field |
| `gauge` | Спидометр / индикатор (% выполнения SLA) | value, min, max, thresholds |
| `sparkline` | Компактный мини-график (для ячеек таблиц) | values_field |
| `table` | Таблица с данными | columns[], row_click, highlight_rule |
| `mini_map` | Компактная карта с точками | center, zoom, cluster |
| `list` | Простой список элементов | items[], icon, badge |

### 15.6 Конструктор дашбордов (UI)

Визуальный drag-and-drop конструктор для создания и настройки дашбордов.

#### Возможности конструктора

| Функция | Описание |
|---|---|
| Drag-and-drop виджетов | Перетаскивание виджетов из палитры на сетку |
| Ресайз | Изменение размеров виджетов (col_span, высота ряда) |
| Настройка виджета | Клик → боковая панель с настройками: источник данных, тип графика, цвета |
| SQL-редактор | Встроенный редактор запросов с автокомплитом и кнопкой «Тест» |
| Предпросмотр | Мгновенный предпросмотр виджета на реальных данных |
| Глобальные фильтры | Настройка фильтров, которые применяются ко всем виджетам |
| Клонирование | Копирование существующего дашборда как основы для нового |
| Импорт/экспорт | Скачивание/загрузка YAML-конфигурации дашборда |

#### Интерфейс

```
┌──────────────────────────────────────────────────────────────────────┐
│  Конструктор дашбордов                   [Предпросмотр] [Сохранить] │
├───────────────┬──────────────────────────────────────────────────────┤
│  Палитра      │  Сетка (12 колонок)                                 │
│               │                                                      │
│  ○ KPI-карт.  │  ┌─────────────────────┬─────────────────────┐      │
│  ○ Линейный   │  │ 📊 intake_trend     │ 📊 top_sources      │      │
│  ○ Столбчатый │  │     (col_span: 6)   │     (col_span: 6)   │      │
│  ○ Круговой   │  │                     │                     │      │
│  ○ Кольцевой  │  └─────────────────────┴─────────────────────┘      │
│  ○ Тепловая   │  ┌───────────────────────────────────────────┐      │
│  ○ Воронка    │  │ 🗺 geo_overview                            │      │
│  ○ Спидометр  │  │     (col_span: 12)                        │      │
│  ○ Таблица    │  │                                           │      │
│  ○ Мини-карта │  └───────────────────────────────────────────┘      │
│  ○ Список     │                                                      │
│               │         ╔═══════════════════════╗                    │
│  ─────────    │         ║  Перетащите виджет    ║                    │
│  Фильтры:    │         ║  сюда                 ║                    │
│  + Добавить   │         ╚═══════════════════════╝                    │
├───────────────┼──────────────────────────────────────────────────────┤
│               │  ⚙ Настройка: intake_trend                          │
│               │  ┌──────────────────────────────────────────────┐   │
│               │  │ Тип: [Линейный график ▼]                     │   │
│               │  │ Заголовок: [Поступление материалов          ] │   │
│               │  │ SQL:                                         │   │
│               │  │ ┌──────────────────────────────────────────┐ │   │
│               │  │ │ SELECT DATE(received_at) AS day,         │ │   │
│               │  │ │        COUNT(*) AS count                 │ │   │
│               │  │ │ FROM materials ...                       │ │   │
│               │  │ └──────────────────────────────────────────┘ │   │
│               │  │ Ось X: [day ▼]  Ось Y: [count ▼]            │   │
│               │  │ Цвет: [#2196F3]  Заливка: [☑]               │   │
│               │  │                          [▶ Тест] [Применить]│   │
│               │  └──────────────────────────────────────────────┘   │
└───────────────┴──────────────────────────────────────────────────────┘
```

### 15.7 API дашбордов

| Метод | Путь | Описание |
|---|---|---|
| GET | `/api/dashboards` | Список дашбордов для текущего пользователя |
| GET | `/api/dashboards/{code}` | Конфигурация дашборда |
| GET | `/api/dashboards/{code}/data` | Данные всех виджетов (с учётом фильтров) |
| GET | `/api/dashboards/{code}/widgets/{widgetRef}/data` | Данные одного виджета |
| CRUD | `/api/admin/dashboards` | Управление дашбордами (админ) |
| POST | `/api/admin/dashboards/{code}/import` | Импорт YAML |
| GET | `/api/admin/dashboards/{code}/export` | Экспорт YAML |

### 15.8 Real-time обновление

```
SignalR Hub: /hubs/dashboard

События:
  MaterialStatusChanged   → пересчёт KPI-карточек материалов
  DocumentStatusChanged   → пересчёт KPI-карточек документов
  NewMaterialLoaded       → обновление графика поступления + счётчиков
  AssignmentChanged       → обновление таблицы блокировок

Поведение:
  - При получении события клиент запрашивает данные только затронутых виджетов
  - Дебаунс 2 сек (если несколько событий подряд — один запрос)
  - Фоновый fallback: полное обновление каждые N секунд (auto_refresh_seconds)
```

### 15.9 Хранение в БД

```sql
CREATE TABLE dashboards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    config_yaml TEXT NOT NULL,
    available_to_roles VARCHAR(50)[],
    is_default BOOLEAN DEFAULT FALSE,       -- дашборд по умолчанию при входе
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Пользовательские настройки дашборда (какой дашборд выбран по умолчанию)
CREATE TABLE user_dashboard_preferences (
    user_id UUID REFERENCES users(id) PRIMARY KEY,
    default_dashboard_code VARCHAR(100),
    settings JSONB                          -- фильтры по умолчанию, свёрнутые виджеты и т.д.
);
```

---

## 16. Рассылка документов и оценки

### 16.1 Назначение

Готовый документ рассылается нескольким адресатам (получателям). Каждый адресат может выставить оценку документу (от 1 до 5). Система собирает оценки и рассчитывает итоговый балл.

### 16.2 Модель данных

```
DocumentDistribution (Рассылка)
├── Id: Guid
├── DocumentId: Guid                  — ссылка на документ
├── RecipientId: Guid                 — адресат (пользователь или внешний адресат)
├── RecipientName: string             — имя адресата (для внешних)
├── SentAt: DateTime                  — когда отправлен
├── SentBy: UserId                    — кто отправил
├── Status: DistributionStatus        — Sent | Received | Evaluated
├── Score: int?                       — оценка от 1 до 5 (NULL пока не оценён)
├── Comment: string?                  — комментарий к оценке
├── EvaluatedAt: DateTime?            — когда оценён
└── CreatedAt: DateTime

Recipient (Справочник адресатов)
├── Id: Guid
├── Name: string                      — название (подразделение, должность, ФИО)
├── Description: string?
├── IsInternal: bool                  — внутренний пользователь или внешний адресат
├── UserId: Guid?                     — ссылка на пользователя (если внутренний)
├── IsActive: bool
└── CreatedAt: DateTime
```

### 16.3 Схема БД

```sql
CREATE TABLE recipients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_internal BOOLEAN DEFAULT FALSE,
    user_id UUID REFERENCES users(id),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE document_distributions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES documents(id) NOT NULL,
    recipient_id UUID REFERENCES recipients(id) NOT NULL,
    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    sent_by UUID REFERENCES users(id) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Sent',
    score INT CHECK (score >= 1 AND score <= 5),
    comment TEXT,
    evaluated_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(document_id, recipient_id)        -- один документ — одна рассылка на адресата
);

CREATE INDEX idx_distrib_document ON document_distributions(document_id);
CREATE INDEX idx_distrib_recipient ON document_distributions(recipient_id);
CREATE INDEX idx_distrib_status ON document_distributions(status);

-- Средняя оценка документа (представление)
CREATE VIEW document_avg_scores AS
SELECT
    document_id,
    COUNT(*) FILTER (WHERE score IS NOT NULL) AS evaluations_count,
    ROUND(AVG(score) FILTER (WHERE score IS NOT NULL), 2) AS avg_score,
    COUNT(*) AS total_recipients
FROM document_distributions
GROUP BY document_id;
```

### 16.4 Workflow рассылки

```
Документ (статус Registered/Controlled/Evaluated)
    │
    ▼
Рассылка (выбор адресатов из справочника)
    │
    ├── Адресат 1: Sent → Received → Evaluated (оценка: 4, комментарий: "...")
    ├── Адресат 2: Sent → Received → Evaluated (оценка: 5)
    └── Адресат 3: Sent (ожидает оценки)
    │
    ▼
Итог: средняя оценка 4.5, оценено 2 из 3
```

### 16.5 API рассылки

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/documents/{id}/distribute` | Разослать документ (body: список recipient_id) |
| GET | `/api/documents/{id}/distributions` | Статус рассылки (кому, когда, оценки) |
| PUT | `/api/distributions/{id}/evaluate` | Выставить оценку (score 1-5, comment) |
| GET | `/api/documents/{id}/score` | Сводка оценок (средний балл, кол-во) |
| CRUD | `/api/admin/recipients` | Управление справочником адресатов |

### 16.6 Интеграция с дашбордами

| Виджет | Описание |
|---|---|
| Средняя оценка документов | За период, по аналитикам, динамика |
| Топ документов по оценке | Лучшие и худшие документы |
| Статус рассылки | Сколько документов ожидает оценки |
| Оценки по адресатам | Кто из адресатов выставляет оценки, а кто игнорирует |

---

## 17. Безопасность

| Мера | Реализация |
|---|---|
| Пароли | BCrypt / Argon2 хеширование |
| JWT | RS256 подпись, access 30 мин, refresh 8 ч |
| RBAC | Роль → Рабочее место → Действия |
| Rate limiting | ASP.NET Rate Limiting middleware |
| Валидация ввода | FluentValidation на каждом запросе |
| CORS | Только разрешённые origin (или отключён в изоляции) |
| Аудит | Все действия логируются в БД |
| SQL Injection | Параметризованные запросы через EF Core |
| XSS | Content Security Policy, HTML-экранирование |
| Блокировка учёток | После 5 неудачных попыток на 30 мин |

---

## 18. Требования к развёртыванию

### 18.1 Минимальные системные требования (сервер)

| Ресурс | Значение |
|---|---|
| CPU | 8 ядер |
| RAM | 32 ГБ |
| Диск (система) | 100 ГБ SSD |
| Диск (MinIO) | 1+ ТБ (отдельный том, зависит от объёма медиа) |
| ОС | Windows Server 2022 |
| PostgreSQL | 16+ с PostGIS |
| MinIO | latest (S3 хранилище) |
| Martin | latest (тайловый сервер) |
| FFmpeg | latest (транскодирование) |
| Traefik | v3+ (reverse proxy) |
| .NET Runtime | 8.0+ |

### 18.2 Развёртывание

- **Windows Server 2022** + Traefik (reverse proxy) + PostgreSQL (Windows-служба) + MinIO (Windows-служба) + Martin (Windows-служба)
- Публикация ASP.NET Core как self-hosted (Kestrel) за Traefik
- MinIO: `minio.exe server D:\data\minio --console-address ":9001"` (запуск через NSSM как служба)
- Martin: `martin.exe --config martin.yaml` (тайловый сервер, служба)
- Опционально: Docker через WSL2
- Все зависимости (NuGet-пакеты, runtime, PostgreSQL, MinIO, Martin, FFmpeg, Traefik) подготавливаются заранее и переносятся в изолированную сеть на носителе

### 18.3 Traefik — reverse proxy и маршрутизация

#### Почему Traefik

| Преимущество | Описание |
|---|---|
| Один бинарник | `traefik.exe` — скачивается заранее, переносится на носителе |
| Конфигурация через файлы | YAML/TOML, без внешних зависимостей |
| Автоматический маршрут | File provider — описание сервисов в YAML |
| Dashboard | Встроенная веб-панель состояния маршрутов |
| Middleware | Сжатие, rate limiting, заголовки безопасности, retry — из коробки |
| WebSocket/SSE | Нативная поддержка — критично для SignalR (Blazor Server) |
| Балансировка | Round-robin, при масштабировании на несколько инстансов API |

#### Архитектура сети

```
                         Клиенты (браузеры)
                                │
                          :443 / :80
                                │
                    ┌───────────┴───────────┐
                    │       Traefik          │
                    │   (reverse proxy)      │
                    │   :80 → redirect :443  │
                    │   dashboard: :8080     │
                    └───┬────┬────┬────┬─────┘
                        │    │    │    │
          ┌─────────────┤    │    │    ├──────────────┐
          │             │    │    │    │              │
          ▼             ▼    │    ▼    ▼              ▼
     ┌─────────┐  ┌─────┴──┐│ ┌──┴──────┐  ┌──────────────┐
     │ Kestrel │  │ MinIO  ││ │ Martin  │  │MinIO Console │
     │ API +   │  │ S3 API ││ │ Tiles   │  │              │
     │ Blazor  │  │ :9000  ││ │ :3000   │  │  :9001       │
     │ :5000   │  │        ││ │         │  │              │
     └─────────┘  └────────┘│ └─────────┘  └──────────────┘
                             │
                    ┌────────┴────────┐
                    │   PostgreSQL    │
                    │   :5432         │
                    │ (только внутр.) │
                    └─────────────────┘
```

#### Статическая конфигурация (`traefik.yaml`)

```yaml
# Traefik — статическая конфигурация
# Файл: C:\traefik\traefik.yaml

global:
  checkNewVersion: false               # изолированная среда, нет интернета
  sendAnonymousUsage: false

api:
  dashboard: true                       # панель мониторинга
  insecure: true                        # доступ к dashboard без TLS (внутренняя сеть)

entryPoints:
  web:
    address: ":80"
    http:
      redirections:
        entryPoint:
          to: websecure
          scheme: https

  websecure:
    address: ":443"
    http:
      tls: {}

  traefik:
    address: ":8080"                    # dashboard

providers:
  file:
    directory: "C:\\traefik\\conf.d"    # динамическая конфигурация
    watch: true                         # автоподхват изменений

tls:
  certificates:
    - certFile: "C:\\traefik\\certs\\newsflow.crt"
      keyFile: "C:\\traefik\\certs\\newsflow.key"
  stores:
    default:
      defaultCertificate:
        certFile: "C:\\traefik\\certs\\newsflow.crt"
        keyFile: "C:\\traefik\\certs\\newsflow.key"

log:
  level: "INFO"
  filePath: "C:\\traefik\\logs\\traefik.log"

accessLog:
  filePath: "C:\\traefik\\logs\\access.log"
  format: "json"
```

#### Динамическая конфигурация — маршруты (`conf.d/routes.yaml`)

```yaml
# Traefik — динамическая конфигурация маршрутов
# Файл: C:\traefik\conf.d\routes.yaml

http:

  # ── Роутеры ──────────────────────────────────────────

  routers:

    # Основное приложение (API + Blazor)
    app:
      rule: "Host(`newsflow.local`)"
      entryPoints:
        - websecure
      service: app
      tls: {}
      middlewares:
        - security-headers
        - compress
        - rate-limit

    # MinIO S3 API
    minio-api:
      rule: "Host(`s3.newsflow.local`)"
      entryPoints:
        - websecure
      service: minio-api
      tls: {}

    # MinIO Console (админка хранилища)
    minio-console:
      rule: "Host(`minio.newsflow.local`)"
      entryPoints:
        - websecure
      service: minio-console
      tls: {}
      middlewares:
        - admin-ip-whitelist

    # Тайловый сервер Martin
    tiles:
      rule: "Host(`tiles.newsflow.local`)"
      entryPoints:
        - websecure
      service: tiles
      tls: {}
      middlewares:
        - compress
        - tiles-cache

    # Traefik Dashboard
    dashboard:
      rule: "Host(`traefik.newsflow.local`)"
      entryPoints:
        - traefik
      service: api@internal
      middlewares:
        - admin-ip-whitelist

  # ── Сервисы ──────────────────────────────────────────

  services:

    app:
      loadBalancer:
        servers:
          - url: "http://127.0.0.1:5000"
        healthCheck:
          path: "/health"
          interval: "10s"
          timeout: "3s"
        # При масштабировании — добавить серверы:
        # - url: "http://127.0.0.1:5001"

    minio-api:
      loadBalancer:
        servers:
          - url: "http://127.0.0.1:9000"
        passHostHeader: true

    minio-console:
      loadBalancer:
        servers:
          - url: "http://127.0.0.1:9001"

    tiles:
      loadBalancer:
        servers:
          - url: "http://127.0.0.1:3000"

  # ── Middleware ────────────────────────────────────────

  middlewares:

    # Заголовки безопасности
    security-headers:
      headers:
        frameDeny: true                         # X-Frame-Options: DENY
        contentTypeNosniff: true                # X-Content-Type-Options: nosniff
        browserXssFilter: true                  # X-XSS-Protection
        referrerPolicy: "strict-origin-when-cross-origin"
        contentSecurityPolicy: >
          default-src 'self';
          script-src 'self' 'unsafe-inline' 'unsafe-eval';
          style-src 'self' 'unsafe-inline';
          img-src 'self' data: blob: https://s3.newsflow.local;
          connect-src 'self' wss://newsflow.local https://s3.newsflow.local https://tiles.newsflow.local;
          media-src 'self' blob: https://s3.newsflow.local;
          font-src 'self';
        customResponseHeaders:
          X-Powered-By: ""                      # скрыть технологию
          Server: ""                            # скрыть сервер

    # Сжатие ответов
    compress:
      compress:
        excludedContentTypes:
          - "video/mp4"
          - "video/webm"
          - "audio/mpeg"
          - "application/octet-stream"

    # Rate limiting
    rate-limit:
      rateLimit:
        average: 100                            # запросов в секунду (средний)
        burst: 200                              # пиковый
        period: "1s"

    # Кеширование тайлов
    tiles-cache:
      headers:
        customResponseHeaders:
          Cache-Control: "public, max-age=86400"  # кеш тайлов на 24 ч

    # Ограничение по IP (для админских панелей)
    admin-ip-whitelist:
      ipAllowList:
        sourceRange:
          - "192.168.1.0/24"                    # подсеть администраторов
          - "127.0.0.1/32"
```

#### Альтернативный вариант — маршрутизация по путям (один домен)

Если нет возможности настроить несколько DNS-записей:

```yaml
http:
  routers:
    app:
      rule: "Host(`newsflow.local`) && !PathPrefix(`/s3`, `/tiles`, `/minio`)"
      service: app
      # ...

    minio-api:
      rule: "Host(`newsflow.local`) && PathPrefix(`/s3`)"
      service: minio-api
      middlewares:
        - strip-s3-prefix
      # ...

    tiles:
      rule: "Host(`newsflow.local`) && PathPrefix(`/tiles`)"
      service: tiles
      middlewares:
        - strip-tiles-prefix
      # ...

    minio-console:
      rule: "Host(`newsflow.local`) && PathPrefix(`/minio`)"
      service: minio-console
      middlewares:
        - strip-minio-prefix
      # ...

  middlewares:
    strip-s3-prefix:
      stripPrefix:
        prefixes:
          - "/s3"
    strip-tiles-prefix:
      stripPrefix:
        prefixes:
          - "/tiles"
    strip-minio-prefix:
      stripPrefix:
        prefixes:
          - "/minio"
```

#### TLS-сертификаты

В изолированной среде используется **самоподписанный сертификат** или сертификат от внутреннего CA:

```bash
# Генерация самоподписанного сертификата (выполняется заранее)
openssl req -x509 -nodes -days 3650 \
  -newkey rsa:2048 \
  -keyout newsflow.key \
  -out newsflow.crt \
  -subj "/CN=newsflow.local" \
  -addext "subjectAltName=DNS:newsflow.local,DNS:s3.newsflow.local,DNS:tiles.newsflow.local,DNS:minio.newsflow.local,DNS:traefik.newsflow.local"
```

Сертификат устанавливается в Trusted Root CA на всех клиентских машинах через GPO.

#### Структура каталогов Traefik

```
C:\traefik\
├── traefik.exe                  — бинарник
├── traefik.yaml                 — статическая конфигурация
├── conf.d\
│   └── routes.yaml              — динамическая конфигурация (маршруты)
├── certs\
│   ├── newsflow.crt             — TLS-сертификат
│   └── newsflow.key             — приватный ключ
└── logs\
    ├── traefik.log              — лог Traefik
    └── access.log               — access-лог (JSON)
```

#### Запуск как Windows-служба

```bash
# Установка через NSSM (Non-Sucking Service Manager)
nssm install Traefik "C:\traefik\traefik.exe" "--configFile=C:\traefik\traefik.yaml"
nssm set Traefik AppDirectory "C:\traefik"
nssm set Traefik DisplayName "Traefik Reverse Proxy"
nssm set Traefik Start SERVICE_AUTO_START
nssm start Traefik
```

#### Мониторинг

| URL | Описание |
|---|---|
| `https://traefik.newsflow.local:8080` | Dashboard — состояние роутеров, сервисов, middleware |
| `https://traefik.newsflow.local:8080/api/http/routers` | API — список роутеров (JSON) |
| `https://traefik.newsflow.local:8080/api/http/services` | API — состояние сервисов и health checks |

#### Сводка портов

| Сервис | Порт | Доступ |
|---|---|---|
| Traefik HTTP | 80 | → redirect на 443 |
| Traefik HTTPS | 443 | Основная точка входа для всех клиентов |
| Traefik Dashboard | 8080 | Только для администраторов (IP whitelist) |
| Kestrel (API + Blazor) | 5000 | Только localhost, за Traefik |
| PostgreSQL | 5432 | Только localhost |
| MinIO S3 API | 9000 | Только localhost, за Traefik |
| MinIO Console | 9001 | Только localhost, за Traefik |
| Martin (tiles) | 3000 | Только localhost, за Traefik |

---

## 19. Открытые вопросы

| # | Вопрос | Варианты | Статус |
|---|---|---|---|
| 1 | Нужна ли поддержка одновременной работы нескольких аналитиков над одним документом (совместное редактирование)? | **Нет** — один редактор, блокировка. Вклад нескольких аналитиков — последовательно через очередь | **Закрыт** |
| 2 | Нужна ли версионность документов (история изменений)? | **Да** — снимок документа создаётся автоматически при каждой смене статуса (не при каждом сохранении) | **Закрыт** |
| 3 | Какие конкретно языки необходимо поддерживать (для справочника)? | **Универсальный справочник ISO 639-1 (~30 языков)**, расширяется через админку. Целевой язык перевода — всегда **русский** | **Закрыт** |
| 4 | Нужна ли интеграция с внешними системами в будущем (даже через файловый обмен)? | **Да** — заложить абстракции `IImportService` / `IExportService`. Сценарии: автоимпорт из сетевой папки, экспорт документов (PDF/DOCX), файловый обмен со смежными системами | **Закрыт** |
| 5 | Требуется ли полнотекстовый поиск по материалам и документам? | **Да — PostgreSQL FTS** с поддержкой русского языка. Без внешних зависимостей | **Закрыт** |
| 6 | Нужна ли система уведомлений (новые материалы в очереди, возврат на доработку)? | **Да — SignalR push**. Инфраструктура уже есть (Blazor Server). Колокольчик + тосты | **Закрыт** |
| 7 | Максимальный ожидаемый объём данных (материалов/документов в месяц)? | **Средний масштаб**: 500–5 000 материалов, 100–1 000 документов, до 500 ГБ медиа в месяц. Предусмотреть архивацию и политику хранения | **Закрыт** |
| 8 | Нужна ли поддержка RTL-языков (арабский, иврит, фарси) в интерфейсе? | **Частично** — `dir="auto"` на текстовых полях для корректного отображения RTL-текстов. Полный RTL-интерфейс не нужен, UI на русском | **Закрыт** |
| 9 | Порядок этапов обработки документа фиксирован или тоже конфигурируется? | **Конфигурируемый через YAML** — порядок этапов, переходы, условия возврата. Возможность создавать разные конвейеры для разных типов документов | **Закрыт** |
| 10 | ОС для сервера — Linux или Windows? | **Windows Server 2022**. Traefik как reverse proxy. Развёртывание без Docker (или Docker через WSL2 опционально) | **Закрыт** |

---

## 20. Дорожная карта (фазы) и статус реализации

> **Актуальная дата:** 2026-03-05
> **Общий статус:** Фазы 1 и 2 реализованы. Система компилируется, проходит 34 юнит-теста, готова к демонстрации.

### Фаза 1 — MVP ✅ РЕАЛИЗОВАНА

| Требование | Статус | Детали реализации |
|---|---|---|
| Аутентификация (JWT) | ✅ Готово | `LoginCommand`, BCrypt, access token 30 мин, refresh 8 ч, блокировка после 5 попыток на 15 мин. HMAC-SHA256 (не RS256 — упрощение). Blazor использует `AuthStateService` вместо JWT (in-process). |
| Загрузка материалов (текст + файлы) | ✅ Готово | `CreateMaterialCommand`, `LocalFileStorage` (диск, не MinIO). API multipart upload. UI — `MaterialCreateDialog.razor`. |
| Рабочее место переводчика | ✅ Готово | `TranslatorWorkspace.razor` — split-panel (оригинал / перевод). Очередь фильтрует по языкам переводчика, исключает русский. Команды: take, save draft, complete, release, reject. |
| Рабочее место аналитика | ✅ Готово | `AnalystWorkspace.razor` — three-panel (материал / документ / справка). Кастомная реализация (не GenericWorkspace). Создание документа из 1+ материалов. Дополнительные действия: return to translation, mark not of interest, mark distorted. |
| Базовый аудит-лог | ✅ Готово | `AuditLog` entity, `GetAuditLogsQuery` с фильтрацией по entity type, user, date range. Blazor: `AuditLog.razor`. |
| Админка: пользователи, роли, справочники | ✅ Готово | CRUD для Users, Sources, Languages, Countries, Tags. Назначение ролей и языков пользователям. Blazor-страницы: `Users.razor`, `Sources.razor`, `Languages.razor`, `Countries.razor`, `Tags.razor`. |

### Фаза 2 — Конвейер документов ✅ РЕАЛИЗОВАНА

| Требование | Статус | Детали реализации |
|---|---|---|
| Рабочие места: ревьюер, регистратор, контролёр, оценщик | ✅ Готово | `GenericWorkspace.razor` — универсальная реализация, рендерит UI из YAML-конфигурации. Поддерживает все 4 роли с динамическими полями действий (comment, registration_number, evaluation_score). |
| Конфигурирование рабочих мест через YAML | ✅ Готово | 2 pipeline YAML (`material_default`, `document_default`) + 6 workspace YAML. `PipelineProvider` / `WorkspaceProvider` с кэшированием. `PipelineValidator` / `WorkspaceValidator` проверяют корректность. Визуальный просмотр: `Pipelines.razor`, `PipelineEditor.razor`, `PipelineStateDiagram.razor`, `WorkspaceEditor.razor`. |
| Рассылка документов адресатам, сбор оценок | ✅ Готово | `DocumentDistribution` + `Recipient` entities. Статусы: Sent, Viewed, Evaluated. Оценка 1-5 с комментарием. API endpoints для рассылки и оценки. |
| Полноценный аудит | ✅ Готово | `AuditLog.razor` с фильтрами. API endpoint `/api/admin/audit-logs`. |
| Уведомления (SignalR) | ⚠️ Частично | `NotificationBell.razor` создан. SignalR hub `/hubs/notifications` сконфигурирован. Полноценная серверная рассылка событий при смене статусов — не реализована. |

### Фаза 3 — ГИС и медиа ⛔ НЕ НАЧАТА

| Требование | Статус | Примечание |
|---|---|---|
| Интеграция PostGIS, тайловый сервер Martin | ⛔ | Нет PostGIS extension, нет поля coordinates в Material |
| Электронная карта (Leaflet) | ⛔ | Нет JS Interop, нет Leaflet |
| Пространственные запросы | ⛔ | Нет geo API endpoints |
| Видеоплеер (Video.js) / Аудиоплеер (WaveSurfer.js) | ⛔ | Нет JS Interop |
| Стриминг медиа (Range Requests) | ⚠️ Частично | API endpoint `/api/materials/{id}/attachments/{aid}/stream` существует, но Range Requests не реализован |
| Транскодирование (FFmpeg) | ⛔ | Нет `MediaTranscodingService` |
| Импорт геоданных | ⛔ | Нет geo tables, нет import API |

### Фаза 4 — Отчёты и аналитика ⛔ НЕ НАЧАТА

| Требование | Статус | Примечание |
|---|---|---|
| Шаблонные отчёты (Word, Excel, CSV) | ⚠️ Заглушка | `DocxExportService` — placeholder (возвращает UTF-8 text). Нет Open XML SDK в packages/ |
| Диаграммы и графики (ScottPlot) | ⛔ | Нет пакета ScottPlot |
| Конструктор отчётов (UI) | ⛔ | |
| Планировщик (Hangfire) | ⛔ | Нет пакета Hangfire |
| Полнотекстовый поиск (PostgreSQL FTS) | ⛔ | |
| Дашборды и статистика | ⛔ | Нет ApexCharts, нет dashboard API |

### Фаза 5 — Расширенные возможности ⛔ НЕ НАЧАТА

| Требование | Статус | Примечание |
|---|---|---|
| Расширенное конфигурирование конвейера | ⚠️ Частично | YAML-конвейеры работают. Нет UI-редактирования YAML (только просмотр). Нет `ExecuteWorkspaceActionCommand` для полного dynamic pipeline engine |
| Импорт/экспорт (файловый обмен) | ⛔ | Нет `IImportService` / `IExportService` |
| Расширенная картография | ⛔ | Зависит от Фазы 3 |
| Архивация и политика хранения | ⛔ | Нет `StaleAssignmentCleanupService`, нет lifecycle policy |

---

## 21. Актуальные метрики проекта (на 2026-03-05)

| Метрика | Значение |
|---------|----------|
| Исходные файлы (src) | ~143 (.cs + .razor) |
| Строк кода (C#) | ~4 700 |
| Строк кода (Razor) | ~1 700 |
| Строк тестов | ~660 |
| Юнит-тестов | 34 (все проходят) |
| Проектов (src) | 6 |
| Проектов (tests) | 4 (2 с тестами, 2 пустых) |
| YAML-конфигураций | 8 (2 pipeline + 6 workspace) |
| NuGet-пакетов (offline) | 172 |
| Доменных сущностей | 15 |
| MediatR Commands/Queries | ~50 |
| Blazor-страниц | 18 |
| API endpoint-групп | 8 |

---

## 22. Расхождения PRD и реализации

В ходе реализации приняты следующие решения, отличающиеся от PRD:

### Архитектурные

| Аспект PRD | Фактическая реализация | Обоснование |
|---|---|---|
| JWT RS256 | HMAC-SHA256 | Упрощение; для изолированной среды достаточно |
| MinIO (S3) | LocalFileStorage (диск) | Заглушка; интерфейс `IFileStorage` позволяет заменить |
| Mapster / AutoMapper | Статические `ToDto()` методы | Меньше зависимостей, проще отладка |
| ASP.NET Identity | Ручная реализация (BCrypt + JWT) | Легковеснее, не требует Identity tables |
| FluentMigrator + EF Migrations | Только EF Core Migrations | Достаточно для текущего масштаба |
| Serilog + PostgreSQL sink | Serilog + Console + File | PostgreSQL sink не добавлен |

### Доменные

| Аспект PRD | Фактическая реализация | Примечание |
|---|---|---|
| MaterialStatus: `Skipped` | Не реализован в enum | Есть в YAML pipeline, нет в C# enum |
| Material: `coordinates`, `geo_area` | Отсутствуют | Будет в Фазе 3 (PostGIS) |
| Material: `Tags` (M:M) | ✅ Реализован | `material_tags` join table |
| Document: `Cancelled` status | ✅ В enum и YAML | Доменный метод Cancel не реализован |
| Workspace entity в БД | Нет | Workspaces хранятся только в YAML (не в `workspaces` таблице) |
| Pipeline entity в БД | Нет | Pipelines хранятся только в YAML (не в `pipelines` таблице) |
| `document_versions` | ✅ Реализован | `DocumentVersion` entity с `CreateVersionSnapshot()` |
| Таймаут блокировок | Не реализован | Нет `StaleAssignmentCleanupService` hosted service |
| Optimistic concurrency (RowVersion/xmin) | Не реализован | Нет concurrency token в EF |
| `report_definitions`, `generated_reports` | Не реализованы | Будет в Фазе 4 |
| `dashboards`, `user_dashboard_preferences` | Не реализованы | Будет в Фазе 4 |
| `geo_layers`, `geo_features` | Не реализованы | Будет в Фазе 3 |

### User Stories

| US | Статус | Примечание |
|---|---|---|
| US-1: Аутентификация | ✅ | Реализовано. Refresh token хранится в User entity (не в отдельной таблице). Блокировка: 5 попыток / 15 мин (PRD: N попыток / 30 мин). |
| US-2: Загрузка материалов | ✅ | Реализовано. Форма `MaterialCreateDialog.razor`. Ограничение размера файла — не настроено (нет валидации 500 МБ). |
| US-3: Перевод | ✅ | Реализовано. Фильтр по языкам переводчика работает. Черновик перевода поддерживается. |
| US-4: Анализ и документы | ✅ | Реализовано. Создание документа из нескольких материалов. |
| US-5: Ревью | ✅ | Реализовано через `GenericWorkspace`. Одобрение/возврат с комментарием. |
| US-6: Регистрация, контроль, оценка | ✅ | Реализовано через `GenericWorkspace`. Все переходы работают. |
| US-7: Администрирование | ✅ | CRUD пользователей, справочников. Конфигурирование workspace через YAML (UI только просмотр, не редактирование). |

---

## 23. Рекомендации по дальнейшей доработке

### Приоритет: Высокий (стабилизация и безопасность)

1. **Optimistic concurrency** — добавить `RowVersion` / `ConcurrencyToken` в Material и Document для защиты от race condition при одновременном взятии в работу.

2. **Таймаут блокировок** — реализовать `StaleAssignmentCleanupService` (IHostedService), который каждые 15 мин проверяет зависшие назначения (>4 часов) и возвращает в очередь.

3. **SignalR уведомления** — завершить серверную рассылку событий при смене статусов (MaterialStatusChanged, DocumentStatusChanged). Инфраструктура уже есть (`NotificationBell.razor`, hub).

4. **FluentValidation** — добавить валидаторы для всех команд (сейчас валидация только в доменных методах через `InvalidOperationException`).

5. **RefreshToken в БД** — вынести refresh token из User entity в отдельную таблицу с TTL и возможностью инвалидации.

6. **Application и API тесты** — проекты созданы, но пусты. Добавить integration tests с in-memory SQLite.

### Приоритет: Средний (функциональность Фазы 3)

7. **MinIO вместо LocalFileStorage** — скачать Minio .NET SDK в packages/, реализовать `MinioFileStorage : IFileStorage`.

8. **Range Requests для стриминга** — реализовать в attachment endpoint для видео/аудио перемотки.

9. **RTL-поддержка** — добавить `dir="auto"` на текстовые поля для арабского/иврита/фарси.

10. **Страница оператора загрузки** — сейчас загрузка только через `MaterialCreateDialog`. Нужна полноценная `/workspace/operator` с очередью загруженных материалов.

11. **Фильтр очереди переводчика по языкам** — уже реализован в `GetTranslatorQueueQuery`, но нужно проверить edge cases (пользователь без языков).

### Приоритет: Низкий (Фазы 4-5)

12. **Система отчётов** — начать с DocxExportService (Open XML SDK), затем ClosedXML для Excel.
13. **Дашборды** — начать с операционного дашборда (KPI-карточки статусов).
14. **ГИС** — начать с PostGIS extension и поля coordinates в Material.
15. **Полнотекстовый поиск** — PostgreSQL FTS с русским языком.

---

*Конец документа*
