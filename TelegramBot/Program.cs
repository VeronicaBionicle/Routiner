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
            try
            {
                var settings = new Settings.Settings("settings.json");

                if (settings.SettingsLoaded == SettingsStatus.Ok)
                {
                    var dbConnectionString = settings.CreateDatabaseConnectionString();
                    var connectionOk = BotUtil.CheckConnection(dbConnectionString);

                    if (connectionOk)
                    {
                        var token = settings.GetBotToken();
                        var botClient = new TelegramBotClient(token);

                        var botOk = botClient.TestApi().Result;

                        if (botOk)
                        {
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

                            Console.WriteLine("Бот стартанул.");
                        }
                        else 
                        {
                            Console.WriteLine($"Ошибка при подключении к боту BotId={botClient.BotId}.");
                        }

                    }
                    else
                    {
                        Console.WriteLine("Ошибка подключения к БД, параметры подключения:");
                        Console.WriteLine(String.Join("\n", dbConnectionString.Split("; ")));
                    }

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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Чтобы не схлопывалось окно
            Console.ReadLine();
        }
    }
}