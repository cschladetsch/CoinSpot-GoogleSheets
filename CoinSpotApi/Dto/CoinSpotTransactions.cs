﻿using System.Collections.Generic;
using System.Text;

namespace CoinSpotApi.Dto
{
    #pragma warning disable 649
    public class CoinSpotTransactions
    {
        public string status;
        public List<CoinSpotOrder> sellorders;
        public List<CoinSpotOrder> buyorders;

        public string BuyOrdersToString()
            => OrdersToString(buyorders);

        public string SellOrdersToString()
            => OrdersToString(sellorders);

        private string OrdersToString(IList<CoinSpotOrder> sellorders)
        {
            var sb = new StringBuilder();
            foreach (var order in sellorders)
            {
                sb.AppendLine(order.ToString());
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("BUY:");
            sb.Append(BuyOrdersToString());
            sb.AppendLine();
            sb.AppendLine("SELL:");
            sb.Append(SellOrdersToString());
            return sb.ToString();
        }
    }
}
