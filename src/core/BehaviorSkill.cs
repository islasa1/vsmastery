using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

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

    if ( skillTree_ != null )
    {
      
      // loop through the tree and get out all the skills
      foreach ( KeyValuePair< string, IAttribute > category in skillTree_ )
      {

        // adding this category - throws if not unique
        skills_.Add( category.Key, new Dictionary<string, Skill>() );

        TreeArrayAttribute skillsInCategory = category.Value as TreeArrayAttribute;
        
        // Get all our skills
        foreach ( IAttribute skillValue in skillsInCategory.value )
        {
          // Parse the skill using the server default as the fall back
          ITreeAttribute skill = skillValue as ITreeAttribute;
          vsmastery.Skill sk = new Skill( skill, defaultSkill_ );

          // Sync skills with attribute tree
          skill.MergeTree( sk.asTreeAttribute() );
          
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
    
    }

    // Start pulling out all the info
    parseSkills( typeAttributes );

    // FOR TESTING PURPOSES
    listenerId = entity.World.RegisterGameTickListener( testUpdate, 1000 );

  }

  public override void OnEntityDespawn( EntityDespawnReason despawn )
  {
      base.OnEntityDespawn( despawn );

      entity.World.UnregisterGameTickListener( listenerId );
  }

  public override string PropertyName()
  {
    return BEHAVIOR;
  }

  //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
  // CORE BEHAVIOR 
  private void testUpdate( float dt )
  {
    addSkillPoint( "mining", "oreminer", Skill.SkillPoint.PRIMARY );
  }


}

}