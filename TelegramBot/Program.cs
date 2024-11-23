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

        public static UserInformation.User currentUser;

        public static int bankId;
        public static string dbConnectionString;
        public static UserInfo userInfo;

        public static readonly List<string> mainMenu = new List<string> { "Выбор банка" };


        public static Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cancellationToken)
        {
            var errorMessage = ex.ToString();
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        public static async Task MakeMainMenu(ITelegramBotClient botClient, Chat currentChat) 
        {
            var mainKeyboard = new ReplyKeyboardMarkup(
            new[] { BotUtil.GetKeyboardButtons(mainMenu) }
            )
            { ResizeKeyboard = true };

            await botClient.SendMessage(currentChat.Id,
                   "Функции бота",
                   replyMarkup: mainKeyboard);
            currentUser.State = MenuState.MainMenu;
            await userInfo.UpdateUserState(currentUser, currentUser.State);
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message is not null) 
                {
                    var currentChat = update.Message.Chat;
                    // Сперва берем юзера
                    var mes = $"{currentChat.Username} {currentChat.Id}:\n{update.Message.Text}";
                    Console.WriteLine(mes);
                    
                    currentUser = new UserInformation.User(currentChat.FirstName, currentChat.LastName, currentChat.Username, currentChat.Id);

                    // Существует ли данный пользователь / чат в БД?
                    var userStatus = await userInfo.CheckUserStatus(currentUser);
                    if (userStatus == 0)
                    {
                        await botClient.SendMessage(currentChat.Id, $"Регистрирую вас, {currentChat.FirstName ?? currentChat.Username}.");

                        Console.WriteLine("Создаем пользователя");

                        var insertStatus = await userInfo.CreateUser(currentUser);
                        Console.WriteLine($"{insertStatus} row(s) inserted.");
                        if (insertStatus > 0)
                        {
                            currentUser.State = MenuState.UserRegistered;
                            await userInfo.UpdateUserState(currentUser, currentUser.State);
                            await MakeMainMenu(botClient, currentChat);
                        }
                        else
                        {
                            await botClient.SendMessage(currentChat.Id, "Не удалось зарегистрировать вас в базе, попробуйте позже еще раз, отправив любое сообщение.");
                            currentUser.State = MenuState.NewUser;
                            await userInfo.UpdateUserState(currentUser, currentUser.State);
                        }
                    }
                    else
                    {
                        currentUser.UserId = await userInfo.GetUserId(currentUser);
                        currentUser.State = await userInfo.GetUserStateById(currentUser);

                        if (currentUser.State == MenuState.NewUser) 
                        {
                            currentUser.State = MenuState.UserRegistered;
                            await userInfo.UpdateUserState(currentUser, currentUser.State);
                        }


                        if (currentUser.State == MenuState.UserRegistered)
                        {
                            await MakeMainMenu(botClient, currentChat);
                        }
                        else if (currentUser.State == MenuState.MainMenu)
                        {
                            switch (update.Message.Text)
                            {
                                case "Выбор банка":
                                    currentUser.State = MenuState.InitBankFind;
                                    await userInfo.UpdateUserState(currentUser, currentUser.State);
                                    await bankChooseMenu.ProccessChooseBankMenu(update, currentUser);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (currentUser.State == MenuState.InitBankFind || currentUser.State == MenuState.InitBankFind ||
                                 currentUser.State == MenuState.BankFind || currentUser.State == MenuState.BankFindByBic ||
                                 currentUser.State == MenuState.BankFindByName || currentUser.State == MenuState.BankChoose )
                        {
                                await bankChooseMenu.ProccessChooseBankMenu(update, currentUser);
                                if (currentUser.State == MenuState.BankFound)
                                {
                                    bankId = bankChooseMenu.bankId;
                                    await botClient.SendMessage(currentChat.Id, "Выбрали банк с Id " + bankId);
                                    // Возвращаемся в меню
                                    await MakeMainMenu(botClient, currentChat);
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ErrorHandlerAsync(botClient, ex, cancellationToken);
            } 
        }

        static async Task Main(string[] args)
        {
            var settings = new Settings.Settings("settings.json");

            if (settings.SettingsLoaded == SettingsStatus.Ok)
            {
                dbConnectionString = settings.CreateDatabaseConnectionString();
                var token = settings.GetBotToken();
                
                var botClient = new TelegramBotClient(token);

                bankChooseMenu = new ChooseBankMenu(botClient, dbConnectionString);
                userInfo = new UserInfo(dbConnectionString);

                using (IDbConnection db = new NpgsqlConnection(dbConnectionString))
                {
                    await db.QuerySingleOrDefaultAsync<MenuState>("UPDATE routiner.t_users SET state = @State",
                    new { State = MenuState.NewUser });
                }

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
