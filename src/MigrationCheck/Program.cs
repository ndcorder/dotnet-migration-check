using MigrationCheck.Commands;
using Spectre.Console.Cli;

var app = new CommandApp<CheckCommand>();
app.Configure(config =>
{
    config.SetApplicationName("dotnet-migration-check");
    config.SetApplicationVersion("0.1.0");
});
return app.Run(args);
