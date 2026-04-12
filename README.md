# Birds 🐦

Windows desktop application for keeping bird records, working with an archive, tracking lifecycle events, viewing statistics, and running in an offline-first mode with optional remote synchronization.

## English 🇬🇧

### Overview

Birds is a WPF application for recording bird arrivals, departures, statuses, notes, and long-term archive data.  
The app is designed around a local SQLite database as the primary data store, with optional PostgreSQL synchronization in the background.

### ✨ What the application can do

- Add new bird records with species, dates, status, and description.
- Edit existing records directly from the archive.
- Delete records with a short undo window.
- Search the archive by species, date, or text.
- Filter archive records by lifecycle state.
- Show rich statistics for overview, distribution, and keeping duration.
- Export data to JSON manually.
- Import data from JSON in merge mode or replace mode.
- Remember a custom export file path.
- Open the export folder or the export file directly from settings.
- Run auto-export in the background after data changes.
- Work offline on local SQLite even when the remote database is unavailable.
- Synchronize local changes with a remote PostgreSQL backend when sync is enabled.
- Pull remote changes back into the local database.
- Re-download a full remote snapshot into the local database from settings.
- Show notifications, recent sync activity, and short-lived operation status indicators.
- Switch application language between English and Russian.
- Switch theme and keep UI preferences between launches.

### 🔄 Offline-first behavior

- SQLite is the main working database for the UI.
- PostgreSQL is used as an optional synchronization backend.
- Local changes are stored first and synchronized later.
- If the remote backend is unavailable, the application continues working locally.
- When connectivity returns, the sync layer pushes pending changes and pulls remote updates.

### 🧭 Main feature areas

#### Archive

- Fast archive browsing with lazy card creation and virtualization-friendly rendering.
- Inline edit flow for existing bird records.
- Search, filtering, and compact status presentation.
- Short undo for destructive delete operations.

> ⚡ **Memory-efficient archive rendering**
>
> The archive keeps lightweight DTO records in memory and materializes full `BirdViewModel` instances only for visible cards.
> As the user scrolls, view models are created on demand, which keeps memory usage lower even for very large archives.

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

### 🛠 Technology

- .NET 9
- WPF
- MVVM
- MediatR / CQRS
- EF Core
- SQLite
- PostgreSQL
- Serilog
- xUnit + FluentAssertions

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
Приложение построено по модели offline-first: локальная SQLite используется как основное хранилище, а PostgreSQL может работать как удалённый бэкенд синхронизации.

### ✨ Что приложение умеет

- Добавлять новые записи о птицах с видом, датами, статусом и описанием.
- Редактировать существующие записи прямо из архива.
- Удалять записи с коротким окном для отмены.
- Искать по архиву по виду, дате и тексту.
- Фильтровать архив по состоянию жизненного цикла.
- Показывать статистику по обзору, распределению и длительности содержания.
- Делать ручной экспорт данных в JSON.
- Импортировать данные из JSON в режиме merge или replace.
- Запоминать пользовательский путь для файла экспорта.
- Открывать папку экспорта и сам файл экспорта из настроек.
- Выполнять автоэкспорт после изменений данных.
- Работать локально на SQLite даже при недоступной удалённой базе.
- Синхронизировать локальные изменения с удалённым PostgreSQL, если sync включён.
- Подтягивать удалённые изменения обратно в локальную базу.
- Повторно скачивать удалённый снимок в локальную базу из настроек.
- Показывать уведомления, недавнюю sync-активность и краткие индикаторы статуса операций.
- Переключать язык приложения между английским и русским.
- Переключать тему и сохранять пользовательские настройки между запусками.

### 🔄 Как работает offline-first

- Основная рабочая база для UI — SQLite.
- PostgreSQL используется как опциональный удалённый бэкенд синхронизации.
- Локальные изменения сначала сохраняются локально, а потом синхронизируются.
- Если удалённый бэкенд недоступен, приложение продолжает работать локально.
- Когда соединение возвращается, слой синхронизации отправляет pending-изменения и подтягивает удалённые обновления.

### 🧭 Основные зоны функциональности

#### Архив

- Быстрый просмотр архива с ленивым созданием карточек и дружественным к виртуализации рендерингом.
- Inline-редактирование существующих записей.
- Поиск, фильтрация и компактное отображение статусов.
- Короткий undo для удаления.

> ⚡ **Экономный рендеринг архива по памяти**
>
> Архив хранит в памяти лёгкие DTO-записи, а полноценные `BirdViewModel` создаются только для видимых карточек.
> По мере прокрутки view model материализуются на лету, поэтому даже на больших архивах расход памяти остаётся заметно ниже.

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
- Безопасные destructive-действия для обслуживания локальных данных.

### 🛠 Технологии

- .NET 9
- WPF
- MVVM
- MediatR / CQRS
- EF Core
- SQLite
- PostgreSQL
- Serilog
- xUnit + FluentAssertions

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
