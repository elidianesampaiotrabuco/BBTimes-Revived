using TMPro;
using UnityEngine;

namespace BBTimes.CustomComponents.NpcSpecificComponents.Penny;

public class CorruptedLetter : MonoBehaviour
{
    public TextMeshPro text;
    public TextMeshProUGUI uiText;
    public Canvas canvasPre;
    public Collider collider;
    public string[] corruptedLetterSet = [":", "/", "\\", "^", "~", "]", "[", "ª", "º", "+", "-", ")", "(", "*", "&", "¨", "%", "$", "#", "@", "!", "?", ";", "<", ">", ".", ",", "§"];
    public float minSize = 1f, maxSize = 2.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && other.CompareTag("Player") && other.TryGetComponent<PlayerManager>(out var pm))
        {
            uiText.enabled = true;
            uiText.text = corruptedLetterSet[Random.Range(0, corruptedLetterSet.Length)];
            canvasPre.gameObject.SetActive(true);
            canvasPre.worldCamera = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).canvasCam;
            uiText.rectTransform.sizeDelta = Vector2.one * Random.Range(minSize, maxSize);
            uiText.rectTransform.rotation = Quaternion.Euler(Random.Range(0f, 360f), 0f, 0f);
            uiText.rectTransform.localPosition = new(Random.Range(-120f, 120f), Random.Range(-70f, 70f));

            collider.enabled = false;
            text.enabled = false;
        }
    }

    void Update()
    {
        text.text = corruptedLetterSet[Random.Range(0, corruptedLetterSet.Length)];
    }

}