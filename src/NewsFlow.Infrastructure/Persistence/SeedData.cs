using Microsoft.EntityFrameworkCore;
using NewsFlow.Domain.Entities;

namespace NewsFlow.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(NewsFlowDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            var roles = new[]
            {
                new Role { Name = "Administrator", Description = "Управление пользователями, справочниками, конфигурацией" },
                new Role { Name = "Operator", Description = "Загрузка и первичная разметка материалов" },
                new Role { Name = "Translator", Description = "Перевод иноязычных материалов" },
                new Role { Name = "Analyst", Description = "Анализ материалов и подготовка документов" },
                new Role { Name = "Reviewer", Description = "Проверка качества документов" },
                new Role { Name = "Registrar", Description = "Регистрация и присвоение номеров документам" },
                new Role { Name = "Controller", Description = "Финальный контроль и оценка документов" },
                new Role { Name = "Evaluator", Description = "Оценка документов" },
            };
            context.Roles.AddRange(roles);
        }

        if (!await context.Languages.AnyAsync())
        {
            var languages = new[]
            {
                new Language { Code = "ru", Name = "Русский" },
                new Language { Code = "en", Name = "Английский" },
                new Language { Code = "ar", Name = "Арабский" },
                new Language { Code = "zh", Name = "Китайский" },
                new Language { Code = "fr", Name = "Французский" },
                new Language { Code = "de", Name = "Немецкий" },
                new Language { Code = "es", Name = "Испанский" },
                new Language { Code = "pt", Name = "Португальский" },
                new Language { Code = "ja", Name = "Японский" },
                new Language { Code = "ko", Name = "Корейский" },
                new Language { Code = "tr", Name = "Турецкий" },
                new Language { Code = "fa", Name = "Персидский" },
                new Language { Code = "hi", Name = "Хинди" },
                new Language { Code = "ur", Name = "Урду" },
                new Language { Code = "it", Name = "Итальянский" },
                new Language { Code = "pl", Name = "Польский" },
                new Language { Code = "uk", Name = "Украинский" },
                new Language { Code = "he", Name = "Иврит" },
                new Language { Code = "sv", Name = "Шведский" },
                new Language { Code = "nl", Name = "Нидерландский" },
                new Language { Code = "cs", Name = "Чешский" },
                new Language { Code = "ro", Name = "Румынский" },
                new Language { Code = "hu", Name = "Венгерский" },
                new Language { Code = "el", Name = "Греческий" },
                new Language { Code = "bg", Name = "Болгарский" },
                new Language { Code = "sr", Name = "Сербский" },
                new Language { Code = "th", Name = "Тайский" },
                new Language { Code = "vi", Name = "Вьетнамский" },
                new Language { Code = "id", Name = "Индонезийский" },
                new Language { Code = "ms", Name = "Малайский" },
            };
            context.Languages.AddRange(languages);
        }

        await context.SaveChangesAsync();

        // Seed admin user
        if (!await context.Users.AnyAsync())
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Administrator");
            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "Администратор системы",
                IsActive = true,
                Roles = [adminRole]
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
