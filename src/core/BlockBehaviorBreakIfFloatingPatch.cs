using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace vsmastery
{

[HarmonyPatch( typeof( BlockBehaviorBreakIfFloating ), "GetDrops" ) ]
class BlockBehaviorBreakIfFloatingPatch
{

  public static string NOTIFY_KEY = "BlockBehaviorBreakIfFloating::GetDrops";

  [HarmonyPostfix]
  private static void PostFix( IWorldAccessor world, BlockPos pos, ItemStack[] __result )
  {
    // Only do postfix if we got a good recipe
    if ( __result != null )
    {
      // Nearest player gets the exp
      EntityAgent player = world.NearestPlayer( pos.X, pos.Y, pos.Z ).Entity as EntityAgent;

      if ( player != null )
      {
        // Use world accessor to 
        player.Notify( NOTIFY_KEY, __result[0] );
      }
    }
  }

}

}