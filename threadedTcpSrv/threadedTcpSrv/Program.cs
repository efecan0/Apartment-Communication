using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net.Http;
using System.Text.Json;
using Weather;

class ThreadedTcpSrvr
{
    private TcpListener cinsSrvListener;

    public ThreadedTcpSrvr()
    {
        cinsSrvListener = new TcpListener(9040);
        cinsSrvListener.Start();

        Console.WriteLine("Waiting for clients...");
        while (true)
        {
            while (!cinsSrvListener.Pending())
            {
                Thread.Sleep(1000);
            }

            ClientClass newclient = new ClientClass();
            newclient.threadListener = this.cinsSrvListener;
            Thread newthread = new Thread(new
                      ThreadStart(newclient.HandleConnection));
            newthread.Start();
        }
    }

    public static void Main()
    {
        ThreadedTcpSrvr server = new ThreadedTcpSrvr();
    }
}

class ClientClass
{
    private static Dictionary<TcpClient, NetworkStream> clientStreams = new Dictionary<TcpClient, NetworkStream>();

    public TcpListener threadListener;
    private static int connections = 0;

    public void HandleConnection()
    {
        int recv;
        byte[] data = new byte[1024];

        TcpClient client = threadListener.AcceptTcpClient();
        NetworkStream ns = client.GetStream();
        connections++;
        Console.WriteLine("New client accepted: {0} active connections",
                          connections);

        string welcome = "Welcome to Cins Apartment";
        data = Encoding.ASCII.GetBytes(welcome);
        ns.Write(data, 0, data.Length);
        clientStreams.Add(client, ns);

        Timer timer_exchange = new Timer(async (_) =>
        {
            // Send a request to the exchange rate API to get the TRY to USD exchange rate
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://api.freecurrencyapi.com/v1/latest?apikey=LaQTeFjKJ7LVLXH6gR43qagvweQQ2wWdZel8rFc3");

            // Read the response content
            string json = await response.Content.ReadAsStringAsync();

            byte[] exchangeRateDataBytes = Encoding.ASCII.GetBytes(json);
            BroadcastMessage(exchangeRateDataBytes);
        }, null, TimeSpan.Zero, TimeSpan.FromHours(1));

        Timer timer = new Timer(async (_) =>
        {
            // Retrieve weather data from the API
            WeatherAPI api = new WeatherAPI("7613f3ec78b94216915e7754f324a8c5");
            WeatherData weatherData = await api.GetWeatherAsync();

            // Serialize the weather data to JSON
            string json = JsonSerializer.Serialize(weatherData);

            // Broadcast the weather data to all connected clients
            byte[] weatherDataBytes = Encoding.ASCII.GetBytes(json);
            BroadcastMessage(weatherDataBytes);
        }, null, TimeSpan.Zero, TimeSpan.FromHours(1));


        while (true)
        {
            data = new byte[1024];
            try
            {
                recv = ns.Read(data, 0, data.Length);
            }
            catch (IOException e)
            {
                ns.Close();
                client.Close();
                clientStreams.Remove(client);
                connections--;
                Console.WriteLine("Client disconnected: {0} active connections", connections);
                break;
            }
            if (recv == 0)
                break;
            BroadcastMessage(data);
        }
    }

    public static void BroadcastMessage(byte[] data)
    {
        // Loop through each client's NetworkStream and send the message
        foreach (KeyValuePair<TcpClient, NetworkStream> entry in clientStreams)
        {
            NetworkStream ns = entry.Value;
            ns.Write(data, 0, data.Length);
        }
    }

}

namespace Weather
{
    public class WeatherAPI
    {
        private readonly string _apiKey;

        public WeatherAPI(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<WeatherData> GetWeatherAsync()
        {
            // Construct the URL for the request
            string url = "https://api.weatherbit.io/v2.0/current?city=Izmir&key=" + _apiKey;

            // Create an HttpClient object and make the request
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

            // Read the response content
            string json = await response.Content.ReadAsStringAsync();

            // Deserialize the JSON data into a WeatherData object, including the description field from the weather object
            return JsonSerializer.Deserialize<WeatherData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                AllowTrailingCommas = true
            });
        }
    }

    public class WeatherData
    {
        public WeatherData()
        {
            Data = new List<DataItem>();
        }

        public List<DataItem> Data { get; set; }

        public class DataItem
        {
            public decimal App_Temp { get; set; }

            public WeatherData Weather { get; set; }

            public class WeatherData
            {
                public string Description { get; set; }
            }
        }
    }
}