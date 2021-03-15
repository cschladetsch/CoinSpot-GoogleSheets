namespace CoinSpotApi.Dto
{
    public class CoinSpotSellOrder : CoinSpotQuickSellOrder
    {
        public float rate;

        public override string ToString()
            => $"type={cointype}, amount={amount}, rate={rate}";
    }
}