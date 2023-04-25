using System;
using (var p = Pipeline.Create())
{
    var videoDest = "E:\\PTG\\Data\\Output\\Video and Audio";
    var stepDest = "E:\\PTG\\Data\\Output\\Steps";
    var asrDest = "E:\\PTG\\Data\\Output\\ASR";

    var baseDataDirectory = "E:\\PTG\\Data\\Trials";

    // Open the store
    var store = PsiStore.Open(p, "demo", "E:\\recordings");

    // Open the Sequence stream
    var sequence = store.OpenStream<double>("Sequence");

    // Compute derived streams
    var sin = sequence.Select(Math.Sin).Do(t => Console.WriteLine($"Sin: {t}"));
    var cos = sequence.Select(Math.Cos);

    // Run the pipeline
    p.Run();
}