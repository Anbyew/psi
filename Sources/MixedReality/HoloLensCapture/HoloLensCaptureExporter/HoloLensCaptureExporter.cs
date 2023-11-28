﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureExporter
{
    using System;
    using System.Collections.Generic;
    using CommandLine;

    /// <summary>
    /// Tool to export \psi store data persisted by HoloLensCaptureServer to other formats.
    /// </summary>
    internal class HoloLensCaptureExporter
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Command-line status.</returns>
        public static int Main(string[] args)
        {
            Console.WriteLine($"HoloLensCaptureExporter Tool");
            try
            {
                return Parser.Default.ParseArguments<Verbs.ExportCommand>(args)
                    .MapResult(
                        (Verbs.ExportCommand command) =>
                        {
                            if (command.AudioOnly)
                            {
                                return DataExporter.RunAudioOnly(command);
                            }
                            else
                            {
                                return DataExporter.Run(command);
                            }
                        },
                        DisplayParseErrors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Display command-line parser errors.
        /// </summary>
        /// <param name="errors">Errors reported.</param>
        /// <returns>Success flag.</returns>
        private static int DisplayParseErrors(IEnumerable<Error> errors)
        {
            Console.WriteLine("Errors:");
            var ret = 0;
            foreach (var error in errors)
            {
                Console.WriteLine($"{error}");
                if (error.StopsProcessing)
                {
                    ret = 1;
                }
            }

            return ret;
        }

        /// <summary>
        /// EyeGazeData to be exported.
        /// </summary>
        internal class EyeGazeData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EyeGazeData"/> class.
            /// </summary>
            /// <param name="valid">The valid.</param>
            /// <param name="pixelX">The pixel x.</param>
            /// <param name="pixelY">The pixel y.</param>
            public EyeGazeData(bool valid, double pixelX, double pixelY)
            {
                this.Valid = valid;
                this.PixelX = pixelX;
                this.PixelY = pixelY;
            }

            /// <summary>
            /// Gets a value indicating whether this is a valid datapoint.
            /// </summary>
            public bool Valid { get; private set; }

            /// <summary>
            /// Gets a value indicating the pixel x coordinate.
            /// </summary>
            public double PixelX { get; private set; }

            /// <summary>
            /// Gets a value indicating the pixel y coordinate.
            /// </summary>
            public double PixelY { get; private set; }
        }
    }
}
