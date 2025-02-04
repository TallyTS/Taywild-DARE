using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [SerializeField] private Vector2 parallaxEffectMultiplier = new Vector2(1.0f, 0.5f);
    [SerializeField] private bool infiniteScrolling = true;

    private Transform playerCameraTransform = null;
    private Vector3 lastCameraPosition = Vector2.one;
    [SerializeField] private float textureUnitSizeX = 19.2f * 4;

    private void Start()
    {
        playerCameraTransform = CameraController.Instance.transform;
        lastCameraPosition = playerCameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 deltaMovement = (playerCameraTransform.position - lastCameraPosition) * parallaxEffectMultiplier;
        transform.position += deltaMovement;
        lastCameraPosition = playerCameraTransform.position;

        if (infiniteScrolling)
        {
            if (Mathf.Abs(playerCameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
            {
                float offsetPositionX = (playerCameraTransform.position.x - transform.position.x) % textureUnitSizeX;

                if (playerCameraTransform.position.x - transform.position.x < 0)
                {
                    transform.position -= new Vector3(textureUnitSizeX + offsetPositionX, 0f, 0f);
                }
                else
                {
                    transform.position += new Vector3(textureUnitSizeX + offsetPositionX, 0f, 0f);
                }
            }
        }
    }
}