using ctf_final.Models;
using ctf_final.PlanModels;
using Firebase.Storage;
using Plugin.CloudFirestore;
using Plugin.CloudFirestore.Reactive;
using Plugin.LocalNotifications;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using XamarinFirebase.Model;

namespace ctf_final
{
    public class AppController
    {
        public static App _app = Application.Current as App;

        public class UserUtilities
        {
            //------ DOCUMENTS ------

                //--- VARIABLES AND CLASSES ---

            public static IDocumentReference UserDocument;
            public static IListenerRegistration UserDocumentListener;

            public class ClassReference
            {
                public IListenerRegistration listener;
                public IDocumentReference docReference;
            }
            /// <summary>
            /// Uses selected "classes" doc_path as key. (eg. 2019-09-26/09:30/Treino)
            /// </summary>
            public static Dictionary<string, ClassReference> TemporaryUserClassReferences = new Dictionary<string, ClassReference>();

                //--- VARIABLES AND CLASSES ---

            public async static Task LoadUserClasses(User myUser, CancellationToken ct)
            {
                try
                {
                    await Task.Run(async () =>
                    {
                        List<string> user_schedules = SharedUtilities.FormattUserClassesWithExceptions(myUser);
                        foreach (string sr in user_schedules)
                        {
                            //BasicClass: ClassID@DocPath@Time/Type
                            //ClassException: empty@DocPath@empty (Is certain that this class doc already exists)
                            string[] classFinder = sr.Split('@');
                            string docPath = classFinder[1];

                            var query_class = CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("classes")
                                        .Document(docPath);

                            var classDoc = await query_class.GetAsync();
                            if (classDoc.Exists)
                            {
                                var sc = classDoc.ToObject<SimpleClass>();

                                if (sc != null)
                                {
                                    _app.ApplicationUserData.UserClasses.Add(sc);
                                    AddClassListener(query_class);
                                }
                            }
                            else
                            {
                                await SharedUtilities.CreateClass(classFinder);
                            }
                        }
                        _app.DataStatus = true;
                    }, ct);
                }
                catch {}
            }
            public async static Task LoadUserClasses(User myUser)
            {
                try
                {
                    List<string> user_schedules = SharedUtilities.FormattUserClassesWithExceptions(myUser);
                    foreach (string sr in user_schedules)
                    {
                        //BasicClass: ClassID@DocPath@Time/Type
                        //ClassException: empty@DocPath@empty (Is certain that this class doc already exists)
                        string[] classFinder = sr.Split('@');
                        string docPath = classFinder[1];

                        var query_class = CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("classes")
                                    .Document(docPath);

                        var classDoc = await query_class.GetAsync();
                        if (classDoc.Exists)
                        {
                            var sc = classDoc.ToObject<SimpleClass>();

                            if (sc != null)
                            {
                                _app.ApplicationUserData.UserClasses.Add(sc);
                                AddClassListener(query_class);
                            }
                        }
                        else
                        {
                            await SharedUtilities.CreateClass(classFinder);
                        }
                    }
                    _app.DataStatus = true;
                }
                catch
                {

                }
            }

