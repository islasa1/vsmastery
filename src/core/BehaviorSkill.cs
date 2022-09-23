using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace vsmastery
{

public class BehaviorSkills : EntityBehavior
{
  
  // Our two reads on skills, skillTree is truth, though for the ease
  // of not needing to constantly convert things we will keep skills up-to-date
  ITreeAttribute skillTree_;
  Dictionary< string, Dictionary<  string, Skill > > skills_;

  ICoreAPI       api_;
  public static string  BEHAVIOR = "vsmasteryskills";

  Skill          defaultSkill_ = new Skill();

  long listenerId;
  

  public BehaviorSkills( Entity entity ) : base( entity )
  {

  }

  void addSkillPoint( string category, string skill, Skill.SkillPoint pointType )
  {
    if ( skillTree_.HasAttribute( category ) )
    {
      TreeArrayAttribute categoryTree = ( skillTree_[ category ] as TreeArrayAttribute );
      if ( categoryTree != null )
      {
        // Grab the first skill that has this skillname, these *should* be unique
        // The main reason I didn't opt for a dictionary of skillnames and instead did a list is preserve
        // order in case I want them to be logically placed based on the entry
        ITreeAttribute skillAttributes = categoryTree.value.First( 
                                                                  tree => 
                                                                    tree.HasAttribute( "skillname" ) && tree.GetString( "skillname" ) == skill  
                                                                    );
        if ( skillAttributes != null )
        {
          System.Console.WriteLine( VSMastery.MODLOG + "Skill found and added point " + pointType.ToString() );

          // By time we call this a skill should be fully defined
          skillAttributes.SetFloat( "exp", skillAttributes.GetFloat( "exp" ) + skillAttributes.GetFloat( pointType.ToString().ToLower() ) );
          // Update skills
          skills_[ category ][ skill ].exp_ = skillAttributes.GetFloat( "exp" );
          entity.WatchedAttributes.MarkPathDirty( BEHAVIOR );
        }
      }
    }
  }

  

  void
  parseSkills( JsonObject typeAttributes )
  {
    
    /// \todo This can be done once at the beginning of the start of the client....
    //------------------------------------------------------------------------
    // First get the defaults
    defaultSkill_ = new Skill( typeAttributes[ "defaults" ].ToAttribute() as ITreeAttribute, defaultSkill_ );
    // This immediately should mirror what we have in the config/player json patch, we will need to fill out defaults though
    skillTree_.MergeTree( typeAttributes[ "skills" ].ToAttribute() as ITreeAttribute );
    //------------------------------------------------------------------------
    loadSkills( );

  }

  public void loadSkills( )
  {
    
    if ( skillTree_ != null )
    {
      
      // loop through the tree and get out all the skills
      foreach ( KeyValuePair< string, IAttribute > category in skillTree_ )
      {

        // adding this category - throws if not unique
        skills_.Add( category.Key, new Dictionary<string, Skill>() );

        TreeArrayAttribute skillsInCategory = category.Value as TreeArrayAttribute;
        
        // Get all our skills, do a traditional loop so we can overwrite
        // foreach ( IAttribute skillValue in skillsInCategory.value )
        for ( int skillIdx = 0; skillIdx < skillsInCategory.value.Count(); skillIdx++ )
        {
          // Parse the skill using the server default as the fall back
          vsmastery.Skill sk = new Skill( skillsInCategory.value[skillIdx] as ITreeAttribute, defaultSkill_ );

          // Overwrite since we are now all good
           skillsInCategory.value[ skillIdx ] = sk.asTreeAttribute();

          //--------------------------------------------------------------------
          // I could probably just re-assign instead of merging since I naturally
          // since on creation
          // Sync skills with attribute tree - work around merge trees manually
          // as the current implementation is broken
          // skill.MergeTree( sk.asTreeAttribute( false ) );
          // // Merge
          // if ( skill.HasAttribute( "factor" ) )
          // {
          //   ( skill[ "factor" ] as ITreeAttribute ).MergeTree( sk.factor_.asTreeAttribute() );
          // }
          // else
          // {
          //   skill[ "factor" ] = sk.factor_.asTreeAttribute();
          // }
          //--------------------------------------------------------------------

          
          // Add it to the category
          skills_[ category.Key ].Add( sk.skillname_, sk );

        }

      }
      
    }
  }

  // I think typeAttributes is the rest of the json that can be packed with a behavior
  public override void Initialize( EntityProperties properties, JsonObject typeAttributes )
  {
    skillTree_ = entity.WatchedAttributes.GetTreeAttribute( BEHAVIOR );
    api_       = entity.World.Api;

    skills_    = new Dictionary<string, Dictionary<string, Skill>>();

    if ( skillTree_ == null )
    {

      entity.WatchedAttributes.SetAttribute( BEHAVIOR, skillTree_ = new TreeAttribute() );

      // Start pulling out all the info
      parseSkills( typeAttributes );
    
    }
    else
    {
      // Load up our skills from the WatchedAttributes
      loadSkills( );
    }

    // FOR TESTING PURPOSES
    // listenerId = entity.World.RegisterGameTickListener( testUpdate, 5000 );

  }

  public override void OnEntityDespawn( EntityDespawnReason despawn )
  {
      base.OnEntityDespawn( despawn );

      // entity.World.UnregisterGameTickListener( listenerId );
  }

  public override string PropertyName()
  {
    return BEHAVIOR;
  }

  public override void OnInteract( 
                                  EntityAgent      byEntity,
                                  ItemSlot         itemSlot,
                                  Vec3d            hitPosition,
                                  EnumInteractMode mode,
                                  ref EnumHandling handled
                                  )
  {
    
  }

  public override void DidAttack(
                                  DamageSource     source, 
                                  EntityAgent      targetEntity, 
                                  ref EnumHandling handled 
                                  )
  {
    if ( source.Source == EnumDamageSource. )
  }

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  // CORE BEHAVIOR 

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ///
  /// MINING
  ///
  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ///
  /// METALSMITHING
  ///
  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  ///
  /// COMBAT
  ///
  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  
  

  private void testUpdate( float dt )
  {
    System.Console.WriteLine( VSMastery.MODLOG + "Adding skillpoint!" );
    addSkillPoint( "mining", "oreminer", Skill.SkillPoint.PRIMARY );
  }


}

}