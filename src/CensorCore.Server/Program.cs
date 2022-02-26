using CensorCore;
using CensorCore.Censoring;
using CensorCore.ModelLoader;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var loader = new ModelLoaderBuilder()
        .AddDefaultPaths()
        // .AddSearchPath(config)
        .SearchAssembly(System.Reflection.Assembly.GetEntryAssembly())
        .Build();
var model = await loader.GetModel();

builder.Services.AddControllers().AddApplicationPart(typeof(CensorCore.Web.CensoringController).Assembly);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IImageHandler>(p => new ImageSharpHandler());
builder.Services.AddSingleton<ModelLoader>(p => {
    return new ModelLoaderBuilder()
        .AddDefaultPaths()
        // .AddSearchPath(config)
        .SearchAssembly(System.Reflection.Assembly.GetEntryAssembly())
        .Build();
});
builder.Services.AddSingleton<AIService>(p => {
    // var loader = p.GetRequiredService<ModelLoader>();
    // var model = await loader.GetModel();
    if (model == null) {
        throw new InvalidOperationException("Could not load model from any available source!");
    }
    return AIService.Create(model, p.GetRequiredService<IImageHandler>());
});

builder.Services.AddSingleton<IAssetStore, EmptyAssetStore>();
builder.Services.AddSingleton<GlobalCensorOptions>();
builder.Services.AddSingleton<ICensorTypeProvider, BlurProvider>();
builder.Services.AddSingleton<ICensorTypeProvider, PixelationProvider>();
builder.Services.AddSingleton<ICensorTypeProvider, BlackBarProvider>();
builder.Services.AddSingleton<ICensorTypeProvider, StickerProvider>();
builder.Services.AddSingleton<ICensoringProvider, ImageSharpCensoringProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
