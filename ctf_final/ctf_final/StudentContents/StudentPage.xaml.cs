using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Plugin.CloudFirestore;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;

using static ctf_final.AppController;

namespace ctf_final.StudentContents
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StudentPage : MasterDetailPage
    {
        private const string MENU_LABEL_PERFIL = "MEU PERFIL";
        private const string MENU_LABEL_PLANO = "MEU PLANO";
        private const string MENU_LABEL_AVALIACOES = "AVALIAÇÕES";
        private const string MENU_LABEL_EVENTOS = "EVENTOS";
        private const string MENU_LABEL_INICIO = "INÍCIO";
        private const string MENU_LABEL_SAIR = "SAIR";

        static readonly MenuItem MENU_PERFIL = new MenuItem()
        {
            IconSource = "ic_classes.png",
            Label = MENU_LABEL_PERFIL
        };
        static readonly MenuItem MENU_PLANO = new MenuItem()
        {
            IconSource = "ic_students.png",
            Label = MENU_LABEL_PLANO
        };
        static readonly MenuItem MENU_AVALIACOES = new MenuItem()
        {
            IconSource = "ic_schedule.png",
            Label = MENU_LABEL_AVALIACOES
        };
        static readonly MenuItem MENU_EVENTOS = new MenuItem()
        {
            IconSource = "ic_events.png",
            Label = MENU_LABEL_EVENTOS
        };
        static readonly MenuItem MENU_INICIO = new MenuItem()
        {
            IconSource = "ic_main.png",
            Label = MENU_LABEL_INICIO
        };
        static readonly MenuItem MENU_SAIR = new MenuItem()
        {
            IconSource = "ic_exit.png",
            Label = MENU_LABEL_SAIR
        };

        public class MenuItem
        {
            public string IconSource { get; set; }
            public string Label { get; set; }
        }

        private ObservableCollection<MenuItem> Menu;

        public StudentPage(bool checkExpiry = false)
        {
            InitializeComponent();

            if (checkExpiry)
            {
                if (!UserUtilities.CheckExpiryDates())
                {
                    Task.Run(async () =>
                    {
                        await UserUtilities.LockUserPlan(
                            _app.LoggedInUser, 
                            CrossCloudFirestore.Current
                                .Instance
                                .Collection("users")
                                .Document(_app.LoggedInUser.UserID.ToString())
                        );
                    });

                    _app.LoggedInUser.PlanAbscence = 1;
                    _app.LoggedInUser = _app.LoggedInUser;
                }
            }

            var str = _app.LoggedInUser.PictureToken;

            menuHeader.BindingContext = _app.LoggedInUser;
            Detail = new NavigationPage(new StudentContent())
            {
                BarBackgroundColor = Color.FromHex(Application.Current.Resources["PrimaryDark"].ToString()),
                BarTextColor = Color.FromHex(Application.Current.Resources["Orange"].ToString())
            };

            Menu = new ObservableCollection<MenuItem>()
            {
                MENU_PERFIL,
                MENU_EVENTOS,
                MENU_PLANO,
                MENU_AVALIACOES,
                MENU_SAIR
            };
            menuList.ItemsSource = Menu;

            MessagingCenter.Subscribe<PageControlMessage>(this, "UserUpdate", msg =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (msg.Command == "success")
                    {
                        var s = _app.LoggedInUser.PictureToken;
                        string picToken = s == "" ? SharedUtilities.DefaultPictureToken : s;
                        ProfilePic.Source = picToken;
                    }
                });
            });
        }

        private void MenuList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            IsPresented = false;
            string l = (e.Item as MenuItem).Label;

            switch (l)
            {
                case MENU_LABEL_PERFIL:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadProfile" }, "LoadSPage");
                    break;
                case MENU_LABEL_PLANO:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadPlan" }, "LoadSPage");
                    break;
                case MENU_LABEL_EVENTOS:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadEvents" }, "LoadSPage");
                    break;
                case MENU_LABEL_AVALIACOES:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadRating" }, "LoadSPage");
                    break;
                case MENU_LABEL_INICIO:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadStartPage" }, "LoadSPage");
                    break;
                case MENU_LABEL_SAIR:
                    UserUtilities.Loggout();

                    _app.LoggedInUser = new User();
                    _app.SavePropertiesAsync();

                    Application.Current.MainPage = new Login();
                    return;
            }
            EditMenu(l);
        }

        void EditMenu(string label)
        {
            //reset menu
            Menu.Clear();
            Menu.Add(MENU_PERFIL);
            Menu.Add(MENU_EVENTOS);
            Menu.Add(MENU_PLANO);
            Menu.Add(MENU_AVALIACOES);
            Menu.Add(MENU_SAIR);

            //edit menu
            if (!label.Equals(MENU_LABEL_INICIO))
            {
                Menu.Remove(Menu.Single(item => item.Label == label));
                Menu.Insert(0, MENU_INICIO);
            }
        }
    }
}