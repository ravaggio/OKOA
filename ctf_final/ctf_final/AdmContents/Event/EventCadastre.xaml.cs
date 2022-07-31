using Plugin.Media;
using Plugin.Media.Abstractions;

using Plugin.Permissions.Abstractions;
using System;
using System.Collections.Generic;

using static ctf_final.AppController;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using ctf_final.Models;
using Rg.Plugins.Popup.Services;
using System.Globalization;
using System.Linq;

namespace ctf_final.AdmContents.Event
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EventCadastre : ContentPage
    {
        Events e = null;

        /// <summary>
        /// Student cadastree form. Can be used for updating user data if "u" is given.
        /// </summary>
        /// <param name="u"></param>
        public EventCadastre(Events old_event = null)
        {
            InitializeComponent();
            entryName.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeWord);

            if (old_event != null)
            {
                e = old_event;

                entryName.Text = e.Name;
                entryDescription.Text = e.Description;
                entryTime.Time = TimeSpan.Parse(e.Time);
                entryBirthday.Text = e.Date;
            }
            
        }

        public Events GetEventFromEntries()
        {
            try
            {                               
                if (string.IsNullOrWhiteSpace(entryName.Text) || string.IsNullOrWhiteSpace(entryBirthday.Text) || entryBirthday.Text.Length != 10)
                {
                    return null;
                }
                else
                {
                    try
                    {
                        DateTime.ParseExact(entryBirthday.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return new Events() { Date = "" };
                    }

                    var id = e == null ? GenerateID() : e.ID;
                    var ev = new Events()
                    {
                        ID = id,
                        Name = entryName.Text,
                        Description = entryDescription.Text,
                        Time = entryTime.Time.ToString(),
                        Date = entryBirthday.Text,
                        ConfirmedUsers = e == null ? new List<int>() : e.ConfirmedUsers
                    };

                    return ev;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Erro desconhecido", "." + e, "OK");
                return null;
            }
        }
        public bool IsEventValid(Events eve)
        {
            if (eve == null)
            {
                DisplayAlert("Valores inválidos!", "", "Ok");
                return false;
            }
            else if (_app.SavedEvents.Any(ev => ev.Name == eve.Name) && e == null || _app.SavedEvents.Any(ev => ev.Name == eve.Name && ev.Name != e.Name))
            {
                DisplayAlert("Valores inválidos", "O \"Nome\" do evento deve ser único!", "Ok");
                return false;
            }
            else if (eve.Date == "" || eve.Time == "")
            {
                DisplayAlert("Valores inválidos", "Data ou horário inválidos.", "Ok");
                return false;
            }

            return true;
        }
        private int GenerateID()
        {
            var id = 0;
            bool z = true;
            while (z)
            {
                id++;
                z = _app.SavedEvents.Any(ev => ev.ID == id);
            }

            return id;
        }

        private async void RegisterEvent(object sender, EventArgs ev)
        {
            finishBtn.IsEnabled = false;
            try
            {
                var newEvent = GetEventFromEntries();

                if (!IsEventValid(newEvent))
                {
                    finishBtn.IsEnabled = true;
                    return;
                }

                if(e == null)
                    if (await AdmUtilities.CreateEvent(newEvent))
                    {
                        await DisplayAlert("Sucesso!", "Evento cadastrado com sucesso!", "OK");
                        MessagingCenter.Send(new PageControlMessage() { Command = "LoadEventsPage" }, "LoadPage");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Erro desconhecido", "Não foi possivel cadastrar o evento.", "OK");
                    }
                else
                    if (await AdmUtilities.UpdateEvent(newEvent))
                    {
                        await DisplayAlert("Sucesso!", "Evento atualizado com sucesso!", "OK");
                        MessagingCenter.Send(new PageControlMessage() { Command = "LoadEventsPage" }, "LoadPage");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Erro desconhecido", "Não foi possivel atualizar o evento.", "OK");
                    }
            }
            catch (Exception exc)
            {
                await DisplayAlert("Erro desconhecido", "Incapaz de cadastrar o evento. Se o erro persistir, contate o desenvolvedor:  \n"+exc, "OK"); 
            }
            finishBtn.IsEnabled = true;
        }
    }
}