using Android.App;
using Android.Content;
using System;
using static ctf_final.AppController;

namespace BootCompletedExample
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action == Intent.ActionBootCompleted)
            {
                try
                {
                    if (_app.LoggedInUser != null)
                        UserUtilities.AddPlanExpiryNotifications();
                }catch (Exception) { }
            }
        }
    }
}