using ctf_final.PlanModels;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;

using static ctf_final.AppController;

namespace ctf_final.AdmContents.Students
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlanConfigPage : ContentPage
    {
        Button planAbscenceBtn;
        User user;

        readonly List<App.SelectedSchedules> selectedTrains = new List<App.SelectedSchedules>();
        readonly List<App.SelectedSchedules> selectedYoga = new List<App.SelectedSchedules>();
        readonly List<App.SelectedSchedules> selectedPilates = new List<App.SelectedSchedules>();

        List<View> showingScheduleLayouts = new List<View>();

        class PlanViewProperties
        {
            public Label expDate { get; set; }
            public Label tpw { get; set; }
            public Label price { get; set; }
            public Label type { get; set; }
            public DatePicker dp { get; set; }
            public Switch sw { get; set; }
            public Button rn_btn { get; set; }

            public PlanViewProperties(Label expDate, Label tpw, Label price, Label type, DatePicker dp, Switch sw, Button rn_btn)
            {
                this.expDate = expDate;
                this.tpw = tpw;
                this.price = price;
                this.type = type;
                this.dp = dp;
                this.sw = sw;
                this.rn_btn = rn_btn;
            }
        }
        readonly PlanViewProperties[] pvp = new PlanViewProperties[3] { null, null, null };

        public PlanConfigPage(User u)
        {
            InitializeComponent();
            user = u;

            try
            {
                GenerateView();
                SetSchedules();
            }
            catch(Exception e) { Console.WriteLine(e); }

            MessagingCenter.Subscribe<PageControlMessage>(this, "PlansUpdate", msg => {
                if (msg.Command == "just_plans")
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (pvp[i] != null)
                            ResetPlanView(i);
                    };
                }
                if(msg.Command == "schedules_too")
                {
                    try
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            contentLayout.Children.Clear();

                            GenerateView();
                            SetSchedules();
                        });
                    }catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });
        }

        void GenerateView()
        {
            if (user.UserPlan.TrainPlan != null)
                contentLayout.Children.Add(GetPlansGrid(user.UserPlan.TrainPlan, user.UserPlan.TrainPlanExpiryDate));
            if (user.UserPlan.YogaPlan != null)
                contentLayout.Children.Add(GetPlansGrid(user.UserPlan.YogaPlan, user.UserPlan.YogaPlanExpiryDate));
            if (user.UserPlan.PilatesPlan != null)
                contentLayout.Children.Add(GetPlansGrid(user.UserPlan.PilatesPlan, user.UserPlan.PilatesPlanExpiryDate));

            var changePlanBtn = new Button
            {
                Text = "ALTERAR PLANOS",
                TextColor = (Color)_app.Resources["TextDark"],
                BackgroundColor = (Color)_app.Resources["Orange"],
                Margin = new Thickness(20, 0)
            };
            changePlanBtn.Clicked += ChangePlanBtn_Clicked;
            contentLayout.Children.Add(changePlanBtn);

            planAbscenceBtn = new Button
            {
                Text = user.PlanAbscence == 0 ? "TRANCAR" : "DESTRANCAR",
                TextColor = (Color)_app.Resources["TextDark"],
                BackgroundColor = user.PlanAbscence == 0 ? (Color)_app.Resources["Red"] : Color.FromHex("#55E31B"),
                Margin = new Thickness(20, 0)
            };
            planAbscenceBtn.Clicked += PlanAbscenceBtn_Clicked;
            contentLayout.Children.Add(planAbscenceBtn);
        }

        
        Grid GetPlansGrid(Plan plan, string expiryDate)
        {
            if(expiryDate == null)
            {
                expiryDate = SharedUtilities.GetTodayDateTime().ToString("dd-MM-yyyy");
                try
                {
                    Task.Run(async () =>
                    {
                        var field = (plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : "Train") + "PlanExpiryDate";
                        await CrossCloudFirestore.Current.Instance.Collection("users").Document(user.UserID.ToString()).UpdateAsync(new FieldPath("UserPlan", field), expiryDate);
                        if(plan.IsPilates)
                            user.UserPlan.PilatesPlanExpiryDate = expiryDate;
                        else if(plan.IsYoga)
                            user.UserPlan.YogaPlanExpiryDate = expiryDate;
                        else
                            user.UserPlan.TrainPlanExpiryDate = expiryDate;
                    });
                }catch(Exception e) { Console.WriteLine(e); }
            }

            Grid planGrid = new Grid
            {
                BackgroundColor = (Color)_app.Resources["Primary"],
                ColumnSpacing = 0,
                RowSpacing = 0
            };

            planGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            planGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            planGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            planGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            planGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            planGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            planGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var type = new Label
            {
                Text = (plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : plan.Type) +
                        (plan.IsFloating ? " ☁" : ""),
                Margin = new Thickness(20, 20, 0, 0),
                TextColor = (plan.IsYoga || plan.IsPilates) ? (Color)_app.Resources["Yoga"] : (Color)_app.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            };
            planGrid.Children.Add(type);

            var tpw = new Label
            {
                Text = plan.TimesPerWeek + "x por semana (" + plan.Duration + ")",
                Margin = new Thickness(20, 0, 0, 20),
                TextColor = (Color)_app.Resources["TextLight"],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
            };
            planGrid.Children.Add(tpw, 0, 1);

            var price = new Label
            {
                Text = plan.Price + " R$",                
                TextColor = (Color)_app.Resources["Orange"],
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            };
            planGrid.Children.Add(price, 1, 0);
            Grid.SetRowSpan(price, 2);

            var datepicker = new DatePicker
            {
                ClassId = plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : "Treino",
                Date = DateTime.Parse(expiryDate),
                IsVisible = true,
                TextColor = (Color)_app.Resources["TextDark"]
            };
            planGrid.Children.Add(datepicker, 0, 2);
            datepicker.DateSelected += Datepicker_DateSelected;

            var bg = new BoxView
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = 1,
                BackgroundColor = Color.FromHex("#171717")
            };
            planGrid.Children.Add(bg, 0, 2);
            Grid.SetColumnSpan(bg, 2);
            Grid.SetRowSpan(bg, 2);
            
            var divider = new BoxView
            {
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                HeightRequest = 1,
                BackgroundColor = (Color)_app.Resources["LightTransparent"]
            };
            planGrid.Children.Add(divider, 0, 1);
            Grid.SetColumnSpan(divider, 2);

            var expDate = new Label
            {
                Text = "Vencimento: " + DateTime.Parse(expiryDate).ToString("dd/MM/yyyy"),
                Margin = new Thickness(0, 10, 0, 0),
                TextColor = (Color)_app.Resources["TextLight"],
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
            };
            planGrid.Children.Add(expDate, 0, 2);
            Grid.SetColumnSpan(expDate, 2);

            planGrid.Children.Add(new Label
            {
                Text = "Renovação automática: ",
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 10),
                VerticalOptions = LayoutOptions.Center,
                TextColor = (Color)_app.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
            }, 0, 3);

            var it = false;
            try { it = plan.IsYoga ? (user.UserPlan.YogaAutoRenewal == 1 ? true : false) : 
                                    plan.IsPilates ? (user.UserPlan.PilatesAutoRenewal == 1 ? true : false) : 
                                    (user.UserPlan.TrainAutoRenewal == 1 ? true : false) ; }
            catch { it = false; }
            var auto_renewal = new Switch
            {
                ClassId = plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : "Treino",
                IsToggled = it,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
            };
            auto_renewal.Toggled += ActivateAutoRenewal;
            planGrid.Children.Add(auto_renewal, 1, 3);

            planGrid.Children.Add(new BoxView() { BackgroundColor = (Color)_app.Resources["Orange"] }, 0, 4);
            planGrid.Children.Add(new BoxView() { BackgroundColor = (Color)_app.Resources["Orange"] }, 1, 4);

            var setdate_btn = new Button
            {
                Text = "DEFINIR DATA",
                ClassId = plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : "Treino",
                TextColor = (Color)_app.Resources["TextDark"],
                BackgroundColor = (Color)_app.Resources["Orange"]
            };
            setdate_btn.Clicked += SetdateBtn_Clicked;
            planGrid.Children.Add(setdate_btn, 0, 4);
            
            var renewal_btn = new Button
            {
                Text = "RENOVAR",
                ClassId = plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : "Treino",
                TextColor = (Color)_app.Resources["TextDark"],
                IsEnabled = plan.IsYoga ? user.UserPlan.YogaAutoRenewal == 0 : user.UserPlan.TrainAutoRenewal == 0,
                BackgroundColor = (Color) _app.Resources["Orange"]
            };
            renewal_btn.Clicked += RenewalBtn_Clicked;
            planGrid.Children.Add(renewal_btn, 1, 4);

            pvp[plan.IsYoga ? 1 : plan.IsPilates ? 2 : 0] = new PlanViewProperties(expDate, tpw, price, type, datepicker, auto_renewal, renewal_btn);

            return planGrid;
        }
        
        Grid GetClassGrid(Schedule.Weekday c, string details)
        {
            Grid classView = new Grid
            {
                BackgroundColor = (Color)_app.Resources["Primary"],
                ColumnSpacing = 0,
                RowSpacing = 0
            };

            classView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            classView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            classView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            classView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var split = details.Split('@');
            var type = _app.AdmSchedules.Find(s => s.Id.ToString() == split[1]).Type;

            classView.Children.Add(new Label
            {
                Text = split[0] + " - " + type,
                Margin = new Thickness(10, 10, 0, 0),
                TextColor = type == "Yoga" || type == "Pilates" ? (Color)_app.Resources["Yoga"] : (Color)_app.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            });

            classView.Children.Add(new Label
            {
                Text = SharedUtilities.IntToWeekday(c.Day),
                Margin = new Thickness(10, 0, 0, 10),
                TextColor = (Color)_app.Resources["TextLight"],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
            }, 0, 1);

            var changeBtn = new Button
            {
                ClassId = details +"@"+ c.Day + "changing",
                Text = "ALTERAR",
                TextColor = (Color)_app.Resources["Orange"],
                BackgroundColor = Color.Transparent
            };
            changeBtn.Clicked += ChangeBtn_Clicked;
            classView.Children.Add(changeBtn, 1, 0);
            Grid.SetRowSpan(changeBtn, 2);

            return classView;
        }

        StackLayout GetMissingClassStackLayout(string type)
        {
            StackLayout missingView = new StackLayout
            {
                BackgroundColor = (Color)_app.Resources["Primary"],
                Spacing = 0,
                Padding = 10
            };

            missingView.Children.Add(new Label
            {
                Text = "Aula pendente!",
                Margin = new Thickness(0,10,0,0),
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                TextColor = (Color)_app.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            });

            var changeBtn = new Button
            {
                Text = "SELECIONAR HORÁRIO",
                ClassId = type,
                TextColor = (Color)_app.Resources["Red"],
                BackgroundColor = Color.Transparent
            };
            changeBtn.Clicked += ChangeBtn_Clicked;
            missingView.Children.Add(changeBtn);

            return missingView;
        }

        bool changedByCode = false;
        
        private async void ActivateAutoRenewal(object sender, EventArgs e)
        {
            if (changedByCode)
            {
                changedByCode = false;
                return;
            }

            try
            {
                var sw = sender as Switch;
                var id = sw.ClassId == "Treino" ? 0 : sw.ClassId == "Yoga" ? 1 : 2;
                string field = id == 0 ? "TrainAutoRenewal" : id == 1 ? "YogaAutoRenewal" : "PilatesAutoRenewal";

                int result = -1;

                if (pvp[id].sw.IsToggled == true)
                {
                    if (await DisplayAlert("Renovação Automática", "Deseja ativar a renovação automática?", "Sim", "Não"))
                    {
                        result = 1;
                    }
                    else
                    {
                        changedByCode = true;
                        pvp[id].sw.IsToggled = false;
                    }
                }
                else
                {
                    if (await DisplayAlert("Renovação Automática", "Deseja desativar a renovação automática?", "Sim", "Não"))
                    {
                        result = 0;
                    }
                    else
                    {
                        changedByCode = true;
                        pvp[id].sw.IsToggled = true;
                    }
                }

                if (result != -1)
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                    await CrossCloudFirestore.Current.Instance.Collection("users").Document(user.UserID.ToString()).UpdateAsync(new FieldPath("UserPlan", field), result);

                    if (id == 0)
                        user.UserPlan.TrainAutoRenewal = result;
                    else if (id == 1)
                        user.UserPlan.YogaAutoRenewal = result;
                    else if (id == 2)
                        user.UserPlan.PilatesAutoRenewal = result;

                    pvp[id].rn_btn.IsEnabled = result == 0 ? true : false;

                    await PopupNavigation.Instance.PopAsync();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }
        }
        
        private async void Datepicker_DateSelected(object sender, DateChangedEventArgs e)
        {
            var classId = (sender as DatePicker).ClassId;
            var id = classId == "Treino" ? 0 : classId == "Yoga" ? 1 : 2;

            if (await DisplayAlert("Alterar Data", "Deseja alterar a data de vencimento do plano para " + e.NewDate.ToString("dd/MM") + "?", "Sim", "Não"))
            {
                await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                try
                {
                    string field = id == 0 ? "TrainPlanExpiryDate" : id == 1 ? "YogaPlanExpiryDate" : "PilatesPlanExpiryDate";
                    var newDate = e.NewDate.ToString("yyyy-MM-dd");

                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    
                    batch.Update(CrossCloudFirestore.Current.Instance.Collection("users").Document(user.UserID.ToString()), new FieldPath("UserPlan", field), newDate);
                    SharedUtilities.UpdateExpiryResumeWithBatch(batch,
                        new Models.ExpiryResume.Resume //old
                        {
                            UserID = user.UserID,
                            ExpiryDate = user.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = user.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = user.UserPlan.PilatesPlanExpiryDate
                        },
                        new Models.ExpiryResume.Resume //new
                        {
                            UserID = user.UserID,
                            ExpiryDate = id == 0 ? newDate : user.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = id == 1 ? newDate : user.UserPlan.YogaPlanExpiryDate, 
                            ExpiryDatePilates = id == 2 ? newDate : user.UserPlan.PilatesPlanExpiryDate
                        });

                    await batch.CommitAsync();

                    pvp[id].expDate.Text = "Vencimento: " + e.NewDate.ToString("dd/MM/yyyy");

                    await DisplayAlert("Sucesso!", "A data de vencimento foi atualizada com sucesso!", "Ok");
                }
                catch
                {
                    await DisplayAlert("Erro", "Ocorreu um erro ao tentar atualizar a data de vencimento, tente novamente mais tarde!", "Ok");
                }
                await PopupNavigation.Instance.PopAsync();
            }
        }
        
        private void SetdateBtn_Clicked(object sender, EventArgs e)
        {
            var classId = (sender as Button).ClassId;
            var id = classId == "Treino" ? 0 : classId == "Yoga" ? 1 : 2;

            pvp[id].dp.Focus();
        }
        
        private void ChangePlanBtn_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new PlanPicker(user.UserPlan, user));
        }
        
        private async void PlanAbscenceBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                int result = user.PlanAbscence == 0 ? 1 : 0;
                string text = user.PlanAbscence == 0 ? "trancar" : "destrancar";

                if (await DisplayAlert("Trancar Planos", "Deseja " + text + " os planos?", "Sim", "Não"))
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());

                    if (await AdmUtilities.SetPlanAbscence(user, result))
                    {
                        user.PlanAbscence = result;

                        string resultText = user.PlanAbscence == 0 ? "destrancado" : "trancado";
                        await DisplayAlert("Sucesso!", "O plano foi "+ resultText + " com sucesso!", "Ok");

                        planAbscenceBtn.BackgroundColor = user.PlanAbscence == 0 ? (Color)_app.Resources["Red"] : Color.FromHex("#55E31B"); ;
                        planAbscenceBtn.Text = user.PlanAbscence == 0 ? "TRANCAR" : "DESTRANCAR";
                    }
                    else
                    {
                        await DisplayAlert("Erro", "Não foi possível trancar o plano, tente novamente mais tarde...", "Ok");
                    }

                    await PopupNavigation.Instance.PopAsync();
                }
            }
            catch { }
        }
        
        private async void ChangeBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                var cmd = (sender as Button).ClassId;

                List<App.SelectedSchedules> selectedSchedules = new List<App.SelectedSchedules>();
                string[] split = null;

                Plan plan = new Plan
                {
                    Type = cmd,
                    TimesPerWeek = cmd == "Treino" ? selectedTrains.Count + 1 : cmd == "Pilates" ? selectedPilates.Count + 1 : selectedYoga.Count + 1,
                    IsYoga = cmd == "Yoga",
                    IsPilates = cmd == "Pilates"
                };

                App.SelectedSchedules changingSchedule = null;
                if (cmd.EndsWith("changing"))
                {
                    cmd = cmd.Replace("changing", "");
                    split = cmd.Split('@');

                    var dw = Int32.Parse(split[2]);

                    changingSchedule = new App.SelectedSchedules(split[0], dw, Int32.Parse(split[1]));

                    var sch = _app.AdmSchedules.Find(s => s.Id == Int32.Parse(split[1]));

                    var todayDateTime = SharedUtilities.GetTodayDateTime();

                    int today = (int)todayDateTime.DayOfWeek;
                    int z = dw < today ? 7 - (today - dw) : dw - today;
                    DateTime classDay = todayDateTime.AddDays(z);
                    string classDayString = classDay.ToString("yyyy-MM-dd");

                    var docpath = classDayString + "/" + sch.Time + "/" + sch.Type;
                    if (user.ClassesExceptions.Find(s => s.StartsWith(docpath)) != null)
                        changingSchedule.ClassException = docpath + "@remove";
                    else
                        changingSchedule.ClassException = docpath + "@add";

                    cmd = sch.Type;

                    plan = cmd == "Treino" ? user.UserPlan.TrainPlan : cmd == "Yoga" ? user.UserPlan.YogaPlan : user.UserPlan.PilatesPlan;
                }

                selectedSchedules = cmd == "Treino" ? selectedTrains : cmd == "Yoga" ? selectedYoga : selectedPilates;

                List<App.SelectedSchedules> copiedSchedules = new List<App.SelectedSchedules>();
                selectedSchedules.ForEach(ss =>
                {
                    copiedSchedules.Add(new App.SelectedSchedules(ss.Time, ss.Day, ss.ID) { Unchangeable = true });
                });

                if (split != null)
                    copiedSchedules.Remove(copiedSchedules.Find(ss => ss.Day == Int32.Parse(split[2]) && ss.ID == Int32.Parse(split[1])));

                await Navigation.PushAsync(new ClassSetupPage(ref user, user.UserID, cmd, plan, copiedSchedules, changingSchedule));
            }
            catch
            {
                await DisplayAlert("Erro", "Não foi possível carregar a página para alterar o horário. Tente reabrir a página de edição de plano para resolver o problema", "Ok");
            }
        }
        
        private async void RenewalBtn_Clicked(object sender, EventArgs e)
        {
            if(await DisplayAlert("Renovar Plano", "Deseja renovar o plano?", "Sim", "Não"))
            {
                await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                try
                {
                    var classId = (sender as Button).ClassId;
                    var id = classId == "Treino" ? 0 : 
                             classId == "Yoga" ? 1 : 
                             2;

                    string field = id == 0 ? "TrainPlanExpiryDate" : 
                                   id == 1 ? "YogaPlanExpiryDate" : 
                                   "PilatesPlanExpiryDate";

                    var oldDate = id == 0 ? DateTime.Parse(user.UserPlan.TrainPlanExpiryDate) : 
                                  id == 1 ? DateTime.Parse(user.UserPlan.YogaPlanExpiryDate) :
                                  DateTime.Parse(user.UserPlan.PilatesPlanExpiryDate);

                    var plan = id == 0 ? user.UserPlan.TrainPlan : 
                               id == 1 ? user.UserPlan.YogaPlan : 
                               user.UserPlan.PilatesPlan;

                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    string newDate = SharedUtilities.GetExpiryDate(plan, oldDate);
                    var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(user.UserID.ToString());

                    batch.Update(userDoc, new FieldPath("UserPlan", field), newDate);
                    SharedUtilities.UpdateExpiryResumeWithBatch(batch, 
                        new Models.ExpiryResume.Resume //old
                        {
                            UserID = user.UserID,
                            ExpiryDate = user.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = user.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = user.UserPlan.PilatesPlanExpiryDate
                        }, 
                        new Models.ExpiryResume.Resume //new
                        {
                            UserID = user.UserID,
                            ExpiryDate = id == 0 ? newDate : user.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = id == 1 ? newDate : user.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = id == 2 ? newDate : user.UserPlan.PilatesPlanExpiryDate
                        });

                    //floating plan renewal

                    var listOfNewDates = new List<string>();
                    var newMC = 0;
                    if (plan.IsFloating)
                    {
                        var todayDate = SharedUtilities.GetTodayDateTime();
                        var oldMCDate = id == 0 ? user.MCTrainDates :
                               id == 1 ? user.MCYogaDates :
                               user.MCPilatesDates;

                        string datesField = id == 0 ? "MCTrainDates" :
                                   id == 1 ? "MCYogaDates" :
                                   "MCPilatesDates";
                        string makeupField = id == 0 ? "MakeupClasses" :
                                   id == 1 ? "MakeupClassesYoga" :
                                   "MakeupClassesPilates";

                        var i = 1;
                        var stringDate = todayDate.ToString("yyyy-MM-dd") + "@" + i;
                        while (oldMCDate.Contains(stringDate))
                        {
                            i++;
                            stringDate = todayDate.ToString("yyyy-MM-dd") + "@" + i;
                        }

                        for (int x = 0; x <= plan.TimesPerWeek * 4; x++)
                            listOfNewDates.Add(todayDate.ToString("yyyy-MM-dd") + "@" + (i + x));

                        listOfNewDates.AddRange(oldMCDate);

                        newMC = (id == 0 ? user.MakeupClasses :
                                id == 1 ? user.MakeupClassesYoga :
                                user.MakeupClassesPilates) + (plan.TimesPerWeek * 4);

                        
                        batch.Update(userDoc, datesField, listOfNewDates);
                        batch.Update(userDoc, makeupField, newMC);
                    }
                        

                    await batch.CommitAsync();

                    if (id == 0)
                    {
                        user.UserPlan.TrainPlanExpiryDate = newDate;
                        if (user.UserPlan.TrainPlan.IsFloating)
                        {
                            user.MCTrainDates = listOfNewDates;
                            user.MakeupClasses = newMC;
                        }
                    }
                    else if(id == 1)
                    {
                        user.UserPlan.YogaPlanExpiryDate = newDate;
                        if (user.UserPlan.YogaPlan.IsFloating)
                        {
                            user.MCYogaDates = listOfNewDates;
                            user.MakeupClassesYoga = newMC;
                        }
                    }
                    else if (id == 2)
                    {
                        user.UserPlan.PilatesPlanExpiryDate = newDate;
                        if (user.UserPlan.PilatesPlan.IsFloating)
                        {
                            user.MCPilatesDates = listOfNewDates;
                            user.MakeupClassesPilates = newMC;

                        }
                    }

                    pvp[id].expDate.Text = "Vencimento: " + DateTime.Parse(newDate).ToString("dd/MM/yyyy");
                }
                catch { }
                await PopupNavigation.Instance.PopAsync();
            }
        }

        
        void ResetPlanView(int type)
        {
            Plan pl = type == 0 ? user.UserPlan.TrainPlan : type == 1 ? user.UserPlan.YogaPlan : user.UserPlan.PilatesPlan;
            string xpDate = "Vencimento: " + DateTime.Parse(type == 0 ? user.UserPlan.TrainPlanExpiryDate : type == 1 ? user.UserPlan.YogaPlanExpiryDate : user.UserPlan.PilatesPlanExpiryDate).ToString("dd/MM/yyyy");

            pvp[type].expDate.Text = xpDate;
            pvp[type].price.Text = pl.Price + " R$";
            pvp[type].tpw.Text = pl.TimesPerWeek + "x por semana (" + pl.Duration + ")";
            pvp[type].type.Text = pl.IsYoga ? "Yoga" : pl.Type;
        }
        
        void SetSchedules()
        {
            selectedTrains.Clear();
            selectedYoga.Clear();
            selectedPilates.Clear();

            int trainTpw = 0;
            int yogaTpw = 0;
            int pilatesTpw = 0;
            if (user.UserPlan.TrainPlan != null && !user.UserPlan.TrainPlan.IsFloating)
            {
                trainTpw = user.UserPlan.TrainPlan.TimesPerWeek;
            }
            if (user.UserPlan.YogaPlan != null && !user.UserPlan.YogaPlan.IsFloating)
            {
                yogaTpw = user.UserPlan.YogaPlan.TimesPerWeek;
            }
            if (user.UserPlan.PilatesPlan != null && !user.UserPlan.PilatesPlan.IsFloating)
            {
                pilatesTpw = user.UserPlan.PilatesPlan.TimesPerWeek;
            }

            var foundClasses = new List<Schedule.Weekday>();
            var classesDetails = new List<string>();
            _app.AdmSchedules.ForEach(s =>
            {
                var fc = s.Classes.FindAll(c => c.StudentsList.Contains(user.UserID));
                foundClasses.AddRange(fc);

                if (s.Type == "Treino")
                    fc.ForEach(c =>
                    {
                        selectedTrains.Add(new App.SelectedSchedules(s.Time, c.Day, s.Id)
                        {
                            Unchangeable = true
                        });
                    });
                else if(s.Type == "Yoga")
                    fc.ForEach(c =>
                    {
                        selectedYoga.Add(new App.SelectedSchedules(s.Time, c.Day, s.Id)
                        {
                            Unchangeable = true
                        });
                    });
                else if(s.Type == "Pilates")
                    fc.ForEach(c =>
                    {
                        selectedPilates.Add(new App.SelectedSchedules(s.Time, c.Day, s.Id)
                        {
                            Unchangeable = true
                        });
                    });

                fc.ForEach(c => { classesDetails.Add(s.Time + "@" + s.Id); });
            });

            for (int x = 0; x < 3; x++)
                if (x == 0 ? selectedTrains.Count < trainTpw : x == 1 ? selectedYoga.Count < yogaTpw : selectedPilates.Count < pilatesTpw)
                    for (int i = 0; i < (x == 0 ? trainTpw - selectedTrains.Count : x == 1 ? yogaTpw - selectedYoga.Count : pilatesTpw - selectedPilates.Count); i++)
                    {
                        var missingClView = GetMissingClassStackLayout(x == 0 ? "Treino" : x == 1 ? "Yoga" : "Pilates");
                        showingScheduleLayouts.Add(missingClView);
                        contentLayout.Children.Add(missingClView);
                    }
                        

            foundClasses.ForEach(fc =>
            {
                var clView = GetClassGrid(fc, classesDetails[foundClasses.IndexOf(fc)]);
                showingScheduleLayouts.Add(clView);
                contentLayout.Children.Add(clView);
            });
        }
    }
}