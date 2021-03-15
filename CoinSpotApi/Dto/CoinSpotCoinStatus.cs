namespace CoinSpotApi.Dto
{
    public class CoinSpotCoinStatus
    {
        public float bid;
        public float ask;
        public float last;

        public override string ToString()
            => $"bid={bid,11:C}, ask={ask,10:C}, last={last,10:C}";
    }
}
