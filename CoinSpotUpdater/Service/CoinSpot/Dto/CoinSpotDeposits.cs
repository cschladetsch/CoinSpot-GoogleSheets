using System;
using System.Collections.Generic;
using System.Text;

namespace CoinSpotUpdater.CoinSpot.Dto
{
    class CoinSpotDeposits
    {
        public string status;
        public List<CoinSpotDeposit> deposits;

        public float GetTotalDeposited()
        {
            float total = 0;
            var earliest = Earliest();
            foreach (var deposit in deposits)
            {
                if (deposit.created > earliest)
                    total += deposit.amount;
            }
            return total;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var earliest = Earliest();
            foreach (var deposit in deposits)
            {
                if (deposit.created > earliest)
                    sb.AppendLine(deposit.ToString());
            }
            return sb.ToString();
        }

        private static DateTime Earliest()
        {
            return DateTime.Parse("2020-11-01T00:00:00Z");
        }
    }
}
