using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [SerializeField] private float inactiveBounceFactor = 20f;
    [SerializeField] private float activeBounceFactor = 20f;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;

    [SerializeField] private AudioClip[] bounceSounds;

    [SerializeField] private bool isTreeLevelRequired;

    private void Update()
    {
        Debug.DrawLine(transform.position, transform.position + -transform.up, Color.red);
    }

    private void Start()
    {
        if (isTreeLevelRequired)
        {
            TreeLevelController.Instance.OnTreeLevelUp += treeLevelUpUpdate;
            spriteRenderer.sprite = inactiveSprite;

        }
        else
        {
            spriteRenderer.sprite = activeSprite;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //Apply a big boost upwards.
            PlayerController.Instance.MushroomBounce();
            float _bounceFactor = inactiveBounceFactor;
            if (TreeLevelController.Instance.CurrentTreeLevel >= 1) _bounceFactor = activeBounceFactor;
            //collision.gameObject.GetComponent<Rigidbody2D>().AddForce(-transform.up * _bounceFactor, ForceMode2D.Impulse);
            //Vector2 _transform = new Vector2(-transform.up.x * 20, -transform.up.y);
            collision.gameObject.GetComponent<Rigidbody2D>().velocity = -transform.up * _bounceFactor;
            //Add sound
            AudioClip clip = bounceSounds[Random.Range(0, bounceSounds.Length - 1)];
            AudioController.Instance.PlaySound(clip, false);

        }
    }

    //Called when tree level is updated
    private void treeLevelUpUpdate()
    {
        //Swap sprite
        if (TreeLevelController.Instance.CurrentTreeLevel == 1) spriteRenderer.sprite = activeSprite;
    }
}
