# Birds 🐦

Windows desktop application for keeping bird records, working with an archive, tracking lifecycle events, viewing
statistics, and running in an offline-first mode with optional remote synchronization.

## English 🇬🇧

### Overview

Birds is a WPF application for recording bird arrivals, departures, statuses, notes, and long-term archive data.
The app is designed around a local SQLite database as the primary data store. PostgreSQL synchronization is optional
and opt-in, so the application can run safely as a local-only archive unless remote sync is explicitly configured.

### ✨ What the application can do

- Add new bird records with species, dates, status, and description.
- Edit existing records directly from the archive.
- Delete records with a short undo window.
- Search the archive by species, date, or text.
- Filter archive records by lifecycle state.
- Show rich statistics for overview, distribution, and keeping duration.
- Export versioned JSON archives manually through atomic file writes.
- Import current and legacy JSON archives in merge mode or replace mode.
- Remember a custom export file path.
- Open the export folder or the export file directly from settings.
- Run auto-export in the background after data changes.
- Work offline on local SQLite even when the remote database is unavailable.
- Synchronize local changes with a remote PostgreSQL backend when sync is enabled and configured.
- Pull remote changes back into the local database.
- Re-download a full remote snapshot into the local database from settings.
- Show notifications, recent sync activity, remote data loading state, and short-lived operation status indicators.
- Switch application language between English and Russian.
- Switch theme and keep UI preferences between launches.

### 🔄 Offline-first behavior

- SQLite is the main working database for the UI.
- PostgreSQL is used as an optional synchronization backend and is disabled by default.
- Local changes are stored first and synchronized later.
- If the remote backend is unavailable, the application continues working locally.
- When connectivity returns, the sync layer pushes pending changes and pulls remote updates.
- Remote sync is treated as not configured when required connection string environment variables are missing.

### 🧭 Main feature areas

#### Archive

- Fast archive browsing with lazy card creation and virtualization-friendly rendering.
- Inline edit flow for existing bird records.
- Search, filtering, and compact status presentation.
- Short undo for destructive delete operations.

> [!IMPORTANT]
> **⚡ Memory-efficient archive rendering**
>
> The archive keeps lightweight DTO records in memory and materializes full `BirdViewModel` instances only for visible
> cards.
> As the user scrolls, view models are created on demand, which keeps memory usage lower even for very large archives.
>
> This is a deliberate performance tradeoff for large archives, not a blanket rule for every list. The archive stays on
> the current `ListBox` virtualization path with coarse item-based scrolling instead of smooth pixel scrolling. In
> practice, the list advances in larger card steps. That behavior is what keeps on-demand view model materialization
> working. If the scrolling mode is changed, the current memory-saving approach stops working as intended.

#### Statistics

- Overview tab with key counts and summary metrics.
- Distribution tab with species, year, and month analytics.
- Keeping tab for duration-oriented information.
- Year filter for narrowing the analytics scope.

#### Settings

- Language, theme, and date format preferences.
- Import/export tools.
- Auto-export controls.
- Sync controls and recent sync activity.
- Safe destructive actions for local data maintenance.

#### Data safety and diagnostics

- Local SQLite schema upgrades are managed by EF Core migrations.
- Bird records keep local user-facing timestamps and separate UTC sync stamps for remote ordering.
- Bird updates use optimistic concurrency protection to avoid silently overwriting newer local changes.
- Archive exports include format metadata and stable bird species identifiers while keeping display names readable.
- Archive imports validate the full file before database mutation and apply changes transactionally.
- Preferences are saved through a temporary file and corrupted preference files are backed up before defaults are used.
- Local diagnostics are written to log files only; the app does not send telemetry or crash reports externally.
- Development/repository runs write logs under `logs`; installed production runs write under `%LOCALAPPDATA%\Birds\Logs`.
- `BIRDS_LOG_DIR` can override the log directory when needed.

### ⚙️ Configuration notes

- The local SQLite connection string defaults to `%LOCALAPPDATA%\Birds\birds.db`.
- Remote sync is disabled by default through `Database:RemoteSync:Enabled = false` / `REMOTE_SYNC_ENABLED=false`.
- To enable remote PostgreSQL sync, set `REMOTE_SYNC_ENABLED=true` and provide `DB_HOST`, `DB_PORT`, `DB_NAME`,
  `DB_USER`, and `DB_PASSWORD`.
- Unresolved placeholders such as `${DB_HOST}` are detected before connecting, and the UI reports remote sync as
  missing configuration instead of attempting a broken PostgreSQL connection.
- When remote sync is configured, the remote schema initializer prepares required tables, indexes, and schema version
  metadata idempotently.

### 📦 Release packaging

Local release artifacts can be created from the repository root:

```powershell
./deploy/package-release.ps1 -Version v1.0.0
```

