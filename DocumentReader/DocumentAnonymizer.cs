namespace DocumentReader;

internal class DocumentAnonymizer
{
    public DocumentAnonymizer()
    {
    }

    internal async Task<List<DocumentInfo>> AnonymizeDocuments(List<DocumentInfo> documents)
    {
        foreach (var doc in documents)
        {
            var result = await OllamaService.Prompt(
                prompt: "",
                systemPrompt: "Anonymize the following text. Do not change the meaning of the text, but replace all names, addresses, and other identifying information with random ones.",
                model: "llama3.2:latest"
            );

            doc.content = result;
        }

        return documents;
    }
}
