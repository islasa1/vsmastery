using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace vsmastery
{
public class SkillEvent
{

  public enum Event
  {
    BREAK,
    INTERACT,
    ATTACK,
    KILL,
    FORGE,
    CRAFT

  }

  public static readonly Event[] ALL_EVENTS = new Event[] { 
                                                            Event.BREAK,
                                                            Event.INTERACT,
                                                            Event.ATTACK,
                                                            Event.KILL,
                                                            Event.FORGE,
                                                            Event.CRAFT
                                                            };


  // Actual fields
  public Skill.SkillPoint pointType_;
  public Event            event_;
  public string           toolname_ = null; // for when things don't line up
  public EnumTool?        tool_ = null;
  public List< string >   valid_;


  public SkillEvent( ) { }

  public SkillEvent( ITreeAttribute eventTree, SkillEvent defaultSkillEvent )
  {
    this.readFromTree( eventTree, defaultSkillEvent );
  }

  public void readFromTree( ITreeAttribute eventTree, SkillEvent defaultSkillEvent )
  {
    // enum parsing
    Skill.SkillPoint pointType;
    bool parsed = System.Enum.TryParse( eventTree.GetString ( "pointType" ), out pointType );
    pointType_  = parsed ? pointType : defaultSkillEvent.pointType_;

    // enum parsing
    Event eventType;
    parsed = System.Enum.TryParse( eventTree.GetString ( "event" ), out eventType );
    event_ = parsed ? eventType : defaultSkillEvent.event_;

    // enum parsing
    EnumTool toolType;
    toolname_ = eventTree.GetString ( "tool" );
    parsed = System.Enum.TryParse( toolname_, out toolType );
    tool_  = parsed ? toolType : defaultSkillEvent.tool_;
    

    valid_     = ( eventTree[ "valid" ] as StringArrayAttribute )?.value.ToList() ?? defaultSkillEvent.valid_;

  }

  public bool eventTriggered( EnumTool? tool, string toolname, object data )
  {
    // Check tool if required
    if ( tool_ != null && tool_ != tool )
    {
      return false;
    }
    // Check if we have a specific toolname not part of the enum
    else if ( toolname_ != null && toolname_ != toolname )
    {
      return false;
    }

    // if ( tool != null )
    // {
      
      // switch ( tool )
      // {
      //   case EnumTool.Knife    : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Pickaxe  : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Axe      : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Sword    : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Shovel   : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Hammer   : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Spear    : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Bow      : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Shears   : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Sickle   : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Hoe      : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Saw      : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Chisel   : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Scythe   : 
      //   {
      //     break;
      //   }
      //   case EnumTool.Sling    : 
      //   {
      //     break;
      //   }
      // }
    // }

    // No tool requirements or requirements met

    // Requires object data be valid
    if ( valid_.Count > 0 )
    {
      // Each type of event will expect different types of data
      switch ( event_ )
      {
        case Event.BREAK     :
        {
          // Object data is an item stack of what was broken
          ItemStack drops = data as ItemStack;
          if ( drops == null )
          {
            return false;
          }

          // Check if drops match valid
          foreach( string asset in valid_ )
          {
            if ( WildcardUtil.Match( new AssetLocation( asset ), drops.Collectible.Code ) )
            {
              // Found to match, valid exp
              return true;
            }
          }
          
          break;
        }
        case Event.INTERACT  :
        {
          break;
        }
        case Event.ATTACK    :
        {
          break;
        }
        case Event.KILL      :
        {
          break;
        }
        case Event.FORGE     :
        {
          break;
        }
        case Event.CRAFT     :
        {
          break;
        }
        default : return false;
      }
      
      // If we have not returned a validation by this point, valid was not handled
      return false;

    }

    // We have satisfied the requirements for this event
    return true;

  }

}

}