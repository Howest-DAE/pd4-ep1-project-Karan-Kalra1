using Dodgeball.Presenter;
using UnityEngine;

public static class NetworkGameOverBridge
{
    public static void TriggerGameOver(GamePresenter gamePresenter)
    {
        if (gamePresenter == null)
        {
            Debug.LogError("NetworkGameOverBridge: GamePresenter is null.");
            return;
        }

        if (gamePresenter.Model == null)
        {
            Debug.LogError("NetworkGameOverBridge: GamePresenter.Model is null.");
            return;
        }

        gamePresenter.Model.NotifyNetworkGameOver();
    }
}