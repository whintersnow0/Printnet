using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Routing;

class Program
{
    static async Task Main()
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
        var url = $"http://*:{port}/";
        using var server = new WebServer(url)
            .WithAction("/{animName}", HttpVerb.Get, async ctx =>
            {
                ctx.Response.ContentType = "text/plain; charset=utf-8";
                ctx.Response.Headers.Add("Cache-Control", "no-cache");
                ctx.Response.Headers.Add("Connection", "keep-alive");
                var animName = ctx.Request.Url.Segments.Last().TrimEnd('/');
                var animsFolder = Path.Combine(AppContext.BaseDirectory, "Anims");
                var filePath = Path.Combine(animsFolder, $"{animName}.json");
                if (!File.Exists(filePath))
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.SendStringAsync("Animation not found", "text/plain", Encoding.UTF8);
                    return;
                }
                var json = await File.ReadAllTextAsync(filePath);
                var animData = JsonSerializer.Deserialize<AnimationData>(json);
                int frameDelay = animData.framerate;
                string[][] frames = animData.frames;
                int squareSize = Math.Min(frames[0].Length, frames[0][0].Length);
                using var writer = new StreamWriter(ctx.Response.OutputStream, Encoding.UTF8);
                while (!ctx.CancellationToken.IsCancellationRequested)
                {
                    foreach (var frame in frames)
                    {
                        if (ctx.CancellationToken.IsCancellationRequested)
                            break;
                        var squareFrame = frame
                            .Take(squareSize)
                            .Select(line => line.Length > squareSize ? line.Substring(0, squareSize) : line.PadRight(squareSize))
                            .ToArray();
                        var frameStr = string.Join("\n", squareFrame);
                        await writer.WriteAsync(frameStr + "\n\n");
                        await writer.FlushAsync();
                        await Task.Delay(frameDelay, ctx.CancellationToken);
                    }
                }
            });
        await server.RunAsync();
        Console.WriteLine($"Server running on port {port}. Press Enter to exit.");
        Console.ReadLine();
    }
}