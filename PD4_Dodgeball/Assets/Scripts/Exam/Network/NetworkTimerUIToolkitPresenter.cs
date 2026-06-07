using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class NetworkTimerUIToolkitPresenter : MonoBehaviour
{
    [SerializeField]
    private string _timerLabelName = "Timer";

    private UIDocument _uiDocument;
    private Label _timerLabel;
    private NetworkMatchTimer _timer;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        FindLabel();
        TrySubscribe();
        Refresh();
    }

    private void Update()
    {
        if (_timerLabel == null)
        {
            FindLabel();
        }

        if (_timer == null)
        {
            TrySubscribe();
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (_timer != null)
        {
            _timer.RemainingTimeChanged -= OnRemainingTimeChanged;
        }
    }

    private void FindLabel()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null)
        {
            return;
        }

        _timerLabel = _uiDocument.rootVisualElement.Q<Label>(_timerLabelName);

        if (_timerLabel == null)
        {
            Debug.LogWarning($"Could not find UI Toolkit Label named '{_timerLabelName}'.");
        }
    }

    private void TrySubscribe()
    {
        _timer = NetworkMatchTimer.Instance;

        if (_timer == null)
        {
            return;
        }

        _timer.RemainingTimeChanged -= OnRemainingTimeChanged;
        _timer.RemainingTimeChanged += OnRemainingTimeChanged;
    }

    private void OnRemainingTimeChanged(float remainingSeconds)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_timerLabel == null)
        {
            return;
        }

        if (_timer == null)
        {
            _timerLabel.text = "00:00";
            return;
        }

        int totalSeconds = Mathf.CeilToInt(_timer.RemainingSeconds.Value);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        _timerLabel.text = $"{minutes:00}:{seconds:00}";
    }
}