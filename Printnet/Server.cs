using System;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;

class Program
{
    static async Task Main()
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
        var url = $"http://*:{port}/";

        using var server = new WebServer(url)
            .WithAction("/", HttpVerb.Get,
                ctx => ctx.SendStringAsync("Hello from Server!", "text/plain", Encoding.UTF8));

        await server.RunAsync(); 
    }
}
