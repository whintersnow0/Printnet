using System;
using System.Text;
using EmbedIO;
using EmbedIO.Actions;

class Program
{
    static void Main()
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
        var url = $"http://*:{port}/";

        using var server = new WebServer(url)
            .WithAction("/", HttpVerb.Get,
                ctx => ctx.SendStringAsync("Hello from console server!", "text/plain", Encoding.UTF8));

        server.RunAsync(); 

        Console.WriteLine($"Server running on port {port}. Press Enter to exit.");
        Console.ReadLine();
    }
}
