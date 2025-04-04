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
                prompt: doc.content,
                systemPrompt: "Anonymize the following text. Do not change the meaning of the text, but replace all names, addresses, zip codes, and other identifying information with random ones. Do not add any information. Only respond with with the anonymized Text, no other Output.",
                model: "llama3.2:latest"
            );

            doc.content = result;
        }

        return documents;
    }
}
