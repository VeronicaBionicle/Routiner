using BankInformation;
using BotUtilities;
using System.Text;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace UserInformation
{
    public class UserMenu 
    {
        private MenuState _currentState;
        private BankInfo _bankRepository;
        private List<Bank> _banks;
        private TelegramBotClient _botClient;
        private readonly List<string> _searchVariants = new List<string> { "Поиск по БИК", "Поиск по названию" };
        private ReplyKeyboardMarkup _searchKeyboard;
        private int _bankId;
        public int bankId { get { return _bankId; } }

        private async Task PeekBankMenu(long chatId)
        {

            ReplyKeyboardMarkup keyboard = BotUtil.GetKeyboardMarkup(_banks.Select(bank => bank.ShortName).ToList());
            await _botClient.SendMessage(chatId,
                                           "Выберите банк",
                                           replyMarkup: keyboard);
            _currentState = MenuState.BankChoose;
        }

        public async Task<MenuState> ProccessUserMenu(Update update)
        {
            var chatId = update.Message.Chat.Id;

            switch (update.Type)
            {
                case UpdateType.Message:

                    
                    break;
                default:
                    break;
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

       public UserMenu(TelegramBotClient botClient, string dbConnectionString)
        {
            _currentState = MenuState.InitBankFind;
            _bankRepository = new BankInfo(dbConnectionString);
            _banks = new List<Bank>();
            _botClient = botClient;
            _bankId = -1;
            _searchKeyboard = BotUtil.GetKeyboardMarkup(_searchVariants);
        }
    }
}
