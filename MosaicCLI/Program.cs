
using MessierMosaic.ViewModels;
using MosaicLibrary;
using System.Text.Json;

var stream = File.OpenRead(@"C:\Users\sergi\Documents\repos\MessierMosaic\MessierMosaic\wwwroot\fonts\roboto-mono.ttf");
var service = new MosaicService(stream);
var ImageSize = 600;
var ColumnsCount = 10;
var jsonString = File.ReadAllText(@"C:\Users\sergi\Documents\repos\MessierMosaic\MessierMosaic\wwwroot\data\messier.json");
var MessierObjects = JsonSerializer.Deserialize<MessierObject[]>(jsonString)!;
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