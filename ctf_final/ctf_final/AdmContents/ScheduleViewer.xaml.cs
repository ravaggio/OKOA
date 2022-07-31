using Plugin.CloudFirestore;
using Plugin.LocalNotifications;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ctf_final.Models;

using static ctf_final.AppController;
using System.Linq;
using System.Collections.Generic;
using System;
using XamarinFirebase.Model;

namespace ctf_final.AdmContents
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScheduleViewer : ContentPage
    {
        List<SchedulesByDayOfWeek> downloadedSchedules;
        Dictionary<int, List<string>> formattedClasses;
        List<string> mistakes = new List<string>();
        List<string> history = new List<string>();
        int selectedDOW = 1;
        public ScheduleViewer()
        {
            InitializeComponent();

            //SetSchedules();
            MessagingCenter.Subscribe<PageControlMessage>(this, "TEMP_UPDATE_SCHEDULES", msg =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    mainLayout.Children.Clear();
                    GenerateView(downloadedSchedules);
                });
            });
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var query = await CrossCloudFirestore.Current.Instance
                                                .Collection("real_schedules")
                                                .GetAsync();
                var listOfClasses = query.ToObjects<SchedulesByDayOfWeek>().ToList();

                downloadedSchedules = listOfClasses;

                CrossCloudFirestore.Current
                       .Instance
                       .Collection("real_schedules")
                       .AddSnapshotListener((snapshot, error) =>
                       {
                           try
                           {
                               if (snapshot != null && !snapshot.Metadata.IsFromCache && !snapshot.Metadata.HasPendingWrites)
                               {
                                   foreach (var documentChange in snapshot.DocumentChanges)
                                   {
                                       if (documentChange.Type == DocumentChangeType.Modified)
                                       {
                                           var doc = documentChange.Document.ToObject<SchedulesByDayOfWeek>();
                                           if (doc != null)
                                           {
                                               var downloaded = downloadedSchedules.Find(d => d.DayOfWeek == doc.DayOfWeek);
                                               foreach (var c in doc.Classes)
                                               {
                                                   var foundClass = downloaded.Classes.Find(cl => cl.Time == c.Time && cl.Type == c.Type);

                                                   if (foundClass != null)
                                                   {
                                                       var newStudents = c.StudentsList.Except(foundClass.StudentsList);
                                                       var removedStudents = foundClass.StudentsList.Except(c.StudentsList);

                                                       if (newStudents.Count() > 0)
                                                       {
                                                           foreach (var n in newStudents)
                                                           {
                                                               history.Add("aluno adicionado: " + n + ", " + c.Date + " (" + DateTime.Now + ")");
                                                           }
                                                       }
                                                       if (removedStudents.Count() > 0)
                                                       {
                                                           foreach (var n in removedStudents)
                                                           {
                                                               history.Add("aluno removido: " + n + ", " + c.Date + " (" + DateTime.Now + ")");
                                                           }
                                                       }
                                                   }
                                                   else
                                                   {
                                                       history.Add("horário adicionado: " + c.Time + " (" + DateTime.Now + ")");
                                                   }

                                                   downloadedSchedules[doc.DayOfWeek - 1] = doc;

                                                   Device.BeginInvokeOnMainThread(() =>
                                                   {
                                                       mainLayout.Children.Clear();
                                                       GenerateView(downloadedSchedules);
                                                   });
                                               }
                                           }
                                       }
                                   }
                               }
                           }
                           catch(Exception e)
                           {
                               Console.WriteLine(e);
                           }
                           
                       });

                try
                {
                    var users = await CrossCloudFirestore.Current.Instance
                                                .Collection("users")
                                                .GetAsync();
                    var listOfUsers = users.ToObjects<User>().ToList();
                    formattedClasses = new Dictionary<int, List<string>>();
                    foreach (var u in listOfUsers)
                    {
                        if(u.Function == "USER" && u.PlanAbscence != 1)
                        {
                            List<string> user_schedules = SharedUtilities.FormattUserClassesWithExceptions(u);
                            var ftd = new List<string>();
                            foreach (string sr in user_schedules)
                            {
                                //BasicClass: ClassID@DocPath@Time/Type
                                //ClassException: empty@DocPath@empty (Is certain that this class doc already exists)
                                string[] classFinder = sr.Split('@');
                                string docPath = classFinder[1];
                                ftd.Add(docPath);
                                string[] classData = docPath.Split('/');

                                try
                                {
                                    var foundSchedule = listOfClasses.Find(c => c.Classes.Find(cl => cl.Date == classData[0]) != null);
                                    var foundClass = foundSchedule.Classes.Find(c => c.Type == classData[2] && c.Time == classData[1]);

                                    if (!foundClass.StudentsList.Contains(u.UserID))
                                        mistakes.Add(u.UserID + "not found in: " + classData[0] + classData[1] + classData[2]);
                                }
                                catch (Exception) { }
                                
                            }

                            formattedClasses.Add(u.UserID, ftd);
                        }
                    }
                }
                catch(Exception e)
                { Console.WriteLine(e); }

                GenerateView(listOfClasses);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void GenerateView(List<SchedulesByDayOfWeek> listOfClasses)
        {

            var typePicker = new Picker()
            {
                HorizontalOptions = LayoutOptions.Fill,
                TextColor = Color.Orange,
                ClassId = "dow",
            };
            typePicker.Items.Add("Sábado");
            typePicker.Items.Add("Segunda");
            typePicker.Items.Add("Terça");
            typePicker.Items.Add("Quarta");
            typePicker.Items.Add("Quinta");
            typePicker.Items.Add("Sexta");
            typePicker.Items.Add("Domingo");
            typePicker.SelectedIndex = selectedDOW;

            typePicker.SelectedIndexChanged += TypePicker_SelectedIndexChanged;

            if (Device.RuntimePlatform == Device.iOS)
            {
                typePicker.BackgroundColor = (Color)_app.Resources["PrimaryDark"];
            }

            mainLayout.Children.Add(typePicker);

            var mistakeSl = new StackLayout
            {
                BackgroundColor = (Color)_app.Resources["PrimaryDark"],
            };
            mainLayout.Children.Add(mistakeSl);

            foreach (var dow in listOfClasses)
            {
                if(dow.DayOfWeek == selectedDOW)
                {
                    try
                    {
                        var sl = new StackLayout
                        {
                            BackgroundColor = (Color)_app.Resources["PrimaryDark"],
                        };
                        sl.Children.Add(new Label
                        {
                            Text = SharedUtilities.IntToWeekday(dow.DayOfWeek),
                            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Color.Orange
                        });

                        var orderedList = dow.Classes.OrderBy(c => c.Time).ToList();
                        foreach (var c in orderedList)
                        {
                            try
                            {
                                var selectedTimeType = _app.AdmSchedules.Find(s => s.Time == c.Time && s.Type == c.Type);
                                var foundClass = selectedTimeType.Classes.Find(cl => cl.Day == dow.DayOfWeek);

                                try
                                {
                                    foreach (var f in c.StudentsList)
                                    {
                                        var data = new List<string>();
                                        formattedClasses.TryGetValue(f, out data);

                                        if (!data.Contains(c.Date + '/' + c.Time + '/' + c.Type))
                                            mistakes.Add(f + "shouldn't be in:" + c.Date + c.Time + c.Type);
                                    }
                                }
                                catch(Exception e) { }
                                

                                sl.Children.Add(CreateComparer(c.StudentsList, foundClass.StudentsList, c.Time + "/" + c.Type));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }

                        mainLayout.Children.Add(sl);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            foreach (var m in mistakes)
            {
                var lbl = new Label()
                {
                    HorizontalOptions = LayoutOptions.Fill,
                    TextColor = Color.Orange,
                    Text = m
                };

                mistakeSl.Children.Add(lbl);
            }
        }

        private void TypePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            Picker picker = (Picker)sender;
            selectedDOW = picker.SelectedIndex;

            MessagingCenter.Send(new PageControlMessage(), "TEMP_UPDATE_SCHEDULES");
        }

        Grid CreateComparer(List<int> classStudents, List<int>scheduleStudents, string identifier)
        {
            var grid = new Grid
            {
                BackgroundColor = (Color)_app.Resources["Primary"],
            };

            var header = new Label()
            {
                Text = identifier,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.White
            };
            grid.Children.Add(header, 0, 0);
            Grid.SetColumnSpan(header, 2);

            var normalStudents = classStudents.Intersect(scheduleStudents);
            var removedStudents = scheduleStudents.Except(classStudents);
            var addedStudents = classStudents.Except(scheduleStudents);

            var row = 1;
            foreach(var rs in removedStudents)
            {
                try
                {
                    var name = _app.UsersResume.Users.Find(u => u.UserID == rs).Name;
                    var len = name.Length > 15 ? 15 : name.Length;
                    grid.Children.Add(CreateIDLabel(name.Substring(0, len), rs.ToString(), Color.Red), 0, row);
                    row++;
                }
                catch(Exception)
                {
                    grid.Children.Add(CreateIDLabel(rs.ToString(), rs.ToString(), Color.Red), 0, row);
                    row++;
                }
                
            }

            foreach (var ns in normalStudents)
            {
                try
                {
                    var name = _app.UsersResume.Users.Find(u => u.UserID == ns).Name;
                    var len = name.Length > 15 ? 15 : name.Length;
                    grid.Children.Add(CreateIDLabel(name.Substring(0, len), ns.ToString(), Color.Gray), 0, row);
                    grid.Children.Add(CreateIDLabel(name.Substring(0, len), ns.ToString(), Color.Gray), 1, row);
                    row++;
                }
                catch(Exception)
                {
                    grid.Children.Add(CreateIDLabel(ns.ToString(), ns.ToString(), Color.Gray), 0, row);
                    grid.Children.Add(CreateIDLabel(ns.ToString(), ns.ToString(), Color.Gray), 1, row);
                    row++;
                }
            }

            foreach (var ads in addedStudents)
            {
                try
                {
                    var name = _app.UsersResume.Users.Find(u => u.UserID == ads).Name;
                    var len = name.Length > 15 ? 15 : name.Length;
                    grid.Children.Add(CreateIDLabel(name.Substring(0, len), ads.ToString(), Color.Green), 1, row);
                    row++;
                }
                catch(Exception)
                {
                    grid.Children.Add(CreateIDLabel(ads.ToString(), ads.ToString(), Color.Green), 1, row);
                    row++;
                }
            }

            return grid;
        }

        Label CreateIDLabel(string value, string id, Color textColor)
        {
            var label = new Label
            {
                ClassId = id,
                Text = value,
                Padding = 10,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = textColor
            };

            var tapRec = new TapGestureRecognizer();
            tapRec.Tapped += idTapped;
            tapRec.NumberOfTapsRequired = 1;
            label.GestureRecognizers.Add(tapRec);
            
            return label;
        }
        private async void idTapped(object sender, System.EventArgs e)
        {
            int id = int.Parse((sender as Label).ClassId);

            List<string> classes = new List<string>();
            List<string> schedules = new List<string>();

            foreach(var s in _app.AdmSchedules)
            {
                var foundClasses = s.Classes.FindAll(c => c.StudentsList.Contains(id));
                if(foundClasses != null)
                {
                    foreach (var c in foundClasses)
                    {
                        schedules.Add(SharedUtilities.IntToWeekday(c.Day) + " - " + s.Time + "/" + s.Type);
                    }
                }
            }

            foreach (var dow in downloadedSchedules)
            {
                var foundClasses = dow.Classes.FindAll(c => c.StudentsList.Contains(id));
                if (foundClasses != null)
                {
                    foreach (var c in foundClasses)
                    {
                        classes.Add(SharedUtilities.IntToWeekday(dow.DayOfWeek) + " - " + c.Time + "/" + c.Type);
                    }
                }
            }

            var classesString = "";
            var schedulesString = "";

            classes.ForEach(c =>
            {
                classesString += c + "\n";
            });
            schedules.ForEach(c =>
            {
                schedulesString += c + "\n";
            });

            await DisplayAlert(_app.UsersResume.Users.Find(u => u.UserID == id).Name,
                classes.Count + " - " + schedules.Count + "\n" +
                "\n Horários: \n"
                + schedulesString +
                "\n Aulas: \n"
                + classesString,
                "OK");
        }
    }
}