using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRewardSlot : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amount;

    public Image Icon => _icon;
    public TextMeshProUGUI Amount => _amount;
}
