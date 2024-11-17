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
        private enum BankMenuState
        {
            Init,
            BankFind,
            BankFindByBic,
            BankFindByName,
            BankChoose,
            Error
        }

        private BankMenuState lastAnswer;
        private BankInfo bankRep;
        private List<Bank> banks;
        private TelegramBotClient _botClient;
        private readonly List<string> _searchVariants = new List<string> { "Поиск по БИК", "Поиск по названию" };
        private ReplyKeyboardMarkup _searchKeyboard;
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
            lastAnswer = BankMenuState.BankFind;
        }

        private async Task PeekBankMenu(long chatId)
        {

            ReplyKeyboardMarkup keyboard = new(
                        new[] {
                            GetKeyboardButtons(banks.Select(bank => bank.ShortName).ToList())
                        }
                        )
            { ResizeKeyboard = true };

            await _botClient.SendMessage(chatId,
                                           "Выберите банк",
                                           replyMarkup: keyboard);
            lastAnswer = BankMenuState.BankChoose;
        }

        public async Task/*<int>*/ ProccessChooseBankMenu(Update update) 
        {
            var chatId = update.Message.Chat.Id;
                switch (update.Type)
                {
                    case UpdateType.Message:
                        Console.WriteLine(update.Message.Chat.Username + ": " + update.Message!.Text);

                        if (lastAnswer == BankMenuState.Init) 
                        {
                            await VariantsFindBankMenu(chatId);
                        }
                        else if (lastAnswer == BankMenuState.BankFind)
                        {
                            switch (update.Message!.Text)
                            {
                                case "Поиск по БИК":
                                    await _botClient.SendMessage(chatId, "Введите БИК частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                    lastAnswer = BankMenuState.BankFindByBic;
                                    break;
                                case "Поиск по названию":
                                    await _botClient.SendMessage(chatId, "Введите название банка частично или целиком", replyMarkup: new ReplyKeyboardRemove());
                                    lastAnswer = BankMenuState.BankFindByName;
                                    break;
                                default:
                                    await VariantsFindBankMenu(chatId);
                                    break;
                            }
                        }
                        else if (lastAnswer == BankMenuState.BankFindByName || lastAnswer == BankMenuState.BankFindByBic)
                        {
                            var byNameOrByBic = (lastAnswer == BankMenuState.BankFindByName ? 0 : 1);
                            banks = bankRep.GetBanksBy(update.Message.Text, byNameOrByBic);
                            string message = "";
                            if (banks.Count == 0)
                            {
                                message = "Банков с таким " + ((byNameOrByBic) == 0 ? "наименованием" : "БИК") + " не нашлось";
                                await _botClient.SendMessage(chatId, message);
                                await VariantsFindBankMenu(chatId);
                            }
                            else
                            {
                                var strBuilder = new StringBuilder();
                                for (int i = 0; i < banks.Count; ++i)
                                {
                                    strBuilder.Append($"{i + 1}.\t{banks[i].ShortName}\t{banks[i].RCBic}\n");
                                }
                                message = strBuilder.ToString();
                                await _botClient.SendMessage(chatId, message);
                                await PeekBankMenu(chatId);
                            }

                        }
                        else if (lastAnswer == BankMenuState.BankChoose)
                        {
                            var bankId = banks.Where(bank => bank.ShortName == update.Message!.Text).Select(bank => bank.Id).FirstOrDefault();
                            //lastAnswer = BankMenuState.Init;
                            //return bankId;
                            await _botClient.SendMessage(chatId, "Выбрали банк с Id " + bankId);
                           // Придумываем, как передать bankId...
                            await VariantsFindBankMenu(chatId);
                        }
                        else
                        {
                            await _botClient.SendMessage(chatId, "Что-то: " + update.Message.Text);
                            await VariantsFindBankMenu(chatId);
                            //return -1;
                        }
                        //return -1;
                        break;
                    default:
                        //return -1;
                        break;
                }            
        }
        public ChooseBankMenu(TelegramBotClient botClient, string dbConnectionString) 
        {
            lastAnswer = BankMenuState.Init;
            bankRep = new BankInfo(dbConnectionString);
            banks = new List<Bank>();
            _botClient = botClient;

            _searchKeyboard = new(
                        new[] { GetKeyboardButtons(_searchVariants) }
                        )
            { ResizeKeyboard = true };
        }
    }
}
