namespace ENTcapture2.Core.Services;

public static class AppDataPaths
{
    public const string CompanyFolderName = "YUKI_ENT_clinic";
    public const string ProductFolderName = "ENTcapture2";
    public const string LegacyProductFolderName = "ENTcapture2";

    public static string LocalApplicationDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            CompanyFolderName,
            ProductFolderName);

    public static string LegacyLocalApplicationDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LegacyProductFolderName);

    public static string SettingsFilePath =>
        Path.Combine(LocalApplicationDataDirectory, "settings.json");

    public static string LegacySettingsFilePath =>
        Path.Combine(LegacyLocalApplicationDataDirectory, "settings.json");

    public static string DatabaseFilePath =>
        Path.Combine(LocalApplicationDataDirectory, "entcapture2.db");

    public static string LegacyDatabaseFilePath =>
        Path.Combine(LegacyLocalApplicationDataDirectory, "entcapture2.db");

    public static string LogsDirectory =>
        Path.Combine(LocalApplicationDataDirectory, "logs");
}
