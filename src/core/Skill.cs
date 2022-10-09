using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace vsmastery
{

public class Skill
{

  public enum SkillPoint
  {
    PRIMARY,
    SECONDARY,
    MISC
  }


  public float exp  { get { return System.Math.Max( max_, exp_ + expprimary_ + expsecondary_ + expmisc_ ); } }


  public float exp_          =     0.0f;
  public float expprimary_   =     0.0f;
  public float expsecondary_ =     0.0f;
  public float expmisc_      =     0.0f;

  public float max_          = 10000.0f;
  
  public float maxsecondary_ =  2000.0f;
  public float maxmisc_      =   500.0f;

  public float primary_    =     5.0f;
  public float secondary_  =     2.5f;
  public float misc_       =     1.0f;

  public List< SkillEvent > events_;

  // Might not be used here
  // public static float LEVEL_STEP = 1.0 / 7.0;

  public string skillname_ = null;
  public SkillFactor factor_ = new SkillFactor();

  // Not loaded or saved
  ITreeAttribute     points_ = new TreeAttribute();


  /// default ctor
  public Skill( ) { }

  /// Parse from a tree
  public Skill( string skillname, ITreeAttribute skillAttributes, Skill defaultSkill )
  {
    this.skillname_ = skillname;
    this.events_    = new List< SkillEvent >();
    this.readFromTree( skillAttributes, defaultSkill );
  }

  public void readFromTree( ITreeAttribute skillAttributes, Skill defaultSkill )
  {
    // this.skillname_ = skillAttributes.GetString( "skillname" ) ?? defaultSkill.skillname_;

    this.exp_          = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "exp"            ) ?? defaultSkill.exp_;
    this.expprimary_   = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "expprimary"     ) ?? defaultSkill.expprimary_;
    this.expsecondary_ = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "expsecondary"   ) ?? defaultSkill.expsecondary_;
    this.expmisc_      = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "expmisc"        ) ?? defaultSkill.expmisc_;
    
    this.max_          = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "max"            ) ?? defaultSkill.max_;
    this.maxsecondary_ = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "maxsecondary"   ) ?? defaultSkill.maxsecondary_;
    this.maxmisc_      = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "maxmisc"        ) ?? defaultSkill.maxmisc_;

    this.primary_      = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "primary"        ) ?? defaultSkill.primary_;
    this.secondary_    = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "secondary"      ) ?? defaultSkill.secondary_;
    this.misc_         = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "misc"           ) ?? defaultSkill.misc_;
    

    ITreeAttribute factorTree = skillAttributes.GetTreeAttribute( "factor" );
    if ( factorTree != null )
    {
      this.factor_.readFromTree( factorTree, defaultSkill.factor_ );
    }

    TreeArrayAttribute events = skillAttributes[ "events" ] as TreeArrayAttribute;
    if ( events != null )
    {
      SkillEvent defaultEvent = new SkillEvent();

      foreach ( TreeAttribute skillEvent in events.value )
      {
        events_.Add( new SkillEvent( skillEvent, defaultEvent ) );
      }
    }
    else 
    {
      System.Console.WriteLine( "{0}Skill {1} has no events to accumulate experience", VSMastery.MODLOG, skillname_ );
    }

  }

  public float getExpValue( SkillPoint pointType )
  {
    switch ( pointType )
    {
      case SkillPoint.PRIMARY   : return this.expprimary_;
      case SkillPoint.SECONDARY : return this.expsecondary_;
      case SkillPoint.MISC      : return this.expmisc_;
      default                   : return 0.0f;
    }
  }

  public float getPointValue( string pointType )
  {
    // enum parsing
    SkillPoint point;
    bool parsed = System.Enum.TryParse( pointType, out point );
    if ( parsed )
    {
      return getPointValue( point );
    }
    else
    { 
      return 0.0f;
    }
  }

  public float getPointValue( SkillPoint pointType )
  {
    switch ( pointType )
    {
      case SkillPoint.PRIMARY   : return this.primary_;
      case SkillPoint.SECONDARY : return this.secondary_;
      case SkillPoint.MISC      : return this.misc_;
      default                   : return 0.0f;
    }
  }

  public bool addPointValue( string pointType )
  {
    SkillPoint point;
    bool parsed = System.Enum.TryParse( pointType, out point );
    if ( parsed )
    {
      return addPointValue( point );
    }
    else
    { 
      return false;
    }
  }

  public bool addPointValue( SkillPoint pointType )
  {
  
    float pointAdd = getPointValue( pointType );
    if ( pointAdd == 0.0f ) return false;

    switch ( pointType )
    {
      case SkillPoint.PRIMARY   :
      {
        expprimary_ += pointAdd;
        break;
      }
      case SkillPoint.SECONDARY :
      {
        expsecondary_ = System.Math.Min( maxsecondary_, expsecondary_ + pointAdd );
        if ( expsecondary_ == maxsecondary_ )
        {
          System.Console.WriteLine( VSMastery.MODLOG + "Max secondary experience reached for : " + skillname_ );
        }
        break;
      }
      case SkillPoint.MISC      :
      {
        expmisc_ = System.Math.Min( maxmisc_, expmisc_ + pointAdd );
        if ( expmisc_ == maxmisc_ )
        {
          System.Console.WriteLine( VSMastery.MODLOG + "Max misc experience reached for : " + skillname_ );
        }
        break;
      }
      default                   :
      {
        // Shouldn't get here but will check anyways
        return false;
      }
    }

    return true;

  }

  private float hyperbolic( float x )
  {
    return x / ( x + 1 );
  }

  public float experienceLevel( )
  {

    float relativeExperience = exp / max_;

    // The first hyperbolic gets us into the 0->7/8 range, the next gets us (0->10/11)*1/8
    // Together when added we get *really* close to 8/8 but not quite while maintaining a hyperbolic shape
    float normalizedExpLevel = hyperbolic( relativeExperience * 7.0f ) + ( hyperbolic( relativeExperience * 10.0f ) * 1.0f / 8.0f );

    return normalizedExpLevel;

  }


  // Our primary function that basically calculates what this skill amounts to. 
  // How this will be used is dependent on the skill logic, but that is for the behavior to decide
  public float skillFactor( )
  {
    return factor_.calc( experienceLevel() );
  }

  public TreeAttribute asTreeAttribute( bool getFactor = true )
  {
    TreeAttribute skillTree = new TreeAttribute();

    // skillTree.SetString( "skillname", skillname_ );

    skillTree.SetFloat( "exp",          exp_          );
    skillTree.SetFloat( "expprimary",   expprimary_   );
    skillTree.SetFloat( "expsecondary", expsecondary_ );
    skillTree.SetFloat( "expmisc",      expmisc_      );

    skillTree.SetFloat( "max",          max_          );
    skillTree.SetFloat( "maxsecondary", maxsecondary_ );
    skillTree.SetFloat( "maxmisc",      maxmisc_      );

    skillTree.SetFloat( "primary",      primary_      );
    skillTree.SetFloat( "secondary",    secondary_    );
    skillTree.SetFloat( "misc",         misc_         );
    
    
    if ( getFactor )
    {
      skillTree.SetAttribute( "factor", factor_.asTreeAttribute() );
    }

    return skillTree;

  }

  // Propagate events through all our events
  public List< SkillPoint > triggerEvent( SkillEvent.Event eventType, EnumTool? tool, string toolname, object data )
  {
    List< SkillPoint > points = new List< SkillPoint >();
    foreach( SkillEvent ev in events_.Where( ( SkillEvent ev ) => { return ev.event_ == eventType; } ) )
    {
      if ( ev.eventTriggered( tool, toolname, data ) )
      {
        points.Add( ev.pointType_ );
      }
    }

    return points;
  }
  

}

}