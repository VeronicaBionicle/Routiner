using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Settings;
using System;
using BankInformation;
using Npgsql;
using System.Data;
using Dapper;
using static TelegramBot.ChooseBankMenu;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    internal class Program
    {
        public static ChooseBankMenu bankChooseMenu;
        public static bool isFoundBank = false;
        public static int bankId;
        public static string dbConnectionString;

        private static KeyboardButton[] GetKeyboardButtons(List<string> list)
        {
            var res = new KeyboardButton[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                res[i] = new KeyboardButton(list[i]);
            }
            return res;
        }



        public static Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cancellationToken)
        {
            var errorMessage = ex.ToString();
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }



        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                var mes = $"{update.Message.Chat.Username} {update.Message.Chat.FirstName} {update.Message.Chat.Id}";
                Console.WriteLine(mes);

                /* Проверим юзера */
                using (IDbConnection db = new NpgsqlConnection(dbConnectionString))
                {
                    var userStatus = await db.QuerySingleOrDefaultAsync<int>("select routiner.user_status(@ChatId, @Username)",
                                        new { ChatId = update.Message.Chat.Id, Username = update.Message.Chat.Username });
                    if (userStatus == 0) 
                    {
                        await botClient.SendMessage(update.Message.Chat.Id, $"В первый раз тебя вижу, { update.Message.Chat.FirstName ?? update.Message.Chat.Username }");
                        
                        var v = new List<string> { "Да", "Нет" };

                        ReplyKeyboardMarkup keyboard = new(
                        new[] {
                            GetKeyboardButtons(v)
                        }
                        )
                        { ResizeKeyboard = true };

                        await botClient.SendMessage(update.Message.Chat.Id,
                               "Хотите зарегистрироваться в боте?",
                               replyMarkup: keyboard);
                    }
                    else
                    {
                        await botClient.SendMessage(update.Message.Chat.Id, $"Дарова, {update.Message.Chat.FirstName ?? update.Message.Chat.Username}");
                    }
                    //await botClient.SendMessage(update.Message.Chat.Id, userStatus.ToString());
                }

                /*
                // Выбор банка
                if (!isFoundBank)
                {
                    isFoundBank = await bankChooseMenu.ProccessChooseBankMenu(update);
                    if (isFoundBank)
                    {
                        bankId = bankChooseMenu.bankId;
                        await botClient.SendMessage(update.Message.Chat.Id, "Выбрали банк с Id " + bankId);
                    }
                }
                // Тут уже знаем Id
                */

            }
            catch (Exception ex)
            {
                await ErrorHandlerAsync(botClient, ex, cancellationToken);
            } 
        }

        static void Main(string[] args)
        {
            var settings = new Settings.Settings("settings.json");

            if (settings.SettingsLoaded == SettingsStatus.Ok)
            {
                dbConnectionString = settings.CreateDatabaseConnectionString();
                var token = settings.GetBotToken();
                
                var botClient = new TelegramBotClient(token);

                bankChooseMenu = new ChooseBankMenu(botClient, dbConnectionString);

                botClient.StartReceiving(HandleUpdateAsync, ErrorHandlerAsync);
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
