using System.Collections.Generic;

namespace CryptoHelper.App
{
    partial class SheetUpdater
    {
        class RecordedHolding
        {
            public string Coin;
            public float Holding;
            public float BuyInPrice;
            public float CurrentPrice;

            public RecordedHolding(IList<object> list)
            {
                Coin = (string)list[0];
                Holding = float.Parse((string)list[1]);
                BuyInPrice = float.Parse((string)list[2]);
                CurrentPrice = float.Parse((string)list[3]);
            }

            public override string ToString()
            {
                return $"{Coin} {Holding} {BuyInPrice} {CurrentPrice}";
            }
        }

        class RecordedHoldings
        {
            public Dictionary<string, RecordedHolding> Holdings = new Dictionary<string, RecordedHolding>();

            public void Add(RecordedHolding holding)
            {
                var coin = holding.Coin;
                if (!Holdings.TryGetValue(coin, out var value))
                {
                    Holdings.Add(coin, holding);
                }
                Holdings[coin] = holding;
            }
        }
    }
}
