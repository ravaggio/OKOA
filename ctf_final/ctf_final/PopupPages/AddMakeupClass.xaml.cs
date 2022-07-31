using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.PopupPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddMakeupClass : PopupPage
    {
        readonly User user;
        string type = "";
        public AddMakeupClass(User u)
        {
            InitializeComponent();

            user = u;
            repoLabel.Text = "Controle de reposições";

            if (u.UserPlan.TrainPlan != null)
                typePicker.Items.Add("Treino");
            if (u.UserPlan.YogaPlan != null)
                typePicker.Items.Add("Yoga");
            if (u.UserPlan.PilatesPlan != null)
                typePicker.Items.Add("Pilates");

            typePicker.IsVisible = true;
            typePicker.SelectedIndex = 0;

            if(u.MakeupClasses > 0 || u.MakeupClassesYoga > 0 || u.MakeupClassesPilates > 0)
            {
                modeLabel.IsVisible = false;
                modePicker.IsVisible = true;
                modePicker.SelectedItem = "Adicionar";
            }
            else
            {
                modeLabel.Text = "Adicionar";
            }
        }
        private async void CancelBtn(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }

        private async void AddBtn(object sender, EventArgs e)
        {
            try
            {
                await PopupNavigation.Instance.PushAsync(new LoadingPopup());

                if(typePicker.IsVisible == true)
                    type = typePicker.SelectedItem.ToString();

                var batch = CrossCloudFirestore.Current.Instance.Batch();
                var userDoc = CrossCloudFirestore.Current
                                       .Instance
                                       .Collection("users")
                                       .Document(user.UserID.ToString());

                var todayDate = SharedUtilities.GetTodayDateTime();
                var stringDate = todayDate.ToString("yyyy-MM-dd");
                if (!modePicker.IsVisible || modePicker.SelectedItem.ToString() == "Adicionar")
                {
                    if (type == "Treino")
                    {
                        if (user.MCTrainDates.Count > 0)
                        {
                            user.MCTrainDates.Sort();
                            var fd = user.MCTrainDates.FindAll(d => d.StartsWith(todayDate.ToString("yyyy-MM-dd")));
                            if (fd == null)
                                stringDate += "@1";
                            else
                                stringDate = stringDate + "@" + (1 + fd.Count);

                            var i = 0;
                            while (user.MCTrainDates.Contains(stringDate))
                            {
                                stringDate = todayDate.ToString("yyyy-MM-dd") + "@" + (i + fd.Count);
                                i++;
                            }
                        }
                        else
                            stringDate += "@1";
                    }
                    else if (type == "Yoga")
                    {
                        if (user.MCYogaDates.Count > 0)
                        {
                            user.MCYogaDates.Sort();
                            var fdy = user.MCYogaDates.FindAll(d => d.StartsWith(todayDate.ToString("yyyy-MM-dd")));
                            if (fdy == null)
                                stringDate += "@1";
                            else
                                stringDate = stringDate + "@" + (1 + fdy.Count);

                            var i = 0;
                            while (user.MCYogaDates.Contains(stringDate))
                            {
                                stringDate = todayDate.ToString("yyyy-MM-dd") + "@" + (i + fdy.Count);
                                i++;
                            }
                        }
                        else
                            stringDate += "@1";
                    }
                    else if (type == "Pilates")
                    {
                        if (user.MCPilatesDates.Count > 0)
                        {
                            user.MCPilatesDates.Sort();
                            var fdy = user.MCPilatesDates.FindAll(d => d.StartsWith(todayDate.ToString("yyyy-MM-dd")));
                            if (fdy == null)
                                stringDate += "@1";
                            else
                                stringDate = stringDate + "@" + (1 + fdy.Count);

                            var i = 0;
                            while (user.MCPilatesDates.Contains(stringDate))
                            {
                                stringDate = todayDate.ToString("yyyy-MM-dd") + "@" + (i + fdy.Count);
                                i++;
                            }
                        }
                        else
                            stringDate += "@1";
                    }

                    if (type == "Treino")
                    {
                        user.MakeupClasses++;
                        user.MCTrainDates.Add(stringDate);
                        batch.Update(userDoc, "MakeupClasses", user.MakeupClasses);
                        batch.Update(userDoc, "MCTrainDates", FieldValue.ArrayUnion(stringDate));
                    }
                    else if(type == "Yoga")
                    {
                        user.MakeupClassesYoga++;
                        user.MCYogaDates.Add(stringDate);
                        batch.Update(userDoc, "MakeupClassesYoga", user.MakeupClassesYoga);
                        batch.Update(userDoc, "MCYogaDates", FieldValue.ArrayUnion(stringDate));
                    }
                    else if (type == "Pilates")
                    {
                        user.MakeupClassesPilates++;
                        user.MCPilatesDates.Add(stringDate);
                        batch.Update(userDoc, "MakeupClassesPilates", user.MakeupClassesPilates);
                        batch.Update(userDoc, "MCPilatesDates", FieldValue.ArrayUnion(stringDate));
                    }
                }
                else
                {
                    var listOfDates = type == "Treino" ? user.MCTrainDates : type == "Yoga" ? user.MCYogaDates : user.MCPilatesDates;
                    var makeupClasses = type == "Treino" ? user.MakeupClasses : type == "Yoga" ? user.MakeupClassesYoga : user.MakeupClassesPilates;
                    var datePathString = type == "Treino" ? "MCTrainDates" : type == "Yoga" ? "MCYogaDates" : "MCPilatesDates";
                    var mcPathString = type == "Treino" ? "MakeupClasses" : type == "Yoga" ? "MakeupClassesYoga" : "MakeupClassesPilates";
                    listOfDates.Sort();
                    
                    batch.Update(userDoc, datePathString, FieldValue.ArrayRemove(listOfDates[0]));
                    batch.Update(userDoc, mcPathString, makeupClasses-1);

                    listOfDates.Remove(listOfDates[0]);
                    makeupClasses--;
                }

                await batch.CommitAsync();

                await PopupNavigation.Instance.PopAsync();
                await DisplayAlert("Sucesso!", "Reposição adicionada com sucesso!", "OK");
                await PopupNavigation.Instance.PopAsync();
            }
            catch(Exception ex)
            {
                await PopupNavigation.Instance.PopAsync();
                await DisplayAlert("Erro", "Não foi possível adicionar reposição. Erro desconhecido: " + ex, "OK");
                await PopupNavigation.Instance.PopAsync();
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}