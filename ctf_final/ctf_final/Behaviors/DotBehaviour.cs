using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace ctf_final.Behaviors
{
    class DotBehaviour : Behavior<Entry>
    {
        public bool AddPercentage { get; set; }
        public bool AddMoneySign { get; set; }
        protected override void OnAttachedTo(Entry bindable)
        {
            bindable.TextChanged += OnEntryTextChanged;

            if (AddPercentage)
                bindable.Unfocused += AddPercentageToEnd;
            else if (AddMoneySign)
                bindable.Unfocused += AddMoneySignToEnd;

            base.OnAttachedTo(bindable);
        }

        private void AddPercentageToEnd(object sender, FocusEventArgs e)
        {
            var entry = sender as Entry;
            if (!entry.Text.Contains("%"))
            {
                if(entry.Text.Length < 1)
                    entry.Text = "0%";
                else
                    entry.Text += "%";
            }
        }

        private void AddMoneySignToEnd(object sender, FocusEventArgs e)
        {
            var entry = sender as Entry;
            var newText = entry.Text.Replace("R", "").Replace("$", "").Replace(" ", "");
            if (newText.Length < 1)
                entry.Text = "0 R$";
            else
                entry.Text = newText + " R$";
        }

        protected override void OnDetachingFrom(Entry bindable)
        {
            bindable.TextChanged -= OnEntryTextChanged;

            if (AddPercentage)
                bindable.Unfocused -= AddPercentageToEnd;
            if (AddPercentage)
                bindable.Unfocused -= AddMoneySignToEnd;

            base.OnDetachingFrom(bindable);
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            var entry = sender as Entry;
            entry.Text = entry.Text.Replace(",", ".");
        }
    }
}
