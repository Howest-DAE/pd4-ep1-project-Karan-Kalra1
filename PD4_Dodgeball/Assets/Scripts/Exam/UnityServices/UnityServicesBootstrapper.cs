using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesBootstrapper : MonoBehaviour
{
    private bool _isInitializing;

    public async Task EnsureInitializedAndSignedInAsync()
    {
        while (_isInitializing)
        {
            await Task.Yield();
        }

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            _isInitializing = true;

            try
            {
                InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
               
                // A unique profile avoids both editor instances fighting over the same UGS auth session
                string profileName = $"profile_{Guid.NewGuid():N}".Substring(0, 20);
                options.SetProfile(profileName);
#endif

                await UnityServices.InitializeAsync(options);
                Debug.Log("Unity Services initialized.");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log(
                $"Unity Services signed in. PlayerId: {AuthenticationService.Instance.PlayerId}"
            );
        }
    }
}