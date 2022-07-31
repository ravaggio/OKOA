using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.PopupPages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddSchedulePopup : PopupPage
    {
        List<int> selectedWeekdays = new List<int>();
        public AddSchedulePopup()
        {
            InitializeComponent();
            typePicker.SelectedIndex = 0;
        }

        public void AddOrRemoveWeekday(object sender, EventArgs e)
        {
            try
            {
                var bv = (sender as BoxView);
                int wdIndex = Int32.Parse(bv.ClassId);

                if (selectedWeekdays.Contains(wdIndex))
                {
                    selectedWeekdays.Remove(wdIndex);
                    bv.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                }
                else
                {
                    selectedWeekdays.Add(wdIndex);
                    bv.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async void AddButon(object sender, EventArgs e)
        {
            var pickedTime = timePicker.Time.ToString().Substring(0, 5);
            var pickedType = typePicker.SelectedItem.ToString();
            if (_app.AdmSchedules.Find(s => s.Time == pickedTime && s.Type == pickedType) != null)
            {
                await DisplayAlert("Alerta", "Esse horário já existe!", "Ok");
                return;
            }

            await PopupNavigation.Instance.PushAsync(new LoadingPopup());
            try
            {
                if (timePicker.Time.ToString().Equals("00:00:00") || selectedWeekdays.Count > 0)
                {
                    Schedule newSch = new Schedule
                    {
                        Id = SharedUtilities.GenerateNewID("schedule"),
                        Type = pickedType,
                        Time = pickedTime,
                        Classes = new List<Schedule.Weekday>()
                    };
                    selectedWeekdays.ForEach(wd => newSch.Classes.Add(new Schedule.Weekday { Day = wd, StudentsList = new List<int>() }));

                    await AdmUtilities.AddSchedule(newSch);

                    await PopupNavigation.Instance.PopAsync();
                    await DisplayAlert("Sucesso!", "Horário adicionado com sucesso!", "Ok");
                }
                else
                {
                    await DisplayAlert("Alerta", "Você precisa selecionar ao menos um dia da semana para adicionar um novo horário.", "Ok");
                }
            }
            catch
            {
                await PopupNavigation.Instance.PopAsync();
                await DisplayAlert("Erro", "Não foi possível adicionar o novo horário, por favor tente novamente.", "Ok");
            }
            await PopupNavigation.Instance.PopAsync();
        }
        public async void CancelButton(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
        }
    }
}