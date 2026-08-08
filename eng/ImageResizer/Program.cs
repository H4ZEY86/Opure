using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: ImageResizer <sourcePng> <outputDir>");
            return;
        }

        string sourcePath = args[0];
        string outputDir = args[1];
        
        using (Bitmap original = new Bitmap(sourcePath))
        {
            // Create PNGs
            SaveResized(original, Path.Combine(outputDir, "SplashScreen.png"), 620, 300);
            SaveResized(original, Path.Combine(outputDir, "Square150x150Logo.png"), 150, 150);
            SaveResized(original, Path.Combine(outputDir, "Square44x44Logo.png"), 44, 44);
            SaveResized(original, Path.Combine(outputDir, "StoreLogo.png"), 50, 50);
            SaveResized(original, Path.Combine(outputDir, "Wide310x150Logo.png"), 310, 150);
            
            // Create ICO
            using (var ms = new MemoryStream())
            {
                using (var icoBmp = ResizeImage(original, 256, 256))
                {
                    icoBmp.Save(ms, ImageFormat.Png);
                    byte[] pngBytes = ms.ToArray();
                    
                    string icoPath = Path.Combine(outputDir, "icon.ico");
                    using (var fs = new FileStream(icoPath, FileMode.Create))
                    using (var writer = new BinaryWriter(fs))
                    {
                        writer.Write((short)0); // reserved
                        writer.Write((short)1); // type (1 = ico)
                        writer.Write((short)1); // count
                        
                        writer.Write((byte)0); // width (0 = 256)
                        writer.Write((byte)0); // height (0 = 256)
                        writer.Write((byte)0); // color count
                        writer.Write((byte)0); // reserved
                        writer.Write((short)1); // color planes
                        writer.Write((short)32); // bpp
                        writer.Write((int)pngBytes.Length); // size
                        writer.Write((int)22); // offset
                        
                        writer.Write(pngBytes);
                    }
                }
            }
        }
    }

    static void SaveResized(Bitmap original, string path, int width, int height)
    {
        using (var resized = ResizeImage(original, width, height))
        {
            resized.Save(path, ImageFormat.Png);
        }
    }

    static Bitmap ResizeImage(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                // Center the image maintaining aspect ratio
                float ratioX = (float)width / image.Width;
                float ratioY = (float)height / image.Height;
                float ratio = Math.Min(ratioX, ratioY);
                
                int newWidth = (int)(image.Width * ratio);
                int newHeight = (int)(image.Height * ratio);
                
                int posX = (width - newWidth) / 2;
                int posY = (height - newHeight) / 2;
                
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(image, new Rectangle(posX, posY, newWidth, newHeight), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }
        return destImage;
    }
}
