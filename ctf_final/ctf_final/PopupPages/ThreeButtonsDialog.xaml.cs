using Xamarin.Forms.Xaml;
using Rg.Plugins.Popup.Pages;
using Xamarin.Forms;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Services;

namespace ctf_final.PopupPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ThreeButtonsDialog : PopupPage
    {
        public ThreeButtonsDialog(string header, string text, string negative, string positive)
        {
            InitializeComponent();

            headerLabel.Text = header;

            textLabel.Text = text;

            negativeButton.Text = negative;
            positiveButton.Text = positive;
        }

        private int result;
        private TaskCompletionSource<bool> _TaskCompletion;
        public async Task<int> GetSelection()
        {
            _TaskCompletion = new TaskCompletionSource<bool>();
            if(await _TaskCompletion.Task)
            {
                await PopupNavigation.Instance.PopAsync();
                return result;
            }
            else
            {
                return 0;
            }
        }

        private void CancelButtonClicked(object sender, System.EventArgs e)
        {
            if (_TaskCompletion != null)
            {
                result = 0;
                _TaskCompletion.TrySetResult(true);

                _TaskCompletion = null;
            }
        }

        private void NegativeButtonClicked(object sender, System.EventArgs e)
        {
            if (_TaskCompletion != null)
            {
                result = 1;
                _TaskCompletion.TrySetResult(true);

                _TaskCompletion = null;
            }
        }

        private void PositiveButtonClicked(object sender, System.EventArgs e)
        {
            if(_TaskCompletion != null)
            {
                result = 2;
                _TaskCompletion.TrySetResult(true);

                _TaskCompletion = null;
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