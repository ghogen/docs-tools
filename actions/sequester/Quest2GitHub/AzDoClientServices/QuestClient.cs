﻿namespace Quest2GitHub.AzureDevOpsCommunications;

/// <summary>
/// The client services to work with Azure Dev ops.
/// </summary>
/// <remarks>
/// Azure DevOps tokens are scoped to the org / project so pass them in at construction time.
/// You'd need to have a second client with a different token if you were passing objects
/// between orgs.
/// </remarks>
public class QuestClient : IDisposable
{
    private readonly HttpClient _client;
    private readonly string _questOrg;

    public string QuestProject { get; }

    /// <summary>
    /// Create the quest client services object
    /// </summary>
    /// <param name="token">The personal access token</param>
    /// <param name="org">The Azure devops organization</param>
    /// <param name="project">The Azure devops project</param>
    public QuestClient(string token, string org, string project)
    {
        _questOrg = org;
        QuestProject = project;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json)
        );

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{token}")));
    }

    /// <summary>
    /// Create a work item from an array of JsonPatch documents.
    /// </summary>
    /// <param name="document">The Json patch document that represents
    /// the new item.</param>
    /// <returns>The JSON packet representing the new item.</returns>
    public async Task<JsonElement> CreateWorkItem(List<JsonPatchDocument> document)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var jsonString = JsonSerializer.Serialize(document, options);
        using var request = new StringContent(jsonString);
        request.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);

        string createWorkItemUrl = 
            $"https://dev.azure.com/{_questOrg}/{QuestProject}/_apis/wit/workitems/$User%20Story?api-version=6.0&expand=Fields";

        var response = await _client.PostAsync(createWorkItemUrl, request);
        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return jsonDocument.RootElement;
    }

    /// <summary>
    /// Retrieve a work item from ID
    /// </summary>
    /// <param name="id">The ID</param>
    /// <returns>The JSON element for the returned item.</returns>
    public async Task<JsonElement> GetWorkItem(int id)
    {
        string getWorkItemUrl = $"https://dev.azure.com/{_questOrg}/{QuestProject}/_apis/wit/workitems/{id}?api-version=6.0&expand=Fields";
        using var response = await _client.GetAsync(getWorkItemUrl);

        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return jsonDocument.RootElement;
    }

    /// <summary>
    /// Update a Quest work item.
    /// </summary>
    /// <param name="id">The work item ID</param>
    /// <param name="document">The Patch document that enumerates the updates.</param>
    /// <returns>The JSON element that represents the updated work item.</returns>
    public async Task<JsonElement> PatchWorkItem(int id, List<JsonPatchDocument> document)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        var jsonString = JsonSerializer.Serialize(document, options);
        using var request = new StringContent(jsonString);
        request.Headers.ContentType = new MediaTypeHeaderValue("application/json-patch+json");
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);

        string patchWorkItemUrl = $"https://dev.azure.com/{_questOrg}/{QuestProject}/_apis/wit/workitems/{id}?api-version=6.0&expand=Fields";

        var response = await _client.PatchAsync(patchWorkItemUrl, request);
        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return jsonDocument.RootElement;
    }

    /// <summary>
    /// Dispose of the embedded HTTP client.
    /// </summary>
    public void Dispose() => _client.Dispose();
}
