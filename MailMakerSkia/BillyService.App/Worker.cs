using System.Reflection;
using SkiaSharp;

namespace BillyService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;

    public Worker(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Decode bitmap, create image info and surface
        SKBitmap bitmap = SKBitmap.Decode(@"C:\Users\billy\Desktop\MailMakerSkia\BillyService.App\Raf.bmp");
        SKImageInfo imageInfo = new(bitmap.Width, bitmap.Height);
        SKSurface surface = SKSurface.Create(imageInfo);

        // Draw bitmap on canvas
        surface.Canvas.DrawBitmap(bitmap, 0, 0);

        // Screenshot surface, convert to bitmap
        SKBitmap output = SKBitmap.FromImage(surface.Snapshot());
        BmpSharp.Bitmap bmp = new(output.Width, output.Height, output.Bytes, BmpSharp.BitsPerPixelEnum.RGBA32);

        File.WriteAllBytes(@"C:\Users\billy\Desktop\asdf.bmp", bmp.GetBmpBytes(true));
    }
}
