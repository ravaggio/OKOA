using Xamarin.Forms.Xaml;
using Rg.Plugins.Popup.Pages;

namespace ctf_final.PopupPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingPopup : PopupPage
    {
        public LoadingPopup()
        {
            InitializeComponent();
        }

        // Invoked when a hardware back button is pressed
        protected override bool OnBackButtonPressed()
        {
            // Return true if you don't want to close this popup page when a back button is pressed
            return true;
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            // Return false if you don't want to close this popup page when a background of the popup page is clicked
            return false;
        }
    }
}