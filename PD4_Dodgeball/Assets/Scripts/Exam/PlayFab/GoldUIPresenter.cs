using TMPro;
using UnityEngine;

public class GoldUIPresenter : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _goldText;

    private void OnEnable()
    {
        if (PlayFabEconomyService.Instance != null)
        {
            PlayFabEconomyService.Instance.GoldChanged += OnGoldChanged;
            OnGoldChanged(PlayFabEconomyService.Instance.CurrentGold);
        }
    }

    private void OnDisable()
    {
        if (PlayFabEconomyService.Instance != null)
        {
            PlayFabEconomyService.Instance.GoldChanged -= OnGoldChanged;
        }
    }

    private async void Start()
    {
        if (PlayFabEconomyService.Instance != null)
        {
            await PlayFabEconomyService.Instance.RefreshGoldAsync();
        }
    }

    private void OnGoldChanged(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = $"Gold: {gold}";
        }
    }
}