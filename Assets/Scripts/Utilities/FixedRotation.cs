using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Utilities { 
    public class FixedRotation : MonoBehaviour
    {
        // Plane facing front
        readonly Quaternion m_Rotation = Quaternion.Euler(-45,45,0); 
        void Awake()
        {
           // rotation = transform.rotation;
        }
        void LateUpdate()
        {
            transform.rotation = m_Rotation;
        }
    }
}
