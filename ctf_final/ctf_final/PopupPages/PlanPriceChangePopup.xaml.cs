using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ctf_final.PopupPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlanPriceChangePopup : PopupPage
    {
        PlanModels.Plan plan;
        bool btnClicked = false;
        public PlanPriceChangePopup(PlanModels.Plan pl)
        {
            InitializeComponent();
            plan = pl;

            planResume.Text = (pl.IsYoga ? "Yoga" : pl.Type) + " - " + pl.TimesPerWeek + "x por semana, " + pl.Duration.ToLower() + ".";
            priceEntry.Text = pl.Price + " R$";
            priceEntry.TextChanged += PriceEntry_TextChanged;
        }

        private void PriceEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(priceEntry.Text.Replace("R$", "") != plan.Price.ToString())
                finishBtn.IsEnabled = true;
            else
                finishBtn.IsEnabled = false;
        }

        private async void CancelBtn(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }

        private async void FinishBtn(object sender, EventArgs e)
        {
            if (!btnClicked)
            {
                btnClicked = true;
                await PopupNavigation.Instance.PushAsync(new LoadingPopup());

                try
                {
                    //--- SERVER ---

                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    var pricesDoc = CrossCloudFirestore.Current.Instance.Collection("plans").Document("prices");

                    var newPrice = priceEntry.Text.Replace(" R$", "").Replace(".",",").Replace(".00", "");
                    //check if valid

                    var startString = (plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : plan.Type) + "/" + plan.Duration + "/" + plan.TimesPerWeek;
                    var oldPriceString = startString + "@" + plan.Price.ToString().Replace(".", ",").Replace(".00", ""); 
                    var newPriceString = startString + "@" + newPrice;

                    batch.Update(pricesDoc, "PricesList", FieldValue.ArrayRemove(oldPriceString));
                    batch.Update(pricesDoc, "PricesList", FieldValue.ArrayUnion(newPriceString));

                    var planTypeText = plan.IsYoga ? "YogaPlan" : plan.IsPilates ? "PilatesPlan" : "TrainPlan";
                    var userDocs = CrossCloudFirestore.Current.Instance.Collection("users").WhereEqualsTo(new FieldPath("UserPlan", planTypeText), plan);

                    var newPriceDouble = double.Parse(newPrice);
                    var foundDocs = await userDocs.GetAsync();
                    foreach(var doc in foundDocs.Documents)
                        batch.Update(doc.Reference, new FieldPath("UserPlan", planTypeText, "Price"), newPriceDouble); 

                    await batch.CommitAsync();

                    //--- SERVER ---

                    //--- LOCAL ---

                    plan.Price = newPriceDouble;

                    (Application.Current as App).PlanPrices[startString] = newPriceDouble;
                    (Application.Current as App).PlanPrices = (Application.Current as App).PlanPrices;
                    await (Application.Current as App).SavePropertiesAsync();

                    //--- LOCAL ---

                    //UI

                    MessagingCenter.Send(new PageUpdateMessage() { Command = priceEntry.Text }, "UpdatePlanPickerPage");
                    await DisplayAlert("Sucesso", "Preço alterado com sucesso!", "Ok");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    await DisplayAlert("Erro", "Não foi possível alterar o preço, tente novamente mais tarde.", "Ok");
                }
                
                await PopupNavigation.Instance.PopAllAsync();
            }
            btnClicked = false;
        }
    }
}