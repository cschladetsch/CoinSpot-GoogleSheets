using System.Text;
using System.Collections.Generic;

namespace CoinSpotUpdater
{
    public class Balances
    {
        public class Holding
        {
            public float balance;
            public float audbalance;
            public float rate;
        }

        public string status;
        public List<Dictionary<string, Holding>> balances;

        public float GetTotal()
        {
            var total = 0.0f;
            foreach (var holding in balances)
            {
                foreach (var hold in holding)
                {
                    total += hold.Value.audbalance;
                }
            }
            return total;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var holding in balances)
            {
                foreach (var kv in holding)
                {
                    var h = kv.Value;
                    sb.AppendLine($"{kv.Key}: {h.balance,8:0.######} × {h.rate,10:C} = {h.audbalance:C} AUD");
                }
            }
            return sb.ToString();
        }
    }
}
