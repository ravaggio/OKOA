using Plugin.Media;
using Plugin.Media.Abstractions;


using System;
using System.Collections.Generic;

using static ctf_final.AppController;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using ctf_final.Models;
using Rg.Plugins.Popup.Services;
using System.Globalization;
using System.Linq;

namespace ctf_final.AdmContents.Review
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ResearchCadastre : ContentPage
    {
        /// <summary>
        /// Student cadastree form. Can be used for updating user data if "u" is given.
        /// </summary>
        /// <param name="u"></param>
        Button finishBtn = null;
        Button addBtn = null;
        List<Grid> questionsGrids = new List<Grid>();

        StackLayout questionsLayout = null;
        Entry titleEntry = null;

        public ResearchCadastre()
        {
            InitializeComponent();

            mainLayout.BackgroundColor = (Color)_app.Resources["DarkTransparent"];

            titleEntry = new Entry
            {
                Placeholder = "Título da pesquisa"
            };
            if (Device.RuntimePlatform == Device.iOS)
                titleEntry.BackgroundColor = (Color)_app.Resources["PrimaryDark"];
            mainLayout.Children.Add(titleEntry);


            questionsLayout = new StackLayout
            {
                Padding = 0,
                Spacing = 5
            };
            mainLayout.Children.Add(questionsLayout);
            questionsLayout.Children.Add(GetQuestionGrid());

            addBtn = new Button
            {
                Text = "ADD +",
                BackgroundColor = (Color)_app.Resources["Orange"],
                TextColor = (Color)_app.Resources["TextDark"]
            };
            addBtn.Clicked += (sender, e) =>
            {
                questionsLayout.Children.Add(GetQuestionGrid());
             
                if (questionsGrids.Count >= 3) 
                { 
                    addBtn.IsEnabled = false;
                    addBtn.IsVisible = false;
                }
            };
            mainLayout.Children.Add(addBtn);

            finishBtn = new Button
            {
                Text = "Finalizar",
                BackgroundColor = (Color)_app.Resources["Orange"],
                TextColor = (Color)_app.Resources["TextDark"]
            };
            finishBtn.Clicked += (sender, e) => RegisterEvent(sender, e);
            mainLayout.Children.Add(finishBtn);
        }

        public Questionnaire GetQuestionnaireFromEntries()
        {
            try
            {                               
                if (string.IsNullOrEmpty(titleEntry.Text))
                {
                    return null;
                }
                else
                {
                    Question[] questions = new Question[3] { null, null, null };
                    var x = 0;
                    foreach (Grid qGrid in questionsGrids)
                    {
                        var title = (qGrid.Children[0] as Entry).Text;
                        var desc = (qGrid.Children[1] as Entry).Text;
                        var type = (qGrid.Children[2] as Picker).SelectedItem.ToString();

                        switch (type)
                        {
                            case "Sim/Não": type = "yes-no"; break;
                            case "Quantitativo": type = "quantitative"; break;
                            case "Estrelas": type = "stars"; break;
                        }

                        if(!string.IsNullOrEmpty(title))
                        {
                            questions[x] = new Question
                            {
                                QuestionID = x,
                                Title = title,
                                Type = type,
                                Description = desc,
                                ReplyList = new List<Reply>()
                            };
                        }
                        else
                        {
                            return null;
                        }
                        x++;
                    }


                    var id = GenerateID();
                    var q = new Questionnaire
                    {
                        CreationDate = SharedUtilities.GetTodayDateTime().ToString("dd/MM/yyyy"),
                        QuestionnaireTitle = titleEntry.Text,
                        QuestionnaireID = id,
                        Q1 = questions[0],
                        Q2 = questions[1],
                        Q3 = questions[2],
                        ReplyIDs = new List<int>(),
                        Closed = 0,
                    };

                    return q;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("." + e);
                return null;
            }
        }
        public bool IsQuestionnaireValid(Questionnaire q)
        {
            if(q == null)
            {
                DisplayAlert("Erro", "Algum campo obrigatório não foi preenchido!", "OK");
            }
            return q != null ? true : false;
        }
        private int GenerateID()
        {
            var id = 0;
            bool z = true;
            while (z)
            {
                id++;
                z = _app.QuestionnaireList.Any(q => q.QuestionnaireID == id);
            }

            return id;
        }

        private Grid GetQuestionGrid()
        {
            var grid = new Grid()
            {
                HorizontalOptions = LayoutOptions.Fill,
                BackgroundColor = (Color) _app.Resources["DarkTransparent"],
                Margin = new Thickness(15, 0),
                Padding = new Thickness(5)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(9, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var titleEntry = new Entry()
            {
                HorizontalOptions = LayoutOptions.Fill,
                ClassId = "title",
                Placeholder = "Pergunta"
            };
            grid.Children.Add(titleEntry);
            var descEntry = new Entry()
            {
                HorizontalOptions = LayoutOptions.Fill,
                ClassId = "desc",
                Placeholder = "Detalhes (opcional)"
            }; 
            grid.Children.Add(descEntry, 0, 1);
            var typePicker = new Picker()
            {
                HorizontalOptions = LayoutOptions.Fill,
                ClassId = "type",
            };
            typePicker.Items.Add("Sim/Não");
            typePicker.Items.Add("Quantitativo");
            typePicker.Items.Add("Estrelas");
            typePicker.SelectedIndex = 0;
            grid.Children.Add(typePicker, 0, 2);

            if (Device.RuntimePlatform == Device.iOS)
            {
                titleEntry.BackgroundColor = (Color)_app.Resources["PrimaryDark"];
                descEntry.BackgroundColor = (Color)_app.Resources["PrimaryDark"];
                typePicker.BackgroundColor = (Color)_app.Resources["PrimaryDark"];
            }

            var image = new Image
            {
                Source = "ic_close.png"
            };
            var removeBtn = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
            removeBtn.Tapped += RemoveBtn_Tapped;
            image.GestureRecognizers.Add(removeBtn);
            grid.Children.Add(image, 1, 0);

            questionsGrids.Add(grid);
            return grid;
        }

        private async void RemoveBtn_Tapped(object sender, EventArgs e)
        {
            if (questionsGrids.Count > 1)
            {
                if (await DisplayAlert("Remover?", "Deseja remover esta pergunta?", "Sim", "Não"))
                {
                    var grid = (sender as Image).Parent as Grid;

                    questionsLayout.Children.Remove(grid);
                    questionsGrids.Remove(grid);

                    if (questionsGrids.Count < 3 && !addBtn.IsVisible)
                    {
                        addBtn.IsEnabled = true;
                        addBtn.IsVisible = true;
                    }
                }
            }
        }

        private async void RegisterEvent(object sender, EventArgs ev)
        {
            finishBtn.IsEnabled = false;

            try
            {
                var newQuestionnaire = GetQuestionnaireFromEntries();

                if (!IsQuestionnaireValid(newQuestionnaire))
                {
                    finishBtn.IsEnabled = true;
                    return;
                }

                if (await DisplayAlert("Finalizar?", "Deseja criar a pesquisa? (Não é possível altera-lá mais tarde)", "Sim", "Não"))
                {
                    if (await AdmUtilities.CreateQuestionnaire(newQuestionnaire))
                    {
                        await DisplayAlert("Sucesso!", "Pesquisa cadastrada com sucesso!", "OK");
                        MessagingCenter.Send(new PageControlMessage() { Command = "LoadReviewPage" }, "LoadPage");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Erro desconhecido", "Não foi possivel cadastrar o evento.", "OK");
                    }
                }
            }
            catch (Exception exc)
            {
                await DisplayAlert("Erro desconhecido", "Incapaz de cadastrar o evento. Se o erro persistir, contate o desenvolvedor:  \n" + exc, "OK");
            }
            finishBtn.IsEnabled = true;
        }
    }
}