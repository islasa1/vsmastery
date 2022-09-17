using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace vsmastery
{
public class SkillFactor
{

  // Fields for doing the hard work
  public float min     { get { return min_;     } set { factorGenerator_ = null; min_ = value; } }
  public float max     { get { return max_;     } set { factorGenerator_ = null; max_ = value; } }
  public float avgcoef { get { return avgcoef_; } set { factorGenerator_ = null; avgcoef_ = value; } }
  public float varcoef { get { return varcoef_; } set { factorGenerator_ = null; varcoef_ = value; } }

  // Our base, just pulled up so we can do checks
  public float avg     { get { return avg_; }          set { factorGenerator_ = null; avg_ = value; } }
  public float var     { get { return var_; }          set { factorGenerator_ = null; var_ = value; } }
  public EnumDistribution dist { get { return dist_; } set { factorGenerator_ = null; dist_ = value; } }
  

  // Actual fields
  private float min_;
  private float max_;
  private float avgcoef_;
  private float varcoef_;

  private float avg_;
  private float var_;
  private EnumDistribution dist_;

  private NatFloat factorGenerator_ = null;




  public SkillFactor( ) { }

  public SkillFactor( ITreeAttribute factorTree, SkillFactor defaultSkillFactor )
  {
    this.readFromTree( factorTree, defaultSkillFactor );
  }

  public void readFromTree( ITreeAttribute factorTree, SkillFactor defaultSkillFactor )
  {
    this.min     = TreeAtributeExtractor.tryGetAsType< float >( factorTree, "min"     ) ?? defaultSkillFactor.min;
    this.max     = TreeAtributeExtractor.tryGetAsType< float >( factorTree, "max"     ) ?? defaultSkillFactor.max;
    this.avgcoef = TreeAtributeExtractor.tryGetAsType< float >( factorTree, "avgcoef" ) ?? defaultSkillFactor.avgcoef;
    this.varcoef = TreeAtributeExtractor.tryGetAsType< float >( factorTree, "varcoef" ) ?? defaultSkillFactor.varcoef;
    this.avg     = TreeAtributeExtractor.tryGetAsType< float >( factorTree, "avg"     ) ?? defaultSkillFactor.avg;
    this.var     = TreeAtributeExtractor.tryGetAsType< float >( factorTree, "var"     ) ?? defaultSkillFactor.var;
    // enum parsing
    EnumDistribution distType;
    bool parsed = System.Enum.TryParse( factorTree.GetString ( "dist" ), out distType );
    this.dist   = parsed ? distType : defaultSkillFactor.dist;
  }


  public float calc( float level )
  {
    // Construct the factor 
    if ( factorGenerator_ == null )
    {
      // I've looked at EvolvingNatFloat and it *almost* does what I want but not quite. I need
      // more control over the function than the built-in tranform functions. The single factor does
      // not buy me as much as the 2 coefficients  + clamps though it would not be too difficult to combine the two
      factorGenerator_ = new NatFloat(
                                      avg_ + level * avgcoef_, // The moving average
                                      ( var_ + level * varcoef_ ) * (float)System.Math.Sqrt( level ), // The changing variance with tapering
                                      dist_
                                      );
    }

    return factorGenerator_.nextFloat();
    
  }

  public TreeAttribute asTreeAttribute()
  {
    TreeAttribute skillFactor = new TreeAttribute();
    
    skillFactor.SetFloat ( "min",     min_ );
    skillFactor.SetFloat ( "max",     max_ );
    skillFactor.SetFloat ( "avgcoef", avgcoef_ );
    skillFactor.SetFloat ( "varcoef", varcoef_ );

    skillFactor.SetFloat ( "avg",     avg_ );
    skillFactor.SetFloat ( "var",     var_ );
    skillFactor.SetString( "dist",    dist_.ToString().ToLower() );

    return skillFactor;

  }

}

}