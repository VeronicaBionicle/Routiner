using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using BotUtilities;
using UserInformation;

namespace BankInformation
{
    public class ChooseBankMenu
    {
        //private MenuState user.State;

        private BankInfo _bankRepository;
        private List<Bank> _banks;
        private TelegramBotClient _botClient;
        private readonly List<string> _searchVariants = new List<string> { "Поиск по БИК", "Поиск по названию" };
        private ReplyKeyboardMarkup _searchKeyboard;
        private int _bankId;
        public int bankId { get { return _bankId; } }
        //public UserInformation.User user;
        public UserInfo userInfo;

        private async Task VariantsFindBankMenu(long chatId, UserInformation.User user)
        {
            await _botClient.SendMessage(chatId,
                                           "Варианты поиска банка:\n1. По БИК\n2. По названию",
                                           replyMarkup: _searchKeyboard);
            user.State = MenuState.BankFind;
            await userInfo.UpdateUserState(user, user.State);
        }

        private async Task PeekBankMenu(long chatId, UserInformation.User user)
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
            user.State = MenuState.BankChoose;
            await userInfo.UpdateUserState(user, user.State);
        }

        public async Task<MenuState> ProccessChooseBankMenu(Update update, UserInformation.User user)
        {
            var chatId = update.Message.Chat.Id;

            if (user.State == MenuState.InitBankFind)
            {
                await VariantsFindBankMenu(chatId, user);
            }
            else if (user.State == MenuState.BankFind)
            {
                switch (update.Message!.Text)
                {
                    case "Поиск по БИК":
                        await _botClient.SendMessage(chatId, "Введите БИК частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                        user.State = MenuState.BankFindByBic;
                        await userInfo.UpdateUserState(user, user.State);
                        break;
                    case "Поиск по названию":
                        await _botClient.SendMessage(chatId, "Введите название банка частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                        user.State = MenuState.BankFindByName;
                        await userInfo.UpdateUserState(user, user.State);
                        break;
                    default:
                        await VariantsFindBankMenu(chatId, user);
                        break;
                }
            }
            else if (user.State == MenuState.BankFindByName || user.State == MenuState.BankFindByBic)
            {
                var byNameOrByBic = (user.State == MenuState.BankFindByName ? 0 : 1);
                _banks = _bankRepository.GetBanksBy(update.Message.Text, byNameOrByBic);
                string bankData = "";
                if (_banks.Count == 0)
                {
                    bankData = "Банков с таким " + ((byNameOrByBic) == 0 ? "наименованием" : "БИК") + " не нашлось";
                    await _botClient.SendMessage(chatId, bankData);
                    await VariantsFindBankMenu(chatId, user);
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
                    await PeekBankMenu(chatId, user);
                }
            }
            else if (user.State == MenuState.BankChoose)
            {
                if (_banks.Exists(bank => bank.ShortName == update.Message!.Text))
                {
                    _bankId = _banks.Where(bank => bank.ShortName == update.Message!.Text).Select(bank => bank.Id).FirstOrDefault();
                    user.State = MenuState.BankFound;
                    await userInfo.UpdateUserState(user, user.State);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Выбирайте из списка!");
                    await PeekBankMenu(chatId, user);
                }
            }
            else // BankFound или ... 
            {
                await _botClient.SendMessage(chatId, "Что-то: " + update.Message.Text);
                await VariantsFindBankMenu(chatId, user);
            }

            // Если банк нашелся, заканчиваем
            if (user.State == MenuState.BankFound)
            {
                await _botClient.SendMessage(chatId, $"Выбран банк {update.Message!.Text}", replyMarkup: new ReplyKeyboardRemove());
                /*user.State = MenuState.InitBankFind;
                await userInfo.UpdateUserState(user, user.State);*/
                return MenuState.BankFound;
            }
            return user.State;
        }
        public ChooseBankMenu(TelegramBotClient botClient, string dbConnectionString)
        {
            //user.State = MenuState.InitBankFind;
            userInfo = new UserInfo(dbConnectionString);
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
