using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ctf_final.Models;
using static ctf_final.AppController;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Rg.Plugins.Popup.Services;

namespace ctf_final.AdmContents.Review
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReviewDetails : ContentPage
    {
        Questionnaire questionnaire = null;

        public ReviewDetails(Questionnaire q)
        {
            InitializeComponent();

            try
            {
            questionnaire = q;
            var header = new StackLayout
            {
                Spacing = 0,
                Padding = 10,
                BackgroundColor = BackgroundColor = (Color)Application.Current.Resources["PrimaryLight"]
            };

            var title = new Label
            {
                Text = q.QuestionnaireTitle + " - " + q.CreationDate,
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                TextColor = (Color)_app.Resources["Orange"]
            };
            header.Children.Add(title);
            var count = new Label
            {
                Text = "Respostas: " + q.ReplyIDs.Count() + "/" + _app.UsersResume.Users.Count(),
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                TextColor = (Color)_app.Resources["TextLight"]
            };
            header.Children.Add(count);
            detailLayout.Children.Add(header);

            var qList = new List<Question> { questionnaire.Q1, questionnaire.Q2, questionnaire.Q3 };
            foreach (Question quest in qList)
                if(quest != null)
                    detailLayout.Children.Add(GetQuestionView(quest));

            var redBtn = new Button
            {
                Text = questionnaire.Closed == 0 ? "Terminar pesquisa" : "Remover",
                BackgroundColor = (Color)_app.Resources["Red"],
                TextColor = (Color)_app.Resources["TextDark"],
                Margin = new Thickness(10, 0)
            };
            redBtn.Clicked += async (sender, e) =>
            {
                if(questionnaire.Closed == 0)
                {
                    if (await DisplayAlert("Remover", "Deseja terminar essa pesquisa?", "Sim", "Não"))
                    {
                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                        if (await AdmUtilities.CloseQuestionnaire(questionnaire))
                        {
                            (sender as Button).Text = "Remover";
                            await DisplayAlert("Sucesso", "Pesquisa terminada com sucesso!", "Ok");
                        }
                        else
                        {
                            await DisplayAlert("Erro", "Não foi possível terminar a pesquisa, tente novamente mais tarde", "Ok");
                        }
                        await PopupNavigation.Instance.PopAsync();
                    }
                }
                else
                {
                    if (await DisplayAlert("Remover", "Deseja remover essa pesquisa do servidor?", "Sim", "Não"))
                    {
                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                        if (await AdmUtilities.RemoveQuestionnaire(questionnaire))
                        {
                            MessagingCenter.Send(new PageControlMessage() { Command = "LoadReviewPage" }, "LoadPage");
                            await Navigation.PopAsync();
                            await DisplayAlert("Sucesso", "Pesquisa removida com sucesso!", "Ok");
                        }
                        else
                        {
                            await DisplayAlert("Erro", "Não foi possível remover a pesquisa, tente novamente mais tarde", "Ok");
                        }
                        await PopupNavigation.Instance.PopAsync();
                    }
                }
            };
            detailLayout.Children.Add(redBtn);

            }
            catch (Exception e) { Console.WriteLine(e); }

        }

        public Grid GetQuestionView(Question question)
        {
            var grid = new Grid
            {
                BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                RowSpacing = 5,
                Margin = 0,
                Padding = 0
            };


            var title = new Label
            {
                Text = question.Title,
                HorizontalOptions = LayoutOptions.Center,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color)_app.Resources["TextLight"]
            };
            grid.Children.Add(title);
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            var pos = 1;
            var spam = 1;

            Label desc = null;
            if(!string.IsNullOrEmpty(question.Description))
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                desc = new Label
                {
                    Text = question.Description,
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    TextColor = (Color)_app.Resources["LightTransparent"]
                };
                grid.Children.Add(desc, 0, 1);

                pos = 2;
            }

            if(question.ReplyList.Count > 0)
                switch (question.Type)
                {
                    case "yes-no":
                        var barGrid = new Grid
                        {
                            Margin = new Thickness(15, 0),
                            Padding = 1,
                            BackgroundColor = (Color) _app.Resources["Orange"],
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            ColumnSpacing = 0
                        };

                        var yesCount = question.ReplyList.Where(qu => qu.Answer == "yes").Count();
                        var noCount = question.ReplyList.Where(qu => qu.Answer == "no").Count();

                        barGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                        barGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(yesCount, GridUnitType.Star) });
                        barGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(noCount, GridUnitType.Star) });

                        barGrid.Children.Add(new BoxView 
                        { 
                        });
                        barGrid.Children.Add(new Label
                        {
                            Text = "S",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                            TextColor = (Color) _app.Resources["TextLight"]
                        });

                        barGrid.Children.Add(new BoxView
                        {
                            BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                        }, 1, 0);
                        barGrid.Children.Add(new Label
                        {
                            Text = "N",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                            TextColor = (Color)_app.Resources["TextLight"]
                        }, 1, 0);

                        grid.Children.Add(barGrid, 0, 2);
                        grid.Children.Add(new Label
                        {
                            Text = "SIM: " + yesCount,
                            HorizontalOptions = LayoutOptions.Center,
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                            TextColor = (Color)_app.Resources["TextLight"]
                        }, 0, 3); 
                        grid.Children.Add(new Label
                        {
                            Text = "NÃO: " + noCount,
                            HorizontalOptions = LayoutOptions.Center,
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                            TextColor = (Color)_app.Resources["TextLight"]
                        }, 0, 4);

                    
                        pos = 5;
                        spam = 1;

                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        break;
                    case "quantitative":
                        Grid.SetColumnSpan(title, 3);
                        if (!string.IsNullOrEmpty(question.Description))
                            Grid.SetColumnSpan(desc, 3);

                        var quantGrid = new Grid
                        {
                            Margin = new Thickness(15, 0),
                            Padding = 1,
                            BackgroundColor = (Color)_app.Resources["Orange"],
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            ColumnSpacing = 1
                        };

                        var vgoodCount = question.ReplyList.Where(qu => qu.Answer == "very_good").Count();
                        var goodCount = question.ReplyList.Where(qu => qu.Answer == "good").Count();
                        var badCount = question.ReplyList.Where(qu => qu.Answer == "bad").Count();
                        var vbadCount = question.ReplyList.Where(qu => qu.Answer == "very_bad").Count();

                        quantGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                        quantGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(vgoodCount, GridUnitType.Star) });
                        quantGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(goodCount, GridUnitType.Star) });
                        quantGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(badCount, GridUnitType.Star) });
                        quantGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(vbadCount, GridUnitType.Star) });

                        var countArray = new int[4]
                        {
                            vgoodCount,
                            goodCount,
                            badCount,
                            vbadCount
                        };
                        var labelsText = new String[4]
                        {
                            "MUITO BOM",
                            "BOM",
                            "RUIM",
                            "MUITO RUIM"
                        };
                        var small_labelsText = new String[4]
                        {
                            "MB",
                            "B",
                            "R",
                            "MR"
                        };
                        for (int i = 0; i < 4; i++)
                        {
                            quantGrid.Children.Add(new BoxView
                            {
                                BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                            }, i, 0);

                            quantGrid.Children.Add(new Label
                            {
                                Text = small_labelsText[i],
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                TextColor = (Color)_app.Resources["TextLight"]
                            }, i, 0);

                            grid.Children.Add(new Label
                            {
                                Text = labelsText[i],
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                TextColor = (Color)_app.Resources["TextLight"]
                            }, 0, 4 + i);
                            grid.Children.Add(new Label
                            {
                                Text = countArray[i].ToString(),
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                TextColor = (Color)_app.Resources["TextLight"]
                            }, 1, 4 + i);
                            grid.Children.Add(new Label
                            {
                                Text = Math.Round((double) 100/question.ReplyList.Count() * countArray[i], 1) + " %",
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                TextColor = (Color)_app.Resources["TextLight"]
                            }, 2, 4 + i);
                        }

                        grid.Children.Add(quantGrid, 0, 3);
                        Grid.SetColumnSpan(quantGrid, 3);

                        pos = 8;
                        spam = 3;

                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        break;
                    case "stars":
                        Grid.SetColumnSpan(title, 3);
                        if (!string.IsNullOrEmpty(question.Description))
                            Grid.SetColumnSpan(desc, 3);

                        var starsGrid = new Grid
                        {
                            Margin = new Thickness(15, 0),
                            Padding = 1,
                            BackgroundColor = (Color)_app.Resources["Orange"],
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        };

                        var countPerNumber = new int[5] { 0, 0, 0, 0, 0 };
                        Double sum = 0;
                        foreach (var r in question.ReplyList)
                        {
                            var n = Convert.ToInt32(r.Answer);
                            countPerNumber[n-1]++;
                            sum += n;
                        }
                        var m = Math.Round(sum / question.ReplyList.Count(), 1);

                        starsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(m, GridUnitType.Star) });
                        starsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5 - m, GridUnitType.Star) });

                        starsGrid.Children.Add(new BoxView
                        {
                            BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                        }, 1, 0);

                        var med =  new Label
                        {
                            Text = m.ToString(),
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                            TextColor = (Color)_app.Resources["TextLight"]
                        };
                        starsGrid.Children.Add(med);
                        Grid.SetColumnSpan(med, 2);
                        grid.Children.Add(starsGrid, 0, 3);
                        Grid.SetColumnSpan(starsGrid, 3);

                        for (int x = 0; x < 5; x++)
                        {
                            var starsLabel = "";
                            while (starsLabel.Length < x + 1)
                                starsLabel = starsLabel + "★";

                            grid.Children.Add(new Label
                            {
                                Text = starsLabel,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                TextColor = (Color)_app.Resources["TextLight"]
                            }, 0, 4 + x);
                            grid.Children.Add(new Label
                            {
                                Text = countPerNumber[x].ToString(),
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                TextColor = (Color)_app.Resources["TextLight"]
                            }, 1, 4 + x); 
                            grid.Children.Add(new Label
                            {
                                Text = Math.Round((double)100/question.ReplyList.Count() * countPerNumber[x], 1) + " %",
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                                TextColor = (Color)_app.Resources["TextLight"]
                            }, 2, 4 + x);
                        }

                        pos = 9;
                        spam = 3;

                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                        break;
                }

            var divider = new BoxView
            {
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(10, 0),
                HeightRequest = 1,
                BackgroundColor = (Color)_app.Resources["LightTransparent"]
            };
            grid.Children.Add(divider, 0, pos);
            Grid.SetColumnSpan(divider, spam);

            return grid;
        }
    }
}