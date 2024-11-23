using Npgsql;
using System.Data;
using Dapper;

using static Dapper.SqlMapper;
using System.Collections.Generic;
using Telegram.Bot.Types;
using System.Collections;
using BotUtilities;


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

        public async Task<long> GetUserId(User user) 
        {
            long userId = -1;
            var status = await CheckUserStatus(user);
            if (status == 0)
            {
                return userId;
            }
            else if (status == 1)
            {
                using (IDbConnection db = new NpgsqlConnection(_connectionString))
                {
                    userId = await db.QuerySingleOrDefaultAsync<long>("SELECT user_id FROM routiner.t_users WHERE chat_id = @ChatId AND is_active ORDER BY 1 desc LIMIT 1",
                    new { ChatId = user.ChatId });
                }
            }
            else if (status == 2)
            {
                using (IDbConnection db = new NpgsqlConnection(_connectionString))
                {
                    userId = await db.QuerySingleOrDefaultAsync<long>("SELECT user_id FROM routiner.t_users WHERE telegram_account ilike @Username AND is_active ORDER BY 1 desc LIMIT 1",
                    new { Username = user.TelegramAccount });
                }
            }
            return userId;
        }

        public async Task<MenuState> GetUserStateById(User user)
        {
            MenuState state = MenuState.NewUser;

            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                state = await db.QuerySingleOrDefaultAsync<MenuState>("SELECT state FROM routiner.t_users WHERE user_id = @UserId LIMIT 1",
                new { UserId = user.UserId });
            }
            return state;
        }

        public async Task UpdateUserState(User user, MenuState state)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                await db.QuerySingleOrDefaultAsync<MenuState>("UPDATE routiner.t_users SET state = @State WHERE user_id = @UserId",
                new { State = state, UserId = user.UserId });
            }
        }

    }
}
