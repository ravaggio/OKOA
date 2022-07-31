using Plugin.Media;
using Plugin.Media.Abstractions;

using Plugin.Permissions.Abstractions;
using System;
using System.Collections.Generic;

using static ctf_final.AppController;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using Rg.Plugins.Popup.Services;
using System.Globalization;
using System.Linq;

namespace ctf_final.AdmContents.Teacher
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TeacherCadastre : ContentPage
    {
        //readonly User user;

        /// <summary>
        /// Student cadastree form. Can be used for updating user data if "u" is given.
        /// </summary>
        /// <param name="u"></param>
        public TeacherCadastre()
        {
            InitializeComponent();
            entryName.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeWord);
        }

        public User GetUserFromEntries(int id, User oldUser = null)
        {
            try
            {                               
                if (string.IsNullOrWhiteSpace(entryName.Text) || string.IsNullOrWhiteSpace(entryBirthday.Text) || entryBirthday.Text.Length != 10)
                {
                    return new User() { UserID = -1 };
                }
                else
                {
                    try
                    {
                        DateTime.ParseExact(entryBirthday.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return new User() { UserID = -2 };
                    }

                    User user = new User()
                    {
                        UserID = id,
                        Name = entryName.Text,
                        Birthday = entryBirthday.Text.Replace("/", ""),
                        Function = "TEACHER",
                    };

                    return user;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Erro desconhecido", "." + e, "OK");
                return null;
            }
        }
        public bool IsUserValid(User u)
        {
            if(u == null)
            {
                DisplayAlert("Erro desconhecido", "Incapaz de gerar usuário. Se o erro persistir, favor contatar o desenvolvedor.", "Ok");
                return false;
            }
            else if (u.UserID == -1)
            {
                DisplayAlert("Valores inválidos", "Os campos \"Nome\" e \"Data de Nascimento\" são obrigatórios!", "Ok");
                return false;
            }
            else if (u.UserID == -2)
            {
                DisplayAlert("Valores inválidos", "Insira uma data de nascimento válida.", "Ok");
                return false;
            }

            return true;
        }
        private async void RegisterStudent(object sender, EventArgs e)
        {
            finishBtn.IsEnabled = false;
            try
            {
                int id = SharedUtilities.GenerateNewID();
                User newUser = GetUserFromEntries(id);

                if (!IsUserValid(newUser))
                {
                    finishBtn.IsEnabled = true;
                    return;
                }

                if (await AdmUtilities.CreateTeacher(newUser))
                {
                    await DisplayAlert("Sucesso!", "Professor cadastrado com sucesso!", "OK");
                    MessagingCenter.Send(new PageControlMessage() { Command = "LoadTeachersPage" }, "LoadPage");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Erro desconhecido", "Não foi possivel cadastrar o professor.", "OK");
                }
            }
            catch (Exception exc)
            {
                await DisplayAlert("Erro desconhecido", "Incapaz de cadastrar o professor. Se o erro persistir, contate o desenvolvedor:  \n"+exc, "OK"); 
            }
            finishBtn.IsEnabled = true;
        }
    }
}