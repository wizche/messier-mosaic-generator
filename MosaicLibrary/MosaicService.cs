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

        public byte[] GeneratePreview(byte[] preview, string text)
        {
            var imageSize = 300;
            var fontSize = (int)(imageSize / 18.75);
            var border = fontSize / 2;
            var color = Color.ParseHex("#808080"); 
            var font = Font.CreateFont(fontSize, FontStyle.Regular);
            var radius = imageSize / 2;
            var center = new PointF(radius, radius);

            IImageFormat format;
            var img = Image<Rgba32>.Load(preview, out format);
            img.Mutate(o => o.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Size = new Size(300, 300)
            }));
            var topSegment = new ArcLineSegment(center, new SizeF(radius-border, radius-border), 0, -215, 255);
            PathBuilder pathBuilder = new PathBuilder();
            pathBuilder.AddSegment(topSegment);

            var textSegment = new ArcLineSegment(center, new SizeF(radius- fontSize/2 - border, radius- fontSize/2-border), 0, -220, -100);
            IPath textShape = new Polygon(textSegment);
            TextOptions textOptions = new(font)
            {
                //WrappingLength = textShape.ComputeLength(),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextAlignment = TextAlignment.Center,
                TextDirection = TextDirection.LeftToRight,
                
            };
            DrawingOptions options = new()
            {
                GraphicsOptions = new()
                {
                    ColorBlendingMode = PixelColorBlendingMode.Multiply
                }
            };
            IPen pen = Pens.Solid(color, 1);
            img.Mutate(x => x.Draw(color, 1, pathBuilder.Build()));
            //img.Mutate(x => x.Draw(Color.Yellow, 1, new PathBuilder().AddSegment(textSegment).Build()));

            IPathCollection glyphs = TextBuilder.GenerateGlyphs(text, textShape, textOptions);
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

        private Image<Rgba32> generate(List<byte[]> images, int imageSize, int cols)
        {
            if (images.Count < cols) throw new ArgumentException("Image list too small!");
            var imageCount = Math.Max(images.Count, cols);
            var rows = (int)Math.Floor((double)imageCount / cols);
            var imageWidth = imageSize * cols;
            var imageHeight = imageSize * rows;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var outputImage = new Image<Rgba32>(imageWidth, imageHeight);
            var dx = 0;
            var dy = 0;


            foreach (var r in Enumerable.Range(0, rows))
            {
                foreach (var c in Enumerable.Range(0, cols))
                {
                    Image<Rgba32> img1 = Image.Load<Rgba32>(images[c]);
                    img1.Mutate(o => o.Resize(new Size(imageSize, imageSize)));
                    outputImage.Mutate(o => o
                        .DrawImage(img1, new Point(dx, dy), 1f)
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