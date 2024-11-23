using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Settings;
using System;
using Npgsql;
using System.Data;
using Dapper;

using BankInformation;
using UserInformation;

using Telegram.Bot.Types.ReplyMarkups;
using BotUtilities;
using System.Text;
using Telegram.Bot.Types.Enums;
using static Dapper.SqlMapper;
using System.Xml.Linq;


namespace TelegramBot
{
    internal class Program
    {
        public static ChooseBankMenu bankChooseMenu;
        public static bool isFoundBank = false;

        public static MenuState currentState = MenuState.Init;
        public static UserInformation.User currentUser;

        public static int bankId;
        public static string dbConnectionString;
        public static UserInfo userInfo;
        



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
                if (update.Type == UpdateType.Message) 
                {
                    // Начало работы с ботом
                    if (update.Message.Text == "/start")
                    {
                        // Сперва проверим юзера
                        var mes = $"{update.Message.Chat.Username} {update.Message.Chat.FirstName} {update.Message.Chat.Id}";
                        Console.WriteLine(mes);

                        currentUser = new UserInformation.User(update.Message.Chat.FirstName, update.Message.Chat.LastName, update.Message.Chat.Username, update.Message.Chat.Id);

                        /* Проверим юзера в базе */
                        var userStatus = await userInfo.CheckUserStatus(currentUser);
                        if (userStatus == 0)
                        {
                            await botClient.SendMessage(update.Message.Chat.Id, $"В первый раз тебя вижу, {update.Message.Chat.FirstName ?? update.Message.Chat.Username}");

                            var v = new List<string> { "Да", "Нет" };

                            ReplyKeyboardMarkup keyboard = new(
                            new[] {
                            BotUtil.GetKeyboardButtons(v)
                            }
                            )
                            { ResizeKeyboard = true };

                            await botClient.SendMessage(update.Message.Chat.Id,
                                   "Хотите зарегистрироваться в боте?",
                                   replyMarkup: keyboard);

                            currentState = MenuState.AskUserCreate;
                        }
                        else
                        {
                            await botClient.SendMessage(update.Message.Chat.Id, $"Дарова, {update.Message.Chat.FirstName ?? update.Message.Chat.Username}");
                            currentState = MenuState.UserRegistered;
                            // ShowMenu
                        }
                    }
                    else if (currentState == MenuState.AskUserCreate)
                    {
 
                                if (update.Message!.Text == "Да")
                                {
                                    Console.WriteLine("Создаем пользователя");
                                    var insertStatus = await userInfo.CreateUser(currentUser);
                                    Console.WriteLine($"{insertStatus} row(s) inserted.");
                                    if (insertStatus > 0)
                                    {
                                        currentState = MenuState.UserRegistered;
                                    }
                                    else
                                    {
                                        currentState = MenuState.Init;
                                    }

                                }
                                else
                                {
                                    await botClient.SendMessage(update.Message.Chat.Id, "Без регистрации функции не доступны...", replyMarkup: new ReplyKeyboardRemove());
                                    currentState = MenuState.Init;
                                }
                        }

                    else if (currentState == MenuState.UserRegistered)
                    {
                        //await botClient.SendMessage(update.Message.Chat.Id, "Меню Вклады и Кешбеки");

                        
                // Выбор банка
                if (!isFoundBank)
                {
                    isFoundBank = (await bankChooseMenu.ProccessChooseBankMenu(update) == MenuState.BankFound);
                    if (isFoundBank)
                    {
                        bankId = bankChooseMenu.bankId;
                        await botClient.SendMessage(update.Message.Chat.Id, "Выбрали банк с Id " + bankId);
                    }
                }
                // Тут уже знаем Id
                
                    }
                }
                

                

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
                userInfo = new UserInfo(dbConnectionString);

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
