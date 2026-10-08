using Atad.Application;
using Atad.Application.Interfaces;
using Atad.Infrastructure;
using Atad.UI;
using Atad.UI.Navigation;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

const string DefaultMongoConnectionString = "mongodb://localhost:5204";
const string DefaultMongoDatabaseName = "AtadTodoDb";

var settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
var appSettings = AppSettingsLoader.Load(settingsPath, DefaultMongoConnectionString, DefaultMongoDatabaseName);

var services = new ServiceCollection();

services.AddApplication();
services.AddInfrastructure(
    connectionString: "mongodb://localhost:5204",
    databaseName: "AtadTodoDb"
);

services.AddSingleton<NavigationContext>();
services.AddTransient<MainWindow>();

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    // Run migrators
    var migrator = scope.ServiceProvider.GetRequiredService<IMigratorService>();
    migrator.AddOrderNumbersIfTheyDoesntExistAsync().GetAwaiter().GetResult();
}

Application.Init();

var blackBgNormal = new Attribute(Color.White, Color.Black);
var blackBgFocus = new Attribute(Color.Black, Color.White);
var blackBgHot = new Attribute(Color.BrightYellow, Color.Black);

Colors.Base = new ColorScheme()
{
    Normal = blackBgNormal,
    Focus = blackBgFocus,
    HotNormal = blackBgHot,
    HotFocus = blackBgFocus
};

Colors.TopLevel = new ColorScheme()
{
    Normal = blackBgNormal,
    Focus = blackBgNormal,
    HotNormal = blackBgHot,
};

try
{
    var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
    Application.Run(mainWindow);
}
finally
{
    Application.Shutdown();
}