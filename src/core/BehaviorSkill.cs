using System;
using System.Linq;
using System.Collections.Generic;

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
  Dictionary< string, Dictionary<  string, Skill > >              skills_;
  Dictionary< SkillEvent.Event, List< Tuple< string, string > > > eventRegistry_;

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
        
        // Pull directly from skills_ in this case
        bool update = skills_[ category ][ skill ].addPointValue( pointType );

        if ( update )
        {
          System.Console.WriteLine( "{0}{1}::{2} skill found and added point {3}", VSMastery.MODLOG, category, skill, pointType.ToString() );

          // Set the experience to the watched exp
          skillAttributes.SetFloat( "exp" + pointType.ToString().ToLower(), skills_[ category ][ skill ].getExpValue( pointType ) );
          
        }

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

          // Add it to the event registry
          foreach( SkillEvent ev in skills_[ category.Key ][ sk.skillname_ ].events_ )
          {
            Tuple< string, string > catSkill = new Tuple<string, string>( category.Key, sk.skillname_ );
            if ( !eventRegistry_[ ev.event_ ].Contains( catSkill ) )
            {
              eventRegistry_[ ev.event_ ].Add( catSkill );
            }
          }
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

    // Probably could be a hashset but for now just use this
    // pre-make event registry for each type of event
    eventRegistry_ = SkillEvent.ALL_EVENTS.Select( ( ev ) => new { key = ev, value = new List< Tuple< string, string > >() } )
                                          .ToDictionary( obj => obj.key, obj => obj.value );

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
    // Route custom notifications to appropriate events
    if ( key == GridRecipePatch.NOTIFY_KEY && data is GridRecipe )
    {
      System.Console.WriteLine( VSMastery.MODLOG + "Recipe crafted!" );
    }
    else if ( key == BlockBehaviorBreakIfFloatingPatch.NOTIFY_KEY && data is ItemStack )
    {
      System.Console.WriteLine( VSMastery.MODLOG + "Stone relieved!" );

      // Get thing that broke it
      ItemStack currentItem = ( entity as EntityPlayer ).RightHandItemSlot.Itemstack;
      string    toolname    = currentItem.Collectible.Attributes?["slotRefillIdentifier"]?.ToString();

      experienceEvent( 
                      SkillEvent.Event.BREAK, 
                      currentItem.Collectible.Tool,
                      toolname,
                      data 
                      );
    }
  }

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  // CORE BEHAVIOR 

  public void experienceEvent( 
                                SkillEvent.Event sourceEvent, 
                                EnumTool?        tool, 
                                string           toolname,
                                object           data
                                )
  {
    List< Tuple< string, string > > events = eventRegistry_[ sourceEvent ];

    foreach( Tuple< string, string > skill in events )
    {
      List< Skill.SkillPoint > points = skills_[ skill.Item1 ][ skill.Item2 ].triggerEvent( sourceEvent, tool, toolname, data );
      if ( points.Count > 0 )
      {
        // Add the points to the skill
        points.ForEach( ( Skill.SkillPoint point ) => { this.addSkillPoint( skill.Item1, skill.Item2, point ); } );
      }
    }
  }
  

  private void testUpdate( float dt )
  {
    System.Console.WriteLine( VSMastery.MODLOG + "Adding skillpoint!" );
    addSkillPoint( "mining", "oreminer", Skill.SkillPoint.PRIMARY );
  }


}

}