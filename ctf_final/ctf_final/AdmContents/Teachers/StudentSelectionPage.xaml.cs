using ctf_final.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using static ctf_final.AppController;

namespace ctf_final.AdmContents.Students
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StudentsSelectionPage : ContentPage
    {
        private string selectionUsage;
        private ObservableCollection<SimplifiedUser> users;
        private List<SimplifiedUser> base_user_list;
        private Label emptyLabel = new Label()
        {
            Text = "Nenhum aluno encontrado...",
            TextColor = Color.FromHex("#de4905"),
            Margin = new Thickness(0, 10, 0, 10),
            FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
            HorizontalOptions = LayoutOptions.CenterAndExpand
        };

        List<int> removalIDs;
        public StudentsSelectionPage(string usage, List<int> ids = null)
        {
            InitializeComponent();

            selectionUsage = usage;
            base_user_list = new List<SimplifiedUser>();

            removalIDs = ids;

            SetList();
            CheckIfListIsEmpty();

            MessagingCenter.Subscribe<PageUpdateMessage>(this, "UpdateStudentSelectionPage", msg =>
            {
                Device.BeginInvokeOnMainThread(() => SetList());
            });
        }

        void SetList()
        {
            try
            {
                base_user_list = _app.UsersResume.Users;

                if (removalIDs != null)
                    foreach (var id in removalIDs)
                    {
                        try { base_user_list.Remove(base_user_list.Find(u => u.UserID == id)); } catch { }
                    }
                        
                base_user_list = base_user_list.OrderBy(u => u.Name).ToList();
                users = new ObservableCollection<SimplifiedUser>(base_user_list);

                studentsList.ItemsSource = users;

                searchBar.Text = "";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            users.Clear();
            foreach (SimplifiedUser user in base_user_list.FindAll(u => u.Name.ToLower().StartsWith(e.NewTextValue.ToLower())))
            {
                users.Add(user);
            }
            CheckIfListIsEmpty();
        }
        private void CheckIfListIsEmpty()
        {
            if (users != null)
            {
                if (users.Count < 1)
                {
                    if (!viewLayout.Children.Contains(emptyLabel))
                    {
                        viewLayout.Children.Insert(1, emptyLabel);
                    }
                }
                else
                {
                    if (viewLayout.Children.Contains(emptyLabel))
                    {
                        viewLayout.Children.Remove(emptyLabel);
                    }
                }
            }
        }

        private TaskCompletionSource<bool> _TaskCompletion;
        private int selectedID;
        public async Task<int> GetUserID()
        {
            _TaskCompletion = new TaskCompletionSource<bool>();
            if (await _TaskCompletion.Task)
            {
                await Navigation.PopAsync();
                return selectedID;
            }
            else
            {
                await Navigation.PopAsync();
                return -1;
            }
        }

        private async void OnStudentSelected(object sender, ItemTappedEventArgs e)
        {
            var simplifiedUser = e.Item as SimplifiedUser;
            try
            {
                if (selectionUsage.Equals("rate"))
                    await Navigation.PushAsync(new RatingPage(simplifiedUser));
                else if (selectionUsage.Equals("manage"))
                    await Navigation.PushAsync(new StudentInformationPage(simplifiedUser));
                else if (selectionUsage.Equals("add_to_classes"))
                {
                    if(_TaskCompletion != null)
                    {
                        selectedID = simplifiedUser.UserID;
                        _TaskCompletion.TrySetResult(true);

                        _TaskCompletion = null;
                    }   
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_TaskCompletion != null)
            {
                _TaskCompletion.TrySetResult(false);

                _TaskCompletion = null;
            }
        }
    }
}