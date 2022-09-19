#region Assembly cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// cairo-sharp
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using static Vintagestory.API.Client.GuiDialog;

namespace vsmastery.ui
{

/// <summary>
/// This will be our main UI handler
/// </summary>
public class VSMasteryUI : ModSystem
{

  ICoreClientAPI capi_;
  
  // Baseline default skill 
  Skill defaultSkill_ = new Skill();

  GuiMastery guiDialog_;

  public override bool ShouldLoad( EnumAppSide forSide )
  {
    return forSide == EnumAppSide.Client;
  }

  public override void StartClientSide( ICoreClientAPI api )
  {
    
    capi_ = api;
    base.StartClientSide( api );
    
    capi_.Event.PlayerJoin += playerJoinedClient;

    capi_.Input.RegisterHotKey( VSMastery.MOD_ID, 
                                Lang.Get( VSMastery.MOD_ID + ":hotkey-desc" ),
                                GlKeys.K,
                                HotkeyType.GUIOrOtherControls
                              );

    capi_.Input.SetHotKeyHandler( VSMastery.MOD_ID, toggleGui );

  }

  private bool toggleGui( KeyCombination combo )
  {
    
    if ( guiDialog_.IsOpened() ) { guiDialog_.TryClose(); }
    else                         { guiDialog_.TryOpen(); }

    return true;

  }

  private void playerJoinedClient( IClientPlayer player )
  {
    // We are now ready to make our GUI
    guiDialog_ = new GuiMastery( capi_ );
  }

}

}