# SpaServices.SnowpackDevServer

[![CI Build](https://github.com/fuzzykiller/spaservices-snowpack/workflows/CI%20Build/badge.svg)](https://github.com/fuzzykiller/spaservices-snowpack/actions)
[![Nuget](https://img.shields.io/nuget/v/SpaServices.SnowpackDevServer)](https://www.nuget.org/packages/SpaServices.SnowpackDevServer)

Brings plug'n'play support for Snowpack to ASP.NET Core. [Snowpack](https://www.snowpack.dev)
is a build tool that enables lightning-fast development by not packing JS code at all.

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

## Known issues

Because `Microsoft.AspNetCore.SpaServices.Extensions` has
[a bug with forwarding WebSocket connections](https://github.com/dotnet/aspnetcore/issues/23207) in
ASP.NET Core 3.1, auto-refresh does not work in version 1.0 (for ASP.NET Core 3.1).

## Copyright

Includes code taken directly from `Microsoft.AspNetCore.SpaServices.Extensions`. This includes
all source files included from the `aspnetcore` submodule.

## License

Available under Apache License 2.0
