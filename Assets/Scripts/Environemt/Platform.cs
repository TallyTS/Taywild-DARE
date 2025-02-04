using UnityEngine;

public class Platform : MonoBehaviour
{
    [SerializeField] private bool isFloaitngPlatform = false;

    [Range(0, 1)]
    [SerializeField] private float bounceEffectStrength = 0.1f;

    [Range(0, 1)]
    [SerializeField] private float bounceEffectDuration = 1f;

    [Range(0, 2)]
    [SerializeField] private float shakeEffectStrength = 0.1f;

    [Range(0, 1)]
    [SerializeField] private float shakeEffectDuration = 0.2f;

    private ParticleSystem[] playerLandParticleEffects;
    private Vector2 defaultPosition;

    //Surface type for landed events in player effect controller.
    [SerializeField] private PlayerEffectController.SURFACE_TYPE surfaceType;
    public PlayerEffectController.SURFACE_TYPE getSurfaceType()
    {
        return surfaceType;
    }

    private void Start()
    {
        PlayerController.Instance.OnPlayerLand += OnPlayerLanded;

        playerLandParticleEffects = GetComponentsInChildren<ParticleSystem>();
        defaultPosition = transform.localPosition;
    }

    private void OnPlayerLanded(GameObject _platform)
    {
        if (gameObject == _platform)
        {
            LeanTween.cancel(gameObject);
            transform.localPosition = defaultPosition;

            foreach (ParticleSystem particleEffect in playerLandParticleEffects)
            {
                particleEffect.Play();
            }


            if (isFloaitngPlatform)
            {
                LeanTween.moveLocalY(gameObject, transform.localPosition.y - bounceEffectStrength, bounceEffectDuration).setEasePunch();
            }
            else
            {
                LeanTween.moveLocal(gameObject, (Vector2)transform.localPosition - new Vector2(shakeEffectStrength, shakeEffectStrength), shakeEffectDuration).setEaseShake();
            }
        }
    }
}