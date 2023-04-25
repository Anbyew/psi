
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data.Annotations;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Spatial.Euclidean;

using (var p = Pipeline.Create())
{
    Console.WriteLine("Separating data!");
    var videoDest = "E:\\PTG\\Data\\Output\\Video and Audio";
    var stepDest = "E:\\PTG\\Data\\Output\\Steps";
    var asrDest = "E:\\PTG\\Data\\Output\\ASR";

    var baseDataDirectory = "E:\\PTG\\Data\\Trials";

    var thisRecording = "T8_U-Peter_I-Emily_Coffee";

    // Open the store
    var inputHoloLensStore = PsiStore.Open(p, "HoloLensCapture", baseDataDirectory + "\\" + thisRecording);
    var inputAnnotationStepStore = PsiStore.Open(p, "anno_step", baseDataDirectory + "\\" + thisRecording);
    var inputAnnotationExtStore = PsiStore.Open(p, "anno_ext", baseDataDirectory + "\\" + thisRecording);
    var inputAnnotationHLStore = PsiStore.Open(p, "anno_hl", baseDataDirectory + "\\" + thisRecording);
    var inputAnnotationASRStore = PsiStore.Open(p, "ASR", baseDataDirectory + "\\" + thisRecording);
    var outputStoreVideo = PsiStore.Create(p, thisRecording + "_Video+Audio", videoDest);
    var outputStoreStep = PsiStore.Create(p, thisRecording + "_Step", stepDest);
    var outputStoreASR = PsiStore.Create(p, thisRecording + "_ASR", asrDest);

    // Get the streams for each output store

    // Video
    inputHoloLensStore.OpenStream<AudioBuffer>("Audio").Write("UserAudio", outputStoreVideo);
    inputHoloLensStore.OpenStream<AudioBuffer>("ExternalMicrophone").Write("InstructorAudio", outputStoreVideo);
    inputHoloLensStore.OpenStream<EncodedImageCameraView>("VideoEncodedImageCameraView").Select((img, e) => img.ViewedObject).Write("UserVideo", outputStoreVideo);

    //inputHoloLensStore.OpenStream<EncodedImageCameraView>("VideoEncodedImageCameraView").Select((img, e) => img.ViewedObject).Write("UserVideo", outputStoreStep);
    //inputHoloLensStore.OpenStream<EncodedImageCameraView>("VideoEncodedImageCameraView").Select((img, e) => img.ViewedObject).Write("UserVideo", outputStoreASR);

    // Step
    inputAnnotationStepStore.OpenStream<TimeIntervalAnnotationSet>("anno_step")
        //.Do((a, e) =>
        //{
        //    Console.WriteLine(a.ToString());
        //    foreach(var t in a.Tracks)
        //    {
        //        Console.WriteLine(t.ToString()+","+a[t].Track + "," +a[t].Interval.Left.ToString() + "," +a[t].Interval.Right.ToString() + "," + a[t].AttributeValues + "," + a[t].ToString());
        //    }
        //});
        .Write("AnnotatedSteps", outputStoreStep);

    // ASR
    inputAnnotationExtStore.OpenStream<TimeIntervalAnnotationSet>("anno_ext").Write("InstructorASR", outputStoreASR);
    inputAnnotationHLStore.OpenStream<TimeIntervalAnnotationSet>("anno_hl").Write("UserASR", outputStoreASR);

    // Run the pipeline
    Console.WriteLine("Starting pipeline...");
    p.Run();
    Console.WriteLine("Done!");
}