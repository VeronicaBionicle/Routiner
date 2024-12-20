﻿using Npgsql;
using System.Data;
using Dapper;

using static Dapper.SqlMapper;
using System.Collections.Generic;
using Telegram.Bot.Types;
using System.Collections;
using BotUtilities;
using BankInformation;


namespace UserInformation
{
    public class UserInfo
    {
        private readonly string ? _connectionString = null;

        public UserInfo(string connection) => _connectionString = connection;

        public async Task<int> CreateUser(User user)
        {
            var query = "insert into routiner.t_users (name, surname, telegram_account, is_active, chat_id, state)"
                      + "values (@Name, @Surname, @TelegramAccount, @IsActive, @ChatId, @State)"
                      + "returning user_id";
            try
            {
                using (IDbConnection db = new NpgsqlConnection(_connectionString))
                {
                    var newId = await db.ExecuteScalarAsync<int>(query, new { user.Name, user.Surname, user.TelegramAccount, user.IsActive, user.ChatId, State = MenuState.UserRegistered });
                    return newId;
                }
            }
            catch 
            {
                return -1; // не удалось вставить
            }
        }

        public async Task<int> CheckUserStatus(User user)
        {
            var userStatus = 0;
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                userStatus = await db.QuerySingleOrDefaultAsync<int>("select routiner.user_status(@ChatId, @Username)",
                new { user.ChatId, Username = user.TelegramAccount });
            }
            return userStatus;
        }

        public async Task<int> GetUserId(User user) 
        {
            int userId = -1;
            var status = await CheckUserStatus(user);
            if (status == 0)
            {
                return userId;
            }
            else if (status == 1)
            {
                using (IDbConnection db = new NpgsqlConnection(_connectionString))
                {
                    userId = await db.QuerySingleOrDefaultAsync<int>("SELECT user_id FROM routiner.t_users WHERE chat_id = @ChatId AND is_active ORDER BY 1 desc LIMIT 1",
                    new { user.ChatId });
                }
            }
            else if (status == 2)
            {
                using (IDbConnection db = new NpgsqlConnection(_connectionString))
                {
                    userId = await db.QuerySingleOrDefaultAsync<int>("SELECT user_id FROM routiner.t_users WHERE telegram_account ilike @Username AND is_active ORDER BY 1 desc LIMIT 1",
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
                new { user.UserId });
            }
            return state;
        }

        public async Task UpdateUserState(User user, MenuState state)
        {
            user.State = state;
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                await db.QuerySingleOrDefaultAsync<MenuState>("UPDATE routiner.t_users SET state = @State WHERE user_id = @UserId",
                new { State = state, user.UserId });
            }
        }

        public async Task UpdateUserBankId(User user, int bankId) 
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                await db.QuerySingleOrDefaultAsync<MenuState>("call routiner.save_user_bank_id(@UserId, @BankId)",
                new {user.UserId, BankId = bankId });
            }
        }

        public async Task<int> GetUserBankId(User user)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                return await db.QuerySingleOrDefaultAsync<int>("select bank_id BankId from routiner.t_user_bank_choose where user_id = @UserId",
                new { user.UserId });
            }
        }

        public List<(int, string)> GetUsersInYourGroup(User user)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                return db.Query<(int, string)>("select distinct gr.user_id, coalesce(u.name, u.telegram_account) name from routiner.t_user_group gr"
                                   + " join routiner.t_users u on u.user_id = gr.user_id"
                                   + " where exists (select 1 from routiner.t_user_group g where g.user_id = @UserId)",
                    new { user.UserId }).ToList();
            }
        }

    }
}
