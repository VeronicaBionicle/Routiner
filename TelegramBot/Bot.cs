using Telegram.Bot;
using Telegram.Bot.Types;
using System.Data;
using BankInformation;
using UserInformation;
using Telegram.Bot.Types.ReplyMarkups;
using BotUtilities;
using System.Text;
using Telegram.Bot.Types.Enums;
using CashbackInformation;
using System.Globalization;
using System;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace TelegramBot
{
    public partial class Bot
    {
        public Bot(TelegramBotClient botClient, string conn)
        {
            _userInfo = new UserInfo(conn);
            _bankInfo = new BankInfo(conn);
            _cashbackInfo = new CashebackInfo(conn);

            _botClient = botClient;
            _bankSearchKeyboard = BotUtil.GetKeyboardMarkup(_searchVariants);
            _mainMenuKeyboard = BotUtil.GetKeyboardMarkup(_mainMenuVariants);
            _askKeyboard = BotUtil.GetKeyboardMarkup(_askVariants);
        }

        private readonly ITelegramBotClient _botClient;

        private UserInformation.User? _user;
        private readonly UserInfo _userInfo;

        private readonly BankInfo _bankInfo;

        private readonly CashebackInfo _cashbackInfo;

        private readonly ReplyKeyboardMarkup _bankSearchKeyboard;
        private readonly ReplyKeyboardMarkup _mainMenuKeyboard;
        private readonly ReplyKeyboardMarkup _askKeyboard;

        private readonly List<string> _searchVariants = ["Поиск по БИК", "Поиск по названию", "Отмена"];
        private readonly List<string> _mainMenuVariants = ["Мои кешбеки", "Кешбеки группы",  "Добавление кешбека", "Удаление кешбека"];
        private readonly List<string> _askVariants = ["Да", "Нет"];

        public Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cancellationToken)
        {
            var errorMessage = ex.ToString();
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                // Пропускаем только непустые текстовые сообщения
                if (update.Type == UpdateType.Message && update.Message is not null && update.Message.Text is not null)
                {
                    var chat = update.Message.Chat;

                    // Сперва берем юзера
                    _user = new UserInformation.User(chat.FirstName, chat.LastName, chat.Username, chat.Id);

                    // Существует ли данный пользователь / чат в БД?
                    var userStatus = await _userInfo.CheckUserStatus(_user);
                    if (userStatus == 0)
                    {
                        await CreateUser();
                    }
                    else // Обработка для существующих пользователей
                    {
                        // Уточняем id в базе и последнее состояние меню пользователя
                        _user.UserId = await _userInfo.GetUserId(_user);
                        _user.State = await _userInfo.GetUserStateById(_user);

                        // Обработка состояний
                        switch (_user.State) 
                        {
                            case MenuState.UserRegistered:
                                await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu);
                                break;
                            case MenuState.MainMenu:
                                switch (update.Message.Text)
                                {
                                    case "Мои кешбеки":
                                        await WatchCashbacks();
                                        await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu); // Возврат в меню
                                        break;
                                    case "Кешбеки группы":
                                        await WatchGroupCashbacks();
                                        await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu); // Возврат в меню
                                        break;
                                    case "Добавление кешбека":
                                        await botClient.SendMessage(chat.Id, "Выберите банк, по карте которого действует кешбек", cancellationToken: cancellationToken);
                                        await SendMessageAndChangeState(_bankSearchKeyboard, MenuState.BankFind);
                                        break;
                                    case "Удаление кешбека":
                                        var cashbackList = _cashbackInfo.GetUserCashebacks(_user.UserId, DateTime.Now);
                                        if (cashbackList.Count > 0)
                                        {
                                            var cashbacks = cashbackList.Select(cashback => $"{cashback.BankName}: {cashback.Category} {Math.Round(cashback.Rate * 100, 2)}%").ToList();
                                            var cashbackKeyboard = BotUtil.GetKeyboardMarkup(cashbacks);
                                            await SendMessageAndChangeState(cashbackKeyboard, MenuState.DeleteCashback);
                                        }
                                        else 
                                        {
                                            await _botClient.SendMessage(_user.ChatId, "Не найдены кешбеки для текущего месяца.");
                                            await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu); // возврат в меню
                                        }
                                        break;
                                    default:
                                        await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu); // Возврат в меню
                                        break;
                                }
                                break;
                            case MenuState.BankFind:
                                switch (update.Message!.Text)
                                {
                                    case "Поиск по БИК":
                                        await SendMessageAndChangeState(new ReplyKeyboardRemove(), MenuState.BankFindByBic);
                                        break;
                                    case "Поиск по названию":
                                        await SendMessageAndChangeState(new ReplyKeyboardRemove(), MenuState.BankFindByName);
                                        break;
                                    case "Отмена": case "/cancel":
                                        await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu);
                                        break;
                                    default:
                                        await SendMessageAndChangeState(_bankSearchKeyboard, MenuState.BankFind);
                                        break;
                                }
                                break;
                            case MenuState.BankFindByName:
                            case MenuState.BankFindByBic:
                                if (update.Message.Text == "/cancel")
                                {
                                    await SendMessageAndChangeState(_bankSearchKeyboard, MenuState.BankFind);
                                }
                                else
                                {
                                    await ShowFoundBanks(update.Message.Text);
                                }
                                break;
                            case MenuState.BankChoose:
                                if (update.Message.Text == "Отмена" || update.Message.Text == "/cancel") 
                                {
                                    await SendMessageAndChangeState(_bankSearchKeyboard, MenuState.BankFind);
                                }
                                else
                                {
                                    await ChooseBankForCashback(update.Message.Text);
                                }
                                break;
                            case MenuState.AddCashbackCategory:
                                if (update.Message.Text == "Отмена" || update.Message.Text == "/cancel")
                                {
                                    await SendMessageAndChangeState(_bankSearchKeyboard, MenuState.BankFind);
                                }
                                else
                                {
                                    await ProcessCashback(update.Message.Text);
                                }
                                break;
                            case MenuState.AskAddCashback:
                                switch (update.Message.Text)
                                {
                                    case "Да":
                                        await SendMessageAndChangeState(new ReplyKeyboardRemove(), MenuState.AddCashbackCategory);
                                        break;
                                    case "Нет":
                                        await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu);
                                        break;
                                    default:
                                        await SendMessageAndChangeState(new ReplyKeyboardRemove(), MenuState.AddCashbackCategory);
                                        break;
                                }
                                break;
                            case MenuState.DeleteCashback:
                                if (update.Message.Text != "Отмена" && update.Message.Text != "/cancel")
                                {
                                    var cashbackListToDelete = _cashbackInfo.GetUserCashebacks(_user.UserId, DateTime.Now);
                                    var cashbackToDelete = cashbackListToDelete.FirstOrDefault(c => $"{c.BankName}: {c.Category} {Math.Round(c.Rate * 100, 2)}%" == update.Message.Text);
                                    if (cashbackToDelete is not null)
                                        await _cashbackInfo.DeleteCashback(cashbackToDelete);
                                }
                                await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu); // возврат в меню
                                break;
                            default:
                                await _botClient.SendMessage(chat.Id, "Что-то не так с сообщением: " + update.Message.Text);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ErrorHandlerAsync(botClient, ex, cancellationToken);
            }
        }
    }
}