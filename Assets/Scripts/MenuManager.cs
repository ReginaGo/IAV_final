using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    public string nombreDeEscenaJuego = "MainScene";

    [Header("Configuración de Fade")]
    public Image panelFade;
    public float duracionFade = 1.2f; // Segundos que tarda en oscurecerse

    [Header("Paneles de Información")]
    public GameObject controlsPanel;
    public GameObject entitiesPanel;

    private bool cambiandoEscena = false;

    public void IniciarJuego()
    {
        // Evita que el jugador dé múltiples clics seguidos mientras ya está cargando
        if (!cambiandoEscena)
        {
            StartCoroutine(HacerFadeYCargar());
        }
    }

    public void MostrarControles()
    {
        controlsPanel.SetActive(true);
        entitiesPanel.SetActive(false);
    }

    // botón [Next]
    public void MostrarEntidades()
    {
        controlsPanel.SetActive(false);
        entitiesPanel.SetActive(true);
    }

    //  botón X 
    public void CerrarPaneles()
    {
        controlsPanel.SetActive(false);
        entitiesPanel.SetActive(false);
    }

    IEnumerator HacerFadeYCargar()
    {
        cambiandoEscena = true;

        if (panelFade != null)
        {
            // Bloquea clics en otros botones mientras se oscurece la pantalla
            panelFade.raycastTarget = true;

            float tiempo = 0f;
            Color colorActual = panelFade.color;
            Debug.Log("Iniciando fade");
            while (tiempo < duracionFade)
            {
                tiempo += Time.deltaTime;
                colorActual.a = Mathf.Lerp(0f, 1f, tiempo / duracionFade);
                panelFade.color = colorActual;
                yield return null;
            }

            // Asegura negro total antes del cambio
            colorActual.a = 1f;
            panelFade.color = colorActual;
        }

        // Carga la escena del juego
        SceneManager.LoadSceneAsync(nombreDeEscenaJuego);
    }
}