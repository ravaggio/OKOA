
using ctf_final.PlanModels;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System.Threading.Tasks;
using Xamarin.Forms.Xaml;

namespace ctf_final.PopupPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomPlan : PopupPage
    {
        private string _type = "";
        public CustomPlan(string type, Plan pl = null)
        {
            InitializeComponent();
            _type = type;

            header.Text = "Plano Custom - " + (_type == "Yoga" ? "Yoga" : _type == "Pilates" ? "Pilates" : "Treino");

            for (int i = 1; i < 7; i++)
                tpwPicker.Items.Add(i.ToString());

            floatPicker.Items.Add("Padrão");
            floatPicker.Items.Add("Flutuante");
            floatPicker.SelectedIndex = 0;

            durationPicker.Items.Add("Mensal");
            if (type == "Yoga")
                durationPicker.Items.Add("Trimestral");
            durationPicker.Items.Add("Semestral");
            if (type == "Custom")
                durationPicker.Items.Add("Anual");

            if (pl != null)
            {
                tpwPicker.SelectedIndex = pl.TimesPerWeek - 1;
                durationPicker.SelectedItem = pl.Duration;
                priceEntry.Text = pl.Price + " R$";
                floatPicker.SelectedItem = pl.IsFloating ? "Flutuante" : "Padrão";
            }
            else
            {
                tpwPicker.SelectedIndex = 0;
                durationPicker.SelectedIndex = 0;
                priceEntry.Placeholder = "0.00 R$";
            }
        }

        private Plan result;
        private TaskCompletionSource<bool> _TaskCompletion;
        public async Task<Plan> GetPlan()
        {
            _TaskCompletion = new TaskCompletionSource<bool>();
            if (await _TaskCompletion.Task)
            {
                await PopupNavigation.Instance.PopAsync();
                return result;
            }
            else
            {
                await PopupNavigation.Instance.PopAsync();
                return null;
            }
        }

        private void CancelButtonClicked(object sender, System.EventArgs e)
        {
            if (_TaskCompletion != null)
            {
                _TaskCompletion.TrySetResult(false);

                _TaskCompletion = null;
            }
        }

        private void PositiveButtonClicked(object sender, System.EventArgs e)
        {
            if (_TaskCompletion != null)
            {
                try
                {
                    if(floatPicker.SelectedItem.ToString() == "Flutuante" && durationPicker.SelectedItem.ToString() != "Mensal")
                    {
                        DisplayAlert("Plano flutuante", "O plano flutuante deve ser mensal.", "OK");
                        return;
                    }

                    var duration = durationPicker.SelectedItem.ToString();
                    var tpw = tpwPicker.SelectedItem.ToString();
                    var price = priceEntry.Text.Replace(" R$", "");
                    var isFloating = floatPicker.SelectedItem.ToString() == "Flutuante";
                    result = new Plan
                    {
                        Type = _type,
                        IsYoga = _type == "Yoga",
                        IsPilates = _type == "Pilates",
                        Duration = duration,
                        IsFloating = isFloating,
                        TimesPerWeek = int.Parse(tpw),
                        Price = double.Parse(price)
                    };

                    _TaskCompletion.TrySetResult(true);

                    _TaskCompletion = null;
                }
                catch
                {
                    _TaskCompletion.TrySetResult(false);

                    _TaskCompletion = null;
                }
            }
        }

        protected override void OnDisappearingAnimationEnd()
        {
            base.OnDisappearingAnimationEnd();
            if (_TaskCompletion != null)
            {
                _TaskCompletion.TrySetResult(false);

                _TaskCompletion = null;
            }
        }
    }
}