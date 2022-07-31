using ctf_final.Models;
using Newtonsoft.Json;
using Plugin.CloudFirestore;
using Plugin.LocalNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using XamarinFirebase.Model;
using static ctf_final.AppController;
using static ctf_final.BackgroundTasks;

namespace ctf_final
{
    public partial class App : Application
    {
        private const string LOGGED_IN_USER = "LoggedInUserJson";
        private const string USERS_RESUME = "UsersResume";
        private const string ADM_SCHEDULES = "AdmSchedules";
        private const string TEACHERS = "Teachers";
        private const string EVENTS = "Events";
        private const string PLANS_PRICES = "PlansPrices";
        private const string EXPIRY_DATES = "ExpiryDates";
        private const string WL_FILTER = "WLFilter";
        private const string SAVED_ID = "SavedID";
        private const string LOGIN_DATE = "LastLoginDate";
        private const string QUESTIONNAIRE = "QuestionnaireList";

        public App()
        {
            InitializeComponent();

           
            new ConnectivityTest();
            if (LoggedInUser.Function == "ADM" || LoggedInUser.Function == "TEACHER")
            {
                TemporarySelectedSchedules = new List<SelectedSchedules>[3];
                MainPage = new AdmPage();
            }
            else if (LoggedInUser.Function == "USER")
            {
                MainPage = new StudentContents.StudentPage(true);
            }
            else
            {
                MainPage = new Login();
            }
        }
        protected override void OnStart()
        {
            LastLoginDate = SharedUtilities.GetTodayDateTime();
            if (LoggedInUser != null)
            {
                var resumeDoc = CrossCloudFirestore.Current
                            .Instance
                            .Collection("users")
                            .Document("resume");
                SharedUtilities.AddResumeDocListener(resumeDoc);

                if (LoggedInUser.Function == "ADM" || LoggedInUser.Function == "TEACHER") {
                    AdmUtilities.AddSchedulesListener();

                    Task.Run(async () =>
                    {
                        try
                        {
                            await DownloadWeightliftingFilter();

                            if (LoggedInUser.Function == "ADM")
                            {
                                try
                                {
                                    if(LastLoginDate.DayOfWeek == 0)
                                    {
                                        //TODO clear backups
                                        var backups = await CrossCloudFirestore.Current
                                                                .Instance
                                                                .Collection("users_backup")
                                                                .GetAsync();
                                        var batch = CrossCloudFirestore.Current.Instance.Batch();

                                        foreach (var d in backups.Documents)
                                            batch.Delete(d.Reference);

                                        await batch.CommitAsync();
                                    }
                                }
                                catch (Exception) { }

                                var query_events = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("events")
                                        .GetAsync();
                                var events = query_events.ToObjects<Events>();
                                SavedEvents = events.ToList();

                                var query_questionnaires = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("questionnaires")
                                        .GetAsync();
                                var questionnaires = query_questionnaires.ToObjects<Questionnaire>();
                                _app.QuestionnaireList = questionnaires.ToList();
                            }

                            var expiry_dates_query = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("adm_events")
                                    .Document("expiry_dates")
                                    .GetAsync();
                            var expiry_dates_resume = expiry_dates_query.ToObject<ExpiryResume>();
                            AdmUtilities.AddExpiryResumeListener();
                            ExpiryResumes = expiry_dates_resume;

                            //Defense mechanism against 'phantom users' expiry data
                            foreach (var exp in expiry_dates_resume.DateList)
                                if (_app.UsersResume.Users.Find(u => u.UserID == exp.UserID) == null)
                                    await expiry_dates_query.Reference.UpdateAsync("DateList", FieldValue.ArrayRemove(exp));
                            await AdmUtilities.DownloadTodayClasses();

                            var query_teachers = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("teachers")
                                        .GetAsync();
                            var teachers = query_teachers.ToObjects<User>();
                            Teachers = teachers.ToList();

                            var scheduleHistoryDoc = CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("adm_events")
                                        .Document("schedules_change_history");
                            var historyQuery = await scheduleHistoryDoc.GetAsync();
                            var history = historyQuery.ToObject<ScheduleHistory>();

                            //Check for phantoms
                            foreach(var h in history.History)
                            {
                                int id = Int32.Parse(h.Substring(0, 6));
                                if (_app.UsersResume.Users.Find(u => u.UserID == id) == null)
                                    await scheduleHistoryDoc.UpdateAsync("History", FieldValue.ArrayRemove(h));
                            }
                            if (!HasRightSchedules(history))
                            {
                                var query_schedules = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("schedules")
                                    .GetAsync();
                                var schedules = query_schedules.ToObjects<Schedule>();
                                var schedule_list = new List<Schedule>(schedules);
                                _app.AdmSchedules = schedule_list.OrderBy(s => s.Time).ToList();
                            }

                            AdmUtilities.CanEditSchedules = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
                }
                else if (LoggedInUser.Function == "USER")
                {
                    if(Device.RuntimePlatform == Device.iOS)
                    {
                        //ID_2 PILATES
                        var today = SharedUtilities.GetTodayDateTime();

                        var expiredTrain = false;
                        var trainExpiryDate = LoggedInUser.UserPlan.TrainPlanExpiryDate;
                        if (trainExpiryDate != null)
                            expiredTrain = DateTime.Parse(trainExpiryDate).AddDays(-10).Date <= today.Date;

                        var expiredYoga = false;
                        var yogaExpiryDate = LoggedInUser.UserPlan.TrainPlanExpiryDate;
                        if (yogaExpiryDate != null)
                            expiredYoga = DateTime.Parse(yogaExpiryDate).AddDays(-10).Date <= today.Date;

                        var expiredPilates = false;
                        var pilatesExpiryDate = LoggedInUser.UserPlan.PilatesPlanExpiryDate;
                        if (pilatesExpiryDate != null)
                            expiredPilates = DateTime.Parse(yogaExpiryDate).AddDays(-10).Date <= today.Date;

                        if (expiredTrain || expiredYoga || expiredPilates)
                            CrossLocalNotifications.Current.Show("Vencimento", "Um ou mais de seus planos vencem em 10 dias!");
                    }

                    Task.Run(async () => 
                    {
                        var query = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("users")
                                        .Document(LoggedInUser.UserID.ToString())
                                        .GetAsync();

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

                        await DownloadWeightliftingFilter();
                        //update

                        if (query.Exists)
                        {
                            var user = query.ToObject<User>();
                            _app.LoggedInUser = user;
                            await _app.SavePropertiesAsync();

                            UserUtilities.AddUserDocListener(query.Reference);

                            await SharedUtilities.RemoveOldClassesExceptions(LoggedInUser);
                            await SharedUtilities.RemoveOutdatedMakeupClasses(LoggedInUser);

                            ApplicationUserData = new UserData();
                            await UserUtilities.LoadUserClasses(LoggedInUser);
                        }
                    });
                }
            }
        }
        protected async override void OnResume()
        {
            if (LoggedInUser != null && LastLoginDate.Date != SharedUtilities.GetTodayDateTime().Date)
            {
                if (LoggedInUser.Function == "ADM" || LoggedInUser.Function == "TEACHER")
                {
                    DataStatus = false;
                    LastLoginDate = SharedUtilities.GetTodayDateTime();

                    MessagingCenter.Send(new PageControlMessage(), "OnResume");

                    await AdmUtilities.DownloadTodayClasses();
                }
                else if (LoggedInUser.Function == "USER")
                {
                    DataStatus = false;

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

                    LastLoginDate = SharedUtilities.GetTodayDateTime();
                    _app.ApplicationUserData.UserClasses.Clear();

                    MessagingCenter.Send(new PageControlMessage(), "OnResume");

                    foreach (var reference in UserUtilities.TemporaryUserClassReferences)
                    {
                        reference.Value.listener.Remove();
                    }
                    UserUtilities.TemporaryUserClassReferences.Clear();

                    await SharedUtilities.RemoveOldClassesExceptions(LoggedInUser);
                    await SharedUtilities.RemoveOutdatedMakeupClasses(LoggedInUser);

                    await UserUtilities.LoadUserClasses(LoggedInUser);
                }
            }
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        async Task DownloadWeightliftingFilter()
        {
            var query = await CrossCloudFirestore.Current
                                       .Instance
                                       .Collection("plans")
                                       .Document("weight_lifting_filter")
                                       .GetAsync();

            _app.WeightliftingFilter = query.ToObject<PlanModels.WLFilter>();
        }

        public class ConnectivityTest
        {
            CancellationTokenSource cts;
            bool IsRunning = false;
            public ConnectivityTest()
            {
                Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
                cts = new CancellationTokenSource();
            }

            async void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
            {
                var access = e.NetworkAccess;

                if (access == NetworkAccess.Internet && _app.LoggedInUser != null && _app.LoggedInUser.Function == "USER")
                {
                    if(!IsRunning)
                    {
                        try
                        {
                            IsRunning = true;

                            await Task.Delay(500);
                            await UpdateUserData(cts.Token);

                            IsRunning = false;
                        }
                        catch
                        {
                            Console.WriteLine("no internet connection");

                            IsRunning = false;
                        }
                    }
                }
                else
                { 
                    if(cts != null)
                    {
                        cts.Cancel();
                        cts = new CancellationTokenSource();
                    }
                }
            }

            private async Task UpdateUserData(CancellationToken cancellationToken)
            {
                Task task = null;
                task = Task.Run(async () =>
                {
                    
                    try
                    {
                        await CrossCloudFirestore.Current.Instance.DisableNetworkAsync();
                        await CrossCloudFirestore.Current.Instance.EnableNetworkAsync();

                        var user = await SharedUtilities.DownloadUserAndFixInconsistencies(_app.LoggedInUser.UserID);

                        _app.LoggedInUser = user;
                        await _app.SavePropertiesAsync();

                        if (CheckIfClassesChanged(user))
                        {
                            _app.DataStatus = false;

                            List<SimpleClass> userClasses = new List<SimpleClass>();
                            _app.ApplicationUserData.UserClasses.ForEach(cl =>
                            {
                                userClasses.Add(new SimpleClass
                                {
                                    Date = cl.Date,
                                    StudentsIDs = new List<int>(cl.StudentsIDs),
                                    Time = cl.Time,
                                    Type = cl.Type
                                });
                            });
                            _app.ApplicationUserData.UserClasses.Clear();

                            var temporaryReferences = new Dictionary<string, UserUtilities.ClassReference>();
                            foreach (var reference in UserUtilities.TemporaryUserClassReferences)
                            {
                                temporaryReferences.Add(reference.Key, reference.Value);
                                reference.Value.listener.Remove();
                            }
                            UserUtilities.TemporaryUserClassReferences.Clear();
                            MessagingCenter.Send(new PageControlMessage(), "OnResume");

                            var tasksCts = new CancellationTokenSource();
                            int timeout = 9500;
                            var loadTask = UserUtilities.LoadUserClasses(_app.LoggedInUser, tasksCts.Token);
                            if (await Task.WhenAny(loadTask, Task.Delay(timeout, tasksCts.Token)) == loadTask)
                            {
                                await loadTask;
                                MessagingCenter.Send(new DataFinishedLoadingMessage(), "DataLoaded");
                            }
                            else
                            {
                                tasksCts.Cancel();

                                foreach (var refer in temporaryReferences)
                                {
                                    UserUtilities.AddClassListener(refer.Value.docReference);
                                }
                                _app.ApplicationUserData.UserClasses = userClasses;
                                _app.DataStatus = true;

                                MessagingCenter.Send(new DataFinishedLoadingMessage(), "DataLoaded");
                            }
                        }
                        else
                        {
                        }
                    }
                    catch{}
                }, cancellationToken);
                await task;
            }
            private bool CheckIfClassesChanged(User user)
            {
                bool classesChanged = false;
                var newClasses = SharedUtilities.FormattUserClassesWithExceptions(user);
                var oldClasses = SharedUtilities.FormattUserClassesWithExceptions(_app.LoggedInUser);
                if (newClasses.Count != oldClasses.Count)
                    classesChanged = true;
                else
                {
                    oldClasses.ForEach(c =>
                    {
                        if (!newClasses.Contains(c))
                            classesChanged = true;
                    });
                }

                return classesChanged;
            }

        }
        bool HasRightSchedules(ScheduleHistory history)
        {
            try
            {
                foreach (var sch in AdmSchedules)
                {
                    var selectedScheduleHistory = history.History.FindAll(s => s.EndsWith(sch.Id.ToString()));
                    if (selectedScheduleHistory != null)
                    {
                        foreach (var c in sch.Classes)
                        {
                            var selectedClassHistory = selectedScheduleHistory.FindAll(s => s.EndsWith(c.Day + "@" + sch.Id));
                            if (selectedClassHistory != null)
                            {
                                foreach (var id in selectedClassHistory)
                                {
                                    if (!c.StudentsList.Contains(Int32.Parse(id.Substring(0, 6))))
                                        return false;
                                }
                            }
                            else
                            {
                                if (c.StudentsList.Count > 0)
                                    return false;
                            }
                        }
                    }
                    else
                    {
                        foreach (var c in sch.Classes)
                        {
                            if (c.StudentsList.Count > 0)
                                return false;
                        };
                    }
                };

                return true;
            }
            catch(Exception)
            {
                return false;
            }
            
        }

        public class SelectedSchedules
        {
            public SelectedSchedules(string time, int wd, int id)
            {
                ID = id;
                Time = time;
                Day = wd;
                Desc = SharedUtilities.IntToWeekday(wd) + " - " + time;
            }

            public int ID { get; set; }
            public string Time { get; set; }
            public int Day { get; set; }
            public string Desc { get; set; }
            public string ClassException { get; set; }
            public bool Unchangeable { get; set; }
        }
        public List<SelectedSchedules>[] TemporarySelectedSchedules { get; set; }
        public void ClearTemporarySchedules()
        {
            TemporarySelectedSchedules = new List<SelectedSchedules>[3] { null, null, null };
        }

        public UserData ApplicationUserData { get; set; }

        private bool _dataStatus;
        public bool DataStatus {
            get {
                return _dataStatus;
            }
            set {
                _dataStatus = value;
                if (value)
                    MessagingCenter.Send(new DataFinishedLoadingMessage(), "DataLoaded");
            }
        }

        User _loggedInUser = null;
        public User LoggedInUser {
            get {
                if (_loggedInUser != null)
                    return _loggedInUser;

                if (Properties.ContainsKey(LOGGED_IN_USER))
                {
                    _loggedInUser = JsonConvert.DeserializeObject<User>(Properties[LOGGED_IN_USER].ToString());
                    return _loggedInUser;
                }

                return new User();
            }
            set {
                if (value != null)
                {
                    _loggedInUser = value;
                    Properties[LOGGED_IN_USER] = JsonConvert.SerializeObject(value);
                }
                else
                {
                    _loggedInUser = new User();
                }
            }
        }

        SharedUtilities.UsersResume _usersResume = null;
        public SharedUtilities.UsersResume UsersResume 
        {
            get 
            {
                if (_usersResume != null)
                    return _usersResume;

                if (Properties.ContainsKey(USERS_RESUME))
                {
                    _usersResume = JsonConvert.DeserializeObject<SharedUtilities.UsersResume>(Properties[USERS_RESUME].ToString());
                    return _usersResume;
                }

                return new SharedUtilities.UsersResume();
            }
            set 
            {
                if (value != null)
                {
                    _usersResume = value;
                    Properties[USERS_RESUME] = JsonConvert.SerializeObject(value);
                }
                else
                {
                    Console.Write("Error: can't set usersresume to null.");
                }
            }
        }

        List<User> _teachers = null;
        public List<User> Teachers 
        {
            get {
                if (_teachers != null)
                    return _teachers;

                if (Properties.ContainsKey(TEACHERS))
                {
                    _teachers = JsonConvert.DeserializeObject<List<User>>(Properties[TEACHERS].ToString());
                    return _teachers;
                }

                return new List<User>();
            }
            set {
                if (value != null)
                {
                    _teachers = value;
                    Properties[TEACHERS] = JsonConvert.SerializeObject(value);
                }
                else
                {
                    Console.Write("Error: can't set Teachers to null.");
                }
            }
        }

        List<Events> _events = null;
        public List<Events> SavedEvents {
            get {
                if (_events != null)
                    return _events;

                if (Properties.ContainsKey(EVENTS))
                {
                    _events = JsonConvert.DeserializeObject<List<Events>>(Properties[EVENTS].ToString());
                    return _events;
                }

                return new List<Events>();
            }
            set {
                if (value != null)
                {
                    _events = value;
                    Properties[EVENTS] = JsonConvert.SerializeObject(value);
                }
                else
                {
                    Console.Write("Error: can't set Teachers to null.");
                }
            }
        }

        List<Schedule> _admSchedules = null;
        public List<Schedule> AdmSchedules {
            get 
            {
                if (_admSchedules != null)
                    return _admSchedules;

                if (Properties.ContainsKey(ADM_SCHEDULES))
                {
                    _admSchedules = JsonConvert.DeserializeObject<List<Schedule>>(Properties[ADM_SCHEDULES].ToString());
                    return _admSchedules;
                }

                return new List<Schedule>();
            }
            set 
            {
                if (value != null)
                {
                    _admSchedules = value;
                    Properties[ADM_SCHEDULES] = JsonConvert.SerializeObject(value);
                }
                else
                {
                    Console.Write("Error: can't set AdmSchedules to null.");
                }
            }
        }

        Dictionary<string, double> _planPrices = null;
        public Dictionary<string, double> PlanPrices {
            get 
            {
                if (_planPrices != null)
                    return _planPrices;

                if (Properties.ContainsKey(PLANS_PRICES))
                {
                    _planPrices = JsonConvert.DeserializeObject<Dictionary<string, double>>(Properties[PLANS_PRICES].ToString());
                    return _planPrices;
                }

                return new Dictionary<string, double>();
            }
            set 
            {
                if (value != null)
                {
                    _planPrices = value;
                    Properties[PLANS_PRICES] = JsonConvert.SerializeObject(value);
                }
            }
        }

        ExpiryResume _expiryResumes = null;
        public ExpiryResume ExpiryResumes {
            get {
                if (_expiryResumes != null)
                    return _expiryResumes;

                if (Properties.ContainsKey(EXPIRY_DATES))
                {
                    _expiryResumes = JsonConvert.DeserializeObject<ExpiryResume>(Properties[EXPIRY_DATES].ToString());
                    return _expiryResumes;
                }

                return new ExpiryResume { DateList = new List<ExpiryResume.Resume>() };
            }
            set {
                if (value != null)
                {
                    _expiryResumes = value;
                    Properties[EXPIRY_DATES] = JsonConvert.SerializeObject(value);
                }
            }
        }

        List<Questionnaire> _questionnaireList = null;
        public List<Questionnaire> QuestionnaireList {
            get {
                if (_questionnaireList != null)
                    return _questionnaireList;

                if (Properties.ContainsKey(QUESTIONNAIRE))
                {
                    _questionnaireList = JsonConvert.DeserializeObject<List<Questionnaire>>(Properties[QUESTIONNAIRE].ToString());
                    return _questionnaireList;
                }

                return new List<Questionnaire>();
            }
            set {
                if (value != null)
                {
                    _questionnaireList = value;
                    Properties[QUESTIONNAIRE] = JsonConvert.SerializeObject(value);
                }
            }
        }

        PlanModels.WLFilter _weightliftingFilter = null;
        public PlanModels.WLFilter WeightliftingFilter {
            get {
                if (_weightliftingFilter != null)
                    return _weightliftingFilter;

                if (Properties.ContainsKey(WL_FILTER))
                {
                    _weightliftingFilter = JsonConvert.DeserializeObject<PlanModels.WLFilter>(Properties[WL_FILTER].ToString());
                    return _weightliftingFilter;
                }

                return new PlanModels.WLFilter();
            }
            set {
                if (value != null)
                {
                    _weightliftingFilter = value;
                    Properties[WL_FILTER] = JsonConvert.SerializeObject(value);
                }
            }
        }

        public string SavedUserID 
        {
            get 
            {
                if(Properties.ContainsKey(SAVED_ID))
                    return Properties[SAVED_ID].ToString();

                return null;
            }
            set 
            {
                Properties[SAVED_ID] = value;
            }
        }
        private DateTime LastLoginDate {
            get {
                if (Properties.ContainsKey(LOGIN_DATE))
                    return DateTime.Parse(Properties[LOGIN_DATE].ToString());

                return new DateTime();
            }
            set {
                if (value != null)
                    Properties[LOGIN_DATE] = value.ToString("yyyy-MM-dd");
            }
        }
    }
}
