using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using UglyToad.PdfPig;

namespace DocumentReader;

internal class DocumentReader
{
    public List<DocumentInfo> ReadDocuments(params string[] locations)
    {
        List<FileInfo> documents = [];

        foreach (var arg in locations)
        {
            documents.AddRange(GetAllFiles(arg));
        }

        if (documents.Any() == false) // for debugging
        {
            documents.AddRange(GetAllFiles("C:\\temp\\document-input-small"));
        }

        DebugOutput($"Found {documents.Count} documents to process.");
        DebugOutput($"Documents: {string.Join(", ", documents.Select(x => x.FullName))}");
        DebugOutput($"starting reading...");

        List<DocumentInfo> content = ReadDocuments(documents).ToList();

        DebugOutput($"Read {content.Count} documents.");

        DebugOutput($"Split {content.Count} documents.");

        return content;
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
