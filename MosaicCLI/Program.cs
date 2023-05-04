
using MessierMosaic.ViewModels;
using MosaicLibrary;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json;

/*
var stream = File.OpenRead(@"C:\Users\sergi\source\repos\MessierMosaic\MessierMosaic\wwwroot\fonts\roboto-mono.ttf");
var service = new MosaicService(stream);
var comet = @"C:\Users\sergi\Downloads\comet.png";
var bytes = File.ReadAllBytes(comet);
var result = service.GenerateAnnotatedImage(bytes, "M2", 600);
File.WriteAllBytes(@"C:\Users\sergi\source\repos\MessierMosaic\MosaicCLI\bin\Debug\net7.0\Output\test.png", result);


var stream = File.OpenRead(@"C:\Users\sergi\source\repos\MessierMosaic\MessierMosaic\wwwroot\fonts\roboto-mono.ttf");
var service = new MosaicService(stream);
var ImageSize = 600;
var ColumnsCount = 10;
var testPath = @"C:\Users\sergi\Downloads\comet.png";
var jsonString = File.ReadAllText(@"C:\Users\sergi\source\repos\MessierMosaic\MessierMosaic\wwwroot\data\messier.json");
var MessierObjects = JsonSerializer.Deserialize<MessierObject[]>(jsonString)!;
var imageBytes = File.ReadAllBytes(testPath);
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Image Crawler");

var images = new List<byte[]>();
foreach (var id in Enumerable.Range(1, 110))
{
    Console.WriteLine($"Processing {id}");
    var obj = MessierObjects.Where(o => o.Name == $"M{id}").First();
    var original = await httpClient.GetAsync(obj.PictureUrl);
    var imgBytes = await original.Content.ReadAsByteArrayAsync();
    var image = await service.GenerateAnnotatedImage(imgBytes, obj.GetPictureDescription(), ImageSize);
    images.Add(image);
}
service.GenerateAndSave(images, ImageSize, ColumnsCount, "Output/test.png");

// service.GenerateTitleImage("Messier Catalogue", ImageSize*ColumnsCount, ImageSize)
//    .SaveAsPng("Output/title.png");
*/
//var stream = File.OpenRead(@"C:\Users\sergi\Documents\repos\MessierMosaic\MessierMosaic\wwwroot\fonts\roboto-mono.ttf");
var stream = File.OpenRead(@"C:\Users\sergi\Documents\repos\MessierMosaic\MessierMosaic\wwwroot\fonts\roboto-mono.ttf");
var service = new MosaicService(stream);
service.DrawTest("M12 - WTFFFFFFFFFFFFFFFFFFFFF");