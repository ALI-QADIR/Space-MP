using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float _speed = 10f;
    private float _lifeTime = 2f;

    private void Start()
    {
    }

    private void Update()
    {
        transform.Translate(_speed * Time.deltaTime * Vector3.forward);
        _lifeTime -= Time.deltaTime;

        if (_lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }
}