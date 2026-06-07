using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class BackendApiClient : MonoBehaviour
{
    [SerializeField]
    private string _baseUrl = "https://localhost:7042";

    public static BackendApiClient Instance { get; private set; }

    public string BaseUrl => _baseUrl;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public Task<PlayerDto> UpsertPlayerAsync(string playFabId, string displayName)
    {
        PlayerCreateDto dto = new PlayerCreateDto
        {
            playFabId = playFabId,
            displayName = displayName
        };

        return PostAsync<PlayerCreateDto, PlayerDto>("/Players", dto);
    }

    public Task<PlayerDto> GetPlayerAsync(string playFabId)
    {
        return GetAsync<PlayerDto>($"/Players/{UnityWebRequest.EscapeURL(playFabId)}");
    }

    public Task<MatchCreatedDto> CreateMatchAsync(string hostPlayFabId, string clientPlayFabId)
    {
        CreateMatchDto dto = new CreateMatchDto
        {
            hostPlayFabId = hostPlayFabId,
            clientPlayFabId = clientPlayFabId
        };

        return PostAsync<CreateMatchDto, MatchCreatedDto>("/Matches", dto);
    }

    public Task ReportPlayerHitAsync(int matchId, string attackerPlayFabId, string victimPlayFabId)
    {
        PlayerHitCreateDto dto = new PlayerHitCreateDto
        {
            attackerPlayFabId = attackerPlayFabId,
            victimPlayFabId = victimPlayFabId
        };

        return PostAsync<PlayerHitCreateDto>($"/Matches/{matchId}/PlayerHit", dto);
    }

    public Task<MatchSummaryDto> GetMatchAsync(int matchId)
    {
        return GetAsync<MatchSummaryDto>($"/Matches/{matchId}");
    }

    private async Task<TResponse> GetAsync<TResponse>(string route)
    {
        string url = BuildUrl(route);

        using UnityWebRequest request = UnityWebRequest.Get(url);

        await SendAsync(request);

        string json = request.downloadHandler.text;
        return JsonConvert.DeserializeObject<TResponse>(json);
    }

    private async Task PostAsync<TRequest>(string route, TRequest body)
    {
        string url = BuildUrl(route);
        string json = JsonConvert.SerializeObject(body);

        using UnityWebRequest request = CreateJsonPostRequest(url, json);

        await SendAsync(request);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string route, TRequest body)
    {
        string url = BuildUrl(route);
        string json = JsonConvert.SerializeObject(body);

        using UnityWebRequest request = CreateJsonPostRequest(url, json);

        await SendAsync(request);

        string responseJson = request.downloadHandler.text;
        return JsonConvert.DeserializeObject<TResponse>(responseJson);
    }

    private UnityWebRequest CreateJsonPostRequest(string url, string json)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        return request;
    }

    private async Task SendAsync(UnityWebRequest request)
    {
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler != null
                ? request.downloadHandler.text
                : string.Empty;

            throw new Exception(
                $"Backend request failed: {request.method} {request.url}\n" +
                $"Status: {request.responseCode}\n" +
                $"Error: {request.error}\n" +
                $"Response: {responseText}"
            );
        }
    }

    private string BuildUrl(string route)
    {
        return $"{_baseUrl.TrimEnd('/')}/{route.TrimStart('/')}";
    }
}