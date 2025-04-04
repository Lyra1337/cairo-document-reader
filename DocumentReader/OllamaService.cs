using System.Text;
using OllamaSharp;
using OllamaSharp.Models;

namespace DocumentReader;

internal class OllamaService
{
    public static async Task<string> Prompt(string prompt, string? systemPrompt = null, string model = "llama3.2:latest")
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
        }

        return responseText.ToString();
    }
}
