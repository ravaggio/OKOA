using ctf_final.PlanModels;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.AdmContents.Students
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ClassSetupPage : ContentPage
    {
        Plan _plan;
        string _planType = "";

        int userId = -1;

        User changingUser = null;

        User u = null;
        List<App.SelectedSchedules> _selectedSchedules = new List<App.SelectedSchedules>();

        List<int> weekdays = null;
        List<StackLayout> individualScheduleViews = new List<StackLayout>();
        List<Schedule> schedules = _app.AdmSchedules;

        List<Switch> switches;
        List<Image> add_btns = new List<Image>();

        List<Grid> slClasses;

        bool changing = false;
        App.SelectedSchedules scheduleToChange;

        public ClassSetupPage(User user, string type, bool isChanging = false)
        {
            InitializeComponent();
            changing = isChanging;

            u = user;
            _planType = type;
            if (type == "Yoga")
                _plan = user.UserPlan.YogaPlan;
            else if (type == "Pilates")
                _plan = user.UserPlan.PilatesPlan;
            else
                _plan = user.UserPlan.TrainPlan;

            Initialize(type);
            PopulateScheduleList();
        }
        public ClassSetupPage(ref User user, int id, string type, Plan pl, List<App.SelectedSchedules> ss, App.SelectedSchedules changing)
        {
            InitializeComponent();
            changingUser = user;
            userId = id;

            _plan = pl;
            _planType = type;

            scheduleToChange = changing;

            Initialize(type);

            _selectedSchedules = ss;
            ShowSelectedSchedules();
            PopulateScheduleList();
        }

        private void Initialize(string type)
        {
            slClasses = new List<Grid>() { slClass1, slClass2, slClass3, slClass4, slClass5, slClass6 };
            Title = "Agendar " + type;
            
            switches = new List<Switch>()
            {
                swi0,
                swi1,
                swi2,
                swi3,
                swi4,
                swi5,
                swi6
            };
            selectedSchedulesLabel.Text = "0/" + _plan.TimesPerWeek + " aulas selecionadas.";
        }

        //Selected schedules related functions (reset view/remove selection)
        private void ShowSelectedSchedules()
        {
            selectedSchedulesLabel.Text = _selectedSchedules.Count() + "/" + _plan.TimesPerWeek + " aulas selecionadas.";

            if (_selectedSchedules.Count() == _plan.TimesPerWeek)
                nextBtn.IconImageSource = "ic_right_arrow.png";
            else
                nextBtn.IconImageSource = "ic_right_arrow.png";

            int i = 0;
            foreach (App.SelectedSchedules ss in _selectedSchedules)
            {
                slClasses[i].BindingContext = ss;
                slClasses[i].IsVisible = true;
                (slClasses[i].Children[1] as Button).IsEnabled = !ss.Unchangeable;
                i++;
            }

            switch (i)
            {
                case 0:
                    slClass1.IsVisible = false;
                    slClass2.IsVisible = false;
                    slClass3.IsVisible = false;
                    slClass4.IsVisible = false;
                    slClass5.IsVisible = false;
                    slClass6.IsVisible = false;
                    break;
                case 1:
                    slClass2.IsVisible = false;
                    slClass3.IsVisible = false;
                    slClass4.IsVisible = false;
                    slClass5.IsVisible = false;
                    slClass6.IsVisible = false;
                    break;
                case 2:
                    slClass3.IsVisible = false;
                    slClass4.IsVisible = false;
                    slClass5.IsVisible = false;
                    slClass6.IsVisible = false;
                    break;
                case 3:
                    slClass4.IsVisible = false;
                    slClass5.IsVisible = false;
                    slClass6.IsVisible = false;
                    break;
                case 4:
                    slClass5.IsVisible = false;
                    slClass6.IsVisible = false;
                    break;
                case 5:
                    slClass6.IsVisible = false;
                    break;
            }
        }
        private void RemoveButton(object sender, EventArgs e)
        {
            _selectedSchedules.RemoveAt(Int32.Parse((sender as Button).CommandParameter.ToString()));
            ShowSelectedSchedules();
        }

        //Schedule selection related functions (use of the switchs and show the available list)
        private void Switch_Toggled(object sender, ToggledEventArgs e)
        {
            var selectedSwitch = sender as Switch;
            int value = -1;
            switch (selectedSwitch.ClassId)
            {
                case "switchDom":
                    value = 0;
                    break;
                case "switchSeg":
                    value = 1;
                    break;
                case "switchTer":
                    value = 2;
                    break;
                case "switchQua":
                    value = 3;
                    break;
                case "switchQui":
                    value = 4;
                    break;
                case "switchSex":
                    value = 5;
                    break;
                case "switchSab":
                    value = 6;
                    break;
            }

            if(!_selectedSchedules.Any(schedule => schedule.Day == value))
            {
                if (weekdays.Count + _selectedSchedules.Count < _plan.TimesPerWeek) {
                    if (selectedSwitch.IsToggled)
                    {
                        weekdays.Add(value);
                        weekdays.Sort();
                    }
                    else
                    {
                        weekdays.Remove(value);
                    }

                    PopulateScheduleList();
                }
                else
                {
                    if (selectedSwitch.IsToggled)
                    {
                        DisplayAlert("Limite", "Você não pode exceder o número máximo de aulas por semana do plano (" + _plan.TimesPerWeek + " aulas).", "Ok");
                        selectedSwitch.IsToggled = false;
                    }
                    else
                    {
                        weekdays.Remove(value);
                    }

                    PopulateScheduleList();
                }
            }
            else
            {
                if (selectedSwitch.IsToggled)
                {
                    DisplayAlert("Aula", "Você você já escolheu uma aula para este dia da semana. Remova-a e adicione novamente para alterar o horário.", "Ok");
                    selectedSwitch.IsToggled = false;
                }
                else
                {
                    weekdays.Remove(value);
                }
            }
        }
        private void PopulateScheduleList()
        {
            if (weekdays == null)
            {
                weekdays = new List<int>();
                foreach (Schedule s in schedules)
                {
                    var fullClasses = s.Classes.FindAll(c => c.StudentsList.Count >= SharedUtilities.GetClassSizeLimitByType(s.Type));

                    if (changing)
                        fullClasses.RemoveAll(c => c.StudentsList.Contains(u.UserID));

                if (scheduleToChange != null)
                    if (s.Id == scheduleToChange.ID)
                        fullClasses.RemoveAll(c => c.Day == scheduleToChange.Day);
                    if (s.Type == _planType)
                    {
                        StackLayout view = new StackLayout()
                        {
                            Spacing = 0,
                            BackgroundColor = (Color)Application.Current.Resources["DarkUTransparent"],
                            IsVisible = true
                        };

                        StackLayout sl = new StackLayout() { Orientation = StackOrientation.Horizontal, Spacing = 0, Padding = new Thickness(8, 4) };


                        

                        StackLayout details = new StackLayout()
                        {
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            Spacing = 0
                        };
                        details.Children.Add(new Label
                        {
                            Text = s.Time,
                            Margin = new Thickness(4, 2),
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            TextColor = (Color)Application.Current.Resources["Orange"],
                            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                        });

                        s.Classes = s.Classes.OrderBy(wd => wd.Day).ToList();
                        foreach (Schedule.Weekday wd in s.Classes)
                        {
                            if(fullClasses == null || !fullClasses.Contains(wd))
                            {
                                var inWLFilter = _app.WeightliftingFilter.Classes.Contains(s.Time+"@"+wd.Day);

                                int studentsCount = wd.StudentsList.Count;

                                if (changing)
                                    studentsCount = wd.StudentsList.Contains(u.UserID) ? wd.StudentsList.Count - 1 : studentsCount;

                                if (scheduleToChange != null)
                                    if (s.Id == scheduleToChange.ID)
                                        studentsCount = wd.Day == scheduleToChange.Day ? wd.StudentsList.Count - 1 : studentsCount;

                                string name = SharedUtilities.IntToWeekday(wd.Day);
                                var size = SharedUtilities.GetClassSizeLimitByType(s.Type);
                                name += inWLFilter ? " - LPO" : "";
                                name += " - " + (size - studentsCount) + "/" + size + " vagas";

                                details.Children.Add(new Label
                                {
                                    Text = name,
                                    TextColor = (Color)Application.Current.Resources["TextLight"],
                                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                    HorizontalOptions = LayoutOptions.Start
                                });
                            }
                        }

                        sl.Children.Add(details);
                        Image plusBtn = new Image()
                        {
                            ClassId = s.Time + "@" + s.Id,
                            Source = "ic_plus.png",
                            Aspect = Aspect.AspectFit
                        };

                        TapGestureRecognizer add = new TapGestureRecognizer();
                        add.Tapped += (sen, ex) =>
                        {
                            if (weekdays.Count > 0)
                            {
                                if (_selectedSchedules.Count() < _plan.TimesPerWeek)
                                {
                                    foreach (int i in weekdays)
                                    {
                                        var values = (sen as Image).ClassId.Split('@');
                                        _selectedSchedules.Add(new App.SelectedSchedules(values[0], i, Int32.Parse(values[1])));
                                        _selectedSchedules = _selectedSchedules.OrderBy(x => x.Day).ToList();
                                        ShowSelectedSchedules();
                                    }
                                    switches.ForEach(sw => sw.IsToggled = false);
                                    PopulateScheduleList();
                                }
                            }
                            else
                            {
                                DisplayAlert("Dias da semana", "Selecione os dias da semana para adicionar.", "Ok");
                            }
                        };
                        add.NumberOfTapsRequired = 1;
                        plusBtn.GestureRecognizers.Add(add);

                        add_btns.Add(plusBtn);
                        sl.Children.Add(plusBtn);

                        view.Children.Add(sl);
                        view.Children.Add(new BoxView { BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"], HeightRequest = 1, HorizontalOptions = LayoutOptions.FillAndExpand });

                        schedulesView.Children.Add(view);
                        individualScheduleViews.Add(view);
                    }
                }
            }
            else
            {
                if (weekdays.Count > 0)
                {
                    add_btns.ForEach(img => img.Source = "ic_plus_accent.png");
                    individualScheduleViews.ForEach(sl => sl.IsVisible = false);

                    bool found_any = false;
                    int i = 0;
                    foreach (Schedule s in schedules.FindAll(s => s.Type == _planType))
                    {
                        //limit by size - x.StudentsList.Count < SharedUtilities.GetClassSizeLimitByType(s.Type)).Count
                        var notFullClasses = new List<Schedule.Weekday>();
                        if (changing)
                        {
                            notFullClasses = s.Classes.FindAll(x =>
                                weekdays.Contains(x.Day) && 
                                ((x.StudentsList.Contains(u.UserID) ? x.StudentsList.Count - 1 : x.StudentsList.Count) < SharedUtilities.GetClassSizeLimitByType(s.Type))
                            );
                        }
                        else
                            notFullClasses = s.Classes.FindAll(x => weekdays.Contains(x.Day) && x.StudentsList.Count < SharedUtilities.GetClassSizeLimitByType(s.Type));
                        
                        if (notFullClasses.Count == weekdays.Count)
                        {
                            individualScheduleViews[i].IsVisible = true;
                            found_any = true;
                        }
                        i++;
                    }

                    if (found_any)
                        labelEmpty.IsVisible = false;
                    else
                        labelEmpty.IsVisible = true;
                }
                else
                {
                    labelEmpty.IsVisible = false;
                    add_btns.ForEach(img => img.Source = "ic_plus.png");
                    individualScheduleViews.ForEach(sl => sl.IsVisible = true);
                }
            }
        }

        //Finishes student cadastre. Result comes from subscribed messagin center
        private async void Finish(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup(), true);

            try
            {
                if (_app.TemporarySelectedSchedules == null)
                    _app.TemporarySelectedSchedules = new List<App.SelectedSchedules>[3];

                List<App.SelectedSchedules> copy = new List<App.SelectedSchedules>();
                if (u == null)
                {
                    _selectedSchedules.ForEach(s =>
                    {
                        if (s.Unchangeable == false)
                            copy.Add(new App.SelectedSchedules(s.Time, s.Day, s.ID));
                    });
                }

                if (_selectedSchedules.Count == _plan.TimesPerWeek || copy.Count == 1)
                {
                    if(u == null)
                    {
                        try
                        {
                            var ss = copy.First();
                            var foundSchedule = _app.AdmSchedules.Find(s => s.Id == ss.ID);
                            var scheduleReference = ss.ID + "@" + ss.Day + "@" + ss.Time + "/" + foundSchedule.Type;

                            var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(userId.ToString());
                            var scheduleDoc = CrossCloudFirestore.Current.Instance.Collection("schedules").Document(ss.ID.ToString());
                            var scheduleHistory = CrossCloudFirestore.Current
                                .Instance
                                .Collection("adm_events")
                                .Document("schedules_change_history");

                            //SERVER SIDE

                            var batch = CrossCloudFirestore.Current.Instance.Batch();

                            var selectedClass = foundSchedule.Classes.Find(c => c.Day == ss.Day);
                            var oldClass = new Schedule.Weekday
                            {
                                Day = selectedClass.Day,
                                StudentsList = new List<int>(selectedClass.StudentsList)
                            };
                            var newClass = new Schedule.Weekday
                            {
                                Day = selectedClass.Day,
                                StudentsList = new List<int>(selectedClass.StudentsList)
                            };
                            newClass.StudentsList.Add(userId);

                            batch.Update(userDoc, "ScheduleReferences", FieldValue.ArrayUnion(scheduleReference));
                            batch.Update(scheduleDoc, "Classes", FieldValue.ArrayRemove(oldClass));
                            batch.Update(scheduleDoc, "Classes", FieldValue.ArrayUnion(newClass));
                            batch.Update(scheduleHistory, "History", FieldValue.ArrayUnion(userId + "@" + ss.Day + "@" + ss.ID));

                            var removingScheduleRef = "";
                            Schedule.Weekday oldSelectedClass = null;
                            if (scheduleToChange != null)
                            {
                                var sch = _app.AdmSchedules.Find(s => s.Id == scheduleToChange.ID);
                                removingScheduleRef = scheduleToChange.ID + "@" + scheduleToChange.Day + "@" + scheduleToChange.Time + "/" + sch.Type;
                                var oldScheduleDoc = CrossCloudFirestore.Current.Instance.Collection("schedules").Document(scheduleToChange.ID.ToString());

                                // -- CLASS EXCEPTIONS --

                                int today = (int) SharedUtilities.GetTodayDateTime().DayOfWeek;
                                int z = ss.Day < today ? 7 - (today - ss.Day) : ss.Day - today;
                                DateTime classDay = DateTime.Today.AddDays(z);
                                string classDayString = classDay.ToString("yyyy-MM-dd");

                                var newClassException = classDayString + "/" + scheduleReference.Split('@')[2] + "@remove";

                                if (scheduleToChange.ClassException != null && scheduleToChange.ClassException.EndsWith("@remove"))
                                {
                                    batch.Update(userDoc, "ClassesExceptions", FieldValue.ArrayRemove(scheduleToChange.ClassException));
                                    batch.Update(userDoc, "ClassesExceptions", FieldValue.ArrayUnion(newClassException));
                                }
                                else if(scheduleToChange.ClassException != null && scheduleToChange.ClassException.EndsWith("@add"))
                                {
                                    batch.Update(userDoc, "ClassesExceptions", FieldValue.ArrayUnion(scheduleToChange.ClassException));
                                    batch.Update(userDoc, "ClassesExceptions", FieldValue.ArrayUnion(newClassException));
                                }

                                // -- CLASS EXCEPTIONS --

                                oldSelectedClass = sch.Classes.Find(c => c.Day == scheduleToChange.Day);
                                var removingClass = new Schedule.Weekday
                                {
                                    Day = oldSelectedClass.Day,
                                    StudentsList = new List<int>(oldSelectedClass.StudentsList)
                                };
                                var newlyMadeClass = new Schedule.Weekday
                                {
                                    Day = oldSelectedClass.Day,
                                    StudentsList = new List<int>(oldSelectedClass.StudentsList)
                                };
                                newlyMadeClass.StudentsList.Remove(userId);


                                batch.Update(userDoc, "ScheduleReferences", FieldValue.ArrayRemove(removingScheduleRef));
                                batch.Update(oldScheduleDoc, "Classes", FieldValue.ArrayRemove(removingClass));
                                batch.Update(oldScheduleDoc, "Classes", FieldValue.ArrayUnion(newlyMadeClass));
                                batch.Update(scheduleHistory, "History", FieldValue.ArrayRemove(userId + "@" + scheduleToChange.Day + "@" + scheduleToChange.ID));
                            }
                            await batch.CommitAsync();

                            //SERVER SIDE

                            //LOCAL SIDE

                            changingUser.ScheduleReferences.Add(scheduleReference);
                            selectedClass.StudentsList.Add(userId);
                            if(scheduleToChange != null)
                            {
                                changingUser.ScheduleReferences.Remove(removingScheduleRef);
                                oldSelectedClass.StudentsList.Remove(userId);

                                if (_plan.IsPilates)
                                    changingUser.UserPlan.PilatesPlan = _plan;
                                else if (_plan.IsYoga)
                                    changingUser.UserPlan.YogaPlan = _plan;
                                else
                                    changingUser.UserPlan.TrainPlan = _plan;
                            }

                            _app.AdmSchedules = _app.AdmSchedules;
                            await _app.SavePropertiesAsync();

                            //LOCAL SIDE

                            MessagingCenter.Send(new PageControlMessage() { Command = "schedules_too" }, "PlansUpdate");

                            await DisplayAlert("Sucesso", "Horário alterado com sucesso!", "Ok");
                        }
                        catch
                        {
                            await DisplayAlert("Erro", "Incapaz de atualizar o horário. Por favor, verifique sua conexão com a internet e tente novamente.", "OK");
                        }

                        await Navigation.PopAsync();
                    }
                    else
                    {
                        //[ID_1] defined IsFloating checker
                        if (_planType == FindLastPlan())
                        {
                            var z = _planType == "Treino" ? 0 : _planType == "Yoga" ? 1 : 2;
                            _app.TemporarySelectedSchedules[z] = _selectedSchedules;
                            if (changing)
                            {
                                await CheckClasses(z);
                                if (await AdmUtilities.UpdateUserPlan(u))
                                {
                                    try
                                    {
                                        for (int m = 0; m < z + 1; m++)
                                            Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
                                    }
                                    catch(Exception) { }
                                    MessagingCenter.Send(new PageControlMessage() { Command = "schedules_too" }, "PlansUpdate");

                                    await DisplayAlert("Sucesso", "Plano alterado com sucesso!", "Ok");
                                }
                                else
                                {
                                    await DisplayAlert("Erro", "Incapaz de atualizar o plano. Por favor, verifique sua conexão com a internet e tente novamente.", "OK");
                                }
                                await Navigation.PopAsync();
                            }
                            else
                            {
                                await CheckClasses(z);
                                if (await AdmUtilities.CreateNewUser(u))
                                {
                                    await Navigation.PushAsync(new StudentCadastreCompletion(u));
                                }
                                else
                                {
                                    await Navigation.PopAsync();
                                    await DisplayAlert("Erro", "Não foi possível cadastrar o usuário. Por favor, verifique sua conexão com a internet e tente novamente.", "OK");
                                }
                            }
                        }
                        else if(_planType == "Treino")
                        {
                            string msg;
                            if(changing) 
                                msg = await AdmUtilities.CheckIfClassSetupIsAvailable(_selectedSchedules, userId);
                            else
                                msg = await AdmUtilities.CheckIfClassSetupIsAvailable(_selectedSchedules);

                            if(msg != "")
                            {
                                var splittenMsg = msg.Split('@');
                                if (await DisplayAlert("Aulas Lotadas", splittenMsg[0], "Sim", "Não"))
                                {
                                    var todayString = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd");
                                    var makeupNumber = Int32.Parse(splittenMsg[1]);
                                    for(int i = 1; i <= makeupNumber; i++)
                                    {
                                        var finalString = todayString + "@" + i;
                                        var x = i;
                                        while (u.MCTrainDates.Contains(finalString))
                                        {
                                            x++;
                                            finalString = todayString + "@" + x;
                                        }

                                        u.MCTrainDates.Add(finalString);
                                    }

                                    u.MakeupClasses += makeupNumber;
                                }
                                else
                                {
                                    await PopupNavigation.Instance.PopAsync();
                                    return;
                                }
                            }
                            _app.TemporarySelectedSchedules[0] = _selectedSchedules;
                            if(u.UserPlan.PilatesPlan != null && !u.UserPlan.PilatesPlan.IsFloating)
                                await Navigation.PushAsync(new ClassSetupPage(u, "Pilates", changing));
                            else
                                await Navigation.PushAsync(new ClassSetupPage(u, "Yoga", changing));
                        }
                        else if (_planType == "Pilates")
                        {
                            string msg;
                            if (changing)
                                msg = await AdmUtilities.CheckIfClassSetupIsAvailable(_selectedSchedules, userId);
                            else
                                msg = await AdmUtilities.CheckIfClassSetupIsAvailable(_selectedSchedules);

                            if (msg != "")
                            {
                                var splittenMsg = msg.Split('@');
                                if (await DisplayAlert("Aulas Lotadas", splittenMsg[0], "Sim", "Não"))
                                {
                                    var todayString = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd");
                                    var makeupNumber = Int32.Parse(splittenMsg[1]);
                                    for (int i = 1; i <= makeupNumber; i++)
                                    {
                                        var finalString = todayString + "@" + i;
                                        var x = i;
                                        while (u.MCPilatesDates.Contains(finalString))
                                        {
                                            x++;
                                            finalString = todayString + "@" + x;
                                        }

                                        u.MCPilatesDates.Add(finalString);
                                    }

                                    u.MakeupClassesPilates += makeupNumber;
                                }
                                else
                                {
                                    await PopupNavigation.Instance.PopAsync();
                                    return;
                                }
                            }
                            _app.TemporarySelectedSchedules[2] = _selectedSchedules;
                            await Navigation.PushAsync(new ClassSetupPage(u, "Yoga", changing));
                        }
                    }
                }
                else
                {
                    await DisplayAlert("Aulas", "Selecione os horários das aulas para poder continuar.", "Ok");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            await PopupNavigation.Instance.PopAsync();
        }

        private string FindLastPlan()
        {
            var last = "";

            if (u.UserPlan.YogaPlan != null && !u.UserPlan.YogaPlan.IsFloating)
                last = "Yoga";
            else if (u.UserPlan.PilatesPlan != null && !u.UserPlan.PilatesPlan.IsFloating)
                last = "Pilates";
            else
                last = "Treino";

            return last;
        }

        private int GetPlanCount()
        {
            return (u.UserPlan.PilatesPlan != null ? u.UserPlan.PilatesPlan.IsFloating  ? 1 : 0 : 0) +
                   (u.UserPlan.YogaPlan != null ? u.UserPlan.YogaPlan.IsFloating ? 1 : 0 : 0) +
                   (u.UserPlan.TrainPlan != null ? u.UserPlan.TrainPlan.IsFloating ? 1 : 0 : 0);
        }

        private async Task CheckClasses(int z, int id = -1)
        {
            string msg;
            if (id != -1)
                msg = await AdmUtilities.CheckIfClassSetupIsAvailable(_selectedSchedules, userId);
            else
                msg = await AdmUtilities.CheckIfClassSetupIsAvailable(_selectedSchedules);

            if (msg != "")
            {
                var splittenMsg = msg.Split('@');
                if (await DisplayAlert("Aulas Lotadas", splittenMsg[0], "Sim", "Não"))
                {
                    var makeupNumber = Int32.Parse(splittenMsg[1]);
                    if (z == 0)
                    {
                        var todayString = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd");
                        for (int i = 1; i <= makeupNumber; i++)
                        {
                            var finalString = todayString + "@" + i;
                            var x = i;
                            while (u.MCTrainDates.Contains(finalString))
                            {
                                x++;
                                finalString = todayString + "@" + x;
                            }

                            u.MCTrainDates.Add(finalString);
                        }

                        u.MakeupClasses += makeupNumber;
                    }
                    else if(z == 1)
                    {
                        var todayString = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd");
                        for (int i = 1; i <= makeupNumber; i++)
                        {
                            var finalString = todayString + "@" + i;
                            var x = i;
                            while (u.MCTrainDates.Contains(finalString))
                            {
                                x++;
                                finalString = todayString + "@" + x;
                            }

                            u.MCYogaDates.Add(finalString);
                        }

                        u.MakeupClassesYoga += makeupNumber;
                    }
                    else if(z == 2)
                    {
                        var todayString = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd");
                        for (int i = 1; i <= makeupNumber; i++)
                        {
                            var finalString = todayString + "@" + i;
                            var x = i;
                            while (u.MCPilatesDates.Contains(finalString))
                            {
                                x++;
                                finalString = todayString + "@" + x;
                            }

                            u.MCPilatesDates.Add(finalString);
                        }

                        u.MakeupClassesPilates += makeupNumber;
                    }
                }
                else
                {
                    await PopupNavigation.Instance.PopAsync();
                    return;
                }
            }
        }
    }
}