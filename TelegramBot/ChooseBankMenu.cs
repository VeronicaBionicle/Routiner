using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using BankInformation;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    public class ChooseBankMenu
    {
        public enum BankMenuState
        {
            Init,
            BankFind,
            BankFindByBic,
            BankFindByName,
            BankChoose,
            BankFound,
            Error
        }

        private BankMenuState _lastState;
        private BankInfo _bankRepository;
        private List<Bank> _banks;
        private TelegramBotClient _botClient;
        private readonly List<string> _searchVariants = new List<string> { "Поиск по БИК", "Поиск по названию" };
        private ReplyKeyboardMarkup _searchKeyboard;
        private int _bankId;
        public int bankId { get { return _bankId; } }
        private static KeyboardButton[] GetKeyboardButtons(List<string> list)
        {
            var res = new KeyboardButton[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                res[i] = new KeyboardButton(list[i]);
            }
            return res;
        }

        private async Task VariantsFindBankMenu(long chatId)
        {
            await _botClient.SendMessage(chatId,
                                           "Варианты поиска банка:\n1. По БИК\n2. По названию",
                                           replyMarkup: _searchKeyboard);
            _lastState = BankMenuState.BankFind;
        }

        private async Task PeekBankMenu(long chatId)
        {

            ReplyKeyboardMarkup keyboard = new(
                        new[] {
                            GetKeyboardButtons(_banks.Select(bank => bank.ShortName).ToList())
                        }
                        )
            { ResizeKeyboard = true };

            await _botClient.SendMessage(chatId,
                                           "Выберите банк",
                                           replyMarkup: keyboard);
            _lastState = BankMenuState.BankChoose;
        }

        public async Task<bool> ProccessChooseBankMenu(Update update) 
        {
            var chatId = update.Message.Chat.Id;

            switch (update.Type)
            {
                case UpdateType.Message:
                    Console.WriteLine(update.Message.Chat.Username + ": " + update.Message!.Text);

                    if (_lastState == BankMenuState.Init) 
                    {
                        await VariantsFindBankMenu(chatId);
                    }
                    else if (_lastState == BankMenuState.BankFind)
                    {
                        switch (update.Message!.Text)
                        {
                            case "Поиск по БИК":
                                await _botClient.SendMessage(chatId, "Введите БИК частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                _lastState = BankMenuState.BankFindByBic;
                                break;
                            case "Поиск по названию":
                                await _botClient.SendMessage(chatId, "Введите название банка частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                _lastState = BankMenuState.BankFindByName;
                                break;
                            default:
                                await VariantsFindBankMenu(chatId);
                                break;
                        }
                    }
                    else if (_lastState == BankMenuState.BankFindByName || _lastState == BankMenuState.BankFindByBic)
                    {
                        var byNameOrByBic = (_lastState == BankMenuState.BankFindByName ? 0 : 1);
                        _banks = _bankRepository.GetBanksBy(update.Message.Text, byNameOrByBic);
                        string bankData = "";
                        if (_banks.Count == 0)
                        {
                            bankData = "Банков с таким " + ((byNameOrByBic) == 0 ? "наименованием" : "БИК") + " не нашлось";
                            await _botClient.SendMessage(chatId, bankData);
                            await VariantsFindBankMenu(chatId);
                        }
                        else
                        {
                            var strBuilder = new StringBuilder();
                            for (int i = 0; i < _banks.Count; ++i)
                            {
                                strBuilder.Append($"{i + 1}.\t{_banks[i].ShortName}\t{_banks[i].RCBic}\n");
                            }
                            bankData = strBuilder.ToString();
                            await _botClient.SendMessage(chatId, bankData);
                            await PeekBankMenu(chatId);
                        }

                    }
                    else if (_lastState == BankMenuState.BankChoose)
                    {
                        if (_banks.Exists(bank => bank.ShortName == update.Message!.Text))
                        {
                            _bankId = _banks.Where(bank => bank.ShortName == update.Message!.Text).Select(bank => bank.Id).FirstOrDefault();
                            _lastState = BankMenuState.BankFound;
                        }
                        else 
                        {
                            await _botClient.SendMessage(chatId, "Выбирайте из списка!");
                            await PeekBankMenu(chatId);
                        }
                    }
                    else // BankFound или ... 
                    {
                        await _botClient.SendMessage(chatId, "Что-то: " + update.Message.Text);
                        await VariantsFindBankMenu(chatId);
                    }
                    break;
                default:
                    break;
            }
            // Если банк нашелся, заканчиваем
            if (_lastState == BankMenuState.BankFound) 
            {
                await _botClient.SendMessage(chatId, $"Выбран банк {update.Message!.Text}", replyMarkup: new ReplyKeyboardRemove());
                _lastState = BankMenuState.Init;
                return true;
            }
            return false;
        }
        public ChooseBankMenu(TelegramBotClient botClient, string dbConnectionString) 
        {
            _lastState = BankMenuState.Init;
            _bankRepository = new BankInfo(dbConnectionString);
            _banks = new List<Bank>();
            _botClient = botClient;
            _bankId = -1;
            _searchKeyboard = new(
                        new[] { GetKeyboardButtons(_searchVariants) }
                        )
            { ResizeKeyboard = true };
        }
    }
}
