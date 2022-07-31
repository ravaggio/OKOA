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
    public partial class StudentRatings : ContentPage
    {
        readonly List<StackLayout> expandableViews = new List<StackLayout>();
        readonly User user;
        public StudentRatings(User u)
        {
            InitializeComponent();
            user = u;

            Title = "Avaliações";

            foreach (Rating r in u.Ratings)
            {
                var fr = new FormattedRating(r);

                Style labelStyle = new Style(typeof(Label));
                labelStyle.Setters.Add(new Setter()
                {
                    Property = Label.FontSizeProperty,
                    Value = Device.GetNamedSize(NamedSize.Title, typeof(Label))
                });
                labelStyle.Setters.Add(new Setter()
                {
                    Property = View.VerticalOptionsProperty,
                    Value = LayoutOptions.StartAndExpand
                });

                StackLayout rateLayout = new StackLayout()
                {
                    Spacing = 0,
                    BindingContext = fr,
                    Resources = new ResourceDictionary
                    {
                        labelStyle
                    }
                };

                //HEADER >>
                StackLayout header = new StackLayout()
                {
                    Padding = new Thickness(16),
                    BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"],
                    HorizontalOptions = LayoutOptions.Fill,
                    Orientation = StackOrientation.Horizontal
                };
                Label date = new Label() {
                    Text = fr.RatingDetails.Date,
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    HorizontalOptions = LayoutOptions.StartAndExpand
                };
                header.Children.Add(date);
                Image arrow = new Image()
                {
                    Source = "ic_arrow_down.png"
                };
                header.Children.Add(arrow);
                var tapExpand = new TapGestureRecognizer();
                tapExpand.Tapped += (s, ex) => {
                    Expand(s, ex);
                };
                tapExpand.NumberOfTapsRequired = 1;
                header.GestureRecognizers.Add(tapExpand);

                //DETAILS >>
                StackLayout details = new StackLayout()
                {
                    IsVisible = false,
                    HorizontalOptions = LayoutOptions.Fill,
                    Spacing = 0
                };
                //LABELS STYLE>>
                Style smallLabels = new Style(typeof(Label));
                smallLabels.Setters.Add(new Setter()
                {
                    Property = Label.FontSizeProperty,
                    Value = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
                });
                smallLabels.Setters.Add(new Setter()
                {
                    Property = Label.TextColorProperty,
                    Value = Application.Current.Resources["Orange"]
                });
                smallLabels.Setters.Add(new Setter()
                {
                    Property = View.VerticalOptionsProperty,
                    Value = LayoutOptions.CenterAndExpand
                });
                smallLabels.Setters.Add(new Setter()
                {
                    Property = View.MarginProperty,
                    Value = new Thickness(10, 0, 10, 0)
                });
                //ENTRIES STYLE>>
                Style entryStyle = new Style(typeof(Entry));
                entryStyle.Setters.Add(new Setter()
                {
                    Property = View.HorizontalOptionsProperty,
                    Value = LayoutOptions.Fill
                });
                entryStyle.Setters.Add(new Setter()
                {
                    Property = Entry.TextColorProperty,
                    Value = Application.Current.Resources["TextLight"]
                });
                entryStyle.Setters.Add(new Setter()
                {
                    Property = Entry.FontSizeProperty,
                    Value = Device.GetNamedSize(NamedSize.Medium, typeof(Entry))
                });
                entryStyle.Setters.Add(new Setter()
                {
                    Property = View.MarginProperty,
                    Value = new Thickness(10, 0, 10, 0)
                });

                if(Device.RuntimePlatform == Device.iOS)
                    entryStyle.Setters.Add(new Setter()
                    {
                        Property = BackgroundColorProperty,
                        Value = Color.Transparent
                    });

                Grid entries = new Grid()
                {
                    HorizontalOptions = LayoutOptions.Fill,
                    ColumnSpacing = 0,
                    RowSpacing = 0,
                    Resources = new ResourceDictionary
                    {
                        smallLabels,
                        entryStyle
                    }
                };

                entries.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                entries.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                var labelsBg = new BoxView() { BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"] };
                entries.Children.Add(labelsBg);
                Grid.SetRowSpan(labelsBg, 5);

                //LABELS >>
                entries.Children.Add(new Label() { Text = "Gordura: ", Margin = new Thickness(10, 6, 10, 0) });
                entries.Children.Add(new Label() { Text = "Massa magra: " }, 0, 1);
                entries.Children.Add(new Label() { Text = "Peso: " }, 0, 2);
                entries.Children.Add(new Label() { Text = "Altura: " }, 0, 3);
                entries.Children.Add(new Label() { Text = "Mobilidade: ", Margin = new Thickness(10, 0, 10, 6) }, 0, 4);

                //ENTRIES >>
                Entry fatEntry = new Entry() { Text = fr.RatingDetails.Fat + "%", Margin = new Thickness(10, 6, 10, 0), Keyboard = Keyboard.Numeric };
                fatEntry.TextChanged += (s, ex) => { CheckIfCanUpdate(s, ex); };
                fatEntry.Behaviors.Add(new Behaviors.DotBehaviour() { AddPercentage = true });
                entries.Children.Add(fatEntry, 1, 0);

                Entry massEntry = new Entry() { Text = fr.RatingDetails.Mass + "%", Keyboard = Keyboard.Numeric };
                massEntry.TextChanged += (s, ex) => { CheckIfCanUpdate(s, ex); };
                massEntry.Behaviors.Add(new Behaviors.DotBehaviour() { AddPercentage = true });
                entries.Children.Add(massEntry, 1, 1);

                Entry weightEntry = new Entry() { Text = fr.RatingDetails.Weight, Keyboard = Keyboard.Numeric };
                weightEntry.TextChanged += (s, ex) => { CheckIfCanUpdate(s, ex); };
                weightEntry.Behaviors.Add(new Behaviors.DotBehaviour() { AddPercentage = false });
                entries.Children.Add(weightEntry, 1, 2);

                Entry heightEntry = new Entry() { Text = fr.RatingDetails.Height, Keyboard = Keyboard.Numeric };
                heightEntry.TextChanged += (s, ex) => { CheckIfCanUpdate(s, ex); };
                heightEntry.Behaviors.Add(new Behaviors.MakedEntryBehavior() { Mask = "X.XX" });
                entries.Children.Add(heightEntry, 1, 3);

                Picker mobilityPicker = new Picker()
                {
                    ItemsSource = new string[3] { "Pouco mobilizado", "Bem mobilizado", "Hiper mobilizado" },
                    SelectedItem = fr.RatingDetails.Mobility,
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Picker)),
                    TextColor = (Color)Application.Current.Resources["TextLight"],
                    Margin = new Thickness(10, 0, 10, 6)
                };
                mobilityPicker.SelectedIndexChanged += (s, ex) => { CheckIfCanUpdate(s, ex); };
                if (Device.RuntimePlatform == Device.iOS)
                    mobilityPicker.BackgroundColor = (Color)_app.Resources["PrimaryDark"];

                entries.Children.Add(mobilityPicker, 1, 4);

                //BUTTONS>>
                Grid buttons = new Grid() { Padding = 0, RowSpacing = 0, ColumnSpacing = 0 };

                buttons.Children.Add(new BoxView { BackgroundColor = (Color)_app.Resources["Red"] });
                Button removeBtn = new Button()
                {
                    Text = "REMOVER",
                    TextColor = (Color)_app.Resources["TextDark"],
                    BackgroundColor = (Color)Application.Current.Resources["Red"]
                };
                removeBtn.Clicked += (s, ex) => { Remove(s, ex); };
                buttons.Children.Add(removeBtn);

                buttons.Children.Add(new BoxView { BackgroundColor = (Color)_app.Resources["Orange"] }, 1, 0);
                Button updateBtn = new Button()
                {
                    Text = "SALVAR",
                    TextColor = (Color)_app.Resources["TextDark"],
                    IsEnabled = false,
                    BackgroundColor = (Color)Application.Current.Resources["Orange"]
                };
                updateBtn.Clicked += (s, ex) => { Update(s, ex); };
                buttons.Children.Add(updateBtn, 1, 0);

                details.Children.Add(entries);
                details.Children.Add(buttons);

                rateLayout.Children.Add(header);
                rateLayout.Children.Add(details);

                expandableViews.Add(details);

                viewLayout.Children.Add(rateLayout);

                BoxView separator = new BoxView()
                {
                    HeightRequest = 1,
                    HorizontalOptions = LayoutOptions.Fill,
                    BackgroundColor = (Color) Application.Current.Resources["LightTransparent"]
                };
                viewLayout.Children.Add(separator);
            }

            CheckIfUserHasReview();
        }

        public void Expand(object sender, EventArgs e)
        {
            var rateLayout = (sender as StackLayout).Parent as StackLayout;
            var details = rateLayout.Children[1];
            StackLayout header = rateLayout.Children[0] as StackLayout;

            details.IsVisible ^= true;
            if(details.IsVisible)
                Task.Run(async () => { await header.Children[1].RotateTo(180, 100); });
            else
                Task.Run(async () => { await header.Children[1].RotateTo(0, 100); });

            foreach (StackLayout view in expandableViews)
            {
                if (view != details)
                {
                    view.IsVisible = false;
                    (((view.Parent as StackLayout).Children[0] as StackLayout).Children[1] as Image).Rotation = 0;
                }
            }
        }

        public async void Remove(object sender, EventArgs e)
        {
            if (await DisplayAlert("Remover avaliação", "Deseja remover esta avaliação?", "Sim", "Não"))
            {
                await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                try
                {
                    var rateLayout = (((sender as Button).Parent as Grid).Parent as StackLayout).Parent as StackLayout;
                    int i = (rateLayout.Parent as StackLayout).Children.IndexOf(rateLayout);

                    viewLayout.Children.RemoveAt(i);
                    viewLayout.Children.RemoveAt(i);

                    if (i > 0)
                        i /= 2;

                    await AdmUtilities.DeleteRating(user.UserID.ToString(), user.Ratings[i]);
                    user.Ratings.RemoveAt(i);

                    CheckIfUserHasReview();

                    await DisplayAlert("Sucesso!", "Avaliação removida com sucesso.", "Ok");
                }
                catch
                {
                    await DisplayAlert("Erro", "Não foi possível remover a avaliação, tente novamente mais tarde.", "Ok");
                }
                await PopupNavigation.Instance.PopAsync();
            }
        }

        void CheckIfUserHasReview()
        {
            if (viewLayout.Children.Count < 1)
            {
                viewLayout.Children.Add(new Label
                {
                    Text = "O aluno ainda não foi avaliado",
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = 20,
                    TextColor = (Color)Application.Current.Resources["Orange"],
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label))
                });
            }
        }

        public async void Update(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
            try
            {
                var details = ((sender as Button).Parent as Grid).Parent as StackLayout;
                var entries = details.Children[0] as Grid;

                int i = GetIndexFromDetails(details);
                var inputs = GetRatingInputsFromEntriesGrid(entries);

                Rating newRating = new Rating()
                {
                    Date = user.Ratings[i].Date,
                    Fat = inputs.Entries[0].Text.Replace("%", ""),
                    Mass = inputs.Entries[1].Text.Replace("%", ""),
                    Weight = inputs.Entries[2].Text,
                    Height = inputs.Entries[3].Text,
                    Mobility = inputs.Picker.SelectedItem.ToString(),
                };

                await AdmUtilities.UpdateRating(user.UserID.ToString(), user.Ratings[i], newRating);
                user.Ratings[i] = newRating;

                await DisplayAlert("Sucesso!", "Avaliação atualizada com sucesso.", "Ok");

                CheckIfCanUpdate(inputs.Entries[0], null);
            }
            catch
            {
                await DisplayAlert("Erro", "Não foi possível alterar a avaliação, tente novamente mais tarde.", "Ok");
            }
            await PopupNavigation.Instance.PopAsync();
        }
        public void CheckIfCanUpdate(object sender, EventArgs e)
        {
            StackLayout details;

            try {
                details = ((sender as Entry).Parent as Grid).Parent as StackLayout;
            }
            catch
            {
                details = ((sender as Picker).Parent as Grid).Parent as StackLayout;
            }

            var entries = details.Children[0] as Grid;

            int i = GetIndexFromDetails(details);
            var inputs = GetRatingInputsFromEntriesGrid(entries);

            Rating val = new Rating()
            {
                Date = user.Ratings[i].Date,
                Fat = inputs.Entries[0].Text.Replace("%", ""),
                Mass = inputs.Entries[1].Text.Replace("%", ""),
                Weight = inputs.Entries[2].Text,
                Height = inputs.Entries[3].Text,
                Mobility = inputs.Picker.SelectedItem.ToString(),
            };

            Button saveBtn = (details.Children[1] as Grid).Children[3] as Button;

            if(user.Ratings[i].Fat == val.Fat &&
               user.Ratings[i].Weight == val.Weight &&
               user.Ratings[i].Mobility == val.Mobility &&
               user.Ratings[i].Height == val.Height &&
               user.Ratings[i].Mass == val.Mass)
                saveBtn.IsEnabled = false;
            else
                saveBtn.IsEnabled = true;
        }

        int GetIndexFromDetails(StackLayout details)
        {
            int i = ((details.Parent as StackLayout).Parent as StackLayout).Children.IndexOf(details.Parent as StackLayout);
            if (i > 0)
                i /= 2;
            return i;
        }

        class RatingInputs
        {
            public List<Entry> Entries { get; set; }
            public Picker Picker { get; set; }
        }
        RatingInputs GetRatingInputsFromEntriesGrid(Grid entries)
        {
            return new RatingInputs()
            {
                Entries = new List<Entry>
                {
                    entries.Children[6] as Entry,
                    entries.Children[7] as Entry,
                    entries.Children[8] as Entry,
                    entries.Children[9] as Entry,
                },
                Picker = entries.Children[10] as Picker
            };
        }

        private async void AddRating(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RatingPage(user));
            Navigation.RemovePage(Navigation.NavigationStack[Navigation.NavigationStack.Count - 2]);
        }
    }
}