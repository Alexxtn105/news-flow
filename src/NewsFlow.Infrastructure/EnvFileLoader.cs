namespace NewsFlow.Infrastructure;

/// <summary>
/// Загружает переменные окружения из .env файла.
/// Маппит flat-ключи (CONNECTION_STRING) на ключи IConfiguration (ConnectionStrings__DefaultConnection).
/// </summary>
public static class EnvFileLoader
{
    private static readonly Dictionary<string, string> KeyMapping = new()
    {
        ["DATABASE_PROVIDER"] = "DatabaseProvider",
        ["CONNECTION_STRING"] = "ConnectionStrings__DefaultConnection",
        ["JWT_SECRET"] = "Jwt__Secret",
        ["JWT_ISSUER"] = "Jwt__Issuer",
        ["JWT_AUDIENCE"] = "Jwt__Audience",
        ["MINIO_ENDPOINT"] = "MinIO__Endpoint",
        ["MINIO_ACCESS_KEY"] = "MinIO__AccessKey",
        ["MINIO_SECRET_KEY"] = "MinIO__SecretKey",
    };

    /// <summary>
    /// Ищет .env файл начиная от <paramref name="startDirectory"/> вверх по дереву каталогов.
    /// Парсит KEY=VALUE и устанавливает переменные окружения для текущего процесса.
    /// </summary>
    public static void Load(string? startDirectory = null)
    {
        var envPath = FindEnvFile(startDirectory ?? Directory.GetCurrentDirectory());
        if (envPath is null)
            return;

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex <= 0)
                continue;

            var key = trimmed[..eqIndex].Trim();
            var value = trimmed[(eqIndex + 1)..].Trim();

            var envKey = KeyMapping.TryGetValue(key, out var mapped) ? mapped : key;
            Environment.SetEnvironmentVariable(envKey, value);
        }
    }

    private static string? FindEnvFile(string startDir)
    {
        var dir = startDir;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, ".env");
            if (File.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
