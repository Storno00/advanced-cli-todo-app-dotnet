using System.Diagnostics;
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

var services = new ServiceCollection();

services.AddInfrastructure(
    connectionString: "mongodb://localhost:27017",
    databaseName: "AtadTodoDb"
);

services.AddSingleton<NavigationContext>();
services.AddTransient<MainWindow>();

var serviceProvider = services.BuildServiceProvider();

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