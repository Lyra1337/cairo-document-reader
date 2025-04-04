using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace DocumentReader;

internal class DocumentSplitter
{
    public List<DocumentInfo> SplitDocuments(List<DocumentInfo> documents)
    {
        // TODO: do splitting

        return documents;
    }

    private static async Task<List<DocumentInfo>> SplitDocumentsAsync(List<DocumentInfo> documents)
    {
        List<DocumentInfo> result = [];

        foreach (var doc in documents)
        {
            result.AddRange(await SplitDocumentAsync(doc));
        }

        return result;
    }

    private static async Task<IEnumerable<DocumentInfo>> SplitDocumentAsync(DocumentInfo doc)
    {
        DebugOutput($"Splitting document {doc.source} using LLM...");
        var uri = new Uri("http://localhost:11434");
        var ollama = new OllamaApiClient(uri);

        ollama.SelectedModel = "llama3.2:latest";

        var request = new GenerateRequest()
        {
            Prompt = "Du bist ein Dokumentenleser. " +
                    "Du erhältst den Inhalt eines Dokuments. " +
                    "Deine Aufgabe ist es, das Dokument zu lesen und es in mehrere Dokumente zu unterteilen, " +
                    "wenn es so aussieht, als ob das Dokument eine Verkettung mehrerer unterschiedlicher Dokumente ist. " +
                    "Ändere NIEMALS den Inhalt, sondern gib nur die aufgeteilten Dokumente zurück." +
                    "Übersetze NIEMALS den Inhalt.",
            //Format = JsonConvert.SerializeObject(new List<SingleDocument>() { new(), new() })
            //Format = JsonSchema.ToJsonSchema()
            Format = @"{
  ""$schema"": ""http://json-schema.org/draft-04/schema#"",
  ""type"": ""array"",
  ""items"": [
    {
      ""type"": ""object"",
      ""properties"": {
        ""Content"": {
          ""type"": ""string""
        }
      },
      ""required"": [
        ""Content""
      ]
    },
    {
      ""type"": ""object"",
      ""properties"": {
        ""Content"": {
          ""type"": ""string""
        }
      },
      ""required"": [
        ""Content""
      ]
    }
  ]
}"
        };

        var responseText = new StringBuilder();

        var response = ollama.ChatAsync(new ChatRequest()
        {
            Messages = new List<Message>()
            {
                new Message()
                {
                    Role = ChatRole.System,
                    Content = request.Prompt
                },
                new Message()
                {
                    Role = ChatRole.User,
                    Content = doc.content
                }
            },
            Format = request.Format
        });

        await foreach (var result in response)
        {
            if (result?.Message is null)
            {
                continue;
            }
            var content = result!.Message.Content;
            responseText.Append(content);
            DebugOutput(content);
        }

        var splitDocuments = JsonSerializer.Deserialize<List<SingleDocument>>(responseText.ToString())!
            .Select(x => new DocumentInfo()
            {
                documentid = HashContent(x.Content),
                source = doc.source,
                content = x.Content
            })
            .ToList();

        DebugOutput($"Split document {doc.source} into {splitDocuments.Count} documents.");

        if (splitDocuments.Count == 1)
        {
            DebugOutput("No split detected, returning original document");
            return new List<DocumentInfo>() { doc };
        }

        return splitDocuments;
    }

    private static string HashContent(string content)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return String.Join("", hash.Select(x => x.ToString("x2")));
        }
    }

    private static void DebugOutput(string message)
    {
        Debug.WriteLine(message);
    }
}
