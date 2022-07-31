using ctf_final.Models;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.AdmContents.Students
{    
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RatingPage : ContentPage
    {
        User _user;
        SimplifiedUser u;

        bool hasToDownload = true;

        public RatingPage(SimplifiedUser user)
        {
            InitializeComponent();

            u = user;
            Title = user.Name;

            mobilityPicker.SelectedItem = "Pouco mobilizado";
        }

        public RatingPage(User user)
        {
            InitializeComponent();

            _user = user;
            Title = user.Name;

            hasToDownload = false;

            loadingSign.IsVisible = false;
            layout.IsVisible = true;

            mobilityPicker.SelectedItem = "Pouco mobilizado";
        }

        private async void CreateRating(object sender, EventArgs e)
        {
            if(_user.Ratings.Find(r => r.Date == DateTime.Now.ToString("dd/MM/yyyy")) != null)
            {
                await DisplayAlert("Erro", "Não é possível adicionar duas avaliações no mesmo dia. Considere alterar a avaliação já existente ao invés de criar uma nova.", "OK");
                return;
            }

            if (EveryEntryIsFilled())
            {
                await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                Rating r = new Rating
                {
                    Date = DateTime.Now.ToString("dd/MM/yyyy"),
                    Weight = weightEntry.Text,
                    Height = heightEntry.Text,
                    Mass = massEntry.Text.Replace("%", ""),
                    Fat = fatEntry.Text.Replace("%", ""),
                    Mobility = mobilityPicker.SelectedItem.ToString()
                };

                if (await AdmUtilities.CreateNewRating(_user.UserID.ToString(), r))
                {
                    await DisplayAlert("Sucesso", "Avaliação adicionada com sucesso!", "Ok");

                    if (_user.Ratings == null)
                        _user.Ratings = new List<Rating>() { r };
                    else
                        _user.Ratings.Insert(0, r);

                    await Navigation.PushAsync(new StudentRatings(_user));
                    Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                }
                else
                {
                    await DisplayAlert("Erro", "Não foi possível adicionar a avaliação, tente novamente mais tarde.", "Ok");
                }
                await PopupNavigation.Instance.PopAsync();
            }
            else
            {
                await DisplayAlert("Erro", "Todos os valores devem ser preenchidos.", "OK");
            }
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (hasToDownload)
            {
                try
                {
                    var query = await CrossCloudFirestore.Current
                                                    .Instance
                                                    .Collection("users")
                                                    .Document(u.UserID.ToString())
                                                    .GetAsync();
                    var us = query.ToObject<User>();

                    if (us == null)
                    {
                        await DisplayAlert("Erro", "Não foi possível encontrar o documento deste usuário, tente novamente e se o erro persistir contate o desenvolvedor...", "Ok");
                        await Navigation.PopAsync();
                    }

                    _user = us;

                    loadingSign.IsVisible = false;
                    layout.IsVisible = true;
                }
                catch
                {
                    await DisplayAlert("Erro", "Não foi possível encontrar o documento deste usuário, tente novamente e se o erro persistir contate o desenvolvedor...", "Ok");
                    await Navigation.PopAsync();
                }
            }
        }

        private bool EveryEntryIsFilled()
        {
            if (!string.IsNullOrEmpty(weightEntry.Text) && !string.IsNullOrEmpty(heightEntry.Text) &&
                !string.IsNullOrEmpty(fatEntry.Text.Replace("%", "")) && !string.IsNullOrEmpty(massEntry.Text.Replace("%", "")))
                return true;

            return false;
        }
    }
}