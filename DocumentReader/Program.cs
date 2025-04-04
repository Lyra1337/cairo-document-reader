using System.Text.Json;

namespace DocumentReader;

internal class Program
{
    static async Task Main(string[] args)
    {
        var reader = new DocumentReader();
        var documents = reader.ReadDocuments(args);

        var splitter = new DocumentSplitter();
        documents = splitter.SplitDocuments(documents);

        var anonymizer = new DocumentAnonymizer();
        documents = await anonymizer.AnonymizeDocuments(documents);

        Console.WriteLine(JsonSerializer.Serialize(documents));
    }
}
