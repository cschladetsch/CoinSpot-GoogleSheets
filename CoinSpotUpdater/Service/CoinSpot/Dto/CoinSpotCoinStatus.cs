namespace CoinSpotUpdater.CoinSpot.Dto
{
    public class CoinSpotCoinStatus
    {
        public float bid;
        public float ask;
        public float last;

        public override string ToString()
        {
            return $"bid={bid,11:C}, ask={ask,10:C}, last={last,10:C}";
        }
    }
}
