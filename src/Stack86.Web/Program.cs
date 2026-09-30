using Stack86.Web;

var app = WebApplication.CreateBuilder(args)
    .ConfigureSerilog()
    .ConfigureServices()
    .ConfigureApp();

await app.RunAsync();

#pragma warning disable SA1402 // File may only contain a single type
public partial class Program
{
}
#pragma warning restore SA1402
