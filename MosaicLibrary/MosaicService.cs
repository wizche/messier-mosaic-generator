using SixLabors.ImageSharp.Formats.Jpeg;

using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using IODirectory = System.IO.Directory;
using IOPath = System.IO.Path;
using System.Diagnostics;
using Microsoft.VisualBasic;

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
        byte[]  getBytes(Image image)
        {
            using (var ms = new MemoryStream())
            {
                if(image.Metadata.DecodedImageFormat != null)
                {
                    image.Save(ms, image.Metadata.DecodedImageFormat);

                } else
                {
                    image.SaveAsPng(ms);
                }
                return ms.ToArray();
            }
        }

        public BestParams generatePath(int fontSize, int imageSize, string text)
        {
            while (true) { 
                Font font = Font.CreateFont(fontSize, FontStyle.Regular);
                var radius = imageSize / 2;
                var center = new PointF(radius, radius);
                var border = fontSize;

                // Find the smallest curve encompassing the whole text
                var textSweepAngle = FindLowestSweepAngle(text, font, new SizeF(radius - fontSize / 2 - border, radius - fontSize / 2 - border));

                var fullSweepAngle = 100;
                var gapSweepAngle = (fullSweepAngle - textSweepAngle) / 2;
                var textStartAngle = -220 - gapSweepAngle;

                var textSegment = new ArcLineSegment(center, new SizeF(radius - fontSize / 2 - border, radius - fontSize / 2 - border), 0, textStartAngle, -textSweepAngle);

                IPath textShape = new Polygon(textSegment);
                TextOptions textOptions = new(font)
                {
                    WrappingLength = textShape.ComputeLength(),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextAlignment = TextAlignment.Start,
                    TextDirection = TextDirection.LeftToRight
                };
                try
                {
                    IPathCollection glyphs = TextBuilder.GenerateGlyphs(text, textShape, textOptions);
                    return new BestParams{
                        FontSize = fontSize,
                        TextGlyphs = glyphs
                    };

                } catch(InvalidOperationException ex)
                {
                    // when trying to fit a too big text this will raise an invalid operation, so we scale down and retry
                    fontSize -= 1;
                    continue;
                }
            }
        }

        public struct BestParams
        {
            public int FontSize;
            public IPathCollection TextGlyphs;
        }

        public async Task<byte[]> GenerateAnnotatedImage(byte[] input, string text, int imageSize)
        {
            if (text.Length > 25)
            {
                text = text.Substring(0, 25);
            }
            var img = Image.Load(input);
          
            img.Mutate(o => o.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Size = new Size(imageSize, imageSize)
            }));

            var fontSize = (int)(imageSize / 18.00);
            var color = Color.ParseHex("#808080");

            var radius = imageSize / 2;
            var center = new PointF(radius, radius);

            var bestParams = generatePath(fontSize, imageSize, text);
            IPathCollection glyphs = bestParams.TextGlyphs;
            img.Mutate(i => i.Fill(color, glyphs));

            var border = bestParams.FontSize;
            var topSegment = new ArcLineSegment(center, new SizeF(radius - border, radius - border), 0, -220, 260);
            img.Mutate(x => x.Draw(color, 2, new PathBuilder().AddSegment(topSegment).Build()));
            //var bottomSegment = new ArcLineSegment(center, new SizeF(radius - fontSize / 2 - border, radius - fontSize / 2 - border), 0, -220, -100);
            //img.Mutate(x => x.Draw(color, 2, new PathBuilder().AddSegment(bottomSegment).Build()));

            return getBytes(img);
        }


        private float FindLowestSweepAngle(string text, Font font, SizeF radius)
        {
            var low = 0.1f;
            var high = 359.9f;
            var step = 0.1f;

            while (low < high)
            {
                var mid = (low + high) / 2;
                var arcLineSegment = new ArcLineSegment(PointF.Empty, radius, 0, 0, mid);
                var polygon = new Polygon(arcLineSegment);

                TextOptions textOptions = new(font)
                {
                    WrappingLength = polygon.ComputeLength(),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextAlignment = TextAlignment.Start,
                    TextDirection = TextDirection.LeftToRight
                };

                try
                {
                    TextBuilder.GenerateGlyphs(text, polygon, textOptions);
                    high = mid - step; // Curve is too big. Keep finding a smaller one
                }
                catch // InvalidOperationException: Should always reach a point along the path
                {
                    low = mid + step; // Curve is too small to hold the whole text
                }
            }

            return low;
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
            var images = imagePaths.Select(p => getBytes(Image.Load<Rgba32>(p))).ToList();
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
            return getBytes(image);
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

        public void DrawTest(string text)
        {
            var imageSize = 600;
            if (text.Length > 28)
            {
                text = text.Substring(0, 28);
            }
            var fontSize = 17;
            var border = 10;
            var color = Color.ParseHex("#808080");
            Font font = Font.CreateFont(fontSize, FontStyle.Regular);
            var radius = imageSize / 2;
            var center = new PointF(radius, radius);

            var img = Image<Rgba32>.Load(@"C:\Users\sergi\Documents\repos\MessierMosaic\MessierMosaic\wwwroot\images\missing.png");
            img.Mutate(o => o.Resize(new Size(imageSize, imageSize)));
            var topSegment = new ArcLineSegment(center, new SizeF(radius - border, radius - border), 0, -220, 260);
            PathBuilder pathBuilder = new PathBuilder();
            pathBuilder.AddSegment(topSegment);

            var bottomSegment = new ArcLineSegment(center, new SizeF(radius - fontSize / 2 - border, radius - fontSize / 2 - border), 0, -220, -100);

            // Find the smallest curve encompassing the whole text
            var textSweepAngle = FindLowestSweepAngle(text, font, new SizeF(radius - fontSize / 2 - border, radius - fontSize / 2 - border));

            var fullSweepAngle = 100;
            var gapSweepAngle = (fullSweepAngle - textSweepAngle) / 2;
            var textStartAngle = -220 - gapSweepAngle;

            var textSegment = new ArcLineSegment(center, new SizeF(radius - fontSize / 2 - border, radius - fontSize / 2 - border), 0, textStartAngle, -textSweepAngle);

            IPath textShape = new Polygon(textSegment);
            TextOptions textOptions = new(font)
            {
                WrappingLength = textShape.ComputeLength(),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextAlignment = TextAlignment.Start,
                TextDirection = TextDirection.LeftToRight
            };
            DrawingOptions options = new()
            {
                GraphicsOptions = new()
                {
                    ColorBlendingMode = PixelColorBlendingMode.Multiply
                }
            };
            img.Mutate(x => x.Draw(color, 2, pathBuilder.Build()));
            img.Mutate(x => x.Draw(color, 2, new PathBuilder().AddSegment(bottomSegment).Build()));

            IPathCollection glyphs = TextBuilder.GenerateGlyphs(text, textShape, textOptions);
            img.Mutate(i => i.Fill(color, glyphs));
            string fullPath = IOPath.GetFullPath(IOPath.Combine("Output", IOPath.Combine("test.png")));
            IODirectory.CreateDirectory(IOPath.GetDirectoryName(fullPath));
            img.Save(fullPath);
            Console.WriteLine($"Saved to {fullPath}");
        }

    }

}