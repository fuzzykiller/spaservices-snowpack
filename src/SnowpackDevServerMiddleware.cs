/* Copyright 2021 Daniel Betz

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.NodeServices.Npm;
using Microsoft.AspNetCore.NodeServices.Util;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.Extensions.Util;
using Microsoft.AspNetCore.SpaServices.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static System.FormattableString;

namespace SpaServices.SnowpackDevServer
{
    public static class SnowpackDevServerMiddleware
    {
        private const string LogCategoryName = "SnowpackDevServer";
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5);

        public static void Attach(ISpaBuilder spaBuilder, string scriptName)
        {
            var pkgManagerCommand = spaBuilder.Options.PackageManagerCommand;
            var sourcePath = spaBuilder.Options.SourcePath;
            var devServerPort = spaBuilder.Options.DevServerPort;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));
            }

            if (string.IsNullOrEmpty(scriptName))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(scriptName));
            }

            var appBuilder = spaBuilder.ApplicationBuilder;
            var applicationStoppingToken = appBuilder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);
            var diagnosticSource = appBuilder.ApplicationServices.GetRequiredService<DiagnosticSource>();
            var portTask = StartSnowpackServerAsync(sourcePath, scriptName, pkgManagerCommand, devServerPort, logger, diagnosticSource, applicationStoppingToken);

            var targetUriTask = portTask.ContinueWith(task => new UriBuilder("http", "localhost", task.Result).Uri);

            spaBuilder.UseProxyToSpaDevelopmentServer(() =>
            {
                var timeout = spaBuilder.Options.StartupTimeout;
                return targetUriTask.WithTimeout(timeout, "The Snowpack server did not start listening for requests " +
                                                          $"within the timeout period of {timeout.TotalSeconds} seconds. " +
                                                          "Check the log output for error information.");
            });
        }

        private static async Task<int> StartSnowpackServerAsync(
            string sourcePath, string scriptName, string pkgManagerCommand, int portNumber, ILogger logger, DiagnosticSource diagnosticSource, CancellationToken applicationStoppingToken)
        {
            if (portNumber == 0)
            {
                portNumber = TcpPortFinder.FindAvailablePort();
            }

            logger.LogInformation($"Starting Snowpack server on port {portNumber}...");

            var scriptRunner =
                new NodeScriptRunner(sourcePath, scriptName,
                    Invariant($"--open none --output stream --port {portNumber}"), null, pkgManagerCommand, diagnosticSource, applicationStoppingToken);
            scriptRunner.AttachToLogger(logger);

            using (var stdErrReader = new EventedStreamStringReader(scriptRunner.StdOut))
            {
                try
                {
                    await scriptRunner.StdOut.WaitForMatch(
                        new Regex("Server started", RegexOptions.None, RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{scriptName}' exited without indicating that the " +
                        $"snowpack server was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            return portNumber;
        }
    }
}