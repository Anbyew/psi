// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureExporter
{
    using CommandLine;

    /// <summary>
    /// Command-line verbs.
    /// </summary>
    internal class Verbs
    {
        /// <summary>
        /// Base command-line options.
        /// </summary>
        internal class ExportCommand
        {
            /// <summary>
            /// Gets or sets a value indicating whether to export the audio data only.
            /// </summary>
            [Option('a', "audio-only", Required = false, HelpText = "Whether to export the audio data only (default: false).")]
            public bool AudioOnly { get; set; } = false;

            /// <summary>
            /// Gets or sets the file path of the input Psi data store.
            /// </summary>
            [Option('n', "name", Required = false, HelpText = "Name of the input Psi data store (default: HoloLensCapture).")]
            public string StoreName { get; set; } = "HoloLensCapture";

            /// <summary>
            /// Gets or sets the file path of the input Psi data store.
            /// </summary>
            [Option('p', "path", Required = true, HelpText = "Path to the input Psi data store.")]
            public string StorePath { get; set; }

            /// <summary>
            /// Gets or sets the output path to export data to.
            /// </summary>
            [Option('o', "output", Required = true, HelpText = "Output path to export data to.")]
            public string OutputPath { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to export step detections.
            /// </summary>
            [Option('s', "step-detection", Required = false, HelpText = "Whether to export step detection segments (default: false).")]
            public bool StepDetection { get; set; } = false;

            /// <summary>
            /// Gets or sets a value indicating whether to export ASR text annotations.
            /// </summary>
            [Option('t', "text-asr", Required = false, HelpText = "Whether to export ASR text (default: false).")]
            public bool TextASR { get; set; } = false;

            /// <summary>
            /// Gets or sets the file path of the input Psi data store for step detection.
            /// </summary>
            [Option("step-detection-name", Required = false, HelpText = "Name of the input Psi data store for step detection (default: anno_step).")]
            public string StoreNameStepDetection { get; set; } = "anno_step";

            /// <summary>
            /// Gets or sets the file path of the input Psi data store for (user microphone) ASR annotations.
            /// </summary>
            [Option("text-asr-name-user", Required = false, HelpText = "Name of the input Psi data store for (user) ASR annotations (default: anno_hl).")]
            public string StoreNameTextASRUser { get; set; } = "anno_hl";

            /// <summary>
            /// Gets or sets the file path of the input Psi data store for (instructor microphone) ASR annotations.
            /// </summary>
            [Option("text-asr-name-instructor", Required = false, HelpText = "Name of the input Psi data store for (instructor) ASR annotations (default: anno_ext).")]
            public string StoreNameTextASRInstructor { get; set; } = "anno_ext";
        }
    }
}