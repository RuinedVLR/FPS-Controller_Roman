using TMPro;
using UnityEngine;

public class SpeedDebug : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedText;

    // Update is called once per frame
    void Update()
    {
        speedText.text = "Speed: " + FPS_Controller.horizontalSpeed.ToString("F2") + " m/s";
    }
}
