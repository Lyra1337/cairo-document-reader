using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using UglyToad.PdfPig;

namespace DocumentReader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<FileInfo> documents = [];

            foreach (var arg in args)
            {
                documents.AddRange(GetAllFiles(arg));
            }

            if (documents.Any() == false) // for debugging
            {
                documents.AddRange(GetAllFiles("C:\\temp\\document-input"));
            }

            DebugOutput($"Found {documents.Count} documents to process.");
            DebugOutput($"Documents: {string.Join(", ", documents.Select(x => x.FullName))}");
            DebugOutput($"starting reading...");

            List<DocumentInfo> content = ReadDocuments(documents).ToList();

            DebugOutput($"Read {content.Count} documents.");

            //content = await SplitDocumentsAsync(content);

            DebugOutput($"Split {content.Count} documents.");

            Console.WriteLine(JsonSerializer.Serialize(content));
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


        private static async Task<string> CallOllama(string prompt, string? systemPrompt = null, string model = "llama3.2:latest")
        {
            var uri = new Uri("http://localhost:11434");
            var ollama = new OllamaApiClient(uri);

            ollama.SelectedModel = model;

            var request = new GenerateRequest()
            {
                Model = model,
                Prompt = prompt,
                System = systemPrompt,
                Stream = false
            };

            var response = ollama.GenerateAsync(request);

            var responseText = new StringBuilder();

            await foreach (var result in response)
            {
                if (result?.Response is null)
                {
                    continue;
                }
                var content = result!.Response;
                responseText.Append(content);
                DebugOutput(content);
            }

            return responseText.ToString();
        }

        private static IEnumerable<DocumentInfo> ReadDocuments(List<FileInfo> documents)
        {
            foreach (var doc in documents)
            {
                var sb = new StringBuilder();
                using (var pdfDoc = PdfDocument.Open(doc.FullName))
                {
                    foreach (var page in pdfDoc.GetPages())
                    {
                        sb.AppendLine(page.Text);
                    }
                }

                yield return new DocumentInfo()
                {
                    source = doc.FullName,
                    //DocumentId = Guid.NewGuid().ToString(), // TODO: Hash content
                    documentid = HashContent(sb.ToString()),
                    content = sb.ToString()
                };
            }
        }

        private static string HashContent(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return String.Join("", hash.Select(x => x.ToString("x2")));
            }
        }

        private static IEnumerable<FileInfo> GetAllFiles(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists == true)
            {
                yield return fileInfo;
            }
            else
            {
                var dir = new DirectoryInfo(path);
                if (dir.Exists == true)
                {
                    foreach (var file in dir.GetFiles("*.pdf", new EnumerationOptions()
                    {
                        MatchCasing = MatchCasing.CaseInsensitive,
                        RecurseSubdirectories = true,
                    }))
                    {
                        yield return file;
                    }
                }
            }
        }

        private static void DebugOutput(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
