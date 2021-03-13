namespace CoinSpotUpdater.CoinSpot.Dto
{
    public class CoinSpotQuickSellOrder
    {
        public string cointype;
        public float amount;

        public override string ToString()
            => $"type={cointype}, amount={amount}";
    }
}