using ctf_final.Models;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.StudentContents
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MakeupClassPicker : ContentPage
    {
        //---- VARIABLES ----

        //-- UI --

        readonly ActivityIndicator loadingSing;
        readonly List<int> PickerWeekdays = new List<int>();

        bool disabledDoubleTap = false;

        //-- UI --

        //-- SCHEDULES AND LISTENERS --

        public class TemporarySchedules
        {
            public int Weekday;
            public SchedulesByDayOfWeek SelectedWeekdaySchedules;
            public IListenerRegistration TemporaryListener;

            public TemporarySchedules(int weekday, SchedulesByDayOfWeek selectedWeekdaySchedules, IListenerRegistration temporaryListener)
            {
                Weekday = weekday;
                SelectedWeekdaySchedules = selectedWeekdaySchedules;
                TemporaryListener = temporaryListener;
            }
        }

        readonly List<TemporarySchedules> DownloadedSchedules = new List<TemporarySchedules>();
        SchedulesByDayOfWeek selectedWeekdaySchedules = null;
        SchedulesByDayOfWeek.Times selectedClass = new SchedulesByDayOfWeek.Times();
        IListenerRegistration temporaryListener = null;

        //-- SCHEDULES AND LISTENERS --

        //---- VARIABLES ----

        public MakeupClassPicker()
        {
            InitializeComponent();

            var todayDate = SharedUtilities.GetTodayDateTime();
            int DoW = (int) todayDate.DayOfWeek;

            loadingSing = new ActivityIndicator
            {
                IsRunning = true,
                IsVisible = true,
                Color = (Color)_app.Resources["Orange"]
            };
            mainLayout.Children.Add(loadingSing, new Rectangle(.5, .5, .1, .1), AbsoluteLayoutFlags.All);

            for (int i = 0; i < 7; i++)
            {
                var date = todayDate.AddDays(i);
                var wd = (int)date.DayOfWeek;
                
                if (SharedUtilities.IntToWeekday(wd) == "Domingo")
                    continue;

                if (!_app.ApplicationUserData.UserClasses.Any(uc => uc.Date == date.ToString("yyyy-MM-dd")))
                {
                    string text = wd == DoW ? "Hoje" : wd == (DoW + 1) ? "Amanhã" : SharedUtilities.IntToWeekday(wd) + " - " + date.ToString("dd/MM");

                    weekdayPicker.Items.Add(text);
                    PickerWeekdays.Add(wd);
                }
            }

            if(PickerWeekdays.Count > 0)
            {
                weekdayPicker.SelectedIndex = 0;
                Task.Run(async () => { await GetSchedulesByWeekday(PickerWeekdays[0]); });
            }
            else
            {
                loadingSing.IsRunning = false;
                loadingSing.IsVisible = false;

                mainLayout.Children.Add(new Label
                {
                    Text = "Você já tem aulas agendadas em todos os próximos 7 dias...",
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
                }, new Rectangle(.5, .5, -1, -1), AbsoluteLayoutFlags.PositionProportional);
            }
        }

        public void OnDateChange(object sender, EventArgs e)
        {
            var wd = PickerWeekdays[(sender as Picker).SelectedIndex];
            contentLayout.Children.Clear();

            if (loadingSing != null)
            {
                loadingSing.IsRunning = true;
                loadingSing.IsVisible = true;
            }

            Task.Run(async () => { await GetSchedulesByWeekday(wd); });
        }

        public async Task GetSchedulesByWeekday(int wd)
        {
            try
            {
                var ds = DownloadedSchedules.Find(ts => ts.Weekday == wd);
                if (ds != null)
                {
                    selectedWeekdaySchedules = ds.SelectedWeekdaySchedules;
                    PopulateAvailableClass(); 
                }
                else
                {
                    var queryRef = CrossCloudFirestore.Current
                                       .Instance
                                       .Collection("real_schedules")
                                       .Document(wd.ToString());

                    selectedWeekdaySchedules = await SharedUtilities.UpdateOutdatedRealschedules(queryRef, wd);
                    PopulateAvailableClass();

                    temporaryListener = queryRef.AddSnapshotListener((snp, error) =>
                    {
                        if (!snp.Metadata.IsFromCache)
                        {
                            var update = snp.ToObject<SchedulesByDayOfWeek>();
                            if (update != null)
                            {
                                var dSchedule = DownloadedSchedules.Find(dsch => dsch.Weekday == update.DayOfWeek);
                                if (dSchedule.SelectedWeekdaySchedules != update)
                                {
                                    dSchedule.SelectedWeekdaySchedules = update;
                                    if (selectedWeekdaySchedules.DayOfWeek == update.DayOfWeek)
                                    {
                                        try
                                        {
                                            if (selectedClass.Time != null && 
                                                        update.Classes.Find(c => selectedClass.Time == c.Time && selectedClass.Type == c.Type).StudentsList.Count >= SharedUtilities.GetClassSizeLimitByType(selectedClass.Type))
                                                UserUtilities.NumberOfStudentsChanged = true;
                                        }
                                        catch
                                        {
                                            UserUtilities.NumberOfStudentsChanged = true;
                                        }

                                        selectedWeekdaySchedules = update;
                                        PopulateAvailableClass();
                                    }
                                }
                            }
                        }
                    });
                    DownloadedSchedules.Add(new TemporarySchedules(wd, selectedWeekdaySchedules, temporaryListener));
                }
            } catch(Exception e) { Console.WriteLine("Erro ao carregar a aula." + e); }
        }
        public void PopulateAvailableClass()
        {
            List<View> classesToAdd = new List<View>();
            if (selectedWeekdaySchedules != null)
            {
                selectedWeekdaySchedules.Classes = selectedWeekdaySchedules.Classes.OrderBy(c => c.Time).ToList();
                var todayDate = SharedUtilities.GetTodayDateTime();

                if (selectedWeekdaySchedules.DayOfWeek == (int)SharedUtilities.GetTodayDateTime().DayOfWeek)
                    selectedWeekdaySchedules.Classes.RemoveAll(c => todayDate.AddHours(SharedUtilities.DEFAULT_TIME_LIMIT) > DateTime.ParseExact(c.Date + c.Time, "yyyy-MM-ddHH:mm", CultureInfo.InvariantCulture));

                foreach(var c in selectedWeekdaySchedules.Classes)
                {

                    var isInWLFilter = _app.WeightliftingFilter.Classes.Contains(c.Time + "@" + selectedWeekdaySchedules.DayOfWeek);
                    bool drawTrainClass = false;
                    if (c.Type == "Treino" && _app.LoggedInUser.UserPlan.TrainPlan != null)
                    {
                        drawTrainClass = true;

                        if (isInWLFilter)
                        {
                            if (_app.LoggedInUser.UserPlan.TrainPlan.Type != "LPO" && _app.LoggedInUser.UserPlan.TrainPlan.Type != "LPO + Treino")
                            {
                                drawTrainClass = false;
                            }
                        }
                        else if (_app.LoggedInUser.UserPlan.TrainPlan.Type == "LPO")
                        {
                            drawTrainClass = false;
                        }
                    }

                    if (c.Type == "Treino" ? drawTrainClass :
                        c.Type == "Yoga" ? _app.LoggedInUser.UserPlan.YogaPlan != null : 
                        _app.LoggedInUser.UserPlan.PilatesPlan != null)
                    {
                        int maxSize = SharedUtilities.GetClassSizeLimitByType(c.Type);
                        if (c.StudentsList.Count < maxSize)
                        {
                            StackLayout classLayout = new StackLayout
                            {
                                Padding = new Thickness(10),
                                ClassId = selectedWeekdaySchedules.Classes.IndexOf(c).ToString(),
                                Orientation = StackOrientation.Horizontal,
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                                BackgroundColor = (Color)_app.Resources["PrimaryTransparent"]
                            };
                            TapGestureRecognizer tapMarkAttendance = new TapGestureRecognizer();
                            tapMarkAttendance.Tapped += MarkAttendance;
                            tapMarkAttendance.NumberOfTapsRequired = 1;

                            classLayout.GestureRecognizers.Add(tapMarkAttendance);

                            classLayout.Children.Add(new Label
                            {
                                Text = isInWLFilter ? c.Time + " - " + "LPO" : c.Time + " - " + c.Type,
                                TextColor = c.Type == "Treino" ? (Color)_app.Resources["Orange"] : (Color)_app.Resources["Yoga"],
                                VerticalOptions = LayoutOptions.Center,
                                HorizontalOptions = LayoutOptions.StartAndExpand,
                                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
                            });

                            classLayout.Children.Add(new Label
                            {
                                Text = (maxSize - c.StudentsList.Count) + " vagas",
                                TextColor = (Color)_app.Resources["TextLight"],
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
                            });

                            classLayout.Children.Add(new Image
                            {
                                Source = "ic_checkmark.png",
                                Aspect = Aspect.AspectFit
                            });

                            classesToAdd.Add(classLayout);
                            classesToAdd.Add(new BoxView { HeightRequest = 1, BackgroundColor = (Color)_app.Resources["DarkTransparent"], HorizontalOptions = LayoutOptions.Fill });
                        }
                    }
                }
            }

            //TODO update classes instead of recreating them
            Device.BeginInvokeOnMainThread(() =>
            {
                if (contentLayout.Children.Count > 0)
                    contentLayout.Children.Clear();

                classesToAdd.ForEach(v =>
                {
                    contentLayout.Children.Add(v);
                });

                if (contentLayout.Children.Count < 1)
                {
                    contentLayout.Children.Add(new Label
                    {
                        Text = "Nenhum horário disponível nessa data",
                        Margin = new Thickness(10),
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = (Color)_app.Resources["Orange"]
                    });
                }

                loadingSing.IsRunning = false;
                loadingSing.IsVisible = false;
            });
        }

        private async void MarkAttendance(object sender, EventArgs e)
        {
            try
            {
                if (!disabledDoubleTap)
                {
                    disabledDoubleTap = true;

                    var current = Connectivity.NetworkAccess;

                    if (current != NetworkAccess.Internet)
                    {
                        await DisplayAlert("Erro", "Não foi possível marcar a aula, sem conexão com a internet!", "Ok");
                        await Navigation.PopAsync();
                        return;
                    }

                    int classID = Int32.Parse((sender as StackLayout).ClassId);
                    var c = selectedWeekdaySchedules.Classes[classID];

                    // ID-0000001 - Changed at 01-09-20: checking repositions before marking class 
                    if (_app.LoggedInUser.MakeupClasses < 1 && c.Type == "Treino" || _app.LoggedInUser.MakeupClassesYoga < 1 && c.Type == "Yoga" || _app.LoggedInUser.MakeupClassesPilates < 1 && c.Type == "Pilates")
                    {
                        await DisplayAlert("Erro", "Você não possui reposições disponíveis.", "Ok");
                        //await Navigation.PopAsync();
                        return;
                    }

                    var todayDate = SharedUtilities.GetTodayDateTime();
                    DateTime classDate = DateTime.ParseExact(c.Date + c.Time, "yyyy-MM-ddHH:mm", CultureInfo.InvariantCulture);

                    if (todayDate.AddHours(SharedUtilities.DEFAULT_TIME_LIMIT) > classDate)
                    {
                        await DisplayAlert("Erro", "Não é possível marcar aulas com menos de 3 horas de antecedência.", "Ok");
                        PopulateAvailableClass();

                        selectedClass = new SchedulesByDayOfWeek.Times();
                        disabledDoubleTap = false;

                        return;
                    }

                    if (await DisplayAlert("Reposição", "Marcar uma reposição no dia " + DateTime.Parse(c.Date).ToString("dd/MM") + " as " + c.Time + "?", "Sim", "Não"))
                    {
                        selectedClass = c;

                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                        if(await UserUtilities.MarkAppointment(DownloadedSchedules, c))
                        {
                            await DisplayAlert("Sucesso!", "Reposição agendada com sucesso!", "Ok");
                            await Navigation.PopAsync(true);
                        }
                        else
                        {
                            await DisplayAlert("Erro!", "Não foi possível agendar a reposição, tente novamente mais tarde.", "Ok");
                        }
                        await PopupNavigation.Instance.PopAsync();
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            selectedClass = new SchedulesByDayOfWeek.Times();
            disabledDoubleTap = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            DownloadedSchedules.ForEach(ds =>
            {
                ds.TemporaryListener.Remove();
            });
        }
    }
}