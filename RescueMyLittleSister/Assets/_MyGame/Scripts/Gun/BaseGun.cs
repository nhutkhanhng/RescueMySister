using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS
{
    public class BaseGun : MonoBehaviour
    {
        public string Name;
        [Tooltip("The object that controls the aiming direction")]
        public GameObject Aim;

        [Tooltip("a bullet config")]
        public GameObject Bullet;
    }
}