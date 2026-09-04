using Design.Structures;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRewardItem : MonoBehaviour
{
    [SerializeField] private Image _imgReward;
    [SerializeField] private TextMeshProUGUI _txtAmount;

    public void SetReward(Item item)
    {
        // _imgReward.sprite = GlobalResourceHolder.Instance.GetResourceSprite(item.Id);
        _txtAmount.text = item.Amount.ToResourceValueString();
    }
}
