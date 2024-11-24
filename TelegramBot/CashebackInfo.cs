using BankInformation;
using BotUtilities;
using Dapper;
using Npgsql;
using System.Data;

namespace CashbackInformation
{
    public class CashebackInfo
    {
        private string? _connectionString = null;
        public CashebackInfo(string connection) => _connectionString = connection;

        public List<Casheback> GetUserCashebacks(long userId, DateTime ? date)
        {
            var findDate = date ?? DateTime.Now;
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                return db.Query<Casheback>("select b.short_name BankName, c.bank_id BankId, c.category Category, c.rate Rate, c.user_id UserId"
                                          + " from routiner.t_cashbacks c"
                                          + " join routiner.t_banks b on b.bank_id = c.bank_id"
                                          + " where c.user_id = @UserId and date_trunc('month', @Date) between c.date_begin and c.date_end"
                                          + " order by date_begin, b.short_name, category",
                    new { UserId = userId, Date = findDate }).ToList();
            }
        }

        public async Task AddCashback(Casheback cashback, DateTime dateStart) 
        {
            var dateBegin = new DateTime(dateStart.Year, dateStart.Month, 1);
            var dateEnd = dateBegin.AddMonths(1).AddDays(-1);
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                await db.QuerySingleOrDefaultAsync<MenuState>("insert into routiner.t_cashbacks (category, rate, date_begin, date_end, user_id, bank_id)" 
                                                            + "values (@Category, @Rate, @DateBegin, @DateEnd, @UserId, @BankId)",
                new { cashback.Category, cashback.Rate, DateBegin = dateBegin, DateEnd = dateEnd, cashback.UserId, cashback.BankId });
            }
        }
    }
}