using Telegram.Bot.Types.ReplyMarkups;

namespace BotUtilities 
{
    public static class BotUtil
    {
        public static KeyboardButton[] GetKeyboardButtons(List<string> list)
        {
            var res = new KeyboardButton[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                res[i] = new KeyboardButton(list[i]);
            }
            return res;
        }
    }

    public enum MenuState
    {
        Init,

        AskUserCreate,
        UserCreate,
        UserRegistered,

        InitBankFind,
        BankFind,
        BankFindByBic,
        BankFindByName,
        BankChoose,
        BankFound,

        Error
    }
}
