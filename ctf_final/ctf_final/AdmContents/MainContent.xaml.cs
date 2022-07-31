using ctf_final.Models;
using ImageCircle.Forms.Plugin.Abstractions;
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
using static ctf_final.BackgroundTasks;

namespace ctf_final.AdmContents
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainContent : ContentPage
    {
        //Start Page views
        ScrollView classesView;
        ScrollView eventsView;

        ActivityIndicator classLoadingIndicator;
        ActivityIndicator eventsLoadingIndicator;
        readonly Dictionary<int, StackLayout> classDetailsList = new Dictionary<int, StackLayout>();

        //Schedules Page
        readonly Dictionary<string, Grid> scheduleDetailsList = new Dictionary<string, Grid>();
        class ScheduleEntries
        {
            public Picker TypePicker { get; set; }
            public TimePicker TimePicker { get; set; }
            public List<int> Weekdays { get; set; }
            public List<BoxView> BoxViews { get; set; }
            public Button UpdateButton { get; set; }
        }

        readonly Dictionary<int, ScheduleEntries> scheduleEntriesList = new Dictionary<int, ScheduleEntries>();

        //Classes Page
        ActivityIndicator classesLoadingSign;
        readonly List<StudentContents.MakeupClassPicker.TemporarySchedules> downloadedSchedules = new List<StudentContents.MakeupClassPicker.TemporarySchedules>();

        class WeekdaysSelections
        {
            public List<BoxView> boxViews = new List<BoxView>();
            public List<Label> labels = new List<Label>();
            public int selectedWeekday = -1;
        }
        WeekdaysSelections weekdaysSelections;
        StackLayout schedulesOfWeekdayLayout;
        SchedulesByDayOfWeek selectedDay;
        List<SchedulesByDayOfWeek.Times> showingSchedules;
        readonly List<DateTime> dts = new List<DateTime>();

        SchedulesByDayOfWeek.Times selectedClass;

        bool canChangeDayOnClasses = true;
        int selectedPage = 0;

        public MainContent()
        {
            InitializeComponent();
            SpawnStartView();

            MessagingCenter.Subscribe<PageControlMessage>(this, "OnResume", msg =>
            {
                if(selectedPage == 0)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        classLoadingIndicator.IsRunning = true;
                        classLoadingIndicator.IsVisible = true;

                        classesView.Content = new StackLayout();
                    });

                }
            });
            MessagingCenter.Subscribe<DataFinishedLoadingMessage>(this, "DataLoaded", msg =>
            {
                if(selectedPage != 0)
                    SpawnStartView();

                FillStartPageContent();
                FillEvents();
            });
            MessagingCenter.Subscribe<PageControlMessage>(this, "TodayClassesUpdated", msg =>
            {
                if (selectedPage == 0)
                {
                    FillStartPageContent();
                    FillEvents();
                }
                else if (selectedPage == 1)
                {
                    var wd = (int)DateTime.Today.DayOfWeek;

                    var dSchedule = downloadedSchedules.Find(ts => ts.Weekday == wd);

                    if (selectedDay.DayOfWeek == wd)
                        SpawnSchedulesByWeekday(wd);

                    if (selectedDay.DayOfWeek == AdmUtilities.TodayClasses.DayOfWeek)
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            schedulesOfWeekdayLayout.Children.Clear();

                            classesLoadingSign.IsRunning = true;
                            classesLoadingSign.IsVisible = true;
                        });

                    dSchedule.SelectedWeekdaySchedules = AdmUtilities.TodayClasses;
                    if (selectedDay.DayOfWeek == AdmUtilities.TodayClasses.DayOfWeek)
                        SpawnSchedulesByWeekday(AdmUtilities.TodayClasses.DayOfWeek);

                    if (selectedClass != null && msg.Command == "notPendingWrites")
                    {
                        var foundClass = AdmUtilities.TodayClasses.Classes.Find(c => c.Time == selectedClass.Time && c.Type == selectedClass.Type);
                        if (foundClass != null && foundClass != selectedClass)
                        {
                            MessagingCenter.Send(foundClass, "ChangeClassViewPage");
                        }
                    }
                    
                }
                   
            });
            MessagingCenter.Subscribe<PageControlMessage>(this, "UpdateSchedulesView", msg =>
            {
                Device.BeginInvokeOnMainThread(() => SpawnScheduleView());
            });
            MessagingCenter.Subscribe<PageControlMessage>(this, "LoadPage", message =>
            {
                try
                {
                    if(ToolbarItems.Count > 0 && message.Command != "LoadPlanPage")
                        ToolbarItems.Clear();
                    if(downloadedSchedules != null && message.Command != "LoadPlanPage")
                    {
                        downloadedSchedules.ForEach(ds =>
                        {
                            if(ds.TemporaryListener != null)
                                ds.TemporaryListener.Remove();
                        });

                        downloadedSchedules.Clear();
                    }


                    switch (message.Command)
                    {
                        case "LoadStudentsPage":
                            SpawnStudentsView();
                            break;
                        case "LoadStartPage":
                            SpawnStartView();
                            break;
                        case "LoadClassesPage":
                            SpawnClassesView();
                            break;
                        case "LoadTeachersPage":
                            SpawnTeacher();
                            break;
                        case "LoadEventsPage":
                            SpawnEvents();
                            break;
                        case "LoadReviewPage":
                            SpawnReview();
                            break;
                        case "LoadPlanPage":
                            selectedPage = 6;
                            Navigation.PushAsync(new Students.PlanPicker(null, null, true));
                            break;
                        case "LoadSchedulesPage":
                            SpawnScheduleView();
                            break;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        //Classes view: manage setup classes, experimental classes go here too
        public void SpawnClassesView()
        {
            if (detailLayout.Children.Count > 1)
                detailLayout.Children.RemoveAt(1);

            Title = "Aulas";
            selectedPage = 1;

            weekdaysSelections = new WeekdaysSelections();
            downloadedSchedules.Add(new StudentContents.MakeupClassPicker.TemporarySchedules(AdmUtilities.TodayClasses.DayOfWeek, AdmUtilities.TodayClasses, null));

            Grid classesView = new Grid()
            {
                ColumnSpacing = 0,
                RowSpacing = 0,
                BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]
            };

            classesView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35, GridUnitType.Star) });
            classesView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65, GridUnitType.Star) });

            int today = (int) DateTime.Today.DayOfWeek;
            int starterID = 0;

            for (int i = 0; i < 7; i++)
            {
                classesView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });

                //REORDER LIST BY WEEKDAY
                int z = i < today ? 7 - (today - i) : i - today;
                DateTime day = DateTime.Today.AddDays(z);
                dts.Add(day);
                string labelText = z == 0 ? "Hoje" : z == 1 ? "Amanhã" : SharedUtilities.IntToWeekday((int)day.DayOfWeek) + "\n" + day.Day + "/" + day.Month;

                BoxView weekdayButton = new BoxView
                {
                    BackgroundColor = z == 0 ? (Color)Application.Current.Resources["Orange"] : (Color)Application.Current.Resources["PrimaryTransparent"],
                    ClassId = i.ToString()
                };

                TapGestureRecognizer changeWdTap = new TapGestureRecognizer();
                changeWdTap.Tapped += (sender, ex) =>
                {
                    if (canChangeDayOnClasses)
                    {
                        canChangeDayOnClasses = false;

                        classesLoadingSign.IsRunning = true;
                        classesLoadingSign.IsVisible = true;

                        if (schedulesOfWeekdayLayout != null)
                            schedulesOfWeekdayLayout.Children.Clear();

                        var selectedBoxView = sender as BoxView;

                        int id = Int32.Parse(selectedBoxView.ClassId);

                        if (weekdaysSelections.selectedWeekday != id)
                        {
                            weekdaysSelections.boxViews.ForEach(bv => bv.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"]);
                            weekdaysSelections.labels.ForEach(l => l.TextColor = (Color)Application.Current.Resources["Orange"]);
                            selectedBoxView.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                            weekdaysSelections.labels[id].TextColor = (Color)Application.Current.Resources["TextDark"];
                        }

                        weekdaysSelections.selectedWeekday = id;
                        SpawnSchedulesByWeekday(id);
                    }
                };
                changeWdTap.NumberOfTapsRequired = 1;
                weekdayButton.GestureRecognizers.Add(changeWdTap);

                classesView.Children.Add(weekdayButton, 0, z);
                weekdaysSelections.boxViews.Add(weekdayButton);

                Label dayLabel = new Label
                {
                    Text = labelText,
                    InputTransparent = true,
                    Margin = new Thickness(10, 0),
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = z == 0 ? (Color)Application.Current.Resources["TextDark"] : (Color)Application.Current.Resources["Orange"]
                };
                classesView.Children.Add(dayLabel, 0, z);
                weekdaysSelections.labels.Add(dayLabel);

                classesView.Children.Add(new BoxView
                {
                    InputTransparent = true,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.End,
                    HeightRequest = 1,
                    BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"]
                }, 0, i);

                if (z == 0)
                {
                    starterID = i;
                    weekdaysSelections.selectedWeekday = i;
                }  
            }
            classesView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            classesView.Children.Add(new BoxView
            {
                BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"]
            }, 0, 7);

            schedulesOfWeekdayLayout = new StackLayout(){ Spacing = 0 };
            schedulesOfWeekdayLayout.Children.Add(new Label
            {
                Text = "Selecione um dia!",
                TextColor = (Color)Application.Current.Resources["Orange"],
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(12),
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
            });

            ScrollView sv = new ScrollView { BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"] };
            sv.Content = schedulesOfWeekdayLayout;
            classesView.Children.Add(sv, 1, 0);
            Grid.SetRowSpan(sv, 8);

            classesLoadingSign = new ActivityIndicator()
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                IsRunning = true,
                IsVisible = true,
                Color = (Color)_app.Resources["Orange"]
            };
            classesView.Children.Add(classesLoadingSign, 1, 0);
            Grid.SetRowSpan(classesLoadingSign, 8); 

            BoxView sideSeparator = new BoxView()
            {
                BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"],
                WidthRequest = 1,
                HorizontalOptions = LayoutOptions.Start
            };
            classesView.Children.Add(sideSeparator, 1, 0);
            Grid.SetRowSpan(sideSeparator, 8);

            SpawnSchedulesByWeekday(starterID);

            detailLayout.Children.Add(classesView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
        }
        public async void SpawnSchedulesByWeekday(int weekdayIndex)
        {
            List<View> scheduleLayouts = new List<View>();
            var times = new List<SchedulesByDayOfWeek.Times>();

            try
            {
                var ds = downloadedSchedules.Find(ts => ts.Weekday == weekdayIndex);
                if (ds != null)
                {
                    selectedDay = ds.SelectedWeekdaySchedules;
                    times = ds.SelectedWeekdaySchedules.Classes;
                }
                else
                {
                    var queryRef = CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("real_schedules")
                                        .Document(weekdayIndex.ToString());
                    var sbdw = await SharedUtilities.UpdateOutdatedRealschedules(queryRef, weekdayIndex);
                    
                    if(sbdw != null){
                        var temporaryListener = queryRef.AddSnapshotListener(async (snp, error) =>
                        {
                            if (!snp.Metadata.IsFromCache)
                            {
                                var update = snp.ToObject<SchedulesByDayOfWeek>();
                                if (update != null)
                                {
                                    if(!snp.Metadata.HasPendingWrites)
                                        await SharedUtilities.FixDataInconsistency(update, snp);

                                    var dSchedule = downloadedSchedules.Find(dsch => dsch.Weekday == update.DayOfWeek);
                                    if (dSchedule.SelectedWeekdaySchedules != update)
                                    {
                                        dSchedule.SelectedWeekdaySchedules = update;
                                        if (selectedDay.DayOfWeek == update.DayOfWeek)
                                        {
                                            Device.BeginInvokeOnMainThread(() =>
                                            {
                                                schedulesOfWeekdayLayout.Children.Clear();

                                                classesLoadingSign.IsRunning = true;
                                                classesLoadingSign.IsVisible = true;
                                            });
                                            SpawnSchedulesByWeekday(sbdw.DayOfWeek);
                                        }

                                        if (selectedClass != null && !snp.Metadata.HasPendingWrites)
                                        {
                                            var foundClass = update.Classes.Find(c => c.Time == selectedClass.Time && c.Type == selectedClass.Type);
                                            if (foundClass != null && foundClass != selectedClass)
                                            {
                                                MessagingCenter.Send(foundClass, "ChangeClassViewPage");
                                            }
                                        }
                                    }
                                }
                            }
                        });

                        downloadedSchedules.Add(new StudentContents.MakeupClassPicker.TemporarySchedules(weekdayIndex, sbdw, temporaryListener));
                        times = sbdw.Classes.OrderBy(t => t.Time).ToList();
                        selectedDay = sbdw;
                    }
                    else
                    {
                        times = null;
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //UI
            try {
                times = times.OrderBy(t => t.Time).ToList();
                showingSchedules = times;

                int i = 0;
                if (times != null)
                {
                    times.ForEach(t =>
                    {
                        StackLayout timeLayout = new StackLayout()
                        {
                            Orientation = StackOrientation.Horizontal,
                            BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                            Padding = new Thickness(8),
                            ClassId = i.ToString(),
                            Spacing = 8
                        };

                        TapGestureRecognizer tapOpenSchedule = new TapGestureRecognizer();
                        tapOpenSchedule.Tapped += async (sender, ex) =>
                        {
                            try
                            {
                                int wd = weekdaysSelections.selectedWeekday;
                                var id = Int32.Parse((sender as StackLayout).ClassId);

                                selectedClass = showingSchedules[id];
                                await Navigation.PushAsync(new ClassEditPage(showingSchedules[id], wd, dts[wd]));
                        }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        };
                        tapOpenSchedule.NumberOfTapsRequired = 1;
                        timeLayout.GestureRecognizers.Add(tapOpenSchedule);

                        StackLayout generalInfo = new StackLayout()
                        {
                            Spacing = 2,
                            HorizontalOptions = LayoutOptions.StartAndExpand
                        };
                        generalInfo.Children.Add(new Label
                        {
                            Text = t.Time,
                            TextColor = (Color)Application.Current.Resources["Orange"],
                            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
                        });
                        generalInfo.Children.Add(new Label
                        {
                            Text = t.Type,
                            TextColor = (Color)Application.Current.Resources["TextLight"],
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
                        });

                        timeLayout.Children.Add(generalInfo);
                        timeLayout.Children.Add(new Label
                        {
                            Text = t.StudentsList.Count() + " alunos",
                            TextColor = (Color)Application.Current.Resources["TextLight"],
                            VerticalOptions = LayoutOptions.Center,
                            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
                        });

                    //TODO - ARROW_ACCENT
                    timeLayout.Children.Add(new Image
                        {
                            Source = "ic_right_arrow.png",
                            Aspect = Aspect.AspectFit
                        });

                        scheduleLayouts.Add(timeLayout);
                        scheduleLayouts.Add(new BoxView
                        {
                            HeightRequest = 1,
                            BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"],
                            HorizontalOptions = LayoutOptions.Fill
                        });
                        i++;
                    });
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            try
            { 
                Device.BeginInvokeOnMainThread(() =>
                {
                    schedulesOfWeekdayLayout.Children.Clear();
                    if (scheduleLayouts.Count > 0)
                    {
                        scheduleLayouts.ForEach(layout =>
                        {
                            schedulesOfWeekdayLayout.Children.Add(layout);
                        });
                    }
                    else
                    {
                        schedulesOfWeekdayLayout.Children.Add(new Label
                        {
                            Text = "Nenhuma aula encontrada...",
                            TextColor = (Color)Application.Current.Resources["Orange"],
                            HorizontalOptions = LayoutOptions.Center,
                            Margin = new Thickness(12),
                            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
                        });
                    }

                    classesLoadingSign.IsRunning = false;
                    classesLoadingSign.IsVisible = false;

                    canChangeDayOnClasses = true;
                });
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
  
        //Schedules view: add, remove and edit the schedule
        public void SpawnScheduleView()
        {
            if (detailLayout.Children.Count > 1)
                detailLayout.Children.RemoveAt(1);
            if (scheduleDetailsList.Count > 0)
                scheduleDetailsList.Clear();
            if (scheduleEntriesList.Count > 0)
                scheduleEntriesList.Clear();

            Title = "Horários";
            selectedPage = 3;

            if (ToolbarItems.Count < 1)
            {
                ToolbarItem viewSchedules = new ToolbarItem { IconImageSource = "ic_schedule.png" };
                viewSchedules.Clicked += async (sender, ex) =>
                {
                    await Navigation.PushAsync(new ScheduleViewer());
                };
                ToolbarItems.Add(viewSchedules);

                ToolbarItem addSchedule = new ToolbarItem { IconImageSource = "ic_plus_accent.png" };
                addSchedule.Clicked += async (sender, ex) =>
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.AddSchedulePopup(), true);
                };
                ToolbarItems.Add(addSchedule);
            }

            StackLayout schedulesContent = new StackLayout()
            {
                Padding = new Thickness(12),
                Spacing = 12,
                HorizontalOptions = LayoutOptions.Fill
            };

            int z = 0;
            foreach (Schedule s in _app.AdmSchedules)
            {
                //SCHEDULE HEADER>>
                StackLayout scheduleInfo = new StackLayout()
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 12,
                    Padding = 14,
                    ClassId = z.ToString(),
                    BackgroundColor = BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"]
                };
                TapGestureRecognizer tapExpand = new TapGestureRecognizer();
                tapExpand.Tapped += async (sender, ex) =>
                {
                    var layout = sender as StackLayout;
                    int id = Int32.Parse(layout.ClassId);

                    if (scheduleDetailsList.ContainsKey(layout.ClassId))
                    {
                        scheduleDetailsList[layout.ClassId].IsVisible ^= true;

                        if (scheduleDetailsList[layout.ClassId].IsVisible)
                            await (layout.Children.Last() as Image).RotateTo(180, 100);
                        else
                        {
                            //Reset entries after closing
                            var selectedSchedule = _app.AdmSchedules[id];
                            var entries = scheduleEntriesList[id];
                            int[] sTime = new int[2] { Int32.Parse(selectedSchedule.Time.Substring(0, 2)), Int32.Parse(selectedSchedule.Time.Substring(3, 2)) };

                            entries.TimePicker.Time = new TimeSpan(sTime[0], sTime[1], 0);
                            entries.TypePicker.SelectedItem = selectedSchedule.Type;

                            List<int> week = new List<int>();
                            selectedSchedule.Classes.ForEach(c => week.Add(c.Day));
                            entries.Weekdays = week;

                            entries.BoxViews.ForEach(bv => bv.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]);
                            week.ForEach(x => entries.BoxViews[x].BackgroundColor = (Color)Application.Current.Resources["Orange"]);

                            entries.UpdateButton.IsEnabled = false;

                            await (layout.Children.Last() as Image).RotateTo(0, 100);
                        }
                    }
                    else
                    {
                        scheduleDetailsList[layout.ClassId] = CreateScheduleDetail(id, _app.AdmSchedules[id], layout.Parent as StackLayout);
                        await (layout.Children.Last() as Image).RotateTo(180, 100);
                    }
                };
                tapExpand.NumberOfTapsRequired = 1;
                scheduleInfo.GestureRecognizers.Add(tapExpand);

                StackLayout mainInfo = new StackLayout()
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.StartAndExpand,
                    Spacing = 4
                };

                mainInfo.Children.Add(new Label()
                {
                    Text = s.Time,
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.StartAndExpand
                });

                var formattedWeekdays = new FormattedString();
                for (int i = 0; i < 7; i++)
                {
                    formattedWeekdays.Spans.Add(new Span
                    {
                        Text = SharedUtilities.IntToWeekday(i).Substring(0, 1) + " ",
                        ForegroundColor = s.Classes.Any(c => c.Day == i) ? (Color) _app.Resources["TextLight"] : (Color)Application.Current.Resources["LightTransparent"]
                    });
                }

                mainInfo.Children.Add(new Label()
                {
                    FormattedText = formattedWeekdays,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.StartAndExpand
                });
                scheduleInfo.Children.Add(mainInfo);
                scheduleInfo.Children.Add(new Label()
                {
                    Text = s.Type,
                    TextColor = s.Type.Equals("Treino") ? (Color)Application.Current.Resources["Orange"] : (Color)Application.Current.Resources["Yoga"],
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End
                });

                Image downArrow = new Image
                {
                    Source = "ic_arrow_down.png",
                    Aspect = Aspect.AspectFit,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End
                };
                scheduleInfo.Children.Add(downArrow);

                //SCHEDULE DETAILS>>
                StackLayout scheduleLayout = new StackLayout()
                {
                    Spacing = 0
                };

                scheduleLayout.Children.Add(scheduleInfo);
                //scheduleLayout.Children.Add(scheduleDetails);

                schedulesContent.Children.Add(scheduleLayout);
                z++;
            }

            ScrollView schedulesView = new ScrollView()
            {
                Content = schedulesContent
            };

            detailLayout.Children.Add(schedulesView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
        }
        public Grid CreateScheduleDetail(int z, Schedule s, StackLayout viewToAdd)
        {
            Grid scheduleDetails = new Grid()
            {
                ColumnSpacing = 0,
                RowSpacing = 0,
                ClassId = z.ToString(),
                BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                IsVisible = true
            };
            //GRID DEFINITIONS>>
            scheduleDetails.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            scheduleDetails.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            scheduleDetails.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

            int[] time = new int[2] { Int32.Parse(s.Time.Substring(0, 2)), Int32.Parse(s.Time.Substring(3, 2)) };

            TimePicker timePicker = new TimePicker()
            {
                Time = new TimeSpan(time[0], time[1], 0),
                TextColor = (Color)Application.Current.Resources["Orange"],
                Margin = new Thickness(10, 10, 0, 0),
                Format = "HH:mm",
                BackgroundColor = Device.RuntimePlatform == Device.iOS ? (Color)_app.Resources["PrimaryDark"] : Color.Transparent
        };
            timePicker.PropertyChanged += (sender, ex) =>
            {
                CheckIfScheduleCanUpdate(Int32.Parse(((sender as TimePicker).Parent as Grid).ClassId));
            };

            Picker typePicker = new Picker()
            {
                ItemsSource = new List<string>
                    {
                        "Treino",
                        "Yoga",
                        "Pilates"
                    },
                TextColor = (Color)Application.Current.Resources["Orange"],
                Margin = new Thickness(0, 10, 10, 0),
                SelectedItem = s.Type
            };
            typePicker.SelectedIndexChanged += (sender, ex) =>
            {
                CheckIfScheduleCanUpdate(Int32.Parse(((sender as Picker).Parent as Grid).ClassId));
            };

            if(Device.RuntimePlatform == Device.iOS)
                typePicker.BackgroundColor = (Color) _app.Resources["PrimaryDark"];

            Grid weekdaysGrid = new Grid()
            {
                BackgroundColor = (Color)Application.Current.Resources["Primary"],
                Margin = new Thickness(26, 10),
                ColumnSpacing = 1,
                Padding = 1
            };

            List<int> wd = new List<int>();
            List<BoxView> bvs = new List<BoxView>();
            for (int i = 0; i < 7; i++)
            {
                if (s.Classes.Any(c => c.Day == i))
                    wd.Add(i);

                BoxView bg = new BoxView()
                {
                    BackgroundColor = s.Classes.Any(c => c.Day == i) ? (Color)Application.Current.Resources["Orange"] : (Color)Application.Current.Resources["DarkTransparent"],
                    ClassId = i.ToString()
                };

                TapGestureRecognizer tapWeekday = new TapGestureRecognizer();
                tapWeekday.Tapped += (sender, ex) =>
                {
                    try
                    {
                        var bv = sender as BoxView;
                        var details = (bv.Parent as Grid).Parent as Grid;

                        int wdIndex = Int32.Parse(bv.ClassId);
                        var selectedSchedule = scheduleEntriesList[Int32.Parse(details.ClassId)];

                        if (selectedSchedule.Weekdays.Contains(wdIndex))
                        {
                            selectedSchedule.Weekdays.Remove(wdIndex);
                            bv.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                        }
                        else
                        {
                            selectedSchedule.Weekdays.Add(wdIndex);
                            bv.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                        }
                        CheckIfScheduleCanUpdate(Int32.Parse(details.ClassId));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                };
                tapWeekday.NumberOfTapsRequired = 1;

                bg.GestureRecognizers.Add(tapWeekday);

                weekdaysGrid.Children.Add(bg, i, 0);
                bvs.Add(bg);

                weekdaysGrid.Children.Add(new Label()
                {
                    Text = SharedUtilities.IntToWeekday(i).Substring(0, 1),
                    TextColor = BackgroundColor = (Color)Application.Current.Resources["TextLight"],
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    InputTransparent = true
                }, i, 0);
            }

            Button removeBtn = new Button()
            {
                Text = "REMOVER",
                ClassId = z.ToString(),
                TextColor = (Color)Application.Current.Resources["TextDark"],
                BackgroundColor = (Color)Application.Current.Resources["Red"]
            };
            removeBtn.Clicked += async (sender, ex) =>
            {
                if (await DisplayAlert("Remover", "Deseja remover esse horário? (Esta ação não pode ser desfeita)", "Sim", "Não"))
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                    int id = Int32.Parse((sender as Button).ClassId);
                    var selectedSch = _app.AdmSchedules[id];

                    if(await AdmUtilities.RemoveSchedule(selectedSch))
                    {
                        SpawnScheduleView();
                        await DisplayAlert("Sucesso", "Horário removido com sucesso! Verifique a aba 'plano' dos alunos deste horário se precisar alterá-los", "Ok");
                    }
                    else
                    {
                        await DisplayAlert("Erro", "Não foi possível remover o horário, tente novamente mais tarde", "Ok");
                    }
                    await PopupNavigation.Instance.PopAsync();
                }
            };

            Button updateBtn = new Button()
            {
                Text = "SALVAR",
                ClassId = z.ToString(),
                IsEnabled = false,
                TextColor = (Color)Application.Current.Resources["TextDark"],
                BackgroundColor = (Color)Application.Current.Resources["Orange"]
            };
            updateBtn.Clicked += UpdateScheduleBtn;

            scheduleEntriesList.Add(z, new ScheduleEntries()
            {
                TimePicker = timePicker,
                TypePicker = typePicker,
                Weekdays = wd,
                UpdateButton = updateBtn,
                BoxViews = bvs
            });

            scheduleDetails.Children.Add(timePicker);
            scheduleDetails.Children.Add(typePicker, 1, 0);

            scheduleDetails.Children.Add(weekdaysGrid, 0, 1);
            Grid.SetColumnSpan(weekdaysGrid, 2);

            scheduleDetails.Children.Add(new BoxView() { BackgroundColor = (Color)Application.Current.Resources["Red"] }, 0, 2);
            scheduleDetails.Children.Add(removeBtn, 0, 2);

            scheduleDetails.Children.Add(new BoxView() { BackgroundColor = (Color)Application.Current.Resources["Orange"] }, 1, 2);
            scheduleDetails.Children.Add(updateBtn, 1, 2);

            viewToAdd.Children.Add(scheduleDetails);
            return scheduleDetails;
        }
        
        //Students view: evaluate, register and manage everything student related
        public void SpawnStudentsView()
        {
            if(detailLayout.Children.Count>1)
                detailLayout.Children.RemoveAt(1);

            Title = "Alunos";
            selectedPage = 2;

            //RESOURCES
            Style stackStyle = new Style(typeof(StackLayout));
            stackStyle.Setters.Add(new Setter()
            {
                Property = StackLayout.OrientationProperty,
                Value = StackOrientation.Horizontal
            });
            stackStyle.Setters.Add(new Setter()
            {
                Property = View.HorizontalOptionsProperty,
                Value = LayoutOptions.CenterAndExpand
            });
            stackStyle.Setters.Add(new Setter()
            {
                Property = View.InputTransparentProperty,
                Value = true
            });

            Style boxStyle = new Style(typeof(BoxView));
            boxStyle.Setters.Add(new Setter()
            {
                Property = BackgroundColorProperty,
                Value = Application.Current.Resources["DarkTransparent"]
            });
            boxStyle.Setters.Add(new Setter()
            {
                Property = View.HorizontalOptionsProperty,
                Value = LayoutOptions.FillAndExpand
            });

            Style imageStyle = new Style(typeof(Image));
            imageStyle.Setters.Add(new Setter()
            {
                Property = Image.AspectProperty,
                Value = Aspect.AspectFit
            });
            imageStyle.Setters.Add(new Setter()
            {
                Property = View.VerticalOptionsProperty,
                Value = LayoutOptions.Fill
            });

            Style labelStyle = new Style(typeof(Label));
            labelStyle.Setters.Add(new Setter()
            {
                Property = Label.FontSizeProperty,
                Value = Device.GetNamedSize(NamedSize.Title, typeof(Label))
            });
            labelStyle.Setters.Add(new Setter()
            {
                Property = View.VerticalOptionsProperty,
                Value = LayoutOptions.Center
            });
            labelStyle.Setters.Add(new Setter()
            {
                Property = Label.TextColorProperty,
                Value = Application.Current.Resources["Orange"]
            });

            //BASE GRID
            Grid grid = new Grid()
            {
                Padding = 10,
                RowSpacing = 10,
                HorizontalOptions = LayoutOptions.Fill,
                Resources = new ResourceDictionary()
                {
                    stackStyle,
                    boxStyle,
                    imageStyle,
                    labelStyle
                }
            };

            //LAYOUTS
            StackLayout ratingLayout = new StackLayout();
            BoxView ratingBv = new BoxView(); 

            var tapRating = new TapGestureRecognizer();
            tapRating.Tapped += (s, ex) => {
                TapGestureRecognizer_Rating(s, ex);
            };
            tapRating.NumberOfTapsRequired = 1;
            ratingBv.GestureRecognizers.Add(tapRating);

            ratingLayout.Children.Add(new Image() { Source = "ic_students_rating.png" });
            ratingLayout.Children.Add(new Label() { Text = "AVALIAR" });


            StackLayout cadLayout = new StackLayout();
            BoxView cadBv = new BoxView();

            var tapCad = new TapGestureRecognizer();
            tapCad.Tapped += (s, ex) => {
                TapGestureRecognizer_Cad(s, ex);
            };
            tapCad.NumberOfTapsRequired = 1;
            cadBv.GestureRecognizers.Add(tapCad);

            cadLayout.Children.Add(new Image() { Source = "ic_student_cad.png" });
            cadLayout.Children.Add(new Label() { Text = "CADASTRAR" });

            StackLayout manageLayout = new StackLayout();
            BoxView manageBv = new BoxView();

            var tapManage = new TapGestureRecognizer();
            tapManage.Tapped += (s, ex) => {
                TapGestureRecognizer_Manage(s, ex);
            };
            tapManage.NumberOfTapsRequired = 1;
            manageBv.GestureRecognizers.Add(tapManage);

            manageLayout.Children.Add(new Image() { Source = "ic_students_manage.png" });
            manageLayout.Children.Add(new Label() { Text = "GERENCIAR" });

            //ADDING LAYOUTS TO GRID
            grid.Children.Add(ratingBv);
            grid.Children.Add(ratingLayout);

            grid.Children.Add(cadBv, 0, 1);
            grid.Children.Add(cadLayout, 0, 1);

            grid.Children.Add(manageBv, 0, 2);
            grid.Children.Add(manageLayout, 0, 2);

            //INFLATING GRID TO VIEW
            detailLayout.Children.Add(grid, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
        }

        //Teacher view
        public void SpawnTeacher()
        {
            if (detailLayout.Children.Count > 1)
                detailLayout.Children.RemoveAt(1);
            if (scheduleDetailsList.Count > 0)
                scheduleDetailsList.Clear();
            if (scheduleEntriesList.Count > 0)
                scheduleEntriesList.Clear();

            Title = "Professores";
            selectedPage = 6;

            if (ToolbarItems.Count < 1)
            {
                ToolbarItem addSchedule = new ToolbarItem { IconImageSource = "ic_plus_accent.png" };
                addSchedule.Clicked += async (sender, ex) =>
                {
                    await Navigation.PushAsync(new Teacher.TeacherCadastre());
                };
                ToolbarItems.Add(addSchedule);
            }

            StackLayout teachersContent = new StackLayout()
            {
                Padding = 0,
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Fill
            };

            int z = 0;
            foreach (User t in _app.Teachers)
            {
                StackLayout teacherInfo = new StackLayout()
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 0,
                    BackgroundColor = BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]
                };

                StackLayout firstDetails = new StackLayout()
                {
                    Orientation = StackOrientation.Vertical,
                    Spacing = 0,
                    Padding = 10
                };

                firstDetails.Children.Add(new Label()
                {
                    Text = t.Name,
                    Padding = new Thickness(10, 0),
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Start
                });

                firstDetails.Children.Add(new Label()
                {
                    Text = t.Birthday.Substring(0, 2) + "/" + t.Birthday.Substring(2, 2) + "/" + t.Birthday.Substring(4, 4),
                    Padding = new Thickness(10, 0),
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                });

                teacherInfo.Children.Add(firstDetails);
                teacherInfo.Children.Add(new Label()
                {
                    Text = t.UserID.ToString(),
                    Padding = new Thickness(10, 0),
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.StartAndExpand
                });

                var xButton = new Image
                {
                    ClassId = z.ToString(),
                    Source = "ic_plus_accent.png",
                    Aspect = Aspect.AspectFit,
                    Margin = 10,
                    Scale = 1.5,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.End,
                    Rotation = 45
                };
                TapGestureRecognizer tapRemove = new TapGestureRecognizer();
                xButton.GestureRecognizers.Add(tapRemove);
                tapRemove.Tapped += async (sender, ex) =>
                {
                    if (await DisplayAlert("Remover", "Deseja remover esse professor?", "Sim", "Não"))
                    {
                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                        int id = Int32.Parse((sender as Image).ClassId);
                        var selectedTeacher = _app.Teachers[id];

                        if (await AdmUtilities.RemoveTeacher(selectedTeacher))
                        {
                            SpawnTeacher();
                            await DisplayAlert("Sucesso", "Professor removido com sucesso!", "Ok");
                        }
                        else
                        {
                            await DisplayAlert("Erro", "Não foi possível remover o professor, tente novamente mais tarde", "Ok");
                        }
                        await PopupNavigation.Instance.PopAsync();
                    }
                };
                teacherInfo.Children.Add(xButton);

                BoxView divider = new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["LightTransparent"],
                    VerticalOptions = LayoutOptions.End,
                    HeightRequest = 1
                };

                teachersContent.Children.Add(teacherInfo);
                teachersContent.Children.Add(divider);
                z++;
            }

            ScrollView teachersView = new ScrollView()
            {
                Content = teachersContent
            };
            detailLayout.Children.Add(teachersView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
        }

        //Events view
        public void SpawnEvents()
        {
            if (detailLayout.Children.Count > 1)
                detailLayout.Children.RemoveAt(1);
            if (scheduleDetailsList.Count > 0)
                scheduleDetailsList.Clear();
            if (scheduleEntriesList.Count > 0)
                scheduleEntriesList.Clear();

            Title = "Eventos";
            selectedPage = 6;

            if (ToolbarItems.Count < 1)
            {
                ToolbarItem addSchedule = new ToolbarItem { IconImageSource = "ic_plus_accent.png" };
                addSchedule.Clicked += async (sender, ex) =>
                {
                    await Navigation.PushAsync(new Event.EventCadastre());
                };
                ToolbarItems.Add(addSchedule);
            }

            StackLayout eventsContent = new StackLayout()
            {
                Padding = 10,
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill
            };

            int z = 0;
            foreach (Events e in _app.SavedEvents)
            {
                Grid eventGrid = new Grid()
                {
                    Padding = 0,
                    ColumnSpacing = 0,
                    RowSpacing = 0,
                    BackgroundColor = BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]
                };

                eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                eventGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4, GridUnitType.Star) });
                eventGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                eventGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                BoxView bg = new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["PrimaryDark"],
                    VerticalOptions = LayoutOptions.FillAndExpand
                };
                eventGrid.Children.Add(bg);
                Grid.SetColumnSpan(bg, 3);
                Grid.SetRowSpan(bg, 2);

                eventGrid.Children.Add(new Label()
                {
                    Text = e.Name.ToUpper(),
                    Padding = new Thickness(10, 10, 0, 0),
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.StartAndExpand
                });

                eventGrid.Children.Add(new Label()
                {
                    Text = e.Date.Substring(0, 5) + " - " + e.Time.Substring(0, 5),
                    Padding = new Thickness(10, 0, 0, 10),
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.StartAndExpand
                }, 0, 1);

                var removeImg = new Image()
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    Source = "ic_plus_accent",
                    Rotation = 45,
                    Scale = 1.5,
                    ClassId = z.ToString()
                };
                var removeBtn = new TapGestureRecognizer();
                removeBtn.NumberOfTapsRequired = 1;
                removeBtn.Tapped += async (sender, ex) =>
                {
                    if (await DisplayAlert("Remover", "Deseja remover esse evento?", "Sim", "Não"))
                    {
                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                        int id = Int32.Parse((sender as Image).ClassId);
                        var selectedEvent = _app.SavedEvents[id];

                        if (await AdmUtilities.RemoveEvent(selectedEvent))
                        {
                            SpawnEvents();
                            await DisplayAlert("Sucesso", "Evento removido com sucesso!", "Ok");
                        }
                        else
                        {
                            await DisplayAlert("Erro", "Não foi possível remover o Evento, tente novamente mais tarde", "Ok");
                        }
                        await PopupNavigation.Instance.PopAsync();
                    }
                };
                removeImg.GestureRecognizers.Add(removeBtn);
                eventGrid.Children.Add(removeImg, 2, 0);
                Grid.SetRowSpan(removeImg, 2);

                var editImg = new Image()
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    Source = "ic_edit",
                    ClassId = z.ToString()
                };
                var editBtn = new TapGestureRecognizer();
                editBtn.NumberOfTapsRequired = 1;
                editBtn.Tapped += async (sender, ex) =>
                {
                    int id = Int32.Parse((sender as Image).ClassId);
                    var selectedEvent = _app.SavedEvents[id];

                    await Navigation.PushAsync(new Event.EventCadastre(selectedEvent));
                };
                editImg.GestureRecognizers.Add(editBtn);
                eventGrid.Children.Add(editImg, 1, 0);
                Grid.SetRowSpan(editImg, 2);

                if (!String.IsNullOrEmpty(e.Description))
                {
                    var desc = new Label()
                    {
                        Text = e.Description,
                        Padding = new Thickness(10, 5, 10, 10),
                        TextColor = (Color)Application.Current.Resources["TextLight"],
                        FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                        VerticalOptions = LayoutOptions.Start,
                        HorizontalOptions = LayoutOptions.StartAndExpand
                    };
                    eventGrid.Children.Add(desc, 0, 2);
                    Grid.SetColumnSpan(desc, 2);
                }

                var userList = new Button()
                {
                    Text = "Usuários confirmados",
                    BackgroundColor = (Color)Application.Current.Resources["Orange"],
                    TextColor = (Color)Application.Current.Resources["TextDark"],
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    ClassId = z.ToString()
                };
                userList.Clicked += async (sender, ex) =>
                {
                    int id = Int32.Parse((sender as Button).ClassId);
                    var selectedEvent = _app.SavedEvents[id];

                    await Navigation.PushAsync(new Students.StudentsSelectionPage("check", selectedEvent.ConfirmedUsers));
                };
                eventGrid.Children.Add(userList, 0, 3);
                Grid.SetColumnSpan(userList, 3);

                eventsContent.Children.Add(eventGrid);
                z++;
            }

            ScrollView eventsView = new ScrollView()
            {
                Content = eventsContent
            };
            detailLayout.Children.Add(eventsView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
        }

        //Review view
        public void SpawnReview()
        {
            if (detailLayout.Children.Count > 1)
                detailLayout.Children.RemoveAt(1);
            if (scheduleDetailsList.Count > 0)
                scheduleDetailsList.Clear();
            if (scheduleEntriesList.Count > 0)
                scheduleEntriesList.Clear();

            Title = "Pesquisas";
            selectedPage = 6;

            if (ToolbarItems.Count < 1)
            {
                ToolbarItem addSchedule = new ToolbarItem { IconImageSource = "ic_plus_accent.png" };
                addSchedule.Clicked += async (sender, ex) =>
                {
                    await Navigation.PushAsync(new Review.ResearchCadastre());
                };
                ToolbarItems.Add(addSchedule);
            }

            StackLayout eventsContent = new StackLayout()
            {
                Padding = 10,
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill
            };

            int z = 0;
            foreach (Questionnaire e in _app.QuestionnaireList)
            {
                StackLayout eventGrid = new StackLayout()
                {
                    Padding = 0,
                    Spacing = 0,
                    BackgroundColor = (Color)_app.Resources["PrimaryDark"],
                };

                eventGrid.Children.Add(new Label()
                {
                    Text = e.QuestionnaireTitle.ToUpper() + " - " + e.CreationDate,
                    Padding = 10,
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                });

                var userList = new Button()
                {
                    Text = "Detalhes",
                    BackgroundColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                    TextColor = (Color)Application.Current.Resources["TextDark"],
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    ClassId = z.ToString()
                };
                userList.Clicked += async (sender, ex) =>
                {
                    int id = Int32.Parse((sender as Button).ClassId);
                    var selectedQ = _app.QuestionnaireList[id];

                    await Navigation.PushAsync(new Review.ReviewDetails(selectedQ));
                };
                eventGrid.Children.Add(userList);
                eventsContent.Children.Add(eventGrid);
                z++;
            }

            ScrollView eventsView = new ScrollView()
            {
                Content = eventsContent
            };
            detailLayout.Children.Add(eventsView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
        }

        //Main view: resumes the day's schedules and changes around the app
        public void SpawnStartView()
        {
            if (detailLayout.Children.Count > 1)
                detailLayout.Children.RemoveAt(1);
            if (classDetailsList.Count > 0)
                classDetailsList.Clear();
            if (ToolbarItems.Count > 0)
                ToolbarItems.Clear();

            Title = "Bem vindo!";
            selectedPage = 0;

            Style headerStyle = new Style(typeof(StackLayout)) { BaseResourceKey = "header"};
            headerStyle.Setters.Add(new Setter
            {
                Property = View.HorizontalOptionsProperty,
                Value = LayoutOptions.FillAndExpand
            });
            headerStyle.Setters.Add(new Setter
            {
                Property = BackgroundColorProperty,
                Value = Application.Current.Resources["PrimaryDark"]
            });

            //EVENTS VIEW >>

            StackLayout eventsHeader = new StackLayout()
            {
                Style = headerStyle,
                Padding = new Thickness(0, 6)
            };

            Label eventsHeaderLabel = new Label()
            {
                TextColor = (Color)Application.Current.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                Text = "EVENTOS",
                HorizontalOptions = LayoutOptions.Center
            };
            eventsHeader.Children.Add(eventsHeaderLabel);

            eventsView = new ScrollView()
            {
                VerticalOptions = LayoutOptions.StartAndExpand
            };

            StackLayout eventsLayout = new StackLayout()
            {
                BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                Spacing = 0,
                Margin = new Thickness(8)
            };

            eventsLayout.Children.Add(eventsHeader);
            eventsLayout.Children.Add(eventsView);

            //CLASSES VIEW >>

            //HEADER >>
            StackLayout classesHeader = new StackLayout()
            {
                Style = headerStyle,
                Padding = new Thickness(0, 6)
            };
            Label classesHeaderLabel = new Label()
            {
                TextColor = (Color)Application.Current.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                Text = "AULAS DO DIA",
                HorizontalOptions = LayoutOptions.Center
            };
            classesHeader.Children.Add(classesHeaderLabel);

            //CONTENT>>
            classesView = new ScrollView()
            {
                VerticalOptions = LayoutOptions.StartAndExpand
            };

            //LAYOUT>>
            StackLayout classesLayout = new StackLayout()
            {
                BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                Spacing = 0
            };
            classesLayout.Children.Add(classesHeader);
            classesLayout.Children.Add(classesView);

            //LOADING INDICATOR>>
            classLoadingIndicator = new ActivityIndicator()
            {
                Margin = new Thickness(0, classesHeader.Height, 0, 0),
                IsRunning = true,
                IsVisible = true,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                Color = (Color)Application.Current.Resources["Orange"]
            };
            eventsLoadingIndicator = new ActivityIndicator()
            {
                Margin = new Thickness(0, eventsHeader.Height, 0, 0),
                IsRunning = true,
                IsVisible = true,
                VerticalOptions = LayoutOptions.CenterAndExpand,
                Color = (Color)Application.Current.Resources["Orange"]
            };

            //GRID SETUP>>
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(60, GridUnitType.Star) });

            grid.Children.Add(eventsLayout);
            grid.Children.Add(eventsLoadingIndicator);

            grid.Children.Add(classesLayout, 0, 1);
            grid.Children.Add(classLoadingIndicator, 0, 1);

            detailLayout.Children.Add(grid, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);

            if (_app.DataStatus)
            {
                FillStartPageContent();
                FillEvents();
            }
        }
        private void FillEvents()
        {
            StackLayout eventsViewContent = new StackLayout()
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Fill,
                Padding = new Thickness(2, 8)
            };

            string day = DateTime.Today.Day < 10 ? "0" + DateTime.Today.Day.ToString() : DateTime.Today.Day.ToString();
            string month = DateTime.Today.Month < 10 ? "0" + DateTime.Today.Month.ToString() : DateTime.Today.Month.ToString();

            string bd = day + month;

            var foundUsers = _app.UsersResume.Users.FindAll(user => user.Birthday.StartsWith(bd));
            foreach(SimplifiedUser u in foundUsers)
            {
                try
                {
                    StackLayout birthdayLayout = new StackLayout
                    {
                        Spacing = 10,
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        Padding = new Thickness(0, 8)
                    };

                    birthdayLayout.Children.Add(new Image
                    {
                        Source = "ic_birthday.png",
                        Aspect = Aspect.AspectFit,
                        VerticalOptions = LayoutOptions.Center
                    });

                    birthdayLayout.Children.Add(new Label
                    {
                        Text = "Aniversário de " + u.Name + "!",
                        TextColor = (Color)Application.Current.Resources["Orange"],
                        VerticalOptions = LayoutOptions.Center
                    });

                    eventsViewContent.Children.Add(birthdayLayout);
                    eventsViewContent.Children.Add(new BoxView { HeightRequest = 1, HorizontalOptions = LayoutOptions.Fill, BackgroundColor = (Color)Application.Current.Resources["LightTransparent"] });
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error populating ClassesView: " + e);
                }
            }

            if (_app.LoggedInUser.Function == "ADM")
                _app.ExpiryResumes.DateList.ForEach(r =>
            {
                var today = SharedUtilities.GetTodayDateTime();
                today = today.AddDays(7);

                if (r.ExpiryDate != null && r.ExpiryDateYoga != null && DateTime.Parse(r.ExpiryDate) < today && DateTime.Parse(r.ExpiryDateYoga) < today)
                {
                    var u = _app.UsersResume.Users.Find(s => s.UserID == r.UserID);
                    GenerateWarningLayout(eventsViewContent, "Os planos de yoga e treino de " + u.Name + " expiram em breve, verifique o perfil para trancar ou renovar.");
                }else if (r.ExpiryDate != null && DateTime.Parse(r.ExpiryDate) < today)
                {
                    var u = _app.UsersResume.Users.Find(s => s.UserID == r.UserID);
                    GenerateWarningLayout(eventsViewContent, "O plano de treino de " + u.Name + " expira em breve, verifique o perfil para trancar ou renovar.");
                }
                else if (r.ExpiryDateYoga != null && DateTime.Parse(r.ExpiryDateYoga) < today)
                {
                    var u = _app.UsersResume.Users.Find(s => s.UserID == r.UserID);
                    GenerateWarningLayout(eventsViewContent, "O plano de yoga de " + u.Name + " expira em breve, verifique o perfil para trancar ou renovar.");
                }
            });

            if (eventsViewContent.Children.Count() < 1)
            {
                eventsViewContent.Children.Add(new Label
                {
                    Text = "Nenhum evento no momento...",
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    Margin = new Thickness(0, 6)
                });
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                eventsLoadingIndicator.IsRunning = false;
                eventsLoadingIndicator.IsVisible = false;
                eventsView.Content = eventsViewContent;
            }); 
        }
        private void GenerateWarningLayout(StackLayout eventsViewContent, string msg)
        {
            try
            {
                StackLayout birthdayLayout = new StackLayout
                {
                    Spacing = 10,
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    Padding = new Thickness(0, 8)
                };

                birthdayLayout.Children.Add(new Image
                {
                    Source = "ic_plus_accent.png",
                    Rotation = 45,
                    Aspect = Aspect.AspectFit,
                    VerticalOptions = LayoutOptions.Center
                });

                birthdayLayout.Children.Add(new Label
                {
                    Text = msg,
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    VerticalOptions = LayoutOptions.Center
                });

                eventsViewContent.Children.Add(birthdayLayout);
                eventsViewContent.Children.Add(new BoxView { HeightRequest = 1, HorizontalOptions = LayoutOptions.Fill, BackgroundColor = (Color)Application.Current.Resources["LightTransparent"] });
            }
            catch (Exception e)
            {
                Console.WriteLine("Error populating ClassesView: " + e);
            }
        }
        private void FillStartPageContent()
        {
            int today = (int) DateTime.Today.DayOfWeek;
            StackLayout classesViewContent = new StackLayout()
            {
                Spacing = 0
            };

            if (classDetailsList.Count > 0)
                classDetailsList.Clear();

            int i = 0;
            if(AdmUtilities.TodayClasses != null)
            {
                foreach (var s in AdmUtilities.TodayClasses.Classes.OrderBy(c => c.Time).ToList())
                {
                    int sNumber = s.StudentsList.Count;
                    if (sNumber > 0)
                    {
                        StackLayout classInfo = new StackLayout
                        {
                            Orientation = StackOrientation.Horizontal,
                            HorizontalOptions = LayoutOptions.Fill,
                            Spacing = 16,
                            BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                            ClassId = i.ToString(),
                            Padding = new Thickness(16, 4, 16, 4)
                        };

                        TapGestureRecognizer tapClass = new TapGestureRecognizer();
                        tapClass.Tapped += (sender, e) =>
                        {
                            int id = Int32.Parse((sender as StackLayout).ClassId);

                            if (classDetailsList.ContainsKey(id))
                            {
                                classDetailsList[id].IsVisible ^= true;
                                if (classDetailsList[id].IsVisible)
                                    Task.Run(async () => { await (sender as StackLayout).Children[2].RotateTo(180, 50); });
                                else
                                    Task.Run(async () => { await (sender as StackLayout).Children[2].RotateTo(0, 50); });
                            }
                            else
                            {
                                Task.Run(async () => { await (sender as StackLayout).Children[2].RotateTo(180, 50); });

                                StackLayout classDetails = new StackLayout
                                {
                                    HorizontalOptions = LayoutOptions.Fill,
                                    Spacing = 0
                                };

                                var users = SharedUtilities.GetOrderedByNameUserList(s.StudentsList);
                                foreach (var user in users)
                                {
                                    try
                                    {
                                        StackLayout sl = new StackLayout
                                        {
                                            Orientation = StackOrientation.Horizontal,
                                            Padding = new Thickness(6)
                                        };

                                        string picToken = user.PictureToken == "" ? SharedUtilities.DefaultPictureToken : user.PictureToken;
                                        sl.Children.Add(new CircleImage
                                        {
                                            HeightRequest = 32,
                                            WidthRequest = 32,
                                            Margin = new Thickness(12, 0),
                                            Aspect = Aspect.AspectFill,
                                            Source = UriImageSource.FromUri(new Uri(picToken))
                                        });

                                        sl.Children.Add(new Label
                                        {
                                            Text = user.Name,
                                            VerticalOptions = LayoutOptions.Center,
                                            TextColor = (Color)Application.Current.Resources["TextLight"]
                                        });

                                        classDetails.Children.Add(sl);
                                        classDetails.Children.Add(new BoxView { HeightRequest = 1, HorizontalOptions = LayoutOptions.Fill, BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"] });
                                    }
                                    catch
                                    {
                                        Console.WriteLine("unable to show user???!!!!!!!!!!!");
                                    }
                                }

                                ((sender as StackLayout).Parent as StackLayout).Children.Insert(1, classDetails);
                                classDetailsList.Add(id, classDetails);
                            }
                        };
                        tapClass.NumberOfTapsRequired = 1;
                        classInfo.GestureRecognizers.Add(tapClass);

                        Label classTime = new Label
                        {
                            Text = s.Type + " - " + s.Time,
                            FontSize = Device.GetNamedSize(NamedSize.Title, typeof(Label)),
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            VerticalOptions = LayoutOptions.Center,
                            TextColor = (Color) _app.Resources["Orange"]
                        };

                        string studentsNumberFormatted = sNumber > 1 ? sNumber + " alunos" : sNumber + " aluno";
                        Label studentsNumber = new Label
                        {
                            Text = studentsNumberFormatted,
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            TextColor = (Color)_app.Resources["Orange"]
                        };

                        Image downArrow = new Image
                        {
                            Source = "ic_arrow_down.png",
                            Aspect = Aspect.AspectFit,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.End
                        };

                        classInfo.Children.Add(classTime);
                        classInfo.Children.Add(studentsNumber);
                        classInfo.Children.Add(downArrow);

                        StackLayout classLayout = new StackLayout
                        {
                            Spacing = 0
                        };
                        classLayout.Children.Add(classInfo);
                        classLayout.Children.Add(new BoxView { HeightRequest = 1, HorizontalOptions = LayoutOptions.Fill, BackgroundColor = (Color)Application.Current.Resources["LightTransparent"] });

                        classesViewContent.Children.Add(classLayout);
                        i++;
                    };
                    }
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                classLoadingIndicator.IsRunning = false;
                classLoadingIndicator.IsVisible = false;

                classesView.Content = classesViewContent;
                if(classesViewContent.Children.Count < 1)
                {
                    classesViewContent.Children.Add(new Label
                    {
                        Text = "Nenhuma aula marcada para hoje",
                        FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(20, 10),
                        TextColor = (Color)Application.Current.Resources["Orange"]
                    });
                }
            });
        }

        private void CheckIfScheduleCanUpdate(int scheduleIndex)
        {
            try
            {
                var schedule = _app.AdmSchedules[scheduleIndex];

                var selectedTime = scheduleEntriesList[scheduleIndex].TimePicker.Time.ToString().Substring(0, 5);
                var selectedType = scheduleEntriesList[scheduleIndex].TypePicker.SelectedItem.ToString();
                var selectedWeekdays = scheduleEntriesList[scheduleIndex].Weekdays;

                var count = schedule.Classes.FindAll(c => selectedWeekdays.Contains(c.Day)).Count;
                var wdVerification = schedule.Classes.Count != selectedWeekdays.Count ? true : count != schedule.Classes.Count;

                if (schedule.Time != selectedTime || schedule.Type != selectedType || wdVerification)
                {
                    scheduleEntriesList[scheduleIndex].UpdateButton.IsEnabled = true;
                }
                else
                {
                    scheduleEntriesList[scheduleIndex].UpdateButton.IsEnabled = false;
                }
            }catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private async void UpdateScheduleBtn(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
            try
            {
                var id = Int32.Parse((sender as Button).ClassId);
                var schedule = _app.AdmSchedules[id];

                var newSchedule = new Schedule
                {
                    Id = schedule.Id,
                    Time = scheduleEntriesList[id].TimePicker.Time.ToString().Substring(0, 5),
                    Type = scheduleEntriesList[id].TypePicker.SelectedItem.ToString(),
                    Classes = new List<Schedule.Weekday>()
                };
                var selectedWeekdays = scheduleEntriesList[id].Weekdays;
                selectedWeekdays.ForEach(sw => { newSchedule.Classes.Add(new Schedule.Weekday() { Day = sw, StudentsList = new List<int>() }); });

                if (await AdmUtilities.UpdateSchedule(newSchedule, schedule))
                {
                    SpawnScheduleView();
                    await DisplayAlert("Sucesso", "Horário alterado com sucesso!", "Ok");

                    scheduleDetailsList.Remove(id.ToString());
                    scheduleEntriesList.Remove(id);
                }
                else
                    await DisplayAlert("Erro", "Não foi possível alterar o horário, tente novamente mais tarde", "Ok");
            } 
            catch 
            {
                await DisplayAlert("Erro", "Não foi possível alterar o horário, tente novamente mais tarde", "Ok");
            }
            await PopupNavigation.Instance.PopAsync();
        }

        private async void TapGestureRecognizer_Rating(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new Students.StudentsSelectionPage("rate"));
            }catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            
        }
        private async void TapGestureRecognizer_Cad(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Students.StudentCadastre());
        }
        private async void TapGestureRecognizer_Manage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Students.StudentsSelectionPage("manage"));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (Application.Current.MainPage as AdmPage).IsGestureEnabled = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                (Application.Current.MainPage as AdmPage).IsGestureEnabled = false;
            } catch {}
        }
    }
}