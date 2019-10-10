using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace Tanks.FX
{
	//This class handles the special effect that indicates an incoming powerup spawn.

	public class HotdropLight : NetworkBehaviour 
	{
		//The time before the pickup object to be is spawned. Referenced by the CrateSpawner on the server.
		[SerializeField]
		protected float m_DropTime = 5f;

		public float dropTime
		{
			get { return m_DropTime; }
		}

		//The pitch of the drop path.
		[SerializeField]
		[SyncVar]
		protected float m_DropAnglePitch = 0f;

		//The yaw of the drop path.
		[SerializeField]
		[SyncVar]
		protected float m_DropAngleYaw = 0f;

		//The ratio of the smoke emitter's position between its maximum height and the ground.
		//As our start position is variable, we must control its movement procedurally. Public so that its ratio value can be set by an AnimationClip in the attached animator.
		[SerializeField]
		protected float m_DropRatio = 0f;

		//The object that will be moved to suit the dropRatio.
		[SerializeField]
		protected GameObject m_DropObject;

		//The maximum height and internal start position from which the object will be dropped.
		[SerializeField]
		protected float m_DropHeight = 25f;
		private Vector3 m_DropStartPosition;

		//Internal reference to the effect's animator.
		private Animator m_MyAnimator;

		[ServerCallback]
		private void Awake () 
		{
			//On awake, the server scans around the drop area to determine a random pitch and yaw for the smoke trail effect that doesn't collide with anything.
			//(This prevents the effect path going through obstacles).
			bool hasPath = false;

			int mask = LayerMask.GetMask("Default","Powerups");

			Vector3 testRotation;
			float testPitch;
			float testYaw;

			while(!hasPath)
			{
				testPitch = Random.Range(-30f,30f);
				testYaw = Random.Range(0f, 360f);

				testRotation = Quaternion.Euler(testPitch,testYaw,0f) * Vector3.up;

				Ray hit = new Ray(transform.position, testRotation);

				if(!Physics.SphereCast(hit, 1f,350f,mask))
				{
					m_DropAnglePitch = testPitch;
					m_DropAngleYaw = testYaw;

					hasPath = true;
				}
			}

			//Now that we have a pitch and yaw, we can spawn this effect object across the clients initialized with these values.
			NetworkServer.Spawn(gameObject);
		}

		private void Start()
		{
			//Start the effect animation. 
			m_MyAnimator = GetComponent<Animator>();

			Quaternion dropAngle = Quaternion.Euler(m_DropAnglePitch, m_DropAngleYaw, 0f);

			m_DropStartPosition = transform.position + dropAngle * (Vector3.up * m_DropHeight);
		}
			
		private void Update () 
		{
			if(isServer)
			{
				//If this is the server, we check our animator each tick and NetworkDestroy this effect when it's done.
				if(m_MyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
				{
					NetworkServer.Destroy(gameObject);
				}
			}

			//Set the dropObject's position according to the dropRatio value assigned by the attached Animator.
			m_DropObject.transform.position = Vector3.Lerp(m_DropStartPosition,transform.position,m_DropRatio);

			//Rotate the object so that its facing matches wherever it came from.
			m_DropObject.transform.LookAt(m_DropStartPosition);
		}
	}
}
