using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Services;
using ctf_final.Models;
using static ctf_final.AppController;
using System.Linq;
using Xamarin.Essentials;

namespace ctf_final
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Login : ContentPage
    {
        private List<Schedule> schedule_list;
        public Login()
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(_app.SavedUserID))
                user.Text = _app.SavedUserID;

            if(Device.RuntimePlatform == Device.iOS)
            {
                user.BackgroundColor = (Color)_app.Resources["PrimaryTransparent"];
                pass.BackgroundColor = (Color)_app.Resources["PrimaryTransparent"];
            }
        }

        private async void Login_Clicked(object sender, EventArgs e)
        {
            loginBtn.IsEnabled = false;

            //-- SECURITY CHECKS --

            if (pass.Text != "0" && (string.IsNullOrWhiteSpace(pass.Text) || pass.Text.Length != 10))
            {
                ResetView();
                await DisplayAlert("Erro", "Por favor insira uma data de nascimento válida. (ex. 24/09/1972)", "OK");
                return;
            }
            string password = pass.Text.Replace("/", "");

            if(string.IsNullOrEmpty(user.Text))
            {
                ResetView();
                await DisplayAlert("Erro", "Por favor insira uma ID válido.", "OK");
                return;
            }

            if (!CrossCloudFirestore.IsSupported)
            {
                ResetView();
                await DisplayAlert("Erro", "Seu aparelho não suporta o aplicativo.", "OK");
                return;
            }

            var current = Connectivity.NetworkAccess;
            if (current != NetworkAccess.Internet)
            {
                ResetView();
                await DisplayAlert("Erro", "Não foi conectar-se com o servidor, verifique sua conexão com a internet e tente novamente.", "Ok");
                return;
            }

            //-- SECURITY CHECKS --

            await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup(), true);
            /* Search in the database for the given userID. If found,
             * download the user info and subscribe for any changes in
             * the document (Only for USER, not for ADM) */
            try
            {
                var query = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("users")
                                        .Document(user.Text)
                                        .GetAsync();
                if (query.Exists)
                {
                    var foundUser = query.ToObject<User>();

                    if(foundUser.Function == "ADM" || foundUser.Function == "TEACHER")
                    {
                        await CheckCredentials(foundUser, password);
                    }
                    else if(foundUser.Function == "USER")
                    {
                        if (!await UserUtilities.CheckExpiryDates(query.Reference, foundUser))
                        {
                            if(!await UserUtilities.LockUserPlan(foundUser, query.Reference))
                            {
                                ResetView();
                                await DisplayAlert("Erro", "Seu plano está vencido e não foi possível logar, tente novamente mais tarde.", "OK");
                            }
                        }
                        UserUtilities.AddUserDocListener(query.Reference);

                        /* Update outdated user 'ClassExceptions' if necessary */
                        await SharedUtilities.RemoveOutdatedMakeupClasses(foundUser);
                        await SharedUtilities.RemoveOldClassesExceptions(foundUser);

                        await CheckCredentials(foundUser, password);
                    }
                    else
                    {
                        ResetView();
                        await DisplayAlert("Erro", "Algo deu errado, por favor tente novamente.", "OK");
                    }
                }
                else
                {
                    //Teacher check
                    var queryTeachers = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("teachers")
                                        .Document(user.Text)
                                        .GetAsync();

                    if(queryTeachers.Exists)
                    {
                        var foundTeacher = queryTeachers.ToObject<User>();
                        await CheckCredentials(foundTeacher, password);
                    }
                    else
                    {
                        ResetView();
                        await DisplayAlert("Erro", "ID de usuário não encontrado, por favor tente novamente.", "OK");
                    }
                }
            }
            catch(Exception eex)
            {
                Console.WriteLine(eex);
                ResetView();
                await DisplayAlert("Erro desconhecido", "Erro ao efetuar login. Tente novamente mais tarde", "OK");
            }
        }

        private async Task CheckCredentials(User user, string password) 
        {
            if (user != null && user.Birthday == password)
            {
                _app.LoggedInUser = user;

                /* Loads the resume of users from the database. Used for loading profile pictures
                * and user names without having to download all the other docs. */
                try
                {
                    var resume_query = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("users")
                                    .Document("resume")
                                    .GetAsync();
                    //Reorder by name
                    var resume = resume_query.ToObject<SharedUtilities.UsersResume>();
                    resume.Users = resume.Users.OrderBy(u => u.Name).ToList();
                    _app.UsersResume = resume;

                    SharedUtilities.AddResumeDocListener(resume_query.Reference); 
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    ResetView();
                    await DisplayAlert("Erro desconhecido", "Erro ao efetuar login. Tente novamente mais tarde.", "OK");
                }

                /* Checks the kind of user that is logging in, and open their version of the
                 * app (ADM or USER). If a USER, the app loads his/hers scheduled classes
                 * and setup notifications. If ADM, downloads 'schedules', 'plan_prices' and 'today
                 * classes' to fill the main page. */
                if (user.Function.Equals("ADM"))
                {
                    var prices_query = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("plans")
                                    .Document("prices")
                                    .GetAsync();
                    var price_list = prices_query.ToObject<PlanModels.TemporaryPlanPricesList>();
                    var pricesDictionary = new Dictionary<string, double>();
                    price_list.PricesList.ForEach(priceString =>
                    {
                        var values = priceString.Split('@');
                        pricesDictionary.Add(values[0], double.Parse(values[1]));
                    });
                    _app.PlanPrices = pricesDictionary;

                    //update
                    var query_teachers = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("teachers")
                                    .GetAsync();
                    var teachers = query_teachers.ToObjects<User>();
                    _app.Teachers = new List<User>(teachers);

                    var query_events = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("events")
                                    .GetAsync();
                    var events = query_events.ToObjects<Events>();
                    _app.SavedEvents = events.ToList();

                    var query_questionnaires = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("questionnaires")
                                    .GetAsync();
                    var questionnaires = query_questionnaires.ToObjects<Questionnaire>();
                    _app.QuestionnaireList = questionnaires.ToList();
                    //AdmUtilities.AddExpiryResumeListener(); events listener
                    //update

                    var expiry_dates_query = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("adm_events")
                                    .Document("expiry_dates")
                                    .GetAsync();
                    var expiry_dates_resume = expiry_dates_query.ToObject<ExpiryResume>();
                    AdmUtilities.AddExpiryResumeListener();
                    _app.ExpiryResumes = expiry_dates_resume;

                    var query_schedules = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("schedules")
                                    .GetAsync();
                    var schedules = query_schedules.ToObjects<Schedule>();
                    schedule_list = new List<Schedule>(schedules);
                    AdmUtilities.AddSchedulesListener();

                    await AdmUtilities.DownloadTodayClasses();

                    _app.AdmSchedules = schedule_list.OrderBy(s => s.Time).ToList();
                    AdmUtilities.CanEditSchedules = true;

                    Application.Current.MainPage = new AdmPage();
                }
                else if(user.Function.Equals("USER"))
                {
                    //update
                    var query_events = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("events")
                                    .GetAsync();
                    var events = query_events.ToObjects<Events>();
                    _app.SavedEvents = events.ToList();

                    var query_questionnaires = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("questionnaires")
                                    .GetAsync();
                    var questionnaires = query_questionnaires.ToObjects<Questionnaire>();
                    _app.QuestionnaireList = questionnaires.ToList();
                    //update

                    _app.ApplicationUserData = new UserData();
                    _app.SavedUserID = user.UserID.ToString();
                    await _app.SavePropertiesAsync();

                    await UserUtilities.LoadUserClasses(user);
                    UserUtilities.AddPlanExpiryNotifications();

                    Application.Current.MainPage = new StudentContents.StudentPage();
                }
                else if(user.Function.Equals("TEACHER"))
                {
                    var query_schedules = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("schedules")
                                    .GetAsync();
                    var schedules = query_schedules.ToObjects<Schedule>();
                    schedule_list = new List<Schedule>(schedules);
                    AdmUtilities.AddSchedulesListener();

                    await AdmUtilities.DownloadTodayClasses();

                    _app.AdmSchedules = schedule_list.OrderBy(s => s.Time).ToList();
                    //AdmUtilities.CanEditSchedules = true;

                    Application.Current.MainPage = new AdmPage();
                }

                await PopupNavigation.Instance.PopAsync();
                await Application.Current.SavePropertiesAsync();
            }
            else
            {
                ResetView();
                await DisplayAlert("Erro", "Data de nascimento incorreta, por favor tente novamente.", "OK");
            }
        }

        async void ResetView()
        {
            loginBtn.IsEnabled = true;
            if(PopupNavigation.Instance.PopupStack.Count > 0)
                await PopupNavigation.Instance.PopAsync();
        }
    }
}