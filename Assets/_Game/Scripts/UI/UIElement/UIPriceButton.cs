using Design.Structures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIPriceButton : MonoBehaviour
{
    [SerializeField] private Button _btn;
    [SerializeField] private Image _imgPriceIcon;
    [SerializeField] private TextMeshProUGUI _txtPriceValue;
    
    public Button Button => _btn;
    
    public void SetPrice(Price price)
    {
        // var resourceHolder = GlobalResourceHolder.Instance;
        // _imgPriceIcon.sprite = resourceHolder?.GetResourceSprite(price.Id, false);
        // _txtPriceValue.text = price.GetPriceAsString();
    }

    private void Awake()
    {
        if (_btn == null)
            _btn = GetComponent<Button>();
    }
}
