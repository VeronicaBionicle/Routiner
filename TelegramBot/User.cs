using BotUtilities;

namespace UserInformation
{
    public class User
    {
        private long _userId;
        public string Name;
        public string Surname;
        private string _telegramAccount;
        private bool _isActive;
        private long _chatId;
        public MenuState State;
        public string TelegramAccount { get { return _telegramAccount; } }
        public long ChatId { get { return _chatId; } }
        public bool IsActive { get { return _isActive; } }
        public long UserId { get; set; }

        public User(string name, string surname, string telegramAccount, long chatId) 
        {
            Name = name;
            Surname = surname;
            _telegramAccount = telegramAccount;
            _isActive = true;
            _chatId = chatId;
            State = MenuState.NewUser;
        }
    }
}