using Npgsql;
using System.Data;
using Dapper;

using static Dapper.SqlMapper;
using System.Collections.Generic;
using Telegram.Bot.Types;
using System.Collections;


namespace UserInformation
{
    public class UserInfo
    {
        private const int _defaultLimit = 5;
        private string _connectionString = null;
        public UserInfo(string connection)
        {
            _connectionString = connection;
        }

        public async Task<int> CreateUser(User user)
        {
            var query = "insert into routiner.t_users (name, surname, telegram_account, is_active, chat_id)"
                      + "values (@Name, @Surname, @TelegramAccount, @IsActive, @ChatId)";
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                var rowsAffected = await db.ExecuteAsync(query, new { Name = user.Name, Surname = user.Surname, TelegramAccount = user.TelegramAccount, IsActive = user.IsActive, ChatId = user.ChatId });
                return rowsAffected;
            }
            return -1;
        }

        public async Task<int> CheckUserStatus(User user)
        {
            var userStatus = 0;
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                userStatus = await db.QuerySingleOrDefaultAsync<int>("select routiner.user_status(@ChatId, @Username)",
                new { ChatId = user.ChatId, Username = user.TelegramAccount });
            }
            return userStatus;
        }


        
    }
}
