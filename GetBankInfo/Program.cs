using System.Collections.Generic;
using System.Linq;

namespace GetBankInfo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var _connectionString = "Server=127.0.0.1; Port=5432; Database=RoutinerDB; User ID=veronika_nenyuk; Password=MyAdminPass2708;";
            var bankRep = new BankInfo(_connectionString);

            var banks = bankRep.GetBanks();
            foreach (var bank in banks)
            {
                Console.WriteLine($"{bank.Id}\t{bank.RCBic}\t{bank.FullName}");
            }

            banks = bankRep.GetBanksByName("прим");
            foreach (var bank in banks)
            {
                Console.WriteLine($"{bank.Id}\t{bank.RCBic}\t{bank.FullName}");
            }

            banks = bankRep.GetBanksByRCBic("044525");
            foreach (var bank in banks)
            {
                Console.WriteLine($"{bank.Id}\t{bank.RCBic}\t{bank.FullName}");
            }
        }
    }
}
