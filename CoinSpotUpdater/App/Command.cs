using System;

namespace CoinSpotUpdater.App
{
    class Command
    {
        public string Text;
        public string Description;
        public Action<string[]> Action;

        public Command(string text, string desc, Action<string[]> action)
        {
            Text = text;
            Description = desc;
            Action = action;
        }
    }
}