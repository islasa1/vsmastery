using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace vsmastery
{

public class BehaviorSkills : EntityBehavior
{
  
  // Our three reads on skills:
  //    watchedExp is our entity attributes to watch
  //    skillTree  is our behavior configuration
  //    skills_    is synchronized to the two and for ease of calculations
  ITreeAttribute watchedExp_;
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
  
    ITreeAttribute categoryTree = ( watchedExp_[ category ] as ITreeAttribute );
    if ( categoryTree != null )
    {
      // Grab the skill 
      ITreeAttribute skillAttributes = categoryTree[ skill ] as ITreeAttribute;
      
      if ( skillAttributes != null )
      {
        System.Console.WriteLine( VSMastery.MODLOG + "Skill found and added point " + pointType.ToString() );

        // By time we call this a skill should be fully defined
        float? cap = null;
        if ( ( pointType == Skill.SkillPoint.SECONDARY ) || ( pointType == Skill.SkillPoint.MISC ) )
        {
          cap = skillAttributes.GetFloat( "max" + pointType.ToString().ToLower() );
        }

        // Pull directly from skills_ in this case
        float points    = skills_[ category ][ skill ].getPointValue( pointType );

        // Source original exp to get new
        float updateExp = skillAttributes.GetFloat( "exp" + pointType.ToString().ToLower() ) + points;

        if ( cap != null )
        {
          updateExp = System.Math.Max( (float)cap, updateExp );
          if ( updateExp == cap )
          {
            System.Console.WriteLine( VSMastery.MODLOG + "Max experience reached for : " + skill );
          }
        }

        // Check final exp?

        // Set the experience to the watched exp
        skillAttributes.SetFloat( "exp" + pointType.ToString().ToLower(), updateExp );

        entity.WatchedAttributes.MarkPathDirty( BEHAVIOR );
      }
    }
  }

  

  void
  parseSkills( JsonObject typeAttributes )
  {
    
    /// \todo This can be done once at the beginning of the start of the client....
    //------------------------------------------------------------------------
    // First get the defaults
    defaultSkill_ = new Skill( "default", typeAttributes[ "defaults" ].ToAttribute() as ITreeAttribute, defaultSkill_ );
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

  // public override void OnInteract( 
  //                                 EntityAgent      byEntity,
  //                                 ItemSlot         itemSlot,
  //                                 Vec3d            hitPosition,
  //                                 EnumInteractMode mode,
  //                                 ref EnumHandling handled
  //                                 )
  // {
  //   // Let's only accumulate on the server side?
  //   if ( api_.Side == EnumAppSide.Server )
  //   {
  //     // Get the player on the server
  //     // IServerPlayer splayer = ( byEntity as EntityPlayer )?.Player as IServerPlayer;
      
      
  //     // Use the current interaction test
  //     //////////////////////////////////////////////////////////////////////////
  //     //
  //     // Mining
  //     if ( itemSlot.Itemstack.Collectible.Tool == EnumTool.Pickaxe  )
  //     {
  //       // Prospecting pickaxe
  //       if ( itemSlot.Itemstack.Item is ItemProspectingPick )
  //       {

  //       }
  //       // Regular pickaxe
  //       else
  //       {

  //       }
  //     }
  //     else if ( itemSlot.Itemstack.Collectible.Tool == EnumTool.Hammer )
  //     {

  //     }
  //     else if ( itemSlot.Itemstack.Item is ItemHammer )
  //     {

  //     }
  //     else if ( itemSlot.Itemstack.Item is ItemHammer )
  //     {

  //     }

  //     // byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract

  //   }
  // }

  // public override void DidAttack(
  //                                 DamageSource     source,
  //                                 EntityAgent      targetEntity, 
  //                                 ref EnumHandling handled 
  //                                 )
  // {
  //   if ( source.Source == EnumDamageSource. )
  // }

  public override void Notify( string key, object data )
  {
    if ( key == GridRecipePatch.NOTIFY_KEY && data is GridRecipe )
    {
      System.Console.WriteLine( VSMastery.MODLOG + "Recipe crafted!" );
    }
  }

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  // CORE BEHAVIOR 

  public void distributePoints( 
                                string   sourceEvent, 
                                EnumTool tool, 
                                string   toolname
                                )
  {
    
  }


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