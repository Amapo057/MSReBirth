using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    public int damage = 1;

    private List<Collider> hitEnemy;
    // Start is called before the first frame update

    void OnEnable()
    {
        if (hitEnemy == null)
        {
            hitEnemy = new List<Collider>();
        }
        hitEnemy.Clear();
    }

}