The script creates two Windows x64 self-contained archives under `artifacts/release`:

- `Birds-v1.0.0-win-x64-folder.zip` - a portable folder with `Birds.App.exe`, dependencies, and configuration files.
- `Birds-v1.0.0-win-x64-single.zip` - a portable single-file `Birds.App.exe` for quick distribution.

Publishing a Git tag such as `v1.0.0` runs the `Release` GitHub Actions workflow, builds the same archives, stores them
as workflow artifacts, and uploads them to the matching GitHub Release. The single-file build can start without
`appsettings.json`; it falls back to the local SQLite store and can use remote sync through environment variables.

### 🛠 Technology

- .NET 9
- WPF
- CommunityToolkit.Mvvm
- MediatR / CQRS
- FluentValidation
- EF Core
- SQLite
- PostgreSQL / Npgsql
- Polly
- Serilog
- xUnit + FluentAssertions + Moq

### 📁 Repository structure

- `Birds.App` - WPF entry point, host setup, startup flow.
- `Birds.UI` - views, view models, UI services, themes, converters.
- `Birds.Application` - commands, queries, validators, application services.
- `Birds.Domain` - domain entities and business rules.
- `Birds.Infrastructure` - EF Core, repositories, database services, sync persistence.
- `Birds.Shared` - shared constants, localization, cross-layer models.
- `Birds.Tests` - automated tests.

---

## Русский 🇷🇺

### Обзор

Birds — это WPF-приложение для учёта птиц: поступлений, выбытия, статусов, описаний и работы с архивом.
Приложение построено по модели offline-first: локальная SQLite используется как основное хранилище, а PostgreSQL может
работать как удалённый бэкенд синхронизации только после явного включения и настройки.

### ✨ Что приложение умеет

- Добавлять новые записи о птицах с видом, датами, статусом и описанием.
- Редактировать существующие записи прямо из архива.
- Удалять записи с коротким окном для отмены.
- Искать по архиву по виду, дате и тексту.
- Фильтровать архив по состоянию жизненного цикла.
- Показывать статистику по обзору, распределению и длительности содержания.
- Делать ручной экспорт версионированных JSON-архивов через атомарную запись файла.
- Импортировать текущие и старые JSON-архивы в режиме merge или replace.
- Запоминать пользовательский путь для файла экспорта.
- Открывать папку экспорта и сам файл экспорта из настроек.
- Выполнять автоэкспорт после изменений данных.
- Работать локально на SQLite даже при недоступной удалённой базе.
- Синхронизировать локальные изменения с удалённым PostgreSQL, если sync включён и настроен.
- Подтягивать удалённые изменения обратно в локальную базу.
- Повторно скачивать удалённый снимок в локальную базу из настроек.
- Показывать уведомления, недавнюю sync-активность, загрузку remote-данных и краткие индикаторы статуса операций.
- Переключать язык приложения между английским и русским.
- Переключать тему и сохранять пользовательские настройки между запусками.

### 🔄 Как работает offline-first

- Основная рабочая база для UI — SQLite.
- PostgreSQL используется как опциональный удалённый бэкенд синхронизации и по умолчанию выключен.
- Локальные изменения сначала сохраняются локально, а потом синхронизируются.
- Если удалённый бэкенд недоступен, приложение продолжает работать локально.
- Когда соединение возвращается, слой синхронизации отправляет pending-изменения и подтягивает удалённые обновления.
- Если обязательные переменные окружения для remote connection string не заданы, sync считается ненастроенным.

### 🧭 Основные зоны функциональности

#### Архив

- Быстрый просмотр архива с ленивым созданием карточек и дружественным к виртуализации рендерингом.
- Inline-редактирование существующих записей.
- Поиск, фильтрация и компактное отображение статусов.
- Короткий undo для удаления.

> [!IMPORTANT]
> **⚡ Экономный рендеринг архива по памяти**
>
> Архив хранит в памяти лёгкие DTO-записи, а полноценные `BirdViewModel` создаются только для видимых карточек.
> По мере прокрутки view model материализуются на лету, поэтому даже на больших архивах расход памяти остаётся заметно
> ниже.
>
> Это осознанный performance tradeoff для больших архивов, а не универсальное правило для любого списка. Архив
> остаётся на текущем пути виртуализации `ListBox` с пошаговой прокруткой по элементам вместо плавной пиксельной
> прокрутки. На практике список листается более крупными шагами. Именно это и сохраняет материализацию
> `BirdViewModel` на лету. Если менять режим прокрутки, текущая схема экономии памяти перестаёт работать как задумано.

#### Статистика

- Вкладка обзора с ключевыми счётчиками и summary-метриками.
- Вкладка распределения с аналитикой по видам, годам и месяцам.
- Вкладка содержания с метриками по длительности.
- Фильтр по году для сужения выборки.

