using UnityEngine;
using TMPro;

public class MOTDUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI motdText;
    [SerializeField] private float displayTime = 6f;

    private void OnEnable()
    {
        ClientEvents.OnMOTD += ShowMOTD;
    }

    private void OnDisable()
    {
        ClientEvents.OnMOTD -= ShowMOTD;
    }

    private void ShowMOTD(string message)
    {
        motdText.text = message;
        motdText.gameObject.SetActive(true);

        CancelInvoke();
        Invoke(nameof(Hide), displayTime);
    }

    private void Hide()
    {
        motdText.gameObject.SetActive(false);
    }
}
