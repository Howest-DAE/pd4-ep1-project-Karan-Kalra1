using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginScreenPresenter : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private PlayFabAuthService _authService;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _displayNameInput;

    [Header("Buttons")]
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _registerButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI _statusText;

    [Header("Panels")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _nextPanelAfterLogin;

    private void Awake()
    {
        _loginButton.onClick.AddListener(OnLoginClicked);
        _registerButton.onClick.AddListener(OnRegisterClicked);
    }

    private void OnDestroy()
    {
        _loginButton.onClick.RemoveListener(OnLoginClicked);
        _registerButton.onClick.RemoveListener(OnRegisterClicked);
    }

    private async void OnLoginClicked()
    {
        await RunAuthActionAsync(LoginAsync);
    }

    private async void OnRegisterClicked()
    {
        await RunAuthActionAsync(RegisterAsync);
    }

    private async Task RunAuthActionAsync(Func<Task<AuthSession>> authAction)
    {
        SetInteractable(false);
        SetStatus("Connecting to PlayFab...");

        try
        {
            AuthSession session = await authAction();

            SetStatus($"Logged in as {session.DisplayName}");

            Debug.Log($"Authenticated player: {session.DisplayName} ({session.PlayFabId})");

            if (_loginPanel != null)
            {
                _loginPanel.SetActive(false);
            }

            if (_nextPanelAfterLogin != null)
            {
                _nextPanelAfterLogin.SetActive(true);
            }

            // ToDo
            // await BackendApiClient.UpsertPlayerAsync(session.PlayFabId, session.DisplayName);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            Debug.LogWarning($"Authentication failed: {ex.Message}");
        }
        finally
        {
            SetInteractable(true);
        }
    }

    private Task<AuthSession> LoginAsync()
    {
        return _authService.LoginAsync(
            _emailInput.text.Trim(),
            _passwordInput.text
        );
    }

    private Task<AuthSession> RegisterAsync()
    {
        return _authService.RegisterAsync(
            _emailInput.text.Trim(),
            _passwordInput.text,
            _displayNameInput.text.Trim()
        );
    }

    private void SetInteractable(bool interactable)
    {
        _emailInput.interactable = interactable;
        _passwordInput.interactable = interactable;
        _displayNameInput.interactable = interactable;
        _loginButton.interactable = interactable;
        _registerButton.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }
    }
}