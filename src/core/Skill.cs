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

  // Might not be used here
  // public static float LEVEL_STEP = 1.0 / 7.0;

  public string skillname_ = null;
  public SkillFactor factor_ = new SkillFactor();

  // Not loaded or saved
  ITreeAttribute     points_ = new TreeAttribute();


  /// default ctor
  public Skill( ) { }

  /// Parse from a tree
  public Skill( ITreeAttribute skillAttributes, Skill defaultSkill )
  {
    this.readFromTree( skillAttributes, defaultSkill );
  }

  public void readFromTree( ITreeAttribute skillAttributes, Skill defaultSkill )
  {
    this.skillname_ = skillAttributes.GetString( "skillname" ) ?? defaultSkill.skillname_;

    this.exp_          = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "exp"            ) ?? defaultSkill.exp_;
    this.expprimary_   = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "expprimary"     ) ?? defaultSkill.expprimary_;
    this.expsecondary_ = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "expsecondary"   ) ?? defaultSkill.expsecondary_;
    this.expmisc_      = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "expmisc"        ) ?? defaultSkill.expmisc_;
    this.max_          = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "max"            ) ?? defaultSkill.max_;
    this.primary_      = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "primary"        ) ?? defaultSkill.primary_;
    this.secondary_    = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "secondary"      ) ?? defaultSkill.secondary_;
    this.misc_         = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "misc"           ) ?? defaultSkill.misc_;
    this.maxsecondary_ = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "maxsecondary"   ) ?? defaultSkill.maxsecondary_;
    this.maxmisc_      = TreeAtributeExtractor.tryGetAsType< float >( skillAttributes, "maxmisc"        ) ?? defaultSkill.maxmisc_;

    ITreeAttribute factorTree = skillAttributes.GetTreeAttribute( "factor" );
    if ( factorTree != null )
    {
      this.factor_.readFromTree( factorTree, defaultSkill.factor_ );
    }

    ITreeAttribute factorTree = skillAttributes.GetTreeAttribute( "points" );
    if ( factorTree != null )

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

    skillTree.SetString( "skillname", skillname_ );

    skillTree.SetFloat( "exp",          exp_          );
    skillTree.SetFloat( "expprimary",   expprimary_   );
    skillTree.SetFloat( "expsecondary", expsecondary_ );
    skillTree.SetFloat( "expmisc",      expmisc_      );
    skillTree.SetFloat( "max",          max_          );
    skillTree.SetFloat( "primary",      primary_      );
    skillTree.SetFloat( "secondary",    secondary_    );
    skillTree.SetFloat( "misc",         misc_         );
    skillTree.SetFloat( "maxsecondary", maxsecondary_ );
    skillTree.SetFloat( "maxmisc",      maxmisc_      );
    
    if ( getFactor )
    {
      skillTree.SetAttribute( "factor", factor_.asTreeAttribute() );
    }

    return skillTree;

  }
  

}

}