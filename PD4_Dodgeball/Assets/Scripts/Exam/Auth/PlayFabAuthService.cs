using System;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabAuthService : MonoBehaviour
{
    public async Task<AuthSession> LoginAsync(string email, string password)
    {
        ValidateLoginInput(email, password);

        LoginResult loginResult = await LoginWithEmailAddressAsync(email, password);
        string displayName = await GetCurrentDisplayNameAsync();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = email;
        }

        SessionData.SetLogin(loginResult.PlayFabId, displayName, email);

        return new AuthSession(loginResult.PlayFabId, displayName, email);
    }

    public async Task<AuthSession> RegisterAsync(string email, string password, string displayName)
    {
        ValidateRegisterInput(email, password, displayName);

        RegisterPlayFabUserResult registerResult =
            await RegisterPlayFabUserAsync(email, password, displayName);

        string finalDisplayName = displayName;

        if (string.IsNullOrWhiteSpace(finalDisplayName))
        {
            finalDisplayName = await GetCurrentDisplayNameAsync();
        }

        SessionData.SetLogin(registerResult.PlayFabId, finalDisplayName, email);

        return new AuthSession(registerResult.PlayFabId, finalDisplayName, email);
    }

    private Task<LoginResult> LoginWithEmailAddressAsync(string email, string password)
    {
        TaskCompletionSource<LoginResult> tcs = new TaskCompletionSource<LoginResult>();

        LoginWithEmailAddressRequest request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password
        };

        PlayFabClientAPI.LoginWithEmailAddress(
            request,
            result => tcs.TrySetResult(result),
            error => tcs.TrySetException(new Exception(GetReadableError(error)))
        );

        return tcs.Task;
    }

    private Task<RegisterPlayFabUserResult> RegisterPlayFabUserAsync(
        string email,
        string password,
        string displayName)
    {
        TaskCompletionSource<RegisterPlayFabUserResult> tcs =
            new TaskCompletionSource<RegisterPlayFabUserResult>();

        RegisterPlayFabUserRequest request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            DisplayName = displayName,

            // We are registering with email + password.
            // We do not want to force a separate username field.
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            result => tcs.TrySetResult(result),
            error => tcs.TrySetException(new Exception(GetReadableError(error)))
        );

        return tcs.Task;
    }

    private Task<string> GetCurrentDisplayNameAsync()
    {
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

        PlayFabClientAPI.GetAccountInfo(
            new GetAccountInfoRequest(),
            result =>
            {
                string displayName = result.AccountInfo?.TitleInfo?.DisplayName;
                tcs.TrySetResult(displayName);
            },
            error => tcs.TrySetException(new Exception(GetReadableError(error)))
        );

        return tcs.Task;
    }

    private static void ValidateLoginInput(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.");
        }
    }

    private static void ValidateRegisterInput(string email, string password, string displayName)
    {
        ValidateLoginInput(email, password);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.");
        }

        if (password.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters.");
        }
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