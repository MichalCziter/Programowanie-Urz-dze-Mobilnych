using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Json;
using Android.Locations;
using System.Data.SqlClient;
using System.Text;
using System.Linq;
using Android.Util;
using Android.Content;
using Mono.Data.Sqlite;
using System.Data;
using System.Collections.Generic;

namespace Pogodynka
{
    [Activity(Label = "Pogodynka", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity, ILocationListener
    {

        static readonly List<string> phoneNumbers = new List<string>();


        public async void OnLocationChanged(Location location)
        {
            _currentLocation = location;
            if (_currentLocation == null)
            {
                latitude.Text = "Blad GPS";
            }
            else
            {
                latitude.Text = string.Format("{0:f6}", _currentLocation.Latitude);
                longitude.Text = string.Format("{0:f6}", _currentLocation.Longitude);
                Address address = await ReverseGeocodeCurrentLocation();
            }
        }

        public void OnProviderDisabled(string provider) { }

        public void OnProviderEnabled(string provider) { }

        public void OnStatusChanged(string provider, Availability status, Bundle extras) { }

        static readonly string TAG = "X:" + typeof(Activity1).Name;
        Location _currentLocation;
        LocationManager _locationManager;
        EditText latitude;
        EditText longitude;
        TextView location;
        TextView temperature;
        TextView humidity;
        TextView conditions;
        string _locationProvider;

        protected override void OnResume()
        {
            base.OnResume();
            _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _locationManager.RemoveUpdates(this);
        }

        public void BazaInit()
        {
            var docsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var pathToDatabase = Path.Combine(docsFolder, "db_adonet.db");

            if (!File.Exists(pathToDatabase))
            {
                SqliteConnection.CreateFile(pathToDatabase);
                var connectionString = string.Format("Data Source={0};Version=3;", pathToDatabase);

                try
                {
                    using (var conn = new SqliteConnection((connectionString)))
                    {
                        conn.Open();
                        using (var command = conn.CreateCommand())
                        {
                            command.CommandText = "CREATE TABLE Pogoda (PogodaID INTEGER PRIMARY KEY AUTOINCREMENT, Lokacja ntext, Temperatura ntext, Wilgotnosc ntext, Warunki ntext)";
                            command.CommandType = CommandType.Text;
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Toast aToast = Toast.MakeText(this, "Blad dodania do bazy", ToastLength.Long);
                    aToast.Show();
                }

                if (!File.Exists(pathToDatabase))
                {
                    Toast aToast = Toast.MakeText(this, "Blad bazy danych", ToastLength.Long);
                    aToast.Show();
                }
            }
            else if (File.Exists(pathToDatabase))
            {
                Toast aToast = Toast.MakeText(this, "Jest baza", ToastLength.Long);
                aToast.Show();
            }
        }

        String _LokacjaSave;
        String _TemperaturaSave;
        String _WilgotnoscSave;
        String _WarunkiSave;

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("_LokacjaSave", _LokacjaSave);
            outState.PutString("_TemperaturaSave", _TemperaturaSave);
            outState.PutString("_WilgotnoscSave", _WilgotnoscSave);
            outState.PutString("_WarunkiSave", _WarunkiSave);
            base.OnSaveInstanceState(outState);

        }
        protected override void OnRestoreInstanceState(Bundle savedState)
        {
            base.OnRestoreInstanceState(savedState);
            _LokacjaSave = savedState.GetString("_LokacjaSave");
            _TemperaturaSave = savedState.GetString("_TemperaturaSave");
            _WilgotnoscSave = savedState.GetString("_WilgotnoscSave");
            _WarunkiSave = savedState.GetString("_WarunkiSave");

        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            location = FindViewById<TextView>(Resource.Id.locationText);
            temperature = FindViewById<TextView>(Resource.Id.tempText);
            humidity = FindViewById<TextView>(Resource.Id.humidText);
            conditions = FindViewById<TextView>(Resource.Id.condText);

            if (bundle != null)
            {
                _LokacjaSave = bundle.GetString("_LokacjaSave", "");
                _TemperaturaSave = bundle.GetString("_TemperaturaSave", "");
                _WilgotnoscSave = bundle.GetString("_WilgotnoscSave", "");
                _WarunkiSave = bundle.GetString("_WarunkiSave", "");
            }

            location.Text = _LokacjaSave;
            temperature.Text = _TemperaturaSave;
            humidity.Text = _WilgotnoscSave;
            conditions.Text = _WarunkiSave;

            Button historia_button = FindViewById<Button>(Resource.Id.historia_button);

            InitializeLocationManager();

            // Get the latitude/longitude EditBox and button resources:
            latitude = FindViewById<EditText>(Resource.Id.latText);
            longitude = FindViewById<EditText>(Resource.Id.longText);
            Button button = FindViewById<Button>(Resource.Id.getWeatherButton);

            BazaInit();

            historia_button.Click += (sender, e) =>
            {
                var docsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                var pathToDatabase = Path.Combine(docsFolder, "db_adonet.db");
                var connectionString = string.Format("Data Source={0};Version=3;", pathToDatabase);
                var conn = new SqliteConnection((connectionString));
                conn.Open();
                var command = conn.CreateCommand();

                command.CommandText = "SELECT * FROM [Pogoda]";
                var k = command.ExecuteReader();
                phoneNumbers.Clear();
                while (k.Read())
                {
                    phoneNumbers.Add(k["PogodaID"].ToString());
                    phoneNumbers.Add(k["Lokacja"].ToString());
                    phoneNumbers.Add(k["Temperatura"].ToString());
                    phoneNumbers.Add(k["Wilgotnosc"].ToString());
                    phoneNumbers.Add(k["Warunki"].ToString());
                }
                conn.Close();      
                var intent = new Intent(this, typeof(Historia));
                intent.RemoveExtra("phone_numbers");               
                intent.PutStringArrayListExtra("phone_numbers", phoneNumbers);
                StartActivity(intent);
            };

            // When the user clicks the button ...
            button.Click += async (sender, e) =>
            {

                // Get the latitude and longitude entered by the user and create a query.
                string url = "http://api.geonames.org/findNearByWeatherJSON?lat=" +
                             latitude.Text +
                             "&lng=" +
                             longitude.Text +
                             "&username=janusz314";

                // Fetch the weather information asynchronously, 
                // parse the results, then update the screen:
                JsonValue json = await FetchWeatherAsync(url);
                ParseAndDisplay(json);

                var docsFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                var pathToDatabase = Path.Combine(docsFolder, "db_adonet.db");
                var connectionString = string.Format("Data Source={0};Version=3;", pathToDatabase);
                var conn = new SqliteConnection((connectionString));
                conn.Open();
                var command = conn.CreateCommand();

                command.CommandText = "INSERT INTO [Pogoda] ([Lokacja], [Temperatura], [Wilgotnosc], [Warunki]) VALUES ('" + location.Text.ToString() + "','" + temperature.Text.ToString() + "','" + humidity.Text.ToString() + "','" + conditions.Text.ToString() + "')";
                command.ExecuteNonQuery();
                conn.Close();

            };

        }

        async Task<Address> ReverseGeocodeCurrentLocation()
        {
            Geocoder geocoder = new Geocoder(this);
            System.Collections.Generic.IList<Address> addressList =
                await geocoder.GetFromLocationAsync(_currentLocation.Latitude, _currentLocation.Longitude, 10);

            Address address = addressList.FirstOrDefault();
            return address;
        }

        // Gets weather data from the passed URL.
        private async Task<JsonValue> FetchWeatherAsync(string url)
        {
            // Create an HTTP web request using the URL:
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            request.ContentType = "application/json";
            request.Method = "GET";

            // Send the request to the server and wait for the response:
            using (WebResponse response = await request.GetResponseAsync())
            {
                // Get a stream representation of the HTTP web response:
                using (Stream stream = response.GetResponseStream())
                {
                    // Use this stream to build a JSON document object:
                    JsonValue jsonDoc = await Task.Run(() => JsonObject.Load(stream));
                    Console.Out.WriteLine("Response: {0}", jsonDoc.ToString());

                    // Return the JSON document:
                    return jsonDoc;
                }
            }
        }

        // Parse the weather data, then write temperature, humidity, 
        // conditions, and location to the screen.
        private void ParseAndDisplay(JsonValue json)
        {
            // Get the weather reporting fields from the layout resource:
            location = FindViewById<TextView>(Resource.Id.locationText);
            temperature = FindViewById<TextView>(Resource.Id.tempText);
            humidity = FindViewById<TextView>(Resource.Id.humidText);
            conditions = FindViewById<TextView>(Resource.Id.condText);

            // Extract the array of name/value results for the field name "weatherObservation". 
            JsonValue weatherResults = json["weatherObservation"];

            // Extract the "stationName" (location string) and write it to the location TextBox:
            location.Text = weatherResults["stationName"];

            // The temperature is expressed in Celsius:
            double temp = weatherResults["temperature"];
            // Write the temperature (one decimal place) to the temperature TextBox:
            temperature.Text = String.Format("{0:F1}", temp) + "° C";

            // Get the percent humidity and write it to the humidity TextBox:
            double humidPercent = weatherResults["humidity"];
            humidity.Text = humidPercent.ToString() + "%";

            // Get the "clouds" and "weatherConditions" strings and 
            // combine them. Ignore strings that are reported as "n/a":
            string cloudy = weatherResults["clouds"];
            if (cloudy.Equals("n/a"))
                cloudy = "";
            string cond = weatherResults["weatherCondition"];
            if (cond.Equals("n/a"))
                cond = "";

            // Write the result to the conditions TextBox:
            conditions.Text = cloudy + " " + cond;

            _LokacjaSave = location.Text;
            _TemperaturaSave = temperature.Text;
            _WilgotnoscSave = humidity.Text;
            _WarunkiSave = conditions.Text;
        }

        void InitializeLocationManager()
        {
            _locationManager = (LocationManager)GetSystemService(LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            System.Collections.Generic.IList<string> acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

            if (acceptableLocationProviders.Any())
            {
                _locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                _locationProvider = string.Empty;
            }
            Log.Debug(TAG, "Using " + _locationProvider + ".");
        }

    }
}

