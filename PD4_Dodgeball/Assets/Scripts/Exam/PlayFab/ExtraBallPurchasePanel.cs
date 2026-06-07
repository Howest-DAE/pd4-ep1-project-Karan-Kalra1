using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExtraBallPurchasePanel : MonoBehaviour
{
    [SerializeField]
    private GameObject _panelRoot;

    [SerializeField]
    private TextMeshProUGUI _messageText;

    [SerializeField]
    private TextMeshProUGUI _statusText;

    [SerializeField]
    private Button _confirmButton;

    [SerializeField]
    private Button _cancelButton;

    private int _currentSpawnIndex = -1;
    private bool _isBuying;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(OnConfirmClicked);
        _cancelButton.onClick.AddListener(Hide);

        Hide();
    }

    private void OnDestroy()
    {
        _confirmButton.onClick.RemoveListener(OnConfirmClicked);
        _cancelButton.onClick.RemoveListener(Hide);
    }

    public void Show(int spawnIndex)
    {
        _currentSpawnIndex = spawnIndex;

        Debug.Log($"Showing Extra Ball purchase panel for spawn index {spawnIndex}");

        gameObject.SetActive(true);

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
            _panelRoot.transform.SetAsLastSibling();
        }

        transform.SetAsLastSibling();

        RectTransform rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        if (_messageText != null)
        {
            _messageText.text = $"Buy Extra Ball for 5 Gold?";
        }

        SetStatus("");
        SetButtonsInteractable(true);
    }

    public void Hide()
    {
        Debug.Log("Hiding Extra Ball purchase panel");

        if (_isBuying)
        {
            return;
        }

        _currentSpawnIndex = -1;

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private async void OnConfirmClicked()
    {
        if (_isBuying)
        {
            return;
        }

        if (_currentSpawnIndex < 0)
        {
            SetStatus("No spawn point selected.");
            return;
        }

        if (PlayFabEconomyService.Instance == null)
        {
            SetStatus("PlayFab economy service is missing.");
            return;
        }

        if (ExtraBallSpawnSystem.Instance == null)
        {
            SetStatus("Extra ball spawn system is missing.");
            return;
        }

        _isBuying = true;
        SetButtonsInteractable(false);
        SetStatus("Buying Extra Ball...");

        try
        {
            await PlayFabEconomyService.Instance.PurchaseAndConsumeExtraBallAsync();

            SetStatus("Extra Ball purchased.");

            ExtraBallSpawnSystem.Instance.RequestSpawnExtraBall(_currentSpawnIndex);

            Hide();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            Debug.LogWarning($"Extra Ball purchase failed: {ex}");
        }
        finally
        {
            _isBuying = false;
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_confirmButton != null)
        {
            _confirmButton.interactable = interactable;
        }

        if (_cancelButton != null)
        {
            _cancelButton.interactable = interactable;
        }
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }
    }
}