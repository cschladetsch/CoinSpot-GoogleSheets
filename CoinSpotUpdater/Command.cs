using System;

namespace CoinSpotUpdater
{
    class Command
    {
        public string Text;
        public string Description;
        public Action Action;

        public Command(string text, string desc, Action action)
        {
            Text = text;
            Description = desc;
            Action = action;
        }
    }
}