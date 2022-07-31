using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ImageCircle.Forms.Plugin.Abstractions;
using ctf_final.Models;
using System;
using static ctf_final.AppController;
using Rg.Plugins.Popup.Services;
using System.Collections.Generic;
using Plugin.CloudFirestore;
using XamarinFirebase.Model;

namespace ctf_final.AdmContents
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ClassEditPage : ContentPage
    {
        readonly string selectionTime = "";
        readonly int maxSize = 0;
        StackLayout addBtn;
        readonly List<StackLayout> experimentalClasses = new List<StackLayout>();

        SchedulesByDayOfWeek.Times times;
        readonly int weekday;

        int n_of_eclasses;

        public ClassEditPage(SchedulesByDayOfWeek.Times s, int wd, DateTime dt)
        {
            InitializeComponent();
            Title = SharedUtilities.IntToWeekday(wd) + " - " + dt.ToString("dd/MM");

            times = s;
            weekday = wd;
            maxSize = SharedUtilities.GetClassSizeLimitByType(s.Type);
            selectionTime = s.Time;

            GenerateView(s);
            MessagingCenter.Subscribe<SchedulesByDayOfWeek.Times>(this, "ChangeClassViewPage", newClass =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    contentLayout.Children.Clear();
                    times = newClass;

                    GenerateView(newClass);
                });
            });
        }

        void GenerateView(SchedulesByDayOfWeek.Times s)
        {
            var eclasses = s.StudentsList.FindAll(id => id >= 500 && id < 512);
            n_of_eclasses = eclasses == null ? 0 : eclasses.Count;

            var userList = SharedUtilities.GetOrderedByNameUserList(s.StudentsList, true);
            userList.ForEach(user =>
            {
                try
                {
                    StackLayout studentPreview = new StackLayout()
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        Spacing = 10,
                        Padding = 10,
                        BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]
                    };

                    string picSource = user.PictureToken == "" ? SharedUtilities.DefaultPictureToken : user.PictureToken;
                    studentPreview.Children.Add(new CircleImage
                    {
                        Source = picSource,
                        Aspect = Aspect.AspectFill,
                        HeightRequest = 46,
                        WidthRequest = 46
                    });

                    studentPreview.Children.Add(new Label
                    {
                        Text = user.Name,
                        TextColor = (Color)Application.Current.Resources["Orange"],
                        FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                        VerticalOptions = LayoutOptions.CenterAndExpand,
                        HorizontalOptions = LayoutOptions.StartAndExpand
                    });

                    Image excludeImage = new Image
                    {
                        ClassId = user.UserID.ToString(),
                        Source = "ic_plus_accent.png",
                        Rotation = 45,
                        HorizontalOptions = LayoutOptions.End
                    };
                    TapGestureRecognizer tapExclude = new TapGestureRecognizer();
                    tapExclude.Tapped += async (sender, e) =>
                    {
                        var typeSelectionPopup = new PopupPages.ThreeButtonsDialog("Remover Aluno", "Deseja remover o aluno e adicionar uma reposição?", "Apenas remover", "Sim");
                        await PopupNavigation.Instance.PushAsync(typeSelectionPopup, true);

                        var typeSelection = await typeSelectionPopup.GetSelection();
                        if (typeSelection != 0)
                        {
                            await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                            if (await AdmUtilities.RemoveUserFromClass(times, times.Date + "/" + times.Time + "/" + times.Type, Int32.Parse((sender as Image).ClassId), typeSelection == 2))
                            {
                                contentLayout.Children.Clear();
                                GenerateView(times);

                                await DisplayAlert("Sucesso!", "Aluno removido com sucesso.", "Ok");
                            }
                            else
                            {
                                await DisplayAlert("Erro", "Não foi possível remover o aluno, verifique sua conexão e tente novamente.", "Ok");
                            }
                            await PopupNavigation.Instance.PopAsync();
                        }
                    };
                    tapExclude.NumberOfTapsRequired = 1;
                    excludeImage.GestureRecognizers.Add(tapExclude);
                    studentPreview.Children.Add(excludeImage);

                    contentLayout.Children.Add(studentPreview);
                    contentLayout.Children.Add(new BoxView { BackgroundColor = (Color)Application.Current.Resources["LightTransparent"], HorizontalOptions = LayoutOptions.Fill, HeightRequest = 1 });
                }
                catch { }
            });

            addBtn = new StackLayout
            {
                IsVisible = true,
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Padding = 10
            };
            TapGestureRecognizer tapAdd = new TapGestureRecognizer();
            tapAdd.Tapped += async (sender, ex) =>
            {
                var typeSelectionPopup = new PopupPages.ThreeButtonsDialog("Adicionar Aluno", "Deseja adicionar um aluno as " + selectionTime + "? Selecione o tipo de aula para continuar.", "Experimental", "Aluno");
                await PopupNavigation.Instance.PushAsync(typeSelectionPopup);

                var typeSelection = await typeSelectionPopup.GetSelection();
                if (typeSelection == 1)
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                    if (await AdmUtilities.ChangeExperimentalClass(s, weekday, s.Date + "/" + s.Time + "/" + s.Type, 500 + n_of_eclasses))
                    {
                        contentLayout.Children.Clear();
                        if (Device.RuntimePlatform == Device.iOS)
                            times.StudentsList.Add(500 + n_of_eclasses);
                        GenerateView(times);

                        await DisplayAlert("Sucesso!", "A aula experimental foi adicionada com sucesso.", "Ok");
                    }
                    else
                    {
                        await DisplayAlert("Erro", "Não foi possível adicionar a aula experimental, verifique sua conexão e tente novamente.", "Ok");
                    }
                    await PopupNavigation.Instance.PopAsync();
                }
                else if(typeSelection == 2)
                {
                    var studentSelection = new Students.StudentsSelectionPage("add_to_classes", s.StudentsList);
                    await Navigation.PushAsync(studentSelection);

                    var id = await studentSelection.GetUserID();
                    if(id != -1)
                    {
                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                        var docpath = s.Date + "/" + s.Time + "/" + s.Type;
                        if (await AdmUtilities.ChangeExperimentalClass(s, weekday, docpath, id))
                        {
                            contentLayout.Children.Clear();
                            if (Device.RuntimePlatform == Device.iOS)
                                times.StudentsList.Add(id);
                            GenerateView(times);

                            await DisplayAlert("Sucesso!", "A aula foi adicionada com sucesso.", "Ok");
                        }
                        else
                        {
                            await DisplayAlert("Erro", "Não foi possível adicionar a aula, verifique sua conexão e tente novamente.", "Ok");
                        }
                        await PopupNavigation.Instance.PopAsync();
                    }
                }
            };
            tapAdd.NumberOfTapsRequired = 1;

            addBtn.GestureRecognizers.Add(tapAdd);

            addBtn.Children.Add(new StackLayout
            {
                Spacing = 8,
                Orientation = StackOrientation.Horizontal
            });
            (addBtn.Children[0] as StackLayout).Children.Add(new Image
            {
                Source = "ic_plus_accent.png",
                VerticalOptions = LayoutOptions.CenterAndExpand,
                Aspect = Aspect.AspectFit
            });
            (addBtn.Children[0] as StackLayout).Children.Add(new Label
            {
                Text = "ADD",
                TextColor = (Color)Application.Current.Resources["Orange"],
                VerticalOptions = LayoutOptions.CenterAndExpand,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label))
            });

            contentLayout.Children.Add(addBtn);

            for (int i = 0; i < n_of_eclasses; i++)
            {
                AddExperimentalClass();
            }
        }
        void AddExperimentalClass()
        {

            StackLayout experimentalClass = new StackLayout()
            {
                Orientation = StackOrientation.Horizontal,
                BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                Padding = 14
            };

            experimentalClass.Children.Add(new Label
            {
                Text = "Aula experimental",
                TextColor = BackgroundColor = (Color)Application.Current.Resources["Orange"],
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                HorizontalOptions = LayoutOptions.StartAndExpand,
                VerticalOptions = LayoutOptions.Center
            });

            Image excludeImage = new Image
            {
                Source = "ic_plus_accent.png",
                Rotation = 45
            };
            TapGestureRecognizer tapExclude = new TapGestureRecognizer();
            tapExclude.Tapped += async (sender, e) =>
            {
                if (await DisplayAlert("Aula experimental", "Deseja remover uma aula experimental as " + selectionTime + "?", "Sim", "Não"))
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                    if (await AdmUtilities.ChangeExperimentalClass(times, weekday, times.Date + "/" + times.Time + "/" + times.Type, 500 + (n_of_eclasses - 1), 1))
                    {
                        contentLayout.Children.Clear();
                        if (Device.RuntimePlatform == Device.iOS)
                            times.StudentsList.Remove(500 + (n_of_eclasses - 1));
                        GenerateView(times);

                        await DisplayAlert("Sucesso!", "A aula experimental foi removida com sucesso.", "Ok");
                    }
                    else
                    {
                        await DisplayAlert("Erro", "Não foi possível remover a aula experimental, verifique sua conexão e tente novamente.", "Ok");
                    }
                    await PopupNavigation.Instance.PopAsync();
                }
            };
            tapExclude.NumberOfTapsRequired = 1;
            excludeImage.GestureRecognizers.Add(tapExclude);
            experimentalClass.Children.Add(excludeImage);

            var divider = new BoxView { BackgroundColor = (Color)Application.Current.Resources["LightTransparent"], HorizontalOptions = LayoutOptions.Fill, HeightRequest = 1 };
            contentLayout.Children.Insert(contentLayout.Children.Count - 1, experimentalClass);
            contentLayout.Children.Insert(contentLayout.Children.Count - 1, divider);

            experimentalClasses.Add(experimentalClass);

            /*if (contentLayout.Children.Count > maxSize * 2)
                addBtn.IsVisible = false;*/
        }
    }
}