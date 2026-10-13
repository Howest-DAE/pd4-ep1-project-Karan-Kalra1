using UnityEngine;

public class PlayerCrownPresenter : MonoBehaviour
{
    [SerializeField]
    private NetworkPlayerAvatar _avatar;

    [SerializeField]
    private GameObject _crownRoot;

    [SerializeField]
    private CountUIView _countUIView;

    private CrownCountModel _model;

    private void Awake()
    {
        if (_avatar == null)
        {
            _avatar = GetComponentInParent<NetworkPlayerAvatar>();
        }

        _model = new CrownCountModel();
        _model.CountChanged += HandleModelCountChanged;
    }

    private void OnEnable()
    {
        if (_avatar == null)
        {
            return;
        }

        _avatar.CrownsRemaining.OnValueChanged += HandleNetworkCrownsChanged;

        _model.SetCount(_avatar.CrownsRemaining.Value);
        RefreshView();
    }

    private void OnDisable()
    {
        if (_avatar != null)
        {
            _avatar.CrownsRemaining.OnValueChanged -= HandleNetworkCrownsChanged;
        }
    }

    private void OnDestroy()
    {
        if (_model != null)
        {
            _model.CountChanged -= HandleModelCountChanged;
        }
    }

    private void HandleNetworkCrownsChanged(int oldValue, int newValue)
    {
        _model.SetCount(newValue);
    }

    private void HandleModelCountChanged(int count)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        if (_model == null)
        {
            return;
        }

        if (_crownRoot != null)
        {
            _crownRoot.SetActive(_model.HasCrowns);
        }

        if (_countUIView != null)
        {
            _countUIView.SetCount(_model.Count);
        }
    }
}