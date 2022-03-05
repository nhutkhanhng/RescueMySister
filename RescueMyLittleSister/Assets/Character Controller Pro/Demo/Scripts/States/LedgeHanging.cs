﻿using System.Collections.Generic;
using UnityEngine;
using Lightbug.CharacterControllerPro.Core;
using Lightbug.Utilities;
using Lightbug.CharacterControllerPro.Implementation;

namespace Lightbug.CharacterControllerPro.Demo
{

[AddComponentMenu("Character Controller Pro/Demo/Character/States/Ledge Hanging")]
public class LedgeHanging : CharacterState
{    
    
    [Header("Filter")]

    [SerializeField]
    protected LayerMask layerMask = 0; 

    [SerializeField]
    protected bool filterByTag = false; 

    [SerializeField]
    protected string tagName = "Untagged"; 

    [Header("Detection")]    

    [SerializeField]
    protected bool groundedDetection = false;

    [Tooltip("How far the hands are from the character along the forward direction. The \"extra\" is because this value is added to the current body radius (BodySize.x / 2). " + 
    "This allows runtime changes in the body size without affecting the ledge detection.")]
    [Min(0f)]
    [SerializeField]
    protected float forwardDetectionOffset = 0.5f;

    [Tooltip("How far the hands are the character along the up direction. The \"extra\" is because this value is added to the current body height (BodySize.y). " + 
    "This allows runtime changes in the body size without affecting the ledge detection.")]
    [Min(0.05f)]
    [SerializeField]
	protected float upwardsDetectionOffset = 1.8f;
    
    [Min(0.05f)]
	[SerializeField]
    protected float separationBetweenHands = 1f;

    [Tooltip("The distance used by the raycast methods.")]
    [Min(0.05f)]
	[SerializeField]
	protected float ledgeDetectionDistance = 0.05f;

    [Header("Offset")]

    [SerializeField]
    protected float verticalOffset = 0f;

    [SerializeField]
    protected float forwardOffset = 0f;

    [Header("Movement")]

    [SerializeField]
    protected bool autoClimbUp = true;

    [Tooltip("If the previous state (\"fromState\") is contained in this list the autoClimbUp flag will be triggered.")]
    [SerializeField]
    protected CharacterState[] forceAutoClimbUpStates = null;

    [Header("Animation")]
    
    [SerializeField]
    protected string topUpParameter = "TopUp";

        

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    protected const float MaxLedgeVerticalAngle = 50f;
    

    public enum LedgeHangingState
    {
        Idle ,
        TopUp
    }

    protected LedgeHangingState state;    


    protected bool forceExit = false;

    protected bool forceAutoClimbUp = false;

    protected HitInfoFilter ledgeHitInfoFilter;

    protected override void Awake()
    {
        base.Awake();    
        
        ledgeHitInfoFilter = new HitInfoFilter( layerMask , true , true );
    }    

    protected override void Start()
    {
        base.Start();

        if( CharacterStateController.Animator == null )
        {
            Debug.Log("The LadderClimbing state needs the character to have a reference to an Animator component. Destroying this state...");
            Destroy(this);
        }     
        
    }  

	public override void CheckExitTransition()
    {          
        if( forceExit )
            CharacterStateController.EnqueueTransition<NormalMovement>();

	}

    HitInfo leftHitInfo = new HitInfo();
    HitInfo rightHitInfo = new HitInfo();


	public override bool CheckEnterTransition( CharacterState fromState )
    {        
        
        if( !groundedDetection && CharacterActor.IsGrounded )
            return false;
        
        if( !IsValidLedge( CharacterActor.Position ) )
            return false;
        
        
        return true;
	}
   

    public override void EnterBehaviour( float dt , CharacterState fromState )
    {
        
        CharacterActor.alwaysNotGrounded = true;
        CharacterActor.IsKinematic = true;
        
        forceExit = false;

        for( int i = 0 ; i < forceAutoClimbUpStates.Length ; i++ )
        {
            CharacterState state = forceAutoClimbUpStates[i];
            if( fromState == state )
            {
                forceAutoClimbUp = true;
                break;
            }
        }

        CharacterActor.SetBodySize( CharacterActor.DefaultBodySize );

        CharacterActor.Forward = Vector3.ProjectOnPlane( - CharacterActor.WallContact.normal , CharacterActor.Up );        

        CharacterActor.Velocity =  Vector3.zero;


        Vector3 referencePosition = 0.5f * ( leftHitInfo.point + rightHitInfo.point );

        Vector3 headToReference = referencePosition - CharacterActor.Top;
            
        Vector3 correction = Vector3.Project( headToReference , CharacterActor.Up ) + 
            verticalOffset * CharacterActor.Up + 
            forwardOffset * CharacterActor.Forward;
        
        targetPosition = CharacterActor.Position + correction;
     
        CharacterActor.Position = targetPosition;                
        CharacterStateController.UseRootMotion = true;
        state = LedgeHangingState.Idle;        
       
        
    }

    Vector3 targetPosition;

