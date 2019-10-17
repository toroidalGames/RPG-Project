using RPG.Attributes;
using UnityEngine;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{

    [SerializeField] float projectileSpeed = 2f;
    [SerializeField] bool isHoming = true;
    [SerializeField] GameObject hitEffect = null;
    [SerializeField] float maxLifeTime = 10;
    [SerializeField] GameObject[] destroyOnHitArray = null;
    [SerializeField] float lifeAfterImpact = 2f;
    [SerializeField] UnityEvent onHit;

    Health target = null;
    float damage = 0;
    GameObject instigator = null;
    Vector3 targetStartingLocation;

    private void Start()
    {
        transform.LookAt(GetAimLocation());
    }

    void Update()
    {
        if (target == null){ return; }
        if (isHoming && !target.IsDead())
        {
            transform.LookAt(GetAimLocation());
        }      
        transform.Translate(Vector3.forward * projectileSpeed * Time.deltaTime);
    }

    public void SetTarget(Health target, GameObject instigator, float damage)
    {
        this.target = target;
        this.damage = damage;
        this.instigator = instigator;

        Destroy(gameObject, maxLifeTime);
    }

    private Vector3 GetAimLocation()
    {
        CapsuleCollider targetCapsule = target.GetComponent<CapsuleCollider>();
        if (targetCapsule == null)
        {
            return target.transform.position;
        }        
        return target.transform.position + Vector3.up * targetCapsule.height / 2;       
    }    

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponent<Health>();
        if (health != target) { return; }
        if (target.IsDead()) { return; }

        target.TakeDamage(instigator, damage);
        projectileSpeed = 0;

        onHit.Invoke();
        if (hitEffect)
        {
            Instantiate(hitEffect, transform.position,Quaternion.identity);
            Debug.Log("Creating hit effect");
        }

        foreach (GameObject toDestroy in destroyOnHitArray)
        {
            Destroy(toDestroy);
        }
        Destroy(gameObject, lifeAfterImpact);
    }
}
