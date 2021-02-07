using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;

using Newtonsoft.Json;

namespace CoinSpotUpdater
{
    // see https://www.coinspot.com.au/api for full api
    class CoinspotService
    {
        private readonly string _key;
        private readonly string _secret;
        private string _baseUrl;
        private string _baseReadOnlyUrl = "/api/ro/my/";

        public CoinspotService()
        {
            _key = ConfigurationManager.AppSettings.Get("coinSpotKey");
            _secret = ConfigurationManager.AppSettings.Get("coinSpotSecret");
            _baseUrl = ConfigurationManager.AppSettings.Get("coinSpotSite");
        }

        internal Balances GetMyBalances()
        {
            var json = GetMyBalancesJson();
            return JsonConvert.DeserializeObject<Balances>(json);
        }

        public float GetPortfolioValue()
        {
            return GetMyBalances().GetTotal();
        }

        public string GetMyBalancesJson(string JSONParameters = "{}")
        {
            return RequestCSJson(_baseReadOnlyUrl + "balances", JSONParameters);
        }

        public string GetCoinBalanceJson(string coinType)
        {
            return RequestCSJson(_baseReadOnlyUrl + "balances/:" + coinType);
        }

        private string RequestCSJson(string endPointUrl, string JSONParameters = "{}")
        {
            return CallAPI(endPointUrl, JSONParameters);
        }

        public string CallAPI(string endPoint, string jsonParameters)
        {
            var endpointURL = _baseUrl + endPoint;
            long nonce = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            var json = jsonParameters.Replace(" ", "");
            var nonceParameter = "\"nonce\"" + ":" + nonce;
            if (json != "{}")
            {
                nonceParameter += ",";
            }

            var parameters = jsonParameters.Trim().Insert(1, nonceParameter);
            var parameterBytes = Encoding.UTF8.GetBytes(parameters);
            var signedData = SignData(parameterBytes);

            //Print(endpointURL);

            WebRequest request = HttpWebRequest.Create(endpointURL);
            request.Method = "POST";
            request.Headers.Add("key", _key);
            request.Headers.Add("sign", signedData.ToLower());
            request.ContentType = "application/json";
            request.ContentLength = parameterBytes.Length;

            string responseText;
            try
            {
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(parameterBytes, 0, parameterBytes.Length);
                }
                responseText = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                responseText = "{\"exception\"" + ":\"" + ex.ToString() + "\"}";
            }

            return responseText;
        }

        private static void Print(string endpointURL)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("     POST: " + endpointURL);
            Console.ForegroundColor = color;
        }

        private string SignData(byte[] JSONData)
        {
            var HMAC = new HMACSHA512(Encoding.UTF8.GetBytes(_secret));
            var EncodedBytes = HMAC.ComputeHash(JSONData);

            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i <= EncodedBytes.Length - 1; i++)
                stringBuilder.Append(EncodedBytes[i].ToString("X2"));

            return stringBuilder.ToString();
        }
    }
}
