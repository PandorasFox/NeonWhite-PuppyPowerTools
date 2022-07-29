using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Puppy
{
    public class ColliderVisibility
    {
        // to do how to????
        // get from MelonLoader prefs enabled colliders
        public static MelonPreferences_Category colldier_config;
        public static MelonPreferences_Entry<bool> cardpickup_collider_enabled;
        public static MelonPreferences_Entry<bool> enemy_collider_enabled;
        public static MelonPreferences_Entry<bool> projectile_collider_enabled;
        public static MelonPreferences_Entry<bool> environment_collider_enabled;
        // how to add that to MelonLoader prefs?
        // how to add colliders by 'type'? 
        // i.e. CardPickup, enemy, ProjectileBase.ProjectileHit (explosion?), enviroment etc

        // set up lists of colliders
            // search Scene for colliders belonging to GameObject of type CardPickup/ etc?

        // CardPickup colliders are Capsules
        private static readonly List<UnityEngine.CapsuleCollider> CardPickupColliderList = 
            new System.Collections.Generic.List<UnityEngine.CapsuleCollider>();

        // Enemies are derived from BaseDamageable and have variable Collider types
        private static readonly List<UnityEngine.Collider> EnemyColliderList =
            new System.Collections.Generic.List<UnityEngine.Collider>();

        // Projectile (Dominion) collider are SphereColliders
        private static readonly List<UnityEngine.SphereCollider> ProjectileColliderList =
            new System.Collections.Generic.List<UnityEngine.SphereCollider>();

        // Environment Colliders???

        // get default shader?

        // set up display materials

        // profit???
    }
}