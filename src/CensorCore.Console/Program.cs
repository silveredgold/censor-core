using CensorCore.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(c => {
    c.PropagateExceptions();
    c.AddCommand<CensorCommand>("censor-image");
});
return await app.RunAsync(args);

