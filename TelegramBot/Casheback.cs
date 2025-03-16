using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CashbackInformation
{
    public class Casheback 
    {
        public int CashebackId;
        public string ? BankName;
        public int BankId;
        public string Category;
        public double Rate;
        public long UserId;

        public Casheback(int bankId, string category, double rate, int userId) 
        {
            BankId = bankId;
            Category = category;
            Rate = rate;
            UserId = userId;
        }

        public Casheback(){ } // Нужен для выгрузки в Dapper
    }
}