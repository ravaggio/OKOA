using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;

namespace ctf_final
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AdmPage : MasterDetailPage
    {
        private const string MENU_LABEL_AULAS = "AULAS";
        private const string MENU_LABEL_ALUNOS = "ALUNOS";
        private const string MENU_LABEL_HORARIOS = "HORÁRIOS";
        private const string MENU_LABEL_INICIO = "INÍCIO";
        private const string MENU_LABEL_PROFESSORES = "PROFESSORES";
        private const string MENU_LABEL_EVENTOS = "EVENTOS";
        private const string MENU_LABEL_REVIEWS = "PESQUISAS";
        private const string MENU_LABEL_PLANO = "PLANOS";
        private const string MENU_LABEL_SAIR = "SAIR";

        static readonly MenuItem MENU_AULAS = new MenuItem()
        {
            IconSource = "ic_classes.png",
            Label = MENU_LABEL_AULAS
        };
        static readonly MenuItem MENU_ALUNOS = new MenuItem()
        {
            IconSource = "ic_students.png",
            Label = MENU_LABEL_ALUNOS
        };
        static readonly MenuItem MENU_HORARIOS = new MenuItem()
        {
            IconSource = "ic_schedule.png",
            Label = MENU_LABEL_HORARIOS
        };
        static readonly MenuItem MENU_INICIO = new MenuItem()
        {
            IconSource = "ic_main.png",
            Label = MENU_LABEL_INICIO
        };
        static readonly MenuItem MENU_PLANO = new MenuItem()
        {
            IconSource = "ic_plan.png",
            Label = MENU_LABEL_PLANO
        };
        static readonly MenuItem MENU_REVIEWS = new MenuItem()
        {
            IconSource = "ic_research.png",
            Label = MENU_LABEL_REVIEWS
        };
        static readonly MenuItem MENU_SAIR = new MenuItem()
        {
            IconSource = "ic_exit.png",
            Label = MENU_LABEL_SAIR
        };
        static readonly MenuItem MENU_PROFESSORES = new MenuItem()
        {
            IconSource = "ic_teacher.png",
            Label = MENU_LABEL_PROFESSORES
        };
        static readonly MenuItem MENU_EVENTOS = new MenuItem()
        {
            IconSource = "ic_events.png",
            Label = MENU_LABEL_EVENTOS
        };

        public class MenuItem
        {
            public string IconSource { get; set; }
            public string Label { get; set; }
        }

        private readonly ObservableCollection<MenuItem> Menu;

    public AdmPage()
        {
            InitializeComponent();

            menuHeader.BindingContext = ((App)Application.Current).LoggedInUser;
            Detail = new NavigationPage(new AdmContents.MainContent()) {
                BarBackgroundColor = Color.FromHex(Application.Current.Resources["PrimaryDark"].ToString()),
                BarTextColor = Color.FromHex(Application.Current.Resources["Orange"].ToString())
            };

            Menu = new ObservableCollection<MenuItem>()
            {
                MENU_AULAS,
                MENU_ALUNOS
            };

            if((Application.Current as App).LoggedInUser.Function == "ADM")
            {
                Menu.Add(MENU_PROFESSORES);
                Menu.Add(MENU_HORARIOS);
                Menu.Add(MENU_EVENTOS);
                Menu.Add(MENU_REVIEWS);
                Menu.Add(MENU_PLANO);
            }

            Menu.Add(MENU_SAIR);

            menuList.ItemsSource = Menu;
        }

        private void MenuList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            string l = (e.Item as MenuItem).Label;
            
            switch (l)
            {
                case MENU_LABEL_AULAS:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadClassesPage" }, "LoadPage");
                    break;
                case MENU_LABEL_ALUNOS:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadStudentsPage" }, "LoadPage");
                    break;
                case MENU_LABEL_HORARIOS:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadSchedulesPage" }, "LoadPage");
                    break;
                case MENU_LABEL_PROFESSORES:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadTeachersPage" }, "LoadPage");
                    break;
                case MENU_LABEL_REVIEWS:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadReviewPage" }, "LoadPage");
                    break;
                case MENU_LABEL_EVENTOS:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadEventsPage" }, "LoadPage");
                    break;
                case MENU_LABEL_INICIO:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadStartPage" }, "LoadPage");
                    break;
                case MENU_LABEL_PLANO:
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadPlanPage" }, "LoadPage");
                    break;
                case MENU_LABEL_SAIR:
                    (Application.Current as App).LoggedInUser = new User();
                    (Application.Current as App).SavePropertiesAsync();

                    Application.Current.MainPage = new Login();
                    return;
            }

            IsPresented = false;
            if(l != MENU_LABEL_PLANO)
                EditMenu(l);
        }

        void EditMenu(string label)
        {
            //reset menu
            Menu.Clear();
            Menu.Add(MENU_AULAS);
            Menu.Add(MENU_ALUNOS);

            if ((Application.Current as App).LoggedInUser.Function == "ADM")
            {
                Menu.Add(MENU_PROFESSORES);
                Menu.Add(MENU_HORARIOS);
                Menu.Add(MENU_EVENTOS);
                Menu.Add(MENU_REVIEWS);
                Menu.Add(MENU_PLANO);
            }

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