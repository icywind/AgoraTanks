using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Utilities { 
    public class FixedRotation : MonoBehaviour
    {
        [SerializeField]
        Quaternion rotation;
        void Awake()
        {
            rotation = transform.rotation;
        }
        void LateUpdate()
        {
            transform.rotation = rotation;
        }
    }
}
