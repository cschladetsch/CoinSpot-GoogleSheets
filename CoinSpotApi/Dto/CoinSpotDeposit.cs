﻿using System;

namespace CoinSpotApi.Dto
{
    public class CoinSpotDeposit
    {
        public float amount;
        public DateTime created;
        public string status;
        public string type;
        public string reference;

        public override string ToString()
            =>  $"{amount:C} on {created}";
    }
}