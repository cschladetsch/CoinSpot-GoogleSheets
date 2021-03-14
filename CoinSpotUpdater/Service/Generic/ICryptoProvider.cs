namespace CoinSpotUpdater.Generic.Service
{
    interface ICryptoProvider
    {
        string Buy(string coin, float aud);
        string Sell(string coin, float aud);
        string Swap(string coinFrom, string CoinTo, float amountFrom);
        string SwapAud(string coinFrom, string CoinTo, float aud);
        string AddSellOrder(string coin, float aud, float coinAmountAud);
        string AddBuyOrder(string coin, float aud, float coinAmountAud);
        float GetTotalDeposited();
        float GetBalances();
        float GetBalance(string coin);
    }
}

