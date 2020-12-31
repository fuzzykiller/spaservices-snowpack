/* Copyright 2020 Daniel Betz

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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.NodeServices.Npm;
using Microsoft.AspNetCore.NodeServices.Util;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.Extensions.Util;
using Microsoft.AspNetCore.SpaServices.Util;
using Microsoft.Extensions.Logging;
using static System.FormattableString;

namespace SpaServices.SnowpackDevServer
{
    public static class SnowpackDevServerMiddleware
    {
        private const string LogCategoryName = "SnowpackDevServer";
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5);

        public static void Attach(ISpaBuilder spaBuilder, string npmScriptName)
        {
            var sourcePath = spaBuilder.Options.SourcePath;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));
            }

            if (string.IsNullOrEmpty(npmScriptName))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(npmScriptName));
            }

            var appBuilder = spaBuilder.ApplicationBuilder;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);
            var portTask = StartSnowpackServerAsync(sourcePath, npmScriptName, logger);

            var targetUriTask = portTask.ContinueWith(task => new UriBuilder("http", "localhost", task.Result).Uri);

            spaBuilder.UseProxyToSpaDevelopmentServer(() =>
            {
                var timeout = spaBuilder.Options.StartupTimeout;
                return targetUriTask.WithTimeout(timeout, "The Snowpack server did not start listening for requests " +
                                                          $"within the timeout period of {timeout.TotalSeconds} seconds. " +
                                                          "Check the log output for error information.");
            });
        }

        private static async Task<int> StartSnowpackServerAsync(string sourcePath, string npmScriptName, ILogger logger)
        {
            var portNumber = TcpPortFinder.FindAvailablePort();
            logger.LogInformation($"Starting Snowpack server on port {portNumber}...");

            var npmScriptRunner =
                new NpmScriptRunner(sourcePath, npmScriptName,
                    Invariant($"--open none --output stream --port {portNumber}"), null);
            npmScriptRunner.AttachToLogger(logger);

            using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdOut))
            {
                try
                {
                    await npmScriptRunner.StdOut.WaitForMatch(
                        new Regex("Server started", RegexOptions.None, RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{npmScriptName}' exited without indicating that the " +
                        $"snowpack server was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            return portNumber;
        }
    }
}