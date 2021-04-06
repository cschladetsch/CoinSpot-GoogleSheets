using System.Text;
using System.Collections.Generic;

namespace CoinSpotApi.Dto
{
    public class CoinSpotBalances
    {
        public string status;
        public List<Dictionary<string, CoinSpotHolding>> balances;

        private readonly string _myCurrency;

        public List<Dictionary<string, CoinSpotHolding>> Coins => balances;

        public CoinSpotBalances()
            => _myCurrency = CoinspotService.GetAppSetting("myCurrency");

        public bool SetBalance(string coin, CoinSpotHolding holding)
        {
            foreach (var hold in balances)
            {
                if (hold.ContainsKey(coin))
                {
                    hold[coin] = holding;
                    return true;
                }
            }

            var dict = new Dictionary<string, CoinSpotHolding>
            {
                { coin, holding }
            };
            balances.Add(dict);
            return false;
        }

        public float GetTotal()
        {
            var total = 0.0f;
            foreach (var holding in balances)
            {
                foreach (var hold in holding)
                {
                    if (hold.Key != _myCurrency)
                    {
                        total += hold.Value.audbalance;
                    }
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
                    sb.AppendLine($"{kv.Key,5}: {h.balance,8:0.######} × {h.rate,10:C} = {h.audbalance:C}");
                }
            }
            return sb.ToString();
        }

        public bool HasCoin(string coin)
        {
            return GetHolding(coin) != null;
        }

        public void AddCoin(string coin, CoinSpotHolding holding)
        {
            if (HasCoin(coin))
            {
                return;
            }

            var newHolding = new Dictionary<string, CoinSpotHolding> { { coin, holding } };
            balances.Add(newHolding);
        }

        public bool SetHolding(string coin, CoinSpotHolding holding)
        {
            var hold = GetHolding(coin);
            if (hold == null)
                return false;

            hold.balance = holding.balance;
            hold.audbalance = holding.audbalance;
            hold.rate = holding.rate;
            return true;
        }

        public CoinSpotHolding GetHolding(string coin)
        {
            foreach (var holding in balances)
            {
                foreach (var kv in holding)
                {
                    if (kv.Key == coin)
                        return kv.Value;
                }
            }

            return null;
        }
    }
}
