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

Recommended set:

1. Archive screen with search, filters, and archive cards.
2. Add bird screen with the main form.
3. Statistics overview with summary cards.
4. Settings screen with sync and data tools.

Рекомендуемый набор:

1. Архив с поиском, фильтрами и карточками записей.
2. Экран добавления птицы с основной формой.
3. Статистика с обзорными карточками.
4. Настройки с блоками синхронизации и работы с данными.

### 1. Archive / Архив

![Archive screenshot](docs/screenshots/archive.png)

### 2. Add Bird / Добавление птицы

![Add bird screenshot](docs/screenshots/add-bird.png)

### 3. Statistics / Статистика

![Statistics screenshot](docs/screenshots/statistics.png)

### 4. Settings / Настройки

![Settings screenshot](docs/screenshots/settings.png)
