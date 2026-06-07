using System;
using System.Linq;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabEconomyService : MonoBehaviour
{
    public static PlayFabEconomyService Instance { get; private set; }

    [SerializeField]
    private string _goldCurrencyCode = "GD";

    [SerializeField]
    private string _extraBallItemId = "extra_ball";

    [SerializeField]
    private int _extraBallPrice = 5;

    [SerializeField]
    private string _catalogVersion = "";

    public event Action<int> GoldChanged;

    public int CurrentGold { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public async Task<int> RefreshGoldAsync()
    {
        GetUserInventoryResult result = await GetInventoryAsync();

        if (result.VirtualCurrency != null &&
            result.VirtualCurrency.TryGetValue(_goldCurrencyCode, out int gold))
        {
            CurrentGold = gold;
        }
        else
        {
            CurrentGold = 0;
        }

        GoldChanged?.Invoke(CurrentGold);

        Debug.Log($"Gold refreshed: {CurrentGold}");

        return CurrentGold;
    }

    public async Task<int> AddGoldAsync(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive.");
        }

        TaskCompletionSource<ModifyUserVirtualCurrencyResult> tcs =
            new TaskCompletionSource<ModifyUserVirtualCurrencyResult>();

        AddUserVirtualCurrencyRequest request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = _goldCurrencyCode,
            Amount = amount
        };

        PlayFabClientAPI.AddUserVirtualCurrency(
            request,
            result => tcs.TrySetResult(result),
            error => tcs.TrySetException(new Exception(GetReadableError(error)))
        );

        ModifyUserVirtualCurrencyResult addResult = await tcs.Task;

        CurrentGold = addResult.Balance;
        GoldChanged?.Invoke(CurrentGold);

        Debug.Log($"Added {amount} Gold. New balance: {CurrentGold}");

        return CurrentGold;
    }

    public async Task PurchaseAndConsumeExtraBallAsync()
    {
        await PurchaseExtraBallAsync();
        await ConsumeExtraBallAsync();
        await RefreshGoldAsync();
    }

    private async Task PurchaseExtraBallAsync()
    {
        TaskCompletionSource<PurchaseItemResult> tcs =
            new TaskCompletionSource<PurchaseItemResult>();

        PurchaseItemRequest request = new PurchaseItemRequest
        {
            ItemId = _extraBallItemId,
            VirtualCurrency = _goldCurrencyCode,
            Price = _extraBallPrice
        };

        if (!string.IsNullOrWhiteSpace(_catalogVersion))
        {
            request.CatalogVersion = _catalogVersion;
        }

        PlayFabClientAPI.PurchaseItem(
            request,
            result => tcs.TrySetResult(result),
            error => tcs.TrySetException(new Exception(GetReadableError(error)))
        );

        await tcs.Task;

        Debug.Log("Purchased Extra Ball item.");
    }

    private async Task ConsumeExtraBallAsync()
    {
        GetUserInventoryResult inventory = await GetInventoryAsync();

        ItemInstance extraBall = inventory.Inventory
            .FirstOrDefault(i => i.ItemId == _extraBallItemId);

        if (extraBall == null)
        {
            throw new Exception("Extra Ball item was not found in inventory after purchase.");
        }

        TaskCompletionSource<ConsumeItemResult> tcs =
            new TaskCompletionSource<ConsumeItemResult>();

        ConsumeItemRequest request = new ConsumeItemRequest
        {
            ItemInstanceId = extraBall.ItemInstanceId,
            ConsumeCount = 1
        };

        PlayFabClientAPI.ConsumeItem(
            request,
            result => tcs.TrySetResult(result),
            error => tcs.TrySetException(new Exception(GetReadableError(error)))
        );

        await tcs.Task;

        Debug.Log("Consumed Extra Ball item.");
    }

    private Task<GetUserInventoryResult> GetInventoryAsync()
    {
        TaskCompletionSource<GetUserInventoryResult> tcs =
            new TaskCompletionSource<GetUserInventoryResult>();

        PlayFabClientAPI.GetUserInventory(
            new GetUserInventoryRequest(),
            result => tcs.TrySetResult(result),
            error => tcs.TrySetException(new Exception(GetReadableError(error)))
        );

        return tcs.Task;
    }

    private static string GetReadableError(PlayFabError error)
    {
        if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
        {
            return error.ErrorMessage;
        }

        return error.GenerateErrorReport();
    }
}