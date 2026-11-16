using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image buttonImage;
    private TextMeshProUGUI buttonText;

    [Header("Colores")]
    public Color normalBackground = new Color(0, 0, 0, 0);
    public Color hoverBackground = Color.white;
    public Color normalText = Color.white;
    public Color hoverText = Color.black;

    [Header("Configuración de Fade")]
    public float fadeSpeed = 8f;

    [Header("Audio")]
    public AudioSource audioSource; // El reproductor de sonido
    public AudioClip sonidoHover;   // El archivo de audio (ej. .wav o .mp3)

    private bool isHovering = false;

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        buttonImage.color = normalBackground;
        buttonText.color = normalText;
    }

    void Update()
    {
        Color targetBg = isHovering ? hoverBackground : normalBackground;
        Color targetText = isHovering ? hoverText : normalText;

        buttonImage.color = Color.Lerp(buttonImage.color, targetBg, Time.deltaTime * fadeSpeed);
        buttonText.color = Color.Lerp(buttonText.color, targetText, Time.deltaTime * fadeSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        // Reproduce el sonido si ambos campos están asignados
        if (audioSource != null && sonidoHover != null)
        {
            audioSource.PlayOneShot(sonidoHover);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}