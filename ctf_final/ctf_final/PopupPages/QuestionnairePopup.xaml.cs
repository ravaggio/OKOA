using ctf_final.Models;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using static ctf_final.AppController;

namespace ctf_final.PopupPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class QuestionnairePopup : PopupPage
    {   
        Dictionary<string, List<BoxView>> starsBoxes = new Dictionary<string, List<BoxView>>();
        Dictionary<string, List<BoxView>> yesnoBoxes = new Dictionary<string, List<BoxView>>();

        string[] answers = new string[3] { null, null, null };

        Questionnaire q = null;
        int questionCount = 0;

        public QuestionnairePopup(Questionnaire questionnaire)
        {
            InitializeComponent();
            q = questionnaire;
            MainLayout.Children.Add(new Label
            {
                Text = questionnaire.QuestionnaireTitle,
                TextColor = (Color)_app.Resources["Orange"],
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Margin = 15,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            });

            var qList = new List<Question> { questionnaire.Q1, questionnaire.Q2, questionnaire.Q3 };
            foreach (Question q in qList)
                if(q != null)
                {
                    GenerateQuestionLayout(q);
                    questionCount++;
                }

            var confirmBtn = new Button
            {
                Text = "Confirmar",
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0, 5, 0, 0),
                TextColor = (Color)_app.Resources["TextDark"],
                BackgroundColor = (Color)_app.Resources["Orange"]
            };
            confirmBtn.Clicked += ConfirmBtn_Clicked;
            MainLayout.Children.Add(confirmBtn);
        }

        public void GenerateQuestionLayout(Question question)
        {
            var qLayout = new StackLayout
            {
                Spacing = 5,
                ClassId = question.QuestionID.ToString(),
                Margin = new Thickness(10, 5)
            };

            qLayout.Children.Add(new Label
            {
                Text = question.Title,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                TextColor = (Color) _app.Resources["TextLight"]
            });

            if(!string.IsNullOrEmpty(question.Description))
                qLayout.Children.Add(new Label
                {
                    Text = question.Description,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                    TextColor = (Color)_app.Resources["TextLight"]
                });

            if (question.Type == "yes-no")
            {
                var btnGrid = new Grid
                {
                    ColumnSpacing = 0,
                    ClassId = question.QuestionID.ToString(),
                    HorizontalOptions = LayoutOptions.Fill,
                    Margin = new Thickness(15, 0)
                };

                var boxes = new List<BoxView>();
                var stringList = new string[2] { "Não", "Sim" };
                for(int i = 0; i < 2; i++)
                {
                    var btn = new BoxView
                    {
                        ClassId = i.ToString(),
                        BackgroundColor = (Color)_app.Resources["DarkTransparent"]
                    };
                    var tap = new TapGestureRecognizer();
                    tap.NumberOfTapsRequired = 1;
                    tap.Tapped += (sender, e) =>
                    {
                        try
                        {
                            var bv = sender as BoxView;
                            var id = Convert.ToInt32(bv.ClassId);

                            yesnoBoxes[bv.Parent.ClassId][id].BackgroundColor = (Color)_app.Resources["Orange"];
                            yesnoBoxes[bv.Parent.ClassId][id == 0 ? 1 : 0].BackgroundColor = (Color)_app.Resources["DarkTransparent"];

                            answers[Convert.ToInt32(bv.Parent.Parent.ClassId)] = id == 0 ? "no" : "yes";
                        }
                        catch (Exception ex) { Console.WriteLine(ex); }
                    };
                    btn.GestureRecognizers.Add(tap);


                    btnGrid.Children.Add(btn, i, 0);
                    btnGrid.Children.Add(new Label
                    {
                        Text = stringList[i],
                        TextColor = (Color)_app.Resources["TextLight"],
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        InputTransparent = true
                    }, i, 0);

                    boxes.Add(btn);
                }
                yesnoBoxes.Add(question.QuestionID.ToString(), boxes);

                qLayout.Children.Add(btnGrid);
            }
            else if (question.Type == "quantitative") 
            {
                var picker = new Picker
                {
                    TextColor = (Color) _app.Resources["Orange"],
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                };
                picker.Items.Add("Muito Bom");
                picker.Items.Add("Bom");
                picker.Items.Add("Ruim");
                picker.Items.Add("Muito Ruim");

                picker.SelectedIndex = 0;
                if (Device.RuntimePlatform == Device.iOS)
                    picker.BackgroundColor = (Color)_app.Resources["PrimaryDark"];

                answers[question.QuestionID] = "very_good";
                picker.SelectedIndexChanged += (sender, e) =>
                {
                    var p = sender as Picker;
                    var id = Convert.ToInt32(p.Parent.ClassId);
                    switch (p.SelectedItem) 
                    {
                        case "Muito Bom":
                            answers[id] = "very_good";
                            break;
                        case "Bom":
                            answers[id] = "good";
                            break;
                        case "Ruim":
                            answers[id] = "bad";
                            break;
                        case "Muito Ruim":
                            answers[id] = "very_bad";
                            break;
                    };
                };

                qLayout.Children.Add(picker);
            }
            else if(question.Type == "stars") 
            {

                var starsGrid = new Grid
                {
                    ColumnSpacing = 5,
                    ClassId = question.QuestionID.ToString(),
                    Padding = 1,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    BackgroundColor = (Color)_app.Resources["PrimaryDark"]
                };

                var boxes = new List<BoxView>();
                for (int i = 1; i <= 5; i++)
                {
                    starsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                    var bv = new BoxView
                    {
                        ClassId = i.ToString(),
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        BackgroundColor = (Color) _app.Resources["LightTransparent"]
                    };
                    var btnTap = new TapGestureRecognizer { NumberOfTapsRequired = 1 };
                    btnTap.Tapped += (sender, e) =>
                    {
                        var view = sender as BoxView;
                        var key = view.Parent.ClassId;
                        foreach (var sb in starsBoxes[key])
                            sb.BackgroundColor = (Color)_app.Resources["LightTransparent"];

                        for(int x = 0; x <= Convert.ToInt32(view.ClassId) - 1; x++)
                            starsBoxes[key][x].BackgroundColor = (Color)_app.Resources["Orange"];

                        answers[Convert.ToInt32(view.Parent.Parent.ClassId)] = view.ClassId;
                    };
                    bv.GestureRecognizers.Add(btnTap);
                    boxes.Add(bv);
                    starsGrid.Children.Add(bv, i - 1, 0);

                    starsGrid.Children.Add(new Image
                    {
                        Source = "ic_star.png",
                        InputTransparent = true,
                        VerticalOptions = LayoutOptions.Fill,
                        HorizontalOptions = LayoutOptions.Fill,
                        Aspect = Aspect.AspectFit
                    }, i - 1, 0);

                    qLayout.Children.Add(starsGrid);
                }
                starsBoxes.Add(question.QuestionID.ToString(), boxes);
            }

            MainLayout.Children.Add(qLayout);
        }

        private async void ConfirmBtn_Clicked(object sender, EventArgs e)
        {
            var responses = 0;
            foreach (var a in answers)
                if (a != null)
                    responses++;
            if(responses < questionCount)
            {
                await DisplayAlert("Erro", "Você precisa responder todas as perguntas para confirmar!", "Ok");
                return;
            }

            if (await DisplayAlert("Enviar", "Completar a pesquisa?", "Sim", "Não"))
            {
                await PopupNavigation.Instance.PushAsync(new LoadingPopup());

                try
                {
                    var doc = CrossCloudFirestore.Current.Instance
                                                .Collection("questionnaires")
                                                .Document(q.QuestionnaireID.ToString());

                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    batch.Update(doc, "ReplyIDs", FieldValue.ArrayUnion(_app.LoggedInUser.UserID));
                    for (int x = 1; x <= 3; x++)
                        if (answers[x - 1] != null)
                            batch.Update(doc, new FieldPath("Q" + x, "ReplyList"), FieldValue.ArrayUnion(new Reply { UserID = _app.LoggedInUser.UserID, Answer = answers[x - 1] }));

                    await batch.CommitAsync();

                    _app.QuestionnaireList.Find(que => que.QuestionnaireID == q.QuestionnaireID).ReplyIDs.Add(_app.LoggedInUser.UserID);
                    _app.QuestionnaireList = _app.QuestionnaireList;

                    await DisplayAlert("Sucesso", "Obrigado pela colaboração!", "Ok");
                }
                catch
                {
                    await DisplayAlert("Erro", "Não foi possível enviar suas respostas, tente novamente mais tarde", "Ok");
                }
                
                await PopupNavigation.Instance.PopAsync();
                await PopupNavigation.Instance.PopAsync();
            }
        }
    }
}