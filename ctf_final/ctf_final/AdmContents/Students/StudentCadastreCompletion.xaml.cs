using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.AdmContents.Students
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StudentCadastreCompletion : ContentPage
    {
        private User user;
        public StudentCadastreCompletion(User u)
        {
            InitializeComponent();

            user = u;
            IDLabel.Text = u.UserID.ToString();      
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if(AdmUtilities.GetNeedClassSetup(user))
                    for (int i = 0; i <= Navigation.NavigationStack.Count - 1; i++)
                        Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
            }catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
 