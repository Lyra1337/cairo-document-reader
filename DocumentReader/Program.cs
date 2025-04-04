using System.Text;
using System.Text.Json;

namespace DocumentReader;

internal class Program
{
    static async Task Main(string[] args)
    {
        var jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        var reader = new DocumentReader();
        var documents = reader.ReadDocuments(args);

        var splitter = new DocumentSplitter();
        documents = splitter.SplitDocuments(documents);
        
        await File.WriteAllTextAsync("pre-anonymized.json", JsonSerializer.Serialize(documents, jsonOptions), Encoding.UTF8);

        var anonymizer = new DocumentAnonymizer();
        documents = await anonymizer.AnonymizeDocuments(documents);

        Console.WriteLine(JsonSerializer.Serialize(documents, jsonOptions));
        await File.WriteAllTextAsync("anonymized.json", JsonSerializer.Serialize(documents, jsonOptions), Encoding.UTF8);

    }
}
