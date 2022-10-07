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

        foreach( KeyValuePair< string, IAttribute > skill in category.Value as TreeAttribute )
        {
          // Parse the skill using the server default as the fall back
          vsmastery.Skill sk = new Skill( skill.Key, skill.Value as ITreeAttribute, defaultSkill_ );

          // Manual merge, might be fixed in 1.17.5 - I don't want to write to an enumerable while looping
          ( skill.Value as ITreeAttribute ).MergeTree( sk.asTreeAttribute( false ) );

          // This one actually doesn't need to be a merge since we have the object
          ( skill.Value as ITreeAttribute )["factor"] = sk.factor_.asTreeAttribute();
          
          // Add it to the category
          skills_[ category.Key ].Add( sk.skillname_, sk );
        }

      }
      
    }
  }

  public void syncSkillsExp()
  {
    // FOR NOW I'M JUST IGNORING THIS, skills will be truth
    // // First delete all mismatched things
    // foreach ( KeyValuePair< string, IAttribute > category in watchedExp_ )
    // {
    //   if ( !skills_.ContainsKey( category.Key ) )
    //   {
    //     System.Console.WriteLine( "{0}Category {1} is no longer tracked!", VSMastery.MODLOG, category.Key );
        
    //     continue;
    //   }
    //   foreach ( KeyValuePair< string, IAttribute > skillValue in category.Value as ITreeAttribute )
    //   {
    //     if ( !skills_[ category.Key ].ContainsKey( skillname ) )
    //     {
    //       System.Console.WriteLine( "{0}Skill {1} is no longer tracked!", VSMastery.MODLOG, skillname );
    //       continue;
    //     } 
    //   }
    // }

    // First go through all exp from the watched attributes and fit into existing skills
    foreach ( KeyValuePair< string, IAttribute > category in watchedExp_ )
    {
      if ( !skills_.ContainsKey( category.Key ) )
      {
        System.Console.WriteLine( "{0}Category {1} is no longer tracked!", VSMastery.MODLOG, category.Key );
        continue;
      } 
      
      foreach ( KeyValuePair< string, IAttribute > skillValue in category.Value as ITreeAttribute )
      {
      
        if ( !skills_[ category.Key ].ContainsKey( skillValue.Key ) )
        {
          System.Console.WriteLine( "{0}Skill {1} is no longer tracked!", VSMastery.MODLOG, skillValue.Key );
          continue;
        } 

        // Go to the respective skill and add to it - check if loaded to skills_
        skills_[ category.Key ][ skillValue.Key ].exp_          = ( skillValue.Value as ITreeAttribute ).GetFloat( "exp"            );
        skills_[ category.Key ][ skillValue.Key ].expprimary_   = ( skillValue.Value as ITreeAttribute ).GetFloat( "expprimary"     );
        skills_[ category.Key ][ skillValue.Key ].expsecondary_ = ( skillValue.Value as ITreeAttribute ).GetFloat( "expsecondary"   );
        skills_[ category.Key ][ skillValue.Key ].expmisc_      = ( skillValue.Value as ITreeAttribute ).GetFloat( "expmisc"        );
        
        // Even though we "watch" maxes, that will actually be coming from skill_

      }
    }

    // Now sync skill_ into watchedExp
    foreach ( KeyValuePair< string, Dictionary< string, Skill > > category in skills_ )
    {
      
      if ( !watchedExp_.HasAttribute( category.Key ) )
      {
        System.Console.WriteLine( "{0}Category {1} added to watched experience", VSMastery.MODLOG, category.Key );
        // Initialize it with the same size of skills we will add
        watchedExp_[ category.Key ] = new TreeAttribute();
      }
     
      // Get all our skills' exp
      foreach ( KeyValuePair< string, Skill > skill in category.Value )
      {

        // Find if there is a skill in this category
        if ( ! ( watchedExp_[ category.Key ] as ITreeAttribute ).HasAttribute( skill.Key ) )
        {
          System.Console.WriteLine( "{0}Skill {1} added to watched experience", VSMastery.MODLOG, skill.Key );
          // Initialize it with the same size of skills we will add
          ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] = new TreeAttribute();
        }
        
        // We have not synced skills_ to this then
        ( ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] as ITreeAttribute ).SetFloat( "exp",          skill.Value.exp_ );
        ( ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] as ITreeAttribute ).SetFloat( "expprimary",   skill.Value.expprimary_ );
        ( ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] as ITreeAttribute ).SetFloat( "expsecondary", skill.Value.expsecondary_ );
        ( ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] as ITreeAttribute ).SetFloat( "expmisc",      skill.Value.expmisc_ );

        // Now set max
        ( ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] as ITreeAttribute ).SetFloat( "max",          skill.Value.max_ );
        ( ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] as ITreeAttribute ).SetFloat( "maxsecondary", skill.Value.maxsecondary_ );
        ( ( watchedExp_[ category.Key ] as ITreeAttribute )[ skill.Key ] as ITreeAttribute ).SetFloat( "maxmisc",      skill.Value.maxmisc_ );

      }
    }

  }

  // I think typeAttributes is the rest of the json that can be packed with a behavior
  public override void Initialize( EntityProperties properties, JsonObject typeAttributes )
  {
    watchedExp_ = entity.WatchedAttributes.GetTreeAttribute( BEHAVIOR );
    api_        = entity.World.Api;

    skills_    = new Dictionary<string, Dictionary<string, Skill>>();
    skillTree_ = new TreeAttribute();

    // Start pulling out all the info
    parseSkills( typeAttributes );

    if ( watchedExp_ == null )
    {

      entity.WatchedAttributes.SetAttribute( BEHAVIOR, watchedExp_ = new TreeAttribute() );      
    
    }
    // Load up our experience from the WatchedAttributes and from any new skills
    syncSkillsExp( );

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