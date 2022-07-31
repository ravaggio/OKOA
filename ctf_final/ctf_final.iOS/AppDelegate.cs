using Foundation;
using ImageCircle.Forms.Plugin.iOS;
using UIKit;
using UserNotifications;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace ctf_final.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Forms.Init();
            
            Rg.Plugins.Popup.Popup.Init();
            ImageCircleRenderer.Init();
            Firebase.Core.App.Configure();

            UINavigationBar.Appearance.TintColor = Color.FromHex("#de4905").ToUIColor();
            UINavigationBar.Appearance.BarTintColor = Color.FromHex("#090909").ToUIColor();
            UINavigationBar.Appearance.TitleTextAttributes = new UIStringAttributes() { ForegroundColor = Color.FromHex("#de4905").ToUIColor() };
            
            UITextField.Appearance.TintColor = Color.FromHex("#de4905").ToUIColor();

            UISwitch.Appearance.TintColor = Color.FromHex("#de4905").ToUIColor();
            UISwitch.Appearance.OnTintColor = Color.FromHex("#de4905").ToUIColor();

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // Ask the user for permission to get notifications on iOS 10.0+
                UNUserNotificationCenter.Current.RequestAuthorization(
                        UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
                        (approved, error) => { });
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                // Ask the user for permission to get notifications on iOS 8.0+
                var settings = UIUserNotificationSettings.GetSettingsForTypes(
                        UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound,
                        new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
            }

            LoadApplication(new App());
            return base.FinishedLaunching(app, options);
        }
    }
}
