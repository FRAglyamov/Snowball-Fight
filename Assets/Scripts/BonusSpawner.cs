using UnityEngine;

public class BonusSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject bonusPrefab;
    [SerializeField]
    private float spawnTime = 10f;

    private float _nextSpawn;
    private GameObject _spawnedBonus;

    private void Update()
    {
        if(_spawnedBonus == null)
        {
            if(Time.time > _nextSpawn)
            {
                _spawnedBonus = Instantiate(bonusPrefab, transform.position, Quaternion.identity);
            }
        }
        else
        {
            _nextSpawn = Time.time + spawnTime;
        }
    }
}
