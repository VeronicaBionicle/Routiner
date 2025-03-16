using BotUtilities;
using CashbackInformation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;

namespace TelegramBot
{
    public partial class Bot
    {
        // Функция для перехода в новое состояние
        private async Task SendMessageAndChangeState(IReplyMarkup? keyboard, MenuState state)
        {
            await _botClient.SendMessage(_user.ChatId,
                       BotUtil.StateMessages[state],
                       replyMarkup: keyboard);
            await _userInfo.UpdateUserState(_user, state);
        }

        private async Task CreateUser()
        {
            await _botClient.SendMessage(_user.ChatId, $"Регистрирую вас, {_user.Name ?? _user.TelegramAccount}.");

            var userId = await _userInfo.CreateUser(_user);
            if (userId > 0)
            {
                _user.UserId = userId;
                await SendMessageAndChangeState(_mainMenuKeyboard, MenuState.MainMenu);
            }
            else
            {
                await SendMessageAndChangeState(null, MenuState.NewUser);
            }
        }

        private async Task WatchCashbacks()
        {
            var cashbacks = _cashbackInfo.GetUserCashebacks(_user.UserId, DateTime.Now);
            if (cashbacks.Count == 0)
            {
                await _botClient.SendMessage(_user.ChatId, "Не найдены кешбеки для текущего месяца.");
            }
            else
            {
                var groupedCashbacks = from cashback in cashbacks
                                       group cashback by cashback.BankName;

                var strBuilder = new StringBuilder($"Ваши кешбеки на {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month)}\n");
                await _botClient.SendMessage(_user.ChatId, strBuilder.ToString(), parseMode: ParseMode.Markdown);

                foreach (var bank in groupedCashbacks)
                {
                    strBuilder = new StringBuilder($"*{bank.Key}*\n");

                    foreach (var casheback in bank)
                    {
                        strBuilder.AppendLine($"_{casheback.Category}_:\t{Math.Round(casheback.Rate * 100, 2)}%");
                    }
                    await _botClient.SendMessage(_user.ChatId, strBuilder.ToString(), parseMode: ParseMode.Markdown);
                }
            }
        }
        private async Task WatchGroupCashbacks()
        {
            var usersInGroup = _userInfo.GetUsersInYourGroup(_user); // получаем список групп
            if (usersInGroup.Count == 0) 
            {
                await _botClient.SendMessage(_user.ChatId, "Вы не состоите ни в каких группах");
            }
            else 
            {
                foreach (var user in usersInGroup)
                {
                    var cashbacks = _cashbackInfo.GetUserCashebacks(user.Item1, DateTime.Now);
                    if (cashbacks.Count > 0)
                    {
                        var groupedCashbacks = from cashback in cashbacks
                                               group cashback by cashback.BankName;

                        var strBuilder = new StringBuilder($"*{user.Item2}*: кешбеки на {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month)}\n");
                        await _botClient.SendMessage(_user.ChatId, strBuilder.ToString(), parseMode: ParseMode.Markdown);

                        foreach (var bank in groupedCashbacks)
                        {
                            strBuilder = new StringBuilder($"*{bank.Key}*\n");

                            foreach (var casheback in bank)
                            {
                                strBuilder.AppendLine($"_{casheback.Category}_:\t{Math.Round(casheback.Rate * 100, 2)}%");
                            }
                            await _botClient.SendMessage(_user.ChatId, strBuilder.ToString(), parseMode: ParseMode.Markdown);
                        }
                    }
                }
            }
        }
        

        private async Task ProcessCashback(string input)
        {
            // Обрабатываем ввод
            try
            {
                var category = Regex.Matches(input, @""".+""")[0].Value.Replace("\"", string.Empty); // категория кешбека
                
                var rateStr = Regex.Matches(input, @"[0-9|.|,]+")[0].Value.Replace(".", ","); // процент кешбека
                var rate = double.Parse(rateStr);

                // Если ничего не вводили по месяцу, то будет автоматом выбран текущий
                var dateStart = DateTime.Now;
                var monthMatch = Regex.Matches(input, @"\([А-Яа-я]+\)");

                if (monthMatch.Count > 0)
                {
                    var monthStr = monthMatch[0].Value[1..^1]; // убрать скобки
                    var month = BotUtil.MonthNumberByName(monthStr);

                    if (month > 0) // если номер месяца корректный, пишем
                    {
                        dateStart = new DateTime(dateStart.Year, month, 1);
                    }
                }

                var choosenBankId = await _userInfo.GetUserBankId(_user);
                var cashback = new Casheback(choosenBankId, category, rate / 100.0, _user.UserId);
                await _cashbackInfo.AddCashback(cashback, dateStart);

                await SendMessageAndChangeState(_askKeyboard, MenuState.AskAddCashback);
            }
            catch
            {
                await _botClient.SendMessage(_user.ChatId, "Неправильный формат, попробуйте ввести еще раз");
                await SendMessageAndChangeState(new ReplyKeyboardRemove(), MenuState.AddCashbackCategory);
            }
        }

        private async Task ShowFoundBanks(string input)
        {
            var byNameOrByBic = (_user.State == MenuState.BankFindByName ? 0 : 1);
            var banks = _bankInfo.GetBanksBy(input, byNameOrByBic);
            string bankData = "";
            if (banks.Count == 0)
            {
                bankData = "Банков с таким " + ((byNameOrByBic) == 0 ? "наименованием" : "БИК") + " не нашлось";
                await _botClient.SendMessage(_user.ChatId, bankData);
                await SendMessageAndChangeState(_bankSearchKeyboard, MenuState.BankFind);
            }
            else
            {
                var strBuilder = new StringBuilder();
                for (int i = 0; i < banks.Count; ++i)
                {
                    strBuilder.Append($"{i + 1}.\t{banks[i].ShortName}\t{banks[i].RCBic}\n");
                }
                bankData = strBuilder.ToString();
                await _botClient.SendMessage(_user.ChatId, bankData);
                var bankVariants = banks.Select(bank => bank.ShortName).ToList();
                bankVariants.Add("Отмена"); // Чтобы была кнопка Отмены
                var bankKeyboard = BotUtil.GetKeyboardMarkup(bankVariants);
                await SendMessageAndChangeState(bankKeyboard, MenuState.BankChoose);
            }
        }

        private async Task ChooseBankForCashback(string input)
        {
            var choosenBankIds = _bankInfo.GetBanksByName(name: input, limit: 1); // Находим единственный по названию

            if (choosenBankIds.Count > 0)
            {
                var _bankId = choosenBankIds[0].Id;
                // Сохраним выбранный банк
                await _userInfo.UpdateUserBankId(_user, _bankId);
                await _botClient.SendMessage(_user.ChatId, $"Выбран банк {choosenBankIds[0].FullName}", replyMarkup: new ReplyKeyboardRemove());
                // Идем спрашивать кешбек
                await SendMessageAndChangeState(new ReplyKeyboardRemove(), MenuState.AddCashbackCategory);
            }
            else
            {
                await _botClient.SendMessage(_user.ChatId, "Выбирайте из списка!");
                await SendMessageAndChangeState(_bankSearchKeyboard, MenuState.BankFind);
            }
        }
    }
}
