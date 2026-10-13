using UnityEngine;
using UnityEngine.UIElements;

public class CountUIView : MonoBehaviour
{
	[SerializeField]
	private UIDocument _uiDocument;

	private Label _countLabel;
	private void OnEnable()
	{
		_countLabel = _uiDocument.rootVisualElement.Q<Label>();
	}

	public void SetCount(int count)
	{
		if (_countLabel != null)
		{
            _countLabel.text = count.ToString();
        }
		
	}
}
