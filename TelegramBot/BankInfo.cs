using Dapper;
using System.Data;
using Npgsql;

namespace BankInformation
{
    public class BankInfo
    {
        private const string _getBankStr = "select bank_id Id, short_name ShortName, full_name FullName, rcbic RCBic from routiner.t_banks";
        private const int _defaultLimit = 5;
        private string ? _connectionString = null;
        public BankInfo(string connection) => _connectionString = connection;
        public List<Bank> GetBanks(int limit = _defaultLimit)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                return db.Query<Bank>(_getBankStr + " order by short_name asc limit @Limit",
                    new { Limit = limit }).ToList();
            }
        }
        public List<Bank> GetBanksByName(string ? name, int limit = _defaultLimit)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                return db.Query<Bank>(_getBankStr + " where short_name ilike @Name order by short_name asc limit @Limit",
                    new { Name = $"%{name}%", Limit = limit }).ToList();
            }
        }
        public List<Bank> GetBanksByRCBic(string ? RCBic, int limit = _defaultLimit)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                return db.Query<Bank>(_getBankStr + " where rcbic ilike @RCBic order by rcbic asc limit @Limit",
                    new { RCBic = $"%{RCBic}%", Limit = limit }).ToList();
            }
        }

        public List<Bank> GetBanksBy(string ? str, int byNameOrBic = 0, int limit = _defaultLimit)
        {
            if (byNameOrBic == 0)
            {
                return GetBanksByName(str, limit);
            } 
            else
            {
                return GetBanksByRCBic(str, limit);
            }
        }
    }
}
