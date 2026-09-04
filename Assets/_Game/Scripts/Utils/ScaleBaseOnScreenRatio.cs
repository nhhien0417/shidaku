using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleBaseOnScreenRatio : MonoBehaviour
{
    public Vector2 baseScreenRatio;

    private void Start()
    {
        float width = ScreenSize.GetScreenToWorldWidth;
        float height = ScreenSize.GetScreenToWorldHeight;

        float scaleRatio = (width * baseScreenRatio.y) / (height * baseScreenRatio.x);

        transform.localScale = new Vector3(scaleRatio, scaleRatio, 1);
    }
}
