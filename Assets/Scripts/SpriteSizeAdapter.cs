using UnityEngine;

public class SpriteSizeAdapter : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;
        
        var width = spriteRenderer.sprite.bounds.size.x;
        var height = spriteRenderer.sprite.bounds.size.y;
        
        var worldScreenHeight = Camera.main.orthographicSize * 2f;
        var worldScreenWidth = worldScreenHeight * Screen.width / Screen.height;
        
        transform.localScale = new Vector3(
            worldScreenWidth / width,
            worldScreenHeight / height,
            1f
        ) / transform.parent.localScale.x;
    }
}
