using Telegram.Bot.Types.ReplyMarkups;

namespace BotUtilities 
{
    public static class BotUtil
    {
        private static KeyboardButton[] GetKeyboardButtons(List<string> list)
        {
            var res = new KeyboardButton[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                res[i] = new KeyboardButton(list[i]);
            }
            return res;
        }

        public static ReplyKeyboardMarkup GetKeyboardMarkup(List<string> list) 
        {
            return new ReplyKeyboardMarkup(new[] { GetKeyboardButtons(list) }) { ResizeKeyboard = true };
        }
    }

    public enum MenuState
    {
        NewUser,        // пользователь в первый раз пишет боту, требуется добавить в базу

        UserRegistered,
        MainMenu,

        InitBankFind,
        BankFind,
        BankFindByBic,
        BankFindByName,
        BankChoose,
        BankFound,

        Error
    }
}