#### Настройки

- Настройки языка, темы и формата даты.
- Инструменты импорта и экспорта.
- Управление автоэкспортом.
- Управление синхронизацией и просмотр недавней sync-активности.
- Защищённые операции обслуживания локальных данных с обязательным подтверждением.

#### Безопасность данных и диагностика

- Обновления локальной SQLite-схемы выполняются через EF Core migrations.
- Записи птиц сохраняют локальные пользовательские даты и отдельные UTC sync stamps для удалённого порядка изменений.
- Обновления птиц защищены оптимистичной конкуренцией, чтобы устаревшие изменения не затирали более свежие локальные
  данные.
- Экспорт архива включает метаданные формата и стабильные идентификаторы видов, сохраняя отображаемые имена читаемыми.
- Импорт архива валидирует весь файл до изменения базы и применяет изменения транзакционно.
- Preferences сохраняются через временный файл, а повреждённые preference-файлы сначала копируются в backup.
- Локальная диагностика пишется только в файлы логов; приложение не отправляет телеметрию или crash reports наружу.
- При запуске из репозитория логи пишутся в `logs`; в установленном production-приложении — в
  `%LOCALAPPDATA%\Birds\Logs`.
- `BIRDS_LOG_DIR` можно использовать для явного переопределения папки логов.

### ⚙️ Заметки по конфигурации

- Локальная SQLite connection string по умолчанию указывает на `%LOCALAPPDATA%\Birds\birds.db`.
- Remote sync по умолчанию выключен через `Database:RemoteSync:Enabled = false` / `REMOTE_SYNC_ENABLED=false`.
- Чтобы включить удалённую PostgreSQL-синхронизацию, нужно задать `REMOTE_SYNC_ENABLED=true` и переменные `DB_HOST`,
  `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`.
- Неразрешённые placeholders вроде `${DB_HOST}` обнаруживаются до подключения, а UI показывает, что remote sync не
  настроен, вместо попытки подключиться с битой PostgreSQL connection string.
- Если remote sync настроен, remote schema initializer идемпотентно готовит нужные таблицы, индексы и метаданные версии
  схемы.

### 📦 Релизная упаковка

Локальные релизные артефакты можно собрать из корня репозитория:

```powershell
./deploy/package-release.ps1 -Version v1.0.0
```

Скрипт создаёт два self-contained архива для Windows x64 в `artifacts/release`:

- `Birds-v1.0.0-win-x64-folder.zip` - portable-папка с `Birds.App.exe`, зависимостями и конфигурационными файлами.
- `Birds-v1.0.0-win-x64-single.zip` - portable single-file `Birds.App.exe` для быстрой передачи.

Публикация Git tag вроде `v1.0.0` запускает GitHub Actions workflow `Release`, собирает такие же архивы, сохраняет их
как workflow artifacts и загружает в соответствующий GitHub Release. Single-file сборка может стартовать без
`appsettings.json`: она использует локальную SQLite-базу по умолчанию и может подключить remote sync через переменные
окружения.

### 🛠 Технологии

- .NET 9
- WPF
- CommunityToolkit.Mvvm
- MediatR / CQRS
- FluentValidation
- EF Core
- SQLite
- PostgreSQL / Npgsql
- Polly
- Serilog
- xUnit + FluentAssertions + Moq

### 📁 Структура репозитория

- `Birds.App` - точка входа WPF, host setup, стартовый поток приложения.
- `Birds.UI` - представления, view model, UI-сервисы, темы, конвертеры.
- `Birds.Application` - команды, запросы, валидаторы, application-сервисы.
- `Birds.Domain` - доменные сущности и бизнес-правила.
- `Birds.Infrastructure` - EF Core, репозитории, сервисы БД, sync persistence.
- `Birds.Shared` - общие константы, локализация и модели между слоями.
- `Birds.Tests` - автоматические тесты.

---

## Screenshots / Скриншоты 🖼️

### 1. Archive / Архив

![Archive screenshot](https://github.com/user-attachments/assets/6d4d5916-4381-4e2d-aedb-fda114533230)

### 2. Add Bird / Добавление птицы

![Add bird screenshot](https://github.com/user-attachments/assets/26bf47cc-5d50-4d19-88a7-2f41bedbb181)

### 3. Statistics / Статистика

![Statistics screenshot](https://github.com/user-attachments/assets/d20af0e1-07ea-4f2e-95c6-e5ec0bb02d21)

### 4. Settings / Настройки

![Settings screenshot](https://github.com/user-attachments/assets/2b184fe8-85ca-4836-8861-61ffd94e6e9c)

### 5. Notification Center / Центр уведомлений

![Notification center screenshot](https://github.com/user-attachments/assets/4964aadb-ea2b-46cd-9953-608ade6af816)
