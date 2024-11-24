using Telegram.Bot;
using Telegram.Bot.Polling;
using Settings;
using System;
using Npgsql;
using System.Data;
using Dapper;
using BotUtilities;
using static Dapper.SqlMapper;
using System.Xml.Linq;
using BankInformation;
using Microsoft.AspNetCore.Components.Routing;
using Telegram.Bot.Types;


namespace TelegramBot
{
    internal class Program
    {
        static async Task Main()
        {
            var settings = new Settings.Settings("settings.json");

            if (settings.SettingsLoaded == SettingsStatus.Ok)
            {
                var dbConnectionString = settings.CreateDatabaseConnectionString();
                var token = settings.GetBotToken();
                var botClient = new TelegramBotClient(token);

                var bot = new Bot(botClient, dbConnectionString);

                // Чтобы при перезапуске бота не зависало состояние у пользователей и выбранных ими банков
                using (IDbConnection db = new NpgsqlConnection(dbConnectionString))
                {
                    await db.QuerySingleOrDefaultAsync<MenuState>("UPDATE routiner.t_users SET state = @State",
                    new { State = MenuState.UserRegistered });

                    await db.QuerySingleOrDefaultAsync<MenuState>("truncate routiner.t_user_bank_choose",
                    new { State = MenuState.UserRegistered });
                }

                botClient.StartReceiving(bot.HandleUpdateAsync, bot.ErrorHandlerAsync);
                Console.ReadLine();
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
