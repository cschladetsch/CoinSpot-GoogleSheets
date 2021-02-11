using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;

using Newtonsoft.Json;
using CoinSpotUpdater.CoinSpot.Dto;

namespace CoinSpotUpdater.CoinSpot
{
    // see https://www.coinspot.com.au/api for full api
    class CoinspotService
    {
        private readonly string _key;
        private readonly string _secret;
        private readonly string _baseUrl;
        private const string _baseReadOnlyUrl = "/api/ro/my/";

        public CoinspotService()
        {
            _key = FromAppSettings("coinSpotKey");
            _secret = FromAppSettings("coinSpotSecret");
            _baseUrl = FromAppSettings("coinSpotSite");
        }

        private string FromAppSettings(string key)
            => ConfigurationManager.AppSettings.Get(key);

        public float GetPortfolioValue()
            => GetMyBalances().GetTotal();

        public CoinSpotBalances GetMyBalances()
            => JsonConvert.DeserializeObject<CoinSpotBalances>(GetMyBalancesJson());

        public string GetMyBalancesJson(string JSONParameters = "{}")
            => RequestCSJson(_baseReadOnlyUrl + "balances", JSONParameters);

        public string GetCoinBalanceJson(string coinType)
            => RequestCSJson(_baseReadOnlyUrl + "balances/:" + coinType);

        private string RequestCSJson(string endPointUrl, string JSONParameters = "{}")
            => ApiCall(endPointUrl, JSONParameters);

        internal CoinSpotAllPrices GetAllPrices()
        {
            var json = PublicApiCall("/pubapi/latest");
            var result = JsonConvert.DeserializeObject<CoinSpotAllPrices>(json);
            return result;
        }

        public string PublicApiCall(string url)
        {
            var call = _baseUrl + url;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(call);
            using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public string ApiCall(string endPoint)
            => ApiCall(endPoint, "{}");

        public string ApiCall(string endPoint, string jsonParameters)
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
            var request = MakeRequest(endpointURL, parameterBytes, signedData);

            return MakeCall(parameterBytes, request);
        }

        private HttpWebRequest MakeRequest(string endpointURL, byte[] parameterBytes, string signedData)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(endpointURL);
            request.KeepAlive = false;
            request.Method = "POST";
            request.Headers.Add("key", _key);
            request.Headers.Add("sign", signedData.ToLower());
            request.ContentType = "application/json";
            request.ContentLength = parameterBytes.Length;
            return request;
        }

        private static string MakeCall(byte[] parameterBytes, HttpWebRequest request)
        {
            string responseText;
            try
            {
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(parameterBytes, 0, parameterBytes.Length);
                    stream.Close();
                }
                using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    responseText = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                responseText = "{\"exception\"" + ":\"" + ex.ToString() + "\"}";
            }

            return responseText;
        }

        private string SignData(byte[] JSONData)
        {
            var encodedBytes = new HMACSHA512(Encoding.UTF8.GetBytes(_secret)).ComputeHash(JSONData);
            var sb = new StringBuilder();
            for (int i = 0; i <= encodedBytes.Length - 1; i++)
            {
                sb.Append(encodedBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

    }
}
