using HarmonyLib;
using Vintagestory.API.Common;

namespace vsmastery
{

[HarmonyPatch( typeof( GridRecipe ), "ConsumeInput" ) ]
class GridRecipePatch
{

  public static string NOTIFY_KEY = "GridRecipe::ConsumeInput";

  [HarmonyPostfix]
  private static void PostFixConsumeInput( IPlayer byPlayer, bool __result, GridRecipe __instance )
  {
    // Only do postfix if we got a good recipe
    if ( __result )
    {
      EntityAgent player = byPlayer?.Entity as EntityAgent;

      if ( player != null )
      {
        player.Notify( NOTIFY_KEY, __instance );
      }
    }
  }

}

}