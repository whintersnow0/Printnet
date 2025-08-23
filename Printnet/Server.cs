using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Routing;

class Program
{
    private static readonly Dictionary<string, PrecomputedAnimation> AnimationCache = new();
    private static readonly object CacheLock = new object();

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

                try
                {
                    var animData = await LoadAnimationAsync(animName);
                    if (animData == null)
                    {
                        ctx.Response.StatusCode = 404;
                        await ctx.SendStringAsync("Animation not found", "text/plain", Encoding.UTF8);
                        return;
                    }

                    await StreamPrecomputedAnimationAsync(ctx, animData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error streaming animation {animName}: {ex.Message}");
                    ctx.Response.StatusCode = 500;
                    await ctx.SendStringAsync("Internal server error", "text/plain", Encoding.UTF8);
                }
            });

        await server.RunAsync();
        Console.WriteLine($"Server running on port {port}. Press Enter to exit.");
        Console.ReadLine();
    }

    private static async Task<PrecomputedAnimation> LoadAnimationAsync(string animName)
    {
        lock (CacheLock)
        {
            if (AnimationCache.TryGetValue(animName, out var cachedData))
                return cachedData;
        }

        var animsFolder = Path.Combine(AppContext.BaseDirectory, "Anims");
        var filePath = Path.Combine(animsFolder, $"{animName}.json");

        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var rawAnimData = JsonSerializer.Deserialize<AnimationData>(json);

            var processedAnimData = PreprocessAnimation(rawAnimData);
            var precomputedAnim = PrecomputeFrames(processedAnimData);

            var estimatedSize = EstimatePrecomputedMemoryUsage(precomputedAnim);
            if (estimatedSize < 50 * 1024 * 1024)
            {
                lock (CacheLock)
                {
                    AnimationCache[animName] = precomputedAnim;
                }
            }

            return precomputedAnim;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading animation {animName}: {ex.Message}");
            return null;
        }
    }

    private static AnimationData PreprocessAnimation(AnimationData animData)
    {
        if (animData.frames == null || animData.frames.Length == 0)
            return animData;

        int maxWidth = animData.frames[0].Max(line => line?.Length ?? 0);
        int maxHeight = animData.frames[0].Length;
        int squareSize = Math.Min(maxWidth, maxHeight);

        const int MAX_SQUARE_SIZE = 200;
        if (squareSize > MAX_SQUARE_SIZE)
        {
            squareSize = MAX_SQUARE_SIZE;
            Console.WriteLine($"Animation size reduced from original to {MAX_SQUARE_SIZE}x{MAX_SQUARE_SIZE} for performance");
        }

        var processedFrames = new string[animData.frames.Length][];

        for (int i = 0; i < animData.frames.Length; i++)
        {
            var frame = animData.frames[i];
            processedFrames[i] = frame
                .Take(squareSize)
                .Select(line =>
                {
                    if (line == null) return new string(' ', squareSize);
                    return line.Length > squareSize ?
                        line.Substring(0, squareSize) :
                        line.PadRight(squareSize);
                })
                .ToArray();
        }

        return new AnimationData
        {
            frames = processedFrames,
            framerate = Math.Max(animData.framerate, 16)
        };
    }

    private static PrecomputedAnimation PrecomputeFrames(AnimationData animData)
    {
        var compiledFrames = new byte[animData.frames.Length][];

        for (int i = 0; i < animData.frames.Length; i++)
        {
            var frameText = string.Join("\n", animData.frames[i]) + "\n\n";
            compiledFrames[i] = Encoding.UTF8.GetBytes(frameText);
        }

        return new PrecomputedAnimation
        {
            CompiledFrames = compiledFrames,
            FrameDelay = CalculateAdaptiveDelay(animData.frames[0], animData.framerate)
        };
    }

    private static async Task StreamPrecomputedAnimationAsync(IHttpContext ctx, PrecomputedAnimation animData)
    {
        var outputStream = ctx.Response.OutputStream;

        while (!ctx.CancellationToken.IsCancellationRequested)
        {
            for (int frameIndex = 0; frameIndex < animData.CompiledFrames.Length; frameIndex++)
            {
                if (ctx.CancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var frameBytes = animData.CompiledFrames[frameIndex];
                    await outputStream.WriteAsync(frameBytes, 0, frameBytes.Length, ctx.CancellationToken);
                    await outputStream.FlushAsync(ctx.CancellationToken);

                    await Task.Delay(animData.FrameDelay, ctx.CancellationToken);
                }
                catch (Exception ex) when (ex is ObjectDisposedException || ex is IOException)
                {
                    Console.WriteLine($"Client disconnected during animation streaming");
                    return;
                }
            }
        }
    }

    private static int CalculateAdaptiveDelay(string[] firstFrame, int baseDelay)
    {
        if (firstFrame == null || firstFrame.Length == 0)
            return baseDelay;

        int frameSize = firstFrame.Sum(line => line?.Length ?? 0);

        if (frameSize > 50000)
            return Math.Max(baseDelay * 3, 100);
        else if (frameSize > 20000)
            return Math.Max(baseDelay * 2, 50);
        else if (frameSize > 5000)
            return Math.Max((int)(baseDelay * 1.5), 30);
        else
            return Math.Max(baseDelay, 16);
    }

    private static long EstimatePrecomputedMemoryUsage(PrecomputedAnimation animData)
    {
        if (animData?.CompiledFrames == null)
            return 0;

        return animData.CompiledFrames.Sum(frame => frame?.Length ?? 0);
    }
}
