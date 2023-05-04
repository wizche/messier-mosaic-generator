using Blazored.Modal;
using MessierMosaic;
using MessierMosaic.Db;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MosaicLibrary;
using TG.Blazor.IndexedDB;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddSingleton(sp => httpClient);
builder.Services.AddBlazoredModal();

var stream = await httpClient.GetStreamAsync("/fonts/roboto-mono.ttf");

builder.Services.AddScoped(s =>
{
    var ms = new MosaicService(stream);
    return ms;
});

builder.Services.AddIndexedDB(dbStore =>
{
    dbStore.DbName = DbConfig.DbName;
    dbStore.Version = 1;

    dbStore.Stores.Add(new StoreSchema
    {
        Name = DbConfig.StoreName,
        PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = false },
        Indexes = new List<IndexSpec>
        {
            new IndexSpec{Name="preview", KeyPath = "preview", Auto=false},
            new IndexSpec{Name="image", KeyPath = "image", Auto=false},
            new IndexSpec{Name="content-type", KeyPath = "contenttype", Auto=false}
        }
    });
});

await builder.Build().RunAsync();
