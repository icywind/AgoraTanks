using System;
using UnityEngine;
using UnityEngine.Networking;
using Tanks.Map;
using Tanks.Explosions;
using Tanks.Data;

namespace Tanks.TankControllers
{
	//This class is responsible for handling tank health and damage
	public class TankHealth : NetworkBehaviour, IDamageObject
	{
		//The starting health of the tank. Populated by the TankManager based on stats pulled from the TankLibrary.
		[SyncVar]
		private float m_StartingHealth = 100f;

		//The parameters for the explosion to be spawned on tank death.
		[SerializeField]
		protected ExplosionSettings m_DeathExplosion;

		//Reference to the canvas containing the shot power arrow, to disable on death/respawn.
		[SerializeField]
		protected GameObject m_AimCanvas;

		//Associated manager, to disable control when dying.
		private TankManager m_Manager;
		private TankDisplay m_TankDisplay;

		//Implementation for IDamageObject
		public bool isAlive { get { return m_CurrentHealth > 0; } }

		//Field to set the tank as invulnerable. Mainly used in the shooting range.
		public bool invulnerable
		{
			get;
			set;
		}

		[SyncVar(hook = "OnCurrentHealthChanged")]
		// How much health the tank currently has.*
		private float m_CurrentHealth;

		[SyncVar(hook = "OnShieldLevelChanged")]
		//The current shield level of the tank.
		private float m_ShieldLevel;
		[SyncVar]

		// Has the tank been reduced beyond zero health yet?
		private bool m_ZeroHealthHappened;

		// Used so that the tank doesn't collide with anything when it's dead.
		private BoxCollider m_Collider;

		//Events that fire when specific conditions are reached. Mainly used for the HUD to tie into.
		public event Action<float> healthChanged;
		public event Action<float> shieldChanged;
		public event Action playerDeath;
		public event Action playerReset;

		//This constant defines the player index used to represent player suicide in the damage parsing system.
		public const int TANK_SUICIDE_INDEX = -1;

		//Internal reference to the spawn point where this tank is.
		private SpawnPoint m_CurrentSpawnPoint;
		public SpawnPoint currentSpawnPoint
		{
			get { return m_CurrentSpawnPoint; }
			set { m_CurrentSpawnPoint = value; }
		}
			
		public void NullifySpawnPoint(SpawnPoint point)
		{
			//Make sure we don't nullify a point if the currentPoint has changed
			if (m_CurrentSpawnPoint == point)
			{
				m_CurrentSpawnPoint = null;
			}
		}

		//Field that stores the index of the last player to do damage to this tank.
		private int m_LastDamagedByPlayerNumber = -1;
		public int lastDamagedByPlayerNumber
		{
			get
			{
				return m_LastDamagedByPlayerNumber;
			}
		}

		//Field that stores the last explosion ID to damage this tank. Used for analytics purposes.
		private string m_LastDamagedByExplosionId;
		public string lastDamagedByExplosionId
		{
			get
			{
				return m_LastDamagedByExplosionId;
			}
		}

		public Vector3 GetPosition()
		{
			return transform.position;
		}
		
		// This is called whenever the tank takes damage. Implements IDamageObject.
		public void Damage(float amount)
		{
			if (invulnerable)
			{
				return;
			}

			RpcDamageFlash(m_LastDamagedByPlayerNumber);

			//If we have shields, ensure that these are reduced before applying damage to the tank's main health.
			if (m_ShieldLevel > 0)
			{
				m_ShieldLevel -= amount;

				//If shields have dropped below zero, transfer the balance of the damage to the tank's main health.
				if (m_ShieldLevel <= 0)
				{
					amount = Mathf.Abs(m_ShieldLevel);
					m_ShieldLevel = 0;
				}
				else
				{
					amount = 0;
				}
			}

			// Reduce current health by the amount of damage done.
			m_CurrentHealth -= amount;

			// If the current health is at or below zero and it has not yet been registered, call OnZeroHealth.
			if (m_CurrentHealth <= 0f && !m_ZeroHealthHappened)
			{
				OnZeroHealth();
			}
		}

		//Sets the shield level to a given value. Called by the shield powerup object to enable shields.
		public void SetShieldLevel(float value)
		{
			m_ShieldLevel = value;
		}

