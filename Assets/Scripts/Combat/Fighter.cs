using UnityEngine;
using RPG.Movement;
using RPG.Core;
using RPG.Saving;
using RPG.Attributes;
using RPG.Stats;
using System.Collections.Generic;
using GameDevTV.Utils;
using System;

namespace RPG.Combat
{
   public class Fighter : MonoBehaviour, IAction, ISaveable, IModifierProvider
    {
        Mover mover;

        [SerializeField] float timeBetweenAttacks = 1f;
        [SerializeField] Transform rightHandTransform;
        [SerializeField] Transform leftHandTransform;
        [SerializeField] WeaponConfig  defaultWeapon;

        WeaponConfig currentWeaponConfig;
        LazyValue<Weapon>currentWeapon;
        Animator animator;
        float timeSinceLastAttack = Mathf.Infinity;
        Health target;

        private void Awake()
        {
            currentWeaponConfig = defaultWeapon;
            currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);
            animator = GetComponent<Animator>();
            mover = GetComponent<Mover>();
        }

        private Weapon  SetupDefaultWeapon()
        {
            return AttachWeapon(defaultWeapon);
        }

        private void Start()
        {
            currentWeapon.ForceInit();
        }

        private void Update()
        {           
            timeSinceLastAttack += Time.deltaTime;

            if (target == null) { return; }

            if (target.IsDead()){ return; }

            if (!GetIsInRange())
            {
                mover.MoveTo(target.transform.position, 1f);
            }
            else
            {
                mover.Cancel();
                AttackBehaviour();
            }
           
        }

        public Health GetTarget()
        {
            return target;
        }

        public void EquipWeapon(WeaponConfig  weapon)
        {
            currentWeaponConfig = weapon;
            currentWeapon.value = AttachWeapon(weapon);
        }

        private Weapon AttachWeapon(WeaponConfig  weapon)
        {
            return weapon.Spawn(rightHandTransform, leftHandTransform, animator);            
        }

        private void AttackBehaviour()
        {
            transform.LookAt(target.transform.position);
            if (timeSinceLastAttack > timeBetweenAttacks)
            {
                TriggerAttack();
                timeSinceLastAttack = 0;
            }
        }

        private void TriggerAttack()
        {
            animator.ResetTrigger("stopAttack");
            animator.SetTrigger("attack");
        }

        //Animation Event
        void Hit()
        {
            if (target == null){ return; }

            float damage = GetComponent<BaseStats>().GetStat(Stat.Damage);

            if (currentWeapon.value != null)
            {
                currentWeapon.value.OnHit();
            }

            if (currentWeaponConfig.HasProjectile())
            {
                currentWeaponConfig.LaunchProjectile(rightHandTransform, leftHandTransform, target,gameObject,damage);
            }
            else
            {
                target.TakeDamage(gameObject, damage);
            }
        }
        
        void Shoot()
        {
            Hit();
        }

        private bool GetIsInRange()
        {
            Debug.Log(gameObject.name + ("Trying to check if in range"));
            return Vector3.Distance(transform.position, target.transform.position) < currentWeaponConfig.WeaponRange();
        }

        public bool CanAttack(GameObject combatTarget)
        {
            if (combatTarget == null){ return false; }
            if (!GetComponent<Mover>().CanMoveTo(combatTarget.transform.position)) { return false; }

            Health targetToTest = combatTarget.GetComponent<Health>();
            return targetToTest != null && !targetToTest.IsDead();
        }

        public void Attack(GameObject combatTarget)
        {
            GetComponent<ActionScheduler>().StartAction(this);
            target = combatTarget.GetComponent<Health>();
        }

        public void Cancel()
        {
            StopAttack();
            target = null;
            GetComponent<Mover>().Cancel();
        }

        private void StopAttack()
        {
            animator.ResetTrigger("attack");
            animator.SetTrigger("stopAttack");
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (stat == Stat.Damage)
            {
                yield return currentWeaponConfig.WeaponDamage();
            }
        }

        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (stat == Stat.Damage)
            {
                yield return currentWeaponConfig.GetPercentageBonus();
            }
        }

        public object CaptureState()
        {
            return currentWeaponConfig.name;
        }

        public void RestoreState(object state)
        {
            string weaponName = (string)state;
            WeaponConfig  weapon = UnityEngine.Resources.Load<WeaponConfig >(weaponName);
            EquipWeapon(weapon);
        }       
    }
}