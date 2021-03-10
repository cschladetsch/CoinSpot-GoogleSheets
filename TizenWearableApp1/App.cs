using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace TizenWearableApp1
{
    public class App : Application
    {
        CoinSpotUpdater.CoinSpot.CoinspotService _coinSpotService;
        Label _label;

        public App()
        {
            _label = new Label
            {
                HorizontalTextAlignment = TextAlignment.Center,
                Text = $"Connecting..."
            };
            MainPage = new ContentPage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = {
                        _label
                    }
                }
            };
        }

        protected override void OnStart()
        {
            var key = "bd2e2aee5b9ab82f5061fd7651f30d80";
            var secret = "L03E9L2J9VQ7TU635QA6FTUFP6UUP63FUB9XPMD3JP3KX6B61UU9NK7V3HWN04WFMTBF9PRLJEFAEFQV";
            var baseUrl = "https://www.coinspot.com.au";
            _coinSpotService = new CoinSpotUpdater.CoinSpot.CoinspotService(key, secret, baseUrl);
            Start();
        }

        private async void Start()
        {
            _label.Text = "Reading...";
            var bal = await _coinSpotService.GetPortfolioValue();
            _label.Text = $"{bal} AUD";
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
