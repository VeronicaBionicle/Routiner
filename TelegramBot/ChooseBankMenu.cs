using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using BotUtilities;

namespace BankInformation
{
    public class ChooseBankMenu
    {
        private MenuState _currentState;
        private BankInfo _bankRepository;
        private List<Bank> _banks;
        private TelegramBotClient _botClient;
        private readonly List<string> _searchVariants = new List<string> { "Поиск по БИК", "Поиск по названию" };
        private ReplyKeyboardMarkup _searchKeyboard;
        private int _bankId;
        public int bankId { get { return _bankId; } }

        private async Task VariantsFindBankMenu(long chatId)
        {
            await _botClient.SendMessage(chatId,
                                           "Варианты поиска банка:\n1. По БИК\n2. По названию",
                                           replyMarkup: _searchKeyboard);
            _currentState = MenuState.BankFind;
        }

        private async Task PeekBankMenu(long chatId)
        {

            ReplyKeyboardMarkup keyboard = new(
                        new[] {
                            BotUtil.GetKeyboardButtons(_banks.Select(bank => bank.ShortName).ToList())
                        }
                        )
            { ResizeKeyboard = true };

            await _botClient.SendMessage(chatId,
                                           "Выберите банк",
                                           replyMarkup: keyboard);
            _currentState = MenuState.BankChoose;
        }

        public async Task<MenuState> ProccessChooseBankMenu(Update update)
        {
            var chatId = update.Message.Chat.Id;

            //Console.WriteLine(update.Message.Chat.Username + ": " + update.Message!.Text);

            if (_currentState == MenuState.InitBankFind)
            {
                await VariantsFindBankMenu(chatId);
            }
            else if (_currentState == MenuState.BankFind)
            {
                switch (update.Message!.Text)
                {
                    case "Поиск по БИК":
                        await _botClient.SendMessage(chatId, "Введите БИК частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                        _currentState = MenuState.BankFindByBic;
                        break;
                    case "Поиск по названию":
                        await _botClient.SendMessage(chatId, "Введите название банка частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                        _currentState = MenuState.BankFindByName;
                        break;
                    default:
                        await VariantsFindBankMenu(chatId);
                        break;
                }
            }
            else if (_currentState == MenuState.BankFindByName || _currentState == MenuState.BankFindByBic)
            {
                var byNameOrByBic = (_currentState == MenuState.BankFindByName ? 0 : 1);
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
            else if (_currentState == MenuState.BankChoose)
            {
                if (_banks.Exists(bank => bank.ShortName == update.Message!.Text))
                {
                    _bankId = _banks.Where(bank => bank.ShortName == update.Message!.Text).Select(bank => bank.Id).FirstOrDefault();
                    _currentState = MenuState.BankFound;
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

            // Если банк нашелся, заканчиваем
            if (_currentState == MenuState.BankFound)
            {
                await _botClient.SendMessage(chatId, $"Выбран банк {update.Message!.Text}", replyMarkup: new ReplyKeyboardRemove());
                _currentState = MenuState.InitBankFind;
                return MenuState.BankFound;
            }
            return _currentState;
        }
        public ChooseBankMenu(TelegramBotClient botClient, string dbConnectionString)
        {
            _currentState = MenuState.InitBankFind;
            _bankRepository = new BankInfo(dbConnectionString);
            _banks = new List<Bank>();
            _botClient = botClient;
            _bankId = -1;
            _searchKeyboard = new(
                        new[] { BotUtil.GetKeyboardButtons(_searchVariants) }
                        )
            { ResizeKeyboard = true };
        }
    }
}
