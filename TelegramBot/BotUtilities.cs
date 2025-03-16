using Npgsql;
using System.Data;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotUtilities
{    
    // Состояния меню бота
    public enum MenuState
    {
        NewUser,        // пользователь в первый раз пишет боту, требуется добавить в базу
        UserRegistered,
        MainMenu,
        BankFind,
        BankFindByBic,
        BankFindByName,
        BankChoose,
        AddCashbackCategory,
        AskAddCashback,
        DeleteCashback,
        Error
    }
    public static class BotUtil
    {
        // Тексты для состояний меню
        internal static readonly Dictionary<MenuState, string> StateMessages = new Dictionary<MenuState, string>
        {
            { MenuState.NewUser, "Не удалось зарегистрировать вас в базе, попробуйте позже еще раз, отправив любое сообщение.\nЕсли ошибка не пропала, напишите автору @VeronicaNenyuk"},
            { MenuState.UserRegistered, ""},
            { MenuState.MainMenu, "Выберите одну из функций в меню" },
            { MenuState.BankFind, "Варианты поиска банка:\n1. По БИК\n2. По названию"},
            { MenuState.BankFindByBic, "Введите БИК частично или целиком.\nЕсли передумали искать по БИК, введите /cancel"},
            { MenuState.BankFindByName, "Введите название банка частично или целиком.\nЕсли передумали искать по названию, введите /cancel"},
            { MenuState.BankChoose, "Выберите банк"},
            { MenuState.AddCashbackCategory, "Введите название категории кешбека и процент в формате:\n\"Категория кешбека\" процент.\nЕсли передумали вводить, введите /cancel" },
            { MenuState.AskAddCashback, "Хотите добавить еще категорию?" },
            { MenuState.DeleteCashback, "Выберите кешбек для удаления" },
            { MenuState.Error, "Произошла ошибка"}
        };

        // Функция получения кнопок клавиатуры
        private static KeyboardButton[] GetKeyboardButtons(List<string> list)
        {
            var res = new KeyboardButton[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                res[i] = new KeyboardButton(list[i]);
            }
            return res;
        }

        // Функция получения клавиатуры
        internal static ReplyKeyboardMarkup GetKeyboardMarkup(List<string> list) 
        {
            return new ReplyKeyboardMarkup(new[] { GetKeyboardButtons(list) }) { ResizeKeyboard = true };
        }

        // Функция получения номера месяца из названия
        internal static int MonthNumberByName(string monthName)
        {
            var cultureInfo = new CultureInfo("ru-RU");
            var abbreviatedMonthGenitiveNames = cultureInfo.DateTimeFormat.AbbreviatedMonthGenitiveNames;

            for (int i = 0; i < abbreviatedMonthGenitiveNames.Length - 1; ++i)
            {
                var name = abbreviatedMonthGenitiveNames[i][..^1]; // без точки в конце
                if (monthName.StartsWith(name, true, cultureInfo))
                {
                    return i + 1;
                }
            }
            return 0;
        }

        // Функция проверки подключения к БД
        internal static bool CheckConnection(string dbConnectionString) 
        {
            var result = false;
            try
            {
                var testConnection = new NpgsqlConnection(dbConnectionString);
                testConnection.Open();
                if (testConnection.State == ConnectionState.Open)
                    result = true;
                testConnection.Close();
            }
            catch 
            {
                result = false; // ошибка - что-то не так с подключением
            }
            return result;
        }
    }
}
