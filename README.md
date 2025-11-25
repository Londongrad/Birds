# Birds

Десктопное WPF‑приложение для учёта птиц: добавление новых записей, фильтрация и поиск, управление жизненным циклом (прилет/отлёт, статус жив/погиб), а также просмотр статистики по видам и периодам.

## Архитектура
- **Слои решения**: Clean Architecture с отдельными проектами Domain (модель и правила), Application (CQRS + MediatR), Infrastructure (EF Core + PostgreSQL), UI (MVVM), App (хостинг и конфигурация), Shared (общие константы).【F:Birds.Domain/Entities/Bird.cs†L6-L86】【F:Birds.Application/DependencyInjection.cs†L7-L25】【F:Birds.Infrastructure/DependencyInjection.cs†L9-L18】【F:Birds.UI/DependencyInjection.cs†L18-L43】【F:Birds.App/App.Host.cs†L16-L52】
- **MVVM + CQRS**: UI использует MVVM (CommunityToolkit.Mvvm) с источниками данных через BirdStore/Managers, а операции оформлены отдельными запросами/командами MediatR (например, GetAllBirdsQuery, Create/Update/DeleteBird).【F:Birds.UI/ViewModels/BirdListViewModel.cs†L15-L119】【F:Birds.Application/Queries/GetAllBirds/GetAllBirdsQuery.cs†L1-L10】【F:Birds.Application/Commands/CreateBird/CreateBirdCommand.cs†L1-L13】
- **Валидация и конвейер**: FluentValidation и pipeline behaviors (ValidationBehavior, LoggingBehavior, ExceptionHandlingBehavior) проверяют и логируют запросы на уровне Application, до обращения к хранилищу.【F:Birds.Application/DependencyInjection.cs†L12-L19】【F:Birds.Application/Behaviors/ValidationBehavior.cs†L1-L60】
- **Данные и доступ**: EF Core DbContext с конфигурацией Bird сущности, репозиторий IBirdRepository, PostgreSQL провайдер. Подключение собирается из appsettings.json и .env, плейсхолдеры ${VAR} заменяются при старте хоста.【F:Birds.Infrastructure/Persistence/BirdDbContext.cs†L1-L14】【F:Birds.Infrastructure/Repositories/BirdRepository.cs†L8-L44】【F:Birds.App/appsettings.json†L2-L14】【F:Birds.App/App.Host.cs†L19-L51】
- **Тестирование**: xUnit + FluentAssertions покрывают доменную модель, команды/обработчики, сервисы UI и инфраструктуру (SQLite‑фикстура для репозитория).【F:Birds.Tests/Birds.Tests.csproj†L4-L31】【F:Birds.Tests/Domain/BirdTests.cs†L1-L103】【F:Birds.Tests/Application/Commands/CreateBird/CreateBirdCommandHandlerTests.cs†L1-L140】【F:Birds.Tests/Infrastructure/BirdRepositoryTests.cs†L1-L106】【F:Birds.Tests/UI/Services/BirdManagerTests.cs†L1-L150】

## Основные возможности
- **Создание и редактирование птиц**: форма AddBird сохраняет запись через IBirdManager и сообщает о статусе операции (успех/ошибка). Поддерживается флаг "разовая" (прилет и отлёт в один день).【F:Birds.UI/ViewModels/AddBirdViewModel.cs†L16-L85】
- **Фильтрация, сортировка и поиск**: список птиц поддерживает фильтры по статусу (жив/погиб/отпущен), видам и строковый поиск по имени/датам/описанию; используется ICollectionView с кастомным сравнителем и фильтром.【F:Birds.UI/ViewModels/BirdListViewModel.cs†L15-L118】【F:Birds.UI/ViewModels/BirdListViewModel.cs†L120-L188】
- **Статистика**: расчёт карточек (всего, отпущено, погибло), топы по видам, годам, месяцам, длительность содержания, фильтр по году и сброс фильтра для обзорной аналитики.【F:Birds.UI/ViewModels/BirdStatisticsViewModel.cs†L12-L173】【F:Birds.UI/ViewModels/BirdStatisticsViewModel.cs†L175-L246】
- **Уведомления и навигация**: NotificationService выводит всплывающие окна, NavigationService управляет вьюшками; подписка через MediatR notifications упрощает обработку событий UI.【F:Birds.UI/DependencyInjection.cs†L23-L40】【F:Birds.UI/Services/Notification/NotificationService.cs†L1-L170】
- **Логирование**: Serilog настраивается из appsettings.json, пишет в файлы/Debug и обогащает контекстами; уровни для Microsoft/System понижены до Warning для шумоподавления.【F:Birds.App/appsettings.json†L5-L14】【F:Birds.App/App.Serilog.cs†L1-L55】
- **Экспорт**: JSON‑экспорт через IExportService с определением пути в App слое (IExportPathProvider).【F:Birds.UI/DependencyInjection.cs†L35-L36】【F:Birds.App/App.Host.cs†L32-L36】

## Плюсы
- Чётко разделённые слои и зависимые проекты, DI-конвейер на Generic Host упрощает конфигурацию и тестирование.【F:Birds.App/App.Host.cs†L19-L51】【F:Birds.Application/DependencyInjection.cs†L12-L25】
- Насыщенный UI: фильтры, поиск, аналитика и уведомления строятся на общей коллекции BirdStore без дублирования запросов к БД.【F:Birds.UI/ViewModels/BirdListViewModel.cs†L15-L118】【F:Birds.UI/ViewModels/BirdStatisticsViewModel.cs†L12-L173】
- Сильная защита входных данных: GuardHelper в домене + FluentValidation в Application не допускают неконсистентных записей (даты, статусы, enum).【F:Birds.Domain/Entities/Bird.cs†L21-L78】【F:Birds.Application/Commands/CreateBird/CreateBirdCommandValidator.cs†L1-L69】
- Хорошее покрытие тестами ключевых сценариев (домен, обработчики CQRS, сервисы UI/хранилища, репозиторий).【F:Birds.Tests/Application/Commands/UpdateBird/UpdateBirdCommandHandlerTests.cs†L1-L150】【F:Birds.Tests/UI/Services/BirdInitializerTests.cs†L1-L120】

## Потенциальные улучшения
- Хост PostgreSQL жёстко зашит в конфигурацию, нет docker-compose/скриптов для локального разворачивания БД.【F:Birds.App/appsettings.json†L2-L7】

## Структура репозитория
- `Birds.Domain` — доменные сущности, правила и GuardHelper.
- `Birds.Application` — DTO, команды/запросы, обработчики, валидаторы, pipeline behaviors.
- `Birds.Infrastructure` — EF Core DbContext, конфигурации, репозиторий, миграции и сиды.
- `Birds.UI` — MVVM ViewModels, сервисы навигации/уведомлений/экспорта, конвертеры, XAML‑представления.
- `Birds.App` — точка входа WPF, настройка Host/Serilog, загрузка .env и DI-композиция.
- `Birds.Tests` — модульные/интеграционные тесты для всех слоёв.