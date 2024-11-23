using Telegram.Bot;
using Telegram.Bot.Types;
using System.Data;
using BankInformation;
using UserInformation;
using Telegram.Bot.Types.ReplyMarkups;
using BotUtilities;
using System.Text;
using Telegram.Bot.Types.Enums;


namespace TelegramBot
{
    public class Bot
    {
        public Bot(TelegramBotClient botClient, string conn) 
        {
            _userInfo = new UserInfo(conn);
            _bankInfo = new BankInfo(conn);
            _banks = [];
            _botClient = botClient;
            _bankId = -1;
            _bankSearchKeyboard = BotUtil.GetKeyboardMarkup(_searchVariants); 
            _mainMenuKeyboard = BotUtil.GetKeyboardMarkup(_mainMenuVariants);
        }

        private UserInformation.User ? _currentUser;
        private int _bankId;
        private readonly UserInfo _userInfo;
        private readonly ITelegramBotClient _botClient;
        private readonly BankInfo _bankInfo;
        private List<Bank> _banks;

        private readonly List<string> _searchVariants = [ "Поиск по БИК", "Поиск по названию" ];
        private readonly ReplyKeyboardMarkup _bankSearchKeyboard;
        private readonly List<string> _mainMenuVariants = ["Выбор банка"];
        private readonly ReplyKeyboardMarkup _mainMenuKeyboard;

        public Task ErrorHandlerAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cancellationToken)
        {
            var errorMessage = ex.ToString();
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
        private async Task MakeMainMenu(Chat chat)
        {
            await _botClient.SendMessage(chat.Id,
                   "Функции бота",
                   replyMarkup: _mainMenuKeyboard);
            await _userInfo.UpdateUserState(_currentUser, MenuState.MainMenu);
        }
        private async Task VariantsFindBankMenu(long chatId)
        {
            await _botClient.SendMessage(chatId,
                                           "Варианты поиска банка:\n1. По БИК\n2. По названию",
                                           replyMarkup: _bankSearchKeyboard);
            await _userInfo.UpdateUserState(_currentUser, MenuState.BankFind);
        }
        private async Task PeekBankMenu(long chatId)
        {
            ReplyKeyboardMarkup keyboard = BotUtil.GetKeyboardMarkup(_banks.Select(bank => bank.ShortName).ToList()); 
            await _botClient.SendMessage(chatId,
                                           "Выберите банк",
                                           replyMarkup: keyboard);
            await _userInfo.UpdateUserState(_currentUser, MenuState.BankChoose);
        }
        
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message is not null)
                {
                    var chat = update.Message.Chat;
                    // Сперва берем юзера
                    /*var mes = $"{chat.Username} {chat.Id}:\n{update.Message.Text}";
                    Console.WriteLine(mes);*/

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
                                case "Выбор банка":
                                    await _userInfo.UpdateUserState(_currentUser, MenuState.InitBankFind);
                                    await VariantsFindBankMenu(chat.Id);
                                    //await ProccessChooseBankMenu(update);
                                    break;
                                default:
                                    await MakeMainMenu(chat);
                                    break;
                            }
                        }
                        else if (_currentUser.State == MenuState.InitBankFind)
                        {
                            await VariantsFindBankMenu(chat.Id);
                        }
                        else if (_currentUser.State == MenuState.BankFind)
                        {
                            switch (update.Message!.Text)
                            {
                                case "Поиск по БИК":
                                    await _botClient.SendMessage(chat.Id, "Введите БИК частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                    _currentUser.State = MenuState.BankFindByBic;
                                    await _userInfo.UpdateUserState(_currentUser, _currentUser.State);
                                    break;
                                case "Поиск по названию":
                                    await _botClient.SendMessage(chat.Id, "Введите название банка частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                    _currentUser.State = MenuState.BankFindByName;
                                    await _userInfo.UpdateUserState(_currentUser, _currentUser.State);
                                    break;
                                default:
                                    await VariantsFindBankMenu(chat.Id);
                                    break;
                            }
                        }
                        else if (_currentUser.State == MenuState.BankFindByName || _currentUser.State == MenuState.BankFindByBic)
                        {
                            var byNameOrByBic = (_currentUser.State == MenuState.BankFindByName ? 0 : 1);
                            _banks = _bankInfo.GetBanksBy(update.Message.Text, byNameOrByBic);
                            string bankData = "";
                            if (_banks.Count == 0)
                            {
                                bankData = "Банков с таким " + ((byNameOrByBic) == 0 ? "наименованием" : "БИК") + " не нашлось";
                                await _botClient.SendMessage(chat.Id, bankData);
                                await VariantsFindBankMenu(chat.Id);
                            }
                            else
                            {
                                var strBuilder = new StringBuilder();
                                for (int i = 0; i < _banks.Count; ++i)
                                {
                                    strBuilder.Append($"{i + 1}.\t{_banks[i].ShortName}\t{_banks[i].RCBic}\n");
                                }
                                bankData = strBuilder.ToString();
                                await _botClient.SendMessage(chat.Id, bankData);
                                await PeekBankMenu(chat.Id);
                            }
                        }
                        else if (_currentUser.State == MenuState.BankChoose)
                        {
                            if (_banks.Exists(bank => bank.ShortName == update.Message!.Text))
                            {
                                _bankId = _banks.Where(bank => bank.ShortName == update.Message!.Text).Select(bank => bank.Id).FirstOrDefault();
                                await _userInfo.UpdateUserState(_currentUser, MenuState.BankFound);
                                await _botClient.SendMessage(chat.Id, $"Выбран банк {update.Message!.Text} c id = {_bankId}", replyMarkup: new ReplyKeyboardRemove());
                                // Возвращаемся в меню
                                await MakeMainMenu(chat);
                            }
                            else
                            {
                                await _botClient.SendMessage(chat.Id, "Выбирайте из списка!");
                                await PeekBankMenu(chat.Id);
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