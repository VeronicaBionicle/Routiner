using System;
using System.Runtime;
using System.Text.Json;
using Settings;

namespace SettingsJsonDeserializer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var settings = new Settings.Settings("settings.json");

            if (settings.SettingsLoaded == SettingsStatus.Ok)
            {
                var dbConnectionString = settings.CreateDatabaseConnectionString();
                var token = settings.GetBotToken();

                Console.WriteLine(dbConnectionString);
                Console.WriteLine(token);
            }
            else 
            {
                if (settings.SettingsLoaded == SettingsStatus.FileNotFound)
                {
                    Console.WriteLine("Ошибка загрузки конфигурации: файл не найден");
                } 
                else if (settings.SettingsLoaded == SettingsStatus.JsonNotParsed) 
                {
                    Console.WriteLine("Ошибка загрузки конфигурации: файл неправильного формата");
                }
                return;
            }
        }
    }
}
