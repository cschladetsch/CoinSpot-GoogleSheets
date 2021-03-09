using System;

namespace CoinSpotUpdater.CoinSpot.Dto
{
    public class CoinSpotOrder
    {
        public bool otc;
        public string market;
        public float amount;
        public DateTime created;
        public float audfeeExGst;
        public float audGst;
        public float audtotal;

        public override string ToString()
            => $"market={market}, amount={amount}, audtotal={audtotal}";
    }
}