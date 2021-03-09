using System.Text;
using System.Collections.Generic;

namespace CoinSpotUpdater.CoinSpot.Dto
{
    public class CoinSpotAllPrices
    {
        public string status;
        public Dictionary<string, CoinSpotCoinStatus> prices;

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var kv in prices)
            {
                var h = kv.Value;
                sb.AppendLine($"{kv.Key.ToUpper(),6}: {h}");
            }
            return sb.ToString();
        }
    }
}
