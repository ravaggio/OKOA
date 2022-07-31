using ctf_final.Models;
using ImageCircle.Forms.Plugin.Abstractions;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.AdmContents.Students
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StudentInformationPage : ContentPage
    {
        User user;
        SimplifiedUser simpleUser;

        IListenerRegistration listener = null;

        CircleImage profilePicture;
        Label userName;

        StackLayout classesList;

        Label makeupClasses;
        Label yogaMakeupClasses;
        Label pilatesMakeupClasses;

        Grid gridLayout;

        public StudentInformationPage(SimplifiedUser u)
        {
            InitializeComponent();
            simpleUser = u;

            ToolbarItem viewSchedules = new ToolbarItem { IconImageSource = "ic_edit.png" };
            viewSchedules.Clicked += EditUser;
            ToolbarItems.Add(viewSchedules);

            ToolbarItem addSchedule = new ToolbarItem { IconImageSource = "ic_close.png" };
            addSchedule.Clicked += RemoveUser;
            ToolbarItems.Add(addSchedule);

            MessagingCenter.Subscribe<User>(this, "UserUpdated", newUser =>
            {
                user = newUser;
                Device.BeginInvokeOnMainThread(() =>
                {
                    if(mainLayout.Children.Count > 0)
                        mainLayout.Children.Clear();

                    GenerateFullView();
                });
            });
        }
        
        private void GenerateFullView()
        {
            GenerateHeader();
            GenerateDetails();

            loadingSign.IsRunning = false;
            loadingSign.IsVisible = false;
        }

        private void GenerateHeader()
        {
            try
            {
                var headerGrid = new Grid
                {
                    RowSpacing = 0,
                    ColumnSpacing = 0
                };

                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                headerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                headerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                //Visual Details
                var headerBackground = new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                };
                headerGrid.Children.Add(headerBackground);
                Grid.SetColumnSpan(headerBackground, 3);

                var headerDivider = new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["LightTransparent"],
                    HeightRequest = 1,
                    VerticalOptions = LayoutOptions.End
                };
                headerGrid.Children.Add(headerDivider);
                Grid.SetColumnSpan(headerDivider, 3);

                var buttonsBg = new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["Primary"]
                };
                headerGrid.Children.Add(buttonsBg, 0, 1);
                Grid.SetColumnSpan(buttonsBg, 3);

                //Data
                string picToken = user.PictureToken == "" ? SharedUtilities.DefaultPictureToken : user.PictureToken;
                profilePicture = new CircleImage
                {
                    Source = picToken,
                    HeightRequest = 72,
                    WidthRequest = 72,
                    Aspect = Aspect.AspectFill
                };

                StackLayout pictureHolder = new StackLayout
                {
                    Spacing = 0,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 12)
                };
                pictureHolder.Children.Add(profilePicture);

                headerGrid.Children.Add(pictureHolder, 0, 0);

                var userNameText = new FormattedString();
                userNameText.Spans.Add(new Span { Text = user.Name+"\n", TextColor = (Color)_app.Resources["Orange"], FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))});
                userNameText.Spans.Add(new Span { Text = user.UserID.ToString(), TextColor = (Color)_app.Resources["TextLight"], FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))});

                userName = new Label
                {
                    FormattedText = userNameText,
                    Margin = new Thickness(0, 12, 4, 12),
                    VerticalOptions = LayoutOptions.Center
                };
                headerGrid.Children.Add(userName, 1, 0);
                Grid.SetColumnSpan(userName, 2);

                //Buttons
                var buttonsTexts = new String[3] { "AVALIAÇÕES", "PLANO", "REPOSIÇÕES" };
                if(_app.LoggedInUser.Function == "TEACHER")
                    buttonsTexts = new String[3] { "AVALIAÇÕES", "", "REPOSIÇÕES" };

                int pos = 0;
                foreach(var btnText in buttonsTexts)
                {
                    if (btnText != "")
                    {
                        var btn = new Button
                        {
                            Text = btnText,
                            TextColor = (Color)_app.Resources["Orange"],
                            BackgroundColor = (Color)_app.Resources["Primary"],
                        };

                        if (_app.LoggedInUser.Function == "TEACHER" && pos == 0)
                            Grid.SetColumnSpan(btn, 2);
                        else
                            switch (pos)
                            {
                                case 0:
                                    btn.Clicked += (sender, e) => ManageRatingsBtn(sender, e);
                                    break;
                                case 1:
                                    btn.Clicked += (sender, e) => PlanBtn(sender, e);
                                    break;
                                case 2:
                                    btn.Clicked += (sender, e) => AddRepoBtn(sender, e);
                                    break;
                            }

                        headerGrid.Children.Add(btn, pos, 1);
                    }
                    pos++;
                }

                var headerDivider2 = new BoxView
                {
                    BackgroundColor = (Color)_app.Resources["LightTransparent"],
                    HeightRequest = 1,
                    VerticalOptions = LayoutOptions.End
                };
                headerGrid.Children.Add(headerDivider2, 0, 1);
                Grid.SetColumnSpan(headerDivider2, 3);

                mainLayout.Children.Add(headerGrid);
            }
            catch
            {
                CloseView("Erro desconhecido. Se o erro persistir, contate o desenvolvedor.");
            }
        }
        private void GenerateDetails()
        {
            var detailsLayout = new StackLayout
            {
                Spacing = 0
            };

            GenerateClassesList(detailsLayout);
            GenerateUserAccountDetails(detailsLayout);

            mainLayout.Children.Add(new ScrollView() { Content = detailsLayout });
        }

        private void GenerateClassesList(StackLayout detailsLayout)
        {
            gridLayout = new Grid
            {
                ColumnSpacing = 0,
                RowSpacing = 0,
                Margin = 14,
                BackgroundColor = (Color)_app.Resources["DarkTransparent"],
            };

            gridLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            gridLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            gridLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            gridLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            gridLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            //Header Background
            var headerBg = new BoxView
            {
                BackgroundColor = (Color)_app.Resources["Primary"]
            };
            gridLayout.Children.Add(headerBg);
            Grid.SetRowSpan(headerBg, 4);

            //Header Details
            gridLayout.Children.Add(new Label
            {
                Text = "AULAS E REPOSIÇÕES",
                TextColor = (Color)_app.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                Margin = new Thickness(4, 10),
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            });

            makeupClasses = new Label
            {
                TextColor = (Color)_app.Resources["TextLight"],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            yogaMakeupClasses = new Label
            {
                TextColor = (Color)_app.Resources["TextLight"],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };
            pilatesMakeupClasses = new Label
            {
                TextColor = (Color)_app.Resources["TextLight"],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            UpdateMakeupClassesText(user);

            gridLayout.Children.Add(makeupClasses, 0, 1);
            gridLayout.Children.Add(yogaMakeupClasses, 0, 2);
            gridLayout.Children.Add(pilatesMakeupClasses, 0, 3);

            //Classes Overview
            classesList = new StackLayout
            {
                Spacing = 0
            };
            UpdateUserClasses(user);

            gridLayout.Children.Add(classesList, 0, 4);

            detailsLayout.Children.Add(gridLayout);
        }
        private void GenerateUserAccountDetails(StackLayout detailsLayout)
        {
            var accDetails = new StackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };

            accDetails.Children.Add(new Label
            {
                Text = string.Format("{0}/{1}/{2}", user.Birthday.Substring(0, 2), user.Birthday.Substring(2, 2), user.Birthday.Substring(4)),
                TextColor = (Color)_app.Resources["Orange"],
                HorizontalTextAlignment = TextAlignment.Center
            });

            accDetails.Children.Add(new Label
            {
                Text = user.Gender == 0 ? "Masculino" : user.Gender == 1 ? "Feminino" : "Não Informar",
                TextColor = (Color)_app.Resources["Orange"],
                HorizontalTextAlignment = TextAlignment.Center
            });

            if (user.Phone != null && user.Phone != "")
            {
                string pVal = user.Phone;
                string formattedPhone = "({0}) {1}-{2}";
                var final_phone = string.IsNullOrWhiteSpace(pVal) ? "" : string.Format(formattedPhone, pVal.Substring(0, 2), pVal.Substring(2, (pVal.Length - 6)), pVal.Substring((pVal.Length - 6) + 2));

                accDetails.Children.Add(new Label
                {
                    Text = final_phone,
                    TextColor = (Color)_app.Resources["Orange"],
                    HorizontalTextAlignment = TextAlignment.Center
                });
            }

            if (user.Email != null && user.Email != "")
            {
                accDetails.Children.Add(new Label
                {
                    Text = user.Email,
                    TextColor = (Color)_app.Resources["Orange"],
                    HorizontalTextAlignment = TextAlignment.Center
                });
            }

            if (user.Address != null && user.Address != "")
            {
                accDetails.Children.Add(new Label
                {
                    Text = user.Address,
                    TextColor = (Color)_app.Resources["Orange"],
                    HorizontalTextAlignment = TextAlignment.Center
                });
            }

            detailsLayout.Children.Add(accDetails);
        }

        private void UpdateMakeupClassesText(User u)
        {
            makeupClasses.IsVisible = u.UserPlan.TrainPlan != null;
            if (u.UserPlan.TrainPlan != null)
            {
                makeupClasses.Text = "Treino: " + u.MakeupClasses;
                makeupClasses.Margin = u.UserPlan.YogaPlan == null ? new Thickness(0, 0, 0, 10) : 0;
            }

            yogaMakeupClasses.IsVisible = u.UserPlan.YogaPlan != null;
            if (u.UserPlan.YogaPlan != null)
            {
                yogaMakeupClasses.Text = "Yoga: " + u.MakeupClassesYoga;
            }

            pilatesMakeupClasses.IsVisible = u.UserPlan.PilatesPlan != null;
            if (u.UserPlan.PilatesPlan != null)
            {
                pilatesMakeupClasses.Text = "Pilates: " + u.MakeupClassesPilates;
            }
        }
        private void UpdateUserClasses(User u)
        {
            if (classesList != null && classesList.Children.Count > 0)
                classesList.Children.Clear();
            var classes = SharedUtilities.FormattUserClassesWithExceptions(u);

            var cleanClassesList = new List<string>();
            classes.ForEach(c =>
            {
                cleanClassesList.Add(c.Split('@')[1]);
            });
            cleanClassesList = cleanClassesList.OrderBy(c => c.Substring(0, 10)).ToList();

            cleanClassesList.ForEach(c =>
            {
                classesList.Children.Add(GetClassLayout(c));
                classesList.Children.Add(new BoxView { HorizontalOptions = LayoutOptions.Fill, BackgroundColor = (Color)_app.Resources["LightTransparent"], HeightRequest = 1 });
            });
        }
        private StackLayout GetClassLayout(string details)
        {
            var splittenDetails = details.Split('/');

            var classLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Padding = new Thickness(12, 6)
            };

            var detailsString = new FormattedString();
            detailsString.Spans.Add(new Span { Text = splittenDetails[2] + "\n", TextColor = splittenDetails[2] == "Treino" ? (Color)_app.Resources["Orange"] : (Color)_app.Resources["Yoga"] });
            detailsString.Spans.Add(new Span { Text = splittenDetails[1], TextColor = (Color)_app.Resources["TextLight"] });
            classLayout.Children.Add(new Label
            {
                FormattedText = detailsString,
                HorizontalOptions = LayoutOptions.StartAndExpand,
                HorizontalTextAlignment = TextAlignment.Start,
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label))
            });

            classLayout.Children.Add(new Label
            {
                Text = DateTime.Parse(splittenDetails[0]).ToString("dd/MM"),
                TextColor = (Color)_app.Resources["TextLight"],
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
            });

            return classLayout;
        }

        private async void ManageRatingsBtn(object sender, EventArgs e)
        {
            editing = true;
            await Navigation.PushAsync(new StudentRatings(user));
        }
        private async void AddRepoBtn(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(new PopupPages.AddMakeupClass(user));
        }
        private async void PlanBtn(object sender, EventArgs e)
        {
            editing = true;
            await Navigation.PushAsync(new PlanConfigPage(user));
        }

        private async void EditUser(object sender, EventArgs e)
        {
            editing = true;
            await Navigation.PushAsync(new StudentCadastre(user));
        }
        private async void RemoveUser(object sender, EventArgs e)
        {
            if (await DisplayAlert("Remover usuário", "Tem certeza que deseja remover esse usuário? (Esta ação não pode ser desfeita)", "Sim", "Não"))
            {
                await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup(), true);
                if(await AdmUtilities.RemoveUser(user))
                {
                    listener.Remove();
                    await DisplayAlert("Sucesso!", "O usuário foi removido com sucesso", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Erro", "Incapaz de remover o usuário... Verifique sua conexão com a internet e tente novamente.", "OK");
                }
                await PopupNavigation.Instance.PopAsync();
            }
        }

        private async void CloseView(string error = "")
        {
            if (error != "")
                await DisplayAlert("Erro", error, "Ok");

            await Navigation.PopAsync();
        }

        bool firstAppearence = false;
        bool editing = false;
        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (!firstAppearence)
            {
                firstAppearence = true;
                try
                {
                    var query = await CrossCloudFirestore.Current
                                                .Instance
                                                .Collection("users")
                                                .Document(simpleUser.UserID.ToString())
                                                .GetAsync();
                    var u = query.ToObject<User>();

                    if (u == null)
                        CloseView("Não foi possível encontrar o documento deste usuário, tente novamente e se o erro persistir contate o desenvolvedor...");

                    await SharedUtilities.RemoveOutdatedMakeupClasses(u);
                    await SharedUtilities.RemoveOldClassesExceptions(u);
                    listener = query.Reference.AddSnapshotListener((snp, error) =>
                    {
                        if (snp != null && !snp.Metadata.IsFromCache)
                        {
                            var newUser = snp.ToObject<User>();
                            if (newUser != null)
                            {
                                if(newUser.Name != user.Name)
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        var userNameText = new FormattedString();
                                        userNameText.Spans.Add(new Span { Text = newUser.Name + "\n", TextColor = (Color)_app.Resources["Orange"], FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)) });
                                        userNameText.Spans.Add(new Span { Text = newUser.UserID.ToString(), TextColor = (Color)_app.Resources["TextLight"], FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)) });

                                        userName.FormattedText = userNameText;
                                    });

                                if (newUser.PictureToken != user.PictureToken)
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        string picToken = newUser.PictureToken == "" ? SharedUtilities.DefaultPictureToken : user.PictureToken; 
                                        profilePicture.Source = picToken;
                                    });

                                if (newUser.ClassesExceptions != user.ClassesExceptions || newUser.ScheduleReferences != user.ScheduleReferences)
                                    Device.BeginInvokeOnMainThread(() => UpdateUserClasses(newUser));

                                if (newUser.UserPlan != user.UserPlan || newUser.MakeupClasses != user.MakeupClasses || newUser.MakeupClassesYoga != user.MakeupClassesYoga)
                                    Device.BeginInvokeOnMainThread(() => UpdateMakeupClassesText(newUser));

                                if (user != newUser)
                                    user = newUser;
                            }
                        }
                    });

                    user = u;
                    GenerateFullView();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    CloseView("Não foi possível encontrar o documento deste usuário, tente novamente e se o erro persistir contate o desenvolvedor...");
                }
            }
            else
            {
                if (editing)
                    editing = false;
            }
        }
        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if(listener != null && !editing)
                listener.Remove();
        }


    }
}