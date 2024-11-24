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

namespace TelegramBot
{
    public class Bot
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

        private UserInformation.User? _currentUser;
        private readonly UserInfo _userInfo;

        private readonly BankInfo _bankInfo;

        private readonly CashebackInfo _cashbackInfo;

        private readonly List<string> _searchVariants = ["Поиск по БИК", "Поиск по названию"];
        private readonly ReplyKeyboardMarkup _bankSearchKeyboard;
        private readonly List<string> _mainMenuVariants = ["Просмотр кешбека", "Добавление кешбека"];
        private readonly ReplyKeyboardMarkup _mainMenuKeyboard;
        private readonly List<string> _askVariants = ["Да", "Нет"];
        private readonly ReplyKeyboardMarkup _askKeyboard;

        public Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cancellationToken)
        {
            var errorMessage = ex.ToString();
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
        private async Task MakeMainMenu(Chat chat)
        {
            await _botClient.SendMessage(chat.Id,
                   "Выберите одну из функций в меню",
                   replyMarkup: _mainMenuKeyboard);
            await _userInfo.UpdateUserState(_currentUser, MenuState.MainMenu);
        }
        private async Task MakeFindBankMenu(long chatId)
        {
            await _botClient.SendMessage(chatId,
                                           "Варианты поиска банка:\n1. По БИК\n2. По названию",
                                           replyMarkup: _bankSearchKeyboard);
            await _userInfo.UpdateUserState(_currentUser, MenuState.BankFind);
        }
        private async Task MakePeekBankMenu(long chatId, List<Bank> banks)
        {
            ReplyKeyboardMarkup keyboard = BotUtil.GetKeyboardMarkup(banks.Select(bank => bank.ShortName).ToList());
            await _botClient.SendMessage(chatId,
                                           "Выберите банк",
                                           replyMarkup: keyboard);
            await _userInfo.UpdateUserState(_currentUser, MenuState.BankChoose);
        }

        private async Task MenuAddChashback(long chatId)
        {
            // Переход к вводу категорий
            await _botClient.SendMessage(chatId, $"Введите название категории кешбека и процент в формате:\n\"Категория кешбека\" процент", replyMarkup: new ReplyKeyboardRemove());
            await _userInfo.UpdateUserState(_currentUser, MenuState.AddCashbackCategory);
        }

        private async Task ProcessCashback(string input, long chatId) 
        {
            // Обрабатываем ввод
            try { 
                var category = Regex.Matches(input, @""".+""")[0].Value.Replace("\"", string.Empty);
                var rateStr = Regex.Matches(input, @"[0-9|.|,]+$")[0].Value.Replace(".",",");
                double rate = double.Parse(rateStr);
                var choosenBankId = await _userInfo.GetUserBankId(_currentUser);
                var cashback = new Casheback(choosenBankId, category, rate / 100.0, _currentUser.UserId);
                await _cashbackInfo.AddCashback(cashback, DateTime.Now);

                await MakeAskCashbackMenu(chatId);
            } catch 
            {
                await _botClient.SendMessage(chatId, "Неправильный формат, попробуйте ввести еще раз");
                await MenuAddChashback(chatId);
            }

        }

        private async Task MakeAskCashbackMenu(long chatId)
        {
            await _botClient.SendMessage(chatId,
                                           "Хотите добавить еще категорию?",
                                           replyMarkup: _askKeyboard);
            await _userInfo.UpdateUserState(_currentUser, MenuState.AskAddCashback);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message is not null)
                {
                    var chat = update.Message.Chat;

                    // Сперва берем юзера
                    _currentUser = new UserInformation.User(chat.FirstName, chat.LastName, chat.Username, chat.Id);

                    // Существует ли данный пользователь / чат в БД?
                    var userStatus = await _userInfo.CheckUserStatus(_currentUser);
                    if (userStatus == 0)
                    {
                        await botClient.SendMessage(chat.Id, $"Регистрирую вас, {chat.FirstName ?? chat.Username}.", cancellationToken: cancellationToken);

                        Console.WriteLine("Создаем пользователя");

                        var insertStatus = await _userInfo.CreateUser(_currentUser);
                        Console.WriteLine($"{insertStatus} row(s) inserted.");
                        if (insertStatus > 0)
                        {
                            await _userInfo.UpdateUserState(_currentUser, MenuState.UserRegistered);
                            await MakeMainMenu(chat);
                        }
                        else
                        {
                            await botClient.SendMessage(chat.Id, "Не удалось зарегистрировать вас в базе, попробуйте позже еще раз, отправив любое сообщение.", cancellationToken: cancellationToken);
                            await _userInfo.UpdateUserState(_currentUser, MenuState.NewUser);
                        }
                    }
                    else
                    {
                        _currentUser.UserId = await _userInfo.GetUserId(_currentUser);
                        _currentUser.State = await _userInfo.GetUserStateById(_currentUser);

                        if (_currentUser.State == MenuState.UserRegistered)
                        {
                            await MakeMainMenu(chat);
                        }
                        else if (_currentUser.State == MenuState.MainMenu)
                        {
                            switch (update.Message.Text)
                            {
                                case "Просмотр кешбека":
                                    var cashbacks = _cashbackInfo.GetUserCashebacks(_currentUser.UserId, DateTime.Now);
                                    if (cashbacks.Count == 0)
                                    {
                                        await botClient.SendMessage(chat.Id, "Не найдены кешбеки для текущего месяца.", cancellationToken: cancellationToken);
                                    }
                                    else
                                    {
                                        var groupedCashbacks = from cashback in cashbacks
                                                               group cashback by cashback.BankName;

                                        var strBuilder = new StringBuilder($"Ваши кешбеки на {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month)}\n");
                                        await botClient.SendMessage(chat.Id, strBuilder.ToString(), cancellationToken: cancellationToken, parseMode: ParseMode.Markdown);

                                        foreach (var bank in groupedCashbacks)
                                        {
                                            strBuilder = new StringBuilder($"*{bank.Key}*\n");

                                            foreach (var casheback in bank)
                                            {
                                                strBuilder.AppendLine($"_{casheback.Category}_:\t{Math.Round(casheback.Rate * 100, 2)}%");
                                            }
                                            await botClient.SendMessage(chat.Id, strBuilder.ToString(), cancellationToken: cancellationToken, parseMode: ParseMode.Markdown);
                                        }
                                    }
                                    await MakeMainMenu(chat);
                                    break;
                                case "Добавление кешбека":
                                    await botClient.SendMessage(chat.Id, "Выберите банк, по карте которого действует кешбек", cancellationToken: cancellationToken);
                                    await MakeFindBankMenu(chat.Id);
                                    break;
                                default:
                                    await MakeMainMenu(chat);
                                    break;
                            }
                        }
                        else if (_currentUser.State == MenuState.BankFind)
                        {
                            switch (update.Message!.Text)
                            {
                                case "Поиск по БИК":
                                    await _botClient.SendMessage(chat.Id, "Введите БИК частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                    await _userInfo.UpdateUserState(_currentUser, MenuState.BankFindByBic);
                                    break;
                                case "Поиск по названию":
                                    await _botClient.SendMessage(chat.Id, "Введите название банка частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                    await _userInfo.UpdateUserState(_currentUser, MenuState.BankFindByName);
                                    break;
                                default:
                                    await MakeFindBankMenu(chat.Id);
                                    break;
                            }
                        }
                        else if (_currentUser.State == MenuState.BankFindByName || _currentUser.State == MenuState.BankFindByBic)
                        {
                            var byNameOrByBic = (_currentUser.State == MenuState.BankFindByName ? 0 : 1);
                            var banks = _bankInfo.GetBanksBy(update.Message.Text, byNameOrByBic);
                            string bankData = "";
                            if (banks.Count == 0)
                            {
                                bankData = "Банков с таким " + ((byNameOrByBic) == 0 ? "наименованием" : "БИК") + " не нашлось";
                                await _botClient.SendMessage(chat.Id, bankData);
                                await MakeFindBankMenu(chat.Id);
                            }
                            else
                            {
                                var strBuilder = new StringBuilder();
                                for (int i = 0; i < banks.Count; ++i)
                                {
                                    strBuilder.Append($"{i + 1}.\t{banks[i].ShortName}\t{banks[i].RCBic}\n");
                                }
                                bankData = strBuilder.ToString();
                                await _botClient.SendMessage(chat.Id, bankData);
                                await MakePeekBankMenu(chat.Id, banks);
                            }
                        }
                        else if (_currentUser.State == MenuState.BankChoose)
                        {
                            var choosenBankIds = _bankInfo.GetBanksByName(name: update.Message!.Text, limit: 1);

                            if (choosenBankIds.Count > 0)
                            {
                                var _bankId = choosenBankIds[0].Id;
                                // Сохраним выбранный банк
                                await _userInfo.UpdateUserBankId(_currentUser, _bankId);
                                await _botClient.SendMessage(chat.Id, $"Выбран банк {choosenBankIds[0].FullName}", replyMarkup: new ReplyKeyboardRemove());
                                // Идем спрашивать кешбек
                                await MenuAddChashback(chat.Id);
                            }
                            else
                            {
                                await _botClient.SendMessage(chat.Id, "Выбирайте из списка!");
                                await MakeFindBankMenu(chat.Id);
                            }
                        }
                        else if (_currentUser.State == MenuState.AddCashbackCategory)
                        {
                            await ProcessCashback(update.Message!.Text, chat.Id);
                        }
                        else if (_currentUser.State == MenuState.AskAddCashback) 
                        {
                            switch (update.Message.Text)
                            {
                                case "Да":
                                    await MenuAddChashback(chat.Id);
                                    break;
                                case "Нет":
                                    await MakeMainMenu(chat);
                                    break;
                                default:
                                    await MenuAddChashback(chat.Id);
                                    break;
                            }
                        }
                        else
                        {
                            await _botClient.SendMessage(chat.Id, "Что-то не так с сообщением: " + update.Message.Text);
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