    public override void ExitBehaviour( float dt , CharacterState toState )
    {
        CharacterActor.Velocity =  Vector3.zero;
        CharacterStateController.UseRootMotion = false;  
        CharacterActor.IsKinematic = false;
        CharacterActor.alwaysNotGrounded = false;    
        forceAutoClimbUp = false;
        
    }

	public override void UpdateBehaviour( float dt )
    {        
        // bool isTag = CharacterStateController.Animator.GetCurrentAnimatorStateInfo(0).tagHash == StateNameHash;
        
        switch( state )
        {  

            case LedgeHangingState.Idle:                                
                
                if( CharacterActions.movement.Up || autoClimbUp || forceAutoClimbUp)
                {
                    state = LedgeHangingState.TopUp;
                    CharacterStateController.UseRootMotion = true;
                    CharacterStateController.Animator.SetTrigger( topUpParameter );    

                    
                }
                else if( CharacterActions.movement.Down )
                {
                    forceExit = true;
                }

                break;

            case LedgeHangingState.TopUp:
                
                if( CharacterStateController.Animator.GetCurrentAnimatorStateInfo(0).IsName("Exit") )
                {
                    forceExit = true;
                    CharacterActor.ForceGrounded();
                }       

                
                break;
        }       


	}



    bool IsValidLedge( Vector3 characterPosition )
    {         
        if( !CharacterActor.WallCollision )
            return false;
        
        DetectLedge( 
            characterPosition , 
            out leftHitInfo , 
            out rightHitInfo            
        );
        
        if( !leftHitInfo.hit || !rightHitInfo.hit )
            return false;
        
        // If the object is a rigidbody return.
        if( leftHitInfo.IsRigidbody || rightHitInfo.IsRigidbody )
            return false;
        
        if( filterByTag )
            if( !string.Equals( leftHitInfo.transform.tag , tagName ) || !string.Equals( rightHitInfo.transform.tag , tagName ) )
                return false;

        Vector3 interpolatedNormal = Vector3.Normalize( leftHitInfo.normal + rightHitInfo.normal );
        float ledgeAngle = Vector3.Angle( CharacterActor.Up , interpolatedNormal );
        if( ledgeAngle > MaxLedgeVerticalAngle )
            return false;

        return true;
    }


    void DetectLedge( Vector3 position , out HitInfo leftHitInfo , out HitInfo rightHitInfo )
	{
		leftHitInfo = new HitInfo();
		rightHitInfo = new HitInfo();

        Vector3 forwardDirection = CharacterActor.WallCollision ? - CharacterActor.WallContact.normal : CharacterActor.Forward;

        // if( CharacterActor.CharacterBody.Is2D )
        //     forward = Vector3.ProjectOnPlane( forward , Vector3.forward );
        
        Vector3 sideDirection = Vector3.Cross( CharacterActor.Up , forwardDirection );
        
        // Check if there is an object above
        Vector3 upDetection = position + CharacterActor.Up * ( upwardsDetectionOffset );
        HitInfo auxHitInfo;
        
        CharacterActor.PhysicsComponent.Raycast( 
			out auxHitInfo ,
			CharacterActor.Center ,
			upDetection - CharacterActor.Center ,
			ledgeHitInfoFilter
		);


        if( auxHitInfo.hit )
            return;

        Vector3 middleOrigin = upDetection + forwardDirection * ( forwardDetectionOffset );

        Vector3 leftOrigin = middleOrigin - sideDirection * ( separationBetweenHands / 2f );
        Vector3 rightOrigin = middleOrigin + sideDirection * ( separationBetweenHands / 2f );

        CharacterActor.PhysicsComponent.Raycast( 
            out leftHitInfo ,
            leftOrigin ,
            - CharacterActor.Up * ledgeDetectionDistance ,
            ledgeHitInfoFilter
        );
        
        
        CharacterActor.PhysicsComponent.Raycast( 
            out rightHitInfo ,
            rightOrigin ,
            - CharacterActor.Up * ledgeDetectionDistance ,
            ledgeHitInfoFilter
        );
        		
        
		
	}

    

#if UNITY_EDITOR
        
    CharacterBody characterBody = null;

    void OnValidate()
    {
        characterBody = this.GetComponentInBranch<CharacterBody>();        
    }

    void OnDrawGizmos()
	{       
        Vector3 forwardDirection = transform.forward;
        
        Vector3 sideDirection = Vector3.Cross( transform.up , forwardDirection );

        
        
        Vector3 middleOrigin = 
            transform.position + transform.up * ( upwardsDetectionOffset ) + forwardDirection * ( forwardDetectionOffset );

		Vector3 leftOrigin = middleOrigin - sideDirection * ( separationBetweenHands / 2f );

        Vector3 rightOrigin = middleOrigin + sideDirection * ( separationBetweenHands / 2f );
		        
        
        CustomUtilities.DrawArrowGizmo( leftOrigin , leftOrigin - transform.up * ledgeDetectionDistance , Color.red , 0.15f );
        CustomUtilities.DrawArrowGizmo( rightOrigin , rightOrigin - transform.up * ledgeDetectionDistance , Color.red , 0.15f );
        



    }

#endif

}

}

