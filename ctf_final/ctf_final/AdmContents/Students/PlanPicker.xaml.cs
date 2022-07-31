using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ctf_final.Models;
using ctf_final.PlanModels;
using Plugin.CloudFirestore;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinFirebase.Model;
using static ctf_final.AppController;

namespace ctf_final.AdmContents.Students
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlanPicker : ContentPage
    {
        readonly int[] _floating = new int[3] { 0, 0, 0 };
        readonly int[] _indexes = new int[3] { -1, -1, -1 };
        List<PlanList> train_source = null;
        List<PlanList> yoga_source = null;
        List<PlanList> pilates_source = null;

        readonly PickedPlans oldPlan;

        readonly User user;
        readonly bool changing;

        public class PickerMessage
        {
            public PickedPlans Plans { get; set; }
        }
        StackLayout editingPlan;

        Plan customPlan = null;
        Plan customPlanYoga = null;
        Plan customPlanPilates = null;

        public class PlansLayout
        {
            public List<Plan> train;
            public List<Plan> yoga;
            public List<Plan> pilates;

            public List<PlanList> GetPlanLists(string type)
            {
                var pl = new List<PlanList>();
                var searchList = type == "train" ? train : type == "yoga" ? yoga : pilates;

                List<string> types = new List<string>();
                List<List<Plan>> plansPerType = new List<List<Plan>>();
                foreach(var sl in searchList)
                {
                    if (sl.Type == null) { plansPerType.Add(searchList); break; }
                    if(!types.Contains(sl.Type))
                    {
                        types.Add(sl.Type);
                        var list = new List<Plan>();
                        list.Add(sl);
                        plansPerType.Add(list);
                    }
                    else
                    {
                        plansPerType[types.IndexOf(sl.Type)].Add(sl);
                    }
                }

                int typeIteration = -1;
                bool firstAdded = false;
                foreach(var planPerType in plansPerType)
                {
                    typeIteration++;
                    for (int i = 1; i < 6; i++)
                    {
                        var foundPlans = planPerType.FindAll(t => t.TimesPerWeek == i);
                        if (foundPlans != null && foundPlans.Count > 0)
                        {
                            PlanList planlist;
                            if (!firstAdded && types.Count > 0)
                            {
                                planlist = new PlanList(i, types[typeIteration], true); 
                                firstAdded = true;
                            }
                            else
                                planlist = new PlanList(i);

                            planlist.AddRange(foundPlans);
                            pl.Add(planlist);
                        }
                    }
                    firstAdded = false;
                }
                
                return pl;
            }
        }

        List<StackLayout> selections = new List<StackLayout>();
        List<StackLayout> yoga_selections = new List<StackLayout>();
        List<StackLayout> pilates_selections = new List<StackLayout>();

        ActivityIndicator plansLoadingSign;

        public PlanPicker(PickedPlans pp = null, User u = null, bool changingPrice = false)
        {
            InitializeComponent();

            MessagingCenter.Subscribe<PageUpdateMessage>(this, "UpdatePlanPickerPage", msg =>
            {
              try
              {
                  (editingPlan.Children[1] as Label).Text = msg.Command.Contains('.') ? msg.Command : msg.Command.Replace(" R$", ".00 R$");
              }
              catch { }
            });

            changing = changingPrice;
            if (changing)
                ToolbarItems.Clear();

            plansLoadingSign = new ActivityIndicator()
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                IsRunning = true,
                Margin = new Thickness(16, 4, 16, 4),
                IsVisible = true,
                Color = (Color)_app.Resources["Orange"]
            };
            trainViewLayout.Children.Add(plansLoadingSign);

            user = u;
            oldPlan = pp;

            Task.Run(LoadViewTask);
        }
        async Task<bool> LoadViewTask()
        {
            var data = await CrossCloudFirestore.Current.Instance.Collection("plans").Document("data").GetAsync();
            PlansLayout planslayout = data.ToObject<PlansLayout>();

            train_source = planslayout.GetPlanLists("train");
            yoga_source = planslayout.GetPlanLists("yoga");
            pilates_source = planslayout.GetPlanLists("pilates");
            
            //--- SET REAL PRICES ---

            train_source.ForEach(pl =>
            {
                pl.ForEach(plan =>
                {
                    try
                    {
                        var planKey = plan.Type + "/" + plan.Duration + "/" + plan.TimesPerWeek;
                        plan.Price = (Application.Current as App).PlanPrices[planKey];
                    }
                    catch
                    {
                        plan.Price = 0;
                    }
                });
            });
            yoga_source.ForEach(pl =>
            {
                pl.ForEach(plan =>
                {
                    try
                    {
                        var planKey = "Yoga/" + plan.Duration + "/" + plan.TimesPerWeek;
                        plan.Price = (Application.Current as App).PlanPrices[planKey];
                    }
                    catch
                    {
                        plan.Price = 0;
                    }
                });
            });
            pilates_source.ForEach(pl =>
            {
                pl.ForEach(plan =>
                {
                    try
                    {
                        var planKey = "Pilates/" + plan.Duration + "/" + plan.TimesPerWeek;
                        plan.Price = (Application.Current as App).PlanPrices[planKey];
                    }
                    catch
                    {
                        plan.Price = 0;
                    }
                });
            });

            //--- SET REAL PRICES ---

            GenerateView(oldPlan);

            Device.BeginInvokeOnMainThread(() =>
            {
                plansLoadingSign.IsRunning = false;
                plansLoadingSign.IsVisible = false;
            });

            return true;
        }

        void GenerateView(PickedPlans pp)
        {
            List<Grid> headers_layouts = new List<Grid>();
            List<StackLayout> details_layouts = new List<StackLayout>();

            List<StackLayout> trainList = new List<StackLayout>();
            List<int> idlist = new List<int>();
            try
            {
                StackLayout sl = null;
                StackLayout details = null;
                string layout_type = "";
                int i = 0;
                foreach (PlanList pl in train_source)
                {
                    /* Add the headers of train plans (ex. the plan type)*/
                    if (pl.FirstOfItsType)
                    {
                        if (!string.IsNullOrWhiteSpace(layout_type))
                        {
                            details_layouts.Add(details);
                            sl.Children.Add(details);
                            //trainViewLayout.Children.Insert(i, sl);
                            idlist.Add(i);
                            trainList.Add(sl);
                            i++;
                            sl = new StackLayout() { Spacing = 0 };
                            details = new StackLayout() { Spacing = 0 };
                        }
                        else
                        {
                            sl = new StackLayout() { Spacing = 0 };
                            details = new StackLayout() { Spacing = 0 };
                        }
                        layout_type = pl.Type;
                        details.IsVisible = (pp != null && pp.TrainPlan != null && pp.TrainPlan.Type == layout_type);

                        Grid header = new Grid()
                        {
                            RowSpacing = 0,
                            ColumnSpacing = 0
                        };

                        BoxView bv = new BoxView() { BackgroundColor = (Color)Application.Current.Resources["Primary"] };
                        header.Children.Add(bv);
                        Grid.SetColumnSpan(bv, 2);
                        header.Children.Add(new Image()
                        {
                            Aspect = Aspect.AspectFit,
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            HeightRequest = 60,
                            WidthRequest = 135,
                            Source = "plan_type_holder.png"
                        });
                        header.Children.Add(new Label()
                        {
                            Margin = new Thickness(40, 0, 0, 0),
                            HorizontalOptions = LayoutOptions.Start,
                            VerticalOptions = LayoutOptions.CenterAndExpand,
                            Text = pl.Type,
                            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                            TextColor = string.IsNullOrEmpty(pl.Color) ? (Color)Application.Current.Resources["TextLight"] : Color.FromHex(pl.Color)
                        });
                        header.Children.Add(new Image()
                        {
                            Aspect = Aspect.AspectFit,
                            Margin = new Thickness(10),
                            Source = "ic_arrow_down.png",
                            HorizontalOptions = LayoutOptions.EndAndExpand
                        }, 1, 0);

                        TapGestureRecognizer tapExpand = new TapGestureRecognizer();
                        tapExpand.Tapped += async (s, ex) =>
                        {
                            var grid = s as Grid;
                            details_layouts[headers_layouts.IndexOf(grid)].IsVisible ^= true;
                            if (details_layouts[headers_layouts.IndexOf(grid)].IsVisible)
                                await (grid.Children.Last() as Image).RotateTo(180, 100);
                            else
                                await (grid.Children.Last() as Image).RotateTo(0, 100);
                        };
                        tapExpand.NumberOfTapsRequired = 1;
                        header.GestureRecognizers.Add(tapExpand);

                        headers_layouts.Add(header);
                        sl.Children.Add(header);
                        sl.Children.Add(new BoxView { BackgroundColor = Color.FromHex("#040404"), HeightRequest = 1 });
                    }

                    StackLayout tpw = new StackLayout()
                    {
                        BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]
                    };
                    tpw.Children.Add(new Label()
                    {
                        Margin = new Thickness(12),
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        Text = pl.TimesPerWeekString,
                        TextColor = (Color)Application.Current.Resources["TextLight"]
                    });
                    details.Children.Add(tpw);

                    foreach (Plan p in pl)
                    {
                        StackLayout plan = new StackLayout
                        {
                            BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"],
                            Orientation = StackOrientation.Horizontal,
                            Padding = new Thickness(10)
                        };
                        plan.Children.Add(new Label
                        {
                            Text = p.Duration.ToString(),
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            TextColor = (Color)Application.Current.Resources["TextLight"]
                        });
                        var text = p.Price.ToString().Replace(",", ".");
                        plan.Children.Add(new Label
                        {
                            Text = text.Contains('.') ? text + " R$" : text + ".00 R$",
                            HorizontalOptions = LayoutOptions.End,
                            TextColor = (Color)Application.Current.Resources["TextLight"]
                        });
                        TapGestureRecognizer tap = new TapGestureRecognizer();
                        tap.Tapped += (s, e) => { SelectTrainPlan(s, selections); };
                        tap.NumberOfTapsRequired = 1;
                        plan.GestureRecognizers.Add(tap);

                        details.Children.Add(plan);
                        selections.Add(plan);

                        if (pp != null && pp.TrainPlan != null && pp.TrainPlan.Type == p.Type && pp.TrainPlan.TimesPerWeek == p.TimesPerWeek && pp.TrainPlan.Duration == p.Duration)
                            SelectTrainPlan(plan, selections);

                        details.Children.Add(new BoxView { BackgroundColor = (Color)Application.Current.Resources["LightTransparent"], HeightRequest = 1 });
                    }
                }
                details_layouts.Add(details);
                sl.Children.Add(details);

                idlist.Add(i);
                trainList.Add(sl);
                //trainViewLayout.Children.Insert(i, sl);
            }
            catch (Exception e) { Console.WriteLine(e); }

            List<StackLayout> yogaList = new List<StackLayout>();
            try
            {
                foreach (PlanList pl in yoga_source)
                {
                    StackLayout sl = new StackLayout() { Spacing = 0 };
                    StackLayout tpw = new StackLayout()
                    {
                        BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]
                    };
                    tpw.Children.Add(new Label()
                    {
                        Margin = new Thickness(12),
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        Text = pl.TimesPerWeekString,
                        TextColor = (Color)Application.Current.Resources["TextLight"]
                    });

                    sl.Children.Add(tpw);

                    foreach (Plan p in pl)
                    {
                        StackLayout plan = new StackLayout
                        {
                            BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"],
                            Orientation = StackOrientation.Horizontal,
                            Padding = new Thickness(10)
                        };
                        plan.Children.Add(new Label
                        {
                            Text = p.Duration.ToString(),
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            TextColor = (Color)Application.Current.Resources["TextLight"]
                        });
                        var text = p.Price.ToString().Replace(",", ".");
                        plan.Children.Add(new Label
                        {
                            Text = text.Contains('.') ? text + " R$" : text + ".00 R$",
                            HorizontalOptions = LayoutOptions.End,
                            TextColor = (Color)Application.Current.Resources["TextLight"]
                        });

                        TapGestureRecognizer tap = new TapGestureRecognizer();
                        tap.Tapped += (s, e) => { SelectYogaPlan(s, yoga_selections); };
                        tap.NumberOfTapsRequired = 1;
                        plan.GestureRecognizers.Add(tap);

                        sl.Children.Add(plan);
                        yoga_selections.Add(plan);

                        if (pp != null && pp.YogaPlan != null && pp.YogaPlan.TimesPerWeek == p.TimesPerWeek && pp.YogaPlan.Duration == p.Duration && pp.YogaPlan.Price == p.Price)
                            SelectYogaPlan(plan, yoga_selections);

                        sl.Children.Add(new BoxView { BackgroundColor = (Color)Application.Current.Resources["LightTransparent"], HeightRequest = 1 });
                    }

                    yogaList.Add(sl);
                    //yogaViewLayout.Children.Add(sl);
                }
            }
            catch (Exception e) { Console.WriteLine(e); }

            List<StackLayout> pilatesList = new List<StackLayout>();
            try
            {
                foreach (PlanList pl in pilates_source)
                {
                    StackLayout sl = new StackLayout() { Spacing = 0 };
                    StackLayout tpw = new StackLayout()
                    {
                        BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"]
                    };
                    tpw.Children.Add(new Label()
                    {
                        Margin = new Thickness(12),
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        Text = pl.TimesPerWeekString,
                        TextColor = (Color)Application.Current.Resources["TextLight"]
                    });

                    sl.Children.Add(tpw);

                    foreach (Plan p in pl)
                    {
                        StackLayout plan = new StackLayout
                        {
                            BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"],
                            Orientation = StackOrientation.Horizontal,
                            Padding = new Thickness(10)
                        };
                        plan.Children.Add(new Label
                        {
                            Text = p.Duration.ToString(),
                            HorizontalOptions = LayoutOptions.StartAndExpand,
                            TextColor = (Color)Application.Current.Resources["TextLight"]
                        });
                        var text = p.Price.ToString().Replace(",", ".");
                        plan.Children.Add(new Label
                        {
                            Text = text.Contains('.') ? text + " R$" : text + ".00 R$",
                            HorizontalOptions = LayoutOptions.End,
                            TextColor = (Color)Application.Current.Resources["TextLight"]
                        });

                        TapGestureRecognizer tap = new TapGestureRecognizer();
                        tap.Tapped += (s, e) => { SelectPilatesPlan(s, pilates_selections); };
                        tap.NumberOfTapsRequired = 1;
                        plan.GestureRecognizers.Add(tap);

                        sl.Children.Add(plan);
                        pilates_selections.Add(plan);

                        if (pp != null && pp.PilatesPlan != null && pp.PilatesPlan.TimesPerWeek == p.TimesPerWeek && pp.PilatesPlan.Duration == p.Duration && pp.PilatesPlan.Price == p.Price)
                            SelectPilatesPlan(plan, pilates_selections);

                        sl.Children.Add(new BoxView { BackgroundColor = (Color)Application.Current.Resources["LightTransparent"], HeightRequest = 1 });
                    }

                    pilatesList.Add(sl);
                    //pilatesViewLayout.Children.Add(sl);
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
           
            Device.BeginInvokeOnMainThread(() =>
            {
                trainList.ForEach(sl => trainViewLayout.Children.Insert(idlist[trainList.IndexOf(sl)], sl));
                yogaList.ForEach(sl => yogaViewLayout.Children.Add(sl));
                pilatesList.ForEach(sl => pilatesViewLayout.Children.Add(sl));

                if (pp != null)
                {
                    if (_indexes[0] == -1 && pp.TrainPlan != null)
                    {
                        trainViewLayout.Children.Insert(0, GenerateCustomPlanLayout(pp.TrainPlan));
                    }

                    if (_indexes[1] == -1 && pp.YogaPlan != null)
                    {
                        yogaViewLayout.Children.Insert(0, GenerateCustomPlanLayout(pp.YogaPlan));
                    }

                    if (_indexes[2] == -1 && pp.PilatesPlan != null)
                    {
                        pilatesViewLayout.Children.Insert(0, GenerateCustomPlanLayout(pp.PilatesPlan));
                    }
                }
            });
        }

        private Grid GenerateCustomPlanLayout(Plan pl)
        {
            if (pl.IsYoga) { customPlanYoga = pl; }
            else if (pl.IsPilates) { customPlanPilates = pl; }
            else { customPlan = pl; }

            var layout = new Grid
            {
                RowSpacing = 0,
                BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"],
            };

            var labelsLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center,
                Padding = 10
            };

            labelsLayout.Children.Add(new Label
            {
                Text = pl.TimesPerWeek + "x POR SEMANA - " + pl.Duration,
                HorizontalOptions = LayoutOptions.StartAndExpand,
                TextColor = (Color)(Application.Current as App).Resources["TextLight"]
            });

            labelsLayout.Children.Add(new Label
            {
                Text = pl.Price + " R$",
                HorizontalOptions = LayoutOptions.End,
                TextColor = (Color)(Application.Current as App).Resources["TextLight"]
            });

            layout.Children.Add(labelsLayout);

            var btn = new Button
            {
                Text = "EDITAR",
                BackgroundColor = (Color)(Application.Current as App).Resources["Orange"],
                TextColor = (Color)(Application.Current as App).Resources["TextDark"]
            };

            btn.Clicked += (s, e) =>
            {
                SelectCustomPlan(pl.IsYoga ? customPlanYoga : pl.IsPilates ? customPlanPilates : customPlan);
            };

            layout.Children.Add(new BoxView { BackgroundColor = (Color)(Application.Current as App).Resources["Orange"] }, 0, 1);
            layout.Children.Add(btn, 0, 1);

            return layout;
        }

        private void SelectYogaPlan(object s, List<StackLayout> yoga_selections)
        {
            var tappedLayout = s as StackLayout;
            if (changing)
            {
                var index = yoga_selections.FindIndex(layout => layout.Equals(tappedLayout));
                var plan = new Plan();

                int x = 0;
                foreach (PlanList pl in yoga_source)
                {
                    if (pl.Count + x > index)
                    {
                        plan = pl[index - x];
                        break;
                    }
                    else
                    {
                        x += pl.Count;
                    }
                }

                editingPlan = tappedLayout;
                PopupNavigation.Instance.PushAsync(new PopupPages.PlanPriceChangePopup(plan));
            }
            else
            {
                if (customPlanYoga != null)
                {
                    yogaViewLayout.Children.RemoveAt(0);
                    customPlanYoga = null;
                }

                var others = yoga_selections.FindAll(planLayout => !planLayout.Equals(tappedLayout));
                if (!tappedLayout.BackgroundColor.Equals((Color)Application.Current.Resources["Orange"]))
                {
                    tappedLayout.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                    _indexes[1] = yoga_selections.FindIndex(layout => layout.Equals(tappedLayout));

                    foreach (StackLayout otherLayout in others)
                    {
                        otherLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    }
                }
                else
                {
                    tappedLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    _indexes[1] = -1;
                }
            }

        }
        private void SelectPilatesPlan(object s, List<StackLayout> pilates_selections)
        {
            var tappedLayout = s as StackLayout;
            if (changing)
            {
                var index = pilates_selections.FindIndex(layout => layout.Equals(tappedLayout));
                var plan = new Plan();

                int x = 0;
                foreach (PlanList pl in pilates_source)
                {
                    if (pl.Count + x > index)
                    {
                        plan = pl[index - x];
                        break;
                    }
                    else
                    {
                        x += pl.Count;
                    }
                }

                editingPlan = tappedLayout;
                PopupNavigation.Instance.PushAsync(new PopupPages.PlanPriceChangePopup(plan));
            }
            else
            {
                if (customPlanPilates != null)
                {
                    pilatesViewLayout.Children.RemoveAt(0);
                    customPlanPilates = null;
                }

                var others = pilates_selections.FindAll(planLayout => !planLayout.Equals(tappedLayout));
                if (!tappedLayout.BackgroundColor.Equals((Color)Application.Current.Resources["Orange"]))
                {
                    tappedLayout.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                    _indexes[2] = pilates_selections.FindIndex(layout => layout.Equals(tappedLayout));

                    foreach (StackLayout otherLayout in others)
                    {
                        otherLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    }
                }
                else
                {
                    tappedLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    _indexes[2] = -1;
                }
            }

        }
        private void SelectTrainPlan(object s, List<StackLayout> selections)
        {
            var tappedLayout = s as StackLayout;
            if (changing)
            {
                var index = selections.FindIndex(layout => layout.Equals(tappedLayout));
                var plan = new Plan();

                int i = 0;
                foreach (PlanList pl in train_source)
                {
                    if (pl.Count + i > index)
                    {
                        plan = pl[index - i];
                        break;
                    }
                    else
                    {
                        i += pl.Count;
                    }
                }

                editingPlan = tappedLayout;
                PopupNavigation.Instance.PushAsync(new PopupPages.PlanPriceChangePopup(plan));
            }
            else
            {
                if (customPlan != null)
                {
                    trainViewLayout.Children.RemoveAt(0);
                    customPlan = null;
                }

                var others = selections.FindAll(planLayout => !planLayout.Equals(tappedLayout));
                if (!tappedLayout.BackgroundColor.Equals((Color)Application.Current.Resources["Orange"]))
                {
                    tappedLayout.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                    _indexes[0] = selections.FindIndex(layout => layout.Equals(tappedLayout));

                    foreach (StackLayout otherLayout in others)
                    {
                        otherLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    }
                }
                else
                {
                    tappedLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    _indexes[0] = -1;
                }
            }
        }

        private void TrainButton(object sender, EventArgs s)
        {
            if (bgTrain.BackgroundColor != (Color)Application.Current.Resources["Orange"])
            {
                bgYoga.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                bgPilates.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                bgTrain.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                trainViewLayout.IsVisible = true;
                pilatesViewLayout.IsVisible = false;
                yogaViewLayout.IsVisible = false;
            }
        }
        private void YogaButton(object sender, EventArgs s)
        {
            if (bgYoga.BackgroundColor != (Color)Application.Current.Resources["Orange"])
            {
                bgTrain.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                bgPilates.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                bgYoga.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                trainViewLayout.IsVisible = false;
                pilatesViewLayout.IsVisible = false;
                yogaViewLayout.IsVisible = true;
            }
        }
        private void PilatesButton(object sender, EventArgs s)
        {
            if (bgPilates.BackgroundColor != (Color)Application.Current.Resources["Orange"])
            {
                bgPilates.BackgroundColor = (Color)Application.Current.Resources["Orange"];
                bgTrain.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                bgYoga.BackgroundColor = (Color)Application.Current.Resources["DarkTransparent"];
                trainViewLayout.IsVisible = false;
                pilatesViewLayout.IsVisible = true;
                yogaViewLayout.IsVisible = false;
            }
        }

        string GetExpiryDate(Plan p)
        {
            var date = DateTime.Today;
            switch (p.Duration)
            {
                case "Mensal":
                    date = date.AddMonths(1);
                    break;
                case "Trimestral":
                    date = date.AddMonths(3);
                    break;
                case "Semestral":
                    date = date.AddMonths(6);
                    break;
                case "Anual":
                    date = date.AddYears(1);
                    break;
            }

            return date.ToString("yyyy-MM-dd");
        }

        private async void Finish(object sender, EventArgs e)
        {
            if (_indexes[0] == -1 && _indexes[1] == -1 && _indexes[2] == -1 && customPlan == null && customPlanYoga == null && customPlanPilates == null)
            {
                await DisplayAlert("Selecione um plano", "Nenhum plano foi selecionado. Por favor selecione para continuar.", "Ok");
                return;
            }

            PickedPlans final_plans = new PickedPlans();
            //[ID_1] setting up IsFloating in PP
            //[ID_2] final_plans.PilatesPlan.IsFloating = _floating[2] == 1;

            if (_indexes[0] != -1)
            {
                int i = 0;
                foreach (PlanList pl in train_source)
                {
                    if (pl.Count + i > _indexes[0])
                    {
                        var plan = pl[_indexes[0] - i];
                        final_plans.TrainPlan = plan;
                        final_plans.TrainPlanExpiryDate = GetExpiryDate(plan);
                        final_plans.TrainPlan.IsFloating = _floating[0] == 1;
                        break;
                    }
                    else
                    {
                        i += pl.Count;
                    }
                }
            }
            else
            {
                if (customPlan != null)
                {
                    final_plans.TrainPlan = customPlan;
                    final_plans.TrainPlanExpiryDate = GetExpiryDate(customPlan);
                }
                else
                    final_plans.TrainPlan = null;
            }

            if (_indexes[1] != -1)
            {
                int x = 0;
                foreach (PlanList pl in yoga_source)
                {
                    if (pl.Count + x > _indexes[1])
                    {
                        var plan = pl[_indexes[1] - x];
                        final_plans.YogaPlan = plan;
                        final_plans.YogaPlanExpiryDate = GetExpiryDate(plan);
                        final_plans.YogaPlan.IsFloating = _floating[1] == 1;
                        break;
                    }
                    else
                    {
                        x += pl.Count;
                    }
                }
            }
            else
            {
                if (customPlanYoga != null)
                {
                    final_plans.YogaPlan = customPlanYoga;
                    final_plans.YogaPlanExpiryDate = GetExpiryDate(customPlanYoga);
                }
                else
                    final_plans.YogaPlan = null;
            }

            if (_indexes[2] != -1)
            {
                int x = 0;
                foreach (PlanList pl in pilates_source)
                {
                    if (pl.Count + x > _indexes[2])
                    {
                        var plan = pl[_indexes[2] - x];
                        final_plans.PilatesPlan = plan;
                        final_plans.PilatesPlanExpiryDate = GetExpiryDate(plan);
                        final_plans.PilatesPlan.IsFloating = _floating[2] == 1;
                        break;
                    }
                    else
                    {
                        x += pl.Count;
                    }
                }
            }
            else
            {
                if (customPlanPilates != null)
                {
                    final_plans.PilatesPlan = customPlanPilates;
                    final_plans.PilatesPlanExpiryDate = GetExpiryDate(customPlanPilates);
                }
                else
                    final_plans.PilatesPlan = null;
            }

            //todo
            if (user != null)
            {
                var oldPlansClassCount = (oldPlan.TrainPlan == null || oldPlan.TrainPlan.IsFloating ? 0 : oldPlan.TrainPlan.TimesPerWeek) + 
                                        (oldPlan.YogaPlan == null || oldPlan.YogaPlan.IsFloating ? 0 : oldPlan.YogaPlan.TimesPerWeek) + 
                                        (oldPlan.PilatesPlan == null || oldPlan.PilatesPlan.IsFloating ? 0 : oldPlan.PilatesPlan.TimesPerWeek);

                var newPlanClassCount = (final_plans.TrainPlan == null || final_plans.TrainPlan.IsFloating ? 0 : final_plans.TrainPlan.TimesPerWeek) + 
                                        (final_plans.YogaPlan == null || final_plans.YogaPlan.IsFloating ? 0 : final_plans.YogaPlan.TimesPerWeek) + 
                                        (final_plans.PilatesPlan == null || final_plans.PilatesPlan.IsFloating ? 0 : final_plans.PilatesPlan.TimesPerWeek);

                var allFloating = (final_plans.TrainPlan == null || final_plans.TrainPlan.IsFloating) &&
                                (final_plans.YogaPlan == null || final_plans.YogaPlan.IsFloating) &&
                                (final_plans.PilatesPlan == null || final_plans.PilatesPlan.IsFloating);
                if (oldPlansClassCount != newPlanClassCount)
                {
                    user.UserPlan = final_plans;

                    if (allFloating)
                    {
                        await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());

                        if(await AdmUtilities.UpdateUserPlan(user))
                        {
                            await DisplayAlert("Sucesso", "O plano foi atualizado com sucesso", "OK");
                            MessagingCenter.Send(new PageControlMessage() { Command = "schedules_too" }, "PlansUpdate");
                        }
                        else
                            await DisplayAlert("Erro", "Não foi possível atualizar o plano.", "OK");

                        await Navigation.PopAsync();
                        await PopupNavigation.Instance.PopAsync();
                    }
                    else
                    {
                        await DisplayAlert("Novas Aulas", "O número de aulas foi alterado, por favor selecione os novos horários do aluno.", "Ok");
                        await Navigation.PushAsync(new ClassSetupPage(user, (final_plans.TrainPlan == null || final_plans.TrainPlan.IsFloating) && (final_plans.PilatesPlan == null || final_plans.PilatesPlan.IsFloating) ? "Yoga" :
                                                                            final_plans.TrainPlan == null || final_plans.TrainPlan.IsFloating ? "Pilates" :
                                                                            "Treino", true));
                    }
                    return;
                }

                var trainChanged = oldPlan.TrainPlan != final_plans.TrainPlan;
                var yogaChanged = oldPlan.YogaPlan != final_plans.YogaPlan;
                var pilatesChanged = oldPlan.PilatesPlan != final_plans.PilatesPlan;

                if (trainChanged || yogaChanged || pilatesChanged)
                {
                    await PopupNavigation.Instance.PushAsync(new PopupPages.LoadingPopup());
                    try
                    {
                        var userDoc = CrossCloudFirestore.Current.Instance.Collection("users").Document(user.UserID.ToString());
                        var batch = CrossCloudFirestore.Current.Instance.Batch();

                        var expiryDoc = new ExpiryResume.Resume
                        {
                            ExpiryDate = oldPlan.TrainPlanExpiryDate,
                            ExpiryDateYoga = oldPlan.YogaPlanExpiryDate,
                            ExpiryDatePilates = oldPlan.PilatesPlanExpiryDate,
                            UserID = user.UserID
                        };

                        if (trainChanged)
                        {
                            batch.Update(userDoc, new FieldPath("UserPlan", "TrainPlan"), final_plans.TrainPlan);
                            if(oldPlan.TrainPlan == null)
                            {
                                batch.Update(userDoc, new FieldPath("UserPlan", "TrainPlanExpiryDate"), GetExpiryDate(final_plans.TrainPlan));
                                expiryDoc.ExpiryDate = GetExpiryDate(final_plans.TrainPlan);
                            }
                        }
                        if (yogaChanged)
                        {
                            batch.Update(userDoc, new FieldPath("UserPlan", "YogaPlan"), final_plans.YogaPlan);
                            if (oldPlan.YogaPlan == null)
                            {
                                batch.Update(userDoc, new FieldPath("UserPlan", "YogaPlanExpiryDate"), GetExpiryDate(final_plans.YogaPlan));
                                expiryDoc.ExpiryDateYoga = GetExpiryDate(final_plans.YogaPlan);
                            }
                        }
                        if (pilatesChanged)
                        {
                            batch.Update(userDoc, new FieldPath("UserPlan", "PilatesPlan"), final_plans.PilatesPlan);
                            if (oldPlan.PilatesPlan == null)
                            {
                                batch.Update(userDoc, new FieldPath("UserPlan", "PilatesPlanExpiryDate"), GetExpiryDate(final_plans.PilatesPlan));
                                expiryDoc.ExpiryDatePilates = GetExpiryDate(final_plans.PilatesPlan);
                            }
                        }

                        var oldResume = _app.ExpiryResumes.DateList.Find(d => d.UserID == user.UserID);
                        if (expiryDoc != oldResume)
                        {
                            batch.Update(CrossCloudFirestore.Current.Instance.Collection("adm_events").Document("expiry_dates"), "DateList", FieldValue.ArrayRemove(oldResume));
                            batch.Update(CrossCloudFirestore.Current.Instance.Collection("adm_events").Document("expiry_dates"), "DateList", FieldValue.ArrayUnion(expiryDoc));
                        }

                        await batch.CommitAsync();

                        user.UserPlan = final_plans;
                        MessagingCenter.Send(new PageControlMessage() { Command = "just_plans" }, "PlansUpdate");

                        await DisplayAlert("Sucesso", "O plano foi atualizado com sucesso", "OK");
                    }
                    catch
                    {
                        MessagingCenter.Send(new PageControlMessage() { Command = "schedules_too" }, "PlansUpdate");
                        await DisplayAlert("Erro", "Não foi possível atualizar o plano do usuário, por favor tente novamente mais tarde", "OK");
                    }
                    await PopupNavigation.Instance.PopAsync();
                }
            }
            else
                MessagingCenter.Send(new PickerMessage { Plans = final_plans }, "PlanPicked");
            await Navigation.PopAsync();
        }

        private void CustomPlan(object sender, EventArgs e)
        {
            SelectCustomPlan();
        }

        private async void SelectCustomPlan(Plan cPlan = null)
        {
            var type = yogaViewLayout.IsVisible ? "Yoga" : pilatesViewLayout.IsVisible ? "Pilates" : "Custom";

            Plan pl = null;
            try
            {
                var customPlanPage = new PopupPages.CustomPlan(type, cPlan);
                await PopupNavigation.Instance.PushAsync(customPlanPage);

                pl = await customPlanPage.GetPlan();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (pl != null)
            {
                if (!pl.IsYoga && !pl.IsPilates)
                {
                    if (customPlan != null)
                        trainViewLayout.Children.RemoveAt(0);

                    if (_indexes[0] != -1)
                    {
                        _indexes[0] = -1;
                        foreach (StackLayout otherLayout in selections)
                            otherLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    }

                    var planView = GenerateCustomPlanLayout(pl);
                    trainViewLayout.Children.Insert(0, planView);
                }
                else if (pl.IsYoga)
                {
                    if (customPlanYoga != null)
                        yogaViewLayout.Children.RemoveAt(0);

                    if (_indexes[1] != -1)
                    {
                        _indexes[1] = -1;
                        foreach (StackLayout otherLayout in yoga_selections)
                            otherLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    }

                    var planView = GenerateCustomPlanLayout(pl);
                    yogaViewLayout.Children.Insert(0, planView);
                }
                else if (pl.IsPilates)
                {
                    if (customPlanPilates != null)
                        pilatesViewLayout.Children.RemoveAt(0);

                    if (_indexes[2] != -1)
                    {
                        _indexes[2] = -1;
                        foreach (StackLayout otherLayout in pilates_selections)
                            otherLayout.BackgroundColor = (Color)Application.Current.Resources["PrimaryTransparent"];
                    }

                    var planView = GenerateCustomPlanLayout(pl);
                    pilatesViewLayout.Children.Insert(0, planView);
                }
            }
        }
    }
}