﻿using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Lightbug.Utilities;
using Lightbug.CharacterControllerPro.Core;

namespace Lightbug.CharacterControllerPro.Implementation
{


/// <summary>
/// This class is responsable for detecting inputs and managing character actions. These actions may come from a human (the player) or an AI.
/// </summary>
// [RequireComponent( typeof(CharacterActor) )]
[AddComponentMenu("Character Controller Pro/Implementation/Character/Character Brain")]
public class CharacterBrain : MonoBehaviour
{
    
    [SerializeField]    bool isAI = false;

	[SerializeField]	InputHandlerSettings inputHandlerSettings = new InputHandlerSettings();
		

    // AI brain -------------------------------------------------------------------------------
    [SerializeField]	CharacterAIBehaviour aiBehaviour = null;

	CharacterAIBehaviour currentAIBehaviour = null;
	
	
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
	// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

	[SerializeField]	CharacterActions characterActions = new CharacterActions();

	
	bool dirty = false;

	CharacterActor characterActor = null;
	
	/// <summary>
	/// Gets the current brain mode (AI or Human).
	/// </summary>
	public bool IsAI
	{
		get
		{
			return isAI;
		}
	}

	/// <summary>
	/// Gets the current character action info used by the character.
	/// </summary>
	public CharacterActions CharacterActions
	{
		get
		{
			return characterActions;
		}
	}


	protected virtual void Awake()
	{
		characterActor = this.GetComponentInBranch<CharacterActor>();

		characterActions.InitializeActions();

		inputHandlerSettings.Initialize( gameObject );		
		
	}


	void OnDisable()
	{
		characterActions.Reset();
	}

	
    void Start()
    {
        SetBrainType( isAI );

		StartCoroutine( LateFixedUpdate() );
    }

	/// <summary>
	/// Sets a custom character action info.
	/// </summary>
	public void SetAction( CharacterActions characterAction )
	{
		this.characterActions = characterAction;
	}


	    
	/// <summary>
	/// Sets the type of brain.
	/// </summary>
    public void SetBrainType( bool AI )
    {
		characterActions.Reset();

        if( AI )
        {
            SetAIBehaviour( aiBehaviour );        
        }

        this.isAI = AI;
    
    }

	/// <summary>
	/// Sets the AI behaviour type.
	/// </summary>
    public void SetAIBehaviour( CharacterAIBehaviour aiBehaviour )
    {
		if( aiBehaviour == null )
			return;
		
		characterActions.Reset();

		currentAIBehaviour = aiBehaviour;

		currentAIBehaviour.EnterBehaviour( Time.deltaTime );
        
    }
	
	

	IEnumerator LateFixedUpdate()
	{
		YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();
		while( true )
		{
			yield return waitForFixedUpdate;
			dirty = true;
		}
	}
	
	
	void Update()
	{
		float dt = Time.deltaTime;

		if( dirty )
		{
			dirty = false;
			characterActions.Reset();
		}

		UpdateBrain( dt );
	}


	public void UpdateBrain( float dt = 0f )
	{
		if( Time.timeScale == 0 )
			return;
		
		if( isAI )
		{			

			if( currentAIBehaviour == null )
				return;

			// Update the current AI logic.
			currentAIBehaviour.UpdateBehaviour( dt );

			// Copy the actions from the AI behaviour to the Brain.
			characterActions.SetValues( currentAIBehaviour.characterActions );			
		
			
		}
		else
		{
			// Update the human actions
			characterActions.SetValues( inputHandlerSettings.InputHandler );

		}

		// Update all the fields based on the change of state.
		characterActions.Update( dt );
		
	}


}

}
