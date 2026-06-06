using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabCompileTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("PlayFab SDK is installed. Title ID: " + PlayFabSettings.staticSettings.TitleId);
    }
}