		//Hooked into the currenthealth syncvar. Updates whenever health changes server-side.
		void OnCurrentHealthChanged(float value)
		{
			m_CurrentHealth = value;

			if (healthChanged != null)
			{
				healthChanged(m_CurrentHealth / m_StartingHealth);
			}
		}

		//Hooked into the shield level syncvar. Updates whenever shield level changes server-side.
		void OnShieldLevelChanged(float value)
		{
			m_ShieldLevel = value;

			m_TankDisplay.SetShieldBubbleActive(m_ShieldLevel > 0);

			if (shieldChanged != null)
			{
				shieldChanged(m_ShieldLevel / m_StartingHealth);
			}
		}

		//Fires when health reaches zero on the server.
		private void OnZeroHealth()
		{
			// Set the flag so that this function is only called once.
			m_ZeroHealthHappened = true;

			RpcOnZeroHealth();

			if (isServer)
			{
				GameManager.s_Instance.rulesProcessor.TankDies(m_Manager);
			} 
		}

		//Fired on clients via RPC to perform death cleanup.
		private void InternalOnZeroHealth()
		{
			//Disable any active powerup SFX
			m_ShieldLevel = 0f;
			m_TankDisplay.SetShieldBubbleActive(false);
			m_TankDisplay.SetNitroParticlesActive(false);

			// Disable the collider and all the appropriate child gameobjects so the tank doesn't interact or show up when it's dead.
			SetTankActive(false);

			if (m_CurrentSpawnPoint != null)
			{
				m_CurrentSpawnPoint.Decrement();
			}

			if (playerDeath != null)
			{
				playerDeath();
			}
		}

		//Initializes all required references to external scripts.
		public void Init(TankManager manager)
		{
			m_Manager = manager;
			m_TankDisplay = manager.display;
			m_StartingHealth = manager.playerTankType.hitPoints;
			m_Collider = m_TankDisplay.GetComponent<BoxCollider>();
		}

		[ClientRpc]
		public void RpcDelayedReset()
		{
			m_Manager.Reset(null);
		}

		[ClientRpc]
		//Fired on clients to make tanks damaged by the local player flash red.
		private void RpcDamageFlash(int sourcePlayer)
		{
			if (sourcePlayer == GameManager.s_Instance.GetLocalPlayerId())
			{
				m_TankDisplay.StartDamageFlash();
			}
		}

		[ClientRpc]
		private void RpcOnZeroHealth()
		{
			// Break off our decorations
			m_TankDisplay.DetachDecorations();

			if (ExplosionManager.s_InstanceExists && m_DeathExplosion != null)
			{
				ExplosionManager.s_Instance.SpawnExplosion(transform.position, Vector3.up, gameObject, m_Manager.playerNumber, m_DeathExplosion, false);
			}

			InternalOnZeroHealth();
		}

		private void SetTankActive(bool active)
		{
			if (m_Collider == null && m_TankDisplay != null)
			{
				m_Collider = m_TankDisplay.GetComponent<BoxCollider>();
			}
			if (m_Collider != null)
			{
				m_Collider.enabled = active;
			}

			m_TankDisplay.SetVisibleObjectsActive(active);

			m_AimCanvas.SetActive(active);

			if (active)
			{
				m_Manager.EnableControl();
			}
			else
			{
				m_Manager.DisableControl();
			}
		}

		// This function is called at the start of each round to make sure each tank is set up correctly.
		public void SetDefaults()
		{
			m_CurrentHealth = m_StartingHealth;
			m_ShieldLevel = 0f;
			m_TankDisplay.SetShieldBubbleActive(false);
			m_ZeroHealthHappened = false;
			SetTankActive(true);

			if (playerReset != null)
			{
				playerReset();
			}
		}

		public bool IsPlayerDead()
		{
			return m_ZeroHealthHappened;
		}

		//Assigns internal damage variables from explosion.
		public void SetDamagedBy(int playerNumber, string explosionId)
		{
			//If we've received the tank suicide index, replace it with this tank's player index to count it as a suicide.
			if (playerNumber == TANK_SUICIDE_INDEX)
			{
				playerNumber = m_Manager.playerNumber;
			}

			Debug.LogFormat("Destroyed by playerNumber = {0}", playerNumber);
			m_LastDamagedByPlayerNumber = playerNumber;
			m_LastDamagedByExplosionId = explosionId;
		}
	}
}
