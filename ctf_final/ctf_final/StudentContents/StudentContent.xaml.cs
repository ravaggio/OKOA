using ImageCircle.Forms.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static ctf_final.BackgroundTasks;
using static ctf_final.AppController;
using System.Globalization;
using XamarinFirebase.Model;
using Microcharts.Forms;
using SkiaSharp;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Rg.Plugins.Popup.Services;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Plugin.CloudFirestore;

namespace ctf_final.StudentContents
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StudentContent : ContentPage
    {
        //Start variables
        StackLayout classesView;
        Label makeUpClassesLabel;
        Label makeUpClassesYogaLabel;
        Label makeUpClassesPilatesLabel;
        Button makeUpClassesBtn;
        Grid startGrid;
        ActivityIndicator startLoading;

        bool repoBtnClicked = false;
        readonly Dictionary<string, View> classStudents = new Dictionary<string, View>();

        bool alreadyShownResearch = false;

        //Profile edit variables
        bool isEditing = false;
        bool changedProfile = false;

        Image picEdit;
        Label completeProfileLabel;
        CircleImage profPic;

        readonly List<Label> dataLabel = new List<Label>();
        readonly List<Entry> hiddenEntries = new List<Entry>();
        Picker genderPicker;

        int SelectedPage = 0;

        public StudentContent()
        {
            InitializeComponent();

            SpawnStartView();

            try { Title = _app.LoggedInUser.Gender == 1 ? "Bem Vinda!" : "Bem Vindo!"; } catch { Title = "Bem Vindo!"; }
            MessagingCenter.Subscribe<PageControlMessage>(this, "OnResume", msg =>
            {
                if(SelectedPage == 0)
                    Device.BeginInvokeOnMainThread(() => 
                    {
                        SetLoadingSignVisibility(true);
                    });
            });
            MessagingCenter.Subscribe<DataFinishedLoadingMessage>(this, "DataLoaded", msg =>
            {
                FillClasses();
            });
            MessagingCenter.Subscribe<PageUpdateMessage>(this, "UserDataUpdated", msg =>
            {
                switch (msg.Command)
                {
                    case "MakeupClassesChanged":
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            ChangeMakeupClassesLabel();
                        });
                        break;
                    case "ClassesChanged":
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            if (SelectedPage == 0)
                                FillClasses();
                        });
                        break;
                    case "BasicDataChanged":
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            SetProfileData();
                        });
                        break;
                }
            });
            MessagingCenter.Subscribe<PageControlMessage>(this, "LoadSPage", message =>
            {
                try
                {
                    ClearView();
                    if(EventsListener != null)
                    {
                        EventsListener.Remove();
                        EventsListener = null;
                    }
                    switch (message.Command)
                    {
                        case "LoadProfile":
                            SelectedPage = 1;
                            SpawnProfileView();
                            break;
                        case "LoadPlan":
                            SelectedPage = 2;
                            SpawnPlanView();
                            break;
                        case "LoadRating":
                            SelectedPage = 3;
                            SpawnRatingsView();
                            break;
                        case "LoadEvents":
                            SelectedPage = 4;
                            SpawnEventsView();
                            break;
                        case "LoadStartPage":
                            SelectedPage = 0;
                            SpawnStartView();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        //----------- MAIN VIEW -----------

        //StartView - DONE
        //FillClasses - DONE
        //ExpandClassView - DONE
        //ChangeMakeup - DONE
        //ClearAppointment - DONE
        public void SpawnStartView()
        {
            Title = _app.LoggedInUser.Gender == 1 ? "Bem Vinda!" : "Bem Vindo!";

            // -------- MAIN VIEW LAYOUT --------

            startGrid = new Grid()
            {
                RowSpacing = 0,
                IsVisible = true,
                BackgroundColor = (Color)_app.Resources["DarkTransparent"]
            };
            startGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            startGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            startGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            startGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

            // -------- MAIN VIEW LAYOUT --------

            // -------- CLASSES LAYOUT --------

            StackLayout header = new StackLayout
            {
                HorizontalOptions = LayoutOptions.FillAndExpand,
                BackgroundColor = (Color)_app.Resources["PrimaryDark"],
                Padding = new Thickness(12)
            };
            header.Children.Add(new Label
            {
                Text = "MINHAS AULAS",
                TextColor = (Color)_app.Resources["Orange"],
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
            });
            classesView = new StackLayout()
            {
                Spacing = 0,
                IsVisible = false,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            ScrollView sv = new ScrollView()
            {
                Content = classesView
            };

            // -------- CLASSES LAYOUT --------

            // -------- REPOSITIONS LAYOUT --------

            StackLayout repoDetails = new StackLayout()
            {
                Padding = new Thickness(8),
                BackgroundColor = (Color)_app.Resources["PrimaryDark"],
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            //Makeup classes layout (How many makeup classes are left for yoga and train)
            StackLayout makeUpLayout = new StackLayout()
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Center
            };
            makeUpLayout.Children.Add(new Label
            {
                Text = "REPOSIÇÕES",
                TextColor = (Color)_app.Resources["Orange"],
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
            });
            if (_app.LoggedInUser.UserPlan.TrainPlan != null)
            {
                makeUpClassesLabel = new Label
                {
                    TextColor = (Color)_app.Resources["TextLight"],
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
                };

                makeUpLayout.Children.Add(makeUpClassesLabel);
            }
            if(_app.LoggedInUser.UserPlan.YogaPlan != null)
            {
                makeUpClassesYogaLabel = new Label
                {
                    TextColor = (Color)_app.Resources["TextLight"],
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
                };

                makeUpLayout.Children.Add(makeUpClassesYogaLabel);
            }
            if (_app.LoggedInUser.UserPlan.PilatesPlan != null)
            {
                makeUpClassesPilatesLabel = new Label
                {
                    TextColor = (Color)_app.Resources["TextLight"],
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
                };

                makeUpLayout.Children.Add(makeUpClassesPilatesLabel);
            }
            repoDetails.Children.Add(makeUpLayout);

            //Mark makeup classes btn
            makeUpClassesBtn = new Button
            {
                BackgroundColor = (Color)_app.Resources["Orange"],
                TextColor = (Color)_app.Resources["TextDark"],
                IsEnabled = false,
                Text = "MARCAR REPOSIÇÃO"
            };
            makeUpClassesBtn.Clicked += async (sender, e) =>
            {
                if(!repoBtnClicked)
                {
                    repoBtnClicked = true;
                    await Navigation.PushAsync(new MakeupClassPicker());
                }

                repoBtnClicked = false;
            };
            if (Device.RuntimePlatform == Device.iOS)
                makeUpClassesBtn.Padding = new Thickness(0, 10, 0, 25);

            startGrid.Children.Add(header);
            startGrid.Children.Add(sv, 0, 1);
            startGrid.Children.Add(repoDetails, 0, 2);

            startGrid.Children.Add(new BoxView
            {
                BackgroundColor = (Color)_app.Resources["Orange"],
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            }, 0, 3);
            startGrid.Children.Add(makeUpClassesBtn, 0, 3);

            // -------- REPOSITIONS LAYOUT --------

            //Activity indicator for loading
            startLoading = new ActivityIndicator()
            {
                IsRunning = true,
                Color = (Color)_app.Resources["Orange"]
            };

            detailLayout.Children.Add(startGrid, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            detailLayout.Children.Add(startLoading, new Rectangle(.5, .5, .1, .1), AbsoluteLayoutFlags.All);

            if (_app.DataStatus == true)
                FillClasses();
        }
        public void FillClasses()
        {
            List<View> views = new List<View>();
            if (_app.LoggedInUser.PlanAbscence == 0)
            {
                var classes = _app.ApplicationUserData.UserClasses.OrderBy(sc => sc.Date).ToList();
                int i = 0;
                classes.ForEach(cl =>
                {
                    views.Add(CreateClassGrid(cl, i));
                    views.Add(CreateSeparator());
                    i++;
                });
            }
            else if (_app.LoggedInUser.PlanAbscence == 1)
                views.Add(CreateLockedPlanGrid());
                
            Device.BeginInvokeOnMainThread(() =>
            {
                classStudents.Clear();
                classesView.Children.Clear();

                views.ForEach(v => classesView.Children.Add(v));

                ChangeMakeupClassesLabel();
                SetLoadingSignVisibility(false);

                Task.Run(async () =>
                {
                    //show research <-
                    if(!alreadyShownResearch)
                    {
                        alreadyShownResearch = true;
                        try
                        {
                            var available = _app.QuestionnaireList.FindAll(q => q.Closed == 0);
                            available.OrderBy(q => q.CreationDate);
                            foreach (var q in available)
                            {
                                if (!q.ReplyIDs.Contains(_app.LoggedInUser.UserID) && _app.LoggedInUser.Function == "USER")
                                {
                                    await PopupNavigation.Instance.PushAsync(new PopupPages.QuestionnairePopup(q));
                                    break;
                                }
                            }
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                    }
                });
            });
        }

        //----- LAYOUTS CREATION ------

        Grid CreateClassGrid(Models.SimpleClass cl, int i)
        {
            // --- BASE GRID ---

            Grid classBase = new Grid()
            {
                ClassId = i.ToString(),
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = (Color)_app.Resources["DarkTransparent"],
                RowSpacing = 0,
                Padding = new Thickness(6)
            };
            TapGestureRecognizer tapExpand = new TapGestureRecognizer();
            tapExpand.Tapped += ExpandClassView;
            tapExpand.NumberOfTapsRequired = 1;
            classBase.GestureRecognizers.Add(tapExpand);

            classBase.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            classBase.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

            // --- BASE GRID ---

            // --- DETAILS ---

            classBase.Children.Add(new Label
            {
                Text = cl.Time + " - " + cl.Type,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                HorizontalOptions = LayoutOptions.StartAndExpand,
                TextColor = (Color) (cl.Type == "Treino" ? _app.Resources["Orange"] : _app.Resources["Yoga"])
            });

            var _date = DateTime.Parse(cl.Date);
            var _classDate = SharedUtilities.IntToWeekday((int)_date.DayOfWeek) + " - " + _date.ToString("dd/MM");
            classBase.Children.Add(new Label
            {
                Text = _classDate,
                HorizontalOptions = LayoutOptions.StartAndExpand,
                TextColor = (Color)_app.Resources["TextLight"]
            }, 0, 1);

            // --- DETAILS ---

            // --- EXPAND ARROW ---

            Image expandImage = new Image
            {
                Source = "ic_arrow_down.png",
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.EndAndExpand,
                Margin = new Thickness(10)
            };
            classBase.Children.Add(expandImage);
            Grid.SetRowSpan(expandImage, 2);

            // --- EXPAND ARROW ---

            return classBase;
        }
        Grid CreateLockedPlanGrid()
        {
            var lockedPlanGrid = new Grid
            {
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                RowSpacing = 0,
                ColumnSpacing = 10,
                Padding = 25
            };

            lockedPlanGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            lockedPlanGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });

            lockedPlanGrid.Children.Add(new Image
            {
                Source = "ic_lock.png",
                Aspect = Aspect.AspectFit
            });

            lockedPlanGrid.Children.Add(new Label
            {
                Text = "Plano trancado!",
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                TextColor = (Color)_app.Resources["Orange"]
            }, 1, 0);

            return lockedPlanGrid;
        }

        //----- LAYOUTS CREATION ------

        //----- LAYOUTS FUNCTIONS ------

        void SetLoadingSignVisibility(bool enabled)
        {
            startLoading.IsRunning = enabled;
            startLoading.IsVisible = enabled;
            classesView.IsVisible = !enabled;
        }
        private void ExpandClassView(object sender, EventArgs e)
        {
            var gridLayout = sender as Grid;
            var classKey = gridLayout.ClassId;

            if (classStudents.ContainsKey(classKey))
            {
                classStudents[classKey].IsVisible ^= true;
            }
            else
            {
                int id = classesView.Children.IndexOf(gridLayout) + 1;

                var classes = _app.ApplicationUserData.UserClasses.OrderBy(sc => sc.Date).ToList();
                var cl = classes[Int32.Parse(classKey)];

                //---------- USERS IN CLASS LAYOUT ----------

                StackLayout studentListView = new StackLayout
                {
                    Spacing = 0,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                var users = SharedUtilities.GetOrderedByNameUserList(cl.StudentsIDs);
                users.ForEach(su =>
                {
                    if (su.UserID != _app.LoggedInUser.UserID)
                        try
                        {
                            StackLayout studentView = new StackLayout()
                            {
                                Spacing = 0,
                                Orientation = StackOrientation.Horizontal,
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                            };

                            string picToken = su.PictureToken == "" ? SharedUtilities.DefaultPictureToken : su.PictureToken;
                            studentView.Children.Add(new CircleImage
                            {
                                Source = picToken,
                                Aspect = Aspect.AspectFill,
                                Margin = new Thickness(10),
                                HeightRequest = 40,
                                WidthRequest = 40
                            });
                            studentView.Children.Add(new Label
                            {
                                Text = su.Name,
                                TextColor = (Color)_app.Resources["Orange"],
                                Margin = new Thickness(10),
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
                            });

                            studentListView.Children.Add(studentView);
                            studentListView.Children.Add(CreateSeparator());
                        }
                        catch (Exception ex)
                        {
                            DisplayAlert("Erro desconhecido!", "Código do erro: " + ex, "OK");
                        }
                });

                //---------- USERS IN CLASS LAYOUT ----------

                //---------- CLEAR APPOINTMENT LAYOUT ----------

                //Today date relative to sp timezone
                var todayDate = SharedUtilities.GetTodayDateTime();
                DateTime classDate = DateTime.ParseExact(cl.Date + cl.Time, "yyyy-MM-ddHH:mm", CultureInfo.InvariantCulture);

                if (todayDate.AddHours(SharedUtilities.DEFAULT_TIME_LIMIT) <= classDate)
                {
                    StackLayout clearBtn = new StackLayout
                    {
                        ClassId = cl.Date.ToString() + "/" + cl.Time + "/" + cl.Type,
                        HorizontalOptions = LayoutOptions.Fill,
                        BackgroundColor = (Color)_app.Resources["PrimaryTransparent"],
                        Orientation = StackOrientation.Horizontal,
                    };
                    TapGestureRecognizer tapClearAppointment = new TapGestureRecognizer();
                    tapClearAppointment.Tapped += ClearAppointmentBtn;
                    tapClearAppointment.NumberOfTapsRequired = 1;
                    clearBtn.GestureRecognizers.Add(tapClearAppointment);

                    StackLayout clearContentHolder = new StackLayout
                    {
                        Spacing = 10,
                        Margin = 10,
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.CenterAndExpand
                    };
                    clearContentHolder.Children.Add(new Image
                    {
                        Source = "ic_plus_accent.png",
                        Aspect = Aspect.AspectFit,
                        VerticalOptions = LayoutOptions.Center,
                        Rotation = 45
                    });
                    clearContentHolder.Children.Add(new Label
                    {
                        Text = "DESMARCAR",
                        VerticalOptions = LayoutOptions.Center,
                        TextColor = (Color)_app.Resources["Orange"],
                        FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
                    });

                    clearBtn.Children.Add(clearContentHolder);
                    studentListView.Children.Add(clearBtn);
                }

                //---------- CLEAR APPOINTMENT LAYOUT ----------

                classStudents.Add(classKey, studentListView);
                classesView.Children.Insert(id, studentListView);
            }

            //Sets arrow image rotation 
            var arrowImage = gridLayout.Children[2];
            if (classStudents[classKey].IsVisible)
                Task.Run(async () => { await arrowImage.RotateTo(180, 50); });
            else
                Task.Run(async () => { await arrowImage.RotateTo(0, 50); });
        }
        public void ChangeMakeupClassesLabel()
        {
            try
            {
                if (CheckConnection())
                {
                    makeUpClassesBtn.IsEnabled = false;
                    return;
                }

                if (makeUpClassesLabel != null)
                    makeUpClassesLabel.Text = _app.LoggedInUser.MakeupClasses + " reposições disponíveis (Treino)";
                if (makeUpClassesYogaLabel != null)
                    makeUpClassesYogaLabel.Text = _app.LoggedInUser.MakeupClassesYoga + " reposições disponíveis (Yoga)";
                if (makeUpClassesPilatesLabel != null)
                    makeUpClassesPilatesLabel.Text = _app.LoggedInUser.MakeupClassesPilates + " reposições disponíveis (Pilates)";

                if (_app.LoggedInUser.PlanAbscence == 1)
                    makeUpClassesBtn.IsEnabled = false;
                else
                    makeUpClassesBtn.IsEnabled = true;

                /* ID-0000001 - Changed at 01-09-20: view classes without available repo
                if (_app.LoggedInUser.PlanAbscence == 1 || _app.LoggedInUser.MakeupClasses < 1 && _app.LoggedInUser.MakeupClassesYoga < 1)
                    makeUpClassesBtn.IsEnabled = false;
                else
                    makeUpClassesBtn.IsEnabled = true;
                */
            }
            catch (Exception e)
            {
                makeUpClassesBtn.IsEnabled = false;
                DisplayUnkownErrorMessage(e);
            }
        }

        //----- LAYOUTS FUNCTIONS ------

        public async void ClearAppointmentBtn(object sender, EventArgs e)
        {
            try
            {
                //-- AVAILABILITY CHECK --

                if (CheckConnection())
                {
                    await DisplayAlert("Erro", "Não foi conectar-se com o servidor, verifique sua conexão com a internet e tente novamente.", "Ok");
                    return;
                }

                var senderLayout = sender as StackLayout;
                var path = senderLayout.ClassId;

                //0 == date, 1 == time, 2 == type
                var splittenDetails = path.Split('/');

                var todayDate = SharedUtilities.GetTodayDateTime();
                if (todayDate.AddHours(SharedUtilities.DEFAULT_TIME_LIMIT) >= DateTime.ParseExact(splittenDetails[0] + splittenDetails[1], "yyyy-MM-ddHH:mm", CultureInfo.InvariantCulture))
                {
                    await DisplayAlert("Erro", "Você só pode desmarcar suas aulas com 5 horas de antecedência.", "Ok");
                    return;
                }

                //-- AVAILABILITY CHECK --

                if (await DisplayAlert("Desmarcar", "Deseja desmarcar sua aula no dia " + DateTime.Parse(splittenDetails[0]).ToString("dd/MM") + "?", "Sim", "Não"))
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());

                    if (await UserUtilities.ClearAppointment(path, splittenDetails[2]))
                    {
                        //Remove class from UI
                        FillClasses();

                        await DisplayAlert("Sucesso!", "Aula desmarcada com sucesso! Uma reposição foi adicionada ao seu perfil.", "Ok");
                    }
                    else
                    {
                        await DisplayAlert("Erro", "Não foi possível desmarcar a aula! Tente novamente mais tarde.", "Ok");
                    }

                    await PopupNavigation.Instance.PopAsync();
                }
            }
            catch (Exception ex)
            {
                DisplayUnkownErrorMessage(ex);
            }
        }

        //----------- MAIN VIEW -----------


        //----------- PROFILE VIEW -----------

        public void SpawnProfileView()
        {
            Title = "Meu perfil";

            User userData = _app.LoggedInUser;
            CustomRenderers.KeyboardGrid profileData = new CustomRenderers.KeyboardGrid
            {
                RowSpacing = 0,
                ColumnSpacing = 0
            };

            profileData.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            profileData.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            profileData.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            profileData.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16, GridUnitType.Star) });

            profileData.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            profileData.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
            profileData.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Star) });
            profileData.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            BoxView headerBg = new BoxView
            {
                BackgroundColor = (Color)_app.Resources["Primary"]
            };
            profileData.Children.Add(headerBg);
            Grid.SetRowSpan(headerBg, 2);
            Grid.SetColumnSpan(headerBg, 4);

            BoxView divider = new BoxView
            {
                BackgroundColor = (Color)_app.Resources["LightTransparent"],
                VerticalOptions = LayoutOptions.End,
                HeightRequest = 1
            };
            profileData.Children.Add(divider, 0, 1);
            Grid.SetColumnSpan(divider, 4);

            BoxView dataBg = new BoxView
            {
                BackgroundColor = (Color)_app.Resources["DarkTransparent"]
            };
            profileData.Children.Add(dataBg, 0, 2);
            Grid.SetRowSpan(dataBg, 2);
            Grid.SetColumnSpan(dataBg, 4);

            string picToken = userData.PictureToken == "" ? SharedUtilities.DefaultPictureToken : userData.PictureToken;
            profPic = new CircleImage
            {
                Source = picToken,
                BorderColor = (Color)_app.Resources["LightTransparent"],
                BorderThickness = 1,
                HeightRequest = 82,
                WidthRequest = 82,
                Aspect = Aspect.AspectFill
            };

            StackLayout pictureHolder = new StackLayout
            {
                Spacing = 0,
                HorizontalOptions = LayoutOptions.Center,
            };
            pictureHolder.Children.Add(profPic);

            profileData.Children.Add(pictureHolder, 1, 1);
            Grid.SetRowSpan(pictureHolder, 2);

            TapGestureRecognizer tapPickPic = new TapGestureRecognizer();
            tapPickPic.Tapped += PickPicture;
            tapPickPic.NumberOfTapsRequired = 1;
            profPic.GestureRecognizers.Add(tapPickPic);

            picEdit = new Image
            {
                Source = "ic_pick_pencil.png",
                Aspect = Aspect.AspectFit,
                Margin = new Thickness(20),
                InputTransparent = true,
                IsVisible = false,
                IsEnabled = false
            };
            profileData.Children.Add(picEdit, 1, 1);
            Grid.SetRowSpan(picEdit, 2);

            var nameLabel = new Label
            {
                Text = userData.Name,
                Margin = new Thickness(8),
                VerticalOptions = LayoutOptions.End,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["Orange"]
            };
            profileData.Children.Add(nameLabel, 2, 0);
            Grid.SetRowSpan(nameLabel, 2);

            Image editBtn = new Image
            {
                Source = "ic_edit.png",
                Aspect = Aspect.AspectFit,
                Margin = new Thickness(0, 15, 15, 15)
            };
            TapGestureRecognizer tapEdit = new TapGestureRecognizer();
            tapEdit.Tapped += EditProfile;
            tapEdit.NumberOfTapsRequired = 1;
            editBtn.GestureRecognizers.Add(tapEdit);
            profileData.Children.Add(editBtn, 3, 1);

            StackLayout dataLayout = new StackLayout()
            {
                Spacing = 10,
                Padding = new Thickness(10,20,10,10)
            };

            //LABELS >>

            dataLayout.Children.Add(new Label
            {
                Text = "DADOS DO PERFIL",
                TextDecorations = TextDecorations.Underline,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["Orange"]
            });
            
            var birthdayLabel = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["TextLight"]
            };
            dataLabel.Add(birthdayLabel);
            dataLayout.Children.Add(birthdayLabel);

            var genderLabel = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["TextLight"]
            };
            dataLabel.Add(genderLabel);
            dataLayout.Children.Add(genderLabel);
 
            var phoneLabel = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["TextLight"]
            };
            dataLabel.Add(phoneLabel);
            dataLayout.Children.Add(phoneLabel);

            var emailLabel = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["TextLight"]
            };
            dataLabel.Add(emailLabel);
            dataLayout.Children.Add(emailLabel);

            var addressLabel = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["TextLight"]
            };
            dataLabel.Add(addressLabel);
            dataLayout.Children.Add(addressLabel);

            //END OF LABELS >>

            //ENTRIES >>

            var birthdayEntry = new Entry
            {
                HorizontalTextAlignment = TextAlignment.Center,
                IsVisible = false,
                TextColor = (Color)_app.Resources["TextLight"]
            };
            birthdayEntry.TextChanged += (s, e) => { changedProfile = true; };
            birthdayEntry.Behaviors.Add(new Behaviors.MakedEntryBehavior { Mask = "XX/XX/XXXX" });
            hiddenEntries.Add(birthdayEntry);
            dataLayout.Children.Add(birthdayEntry);

            genderPicker = new Picker
            {
                TextColor = (Color)_app.Resources["TextLight"],
                ItemsSource = new List<string>()
                {
                    "Masculino",
                    "Feminino",
                    "Não informar"
                },
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 140,
                IsVisible = false
            };
            genderPicker.SelectedIndexChanged += (s, e) => { changedProfile = true; };
            dataLayout.Children.Add(genderPicker);

            var phoneEntry = new Entry
            {
                HorizontalTextAlignment = TextAlignment.Center,
                IsVisible = false,
                TextColor = (Color)_app.Resources["TextLight"],
                Placeholder = "Telefone",
                PlaceholderColor = (Color)_app.Resources["LightTransparent"]
            };
            phoneEntry.TextChanged += (s, e) => { changedProfile = true; };
            phoneEntry.Behaviors.Add(new Behaviors.MakedEntryBehavior { Mask = "(XX) XXXXX-XXXX" });
            hiddenEntries.Add(phoneEntry);
            dataLayout.Children.Add(phoneEntry);

            var emailEntry = new Entry
            {
                HorizontalTextAlignment = TextAlignment.Center,
                IsVisible = false,
                TextColor = (Color)_app.Resources["TextLight"],
                Placeholder = "Email",
                PlaceholderColor = (Color)_app.Resources["LightTransparent"]
            };
            emailEntry.TextChanged += (s, e) => { changedProfile = true; };
            hiddenEntries.Add(emailEntry);
            dataLayout.Children.Add(emailEntry);

            var addressEntry = new Entry
            {
                HorizontalTextAlignment = TextAlignment.Center,
                IsVisible = false,
                TextColor = (Color)_app.Resources["TextLight"],
                Placeholder = "Endereço",
                PlaceholderColor = (Color)_app.Resources["LightTransparent"]
            };
            addressEntry.TextChanged += (s, e) => { changedProfile = true; };
            hiddenEntries.Add(addressEntry);
            dataLayout.Children.Add(addressEntry);

            //END OF ENTRIES>>

            if(Device.RuntimePlatform == Device.iOS)
            {
                hiddenEntries.ForEach(e => { (e as Entry).BackgroundColor = (Color)_app.Resources["PrimaryDark"]; });
                genderPicker.BackgroundColor = (Color)_app.Resources["PrimaryDark"];
            }

            SetProfileData();
            
            if (dataLabel.Any(l => l.Text == ""))
            {
                completeProfileLabel = new Label
                {
                    Text = "Clique em editar para completar o seu perfil.",
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    TextColor = (Color)_app.Resources["LightTransparent"]
                };
                dataLayout.Children.Add(completeProfileLabel);
            }

            profileData.Children.Add(dataLayout, 0, 3);
            Grid.SetColumnSpan(dataLayout, 4);

            AbsoluteLayout.SetLayoutBounds(profileData, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(profileData, AbsoluteLayoutFlags.All);

            detailLayout.Children.Add(profileData);
        }
        private async void PickPicture(object sender, EventArgs e)
        {
            if (isEditing)
            {
                try
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>(); 
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.StorageRead>();
                    }
                    if (status == PermissionStatus.Granted)
                    {
                        await CrossMedia.Current.Initialize();
                        if (!CrossMedia.Current.IsPickPhotoSupported)
                        {
                            await DisplayAlert("Incapaz de selecionar foto", "Esta funcionalidade não é compativel com o seu aparelho", "Ok");
                            return;
                        }

                        var mediaOptions = new PickMediaOptions()
                        {
                            PhotoSize = PhotoSize.Small
                        };

                        SharedUtilities.TemporaryProfilePicture = await CrossMedia.Current.PickPhotoAsync(mediaOptions);
                        if (SharedUtilities.TemporaryProfilePicture == null)
                        {
                            return;
                        }

                        profPic.Source = ImageSource.FromStream(() => SharedUtilities.TemporaryProfilePicture.GetStream());
                        changedProfile = true;
                    }
                    else if (status != PermissionStatus.Unknown)
                    {
                        await DisplayAlert("Acesso negado.", "Não foi possível acessar as imagens, por favor tente novamente.", "Ok");
                    }
                }
                catch (Exception exc)
                {
                    await DisplayAlert("Erro desconhecido", "Não foi possível selecionar a imagem. Se o erro persistir, favor contatar o desenvolvedor: \n" + exc, "OK");
                }
            }
        }
        private async void EditProfile(object sender, EventArgs e)
        {
            isEditing ^= true;
            if (isEditing)
            {
                (sender as Image).Source = "ic_checkmark.png";

                picEdit.IsEnabled = true;
                picEdit.IsVisible = true;

                if(completeProfileLabel != null)
                    completeProfileLabel.IsVisible = false;

                dataLabel.ForEach(l => l.IsVisible = false);
                genderPicker.IsVisible = true;
                hiddenEntries.ForEach(ent => ent.IsVisible = true);

                changedProfile = false;
            }
            else
            {
                var current = Connectivity.NetworkAccess;
                if (current != NetworkAccess.Internet)
                {
                    await DisplayAlert("Erro", "Não foi conectar-se com o servidor, verifique sua conexão com a internet e tente novamente.", "Ok");
                }
                else if (changedProfile)
                {
                    if (await DisplayAlert("Salvar", "Deseja salvar as alterações? ", "Sim", "Não"))
                    {
                        try
                        {
                            DateTime.ParseExact(hiddenEntries[0].Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            await DisplayAlert("Valores inválidos", "Por favor, insira uma data de nascimento válida", "Ok");
                            return;
                        }

                        var oldUser = _app.LoggedInUser;
                        User user = new User()
                        {
                            UserID = oldUser.UserID,
                            UserPlan = oldUser.UserPlan,
                            ClassesExceptions = oldUser.ClassesExceptions,
                            Function = oldUser.Function,
                            MakeupClasses = oldUser.MakeupClasses,
                            MakeupClassesYoga = oldUser.MakeupClassesYoga,
                            Name = oldUser.Name,
                            PictureToken = oldUser.PictureToken,
                            PlanAbscence = oldUser.PlanAbscence,
                            ScheduleReferences = oldUser.ScheduleReferences,
                            Ratings = oldUser.Ratings,
                            Birthday = hiddenEntries[0].Text.Replace("/", ""),
                            Gender = genderPicker.SelectedIndex,
                            Phone = hiddenEntries[1].Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                            Email = hiddenEntries[2].Text,
                            Address = hiddenEntries[3].Text,
                            MCTrainDates = oldUser.MCTrainDates,
                            MCYogaDates = oldUser.MCYogaDates,
                            PlanAbscenceDate = oldUser.PlanAbscenceDate
                        };

                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());

                        if (await SharedUtilities.UpdateUser(oldUser, user, true))
                        {
                            SetProfileData();
                            await DisplayAlert("Sucesso!", "Seu perfil foi atualizado com sucesso!", "OK");
                        }
                        else
                        {
                            profPic.Source = _app.LoggedInUser.PictureToken;
                            await DisplayAlert("Erro", "Não foi possível atualizar o perfil. Verifique sua conexão com a internet ou tente novamente mais tarde.", "OK");
                        }

                        changedProfile = false;
                        await PopupNavigation.Instance.PopAsync();
                    }
                }

                (sender as Image).Source = "ic_edit.png";

                picEdit.IsEnabled = false;
                picEdit.IsVisible = false;

                dataLabel.ForEach(l => l.IsVisible = true);
                genderPicker.IsVisible = false;
                hiddenEntries.ForEach(ent => ent.IsVisible = false);

                if (completeProfileLabel != null)
                {
                    if (dataLabel.Any(l => l.Text == ""))
                        completeProfileLabel.IsVisible = true;
                }
            }
        }
        private void SetProfileData()
        {
            if(dataLabel.Count >= 5)
            {
                string formattedDate = "Data de nascimento: {0}/{1}/{2}";
                var final_birthday = string.Format(formattedDate, _app.LoggedInUser.Birthday.Substring(0, 2), _app.LoggedInUser.Birthday.Substring(2, 2), _app.LoggedInUser.Birthday.Substring(4));
                dataLabel[0].Text = final_birthday;

                dataLabel[1].Text = "Sexo: " + (_app.LoggedInUser.Gender == 0 ? "Masculino" : _app.LoggedInUser.Gender == 1 ? "Feminino" : "Não informar");

                string pVal = _app.LoggedInUser.Phone;
                string formattedPhone = "Telefone: ({0}) {1}-{2}";
                var final_phone = string.IsNullOrWhiteSpace(pVal) ? "" : string.Format(formattedPhone, pVal.Substring(0, 2), pVal.Substring(2, (pVal.Length - 6)), pVal.Substring((pVal.Length - 6) + 2));
                dataLabel[2].Text = final_phone;

                dataLabel[3].Text = string.IsNullOrWhiteSpace(_app.LoggedInUser.Email) ? "" : "E-mail: " + _app.LoggedInUser.Email;
                dataLabel[4].Text = string.IsNullOrWhiteSpace(_app.LoggedInUser.Address) ? "" : "Endereço: " + _app.LoggedInUser.Address;
            } 
            if(hiddenEntries.Count >= 4)
            {
                string formattedDate = "{0}/{1}/{2}";
                var final_birthday = string.Format(formattedDate, _app.LoggedInUser.Birthday.Substring(0, 2), _app.LoggedInUser.Birthday.Substring(2, 2), _app.LoggedInUser.Birthday.Substring(4));
                hiddenEntries[0].Text = final_birthday;

                genderPicker.SelectedItem = _app.LoggedInUser.Gender == 0 ? "Masculino" : _app.LoggedInUser.Gender == 1 ? "Feminino" : "Não informar";

                string pVal = _app.LoggedInUser.Phone;
                string formattedPhone = "{0} {1}-{2}";
                hiddenEntries[1].Text = string.IsNullOrWhiteSpace(pVal) ? "" : string.Format(formattedPhone, pVal.Substring(0, 2), pVal.Substring(2, (pVal.Length - 6)), pVal.Substring((pVal.Length - 6) + 2));

                hiddenEntries[2].Text = string.IsNullOrWhiteSpace(_app.LoggedInUser.Email) ? "" :  _app.LoggedInUser.Email;
                hiddenEntries[3].Text = string.IsNullOrWhiteSpace(_app.LoggedInUser.Address) ? "" : _app.LoggedInUser.Address;
            }
        }

        //----------- PROFILE VIEW -----------


        //----------- RATINGS VIEW -----------

        public void SpawnRatingsView()
        {
            Title = "Avaliações";
            var ratings = _app.LoggedInUser.Ratings;

            if (ratings != null && ratings.Count > 0)
            {
                Grid charts = new Grid()
                {
                    RowSpacing = 0
                };

                charts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                charts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180, GridUnitType.Absolute) });
                charts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                charts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180, GridUnitType.Absolute) });
                charts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                charts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180, GridUnitType.Absolute) });

                charts.Children.Add(new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["Primary"]
                });
                charts.Children.Add(new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                }, 0, 1);
                charts.Children.Add(new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["Primary"]
                }, 0, 2);
                charts.Children.Add(new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                }, 0, 3);
                charts.Children.Add(new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["Primary"]
                }, 0, 4);
                charts.Children.Add(new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                }, 0, 5);


                ratings = ratings.OrderBy(r => DateTime.ParseExact(r.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture)).ToList();

                var massEntries = new List<Microcharts.Entry>();
                var fatEntries = new List<Microcharts.Entry>();
                var weightEntries = new List<Microcharts.Entry>();

                ratings.ForEach(r =>
                {
                    massEntries.Add(new Microcharts.Entry(float.Parse(r.Mass))
                    {
                        Color = SKColor.Parse("#de4905"),
                        Label = r.Date,
                        ValueLabel = r.Mass
                    }); ;

                    fatEntries.Add(new Microcharts.Entry(float.Parse(r.Fat))
                    {
                        Color = SKColor.Parse("#de4905"),
                        Label = r.Date,
                        ValueLabel = r.Fat
                    }); ;

                    weightEntries.Add(new Microcharts.Entry(float.Parse(r.Weight))
                    {
                        Color = SKColor.Parse("#de4905"),
                        Label = r.Date,
                        ValueLabel = r.Weight
                    }); ;
                });

                charts.Children.Add(GenerateChartNameLabel("Massa magra"));
                charts.Children.Add(GenerateChart(massEntries), 0, 1);

                charts.Children.Add(GenerateChartNameLabel("Gordura"), 0, 2);
                charts.Children.Add(GenerateChart(fatEntries), 0, 3);

                charts.Children.Add(GenerateChartNameLabel("Peso"), 0, 4);
                charts.Children.Add(GenerateChart(weightEntries), 0, 5);

                StackLayout view = new StackLayout()
                {
                    Spacing = 0
                };
                view.Children.Add(GenerateOverviewHeader(new Rating[] { ratings.First(), ratings.Last() }));
                view.Children.Add(new BoxView { HorizontalOptions = LayoutOptions.FillAndExpand, HeightRequest = 1, BackgroundColor = (Color)_app.Resources["LightTransparent"] });
                view.Children.Add(new ScrollView()
                {
                    Content = charts
                });

                detailLayout.Children.Add(view, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            else
            {
                Grid bg = new Grid()
                {
                    BackgroundColor = (Color)_app.Resources["PrimaryTransparent"],
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };
                bg.Children.Add(new Label
                {
                    Text = "Nenhuma avaliação registrada...",
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Fill,
                    TextColor = (Color)_app.Resources["Orange"]
                });
                detailLayout.Children.Add(bg, new Rectangle(0, 0, 1, 1),
                AbsoluteLayoutFlags.All);
            }
        }
        public ChartView GenerateChart(List<Microcharts.Entry> entries)
        {
            Microcharts.LineChart chart = new Microcharts.LineChart()
            {
                BackgroundColor = SKColor.Empty,
                LabelTextSize = 30,
                LineSize = 10,
                PointSize = 20,
                Entries = entries
            };

            return new ChartView
            {
                Chart = chart,
                Margin = 10,
                HeightRequest = 120
            };
        }
        public Label GenerateChartNameLabel(string name)
        {
            return new Label
            {
                Text = name,
                TextColor = (Color)_app.Resources["Orange"],
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
        }
        public Grid GenerateOverviewHeader(Rating[] r)
        {
            Grid overViewHeader = new Grid()
            {
                RowSpacing = 0,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                BackgroundColor = (Color)_app.Resources["DarkTransparent"],
                ColumnSpacing = 0
            };

            for (int x = 0; x < 7; x++)
            {
                overViewHeader.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            }

            BoxView overviewTextBg = new BoxView
            {
                BackgroundColor = (Color)_app.Resources["PrimaryDark"]
            };
            overViewHeader.Children.Add(overviewTextBg);
            Grid.SetColumnSpan(overviewTextBg, 3);

            var textLabel = new Label
            {
                Text = "VISÃO GERAL",
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                TextColor = (Color)_app.Resources["Orange"]
            };
            overViewHeader.Children.Add(textLabel, 0, 0);
            Grid.SetColumnSpan(textLabel, 3);

            BoxView labelsBg = new BoxView
            {
                BackgroundColor = (Color)_app.Resources["DarkTransparent"]
            };
            overViewHeader.Children.Add(labelsBg, 0, 1);
            Grid.SetRowSpan(labelsBg, 6);

            BoxView valuesBg = new BoxView
            {
                BackgroundColor = (Color)_app.Resources["PrimaryTransparent"]
            };
            overViewHeader.Children.Add(valuesBg, 1, 1);
            Grid.SetRowSpan(valuesBg, 6);
            Grid.SetColumnSpan(valuesBg, 2);

            string[] valuesNames = new string[5] { "Massa magra", "Gordura", "Peso", "Altura", "Mobilidade" };
            int i = 2;
            foreach (string n in valuesNames)
            {
                var margin = new Thickness(0);
                if (i == 6)
                    margin = new Thickness(0, 0, 0, 10);

                overViewHeader.Children.Add(new Label
                {
                    Text = n,
                    TextColor = (Color)_app.Resources["Orange"],
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = margin
                }, 0, i);
                i++;
            }

            int z = 1;
            foreach (Rating rating in r)
            {
                overViewHeader.Children.Add(new Label
                {
                    Text = rating.Date,
                    TextColor = (Color)_app.Resources["LightTransparent"],
                    HorizontalTextAlignment = TextAlignment.Center,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                }, z, 1);
                overViewHeader.Children.Add(new Label
                {
                    Text = rating.Mass + " %",
                    TextColor = (Color)_app.Resources["Orange"],
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }, z, 2);
                overViewHeader.Children.Add(new Label
                {
                    Text = rating.Fat + " %",
                    TextColor = (Color)_app.Resources["Orange"],
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }, z, 3);
                overViewHeader.Children.Add(new Label
                {
                    Text = rating.Weight,
                    TextColor = (Color)_app.Resources["Orange"],
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }, z, 4);
                overViewHeader.Children.Add(new Label
                {
                    Text = rating.Height,
                    TextColor = (Color)_app.Resources["Orange"],
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                }, z, 5);
                overViewHeader.Children.Add(new Label
                {
                    Text = rating.Mobility,
                    TextColor = (Color)_app.Resources["Orange"],
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                }, z, 6);
                z++;
            }

            return overViewHeader;
        }

        //----------- RATINGS VIEW -----------


        //----------- PLAN VIEW -----------

        public void SpawnPlanView()
        {
            Title = "Meu plano";

            StackLayout planLayout = new StackLayout()
            {
                BackgroundColor = (Color)_app.Resources["DarkTransparent"],
                Padding = 20
            };

            var tp = _app.LoggedInUser.UserPlan.TrainPlan;
            if (tp != null)
                planLayout.Children.Add(CreatePlanGrid(tp));

            var yp = _app.LoggedInUser.UserPlan.YogaPlan;
            if (yp != null)
                planLayout.Children.Add(CreatePlanGrid(yp));

            var pp = _app.LoggedInUser.UserPlan.PilatesPlan;
            if (pp != null)
                planLayout.Children.Add(CreatePlanGrid(pp));

            detailLayout.Children.Add(planLayout, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
        }
        Grid CreatePlanGrid(PlanModels.Plan plan)
        {
            Grid planLayout = new Grid()
            {
                BackgroundColor = (Color)_app.Resources["DarkTransparent"],
                RowSpacing = 0,
                Padding = 20
            };

            planLayout.Children.Add(new Label()
            {
                Text = (plan.IsYoga ? "Yoga" : plan.IsPilates ? "Pilates" : plan.Type) +
                        (plan.IsFloating ? " ☁" : ""),
                TextColor = plan.IsYoga || plan.IsPilates ? (Color)_app.Resources["Yoga"] : (Color)_app.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            }, 0, 0);

            planLayout.Children.Add(new Label()
            {
                Text = plan.TimesPerWeek + "x por semana",
                TextColor = (Color)_app.Resources["TextLight"],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
            }, 0, 1);

            var price = new Label()
            {
                Text = plan.Price + " R$",
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 6, 0, 0),
                TextColor = (Color)_app.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            };
            planLayout.Children.Add(price, 1, 0);
            Grid.SetRowSpan(price, 2);

            var date = plan.IsYoga ? _app.LoggedInUser.UserPlan.YogaPlanExpiryDate : plan.IsPilates ? _app.LoggedInUser.UserPlan.PilatesPlanExpiryDate : _app.LoggedInUser.UserPlan.TrainPlanExpiryDate;
            var expiry = new Label()
            {
                Margin = new Thickness(0, 10, 0, 0),
                Text = "Vencimento: " + DateTime.Parse(date).ToString("dd/MM/yyyy"),
                TextColor = (Color)_app.Resources["TextLight"],
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
            };
            planLayout.Children.Add(expiry, 0, 2);
            Grid.SetColumnSpan(expiry, 2);

            return planLayout;
        }

        //----------- PLAN VIEW -----------

        //----------- EVENTS VIEW -----------

        public IListenerRegistration EventsListener = null;
        public void SpawnEventsView()
        {
            Title = "Eventos";
            if(EventsListener == null)
                SetUpEventsListener();

            if (_app.SavedEvents != null && _app.SavedEvents.Count > 0)
            {
                StackLayout eventsContent = new StackLayout()
                {
                    Padding = 10,
                    Spacing = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };

                int z = 0;
                foreach (Events e in _app.SavedEvents)
                {
                    BoxView backgroundColor = new BoxView()
                    {
                        Margin = 0,
                        BackgroundColor = e.ConfirmedUsers.Contains(_app.LoggedInUser.UserID) ? Color.FromHex("#55E31B") : (Color)Application.Current.Resources["Yellow"],
                    };

                    BoxView background = new BoxView()
                    {
                        Margin = 1,
                        BackgroundColor = (Color)_app.Resources["PrimaryDark"]
                    };

                    Grid eventGrid = new Grid()
                    {
                        Padding = 0,
                        ColumnSpacing = 0,
                        RowSpacing = 0,
                        BackgroundColor = BackgroundColor = (Color)Application.Current.Resources["PrimaryDark"]
                    };

                    eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    eventGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                    eventGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                    eventGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                    Grid.SetRowSpan(backgroundColor, 4);
                    Grid.SetColumnSpan(backgroundColor, 2);
                    eventGrid.Children.Add(backgroundColor);

                    Grid.SetRowSpan(background, 4);
                    Grid.SetColumnSpan(background, 2);
                    eventGrid.Children.Add(background);

                    BoxView bg = new BoxView
                    {
                        BackgroundColor = (Color)_app.Resources["DarkTransparent"],
                        Margin = new Thickness(1, 1, 1, 0),
                        VerticalOptions = LayoutOptions.FillAndExpand
                    };
                    eventGrid.Children.Add(bg);
                    Grid.SetColumnSpan(bg, 2);
                    Grid.SetRowSpan(bg, 2);

                    var userList = new Image()
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        Source = e.ConfirmedUsers.Contains(_app.LoggedInUser.UserID) ? "ic_plus_accent" : "ic_checkmark.png",
                        Rotation = e.ConfirmedUsers.Contains(_app.LoggedInUser.UserID) ? 45 : 0,
                        Scale = 1.5,
                        ClassId = z.ToString()
                    };
                    var confirmBtn = new TapGestureRecognizer();
                    confirmBtn.NumberOfTapsRequired = 1;
                    confirmBtn.Tapped += async (sender, ex) =>
                    {
                        int id = Int32.Parse((sender as Image).ClassId);
                        var selectedEvent = _app.SavedEvents[id];

                        if (!selectedEvent.ConfirmedUsers.Contains(_app.LoggedInUser.UserID))
                        {
                            if (await DisplayAlert("Confirmar presença", "Deseja confirmar sua presença?", "Sim", "Não"))
                            {
                                await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                                if (await UserUtilities.EventsPresenceSetup(_app.LoggedInUser.UserID, selectedEvent.ID, "add"))
                                {
                                    SpawnEventsView();
                                    await DisplayAlert("Sucesso", "Presença confirmada!", "Ok");
                                }
                                else
                                {
                                    await DisplayAlert("Erro", "Não foi possível confirmar o evento, tente novamente mais tarde", "Ok");
                                }
                                await PopupNavigation.Instance.PopAsync();
                            }
                        }
                        else
                        {
                            if (await DisplayAlert("Cancelar presença", "Deseja cancelar sua presença?", "Sim", "Não"))
                            {
                                await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                                if (await UserUtilities.EventsPresenceSetup(_app.LoggedInUser.UserID, selectedEvent.ID, "remove"))
                                {
                                    SpawnEventsView();
                                    await DisplayAlert("Sucesso", "Presença cancelada com sucesso!", "Ok");
                                }
                                else
                                {
                                    await DisplayAlert("Erro", "Não foi possível cancelar, tente novamente mais tarde", "Ok");
                                }
                                await PopupNavigation.Instance.PopAsync();
                            }
                        }
                    };
                    userList.GestureRecognizers.Add(confirmBtn);
                    eventGrid.Children.Add(userList, 1, 0);
                    Grid.SetRowSpan(userList, 2);

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

                    if (!String.IsNullOrEmpty(e.Description))
                    {
                        var desc = new Label()
                        {
                            Text = e.Description,
                            Padding = new Thickness(10, 5, 10, 10),
                            TextColor = (Color)Application.Current.Resources["TextLight"],
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                            VerticalOptions = LayoutOptions.Start,
                            HorizontalOptions = LayoutOptions.StartAndExpand
                        };
                        eventGrid.Children.Add(desc, 0, 2);
                        Grid.SetColumnSpan(desc, 2);
                    }

                    var presenceText = new Label()
                    {
                        Text = e.ConfirmedUsers.Contains(_app.LoggedInUser.UserID) ? "PRESENÇA CONFIRMADA" : "CONFIRME SUA PRESENÇA!",
                        TextColor = e.ConfirmedUsers.Contains(_app.LoggedInUser.UserID) ? Color.FromHex("#55E31B") : (Color)Application.Current.Resources["Yellow"],
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        Margin = new Thickness(0, 5, 0, 10)
                    };
                    eventGrid.Children.Add(presenceText, 0, 3);
                    Grid.SetColumnSpan(presenceText, 2);

                    eventsContent.Children.Add(eventGrid);
                    z++;
                }

                ScrollView eventsView = new ScrollView()
                {
                    Content = eventsContent
                };
                detailLayout.Children.Add(eventsView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            else
            {
                Grid bg = new Grid()
                {
                    BackgroundColor = (Color)_app.Resources["PrimaryTransparent"],
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand
                };
                bg.Children.Add(new Label
                {
                    Text = "Nenhum evento disponível...",
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Fill,
                    TextColor = (Color)_app.Resources["Orange"]
                });
                detailLayout.Children.Add(bg, new Rectangle(0, 0, 1, 1),
                AbsoluteLayoutFlags.All);
            }
        }
        public void SetUpEventsListener()
        {
            try
            {
                EventsListener = CrossCloudFirestore.Current.Instance
                            .Collection("events")
                            .AddSnapshotListener((snp, error) =>
                            {
                                if (!snp.Metadata.IsFromCache && !snp.Metadata.HasPendingWrites)
                                {
                                    foreach (var documentChange in snp.DocumentChanges)
                                    {
                                        var changedDoc = documentChange.Document.ToObject<Events>();
                                        switch (documentChange.Type)
                                        {
                                            case DocumentChangeType.Added:
                                                {
                                                    _app.SavedEvents.Add(changedDoc);
                                                    _app.SavedEvents = _app.SavedEvents;
                                                }
                                                break;
                                            case DocumentChangeType.Modified:
                                                {
                                                    var oldDoc = _app.SavedEvents.Find(e => e.ID == changedDoc.ID);

                                                    if (oldDoc != changedDoc)
                                                    {
                                                        _app.SavedEvents[_app.SavedEvents.IndexOf(oldDoc)] = changedDoc;
                                                        _app.SavedEvents = _app.SavedEvents;
                                                    }
                                                }
                                                break;
                                            case DocumentChangeType.Removed:
                                                {
                                                    _app.SavedEvents.Remove(_app.SavedEvents.Find(e => e.ID == changedDoc.ID));
                                                    _app.SavedEvents = _app.SavedEvents;
                                                }
                                                break;
                                        }
                                        SpawnEventsView();
                                    }
                                }
                            });
            }
            catch (Exception) {}
        }

        //----------- EVENTS VIEW -----------

        //----------- OTHERS -----------

        public void ClearView()
        {
            if (ToolbarItems.Count > 0)
                ToolbarItems.Clear();
            if (detailLayout.Children.Count > 1)
            {
                int i = 1;
                while (i < detailLayout.Children.Count)
                {
                    detailLayout.Children.RemoveAt(i);
                }
            }
            dataLabel.Clear();
            hiddenEntries.Clear();

            isEditing = false;
            changedProfile = false;
        }

        BoxView CreateSeparator(string Color = "PrimaryTransparent")
        {
            try
            {
                return new BoxView { BackgroundColor = (Color)_app.Resources[Color], HeightRequest = 1, HorizontalOptions = LayoutOptions.FillAndExpand };
            }
            catch
            {
                return new BoxView { BackgroundColor = (Color)_app.Resources["PrimaryTransparent"], HeightRequest = 1, HorizontalOptions = LayoutOptions.FillAndExpand };
            }
        }

        bool CheckConnection()
        {
            return Connectivity.NetworkAccess != NetworkAccess.Internet;
        }
        void DisplayUnkownErrorMessage(Exception e)
        {
            DisplayAlert("Erro desconhecido!", "Código do erro: " + e, "OK");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (Application.Current.MainPage as StudentPage).IsGestureEnabled = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                (Application.Current.MainPage as StudentPage).IsGestureEnabled = false;
            }
            catch { }
        }

        //----------- OTHERS -----------
    }
}