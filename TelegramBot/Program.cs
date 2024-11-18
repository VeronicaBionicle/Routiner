using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Settings;
using System;

namespace TelegramBot
{
    internal class Program
    {
        public static ChooseBankMenu bankChooseMenu;
        public static bool isFoundBank = false;
        public static int bankId;
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
                var dbConnectionString = settings.CreateDatabaseConnectionString();
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
