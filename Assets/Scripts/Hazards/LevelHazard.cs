using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace Tanks.Hazards
{
	// A base class to handle registration and define reset for all environmental hazards that are to be reset each game round.
	// This is intended only to be used on the server-authoritative version of the object.
	public class LevelHazard : NetworkBehaviour 
	{
		[ServerCallback]
		protected virtual void Start () 
		{
			GameManager.s_Instance.AddHazard(this);
		}

		[ServerCallback]
		protected virtual void OnDestroy()
		{
			GameManager.s_Instance.RemoveHazard(this);
		}
			
		// Reset code that is called on the hazard at the beginning of a round.
		[ServerCallback]
		public virtual void ResetHazard(){}


		// Secondary activation method for when the game manager cedes control to players. Useful for hazards that may trigger erroneously during reset logic.
		[ServerCallback]
		public virtual void ActivateHazard(){}
	}
}
