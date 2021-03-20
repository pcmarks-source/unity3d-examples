using UnityEngine;

public interface IDamageable {

    void TakeDamage(float damage, Vector3 location, Vector3 direction, EntityData source);

}
