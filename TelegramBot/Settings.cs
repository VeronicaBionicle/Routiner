using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Settings
{
    public enum SettingsStatus
    {
        FileNotFound = -2,
        JsonNotParsed = -1,
        Ok = 0
    };

    /* Список настроек для телеграм-бота */
    public class TelegramSettings
    {
        public string Token { get; set; }
    }

    /* Список настроек для сервера базы данных */
    public class DatabaseSettings
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
    }

    /* Настройки приложения */
    public class ApplicationSettings
    {
        public TelegramSettings TelegramSettings { get; set; }
        public DatabaseSettings DatabaseSettings { get; set; }
    }

    public class Settings
    {
        private ApplicationSettings _applicationSettings;
        private SettingsStatus _settingsLoaded;

        public SettingsStatus SettingsLoaded { get { return _settingsLoaded; } }
        public string CreateDatabaseConnectionString()
        {
            var dbSettings = _applicationSettings.DatabaseSettings;
            return $"Server={dbSettings.Server}; Port={dbSettings.Port}; Database={dbSettings.Database}; User ID={dbSettings.UserId}; Password={dbSettings.Password};";
        }
        public string GetBotToken()
        {
            return _applicationSettings.TelegramSettings.Token;
        }
        public Settings(string fileName)
        {
            if (File.Exists(fileName))
            {
                string data = File.ReadAllText(fileName);
                _applicationSettings = JsonSerializer.Deserialize<ApplicationSettings>(data);
                if (_applicationSettings is null || _applicationSettings.DatabaseSettings is null || _applicationSettings.TelegramSettings is null)
                {
                    _settingsLoaded = SettingsStatus.JsonNotParsed;
                }
                else
                {
                    _settingsLoaded = SettingsStatus.Ok;
                }
            }
            else
            {
                _settingsLoaded = SettingsStatus.FileNotFound;
            }
        }
    }
}
