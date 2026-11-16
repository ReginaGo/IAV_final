using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LogoGlitchReveal : MonoBehaviour
{
    private Image logoImage;

    [Header("Configuración de Color y Glitch")]
    public float tiempoDeAparicion = 4f; // Segundos que tarda en llegar a color completo
    public float opacidadGlitchMin = 0.5f; // Qué tan transparente se vuelve el glitch
    public float opacidadGlitchMax = 0.8f; // Qué tan sólido se vuelve el glitch

    private bool introTerminada = false;

    void Start()
    {
        logoImage = GetComponent<Image>();

        // Forzar el estado inicial: Transparente
        Color colorInicial = logoImage.color;
        colorInicial.a = 0f;
        logoImage.color = colorInicial;

        // Iniciar la secuencia de aparición
        StartCoroutine(SecuenciaAparicion());
    }

    void Update()
    {
        // Efecto Glitch: Se activa solo cuando termina de aparecer por completo
        if (introTerminada)
        {
            // 15% de probabilidad por frame de causar un parpadeo (glitch errático)
            if (Random.Range(0f, 100f) < 15f) 
            {
                Color colorGlitch = logoImage.color;
                // Asigna una opacidad aleatoria
                colorGlitch.a = Random.Range(opacidadGlitchMin, opacidadGlitchMax);
                logoImage.color = colorGlitch;
            }
        }
    }

    IEnumerator SecuenciaAparicion()
    {
        float tiempoTranscurrido = 0f;
        Color c = logoImage.color;

        // Fase 1: Aparecer lentamente de 0 a 1 (Fade In)
        while (tiempoTranscurrido < tiempoDeAparicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, tiempoTranscurrido / tiempoDeAparicion);
            logoImage.color = c;
            yield return null; // Espera al siguiente frame
        }

        // Asegurarse de que el color llegue exactamente al 100%
        c.a = 1f;
        logoImage.color = c;

        // Fase 2: Quedarse en color completo por 1 segundo de suspenso
        yield return new WaitForSeconds(1f);

        // Fase 3: Activar el modo glitch
        introTerminada = true;
    }
}