using MessierMosaic;
using MessierMosaic.Db;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TG.Blazor.IndexedDB;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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
