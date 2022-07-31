using Plugin.Media;
using Plugin.Media.Abstractions;


using System;
using System.Collections.Generic;

using static ctf_final.AppController;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using Rg.Plugins.Popup.Services;
using System.Globalization;
using System.Linq;
using Xamarin.Essentials;

namespace ctf_final.AdmContents.Students
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StudentCadastre : ContentPage
    {
        PlanModels.PickedPlans user_plans = null;
        readonly User user;

        /// <summary>
        /// Student cadastree form. Can be used for updating user data if "u" is given.
        /// </summary>
        /// <param name="u"></param>
        public StudentCadastre(User u = null)
        {
            InitializeComponent();
            entryName.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeWord);

            SharedUtilities.TemporaryProfilePicture = null;

            if (u != null)
            {
                string picToken = u.PictureToken == "" ? SharedUtilities.DefaultPictureToken : u.PictureToken;

                user = u;
                selectedImage.Source = picToken;
                pickerSex.SelectedIndex = u.Gender;
                entryName.Text = u.Name;
                entryBirthday.Text = u.Birthday.ToString();
                entryEmail.Text = u.Email;
                entryPhone.Text = u.Phone;
                entryAddress.Text = u.Address;

                planLayout.IsVisible = false;

                finishBtn.Text = "Salvar";
                Title = "Editar aluno";
            }
            else
            {
                pickerSex.SelectedItem = "Masculino";
            }
        }

        private async void PickPlan(object sender, EventArgs e)
        {
            /* Open the screen to select plans and wait for the result. 
             * The result is written as a resume of the selected plans and 
             * saved at the "user_plans" variable */
            await Navigation.PushAsync(new PlanPicker(user_plans));
            MessagingCenter.Subscribe<PlanPicker.PickerMessage>(this, "PlanPicked", msg => {
                SetPlanLabelText(msg.Plans);
                user_plans = msg.Plans;
            });
        }
        private void SetPlanLabelText(PlanModels.PickedPlans pp)
        {
            string formatted_plan = "";

            var tp = pp.TrainPlan;
            if (tp != null)
            {

                formatted_plan = "Plano " + tp.Type + " - " + tp.TimesPerWeek + "x por semana. (" + tp.Duration + ")";
            }

            var yp = pp.YogaPlan;
            if (yp != null)
            {
                if (formatted_plan != "")
                    formatted_plan += "\n";

                formatted_plan += "Yoga - " + yp.TimesPerWeek + "x por semana. (" + yp.Duration + ")";
            }

            var pilp = pp.PilatesPlan;
            if (pilp != null)
            {
                if (formatted_plan != "")
                    formatted_plan += "\n";

                formatted_plan += "Pilates - " + pilp.TimesPerWeek + "x por semana. (" + pilp.Duration + ")";
            }


            labelSelectedPlans.Text = formatted_plan;
        }

        private async void PickImage(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageRead>();
                }
                if (status == PermissionStatus.Granted)
                { 
                    await CrossMedia.Current.Initialize();
                    if (!CrossMedia.Current.IsPickPhotoSupported)
                    {
                        await DisplayAlert("Incapaz de selecionar foto", "Esta funcionalidade não é compativel com o seu aparelho", "Ok");
                        return;
                    }

                    var mediaOptions = new PickMediaOptions()
                    {
                        PhotoSize = PhotoSize.Small
                    };

                    SharedUtilities.TemporaryProfilePicture = await CrossMedia.Current.PickPhotoAsync(mediaOptions);
                    if (SharedUtilities.TemporaryProfilePicture == null)
                    {
                        return;
                    }

                    selectedImage.Source = ImageSource.FromStream(() => SharedUtilities.TemporaryProfilePicture.GetStream());
                    
                }
                else if (status != PermissionStatus.Unknown)
                {
                    await DisplayAlert("Acesso negado.", "Não foi possível acessar as imagens, por favor tente novamente.", "Ok");
                }
            }
            catch(Exception exc)
            {
                await DisplayAlert("Erro desconhecido", "Não foi possível selecionar a imagem. Se o erro persistir, favor contatar o desenvolvedor: \n"+ exc, "OK");
            } 
        }

        public User GetUserFromEntries(int id, User oldUser = null)
        {
            try
            {
                var formattedPhone = "";
                try { formattedPhone = entryPhone.Text.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""); }catch{ Console.WriteLine("invalid phone"); }

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
                        Phone = formattedPhone,
                        Address = entryAddress.Text,
                        Email = entryEmail.Text,
                        Function = "USER",
                        Gender = pickerSex.SelectedIndex,
                        PictureToken = "",
                        UserPlan = user_plans,
                        Ratings = new List<Rating>(),
                        MCTrainDates = new List<string>(),
                        MCYogaDates = new List<string>(),
                        MCPilatesDates = new List<string>()
                    };

                    if(oldUser != null)
                    {
                        user.Ratings = oldUser.Ratings;
                        user.UserPlan = oldUser.UserPlan;
                        user.ClassesExceptions = oldUser.ClassesExceptions;
                        user.ScheduleReferences = oldUser.ScheduleReferences;
                        user.MakeupClasses = oldUser.MakeupClasses;
                        user.MakeupClassesYoga = oldUser.MakeupClassesYoga;
                        user.MakeupClassesPilates = oldUser.MakeupClassesPilates;
                        user.MCTrainDates = oldUser.MCTrainDates;
                        user.MCYogaDates = oldUser.MCYogaDates;
                        user.MCPilatesDates = oldUser.MCPilatesDates;
                        user.PlanAbscenceDate = oldUser.PlanAbscenceDate;
                    }

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
            else if(u.Phone != "" && u.Phone.Length < 10)
            {
                DisplayAlert("Valores inválidos", "Insira um número de telefone válido.", "Ok");
                return false;
            }
            else if (user == null && (u.UserPlan == null || u.UserPlan.TrainPlan == null && u.UserPlan.YogaPlan == null && u.UserPlan.PilatesPlan == null))
            {
                DisplayAlert("Valores inválidos", "Você precisa escolher pelo menos um plano para continuar", "Ok");
                return false;
            }

            return true;
        }
        private async void RegisterStudent(object sender, EventArgs e)
        {
            finishBtn.IsEnabled = false;
            try
            {
                if (user != null) //update
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());

                    User updatedUser = GetUserFromEntries(user.UserID, user);
                    updatedUser.PictureToken = user.PictureToken;
                    
                    if (!IsUserValid(updatedUser))
                    {
                        finishBtn.IsEnabled = true;
                        return;
                    }

                    if (await SharedUtilities.UpdateUser(user, updatedUser))
                    {
                        MessagingCenter.Send(updatedUser, "UserUpdated");

                        await DisplayAlert("Sucesso!", "Os dados foram atualizados com sucesso!", "OK");
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Erro", "Incapaz de atualizar os dados, por favor tente novamente.", "OK");
                    }

                    await PopupNavigation.Instance.PopAsync();
                }
                else //create
                {
                    int id = SharedUtilities.GenerateNewID();
                    User newUser = GetUserFromEntries(id);

                    if (!IsUserValid(newUser))
                    {
                        finishBtn.IsEnabled = true;
                        return;
                    }

                    //[ID_1] creating user without picking classes
                    //[ID_2] added pilates possibility
                    if (AdmUtilities.GetNeedClassSetup(newUser))
                    {
                        if ((newUser.UserPlan.TrainPlan == null || newUser.UserPlan.TrainPlan.IsFloating) &&
                             (newUser.UserPlan.PilatesPlan == null || newUser.UserPlan.PilatesPlan.IsFloating) &&
                             !newUser.UserPlan.YogaPlan.IsFloating)
                            await Navigation.PushAsync(new ClassSetupPage(newUser, "Yoga"));
                        else if ((newUser.UserPlan.TrainPlan == null || newUser.UserPlan.TrainPlan.IsFloating) && !newUser.UserPlan.PilatesPlan.IsFloating)
                            await Navigation.PushAsync(new ClassSetupPage(newUser, "Pilates"));
                        else if (!newUser.UserPlan.TrainPlan.IsFloating)
                            await Navigation.PushAsync(new ClassSetupPage(newUser, "Treino"));
                    }
                    else
                        if (await AdmUtilities.CreateNewUser(newUser))
                            await Navigation.PushAsync(new StudentCadastreCompletion(newUser));
                }
            }
            catch (Exception exc)
            {
                await DisplayAlert("Erro desconhecido", "Incapaz de cadastrar o usuário. Se o erro persistir, contate o desenvolvedor:  \n"+exc, "OK"); 
            }
            finishBtn.IsEnabled = true;
        }
    }
}