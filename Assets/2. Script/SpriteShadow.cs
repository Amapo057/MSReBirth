using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteShadow : MonoBehaviour
{
    private SpriteRenderer spriteRender;

    void Start()
    {
        spriteRender = gameObject.GetComponent<SpriteRenderer>();
        spriteRender.receiveShadows=true;
        spriteRender.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }


}
