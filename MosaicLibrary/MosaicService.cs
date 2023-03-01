using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using IODirectory = System.IO.Directory;
using IOPath = System.IO.Path;
using System.Diagnostics;
using SixLabors.ImageSharp.Formats;

namespace MosaicLibrary
{
    public class MosaicService
    {
        public FontFamily Font { get; private set; }

        public MosaicService(Stream fontStream)
        {
            var fonts = new FontCollection();
            Font = fonts.Add(fontStream);
        }
        public MosaicService()
        {
            Font = SystemFonts.Families.First();
        }
        byte[]  getBytes(Image image, IImageFormat format)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        public async Task<byte[]> GenerateAnnotatedImage(byte[] input, string text, int imageSize)
        {
            if(text.Length > 26)
            {
                text = text.Substring(0, 26);
            }
            var pad = new String(' ', (13 - (text.Length/2)));
            text = pad + text;
            var fontSize = (int)(imageSize / 20.20);
            var border = fontSize;
            var color = Color.ParseHex("#808080"); 
            var font = Font.CreateFont(fontSize, FontStyle.Regular);
            var radius = imageSize / 2;
            var center = new PointF(radius, radius);

            IImageFormat format;
            var img = Image<Rgba32>.Load(input, out format);
            img.Mutate(o => o.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Size = new Size(imageSize, imageSize)
            }));
            var topSegment = new ArcLineSegment(center, new SizeF(radius-border, radius-border), 0, -215, 255);
            var pathBuilder = new PathBuilder();
            pathBuilder.AddSegment(topSegment);

            //var textSegment = new ArcLineSegment(center, new SizeF(radius- fontSize/2 - border, radius- fontSize/2-border), 0, 220, 100);
            var textPathBuilder = new PathBuilder();
            textPathBuilder.AddArc(center, radius - (fontSize/2) - border, radius - (fontSize/2) - border, 0, -215, -105);
            //textPathBuilder.AddLine(new PointF(10, 150), new PointF(290, 150));
            var textShape = textPathBuilder.Build();

            var textBackgroundPathBuilder = new PathBuilder();
            textBackgroundPathBuilder.AddArc(center, radius - border, radius - (fontSize/2) - border, 0, -215, -105);
            var textBackgroundShape = textBackgroundPathBuilder.Build();

            //var textSegment = new LinearLineSegment(new PointF(50, 150), new PointF(250, 150));
            //IPath textShape = new Polygon(textSegment);
            var textOptions = new TextOptions(font)
            {
                WrappingLength = textShape.ComputeLength(),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextAlignment = TextAlignment.Center
            };
            //Console.WriteLine($"Text length: {textShape.ComputeLength()}");
            var options = new DrawingOptions { GraphicsOptions = new GraphicsOptions { Antialias = true } };
            var pen = Pens.Solid(color, 1);
            img.Mutate(x => x.Draw(color, 1, pathBuilder.Build()));
            //img.Mutate(x => x.Draw(Color.Yellow, 1, textShape));
            //img.Mutate(x => x.Draw(Color.Yellow, 1, new Polygon(new LinearLineSegment(new PointF(imageSize/2, 0), new PointF(imageSize/2, imageSize)))));