            public static void AddUserDocListener(IDocumentReference doc)
            {
                UserDocument = doc;

                if (UserDocumentListener != null)
                    UserDocumentListener.Remove();

                UserDocumentListener = doc.AddSnapshotListener((snp, error) =>
                {
                    try
                    {
                        var newUser = snp.ToObject<User>();
                        if (newUser == null)
                        {
                            try { Loggout(); } catch { _app.LoggedInUser = new User(); };
                            _app.MainPage = new Login();
                            return;
                        }

                        if (!snp.Metadata.IsFromCache && !snp.Metadata.HasPendingWrites)
                        {
                            if (newUser == _app.LoggedInUser)
                                return;

                            /* Create an instance of the old user document and compares it to the new one
                            * to update the application. */
                            var oldUser = new User
                            {
                                MakeupClasses = _app.LoggedInUser.MakeupClasses,
                                MakeupClassesYoga = _app.LoggedInUser.MakeupClassesYoga,
                                Name = _app.LoggedInUser.Name,
                                ClassesExceptions = _app.LoggedInUser.ClassesExceptions,
                                Email = _app.LoggedInUser.Email,
                                Birthday = _app.LoggedInUser.Birthday,
                                Address = _app.LoggedInUser.Address,
                                PictureToken = _app.LoggedInUser.PictureToken,
                                Phone = _app.LoggedInUser.Phone,
                                Gender = _app.LoggedInUser.Gender
                            };

                            _app.LoggedInUser = newUser;
                            _app.SavePropertiesAsync();

                            if (oldUser.MakeupClasses != newUser.MakeupClasses || oldUser.MakeupClassesYoga != newUser.MakeupClassesYoga)
                            {
                                MessagingCenter.Send(new PageUpdateMessage() { Command = "MakeupClassesChanged" }, "UserDataUpdated");
                            }

                            if (oldUser.Name != newUser.Name)
                            {
                                MessagingCenter.Send(new PageUpdateMessage() { Command = "NameChanged" }, "UserDataUpdated");
                            }

                            if (oldUser.Birthday != newUser.Birthday || oldUser.Phone != newUser.Phone || oldUser.Gender != newUser.Gender ||
                             oldUser.Email != newUser.Email || oldUser.Address != newUser.Address)
                            {
                                MessagingCenter.Send(new PageUpdateMessage() { Command = "BasicDataChanged" }, "UserDataUpdated");
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    
                });
            }
            public static void AddClassListener(IDocumentReference doc)
            {
                var listener = doc.AddSnapshotListener((snp, error) =>
                {
                    try
                    { 
                        if (!snp.Metadata.IsFromCache && !snp.Metadata.HasPendingWrites)
                        {
                            SimpleClass oldClass = null;

                            var updatedClass = snp.ToObject<SimpleClass>();
                            if (updatedClass != null)
                                oldClass = _app.ApplicationUserData.UserClasses.Find(uc => uc.Date == updatedClass.Date && uc.Type == updatedClass.Type);

                            if (oldClass != null)
                            {
                                oldClass.StudentsIDs = updatedClass.StudentsIDs;

                                if (_app.DataStatus)
                                    MessagingCenter.Send(new PageUpdateMessage() { Command = "ClassesChanged" }, "UserDataUpdated");
                            }
                        }
                    }
                        catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
                    TemporaryUserClassReferences.Add(doc.Path.Replace("classes/", ""), new ClassReference { docReference = doc, listener = listener });

                if(_app.DataStatus)
                    MessagingCenter.Send(new PageUpdateMessage() { Command = "ClassesChanged" }, "UserDataUpdated");
            }


            //------ DOCUMENTS ------


                //------ MAIN FUNCTIONS ------

            public async static Task<bool> ClearAppointment(string path, string type)
            {
                try
                {
                    //-- DATA --

                    var docReference = TemporaryUserClassReferences[path].docReference;
                    var listener = TemporaryUserClassReferences[path].listener;

                    string pathAsDate = path.Substring(0, 10).Replace("/", "-");
                    var selectedSC = _app.ApplicationUserData.UserClasses.Find(sc => sc.Date == pathAsDate && sc.Type == type);

                    //-- DATA --

                    //-- SERVER SIDE --

                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    batch.Update(docReference, "StudentsIDs", FieldValue.ArrayRemove(_app.LoggedInUser.UserID));

                    var classException = UpdateClassExceptionWithBatch("remove", path, type, 1, batch);

                    SharedUtilities.UpdateRealScheduleDocWithBatch(selectedSC, batch, _app.LoggedInUser.UserID, false);

                    await batch.CommitAsync();

                    //-- SERVER SIDE --

                    //-- LOCAL SIDE --

                    UpdateLocalClassExceptionAndMakeupClasses(classException, 1, type);

                    listener.Remove();
                    TemporaryUserClassReferences.Remove(path);
                    _app.ApplicationUserData.UserClasses.Remove(selectedSC);

                    _app.ApplicationUserData = _app.ApplicationUserData;
                    _app.LoggedInUser = _app.LoggedInUser;

                    await _app.SavePropertiesAsync();

                    //-- LOCAL SIDE --

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            public async static Task<bool> MarkAppointment(List<StudentContents.MakeupClassPicker.TemporarySchedules> DownloadedSchedules, SchedulesByDayOfWeek.Times c)
            {
                try
                {
                    //-- AVAILABILITY CHECK --

                    if (c.StudentsList.Count >= SharedUtilities.GetClassSizeLimitByType(c.Type) - 1)
                        if (await LastPlace())
                            return false;

                    //-- AVAILABILITY CHECK --

                    //-- DATA --

                    var docPath = c.Date + "/" + c.Time + "/" + c.Type;
                    var classRef = CrossCloudFirestore.Current.Instance.Collection("classes").Document(docPath);

                    //-- DATA --

                    //-- SERVER SIDE --

                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    batch.Update(classRef, "StudentsIDs", FieldValue.ArrayUnion(_app.LoggedInUser.UserID));
                    SharedUtilities.UpdateRealScheduleDocWithBatch(c, batch, _app.LoggedInUser.UserID);
                    var classException = UpdateClassExceptionWithBatch("add", docPath, c.Type, -1, batch);

                    await batch.CommitAsync();

                    //-- SERVER SIDE --

                    //--LOCAL SIDE--

                    DownloadedSchedules.ForEach(ds =>
                    {
                        ds.TemporaryListener.Remove();
                    });

                    UpdateLocalClassExceptionAndMakeupClasses(classException, - 1, c.Type);
                    
                    var doc = await classRef.GetAsync();
                    SimpleClass sc = doc.ToObject<SimpleClass>();
                    AddClassListener(classRef);
                    _app.ApplicationUserData.UserClasses.Add(sc);

                    _app.ApplicationUserData = _app.ApplicationUserData;
                    _app.LoggedInUser = _app.LoggedInUser;

                    await _app.SavePropertiesAsync();

                    //--LOCAL SIDE--

                    return true;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }

                //--- AUXILIARY FUNCTIONS ---

            static string[] UpdateClassExceptionWithBatch(string cmd, string path, string type, int amount, IWriteBatch batch)
            {
                //--- SELECTED DATA ---

                var finalClassException = "";
                var oldestDate = "";

                string makeupClassesFieldName = type == "Treino" ? "MakeupClasses" : type == "Yoga" ? "MakeupClassesYoga" : "MakeupClassesPilates";
                var newMakeupClassesValue = type == "Treino" ? _app.LoggedInUser.MakeupClasses + amount : 
                                            type == "Yoga" ? _app.LoggedInUser.MakeupClassesYoga + amount :
                                            _app.LoggedInUser.MakeupClassesPilates + amount;

                string datesFieldName = type == "Treino" ? "MCTrainDates" : type == "Yoga" ? "MCYogaDates" : "MCPilatesDates";
                var selectedDatesList = type == "Treino" ? _app.LoggedInUser.MCTrainDates : 
                                        type == "Yoga" ? _app.LoggedInUser.MCYogaDates :
                                        _app.LoggedInUser.MCPilatesDates;
                selectedDatesList.Sort();

                var todayDate = SharedUtilities.GetTodayDateTime();
                var newDateString = todayDate.ToString("yyyy-MM-dd");

                //--- SELECTED DATA ---

                if (cmd == "add")
                    oldestDate = selectedDatesList.First();
                else
                {
                    if (selectedDatesList.Count > 0)
                    {
                        var fd = selectedDatesList.FindAll(d => d.StartsWith(todayDate.ToString("yyyy-MM-dd")));
                        if (fd == null)
                            newDateString += "@1";
                        else
                            newDateString = newDateString + "@" + (1 + fd.Count);

                        var i = 0;
                        while (selectedDatesList.Contains(newDateString))
                        {
                            newDateString = todayDate.ToString("yyyy-MM-dd") + "@" + (i + fd.Count);
                            i++;
                        }
                    }
                    else
                        newDateString += "@1";
                }

                //--- RESULT ---

                var cmdOpposite = cmd == "add" ? "remove" : "add";
                if (_app.LoggedInUser.ClassesExceptions.Any(ce => ce.StartsWith(path)))
                {
                    finalClassException = path + "@" + cmdOpposite;

                    batch.Update(UserDocument, "ClassesExceptions", FieldValue.ArrayRemove(path + "@" + cmdOpposite));
                    batch.Update(UserDocument, makeupClassesFieldName, newMakeupClassesValue);
                    batch.Update(UserDocument, datesFieldName, cmd == "add" ? FieldValue.ArrayRemove(oldestDate) : FieldValue.ArrayUnion(newDateString));
                }
                else
                {
                    finalClassException = path + "@" + cmd;

                    batch.Update(UserDocument, "ClassesExceptions", FieldValue.ArrayUnion(path + "@" + cmd));
                    batch.Update(UserDocument, makeupClassesFieldName, newMakeupClassesValue);
                    batch.Update(UserDocument, datesFieldName, cmd == "add" ? FieldValue.ArrayRemove(oldestDate) : FieldValue.ArrayUnion(newDateString));
                }

                //--- RESULT ---

                var selectedDate = cmd == "add" ? oldestDate : newDateString;
                return new string[2] { finalClassException, selectedDate };
            }
            public static void UpdateLocalClassExceptionAndMakeupClasses(string[] classException, int i = 1, string type = "Treino")
            {
                //- CLASS EXCEPTION -

                if (_app.LoggedInUser.ClassesExceptions.Contains(classException[0]))
                    _app.LoggedInUser.ClassesExceptions.Remove(classException[0]);
                else
                    _app.LoggedInUser.ClassesExceptions.Add(classException[0]);

                //- CLASS EXCEPTION -

                //- MAKEUP CLASSES -

                if (type == "Treino")
                    _app.LoggedInUser.MakeupClasses += i;
                if (type == "Yoga")
                    _app.LoggedInUser.MakeupClassesYoga += i;
                if (type == "Pilates")
                    _app.LoggedInUser.MakeupClassesPilates += i;

                //- MAKEUP CLASSES  -

                //- DATES -

                if (i > 0)
                {
                    if (type == "Treino")
                        _app.LoggedInUser.MCTrainDates.Add(classException[1]);
                    else if (type == "Yoga")
                        _app.LoggedInUser.MCYogaDates.Add(classException[1]);
                    else if (type == "Pilates")
                        _app.LoggedInUser.MCPilatesDates.Add(classException[1]);

                }
                else if (i < 0)
                {
                    if (type == "Treino")
                        _app.LoggedInUser.MCTrainDates.Remove(classException[1]);
                    else if (type == "Yoga")
                        _app.LoggedInUser.MCYogaDates.Remove(classException[1]);
                    else if (type == "Pilates")
                        _app.LoggedInUser.MCPilatesDates.Add(classException[1]);
                }

                //- DATES -

                MessagingCenter.Send(new PageUpdateMessage() { Command = "MakeupClassesChanged" }, "UserDataUpdated");
            }

            public static bool NumberOfStudentsChanged = false;
            public async static Task<bool> LastPlace()
            {
                Random rnd = new Random();
                await Task.Delay(rnd.Next(1000, 2500));

                if (NumberOfStudentsChanged)
                {
                    NumberOfStudentsChanged = false;
                    return true;
                }

                return false;
            }

                //--- AUXILIARY FUNCTIONS ---

            //------ MAIN FUNCTIONS ------


            //------ PLAN RELATED FUNCTIONS ------ 

            public async static Task<bool> CheckExpiryDates(IDocumentReference userDoc, User u)
            {
                if (u.PlanAbscence == 1)
                    return true;

                bool train = u.UserPlan.TrainPlan == null ? false : DateTime.Parse(u.UserPlan.TrainPlanExpiryDate).Date > DateTime.Today.Date;
                bool yoga = u.UserPlan.YogaPlan == null ? false : DateTime.Parse(u.UserPlan.YogaPlanExpiryDate).Date > DateTime.Today.Date;
                bool pilates = u.UserPlan.PilatesPlan == null ? false : DateTime.Parse(u.UserPlan.PilatesPlanExpiryDate).Date > DateTime.Today.Date;

                var newTrainDate = u.UserPlan.TrainPlan == null ? null : SharedUtilities.GetExpiryDate(u.UserPlan.TrainPlan, DateTime.Parse(u.UserPlan.TrainPlanExpiryDate));
                var newYogaDate = u.UserPlan.YogaPlan == null ? null : SharedUtilities.GetExpiryDate(u.UserPlan.YogaPlan, DateTime.Parse(u.UserPlan.YogaPlanExpiryDate));
                var newPilatesDate = u.UserPlan.PilatesPlan == null ? null : SharedUtilities.GetExpiryDate(u.UserPlan.PilatesPlan, DateTime.Parse(u.UserPlan.PilatesPlanExpiryDate));

                var batch = CrossCloudFirestore.Current.Instance.Batch();
                var changeTrain = u.UserPlan.TrainPlan != null && !train && u.UserPlan.TrainAutoRenewal == 1;
                if (u.UserPlan.TrainPlan != null && !train && u.UserPlan.TrainAutoRenewal == 1)
                {
                    train = true;
                    batch.Update(userDoc, new FieldPath("UserPlan", "TrainPlanExpiryDate"), newTrainDate);
                }

                var changeYoga = u.UserPlan.YogaPlan != null && !yoga && u.UserPlan.YogaAutoRenewal == 1;
                if (changeYoga)
                {
                    yoga = true;
                    batch.Update(userDoc, new FieldPath("UserPlan", "YogaPlanExpiryDate"), newYogaDate);
                }

                var changePilates = u.UserPlan.PilatesPlan != null && !pilates && u.UserPlan.PilatesAutoRenewal == 1;
                if (changePilates)
                {
                    pilates = true;
                    batch.Update(userDoc, new FieldPath("UserPlan", "PilatesPlanExpiryDate"), newPilatesDate);
                }

                if (changeYoga || changeTrain || changePilates)
                {
                    SharedUtilities.UpdateExpiryResumeWithBatch(batch,
                        new ExpiryResume.Resume //old
                        {
                            UserID = u.UserID,
                            ExpiryDate = u.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = u.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = u.UserPlan.PilatesPlanExpiryDate
                        },
                        new ExpiryResume.Resume //new
                        {
                            UserID = u.UserID,
                            ExpiryDate = changeTrain ? newTrainDate : u.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = changeYoga ? newYogaDate : u.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = changePilates ? newPilatesDate : u.UserPlan.PilatesPlanExpiryDate
                        });
                }

                await batch.CommitAsync();
                u.UserPlan.TrainPlanExpiryDate = changeTrain ? newTrainDate : u.UserPlan.TrainPlanExpiryDate;
                u.UserPlan.YogaPlanExpiryDate = changeYoga ? newYogaDate : u.UserPlan.YogaPlanExpiryDate;
                u.UserPlan.PilatesPlanExpiryDate = changePilates ? newPilatesDate : u.UserPlan.PilatesPlanExpiryDate;

                return train || yoga || pilates;
            }
            public static bool CheckExpiryDates()
            {
                var user = _app.LoggedInUser;
                var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(user.UserID.ToString());

                bool train = user.UserPlan.TrainPlan == null ? false : DateTime.Parse(user.UserPlan.TrainPlanExpiryDate).Date > DateTime.Today.Date;
                bool yoga = user.UserPlan.YogaPlan == null ? false : DateTime.Parse(user.UserPlan.YogaPlanExpiryDate).Date > DateTime.Today.Date;
                bool pilates = user.UserPlan.PilatesPlan == null ? false : DateTime.Parse(user.UserPlan.PilatesPlanExpiryDate).Date > DateTime.Today.Date;

                var newTrainDate = user.UserPlan.TrainPlan == null ? null : SharedUtilities.GetExpiryDate(user.UserPlan.TrainPlan, DateTime.Parse(user.UserPlan.TrainPlanExpiryDate));
                var newYogaDate = user.UserPlan.YogaPlan == null ? null : SharedUtilities.GetExpiryDate(user.UserPlan.YogaPlan, DateTime.Parse(user.UserPlan.YogaPlanExpiryDate));
                var newPilatesDate = user.UserPlan.PilatesPlan == null ? null : SharedUtilities.GetExpiryDate(user.UserPlan.PilatesPlan, DateTime.Parse(user.UserPlan.PilatesPlanExpiryDate));

                var batch = CrossCloudFirestore.Current.Instance.Batch();
                var changeTrain = user.UserPlan.TrainPlan != null && !train && user.UserPlan.TrainAutoRenewal == 1;
                if (changeTrain)
                {
                    train = true;
                    batch.Update(userDoc, new FieldPath("UserPlan", "TrainPlanExpiryDate"), newTrainDate);
                }

                var changeYoga = user.UserPlan.YogaPlan != null && !yoga && user.UserPlan.YogaAutoRenewal == 1;
                if (changeYoga)
                {
                    yoga = true;
                    batch.Update(userDoc, new FieldPath("UserPlan", "YogaPlanExpiryDate"), newYogaDate);
                }

                var changePilates = user.UserPlan.PilatesPlan != null && !pilates && user.UserPlan.PilatesAutoRenewal == 1;
                if (changePilates)
                {
                    pilates = true;
                    batch.Update(userDoc, new FieldPath("UserPlan", "PilatesPlanExpiryDate"), newPilatesDate);
                }

                if (changeYoga || changeTrain || changePilates)
                {
                    SharedUtilities.UpdateExpiryResumeWithBatch(batch,
                        new ExpiryResume.Resume //old
                        {
                            UserID = user.UserID,
                            ExpiryDate = user.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = user.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = user.UserPlan.PilatesPlanExpiryDate
                        },
                        new ExpiryResume.Resume //new
                        {
                            UserID = user.UserID,
                            ExpiryDate = changeTrain ? newTrainDate : user.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = changeYoga ? newYogaDate : user.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = changePilates ? newPilatesDate : user.UserPlan.PilatesPlanExpiryDate
                        });
                    Task.Run(async () => await batch.CommitAsync());


                    _app.LoggedInUser.UserPlan.TrainPlanExpiryDate = changeTrain ? newTrainDate : user.UserPlan.TrainPlanExpiryDate;
                    _app.LoggedInUser.UserPlan.YogaPlanExpiryDate = changeYoga ? newYogaDate : user.UserPlan.YogaPlanExpiryDate;
                    _app.LoggedInUser.UserPlan.PilatesPlanExpiryDate = changePilates ? newPilatesDate : user.UserPlan.PilatesPlanExpiryDate;
                }

                return train || yoga || pilates;
            }

            public async static Task<bool> LockUserPlan(User foundUser, IDocumentReference query)
            {
                try
                {
                    /* OLD METHOD 
                     * 
                    var todayDate = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd");
                    var userClasses = SharedUtilities.FormattUserClassesWithExceptions(foundUser);

                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    batch.Update(query, "PlanAbscence", 1);
                    batch.Update(query, "PlanAbscenceDate", todayDate);
                    batch.Update(query, "ClassesExceptions", new List<string>());

                    foreach (var c in userClasses)
                    {
                        var path = c.Split('@')[1];
                        var docDetails = path.Split('/');
                        var classDoc = CrossCloudFirestore.Current.Instance.Collection("classes").Document(path);

                        var rSchQuery = await CrossCloudFirestore.Current
                                        .Instance.Collection("real_schedules")
                                        .Document(((int)DateTime.Parse(docDetails[0]).DayOfWeek).ToString())
                                        .GetAsync();
                        var data = rSchQuery.ToObject<SchedulesByDayOfWeek>();

                        await SharedUtilities.FixDataInconsistency(data, rSchQuery);
                        var selectedClass = data.Classes.Find(cl => cl.Time == docDetails[1] && cl.Type == docDetails[2]);

                        SharedUtilities.UpdateRealScheduleDocWithBatch(selectedClass, batch, foundUser.UserID, false);
                        batch.Update(classDoc, "StudentsIDs", FieldValue.ArrayRemove(foundUser.UserID));
                    }
                    await batch.CommitAsync();

                    foundUser.PlanAbscence = 1;
                    foundUser.PlanAbscenceDate = todayDate;
                    foundUser.ClassesExceptions = new List<string>();
                    */
                    await Task.Delay(5);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public static void AddPlanExpiryNotifications()
            {
                if (_app.LoggedInUser.UserPlan.TrainPlan != null && !_app.LoggedInUser.UserPlan.TrainPlan.IsFloating)
                {
                    var train_date = DateTime.Parse(_app.LoggedInUser.UserPlan.TrainPlanExpiryDate);
                    CrossLocalNotifications.Current.Show("Vencimento", "Você tem menos de 10 dias para renovar o seu plano!", 101, train_date.AddDays(-10));
                }
                if (_app.LoggedInUser.UserPlan.YogaPlan != null && !_app.LoggedInUser.UserPlan.YogaPlan.IsFloating)
                {
                    var yoga_date = DateTime.Parse(_app.LoggedInUser.UserPlan.YogaPlanExpiryDate);
                    CrossLocalNotifications.Current.Show("Vencimento", "menos de 10 dias para renovar o seu plano!", 102, yoga_date.AddDays(-10));
                }
                if (_app.LoggedInUser.UserPlan.PilatesPlan != null && !_app.LoggedInUser.UserPlan.PilatesPlan.IsFloating)
                {
                    var pilates_date = DateTime.Parse(_app.LoggedInUser.UserPlan.PilatesPlanExpiryDate);
                    CrossLocalNotifications.Current.Show("Vencimento", "menos de 10 dias para renovar o seu plano!", 103, pilates_date.AddDays(-10));
                }
            }
            public static void ClearNotifications()
            {
                if (_app.LoggedInUser.UserPlan.TrainPlan != null)
                    CrossLocalNotifications.Current.Cancel(101);
                if (_app.LoggedInUser.UserPlan.YogaPlan != null)
                    CrossLocalNotifications.Current.Cancel(102);
                if (_app.LoggedInUser.UserPlan.PilatesPlan != null)
                    CrossLocalNotifications.Current.Cancel(103);
            }

            //------ PLAN RELATED FUNCTIONS ------ 


            //------ OTHERS ------ 

            public async static Task<bool> EventsPresenceSetup(int userID, int eventID, string type)
            {
                try
                {
                    if(type == "add")
                    {
                        await CrossCloudFirestore.Current.Instance
                                        .Collection("events")
                                        .Document(eventID.ToString())
                                        .UpdateAsync("ConfirmedUsers", FieldValue.ArrayUnion(userID));

                        _app.SavedEvents.Find(e => e.ID == eventID).ConfirmedUsers.Add(userID);

                    }
                    else if(type == "remove")
                    {
                        await CrossCloudFirestore.Current.Instance
                                        .Collection("events")
                                        .Document(eventID.ToString())
                                        .UpdateAsync("ConfirmedUsers", FieldValue.ArrayRemove(userID));

                        _app.SavedEvents.Find(e => e.ID == eventID).ConfirmedUsers.Remove(userID);
                    }

                    _app.SavedEvents = _app.SavedEvents;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public static void Loggout()
            {
                //-- RESET USER DOC --

                UserDocumentListener.Remove();
                UserDocumentListener = null;
                UserDocument = null;

                //-- RESET USER DOC --

                //-- RESET CLASS REFERENCES --

                foreach (var reference in TemporaryUserClassReferences)
                {
                    reference.Value.listener.Remove();
                }
                TemporaryUserClassReferences = new Dictionary<string, ClassReference>();

                //-- RESET CLASS REFERENCES --

                //-- REMOVE NOTIFICATIONS --

                ClearNotifications();

                //-- REMOVE NOTIFICATIONS --
            }

            //------ OTHERS ------ 
        }
        public class SharedUtilities
        {
            //----- PROFILE PICTURE FUNCTIONS -----
            public const int DEFAULT_TIME_LIMIT = 3;
            public const string DefaultPictureToken = "https://firebasestorage.googleapis.com/v0/b/ctf-project-fca7f.appspot.com/o/ProfilePictures%2Fempty.jpg?alt=media&token=2993b07f-bebe-4cca-82ea-e98c0b7b8db8";
            public static MediaFile TemporaryProfilePicture;

            public async static Task<User> DownloadUserAndFixInconsistencies(int id)
            {
                try
                {
                    var query = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("users")
                                        .Document(id.ToString())
                                        .GetAsync();

                    if (query.Exists)
                    {
                        var user = query.ToObject<User>();

                        await RemoveOldClassesExceptions(user);
                        await RemoveOutdatedMakeupClasses(user);

                        return user;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }

            public async static Task<string> UploadImage(int id)
            {
                if (TemporaryProfilePicture != null)
                {
                    try
                    {
                        var image = await new FirebaseStorage("ctf-project-fca7f.appspot.com")
                                        .Child("ProfilePictures")
                                        .Child(id.ToString())
                                        .PutAsync(TemporaryProfilePicture.GetStream());

                        TemporaryProfilePicture = null;
                        return image;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        TemporaryProfilePicture = null;
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
            public async static Task DeleteProfilePicture(int id)
            {
                try
                {
                    await new FirebaseStorage("ctf-project-fca7f.appspot.com")
                                    .Child("ProfilePictures")
                                    .Child(id.ToString())
                                    .DeleteAsync();
                }
                catch
                {
                    try
                    {
                        await new FirebaseStorage("ctf-project-fca7f.appspot.com")
                                    .Child("ProfilePictures")
                                    .Child(id.ToString())
                                    .DeleteAsync();
                    }
                    catch
                    {
                        Console.WriteLine("unable to remove profile picture");
                    }
                }
            }

            //----- PROFILE PICTURE FUNCTIONS -----

            //----- TEACHER FUNCTIONS -----


            //----- USERS RESUME FUNCTIONS -----

            public class UsersResume
            {
                public List<SimplifiedUser> Users { get; set; }
            };
            public static void AddResumeDocListener(IDocumentReference doc)
            {
                try
                {
                    doc.AddSnapshotListener(async (snp, error) =>
                    {
                        try
                        {
                            var resume = snp.ToObject<UsersResume>();
                            if (resume != null && !snp.Metadata.IsFromCache && resume != _app.UsersResume)
                            {
                                var listOfIds = new List<int>();
                                var listOfDuplicates = new List<SimplifiedUser>();
                                resume.Users.ForEach(u =>
                                {
                                    if (listOfIds.Contains(u.UserID) && !listOfDuplicates.Any(r => r.UserID == u.UserID))
                                        listOfDuplicates.AddRange(resume.Users.FindAll(r => r.UserID == u.UserID));
                                    else
                                        listOfIds.Add(u.UserID);
                                });

                                if (listOfDuplicates.Count > 0 && !snp.Metadata.HasPendingWrites)
                                {
                                    await FixResumeDuplicates(listOfDuplicates);
                                    return;
                                }

                                _app.UsersResume = resume;
                                await _app.SavePropertiesAsync();

                                MessagingCenter.Send(new PageUpdateMessage(), "UpdateStudentSelectionPage");
                            }
                            else if (resume != null && snp.Metadata.IsFromCache && !snp.Metadata.HasPendingWrites && resume != _app.UsersResume)
                            {
                                var resume_query = await CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("users")
                                        .Document("resume")
                                        .GetAsync();
                                //Reorder by name
                                var resumeData = resume_query.ToObject<UsersResume>();
                                resumeData.Users = resumeData.Users.OrderBy(u => u.Name).ToList();

                                _app.UsersResume = resumeData;
                                await _app.SavePropertiesAsync();
                            }
                        } catch(Exception e) { Console.Write("Error on resume doc: " + e + "\n"); }
                    });                        
                } catch (Exception e) { Console.Write("Error setting resume listener: " + e + "\n"); } 
            }

            //----- USERS RESUME FUNCTIONS -----


            //----- DATE RELATED FUNCTIONS -----

            public static string IntToWeekday(int i)
            {
                string name = "";
                switch (i)
                {
                    case 0:
                        name = "Domingo";
                        break;
                    case 1:
                        name = "Segunda-Feira";
                        break;
                    case 2:
                        name = "Terça-Feira";
                        break;
                    case 3:
                        name = "Quarta-Feira";
                        break;
                    case 4:
                        name = "Quinta-Feira";
                        break;
                    case 5:
                        name = "Sexta-Feira";
                        break;
                    case 6:
                        name = "Sábado";
                        break;
                }
                return name;
            }
            public static DateTime GetTodayDateTime()
            {
                TimeZoneInfo spZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                DateTime todayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spZone);
                if (spZone.IsDaylightSavingTime(todayDate))
                    todayDate = todayDate.AddHours(-1);

                return todayDate;
            }
            public static string GetExpiryDate(Plan p, DateTime lastDate)
            {
                switch (p.Duration)
                {
                    case "Mensal":
                        lastDate = lastDate.AddMonths(1);
                        break;
                    case "Trimestral":
                        lastDate = lastDate.AddMonths(3);
                        break;
                    case "Semestral":
                        lastDate = lastDate.AddMonths(6);
                        break;
                    case "Anual":
                        lastDate = lastDate.AddYears(1);
                        break;
                    default:
                        break;
                }

                return lastDate.ToString("yyyy-MM-dd");
            }
            public static DateTime GetNextDateFromWeekday(int dow)
            {
                var todayDate = GetTodayDateTime();

                int today = (int)todayDate.DayOfWeek;
                int z = dow < today ? 7 - (today - dow) : dow - today;
                DateTime classDay = todayDate.AddDays(z);

                return classDay;
            }

            //----- DATE RELATED FUNCTIONS -----


            //----- UPDATE OUTDATED USERS INFO FUNCTIONS -----

            public async static Task RemoveOldClassesExceptions(User u)
            {
                bool hasToUpdateCEs = false;
                List<string> ceIterations = new List<string>(u.ClassesExceptions);
                foreach (var ce in ceIterations)
                {
                    try
                    {
                        var text = ce.Substring(0, 16);
                        var classData = ce.Replace("@add", "").Replace("@remove", "");

                        var fce = u.ClassesExceptions.FindAll(c => c.StartsWith(classData));
                        if(fce.Count > 1)
                        {
                            hasToUpdateCEs = true;
                            u.ClassesExceptions.RemoveAll(c => c.StartsWith(text));
                            continue;
                        }
                        else if(fce.Count == 0)
                        {
                            continue;
                        }

                        var todayDate = GetTodayDateTime();
                        DateTime classDate = DateTime.ParseExact(text, "yyyy-MM-dd/HH:mm", CultureInfo.InvariantCulture);
                        if (classDate.Date < todayDate.Date)
                        {
                            hasToUpdateCEs = true;
                            u.ClassesExceptions.Remove(ce);
                        }
                    }
                    catch { Console.WriteLine("error at removeoldclasses!"); }
                }
                if (hasToUpdateCEs)
                {
                    await CrossCloudFirestore.Current.Instance.Collection("users").Document(u.UserID.ToString()).UpdateAsync("ClassesExceptions", u.ClassesExceptions);
                }
            }
            public async static Task RemoveOutdatedMakeupClasses(User u)
            {
                try
                {
                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(u.UserID.ToString());
                    var todayDate = GetTodayDateTime();

                    var planList = new List<Plan>()
                    {
                        u.UserPlan.TrainPlan,
                        u.UserPlan.YogaPlan,
                        u.UserPlan.PilatesPlan,
                        //u.UserPlan.WeightLifitingPlan
                    };

                    var datesList = new List<List<string>>()
                    {
                        u.MCTrainDates,
                        u.MCYogaDates,
                        u.MCPilatesDates,
                        //u.UserPlan.WeightLifitingDates
                    };

                    var mcCountList = new List<int>()
                    {
                        u.MakeupClasses,
                        u.MakeupClassesYoga,
                        u.MakeupClassesPilates,
                        //u.UserPlan.WeightLifitingPlan
                    };

                    var expiryDates = new List<string>()
                    {
                        u.UserPlan.TrainPlanExpiryDate,
                        u.UserPlan.YogaPlanExpiryDate,
                        u.UserPlan.PilatesPlanExpiryDate,
                        //u.UserPlan.WeightLifitingPlan
                    };

                    var fieldNames = new List<string>()
                    {
                        "MakeupClasses",
                        "MCTrainDates",
                        "MakeupClassesYoga",
                        "MCYogaDates",
                        "MakeupClassesPilates",
                        "MCPilatesDates",
                        //u.UserPlan.WeightLifitingPlan
                    };

                    for (int i = 0; i < planList.Count; i++)
                    {
                        bool hasToUpdate = false;
                        var plan = planList[i];
                        var dates = datesList[i];
                        var mcCount = mcCountList[i];

                        if (plan != null && !plan.IsFloating)
                        {
                            dates.Sort();
                            if (mcCount != dates.Count)
                            {
                                if (mcCount < dates.Count)
                                    while (mcCount != dates.Count)
                                    {
                                        if (dates.Count > 0)
                                            dates.RemoveAt(0);
                                        else
                                        {
                                            mcCount = 0;
                                            break;
                                        }
                                    }
                                else mcCount = dates.Count;

                                hasToUpdate = true;
                            }
                            if (mcCount > 0)
                            {
                                List<string> tempDatesList = new List<string>(dates);
                                int z = 0;
                                int removalCount = 0;
                                foreach (var date in tempDatesList)
                                {
                                    try
                                    {
                                        var text = date.Substring(0, 10);
                                        DateTime classDate = DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                                        if (classDate.AddDays(30).Date < todayDate.Date)
                                        {
                                            dates.RemoveAt(z - removalCount);
                                            mcCount--;
                                            removalCount++;
                                            hasToUpdate = true;
                                        }
                                    }
                                    catch { Console.WriteLine("error at remove outdated mc!"); }
                                    z++;
                                }
                            }
                        }
                        else if (plan != null && plan.IsFloating)
                        {
                            var expiryDate = expiryDates[i];
                            if (DateTime.ParseExact(expiryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) < todayDate)
                            {
                                dates.Clear();
                                mcCount = 0;

                                hasToUpdate = true;
                            }
                        }
                        if (hasToUpdate)
                        {
                            planList[i] = plan;
                            datesList[i] = dates;
                            mcCountList[i] = mcCount;

                            batch.Update(userDoc, fieldNames[i * 2], mcCount);
                            batch.Update(userDoc, fieldNames[i * 2 + 1], dates);
                        }
                    }

                    //TODO: add weightlifiting
                    u.UserPlan.TrainPlan = planList[0];
                    u.UserPlan.YogaPlan = planList[1];
                    u.UserPlan.PilatesPlan = planList[2];

                    u.MakeupClasses = mcCountList[0];
                    u.MakeupClassesYoga = mcCountList[1];
                    u.MakeupClassesPilates = mcCountList[2];

                    u.MCTrainDates = datesList[0];
                    u.MCYogaDates = datesList[1];
                    u.MCPilatesDates = datesList[2];
                    
                    await batch.CommitAsync();
                }
                catch { Console.WriteLine("exceção"); }
            }

            //----- UPDATE OUTDATED USERS INFO FUNCTIONS -----

            public async static Task<bool> GenerateClassFromSchedules(string time, string type, bool removeOldClass, IDocumentReference docRef, SchedulesByDayOfWeek selectedWeekdaySchedules, IWriteBatch batch)
            {
                try
                {
                    var base_schedules_query = await CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("schedules")
                                    .WhereEqualsTo("Time", time)
                                    .WhereEqualsTo("Type", type)
                                    .GetAsync();

                    var id = -1;
                    if (removeOldClass)
                    {
                        id = selectedWeekdaySchedules.Classes.FindIndex(cl => cl.Time == time && cl.Type == type);
                        batch.Update(docRef, "Classes", FieldValue.ArrayRemove(selectedWeekdaySchedules.Classes[id]));
                    }
                    if (base_schedules_query.Count == 1 && base_schedules_query != null)
                    {
                        Schedule foundSchedule = base_schedules_query.ToObjects<Schedule>().Single();
                        var foundClass = foundSchedule.Classes.Find(cl => cl.Day == selectedWeekdaySchedules.DayOfWeek);
                        if (foundClass == null)
                        {
                            batch.Update(docRef, "ClassesTimeAndType", FieldValue.ArrayRemove(time + '@' + type));

                            Console.WriteLine("Aula não encontrada no Schedule: " + foundSchedule.Time + "/" + foundSchedule.Type);
                            return false;
                        }

                        DateTime classDay = GetNextDateFromWeekday(foundClass.Day); 
                        string classDayString = classDay.ToString("yyyy-MM-dd");
                        string docPath = classDayString + "/" + foundSchedule.Time + "/" + foundSchedule.Type;

                        foundClass.StudentsList.RemoveAll(i => 
                            _app.UsersResume.Users.Find(u => u.UserID == i) == null || 
                            _app.UsersResume.Users.Find(u => u.UserID == i).PlanAbscence == 1
                        );

                        SimpleClass newClass = new SimpleClass();
                        newClass.FromSchedules(foundSchedule, foundClass, classDayString);

                        var newTimes = new SchedulesByDayOfWeek.Times
                        {
                            StudentsList = newClass.StudentsIDs,
                            Time = newClass.Time,
                            Type = newClass.Type,
                            Date = classDayString
                        };

                        var classDoc = CrossCloudFirestore.Current.Instance.Collection("classes").Document(docPath);
                        batch.Set(classDoc, newClass);
                        batch.Update(docRef, "Classes", FieldValue.ArrayUnion(newTimes));

                        if (removeOldClass && id != -1) selectedWeekdaySchedules.Classes[id] = newTimes;
                        else if(!removeOldClass) selectedWeekdaySchedules.Classes.Add(newTimes);
                    }
                    else if (base_schedules_query == null && !removeOldClass) { batch.Update(docRef, "ClassesTimeAndType", FieldValue.ArrayRemove(time + '@' + type)); } //didn't find schedule doc <- remove ClassesTimeAndType
                    else { await AdmUtilities.SaveErrorInServer("Found more than one schedule per time/type, at MainContent/Classes when updating outdated classes"); } //found more than one schedule doc

                    return true;
                }
                catch (Exception e) 
                {
                    await AdmUtilities.SaveErrorInServer(e.ToString());
                    Console.WriteLine(e); 
                }
                return false;
            }
            public async static Task<SchedulesByDayOfWeek> UpdateOutdatedRealschedules(IDocumentReference docRef, int wd)
            {
                try
                {
                    var query = await docRef.GetAsync();
                    SchedulesByDayOfWeek selectedWeekdaySchedules = query.ToObject<SchedulesByDayOfWeek>();

                    /* Finds if any of the downloaded classes is outdated and update it 
                     * if nescessary. */
                    if (selectedWeekdaySchedules != null && selectedWeekdaySchedules.Classes != null)
                    {
                        await FixDataInconsistency(selectedWeekdaySchedules, query);

                        var copiedSelection = new List<SchedulesByDayOfWeek.Times>();
                        selectedWeekdaySchedules.Classes.ForEach(c =>
                        {
                            copiedSelection.Add(new SchedulesByDayOfWeek.Times
                            {
                                Date = c.Date,
                                StudentsList = new List<int>(c.StudentsList),
                                Time = c.Time,
                                Type = c.Type
                            });
                        });

                        var batch = CrossCloudFirestore.Current.Instance.Batch();

                        foreach (var c in copiedSelection)
                        {
                            DateTime dt = DateTime.Parse(c.Date);
                            if (dt < DateTime.Today)
                                await GenerateClassFromSchedules(c.Time, c.Type, true, docRef, selectedWeekdaySchedules, batch);
                        }

                        await batch.CommitAsync();
                    }

                    return selectedWeekdaySchedules;
                }  
                catch (Exception) { return null; }
            }

            public static void UpdateRealScheduleDocWithBatch(SimpleClass selectedClass, IWriteBatch batch, int id, bool adding = true)
            {
                SchedulesByDayOfWeek.Times oldClass = new SchedulesByDayOfWeek.Times
                {
                    Date = selectedClass.Date,
                    StudentsList = new List<int>(selectedClass.StudentsIDs),
                    Time = selectedClass.Time,
                    Type = selectedClass.Type
                };

                if(adding)
                    selectedClass.StudentsIDs.Add(id);
                else
                    selectedClass.StudentsIDs.Remove(id);

                SchedulesByDayOfWeek.Times newClass = new SchedulesByDayOfWeek.Times
                {
                    Date = selectedClass.Date,
                    StudentsList = selectedClass.StudentsIDs,
                    Time = selectedClass.Time,
                    Type = selectedClass.Type
                };

                var realScheduleDoc = CrossCloudFirestore.Current.Instance.Collection("real_schedules").Document(((int)DateTime.Parse(selectedClass.Date).DayOfWeek).ToString());
                batch.Update(realScheduleDoc, "Classes", FieldValue.ArrayRemove(oldClass));
                batch.Update(realScheduleDoc, "Classes", FieldValue.ArrayUnion(newClass));
            }
            public static void UpdateRealScheduleDocWithBatch(SchedulesByDayOfWeek.Times selectedClass, IWriteBatch batch, int id, bool adding = true)
            {
                SchedulesByDayOfWeek.Times oldClass = new SchedulesByDayOfWeek.Times
                {
                    Date = selectedClass.Date,
                    StudentsList = new List<int>(selectedClass.StudentsList),
                    Time = selectedClass.Time,
                    Type = selectedClass.Type
                };

                if(adding)
                    selectedClass.StudentsList.Add(id);
                else
                    selectedClass.StudentsList.Remove(id);

                var realScheduleDoc = CrossCloudFirestore.Current.Instance.Collection("real_schedules").Document(((int) DateTime.Parse(selectedClass.Date).DayOfWeek).ToString());
                batch.Update(realScheduleDoc, "Classes", FieldValue.ArrayRemove(oldClass));
                batch.Update(realScheduleDoc, "Classes", FieldValue.ArrayUnion(selectedClass));
            }

            public static List<string> FormattUserClassesWithExceptions(User myUser)
            {
                List<string> user_schedules = new List<string>();

                foreach (string sr in myUser.ScheduleReferences)
                {
                    //ClassID@DayOfWeek@Time/Type
                    string[] classIndex = sr.Split('@');

                    int DoW = Int32.Parse(classIndex[1]);

                    string classDayString = GetNextDateFromWeekday(DoW).ToString("yyyy-MM-dd");
                    string docPath = classDayString + "/" + classIndex[2];

                    if (!myUser.ClassesExceptions.Any(ce => ce.StartsWith(docPath)))
                    {
                        user_schedules.Add(classIndex[0] + "@" + docPath + "@" + classIndex[2]);
                    }
                }
                myUser.ClassesExceptions.FindAll(ce => ce.EndsWith("add")).ForEach(ce =>
                {
                    user_schedules.Add("empty@" + ce.Replace("@add", "") + "@empty");
                });

                return user_schedules;
            }
            public async static Task CreateClass(string[] classFinder, bool FromAdm = false)
            {
                string docPath = classFinder[1];
                var batch = CrossCloudFirestore.Current.Instance.Batch();

                string classDayString = classFinder[1].Substring(0, 10);
                int DoW = (int)DateTime.Parse(classDayString).DayOfWeek;

                var query_schedule = await CrossCloudFirestore.Current
                        .Instance
                        .Collection("schedules")
                        .Document(classFinder[0])
                        .GetAsync();
                var sch = query_schedule.ToObject<Schedule>();
                var schClass = sch.Classes.Find(wd => wd.Day == DoW);

                schClass.StudentsList.RemoveAll(id => _app.UsersResume.Users.Find(u => u.UserID == id) == null || _app.UsersResume.Users.Find(u => u.UserID == id).PlanAbscence == 1);

                SimpleClass newClass = new SimpleClass();
                newClass.FromSchedules(sch, schClass, classDayString);

                var query_newClass = CrossCloudFirestore.Current
                                .Instance
                                .Collection("classes")
                                .Document(docPath);
                batch.Set(query_newClass, newClass);

                //Create or update the real schedule so it can be used later for finding available classes.
                var query_rsch = CrossCloudFirestore.Current
                                .Instance
                                .Collection("real_schedules")
                                .Document(DoW.ToString());
                var rschDoc = await query_rsch.GetAsync();
                var nCl = new SchedulesByDayOfWeek.Times
                {
                    StudentsList = newClass.StudentsIDs,
                    Time = newClass.Time,
                    Type = newClass.Type,
                    Date = classDayString
                };
                if (rschDoc.Exists)
                {
                    var schByWd = rschDoc.ToObject<SchedulesByDayOfWeek>();
                    var oldClass = schByWd.Classes.Find(c => c.Time == newClass.Time && c.Type == newClass.Type);

                    if(oldClass != null)
                    {
                        batch.Update(query_rsch, "Classes", FieldValue.ArrayRemove(oldClass));
                        batch.Update(query_rsch, "Classes", FieldValue.ArrayUnion(nCl));
                    }
                    else
                    {
                        batch.Update(query_rsch, "Classes", FieldValue.ArrayUnion(nCl));
                        batch.Update(query_rsch, "ClassesTimeAndType", FieldValue.ArrayUnion(nCl.Time + "@" + nCl.Type));

                        batch.Update(query_rsch, "TimesOverview", FieldValue.ArrayUnion(newClass.Time)); //Deprecated
                    }
                }
                else
                {
                    var newDay = new SchedulesByDayOfWeek()
                    {
                        DayOfWeek = DoW,
                        TimesOverview = new List<string>() //Deprecated
                        {
                            nCl.Time
                        },
                        ClassesTimeAndType = new List<string>()
                        {
                            nCl.Time + '@' + nCl.Type
                        },
                        Classes = new List<SchedulesByDayOfWeek.Times>()
                        {
                            nCl
                        }
                    };
                    batch.Set(query_rsch, newDay);
                }
                if (!FromAdm)
                {
                    _app.ApplicationUserData.UserClasses.Add(newClass);
                    UserUtilities.AddClassListener(query_newClass);
                }   
            }
            public async static Task CreateClass(string[] classFinder, bool noBatch, bool FromAdm = false)
            {
                string docPath = classFinder[1];

                string classDayString = classFinder[1].Substring(0, 10);
                int DoW = (int)DateTime.Parse(classDayString).DayOfWeek;

                var query_schedule = await CrossCloudFirestore.Current
                        .Instance
                        .Collection("schedules")
                        .Document(classFinder[0])
                        .GetAsync();
                var sch = query_schedule.ToObject<Schedule>();
                var schClass = sch.Classes.Find(wd => wd.Day == DoW);

                schClass.StudentsList.RemoveAll(id => _app.UsersResume.Users.Find(u => u.UserID == id) == null || _app.UsersResume.Users.Find(u => u.UserID == id).PlanAbscence == 1);

                SimpleClass newClass = new SimpleClass();
                newClass.FromSchedules(sch, schClass, classDayString);

                var query_newClass = CrossCloudFirestore.Current
                                .Instance
                                .Collection("classes")
                                .Document(docPath);
                //batch.Set(query_newClass, newClass);
                await query_newClass.SetAsync(newClass);

                //Create or update the real schedule so it can be used later for finding available classes.
                var query_rsch = CrossCloudFirestore.Current
                                .Instance
                                .Collection("real_schedules")
                                .Document(DoW.ToString());
                var rschDoc = await query_rsch.GetAsync();
                var nCl = new SchedulesByDayOfWeek.Times
                {
                    StudentsList = newClass.StudentsIDs,
                    Time = newClass.Time,
                    Type = newClass.Type,
                    Date = classDayString
                };
                if (rschDoc.Exists)
                {
                    var schByWd = rschDoc.ToObject<SchedulesByDayOfWeek>();
                    var oldClass = schByWd.Classes.Find(c => c.Time == newClass.Time && c.Type == newClass.Type);

                    if (oldClass != null)
                    {
                        await query_rsch.UpdateAsync("Classes", FieldValue.ArrayRemove(oldClass));
                        await query_rsch.UpdateAsync("Classes", FieldValue.ArrayUnion(nCl));

                        /*batch.Update(query_rsch, "Classes", FieldValue.ArrayRemove(oldClass));
                        batch.Update(query_rsch, "Classes", FieldValue.ArrayUnion(nCl));*/
                    }
                    else
                    {
                        await query_rsch.UpdateAsync("ClassesTimeAndType", FieldValue.ArrayUnion(nCl.Time + "@" + nCl.Type));
                        await query_rsch.UpdateAsync("Classes", FieldValue.ArrayUnion(nCl));

                        await query_rsch.UpdateAsync("TimesOverview", FieldValue.ArrayUnion(newClass.Time)); //Deprecated


                        /*batch.Update(query_rsch, "Classes", FieldValue.ArrayUnion(nCl));
                        batch.Update(query_rsch, "TimesOverview", FieldValue.ArrayUnion(newClass.Time));*/
                    }
                }
                else
                {
                    var newDay = new SchedulesByDayOfWeek()
                    {
                        DayOfWeek = DoW,
                        TimesOverview = new List<string>() //Deprecated
                        {
                            nCl.Time
                        }, 
                        ClassesTimeAndType = new List<string>()
                        {
                            nCl.Time + '@' + nCl.Type
                        },
                        Classes = new List<SchedulesByDayOfWeek.Times>()
                        {
                            nCl
                        }
                    };
                    await query_rsch.SetAsync(newDay);
                    //batch.Set(query_rsch, newDay);
                }
                if (!FromAdm)
                {
                    _app.ApplicationUserData.UserClasses.Add(newClass);
                    UserUtilities.AddClassListener(query_newClass);
                }
            }
            public async static Task<SchedulesByDayOfWeek> FixDataInconsistency(SchedulesByDayOfWeek sbdw, IDocumentSnapshot query)
            {
                var batch = CrossCloudFirestore.Current.Instance.Batch();

                /* Remove classes that ended up with the wrong weekday by some reason */
                var copiedSelection = new List<SchedulesByDayOfWeek.Times>();
                sbdw.Classes.ForEach(c =>
                {
                    copiedSelection.Add(new SchedulesByDayOfWeek.Times
                    {
                        Date = c.Date,
                        StudentsList = new List<int>(c.StudentsList),
                        Time = c.Time,
                        Type = c.Type
                    });
                });

                foreach (var c in copiedSelection)
                {
                    try
                    {
                        DateTime dt = DateTime.Parse(c.Date);
                        int weekday = (int)dt.DayOfWeek;

                        if (sbdw.DayOfWeek != weekday)
                        {
                            batch.Update(query.Reference, "Classes", FieldValue.ArrayRemove(c));
                            sbdw.Classes.Remove(sbdw.Classes
                                .Find(cls => cls.Type == c.Type && cls.Time == c.Time && cls.Date == c.Date));
                        }
                    }
                    catch (Exception e) { Console.WriteLine(e); }
                }

                string[] classTypes = new string[3] { "Treino", "Yoga", "Pilates" };
                foreach (var to in sbdw.ClassesTimeAndType)
                {
                    try
                    {
                        var splitTimeAndType = to.Split('@');

                        /* Find if any class is missing through 'ClassesTimeAndType', which may happen
                        * because of some bug somewhere, and try to generate it using schedules. This portion of the 
                        * code should keep everything synched in a worst case scenario... */
                        var foundClasses = sbdw.Classes.FindAll(c => c.Time == splitTimeAndType[0] && c.Type == splitTimeAndType[1]);
                        if (foundClasses == null || foundClasses.Count() == 0)
                        {
                            await GenerateClassFromSchedules(splitTimeAndType[0], splitTimeAndType[1], false, query.Reference, sbdw, batch);
                            continue;
                        }

                        //Remove class duplicates
                        if (foundClasses.Count > 1)
                        {
                            if (foundClasses.Count > 1)
                                foreach (var fc in foundClasses)
                                    batch.Update(query.Reference, "Classes", FieldValue.ArrayRemove(fc));

                            var listOfClasses = new List<SchedulesByDayOfWeek.Times>();
                            listOfClasses.Add(foundClasses.First());

                            var listOfNewTimes = new List<SchedulesByDayOfWeek.Times>();
                            foreach (var typeClass in listOfClasses)
                            {
                                var classDoc = await CrossCloudFirestore.Current.Instance
                                                        .Collection("classes")
                                                        .Document(typeClass.Date + "/" + typeClass.Time + "/" + typeClass.Type)
                                                        .GetAsync();
                                var documentValues = classDoc.ToObject<SimpleClass>();
                                var newTimes = new SchedulesByDayOfWeek.Times
                                {
                                    Type = typeClass.Type,
                                    Time = typeClass.Time,
                                    Date = typeClass.Date,
                                    StudentsList = new List<int>(documentValues.StudentsIDs)
                                };

                                listOfNewTimes.Add(newTimes);
                                batch.Update(query.Reference, "Classes", FieldValue.ArrayUnion(newTimes));
                            }

                            sbdw.Classes = sbdw.Classes.Except(foundClasses).ToList();
                            sbdw.Classes.AddRange(listOfNewTimes);
                        }
                        //Should not happen as well, based on firestore rules, but you never know
                        else if (foundClasses.Count == 1 && foundClasses.First().StudentsList.Count != foundClasses.First().StudentsList.Distinct().Count()) 
                        {
                            var selectedClass = foundClasses[0];
                            if (((int)DateTime.Parse(selectedClass.Date).DayOfWeek).ToString() == query.Id)
                            {
                                var classDoc = await CrossCloudFirestore.Current.Instance
                                                            .Collection("classes")
                                                            .Document(selectedClass.Date + "/" + selectedClass.Time + "/" + selectedClass.Type)
                                                            .GetAsync();
                                var documentValues = classDoc.ToObject<SimpleClass>();
                                var newTimes = new SchedulesByDayOfWeek.Times
                                {
                                    Type = selectedClass.Type,
                                    Time = selectedClass.Time,
                                    Date = selectedClass.Date,
                                    StudentsList = new List<int>(documentValues.StudentsIDs)
                                };

                                //batch.Update(query.Reference, "Classes", sbdw.Classes); //probably doing it wrong right?
                                batch.Update(query.Reference, "Classes", FieldValue.ArrayRemove(selectedClass));
                                batch.Update(query.Reference, "Classes", FieldValue.ArrayUnion(newTimes));

                                sbdw.Classes.Remove(selectedClass);
                                sbdw.Classes.Add(newTimes);
                            }
                        }
                    } 
                    catch (Exception e) { Console.WriteLine(e); return null; }
                }

                await batch.CommitAsync();
                return sbdw;
            }

            public static int GenerateNewID(string idType = "")
            {
                Random random = new Random();
                int id = random.Next(100000, 999999);
                var b = true;
                while (b)
                {
                    id = random.Next(100000, 999999);
                    b = idType == "schedule" ? _app.AdmSchedules.Any(u => u.Id == id) : (_app.UsersResume.Users.Any(u => u.UserID == id) || _app.Teachers.Any(t => t.UserID == id));
                }
                return id;
            }
            public static int GetClassSizeLimitByType(string type)
            {
                if (type == "Treino")
                    return 10;
                else if (type == "Yoga")
                    return 12;
                else if (type == "Pilates")
                    return 2;
                else
                    return 0;
            }
            public static List<SimplifiedUser> GetOrderedByNameUserList(List<int> ids, bool forClassEdit = false)
            {
                try
                {
                    var users = new List<SimplifiedUser>();
                    foreach(var sid in ids)
                    {
                        if (sid.ToString().Length >= 6)
                        {
                            var userToAdd = _app.UsersResume.Users.Find(u => u.UserID == sid);
                            if(userToAdd != null)
                                users.Add(userToAdd);
                            else 
                                RemovePhantomUser(sid);
                        }
                        else if (500 <= sid && sid < 513)
                            if (!forClassEdit)
                                users.Add(new SimplifiedUser() { Name = "Aula Experimental", PictureToken = "" });
                    }
                    users = users.OrderBy(u => u.Name).ToList();

                    return users;
                }
                catch
                {
                    return new List<SimplifiedUser>();
                }
            }

            public async static Task FixResumeDuplicates(List<SimplifiedUser> resumes)
            {
                try
                {
                    var resumeDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document("resume");
                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    while (resumes.Count > 0)
                    {
                        var u = resumes.First();
                        var rList = resumes.FindAll(r => r.UserID == u.UserID);
                        var query = await CrossCloudFirestore.Current.Instance.Collection("users").Document(u.UserID.ToString()).GetAsync();

                        var userData = query.ToObject<User>();

                        var su = new SimplifiedUser()
                        {
                            UserID = userData.UserID,
                            Birthday = userData.Birthday,
                            Name = userData.Name,
                            PictureToken = userData.PictureToken,
                            PlanAbscence = userData.PlanAbscence
                        };

                        rList.ForEach(r =>
                        {
                            batch.Update(resumeDoc, "Users", FieldValue.ArrayRemove(r));
                        });
                        batch.Update(resumeDoc, "Users", FieldValue.ArrayUnion(su));

                        resumes = resumes.Except(rList).ToList();
                    }

                    await batch.CommitAsync();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            public static void RemovePhantomUser(int id)
            {
                Task.Run(async () =>
                {
                    var resume_query = await CrossCloudFirestore.Current
                                                        .Instance
                                                        .Collection("users")
                                                        .Document("resume")
                                                        .GetAsync();
                    //Reorder by name
                    var resume = resume_query.ToObject<UsersResume>();
                    resume.Users = resume.Users.OrderBy(u => u.Name).ToList();
                    _app.UsersResume = resume;

                    var fUser = _app.UsersResume.Users.Find(u => u.UserID == id);
                    if (fUser == null)
                    {
                        await CrossCloudFirestore.Current
                            .Instance
                            .Collection("adm_events")
                            .Document("phantom_users")
                            .UpdateAsync("Users", FieldValue.ArrayUnion(id));
                    }
                    else
                    {
                        MessagingCenter.Send(new PageControlMessage(), "RefreshStudentList");
                        MessagingCenter.Send(new PageControlMessage(), "TodayClassesUpdated");
                    }
                });
            }

            public static void UpdateExpiryResumeWithBatch(IWriteBatch batch, ExpiryResume.Resume oldResume, ExpiryResume.Resume newResume)
            {
                var datesDoc = CrossCloudFirestore.Current.Instance.Collection("adm_events").Document("expiry_dates");
                batch.Update(datesDoc, "DateList", FieldValue.ArrayRemove(oldResume));
                batch.Update(datesDoc, "DateList", FieldValue.ArrayUnion(newResume));
            }
            public async static Task<bool> UpdateUser(User oldUser, User newUser, bool FromUser = false)
            {
                try
                {
                    var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(oldUser.UserID.ToString());
                    var resumeDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document("resume");
                    bool hasToUpdateResume = false;

                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    var newUserResume = new SimplifiedUser()
                    {
                        UserID = oldUser.UserID,
                        Birthday = oldUser.Birthday,
                        Name = oldUser.Name,
                        PictureToken = oldUser.PictureToken
                    };

                    try
                    {
                        if (TemporaryProfilePicture != null)
                        {
                            if (oldUser.PictureToken != "")
                                await DeleteProfilePicture(oldUser.UserID);

                            var uploadResult = await UploadImage(newUser.UserID);
                            newUser.PictureToken = uploadResult;
                            newUserResume.PictureToken = uploadResult;

                            batch.Update(userDoc, "PictureToken", newUser.PictureToken);
                            hasToUpdateResume = true;
                        }
                    }
                    catch { newUser.PictureToken = ""; newUserResume.PictureToken = ""; }

                    if (oldUser.Name != newUser.Name)
                    {
                        newUserResume.Name = newUser.Name;
                        batch.Update(userDoc, "Name", newUser.Name);
                        hasToUpdateResume = true;
                    }
                    
                    if (oldUser.Birthday != newUser.Birthday)
                    {
                        newUserResume.Birthday = newUser.Birthday;
                        batch.Update(userDoc, "Birthday", newUser.Birthday);
                        hasToUpdateResume = true;
                    }

                    if (hasToUpdateResume)
                    {
                        var oldResume = _app.UsersResume.Users.Find(u => u.UserID == newUser.UserID);
                        batch.Update(resumeDoc, "Users", FieldValue.ArrayRemove(oldResume));
                        batch.Update(resumeDoc, "Users", FieldValue.ArrayUnion(newUserResume));
                    }

                    if (oldUser.Gender != newUser.Gender)
                    {
                        batch.Update(userDoc, "Gender", newUser.Gender);
                    }

                    if (oldUser.Email != newUser.Email)
                    {
                        batch.Update(userDoc, "Email", newUser.Email);
                    }

                    if (oldUser.Phone != newUser.Phone)
                    {
                        batch.Update(userDoc, "Phone", newUser.Phone);
                    }

                    if (oldUser.Address != newUser.Address)
                    {
                        batch.Update(userDoc, "Address", newUser.Address);
                    }

                    await batch.CommitAsync();

                    if (FromUser)
                        _app.LoggedInUser = newUser;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        public class AdmUtilities
        {
            //[ID_1] - [ID_2]
            public static bool GetNeedClassSetup(User newUser)
            {
                var classCounter = 0;
                var floatingCounter = 0;
                if (newUser.UserPlan.TrainPlan != null)
                {
                    if (newUser.UserPlan.TrainPlan.IsFloating)
                        floatingCounter++;
                    classCounter++;
                }
                if (newUser.UserPlan.YogaPlan != null)
                {
                    if (newUser.UserPlan.YogaPlan.IsFloating)
                        floatingCounter++;
                    classCounter++;
                }
                if (newUser.UserPlan.PilatesPlan != null)
                {
                    if (newUser.UserPlan.PilatesPlan.IsFloating)
                        floatingCounter++;
                    classCounter++;
                }
                return floatingCounter != classCounter;
            }
            public static bool CanEditSchedules { get; set; }

            public static SchedulesByDayOfWeek TodayClasses { get; set; }
            public async static Task DownloadTodayClasses()
            {
                var query_classes = await CrossCloudFirestore.Current
                                            .Instance
                                            .Collection("real_schedules")
                                            .Document(((int)SharedUtilities.GetTodayDateTime().DayOfWeek).ToString())
                                            .GetAsync();
                var todayClasses = query_classes.ToObject<SchedulesByDayOfWeek>();

                if (todayClasses != null)
                {
                    todayClasses.Classes = todayClasses.Classes.OrderBy(c => c.Time).ToList();
                    TodayClasses = todayClasses;
                }
                else
                {
                    TodayClasses = new SchedulesByDayOfWeek() { DayOfWeek = (int)DateTime.Today.DayOfWeek, Classes = new List<SchedulesByDayOfWeek.Times>() };
                }

                query_classes.Reference.AddSnapshotListener(async (snp, error) =>
                {
                    if (snp != null)
                    {
                        if (!snp.Metadata.IsFromCache)
                        {
                            var classesSnap = snp.ToObject<SchedulesByDayOfWeek>();

                            if (classesSnap != null && classesSnap.Classes != null)
                            {
                                classesSnap.Classes = classesSnap.Classes.OrderBy(c => c.Time).ToList();
                                if (classesSnap != TodayClasses)
                                {
                                    if (!snp.Metadata.HasPendingWrites)
                                        await SharedUtilities.FixDataInconsistency(classesSnap, snp);

                                    var cmd = snp.Metadata.HasPendingWrites ? "" : "notPendingWrites";
                                    TodayClasses = classesSnap;

                                    MessagingCenter.Send(new PageControlMessage() { Command = cmd }, "TodayClassesUpdated");
                                }
                            }
                        }
                    }
                });

                _app.DataStatus = true;
            }
            public static void AddSchedulesListener()
            {
                try //TODO possible crash cause ADM login?
                {
                    CrossCloudFirestore.Current
                           .Instance
                           .Collection("schedules")
                           .ObserveModified()
                           .Subscribe(async documentChange =>
                           {
                               try
                               {
                                   if (documentChange != null)
                                   {
                                       if (!documentChange.Document.Metadata.IsFromCache && !documentChange.Document.Metadata.HasPendingWrites)
                                       {
                                           var newSchedule = documentChange.Document.ToObject<Schedule>();
                                           if (newSchedule != null)
                                           {
                                               var listOfDays = new List<int>();
                                               var hasInconsistencies = false;
                                               for (int i = 0; i < 7; i++)
                                               {
                                                   var foundClasses = newSchedule.Classes.FindAll(c => c.Day == i);
                                                   if (foundClasses != null && foundClasses.Count > 1)
                                                   {
                                                       hasInconsistencies = true;
                                                       listOfDays.Add(i);
                                                   }
                                               }
                                               if (hasInconsistencies)
                                               {
                                                   await FixSchedulesInconsistency(newSchedule, listOfDays);
                                                   return;
                                               }

                                               var foundSchedule = _app.AdmSchedules.Find(s => s.Id == newSchedule.Id);
                                               if (newSchedule != foundSchedule)
                                               {
                                                   _app.AdmSchedules.Remove(_app.AdmSchedules.Find(s => s.Id == newSchedule.Id));
                                                   _app.AdmSchedules.Add(newSchedule);

                                                   _app.AdmSchedules = _app.AdmSchedules.OrderBy(s => s.Time).ToList();
                                                   await _app.SavePropertiesAsync();

                                                   MessagingCenter.Send(new PageControlMessage(), "TEMP_UPDATE_SCHEDULES");
                                               }
                                           }
                                       }
                                   }
                               }
                               catch (Exception e)
                               {
                                   Console.WriteLine("Error at schedules listener: " + e);
                               }
                           });
                } catch(Exception e) { Console.WriteLine("Error while setting schedules listener: " + e); }
            }
            public static void AddExpiryResumeListener()
            {
                CrossCloudFirestore.Current.Instance
                    .Collection("adm_events")
                    .Document("expiry_dates")
                    .AddSnapshotListener(async (snp, error) =>
                    {
                        try
                        {
                            if (snp != null)
                            {
                                var newResume = snp.ToObject<ExpiryResume>();
                                if (newResume != null && newResume != _app.ExpiryResumes)
                                {
                                    var listOfDuplicates = new List<int>();
                                    foreach (var date in newResume.DateList)
                                    {
                                        if (newResume.DateList.FindAll(r => r.UserID == date.UserID).Count > 1)
                                        {
                                            listOfDuplicates.Add(date.UserID);
                                        }
                                    }

                                    if (listOfDuplicates.Count > 0)
                                    {
                                        var doc = CrossCloudFirestore.Current.Instance.Collection("adm_events").Document("expiry_dates");
                                        foreach (var id in listOfDuplicates.Distinct())
                                        {
                                            var batch = CrossCloudFirestore.Current.Instance.Batch();

                                            var query = await CrossCloudFirestore.Current.Instance.Collection("users").Document(id.ToString()).GetAsync();
                                            var downloadedUser = query.ToObject<User>();

                                            foreach (var resume in newResume.DateList.FindAll(c => c.UserID == id))
                                                batch.Update(doc, "DateList", FieldValue.ArrayRemove(resume));

                                            var final_resume = new ExpiryResume.Resume
                                            {
                                                ExpiryDate = downloadedUser.UserPlan.TrainPlanExpiryDate,
                                                ExpiryDateYoga = downloadedUser.UserPlan.YogaPlanExpiryDate,
                                                UserID = downloadedUser.UserID
                                            };

                                            batch.Update(doc, "DateList", FieldValue.ArrayUnion(final_resume));

                                            newResume.DateList.RemoveAll(c => c.UserID == id);
                                            newResume.DateList.Add(final_resume);

                                            await batch.CommitAsync();
                                        }
                                    }
                                } 

                                _app.ExpiryResumes = newResume;
                                await _app.SavePropertiesAsync();
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
            }

            public async static Task FixSchedulesInconsistency(Schedule brokenSchedule, List<int> days)
            {
                try
                {
                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    var scheduleDoc = CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("schedules")
                                    .Document(brokenSchedule.Id.ToString());

                    var scheduleHistoryDoc = CrossCloudFirestore.Current
                                    .Instance
                                    .Collection("adm_events")
                                    .Document("schedules_change_history");
                    var historyQuery = await scheduleHistoryDoc.GetAsync();
                    var history = historyQuery.ToObject<ScheduleHistory>();

                    foreach (var day in days)
                    {
                        var stringEnd = day + "@" + brokenSchedule.Id;
                        var classHistory = history.History.FindAll(s => s.EndsWith(stringEnd));

                        var newClass = new Schedule.Weekday() { Day = day, StudentsList = new List<int>() };
                        foreach (var cmd in classHistory)
                        {
                            newClass.StudentsList.Add(Int32.Parse(cmd.Substring(0, 6)));
                        };

                        brokenSchedule.Classes.FindAll(c => c.Day == day).ForEach(cl =>
                        {
                            batch.Update(scheduleDoc, "Classes", FieldValue.ArrayRemove(cl));
                        });
                        batch.Update(scheduleDoc, "Classes", FieldValue.ArrayUnion(newClass));
                    }

                    await batch.CommitAsync();
                }
                catch
                {

                }
            }

            //User management related functions
            public async static Task<string> CheckIfClassSetupIsAvailable(List<App.SelectedSchedules> _selectedSchedules, int id = -1)
            {
                string msg = "";
                List<string> results = new List<string>();
                foreach (var ss in _selectedSchedules)
                {
                    try
                    {
                        int today = (int)SharedUtilities.GetTodayDateTime().DayOfWeek;
                        int z = ss.Day < today ? 7 - (today - ss.Day) : ss.Day - today;
                        DateTime classDay = DateTime.Today.AddDays(z);
                        string classDayString = classDay.ToString("yyyy-MM-dd");
                        string docPath = classDayString + "/" + ss.Time + "/" + _app.AdmSchedules.Find(s => s.Id == ss.ID).Type;

                        var doc = await CrossCloudFirestore.Current.Instance.Collection("classes").Document(docPath).GetAsync();
                        SimpleClass sc = doc.ToObject<SimpleClass>();

                        if (id != -1)
                            sc.StudentsIDs.Remove(id);

                        if (sc.StudentsIDs.Count >= SharedUtilities.GetClassSizeLimitByType(sc.Type))
                        {
                            results.Add(classDay.ToString("dd/MM") + "@" + SharedUtilities.IntToWeekday(ss.Day));
                            ss.ClassException = docPath + "@remove";
                        }
                    }
                    catch { }
                }

                string dates = "";
                string weekdays = "";

                results.ForEach(r =>
                {
                    var splittenR = r.Split('@');

                    dates += splittenR[0] + ",";
                    weekdays += splittenR[1] + ",";
                });

                string days = "A aula do dia ";
                if (results.Count > 1)
                    days = "As aulas dos dias ";

                weekdays = weekdays.ToLower();

                if (dates != "" && weekdays != "")
                    msg = days + dates.Remove(dates.Length - 1) + " (" + weekdays.Remove(weekdays.Length - 1) +
                    (results.Count > 1 ? " respectivamente) já estão lotadas. " : ") já está lotada. ") +
                    "Adicionar " + results.Count + (results.Count > 1 ? " reposições " : " reposição ") + "para o aluno? @" + results.Count;

                return msg;
            }
            public async static Task<bool> SetPlanAbscence(User u, int value)
            {
                try
                {
                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(u.UserID.ToString());

                    batch.Update(userDoc, "PlanAbscence", value);

                    batch.Update(userDoc, "ClassesExceptions", new List<string>());
                    u.ClassesExceptions = new List<string>();

                    var todayDate = SharedUtilities.GetTodayDateTime();
                    if (value == 1)
                    {
                        u.PlanAbscenceDate = todayDate.ToString("yyyy-MM-dd");
                        batch.Update(userDoc, "PlanAbscenceDate", todayDate.ToString("yyyy-MM-dd"));
                    }
                    else if (value == 0)
                    {
                        DateTime abscenceDate = DateTime.Parse(u.PlanAbscenceDate);

                        if (u.UserPlan.TrainPlan != null)
                        {
                            DateTime trainExpiry = DateTime.Parse(u.UserPlan.TrainPlanExpiryDate);
                            if (trainExpiry > abscenceDate)
                            {
                                TimeSpan dateDiff = trainExpiry - abscenceDate;

                                var finalDate = todayDate.AddDays(dateDiff.Days);
                                batch.Update(userDoc, new FieldPath("UserPlan", "TrainPlanExpiryDate"), finalDate.ToString("yyyy-MM-dd"));
                            }
                        }

                        if (u.UserPlan.YogaPlan != null)
                        {
                            DateTime yogaExpiry = DateTime.Parse(u.UserPlan.YogaPlanExpiryDate);
                            if (yogaExpiry > abscenceDate)
                            {
                                TimeSpan yogaDateDiff = yogaExpiry - abscenceDate;

                                var finalYogaDate = todayDate.AddDays(yogaDateDiff.Days);
                                batch.Update(userDoc, new FieldPath("UserPlan", "YogaPlanExpiryDate"), finalYogaDate.ToString("yyyy-MM-dd"));
                            }
                        }

                        if (u.UserPlan.PilatesPlan != null)
                        {
                            DateTime pilatesExpiry = DateTime.Parse(u.UserPlan.PilatesPlanExpiryDate);
                            if (pilatesExpiry > abscenceDate)
                            {
                                TimeSpan pilatesDateDiff = pilatesExpiry - abscenceDate;

                                var finalPilatesDate = todayDate.AddDays(pilatesDateDiff.Days);
                                batch.Update(userDoc, new FieldPath("UserPlan", "PilatesPlanExpiryDate"), finalPilatesDate.ToString("yyyy-MM-dd"));
                            }
                        }
                    }

                    var resume = _app.UsersResume.Users.Find(us => us.UserID.ToString() == u.UserID.ToString());
                    var newResume = new SimplifiedUser
                    {
                        Birthday = resume.Birthday,
                        Name = resume.Name,
                        UserID = resume.UserID,
                        PictureToken = resume.PictureToken,
                        PlanAbscence = value
                    };

                    var resumeDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document("resume");
                    batch.Update(resumeDoc, "Users", FieldValue.ArrayRemove(resume));
                    batch.Update(resumeDoc, "Users", FieldValue.ArrayUnion(newResume));

                    await SharedUtilities.RemoveOutdatedMakeupClasses(u);
                    await SharedUtilities.RemoveOldClassesExceptions(u);
                    var list = SharedUtilities.FormattUserClassesWithExceptions(u);

                    foreach (var userClass in list)
                    {
                        var path = userClass.Split('@')[1];
                        var classDoc = await CrossCloudFirestore.Current.Instance.Collection("classes").Document(path).GetAsync();

                        if (classDoc.Exists)
                        {
                            string pathAsDate = path.Substring(0, 10).Replace("/", "-");
                            var pathDetails = path.Split('/');

                            var foundClass = classDoc.ToObject<SimpleClass>();

                            if (foundClass.StudentsIDs.Count >= SharedUtilities.GetClassSizeLimitByType(foundClass.Type) && value == 0)
                            {
                                var mc = foundClass.Type == "Treino" ? u.MakeupClasses : foundClass.Type == "Yoga" ? u.MakeupClassesYoga : u.MakeupClassesPilates;
                                mc++;
                                batch.Update(userDoc, foundClass.Type == "Treino" ? "MakeupClasses" : foundClass.Type == "Yoga" ? "MakeupClassesYoga" : "MakeupClassesPilates", mc);
                            }
                            else
                            {
                                if (value == 1)
                                    batch.Update(classDoc.Reference, "StudentsIDs", FieldValue.ArrayRemove(u.UserID));
                                else if (value == 0)
                                    batch.Update(classDoc.Reference, "StudentsIDs", FieldValue.ArrayUnion(u.UserID));

                                SchedulesByDayOfWeek.Times oldClass = new SchedulesByDayOfWeek.Times
                                {
                                    Date = pathAsDate,
                                    StudentsList = new List<int>(foundClass.StudentsIDs),
                                    Time = foundClass.Time,
                                    Type = foundClass.Type
                                };

                                if (value == 1)
                                    foundClass.StudentsIDs.Remove(u.UserID);
                                else if (value == 0)
                                    foundClass.StudentsIDs.Add(u.UserID);

                                SchedulesByDayOfWeek.Times newClass = new SchedulesByDayOfWeek.Times
                                {
                                    Date = pathAsDate,
                                    StudentsList = foundClass.StudentsIDs,
                                    Time = foundClass.Time,
                                    Type = foundClass.Type
                                };

                                var weekDoc = CrossCloudFirestore.Current
                                            .Instance
                                            .Collection("real_schedules")
                                            .Document(((int)DateTime.Parse(pathAsDate).DayOfWeek).ToString());
                                batch.Update(weekDoc, "Classes", FieldValue.ArrayRemove(oldClass));
                                batch.Update(weekDoc, "Classes", FieldValue.ArrayUnion(newClass));
                            }
                        }
                    }

                    await batch.CommitAsync();

                    resume.PlanAbscence = value;
                    _app.UsersResume = _app.UsersResume;

                    await _app.SavePropertiesAsync();

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }

            public async static Task<bool> UpdateUserPlan(User u)
            {
                try
                {
                    if (await RemoveUser(u, true))
                    {
                        u.ClassesExceptions.ForEach(c =>
                        {
                            try
                            {
                                if (c.EndsWith("@add"))
                                {
                                    var type = c.Replace("@add", "").Split('/')[2];

                                    var stringStart = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd");

                                    var selectedInt = 1;
                                    var dateString = stringStart + "@" + selectedInt;
                                    while (type == "Treino" ? u.MCTrainDates.Contains(dateString) :
                                           type == "Yoga" ? u.MCYogaDates.Contains(dateString) :
                                           u.MCPilatesDates.Contains(dateString))
                                    {
                                        selectedInt++;
                                        dateString = stringStart + "@" + selectedInt;
                                    }

                                    if (type == "Treino")
                                    {
                                        u.MCTrainDates.Add(dateString);
                                        u.MakeupClasses++;
                                    }
                                    else if (type == "Yoga")
                                    {
                                        u.MCYogaDates.Add(dateString);
                                        u.MakeupClassesYoga++;
                                    }
                                    else if (type == "Pilates")
                                    {
                                        u.MCPilatesDates.Add(dateString);
                                        u.MakeupClassesPilates++;
                                    }
                                }
                            }
                            catch(Exception e) { Console.WriteLine(e); }
                        });

                        if (await CreateNewUser(u, true))
                        {
                            _app.ClearTemporarySchedules();
                            return true;
                        }
                    }
                throw new Exception();
                }
                catch
                {
                    _app.ClearTemporarySchedules();
                    try
                    {
                        var query = await CrossCloudFirestore.Current.Instance.Collection("users_backup").Document(u.UserID.ToString()).GetAsync();
                        await CreateNewUser(query.ToObject<User>());
                    }
                    catch { }

                    return false;
                }

            }

            public async static Task<bool> CreateNewUser(User u, bool changingPlan = false)
            {
                try
                {
                    if (!await WaitForCanEditSchedules())
                        return false;

                    u.PictureToken = !changingPlan ? await SharedUtilities.UploadImage(u.UserID) : u.PictureToken;
                    var userResume = new SimplifiedUser
                    {
                        UserID = u.UserID,
                        Birthday = u.Birthday,
                        Name = u.Name,
                        PictureToken = u.PictureToken
                    };

                    var tempSchedules = new List<App.SelectedSchedules>[3]
                    {
                        _app.TemporarySelectedSchedules[0],
                        _app.TemporarySelectedSchedules[1],
                        _app.TemporarySelectedSchedules[2]
                    };

                    var usersCollection = CrossCloudFirestore.Current
                                .Instance
                                .Collection("users");
                    var realSchedulesCollection = CrossCloudFirestore.Current
                                .Instance
                                .Collection("real_schedules");
                    var newUserDoc = usersCollection.Document(u.UserID.ToString());
                    var usersResumeDoc = usersCollection.Document("resume");
                    var scheduleHistory = CrossCloudFirestore.Current
                                .Instance
                                .Collection("adm_events")
                                .Document("schedules_change_history");

                    u.ClassesExceptions = new List<string>();
                    u.ScheduleReferences = new List<string>();

                    //--- SERVER SIDE ---

                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    batch.Update(usersResumeDoc, "Users", FieldValue.ArrayUnion(userResume));

                    //-- ADD NEW USER TO SCHEDULE --

                    var listOfClasses = new List<Schedule.Weekday>();
                    var classesToUpdate = new List<string>();
                    for (int i = 0; i <= 2; i++)
                    {
                        try { 
                        string type = i == 0 ? "Treino" :
                                      i == 1 ? "Yoga" :
                                      "Pilates";
                        bool isFloating = i == 0 ? u.UserPlan.TrainPlan != null && u.UserPlan.TrainPlan.IsFloating :
                                          i == 1 ? u.UserPlan.YogaPlan != null && u.UserPlan.YogaPlan.IsFloating :
                                          u.UserPlan.PilatesPlan != null && u.UserPlan.PilatesPlan.IsFloating;
                        if (tempSchedules[i] != null)
                        {
                            int id = 0;
                            foreach (App.SelectedSchedules s in tempSchedules[i])
                            {
                                try
                                {
                                    //-- UPDATE SCHEDULES DOC --

                                    if (s.ID != id)
                                    {
                                        try
                                        {
                                            id = s.ID;
                                            var schedulesToAdd = tempSchedules[i].FindAll(ss => ss.ID == id);
                                            var schedule = _app.AdmSchedules.Find(sch => sch.Id == id);

                                            string path = schedule.Id.ToString();
                                            var classDoc = CrossCloudFirestore.Current
                                                            .Instance
                                                            .Collection("schedules")
                                                            .Document(path);

                                            schedulesToAdd.ForEach(sa =>
                                            {
                                                var selectedClass = schedule.Classes.Find(c => c.Day == sa.Day);
                                                var oldClass = new Schedule.Weekday()
                                                {
                                                    Day = selectedClass.Day,
                                                    StudentsList = new List<int>(selectedClass.StudentsList)
                                                };
                                                var newClass = new Schedule.Weekday()
                                                {
                                                    Day = selectedClass.Day,
                                                    StudentsList = new List<int>(selectedClass.StudentsList)
                                                };
                                                newClass.StudentsList.Add(u.UserID);

                                                listOfClasses.Add(selectedClass);

                                                u.ScheduleReferences.Add(schedule.Id + "@" + selectedClass.Day + "@" + schedule.Time + "/" + schedule.Type);
                                                batch.Update(classDoc, "Classes", FieldValue.ArrayRemove(oldClass));
                                                batch.Update(classDoc, "Classes", FieldValue.ArrayUnion(newClass));
                                                batch.Update(scheduleHistory, "History", FieldValue.ArrayUnion(u.UserID + "@" + sa.Day + "@" + id));
                                            });
                                        }
                                        catch (Exception e)
                                        {
                                            Console.Write("Unable to add user to schedules: " + e + "\n");
                                        }
                                    }

                                    //-- UPDATE SCHEDULES DOC --

                                    //-- ADD MAKEUP CLASSES INSTEAD IF CLASS IS FULL --

                                    if (!string.IsNullOrEmpty(s.ClassException))
                                    {
                                        u.ClassesExceptions.Add(s.ClassException);
                                    }

                                    //-- ADD MAKEUP CLASSES INSTEAD IF CLASS IS FULL --

                                    //-- CHECK IF CLASS EXISTS AND ADD USER TO IT --

                                    else
                                    {
                                        var doc = await CrossCloudFirestore.Current
                                                            .Instance
                                                            .Collection("real_schedules")
                                                            .Document(s.Day.ToString()).GetAsync();
                                        var data = doc.ToObject<SchedulesByDayOfWeek>();

                                        var sClass = data.Classes.Find(c => c.Time == s.Time && c.Type == type);
                                        if (sClass == null || sClass.StudentsList.Count < 1 || DateTime.Parse(sClass.Date).Date < DateTime.Today.Date)
                                        {
                                            classesToUpdate.Add(s.ID + "@" + s.Day + "@" + s.Time + "/" + type);
                                        }
                                        else
                                        {
                                            var classDoc = CrossCloudFirestore.Current
                                                               .Instance
                                                               .Collection("classes")
                                                               .Document(sClass.Date + "/" + sClass.Time + "/" + sClass.Type);

                                            var oldClass = new SchedulesByDayOfWeek.Times
                                            {
                                                Time = sClass.Time,
                                                Type = sClass.Type,
                                                Date = sClass.Date,
                                                StudentsList = new List<int>(sClass.StudentsList)
                                            };
                                            sClass.StudentsList.Add(u.UserID);

                                            batch.Update(doc.Reference, "Classes", FieldValue.ArrayUnion(sClass));
                                            batch.Update(doc.Reference, "Classes", FieldValue.ArrayRemove(oldClass));
                                            batch.Update(classDoc, "StudentsIDs", FieldValue.ArrayUnion(u.UserID));
                                        }
                                    }

                                    //-- CHECK IF CLASS EXISTS AND ADD USER TO IT --
                                }
                                catch (Exception e) { Console.WriteLine(e); }
                            }
                        }  
                        else // FLOATING PLAN
                        {
                            //[ID_1] first makeupclasses available
                            //TODO MCYogaDates <->
                            if (i == 0 && isFloating)
                            {
                                var mcTrain = u.UserPlan.TrainPlan.TimesPerWeek * 4;
                                u.MakeupClasses = mcTrain;
                                for (int x = 1; x <= mcTrain; x++)
                                    u.MCTrainDates.Add(SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd") + '@' + x);
                            }
                            if (i == 1 && isFloating)
                            {
                                var mcYoga = u.UserPlan.YogaPlan.TimesPerWeek * 4;
                                u.MakeupClassesYoga = mcYoga;
                                for (int x = 1; x <= mcYoga; x++)
                                    u.MCYogaDates.Add(SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd") + '@' + x);
                            }
                            /* [ID_2] */
                            if (i == 2 && u.UserPlan.PilatesPlan != null && u.UserPlan.PilatesPlan.IsFloating)
                            {
                                var mcPilates = u.UserPlan.PilatesPlan.TimesPerWeek * 4;
                                u.MakeupClassesPilates = mcPilates;
                                for (int x = 1; x <= mcPilates; x++)
                                    u.MCPilatesDates.Add(SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd") + '@' + x);
                            }
                        }
                        } catch (Exception e) { Console.WriteLine(e); }
                    }

                    //-- ADD NEW USER TO SCHEDULE --

                    batch.Set(CrossCloudFirestore.Current.Instance
                            .Collection("users")
                            .Document(u.UserID.ToString()), u);
                    await batch.CommitAsync();
                    batch = null;

                    try //batch wasn't working???
                    {
                        var datesDoc = CrossCloudFirestore.Current.Instance.Collection("adm_events").Document("expiry_dates");
                        var newDate = new ExpiryResume.Resume()
                        {
                            UserID = u.UserID,
                            ExpiryDate = u.UserPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = u.UserPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = u.UserPlan.PilatesPlanExpiryDate
                        };
                        await datesDoc.UpdateAsync("DateList", FieldValue.ArrayUnion(newDate));
                    }
                    catch (Exception e) { Console.WriteLine(e); }

                    classesToUpdate = SharedUtilities.FormattUserClassesWithExceptions(new User { ScheduleReferences = classesToUpdate, ClassesExceptions = u.ClassesExceptions });
                    foreach (var c in classesToUpdate)
                        try 
                        { 
                            await SharedUtilities.CreateClass(c.Split('@'), true, true); 
                        } catch (Exception) { }

                    //--- SERVER SIDE ---


                    //--- LOCAL SIDE ---

                    listOfClasses.ForEach(c =>
                    {
                        c.StudentsList.Add(u.UserID);
                    });

                    _app.AdmSchedules = _app.AdmSchedules;
                    _app.ClearTemporarySchedules();

                    await _app.SavePropertiesAsync();

                    //--- LOCAL SIDE ---

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("erro: criação" + e);
                    _app.ClearTemporarySchedules();
                    return false;
                }
            }
            public async static Task<bool> RemoveUser(User user, bool changingPlan = false)
            {
                try
                {
                    if (!await WaitForCanEditSchedules())
                        return false;

                    var userCollection = CrossCloudFirestore.Current.Instance.Collection("users");
                    var userBackupCollection = CrossCloudFirestore.Current.Instance.Collection("users_backup");
                    var classesCollection = CrossCloudFirestore.Current.Instance.Collection("classes");
                    var realSchedulesCollection = CrossCloudFirestore.Current.Instance.Collection("real_schedules");

                    var backupDoc = userBackupCollection.Document(user.UserID.ToString());

                    var userDoc = userCollection.Document(user.UserID.ToString());
                    var resumeDoc = userCollection.Document("resume");

                    var scheduleHistory = CrossCloudFirestore.Current
                                .Instance
                                .Collection("adm_events")
                                .Document("schedules_change_history");

                    var pathList = SharedUtilities.FormattUserClassesWithExceptions(user);
                    var foundResume = _app.UsersResume.Users.Find(u => u.UserID == user.UserID);

                    //--- SERVER SIDE ---

                    //Pre-removal Backup
                    try { await backupDoc.SetAsync(user); } catch (Exception) { return false; } //LAST-UPDATE

                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    batch.Delete(userDoc);
                    batch.Update(resumeDoc, "Users", FieldValue.ArrayRemove(foundResume)); //LAST-UPDATE

                    /*new SimplifiedUser
                    {
                        UserID = user.UserID,
                        Birthday = user.Birthday,
                        Name = user.Name,
                        PictureToken = user.PictureToken
                    })*/

                    //Real schedules
                    var listOfClasses = new List<Schedule.Weekday>();
                    user.ScheduleReferences.ForEach(sr =>
                    {
                        try
                        {
                            var splittenSr = sr.Split('@');
                            var scheduleDoc = CrossCloudFirestore.Current
                                                        .Instance
                                                        .Collection("schedules")
                                                        .Document(splittenSr[0]);

                            var schedule = _app.AdmSchedules.Find(s => s.Id.ToString() == splittenSr[0]);
                            if (schedule != null)
                            {
                                var c = schedule.Classes.Find(cl => cl.Day.ToString() == splittenSr[1]);
                                if (c != null)
                                {
                                    var oldClass = new Schedule.Weekday()
                                    {
                                        Day = c.Day,
                                        StudentsList = new List<int>(c.StudentsList)
                                    };
                                    var newClass = new Schedule.Weekday()
                                    {
                                        Day = c.Day,
                                        StudentsList = new List<int>(c.StudentsList)
                                    };
                                    newClass.StudentsList.Remove(user.UserID);
                                    listOfClasses.Add(c);

                                    batch.Update(scheduleDoc, "Classes", FieldValue.ArrayUnion(newClass));
                                    batch.Update(scheduleDoc, "Classes", FieldValue.ArrayRemove(oldClass));
                                    batch.Update(scheduleHistory, "History", FieldValue.ArrayRemove(user.UserID + "@" + c.Day + "@" + schedule.Id));
                                }
                            }
                        } catch (Exception) { }
                    });

                    //Class
                    foreach (var r in pathList)
                    {
                        try
                        {
                            var finalPath = r.Split('@')[1];

                            var query = await classesCollection.Document(finalPath).GetAsync();
                            var data = query.ToObject<SimpleClass>();

                            SharedUtilities.UpdateRealScheduleDocWithBatch(data, batch, user.UserID, false);
                            batch.Update(query.Reference, "StudentsIDs", FieldValue.ArrayRemove(user.UserID));
                        } catch (Exception) { }
                    };

                    //Expiry dates resume
                    var expiryDoc = CrossCloudFirestore.Current.Instance.Collection("adm_events").Document("expiry_dates");
                    batch.Update(expiryDoc, "DateList", FieldValue.ArrayRemove(_app.ExpiryResumes.DateList.Find(d => d.UserID == user.UserID)));

                    await batch.CommitAsync();
                    if (user.PictureToken != "" && !changingPlan)
                        await SharedUtilities.DeleteProfilePicture(user.UserID);

                    //--- SERVER SIDE ---


                    //--- LOCAL SIDE ---

                    listOfClasses.ForEach(c =>
                    {
                        c.StudentsList.Remove(user.UserID);
                    });

                    List<SimplifiedUser> users = _app.UsersResume.Users;
                    users.Remove(users.Find(u => u.UserID == user.UserID));

                    _app.UsersResume = _app.UsersResume;
                    _app.AdmSchedules = _app.AdmSchedules;

                    //--- LOCAL SIDE ---

                    return true;
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    return false;
                }
            }

            const int maxTries = 50;
            static int tryCount = 0;
            public async static Task<bool> WaitForCanEditSchedules()
            {
                while (!CanEditSchedules)
                {
                    if (tryCount >= maxTries)
                    {
                        tryCount = 0;
                        return false;
                    }

                    tryCount++;
                    await Task.Delay(100);
                }

                return true;
            }

            //Ratings related functions
            public async static Task<bool> CreateNewRating(string userID, Rating r)
            {
                try
                {
                    await CrossCloudFirestore.Current
                                .Instance
                                .Collection("users")
                                .Document(userID)
                                .UpdateAsync("Ratings", FieldValue.ArrayUnion(r));

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            public async static Task UpdateRating(string userID, Rating oldRating, Rating newRating)
            {
                var batch = CrossCloudFirestore.Current.Instance.Batch();

                var userDoc = CrossCloudFirestore.Current
                                .Instance
                                .Collection("users")
                                .Document(userID);

                batch.Update(userDoc, "Ratings", FieldValue.ArrayRemove(oldRating));
                batch.Update(userDoc, "Ratings", FieldValue.ArrayUnion(newRating));

                await batch.CommitAsync();
            }
            public async static Task DeleteRating(string userID, Rating r)
            {
                await CrossCloudFirestore.Current
                                .Instance
                                .Collection("users")
                                .Document(userID)
                                .UpdateAsync("Ratings", FieldValue.ArrayRemove(r));
            }

            //Teachers related functions
            public async static Task<bool> CreateTeacher(User teacher)
            {
                try
                {
                    await CrossCloudFirestore.Current
                                .Instance
                                .Collection("teachers")
                                .Document(teacher.UserID.ToString())
                                .SetAsync(teacher);

                    _app.Teachers.Add(teacher);
                    _app.Teachers = _app.Teachers;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            public async static Task<bool> RemoveTeacher(User teacher)
            {
                try
                {
                    await CrossCloudFirestore.Current
                                .Instance
                                .Collection("teachers")
                                .Document(teacher.UserID.ToString())
                                .DeleteAsync();

                    //TODO -> test if it works
                    _app.Teachers.Remove(teacher);
                    _app.Teachers = _app.Teachers;

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            //Events related functions
            public async static Task<bool> CreateEvent(Events e)
            {
                try
                {
                    await CrossCloudFirestore.Current
                                .Instance
                                .Collection("events")
                                .Document(e.ID.ToString())
                                .SetAsync(e);

                    _app.SavedEvents.Add(e);
                    _app.SavedEvents = _app.SavedEvents;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            public async static Task<bool> UpdateEvent(Events e)
            {
                try
                {
                    var query = CrossCloudFirestore.Current
                                .Instance
                                .Collection("events")
                                .Document(e.ID.ToString());

                    var batch = CrossCloudFirestore.Current
                                .Instance
                                .Batch();

                    batch.Update(query, "Name", e.Name, "Description", e.Description, "Time", e.Time, "Date", e.Date);
                    await batch.CommitAsync();

                    _app.SavedEvents.Remove(_app.SavedEvents.Find(ev => ev.ID == e.ID));
                    _app.SavedEvents.Add(e);
                    _app.SavedEvents = _app.SavedEvents;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            public async static Task<bool> RemoveEvent(Events e)
            {
                try
                {
                    await CrossCloudFirestore.Current
                                .Instance
                                .Collection("events")
                                .Document(e.ID.ToString())
                                .DeleteAsync();

                    _app.SavedEvents.Remove(e);
                    _app.SavedEvents = _app.SavedEvents;

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            //Questionnaire related functions
            public async static Task<bool> CreateQuestionnaire(Questionnaire q)
            {
                try
                {
                    await CrossCloudFirestore.Current.Instance
                                        .Collection("questionnaires")
                                        .Document(q.QuestionnaireID.ToString())
                                        .SetAsync(q);

                    _app.QuestionnaireList.Add(q);
                    _app.QuestionnaireList = _app.QuestionnaireList;

                    return true;
                }catch(Exception) { return false; };
            }
            public async static Task<bool> CloseQuestionnaire(Questionnaire q)
            {
                try
                {
                    await CrossCloudFirestore.Current.Instance
                                        .Collection("questionnaires")
                                        .Document(q.QuestionnaireID.ToString())
                                        .UpdateAsync("Closed", 1);

                    _app.QuestionnaireList.Find(qu => qu.QuestionnaireID == q.QuestionnaireID).Closed = 1;
                    _app.QuestionnaireList = _app.QuestionnaireList;

                    return true;
                }
                catch (Exception) { return false; };
            }
            public async static Task<bool> RemoveQuestionnaire(Questionnaire q)
            {
                try
                {
                    await CrossCloudFirestore.Current.Instance
                                        .Collection("questionnaires")
                                        .Document(q.QuestionnaireID.ToString())
                                        .DeleteAsync();

                    _app.QuestionnaireList.Remove(_app.QuestionnaireList.Find(qu => qu.QuestionnaireID == q.QuestionnaireID));
                    _app.QuestionnaireList = _app.QuestionnaireList;

                    return true;
                }
                catch (Exception) { return false; };
            }

            //Schedule related functions
            public async static Task AddSchedule(Schedule s)
            {
                //todo fix?? -> add class to real_schedules
                try
                {
                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    var scheduleDoc = CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("schedules")
                                        .Document(s.Id.ToString());
                    batch.Set(scheduleDoc, s);

                    foreach (var c in s.Classes)
                    {
                        var rsDoc = CrossCloudFirestore.Current
                                        .Instance
                                        .Collection("real_schedules")
                                        .Document(c.Day.ToString());

                        var t = new SchedulesByDayOfWeek.Times
                        {
                            Time = s.Time,
                            StudentsList = new List<int>(),
                            Date = SharedUtilities.GetNextDateFromWeekday(c.Day).ToString("yyyy-MM-dd"),
                            Type = s.Type
                        };
                        batch.Update(rsDoc, "Classes", FieldValue.ArrayUnion(t));
                        batch.Update(rsDoc, "ClassesTimeAndType", FieldValue.ArrayUnion(s.Time + "@" + s.Type));

                        batch.Update(rsDoc, "TimesOverview", FieldValue.ArrayUnion(s.Time)); //Deprecated
                    }
                    await batch.CommitAsync();

                    _app.AdmSchedules.Add(s);
                    _app.AdmSchedules = _app.AdmSchedules.OrderBy(sch => sch.Time).ToList();

                    await _app.SavePropertiesAsync();
                    MessagingCenter.Send(new PageControlMessage(), "UpdateSchedulesView");
                }
                catch (Exception)
                { 

                }
            }
            public async static Task<bool> RemoveSchedule(Schedule s)
            {
                try
                {
                    var batch = CrossCloudFirestore.Current.Instance.Batch();
                    var scheduleHistoryDoc = CrossCloudFirestore.Current
                                                .Instance
                                                .Collection("adm_events")
                                                .Document("schedules_change_history");

                    var listOfClasses = new List<string>();
                    foreach (var c in s.Classes)
                    {
                        listOfClasses.Add(s.Id + "@" + c.Day + "@" + s.Time + "/" + s.Type);

                        var doc = await CrossCloudFirestore.Current.Instance.Collection("real_schedules").Document(c.Day.ToString()).GetAsync();
                        var r_sch = doc.ToObject<SchedulesByDayOfWeek>();

                        var fClass = r_sch.Classes.Find(d => d.Time == s.Time && d.Type == s.Type);

                        batch.Update(doc.Reference, "Classes", FieldValue.ArrayRemove(fClass));
                        batch.Update(doc.Reference, "ClassesTimeAndType", FieldValue.ArrayRemove(s.Time + "@" + s.Type));

                        batch.Update(doc.Reference, "TimesOverview", FieldValue.ArrayRemove(s.Time)); //Deprecated
                    }

                    foreach (var c in listOfClasses)
                    {
                        var docs = await CrossCloudFirestore.Current
                            .Instance
                            .Collection("users")
                            .WhereArrayContains("ScheduleReferences", c)
                            .GetAsync();

                        foreach (var doc in docs.Documents)
                        {
                            var user = doc.ToObject<User>();

                            var splittenClass = c.Split('@');

                            batch.Update(scheduleHistoryDoc, "History", FieldValue.ArrayRemove(user.UserID + "@" + splittenClass[1] + "@" + splittenClass[0]));
                            batch.Update(doc.Reference, "ScheduleReferences", FieldValue.ArrayRemove(c));
                            //TODO - add missing user schedule event
                        }
                    }

                    batch.Delete(CrossCloudFirestore.Current
                            .Instance
                            .Collection("schedules")
                            .Document(s.Id.ToString()));

                    await batch.CommitAsync();

                    _app.AdmSchedules.Remove(s);
                    _app.AdmSchedules = _app.AdmSchedules;

                    await _app.SavePropertiesAsync();

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }
            public async static Task<bool> UpdateSchedule(Schedule newS, Schedule oldS)
            {
                try
                {
                    var doc = CrossCloudFirestore.Current.Instance.Collection("schedules").Document(newS.Id.ToString());
                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    var oldWd = new List<int>();
                    var newWd = new List<int>();
                    oldS.Classes.ForEach(c => oldWd.Add(c.Day));
                    newS.Classes.ForEach(c => newWd.Add(c.Day));
                    foreach (var wd in oldWd)
                    {
                        if (!newWd.Contains(wd))
                        {
                            batch.Update(doc, "Classes", FieldValue.ArrayRemove(new Schedule.Weekday
                            {
                                Day = wd,
                                StudentsList = oldS.Classes.Find(c => c.Day == wd).StudentsList
                            }));

                            var wdDoc = CrossCloudFirestore.Current.Instance.Collection("real_schedules").Document(wd.ToString());
                            var values = await wdDoc.GetAsync();
                            var data = values.ToObject<SchedulesByDayOfWeek>();

                            var selectedClass = data.Classes.Find(c => c.Time == oldS.Time && c.Type == oldS.Type);

                            batch.Update(wdDoc, "Classes", FieldValue.ArrayRemove(selectedClass));
                            batch.Update(wdDoc, "ClassesTimeAndType", FieldValue.ArrayRemove(newS.Time + "@" + newS.Type));

                            batch.Update(wdDoc, "TimesOverview", FieldValue.ArrayRemove(oldS.Time)); //Deprecated
                        }
                        else
                        {
                            newS.Classes.Find(c => c.Day == wd).StudentsList = oldS.Classes.Find(c => c.Day == wd).StudentsList;
                        }
                    }
                    newWd.RemoveAll(wd => oldWd.Contains(wd));
                    newWd.ForEach(wd =>
                    {
                        batch.Update(doc, "Classes", FieldValue.ArrayUnion(new Schedule.Weekday
                        {
                            Day = wd,
                            StudentsList = new List<int>()
                        }));

                        var wdDoc = CrossCloudFirestore.Current.Instance.Collection("real_schedules").Document(wd.ToString());
                        batch.Update(wdDoc, "Classes", FieldValue.ArrayUnion(new SchedulesByDayOfWeek.Times
                        {
                            Time = newS.Time,
                            Type = newS.Type,
                            Date = SharedUtilities.GetNextDateFromWeekday(wd).ToString("yyyy-MM-dd"),
                            StudentsList = new List<int>()
                        }));
                        batch.Update(wdDoc, "ClassesTimeAndType", FieldValue.ArrayUnion(newS.Time + "@" + newS.Type));

                        batch.Update(wdDoc, "TimesOverview", FieldValue.ArrayUnion(newS.Time)); //Deprecated
                    });

                    if (oldS.Time != newS.Time)
                        batch.Update(doc, "Time", newS.Time);
                    if (oldS.Type != newS.Type)
                        batch.Update(doc, "Type", newS.Type);

                    await batch.CommitAsync();

                    _app.AdmSchedules[_app.AdmSchedules.IndexOf(oldS)] = newS;
                    _app.AdmSchedules = _app.AdmSchedules;

                    await _app.SavePropertiesAsync();

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }

            struct ErrorData
            {
                public string date;
                public string description;

                public ErrorData(string date, string description)
                {
                    this.date = date;
                    this.description = description;
                }
            }
            public async static Task SaveErrorInServer(string errorDescription)
            {
                try
                {
                    var error = new ErrorData(DateTime.Today.ToString("yyyyMMdd-HH:mm:ss"), errorDescription);
                    await CrossCloudFirestore.Current.Instance.Collection("errors").Document(error.date).SetAsync(error);
                } catch(Exception) { }
            }

            //0 for add - 1 for remove
            public async static Task<bool> ChangeExperimentalClass(SchedulesByDayOfWeek.Times s, int wd, string docPath, int id, int cmd = 0)
            {
                try
                {
                    var rsdoc = CrossCloudFirestore.Current.Instance.Collection("real_schedules").Document(wd.ToString());
                    var classdoc = CrossCloudFirestore.Current.Instance.Collection("classes").Document(docPath);

                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    var oldTime = new SchedulesByDayOfWeek.Times
                    {
                        Time = s.Time,
                        StudentsList = new List<int>(s.StudentsList),
                        Date = s.Date,
                        Type = s.Type
                    };

                    SharedUtilities.UpdateRealScheduleDocWithBatch(oldTime, batch, id, cmd == 0 ? true : false);
                    batch.Update(classdoc, "StudentsIDs", cmd == 0 ? FieldValue.ArrayUnion(id) : FieldValue.ArrayRemove(id));

                    if (cmd == 0 && id > 1000)
                    {
                        var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(id.ToString());
                        var query = await userDoc.GetAsync();
                        var userData = query.ToObject<User>();

                        var foundCe = userData.ClassesExceptions.Find(c => c.StartsWith(docPath));
                        if (foundCe == null)
                            batch.Update(userDoc, "ClassesExceptions", FieldValue.ArrayUnion(docPath + "@add"));
                        else
                            batch.Update(userDoc, "ClassesExceptions", FieldValue.ArrayRemove(docPath + "@remove"));
                    }

                    await batch.CommitAsync();

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            public async static Task<bool> RemoveUserFromClass(SchedulesByDayOfWeek.Times s, string docPath, int id, bool addMakeupClass = false)
            {
                try
                {
                    var classdoc = CrossCloudFirestore.Current.Instance.Collection("classes").Document(docPath);
                    var userdoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(id.ToString());
                    var batch = CrossCloudFirestore.Current.Instance.Batch();

                    SharedUtilities.UpdateRealScheduleDocWithBatch(s, batch, id, false);
                    batch.Update(classdoc, "StudentsIDs", FieldValue.ArrayRemove(id));

                    var query = await CrossCloudFirestore.Current.Instance
                                                    .Collection("users")
                                                    .Document(id.ToString())
                                                    .GetAsync();
                    var user = query.ToObject<User>();

                    if (addMakeupClass)
                    {
                        var makeupClasses = s.Type == "Treino" ? user.MakeupClasses : user.MakeupClassesYoga;
                        var mcDateList = s.Type == "Treino" ? user.MCTrainDates : user.MCYogaDates;

                        var stringMCPathName = s.Type == "Treino" ? "MakeupClasses" : "MakeupClassesYoga";
                        var stringDatesPathName = s.Type == "Treino" ? "MCTrainDates" : "MCYogaDates";

                        makeupClasses += 1;
                        var i = 1;
                        string final_date = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd") + "@" + i;
                        while (mcDateList.Contains(final_date))
                        {
                            final_date = SharedUtilities.GetTodayDateTime().ToString("yyyy-MM-dd") + "@" + i;
                            i++;
                        }
                        mcDateList.Add(final_date);

                        batch.Update(userdoc, stringMCPathName, makeupClasses);
                        batch.Update(userdoc, stringDatesPathName, FieldValue.ArrayUnion(final_date));
                    }

                    var foundCe = user.ClassesExceptions.Find(c => c.StartsWith(docPath));
                    if (foundCe == null)
                        batch.Update(userdoc, "ClassesExceptions", FieldValue.ArrayUnion(docPath + "@remove"));
                    else
                        batch.Update(userdoc, "ClassesExceptions", FieldValue.ArrayRemove(docPath + "@add"));

                    await batch.CommitAsync();

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
