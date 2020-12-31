# SpaServices.SnowpackDevServer

Brings plug'n'play support for Snowpack to ASP.NET Core. [Snowpack](https://www.snowpack.dev)
is a build tool that enabled lighning-fast development by not packing JS code at all.

## Usage:

    app.UseSpa(
        spa =>
        {
            spa.Options.SourcePath = "ClientApp";

            if (env.IsDevelopment())
            {
                spa.UseSnowpackDevServer(npmScript: "start");
            }
        });

## Copyright

Includes code taken directly from `Microsoft.AspNetCore.SpaServices.Extensions`. This includes
all source files included from the `aspnetcore` submodule and `EventedStreamReader.cs` (a newer
version because of critical bugfixes).

## License

Available under Apache License 2.0