            var glyphs = TextBuilder.GenerateGlyphs(text, textShape, textOptions);
            //img.Mutate(x => x.Draw(Color.Black, fontSize + 1, textBackgroundShape));
            img.Mutate(i => i.Fill(color, glyphs));
            return getBytes(img, format);
        }

        public void SaveImageWithPath(IPathCollection collection, IPath shape, params string[] path)
        {
            // Offset the shape and path collection to ensure our resultant image is
            // large enough to contain the rendered output.
            shape = shape.Translate(-collection.Bounds.Location);
            collection = collection.Translate(-collection.Bounds.Location);

            var bounds = RectangleF.Union(shape.Bounds, collection.Bounds);
            int width = (int)(bounds.Left + bounds.Right);
            int height = (int)(bounds.Top + bounds.Bottom);

            using var img = new Image<Rgba32>(width, height);

            // Fill the canvas background and draw our shape
            img.Mutate(i => i.Fill(Color.DarkBlue).Fill(Color.White.WithAlpha(.25F), shape));

            // Draw our path collection.
            img.Mutate(i => i.Fill(Color.HotPink, collection));

            // Ensure directory exists
            string fullPath = IOPath.GetFullPath(IOPath.Combine("Output", IOPath.Combine(path)));
            IODirectory.CreateDirectory(IOPath.GetDirectoryName(fullPath));
            img.Save(fullPath);
        }

        public void GenerateFromPathsAndSave(List<string> imagePaths, int imageSize, int cols, string outputFile)
        {
            var images = imagePaths.Select(p => getBytes(Image.Load<Rgba32>(p), JpegFormat.Instance)).ToList();
            var image = generate(images, imageSize, cols);
            image.Save(outputFile);
        }
        public void GenerateAndSave(List<byte[]> images, int imageSize, int cols, string outputFile)
        {
            var image = generate(images, imageSize, cols);
            image.Save(outputFile);
        }

        public string GenerateAsBase64(List<byte[]> images, int imageSize, int cols)
        {
            var image = generate(images, imageSize, cols);
            return image.ToBase64String(JpegFormat.Instance);
        }

        public async Task<byte[]> Generate(List<byte[]> images, int imageSize, int cols)
        {
            var image = generate(images, imageSize, cols);
            return getBytes(image, JpegFormat.Instance);
        }

        public Image<Rgba32> GenerateTitleImage(string title, int width, int height)
        {
            var padding = 100;
            var imgSize = new SizeF(width, height);
            var outputImage = new Image<Rgba32>(width, height);
            // Measure the text size
            Font font = Font.CreateFont(10); // for scaling water mark size is largely ignored.
            FontRectangle size = TextMeasurer.Measure(title, new TextOptions(font));

            // Find out how much we need to scale the text to fill the space (up or down)
            float scalingFactor = Math.Min((imgSize.Width - padding) / size.Width, (imgSize.Height - padding) / size.Height);

            // Create a new font
            Font scaledFont = new Font(font, scalingFactor * font.Size);

            var center = new PointF(imgSize.Width / 2, imgSize.Height / 2);
            var textOptions = new TextOptions(scaledFont)
            {
                Origin = center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            outputImage.Mutate(img => img.Fill(Color.Black));
            outputImage.Mutate(img => img.DrawText(textOptions, title, Color.DarkRed));
            return outputImage;
        }

        private Image<Rgba32> generate(List<byte[]> images, int imageSize, int cols)
        {
            if (images.Count < cols) throw new ArgumentException("Image list too small!");
            var imageCount = Math.Max(images.Count, cols);
            var rows = (int)Math.Ceiling((double)imageCount / cols);
            var imageWidth = imageSize * cols;
            var imageHeight = imageSize * (rows + 1);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var outputImage = new Image<Rgba32>(imageWidth, imageHeight);
            var dx = 0;
            var dy = 0;

            var titleImage = GenerateTitleImage("Messier Catalogue", imageWidth, imageSize);
            outputImage.Mutate(o => o
                        .DrawImage(titleImage, new Point(dx, dy), 1f)
                    );

            dy = imageSize;

            var placeholderImg = new Image<Rgba32>(imageSize, imageSize);
            placeholderImg.Mutate(i => i.Fill(Color.Black));

            foreach (var r in Enumerable.Range(0, rows))
            {
                foreach (var c in Enumerable.Range(0, cols))
                {
                    var idx = c + (r * cols);
                    Image<Rgba32> img;
                    if (idx < images.Count)
                    {
                        img = Image.Load<Rgba32>(images[idx]);
                    } else
                    {
                        img = placeholderImg;
                    }
                    
                    outputImage.Mutate(o => o
                        .DrawImage(img, new Point(dx, dy), 1f)
                    );
                    dx += imageSize;
                }
                dx = 0;
                dy += imageSize;
            }
            return outputImage;
        }
    }
}