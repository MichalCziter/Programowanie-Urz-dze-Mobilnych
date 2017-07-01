using Android.App;
using Android.Widget;
using Android.OS;
using System;
using Android.Content;

namespace Zad1
{
    [Activity(Label = "Zad1PUM", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private EditText phoneNumber;
        private EditText message;
        private Button translateButton;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            translateButton = FindViewById<Button>(Resource.Id.Translate);
            phoneNumber = FindViewById<EditText>(Resource.Id.PhoneNumber);
            message = FindViewById<EditText>(Resource.Id.Message);


            translateButton.Click += (object sender, EventArgs e) =>
            {
                int n;
                bool isNumeric = int.TryParse(phoneNumber.Text, out n);

                if (isNumeric && String.IsNullOrWhiteSpace(message.Text))
                {
                    var callIntent = new Intent(Intent.ActionCall);
                    callIntent.SetData(Android.Net.Uri.Parse("tel:" + phoneNumber.Text));
                    StartActivity(callIntent);
                }
                else if (isNumeric && !String.IsNullOrWhiteSpace(message.Text))
                {
                    var smsUri = Android.Net.Uri.Parse("smsto:" + phoneNumber.Text);
                    var smsIntent = new Intent(Intent.ActionSendto, smsUri);
                    smsIntent.PutExtra("sms_body", message.Text);
                    StartActivity(smsIntent);
                }
                else if (!isNumeric && String.IsNullOrWhiteSpace(message.Text))
                {
                    Toast.MakeText(this, "Nie podano żadnych danych", ToastLength.Long).Show();
                }
                else
                {
                    try
                    {
                        var email = new Intent(Android.Content.Intent.ActionSend);
                        email.PutExtra(Android.Content.Intent.ExtraEmail, phoneNumber.Text);

                        email.PutExtra(Android.Content.Intent.ExtraText, message.Text);
                        email.SetType("message/rfc822");

                        StartActivity(email);

                    }
                    catch (Android.Content.ActivityNotFoundException)
                    {
                        Toast.MakeText(this, "Wystapił błąd podczas wysyłania wiadomości", ToastLength.Long).Show();
                    }
                }
            };

        }

    }

}