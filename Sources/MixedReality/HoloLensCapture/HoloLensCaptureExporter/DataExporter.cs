﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureExporter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;
    using OpenXRHand = Microsoft.Psi.MixedReality.OpenXR.Hand;
    using StereoKitHand = Microsoft.Psi.MixedReality.StereoKit.Hand;
    using WinRTEyes = Microsoft.Psi.MixedReality.WinRT.Eyes;

    /// <summary>
    /// Implements the data exporter.
    /// </summary>
    internal class DataExporter
    {
        private const string VideoStreamName = "VideoImageCameraView";
        private const string VideoEncodedStreamName = "VideoEncodedImageCameraView";
        private const string AudioStreamName = "Audio";

        /// <summary>
        /// Exports data based on the specified export command.
        /// </summary>
        /// <param name="exportCommand">The export command.</param>
        /// <returns>An error code or 0 if success.</returns>
        public static int Run(Verbs.ExportCommand exportCommand)
        {
            // Create a pipeline
            using var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.Throttle);

            // Open the psi store for reading
            var store = PsiStore.Open(p, exportCommand.StoreName, exportCommand.StorePath);

            // Open the "anno" psi store for reading annotations and step segments
            PsiImporter stepStore = null;
            PsiImporter asrUserStore = null;
            PsiImporter asrInstructorStore = null;
            if (exportCommand.StepDetection)
            {
                stepStore = PsiStore.Open(p, exportCommand.StoreNameStepDetection, exportCommand.StorePath);
            }

            if (exportCommand.TextASR)
            {
                asrUserStore = PsiStore.Open(p, exportCommand.StoreNameTextASRUser, exportCommand.StorePath);
                asrInstructorStore = PsiStore.Open(p, exportCommand.StoreNameTextASRInstructor, exportCommand.StorePath);
            }

            // Get references to the various streams. If a stream is not present in the store,
            // the reference will be null.
            var accelerometer = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Accelerometer");
            var gyroscope = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Gyroscope");
            var magnetometer = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Magnetometer");
            var head = store.OpenStreamOrDefault<CoordinateSystem>("Head");
            var audio = store.OpenStreamOrDefault<AudioBuffer>(AudioStreamName);
            var externalMicrophone = store.OpenStreamOrDefault<AudioBuffer>("ExternalMicrophone");
            var videoEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>(VideoEncodedStreamName);
            var videoImageCameraView = store.OpenStreamOrDefault<ImageCameraView>(VideoStreamName);
            var previewEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("PreviewEncodedImageCameraView");
            var previewImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("PreviewImageCameraView");
            var depthImageCameraView = store.OpenStreamOrDefault<DepthImageCameraView>("DepthImageCameraView");
            var depthCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("DepthCalibrationMap");
            var infraredEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("DepthInfraredEncodedImageCameraView");
            var infraredImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("DepthInfraredImageCameraView");
            var ahatInfraredEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("AhatDepthInfraredEncodedImageCameraView");
            var ahatInfraredImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("AhatDepthInfraredImageCameraView");
            var ahatDepthImageCameraView = store.OpenStreamOrDefault<DepthImageCameraView>("AhatDepthImageCameraView");
            var ahatDepthCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("AhatDepthCalibrationMap");
            var leftFrontEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftFrontEncodedImageCameraView");
            var leftFrontGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftFrontGzipImageCameraView");
            var leftFrontImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("LeftFrontImageCameraView");
            var leftFrontCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("LeftFrontCalibrationMap");
            var rightFrontEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightFrontEncodedImageCameraView");
            var rightFrontGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightFrontGzipImageCameraView");
            var rightFrontImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("RightFrontImageCameraView");
            var rightFrontCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("RightFrontCalibrationMap");
            var leftLeftEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftLeftEncodedImageCameraView");
            var leftLeftGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftLeftGzipImageCameraView");
            var leftLeftImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("LeftLeftImageCameraView");
            var leftLeftCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("LeftLeftCalibrationMap");
            var rightRightEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightRightEncodedImageCameraView");
            var rightRightGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightRightGzipImageCameraView");
            var rightRightImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("RightRightImageCameraView");
            var rightRightCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("RightRightCalibrationMap");
            var sceneUnderstanding = store.OpenStreamOrDefault<SceneObjectCollection>("SceneUnderstanding");

            // Optional streams for annotations
            var stepDetection = exportCommand.StepDetection ? stepStore.OpenStreamOrDefault<TimeIntervalAnnotationSet>(exportCommand.StoreNameStepDetection) : null;
            var userASR = exportCommand.TextASR ? asrUserStore.OpenStreamOrDefault<TimeIntervalAnnotationSet>(exportCommand.StoreNameTextASRUser) : null;
            var instructorASR = exportCommand.TextASR ? asrInstructorStore.OpenStreamOrDefault<TimeIntervalAnnotationSet>(exportCommand.StoreNameTextASRInstructor) : null;

            // Verify expected stream combinations
            void VerifyMutualExclusivity(dynamic a, dynamic b, string name)
            {
                if (a != null && b != null)
                {
                    throw new Exception($"Found both encoded and unencoded {name} streams (expected one or the other).");
                }
            }

            VerifyMutualExclusivity(videoEncodedImageCameraView, videoImageCameraView, "video");
            VerifyMutualExclusivity(previewEncodedImageCameraView, previewImageCameraView, "preview");
            VerifyMutualExclusivity(infraredEncodedImageCameraView, infraredImageCameraView, "infrared");
            VerifyMutualExclusivity(ahatInfraredEncodedImageCameraView, ahatInfraredImageCameraView, "ahat-infrared");
            VerifyMutualExclusivity(leftFrontEncodedImageCameraView, leftFrontImageCameraView, "left-front");
            VerifyMutualExclusivity(rightFrontEncodedImageCameraView, rightFrontImageCameraView, "right-front");
            VerifyMutualExclusivity(leftLeftEncodedImageCameraView, leftLeftImageCameraView, "left-left");
            VerifyMutualExclusivity(rightRightEncodedImageCameraView, rightRightImageCameraView, "right-right");

            // Construct a list of stream writers to export data with (these will be closed once
            // the export pipeline is completed)
            var streamWritersToClose = new List<StreamWriter>();

            // Export various encoded image camera views
            var decodedVideo = Operators.ExportEncodedImageCameraViews("Video", exportCommand.OutputPath, streamWritersToClose, videoImageCameraView, videoEncodedImageCameraView, isNV12: true);
            Operators.ExportEncodedImageCameraViews("Preview", exportCommand.OutputPath, streamWritersToClose, previewImageCameraView, previewEncodedImageCameraView, isNV12: true);
            Operators.ExportEncodedImageCameraViews("Infrared", exportCommand.OutputPath, streamWritersToClose, infraredImageCameraView, infraredEncodedImageCameraView);
            Operators.ExportEncodedImageCameraViews("AhatInfrared", exportCommand.OutputPath, streamWritersToClose, ahatInfraredImageCameraView, ahatInfraredEncodedImageCameraView);
            Operators.ExportEncodedImageCameraViews("LeftFront", exportCommand.OutputPath, streamWritersToClose, leftFrontImageCameraView, leftFrontEncodedImageCameraView, leftFrontGzipImageCameraView);
            Operators.ExportEncodedImageCameraViews("RightFront", exportCommand.OutputPath, streamWritersToClose, rightFrontImageCameraView, rightFrontEncodedImageCameraView, rightFrontGzipImageCameraView);
            Operators.ExportEncodedImageCameraViews("LeftLeft", exportCommand.OutputPath, streamWritersToClose, leftLeftImageCameraView, leftLeftEncodedImageCameraView, leftLeftGzipImageCameraView);
            Operators.ExportEncodedImageCameraViews("RightRight", exportCommand.OutputPath, streamWritersToClose, rightRightImageCameraView, rightRightEncodedImageCameraView, rightRightGzipImageCameraView);

            // Export various depth image camera views
            depthImageCameraView?.Export("Depth", exportCommand.OutputPath, streamWritersToClose);
            ahatDepthImageCameraView?.Export("AhatDepth", exportCommand.OutputPath, streamWritersToClose);

            // Export various camera calibration maps
            depthCalibrationMap?.Export("Depth", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            ahatDepthCalibrationMap?.Export("AhatDepth", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            leftFrontCalibrationMap?.Export("LeftFront", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            rightFrontCalibrationMap?.Export("RightFront", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            leftLeftCalibrationMap?.Export("LeftLeft", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            rightRightCalibrationMap?.Export("RightRight", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);

            // Export IMU streams
            accelerometer?.SelectManyImuSamples().Export("IMU", "Accelerometer", exportCommand.OutputPath, streamWritersToClose);
            gyroscope?.SelectManyImuSamples().Export("IMU", "Gyroscope", exportCommand.OutputPath, streamWritersToClose);
            magnetometer?.SelectManyImuSamples().Export("IMU", "Magnetometer", exportCommand.OutputPath, streamWritersToClose);

            // Export head, eyes and hands streams
            head?.Export("Head", exportCommand.OutputPath, streamWritersToClose);

            if (store.Contains("Eyes"))
            {
                var simplifiedEyesTypeName = SimplifyTypeName(store.GetMetadata("Eyes").TypeName);
                if (simplifiedEyesTypeName == SimplifyTypeName(typeof(Ray3D).FullName))
                {
                    var eyes = store.OpenStreamOrDefault<Ray3D>("Eyes");
                    eyes?.Export("Eyes", exportCommand.OutputPath, streamWritersToClose);
                }
                else if (simplifiedEyesTypeName == SimplifyTypeName(typeof(WinRTEyes).FullName) ||
                    simplifiedEyesTypeName == "Microsoft.Psi.MixedReality.EyesRT")
                {
                    var eyes = store.OpenStreamOrDefault<WinRTEyes>("Eyes");
                    eyes?.Export("Eyes", exportCommand.OutputPath, streamWritersToClose);
                }
            }

            if (store.Contains("Hands"))
            {
                var simplifiedHandsTypeName = SimplifyTypeName(store.GetMetadata("Hands").TypeName);
                if (simplifiedHandsTypeName == SimplifyTypeName(typeof((StereoKitHand, StereoKitHand)).FullName) ||
                    simplifiedHandsTypeName == "System.ValueTuple`2[[Microsoft.Psi.MixedReality.Hand][Microsoft.Psi.MixedReality.Hand]]")
                {
                    var hands = store.OpenStream<(StereoKitHand Left, StereoKitHand Right)>("Hands");
                    hands.Select(x => x.Left).Export("Hands", "Left", exportCommand.OutputPath, streamWritersToClose);
                    hands.Select(x => x.Right).Export("Hands", "Right", exportCommand.OutputPath, streamWritersToClose);
                }
                else if (simplifiedHandsTypeName == SimplifyTypeName(typeof((OpenXRHand, OpenXRHand)).FullName) ||
                    simplifiedHandsTypeName == "System.ValueTuple`2[[Microsoft.Psi.MixedReality.HandXR][Microsoft.Psi.MixedReality.HandXR]]")
                {
                    var hands = store.OpenStream<(OpenXRHand Left, OpenXRHand Right)>("Hands");
                    hands.Select(x => x.Left).Export("Hands", "Left", exportCommand.OutputPath, streamWritersToClose);
                    hands.Select(x => x.Right).Export("Hands", "Right", exportCommand.OutputPath, streamWritersToClose);
                }
            }

            // Export audio
            audio?.Export("Audio", exportCommand.OutputPath, streamWritersToClose);

            // Export external microphone
            externalMicrophone?.Export("ExternalMicrophone", exportCommand.OutputPath, streamWritersToClose);

            // Export step detection
            if (exportCommand.StepDetection)
            {
                stepDetection?.Export("StepDetection", "StepDetection", exportCommand.OutputPath, streamWritersToClose);
            }

            if (exportCommand.TextASR)
            {
                userASR?.Export("TextASR", "UserAnnotations", exportCommand.OutputPath, streamWritersToClose);
                instructorASR?.Export("TextASR", "InstructorAnnotations", exportCommand.OutputPath, streamWritersToClose);
            }

            // Export scene understanding
            sceneUnderstanding?.Export("SceneUnderstanding", exportCommand.OutputPath, streamWritersToClose);

            // Export MPEG video
            ExportMPEGVideo(exportCommand.StoreName, exportCommand.StorePath, exportCommand.OutputPath, p, decodedVideo, audio, externalMicrophone, streamWritersToClose);

            Console.WriteLine($"Exporting {exportCommand.StoreName} to {exportCommand.OutputPath}");
            var startTime = DateTime.Now;
            p.RunAsync(ReplayDescriptor.ReplayAll, progress: new Progress<double>(p => Console.Write($"Progress: {p:P} Time elapsed: {DateTime.Now - startTime}\r")));
            p.WaitAll();

            foreach (var sw in streamWritersToClose)
            {
                sw.Close();
            }

            Console.WriteLine();
            Console.WriteLine($"Done in {DateTime.Now - startTime}.");

            return 0;
        }

        /// <summary>
        /// Exports only the audio data.
        /// </summary>
        /// <param name="exportCommand">The export command.</param>
        /// <returns>An error code or 0 if success.</returns>
        public static int RunAudioOnly(Verbs.ExportCommand exportCommand)
        {
            // Create a pipeline
            using var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.Throttle);

            // Open the psi store for reading
            var store = PsiStore.Open(p, exportCommand.StoreName, exportCommand.StorePath);

            // Get references to the various streams. If a stream is not present in the store,
            // the reference will be null.
            var audio = store.OpenStreamOrDefault<AudioBuffer>(AudioStreamName);
            var externalMicrophone = store.OpenStreamOrDefault<AudioBuffer>("ExternalMicrophone");

            // Construct a list of stream writers to export data with (these will be closed once
            // the export pipeline is completed)
            var streamWritersToClose = new List<StreamWriter>();

            // Export audio
            audio?.Export("Audio", exportCommand.OutputPath, streamWritersToClose);

            // Export external microphone
            externalMicrophone?.Export("ExternalMicrophone", exportCommand.OutputPath, streamWritersToClose);

            Console.WriteLine($"Exporting {exportCommand.StoreName} (audio only) to {exportCommand.OutputPath}");
            var startTime = DateTime.Now;
            p.RunAsync(ReplayDescriptor.ReplayAll, progress: new Progress<double>(p => Console.Write($"Progress: {p:P} Time elapsed: {DateTime.Now - startTime}\r")));
            p.WaitAll();

            foreach (var sw in streamWritersToClose)
            {
                sw.Close();
            }

            Console.WriteLine();
            Console.WriteLine($"Done in {DateTime.Now - startTime}.");

            return 0;
        }

        /// <summary>
        /// Exports only the video data.
        /// </summary>
        /// <param name="exportCommand">The export command.</param>
        /// <returns>An error code or 0 if success.</returns>
        public static int RunVideoOnly(Verbs.ExportCommand exportCommand)
        {
            // Create a pipeline
            using var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.Throttle);

            // Open the psi store for reading
            var store = PsiStore.Open(p, exportCommand.StoreName, exportCommand.StorePath);

            // Get references to the various streams. If a stream is not present in the store,
            // the reference will be null.
            var audio = store.OpenStreamOrDefault<AudioBuffer>(AudioStreamName);
            var externalMicrophone = store.OpenStreamOrDefault<AudioBuffer>("ExternalMicrophone");
            var videoImageCameraView = store.OpenStreamOrDefault<ImageCameraView>(VideoStreamName);
            var videoEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>(VideoEncodedStreamName);

            // Construct a list of stream writers to export data with (these will be closed once
            // the export pipeline is completed)
            var streamWritersToClose = new List<StreamWriter>();

            // Decode video
            var decodedVideo = Operators.ExportEncodedImageCameraViews("Video", exportCommand.OutputPath, streamWritersToClose, videoImageCameraView, videoEncodedImageCameraView, isNV12: true, exportPng: false);

            // Export MPEG video
            ExportMPEGVideo(exportCommand.StoreName, exportCommand.StorePath, exportCommand.OutputPath, p, decodedVideo, audio, externalMicrophone, streamWritersToClose);

            Console.WriteLine($"Exporting {exportCommand.StoreName} (video only) to {exportCommand.OutputPath}");
            var startTime = DateTime.Now;
            p.RunAsync(ReplayDescriptor.ReplayAll, progress: new Progress<double>(p => Console.Write($"Progress: {p:P} Time elapsed: {DateTime.Now - startTime}\r")));
            p.WaitAll();

            foreach (var sw in streamWritersToClose)
            {
                sw.Close();
            }

            Console.WriteLine();
            Console.WriteLine($"Done in {DateTime.Now - startTime}.");

            return 0;
        }

        /// <summary>
        /// Ensures that a specified path exists.
        /// </summary>
        /// <param name="path">The path to ensure the existence of.</param>
        /// <returns>The path.</returns>
        internal static string EnsurePathExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return path;
        }

        private static void ExportMPEGVideo(string storeName, string storePath, string outputPath, Pipeline p, IProducer<ImageCameraView> decodedVideo, IProducer<AudioBuffer> audio, IProducer<AudioBuffer> externalMicrophone, List<StreamWriter> streamWritersToClose)
        {
            (var videoMeta, var width, var height, var audioFormat, var externalMicrophoneAudioFormat) = GetAudioAndVideoInfo(storeName, storePath);

            if (videoMeta is not null)
            {
                // Configure and initialize the mpeg writer
                var frameRateNumerator = (uint)(videoMeta.MessageCount - 1);
                var frameRateDenominator = (uint)((videoMeta.LastMessageOriginatingTime - videoMeta.FirstMessageOriginatingTime).TotalSeconds + 0.5);
                var frameRate = frameRateNumerator / frameRateDenominator;
                var mpegFile = EnsurePathExists(Path.Combine(outputPath, "Video", $"Video.mpeg"));
                var audioOutputFormat = WaveFormat.Create16BitPcm((int)(audioFormat?.SamplesPerSec ?? 0), audioFormat?.Channels ?? 0);
                var mpegWriter = new Mpeg4Writer(p, mpegFile, new Mpeg4WriterConfiguration()
                {
                    ImageWidth = (uint)width,
                    ImageHeight = (uint)height,
                    FrameRateNumerator = frameRateNumerator,
                    FrameRateDenominator = frameRateDenominator,
                    PixelFormat = PixelFormat.BGRA_32bpp,
                    TargetBitrate = 10000000,
                    ContainsAudio = audioFormat != null,
                    AudioBitsPerSample = audioOutputFormat.BitsPerSample,
                    AudioChannels = audioOutputFormat.Channels,
                    AudioSamplesPerSecond = audioOutputFormat.SamplesPerSec,
                });

                // We will need to resample the video stream for the mpeg
                var mpegVideoInterval = TimeSpan.FromSeconds(1.0 / frameRate);
                IProducer<bool> mpegVideoTicks;
                IProducer<bool> mpegTicks;

                // Write "start" and "end" times of the mpeg to file
                var mpegTimingFile = File.CreateText(EnsurePathExists(Path.Combine(outputPath, "Video", "VideoMpegTiming.txt")));
                streamWritersToClose.Add(mpegTimingFile);

                // Audio
                if (audioFormat is not null && externalMicrophoneAudioFormat is not null)
                {
                    // First, resample and reframe both audio streams, and convert the samples to floats so we can mix and normalize.
                    // The reframe size of 1024 is arbitrary.
                    var resamplerConfig = new AudioResamplerConfiguration() { OutputFormat = audioOutputFormat, };
                    var reframeSize = 1024;
                    var audioSamples = audio.Resample(resamplerConfig).Reframe(reframeSize).ToByteArray().ToFloat(audioOutputFormat);
                    var externalMicrophoneSamples = externalMicrophone.Resample(resamplerConfig).Reframe(reframeSize).ToByteArray().ToFloat(audioOutputFormat);

                    // Second, we mix the two audio streams by joining them using the NearestOrDefault interpolator with a symmetric relative time tolerance of half the duration of an reframed audio buffer.
                    // Using the "Nearest" interpolator is extremely important, as without it, psi looks at the whole stream to look for the nearest audio buffer and incurs a huge latency.
                    // Using the "NearestOrDefault" is also extremely important b/c we need to ignore audio buffers from the external microphone that do not line up closely to the HL2 audio buffers.
                    var outputAudioBufferDuration = TimeSpan.FromTicks(10000000L * reframeSize / audioOutputFormat.AvgBytesPerSec);
                    var mixedAudio = audioSamples
                        .Join(externalMicrophoneSamples, Reproducible.NearestOrDefault<float[]>(new TimeSpan(outputAudioBufferDuration.Ticks / 2)))
                        .Select((tuple, e) =>
                        {
                            (var samples, var externalSamples) = tuple;
                            if (externalSamples == default)
                            {
                                // No nearest external microphone audio samples found, just return samples.
                                return samples;
                            }

                            // Quick sanity check
                            if (samples.Length != externalSamples.Length)
                            {
                                throw new ArgumentException($"samples.Length ({samples.Length}) != externalSamples.Length ({externalSamples.Length})");
                            }

                            var mixedSamples = new float[samples.Length];
                            for (int i = 0; i < samples.Length; i++)
                            {
                                mixedSamples[i] = samples[i] + externalSamples[i];
                            }

                            return mixedSamples;
                        });

                    // Finally, convert float samples back to audio buffers.
                    // NOTE: We skip 24-bits b/c the calculation is annoying.
                    Func<float, byte[]> convertFloatSampleToBytes = audioOutputFormat.BitsPerSample switch
                    {
                        8 => f => new byte[] { (byte)f },
                        16 => f => BitConverter.GetBytes((short)f),
                        32 => f => BitConverter.GetBytes((int)f),
                        _ => throw new FormatException("Valid sample sizes are 8, 16 or 32 bits"),
                    };
                    var mpegAudio = mixedAudio.Select(samples =>
                    {
                        return new AudioBuffer(samples.Select(convertFloatSampleToBytes).SelectMany(bytes => bytes).ToArray(), audioOutputFormat);
                    });
                    mpegAudio.PipeTo(mpegWriter.AudioIn);

                    // Compute frame ticks for the resampled video
                    mpegVideoTicks = mpegAudio
                        .Select((m, e) => e.OriginatingTime - m.Duration)
                        .Zip(decodedVideo.TimeOf())
                        .Process<DateTime[], bool>((m, e, emitter) =>
                        {
                            if (emitter.LastEnvelope.OriginatingTime == default)
                            {
                                // The mpeg "start time" will be the minimum time of the first video message and *start* of the first (resampled) audio buffer.
                                emitter.Post(true, m.Min());
                            }

                            while (emitter.LastEnvelope.OriginatingTime <= e.OriginatingTime)
                            {
                                emitter.Post(true, emitter.LastEnvelope.OriginatingTime + mpegVideoInterval);
                            }
                        });

                    // Zip with the resampled audio times
                    mpegTicks = mpegAudio.Select(_ => true).Zip(mpegVideoTicks).Select(a => a.First());
                }
                else
                {
                    // Compute frame ticks for the resampled video
                    mpegVideoTicks = decodedVideo.Process<ImageCameraView, bool>((_, e, emitter) =>
                    {
                        if (emitter.LastEnvelope.OriginatingTime == default)
                        {
                            emitter.Post(true, e.OriginatingTime);
                        }

                        while (emitter.LastEnvelope.OriginatingTime <= e.OriginatingTime)
                        {
                            emitter.Post(true, emitter.LastEnvelope.OriginatingTime + mpegVideoInterval);
                        }
                    });

                    // No audio, so the mpeg start/end times are just the time of the first/last video message.
                    mpegTicks = mpegVideoTicks;
                }

                // Video
                mpegVideoTicks
                    .Join(decodedVideo, Reproducible.Nearest<ImageCameraView>()).Select(tuple => tuple.Item2.ViewedObject)
                    .PipeTo(mpegWriter);

                // Write the mpeg start and end times time
                mpegTicks.First().Do((_, e) => mpegTimingFile.WriteLine($"{e.OriginatingTime.ToText()}"));
                mpegTicks.Last().Do((_, e) => mpegTimingFile.WriteLine($"{e.OriginatingTime.ToText()}"));
            }
        }

        private static (IStreamMetadata VideoMetadata, int Width, int Height, WaveFormat AudioFormat, WaveFormat ExternalMicrophoneAudioFormat) GetAudioAndVideoInfo(string storeName, string storePath)
        {
            // determine properties for the mpeg writer by peeking at the first video and audio messages
            using var p = Pipeline.Create();
            var store = PsiStore.Open(p, storeName, storePath);

            // Get the image width and height by looking at the first message of the video stream.
            var width = 0;
            var height = 0;
            var videoWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            void SetWidthAndHeight(IImage image)
            {
                if (image.PixelFormat != PixelFormat.BGRA_32bpp)
                {
                    throw new ArgumentException($"Expected video stream of {PixelFormat.BGRA_32bpp} (found {image.PixelFormat}).");
                }

                width = image.Width;
                height = image.Height;
                videoWaitHandle.Set();
            }

            bool TryGetMetadata(string stream, out IStreamMetadata meta)
            {
                if (store.Contains(stream))
                {
                    meta = store.GetMetadata(stream);
                    if (meta.MessageCount > 0)
                    {
                        return true;
                    }
                }

                meta = null;
                return false;
            }

            if (TryGetMetadata(VideoStreamName, out var videoMetadata))
            {
                store.OpenStream<ImageCameraView>(VideoStreamName).First().Do(v => SetWidthAndHeight(v.ViewedObject.Resource));
            }
            else if (TryGetMetadata(VideoEncodedStreamName, out videoMetadata))
            {
                store.OpenStream<EncodedImageCameraView>(VideoEncodedStreamName).First().Do(v => SetWidthAndHeight(v.ViewedObject.Resource));
            }
            else
            {
                videoWaitHandle.Set();
            }

            // Get the audio format by examining the first audio message (if one exists).
            WaveFormat audioFormat = null;
            var audioWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            if (TryGetMetadata(AudioStreamName, out var audioMeta))
            {
                store.OpenStream<AudioBuffer>(AudioStreamName).First().Do((a, env) =>
                {
                    audioFormat = a.Format;
                    audioWaitHandle.Set();
                });
            }
            else
            {
                audioWaitHandle.Set();
            }

            WaveFormat externalMicrophoneAudioFormat = null;
            var externalMicrophoneAudioWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            if (TryGetMetadata(AudioStreamName, out var externalMicrophoneAudioMeta))
            {
                store.OpenStream<AudioBuffer>("ExternalMicrophone").First().Do((a, env) =>
                {
                    externalMicrophoneAudioFormat = a.Format;
                    externalMicrophoneAudioWaitHandle.Set();
                });
            }
            else
            {
                externalMicrophoneAudioWaitHandle.Set();
            }

            // Run the pipeline, just until we've read the first video and audio message
            p.RunAsync();
            WaitHandle.WaitAll(new WaitHandle[3] { videoWaitHandle, audioWaitHandle, externalMicrophoneAudioWaitHandle });

            return (videoMetadata, width, height, audioFormat, externalMicrophoneAudioFormat);
        }

        /// <summary>
        /// Simplify the full type name into just the basic underlying type names,
        /// stripping away details like assembly, version, culture, token, etc.
        /// For example, for the type (Vector3D, DateTime)[]
        /// Input: "System.ValueTuple`2
        ///     [[MathNet.Spatial.Euclidean.Vector3D, MathNet.Spatial, Version=0.6.0.0, Culture=neutral, PublicKeyToken=000000000000],
        ///     [System.DateTime, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=000000000000]]
        ///     [], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=000000000000"
        /// Output: "System.ValueTuple`2[[MathNet.Spatial.Euclidean.Vector3D],[System.DateTime]][]".
        /// </summary>
        private static string SimplifyTypeName(string typeName)
        {
            static string SubstringToComma(string s)
            {
                var commaIndex = s.IndexOf(',');
                if (commaIndex >= 0)
                {
                    return s.Substring(0, commaIndex);
                }
                else
                {
                    return s;
                }
            }

            // Split first on open bracket, then on closed bracket
            var allSplits = new List<string[]>();
            foreach (var openSplit in typeName.Split('['))
            {
                allSplits.Add(openSplit.Split(']'));
            }

            // Re-assemble into a simplified string (without assembly, version, culture, token, etc).
            var assembledString = string.Empty;
            for (int i = 0; i < allSplits.Count; i++)
            {
                // Add back an open bracket (except the first time)
                if (i != 0)
                {
                    assembledString += "[";
                }

                for (int j = 0; j < allSplits[i].Length; j++)
                {
                    // Remove everything after the comma (assembly, version, culture, token, etc).
                    assembledString += SubstringToComma(allSplits[i][j]);

                    // Add back a closed bracket (except the last time)
                    if (j != allSplits[i].Length - 1)
                    {
                        assembledString += "]";
                    }
                }
            }

            return assembledString;
        }
    }
}
