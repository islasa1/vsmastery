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

  Vintagestory.API.Client.GuiDialogCharacterBase dlg_;
  Vintagestory.API.Client.GuiDialog.DlgComposers DlgComposersProp => dlg_.Composers;
  ICoreClientAPI capi_;
  

  public event System.Action<StringBuilder> OnEnvText;

  public override bool ShouldLoad( EnumAppSide forSide )
  {
    return forSide == EnumAppSide.Client;
  }

  bool IsOpened()
  {
    return dlg_.IsOpened();
  }

  public override void StartClientSide( ICoreClientAPI api )
  {
    capi_ = api;
    base.StartClientSide( api );

    dlg_ = api.Gui.LoadedGuis.Find( dlg_ => dlg_ is GuiDialogCharacterBase ) as GuiDialogCharacterBase;

    // Carry over the methods
    dlg_.OnOpened         += Dlg_OnOpened;
    dlg_.OnClosed         += Dlg_OnClosed;
    dlg_.TabClicked       += Dlg_TabClicked;
    dlg_.ComposeExtraGuis += Dlg_ComposeExtraGuis;

    
    api.Event.RegisterGameTickListener( On2sTick, 2000 );
  }

  private void Dlg_TabClicked( int tabIndex )
  {
    if ( tabIndex != 0 ) Dlg_OnClosed();
    if ( tabIndex == 0 ) Dlg_OnOpened();
  }

  private void Dlg_ComposeExtraGuis()
  {
    ComposeEnvGui();
    ComposeStatsGui();
  }

  private void On2sTick( float dt )
  {
    if ( IsOpened() )
    {
      updateEnvText();
    }
  }

  private void Dlg_OnClosed()
  {
    // We no longer need to keep updating our stats bar
    capi_.World.Player.Entity.WatchedAttributes.UnregisterListener( UpdateStatBars );
    capi_.World.Player.Entity.WatchedAttributes.UnregisterListener( UpdateStats );
  }

  private void Dlg_OnOpened()
  {
    // Watch our stats grow/shrink
    capi_.World.Player.Entity.WatchedAttributes.RegisterModifiedListener( "hunger",   UpdateStatBars );
    capi_.World.Player.Entity.WatchedAttributes.RegisterModifiedListener( "stats",    UpdateStats );
    capi_.World.Player.Entity.WatchedAttributes.RegisterModifiedListener( "bodyTemp", UpdateStats );
  }

  public virtual void ComposeEnvGui()
  {
    ElementBounds leftDlgBounds = DlgComposersProp["playercharacter"].Bounds;
    CairoFont     font          = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2);
    
    string envText  = getEnvText();
    int    cntlines = 1 + Regex.Matches(envText, "\n").Count;
    double height   = font.GetFontExtents().Height * font.LineHeightMultiplier * cntlines / RuntimeEnv.GUIScale;

    ElementBounds textBounds = ElementBounds.Fixed(0, 25, (int)(leftDlgBounds.InnerWidth / RuntimeEnv.GUIScale - 40), height);
    textBounds.Name = "textbounds";

    ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
    bgBounds.Name = "bgbounds";
    bgBounds.BothSizing = ElementSizing.FitToChildren;
    bgBounds.WithChildren(textBounds);

    ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.None).WithFixedPosition(leftDlgBounds.renderX / RuntimeEnv.GUIScale, leftDlgBounds.renderY / RuntimeEnv.GUIScale + leftDlgBounds.OuterHeight / RuntimeEnv.GUIScale + 10);
    dialogBounds.Name = "dialogbounds";

    DlgComposersProp["environment"] = capi_.Gui
        .CreateCompo("environment", dialogBounds)
        .AddShadedDialogBG(bgBounds, true)
        .AddDialogTitleBar(Lang.Get("Environment"), () => dlg_.OnTitleBarClose())
        .BeginChildElements(bgBounds)
            .AddDynamicText(envText, font, textBounds, "dyntext")
        .EndChildElements()
        .Compose();
  }

  void updateEnvText()
  {
    if (!IsOpened() || DlgComposersProp?["environment"] == null) return;

    DlgComposersProp["environment"].GetDynamicText("dyntext").SetNewTextAsync(getEnvText());
  }

  string getEnvText()
  {
    string date = capi_.World.Calendar.PrettyDate();
    var conds = capi_.World.BlockAccessor.GetClimateAt(capi_.World.Player.Entity.Pos.AsBlockPos, EnumGetClimateMode.NowValues);
    string temp = "?";
    string rainfallfreq = "?";

    if (conds != null)
    {
      temp = (int)conds.Temperature + "°C";
      rainfallfreq = Lang.Get("freq-veryrare");

      if (conds.WorldgenRainfall > 0.9)
      {
        rainfallfreq = Lang.Get("freq-allthetime");
      }
      else if (conds.WorldgenRainfall > 0.7)
      {
        rainfallfreq = Lang.Get("freq-verycommon");
      }
      else if (conds.WorldgenRainfall > 0.45)
      {
        rainfallfreq = Lang.Get("freq-common");
      }
      else if (conds.WorldgenRainfall > 0.3)
      {
        rainfallfreq = Lang.Get("freq-uncommon");
      }
      else if (conds.WorldgenRainfall > 0.15)
      {
        rainfallfreq = Lang.Get("freq-rarely");
      }
    }
    

    StringBuilder sb = new StringBuilder();
    sb.Append(Lang.Get("character-envtext", date, temp, rainfallfreq));

    OnEnvText?.Invoke(sb);

    return sb.ToString();
  }

  public virtual void ComposeStatsGui()
  {
    // Make a copy of the bounds so we can move inside this as we add stuff
    ElementBounds leftDlgBounds    = DlgComposersProp["playercharacter"].Bounds;
    ElementBounds envDlgBounds     = DlgComposersProp["environment"].Bounds;

    // Where our info will reside
    ElementBounds leftColumnBounds  = ElementBounds.Fixed(0, 25, 90, 20);
    ElementBounds rightColumnBounds = ElementBounds.Fixed(120, 30, 120, 8);

    // Where more stats will live, under the stats above if they exist
    ElementBounds leftColumnBoundsLower  = ElementBounds.Fixed(0, 0, 140, 20);
    ElementBounds rightColumnBoundsLower = ElementBounds.Fixed(165, 0, 120, 20);

    // Get our current player for this client instance
    EntityPlayer entity = capi_.World.Player.Entity;

    // How much space to put between our boxes
    double spaceBorder = 10;

    // This might be the border?
    double b = envDlgBounds.InnerHeight / RuntimeEnv.GUIScale + spaceBorder;

    // Construct the background, everything will reside within this box
    ElementBounds bgBounds = ElementBounds.Fixed( 0, 0, 
                                                  130 + 100 + 5, // ffs don't put random numbers without noting why they are what they are
                                                  leftDlgBounds.InnerHeight / RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20 + b )
                                                .WithFixedPadding( GuiStyle.ElementToDialogPadding );


    ElementBounds dialogBounds = bgBounds.ForkBoundingParent()
                                         .WithAlignment( EnumDialogArea.LeftMiddle )
                                         .WithFixedAlignmentOffset( ( leftDlgBounds.renderX + leftDlgBounds.OuterWidth + spaceBorder ) / RuntimeEnv.GUIScale, b / 2 );


    ////////////////////////////////////////////////////////////////////////////
    //
    // Start to get our values
    float? health        = null;
    float? maxhealth     = null;
    float? saturation    = null;
    float? maxsaturation = null;
    getHealthSat( 
                  out health, 
                  out maxhealth,
                  out saturation,
                  out maxsaturation );

    float walkspeed           = entity.Stats.GetBlended("walkspeed");
    float healingEffectivness = entity.Stats.GetBlended("healingeffectivness");
    float hungerRate          = entity.Stats.GetBlended("hungerrate");
    float rangedWeaponAcc     = entity.Stats.GetBlended("rangedWeaponsAcc");
    float rangedWeaponSpeed   = entity.Stats.GetBlended("rangedWeaponsSpeed");
    ITreeAttribute tempTree   = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");

    float wetness = entity.WatchedAttributes.GetFloat("wetness");
    string wetnessString = "";

    if      ( wetness > 0.7 ) wetnessString = Lang.Get("wetness_soakingwet");
    else if ( wetness > 0.4 ) wetnessString = Lang.Get("wetness_wet");
    else if ( wetness > 0.1 ) wetnessString = Lang.Get("wetness_slightlywet");


    DlgComposersProp["playerstats"] = capi_.Gui.CreateCompo( "playerstats", dialogBounds )
                                        .AddShadedDialogBG ( bgBounds, true )
                                        .AddDialogTitleBar ( Lang.Get("Stats"), () => dlg_.OnTitleBarClose() )
                                        .BeginChildElements( bgBounds );

    
    if ( saturation != null )
    {
      // Note that we are modifying the bounds as we shift it down with a copy
      DlgComposersProp["playerstats"]
          // Add our title with a larger font
          .AddStaticText( Lang.Get( "playerinfo-nutrition"         ), CairoFont.WhiteSmallText().WithWeight( Cairo.FontWeight.Bold ), leftColumnBounds.WithFixedWidth(200))
          // Now add our stats with a smaller font
          .AddStaticText( Lang.Get( "playerinfo-nutrition-Freeza"  ), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy().WithFixedWidth(90))
          .AddStaticText( Lang.Get( "playerinfo-nutrition-Vegita"  ), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
          .AddStaticText( Lang.Get( "playerinfo-nutrition-Krillin" ), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
          .AddStaticText( Lang.Get( "playerinfo-nutrition-Cell"    ), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
          .AddStaticText( Lang.Get( "playerinfo-nutrition-Dairy"   ), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy())
          // Now add the bar, the copy on the y offset is probably a hard-coded value to adjust lining up with the text lines
          .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0, 16), GuiStyle.FoodBarColor, "fruitBar")
          .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0, 12), GuiStyle.FoodBarColor, "vegetableBar")
          .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0, 12), GuiStyle.FoodBarColor, "grainBar")
          .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0, 12), GuiStyle.FoodBarColor, "proteinBar")
          .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0, 12), GuiStyle.FoodBarColor, "dairyBar");

      // Move the lower bounds just under where we've made everything
      leftColumnBoundsLower = leftColumnBoundsLower.FixedUnder(leftColumnBounds, -5);
    }

    DlgComposersProp["playerstats"]
            .AddStaticText(Lang.Get("Physical"), CairoFont.WhiteSmallText().WithWeight(Cairo.FontWeight.Bold), leftColumnBoundsLower.WithFixedWidth(200).WithFixedOffset(0, 23))
            .Execute(() => {
                leftColumnBoundsLower = leftColumnBoundsLower.FlatCopy();
                leftColumnBoundsLower.fixedY += 5;
            });

    if (health != null)
    {
      DlgComposersProp["playerstats"]
        .AddStaticText ( Lang.Get("Health Points"),  CairoFont.WhiteDetailText(), leftColumnBoundsLower  = leftColumnBoundsLower.BelowCopy() )
        .AddDynamicText( health + " / " + maxhealth, CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY).WithFixedHeight(30), "health");
    }

    if (saturation != null) 
    {
      DlgComposersProp["playerstats"]
        .AddStaticText(Lang.Get("Satiety"), CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
        .AddDynamicText((int)saturation + " / " + (int)maxsaturation, CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY), "satiety");
    }

    if (tempTree != null)
    {
      DlgComposersProp["playerstats"]
          .AddStaticText(Lang.Get("Body Temperature"), CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
          .AddRichtext(tempTree == null ? "-" : getBodyTempText(tempTree), CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY), "bodytemp");
    }

    if (wetnessString.Length > 0)
    {
        DlgComposersProp["playerstats"]
            .AddRichtext(wetnessString, CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
        ;
    }

    DlgComposersProp["playerstats"]
        .AddStaticText(Lang.Get("Walk speed"), CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
        .AddDynamicText((int)Math.Round(100 * walkspeed) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY), "walkspeed")

        .AddStaticText(Lang.Get("Healing effectivness"), CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
        .AddDynamicText((int)Math.Round(100 * healingEffectivness) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY), "healeffectiveness")
    ;

    if (saturation != null) 
    { 
        DlgComposersProp["playerstats"]
            .AddStaticText(Lang.Get("Hunger rate"), CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
            .AddDynamicText((int)Math.Round(100 * hungerRate) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY), "hungerrate")
        ;
    }

    DlgComposersProp["playerstats"]
        .AddStaticText(Lang.Get("Ranged Accuracy"), CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
        .AddDynamicText((int)Math.Round(100 * rangedWeaponAcc) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY), "rangedweaponacc")

        .AddStaticText(Lang.Get("Ranged Charge Speed"), CairoFont.WhiteDetailText(), leftColumnBoundsLower = leftColumnBoundsLower.BelowCopy())
        .AddDynamicText((int)Math.Round(100 * rangedWeaponSpeed) + "%", CairoFont.WhiteDetailText(), rightColumnBoundsLower = rightColumnBoundsLower.FlatCopy().WithFixedPosition(rightColumnBoundsLower.fixedX, leftColumnBoundsLower.fixedY), "rangedweaponchargespeed")

        .EndChildElements()
        .Compose()
    ;

    UpdateStatBars();
  }


  string getBodyTempText(ITreeAttribute tempTree)
  {
    float baseTemp = tempTree.GetFloat("bodytemp");
    // Prevent it displaying values greater than around 38 degrees: warm from fire temperatures shown as e.g. 37.3 not 40
    if ( baseTemp > 37f ) baseTemp = 37f + ( baseTemp - 37f ) / 10f;
    
    return string.Format("{0:0.#}°C", baseTemp);
  }

  void getHealthSat(out float? health, out float? maxHealth, out float? saturation, out float? maxSaturation)
  {
      health = null;
      maxHealth = null;
      saturation = null;
      maxSaturation = null;

      ITreeAttribute healthTree = capi_.World.Player.Entity.WatchedAttributes.GetTreeAttribute("health");
      if (healthTree != null)
      {
          health = healthTree.TryGetFloat("currenthealth");
          maxHealth = healthTree.TryGetFloat("maxhealth");
      }

      if (health != null) health = (float)Math.Round((float)health, 1);
      if (maxHealth != null) maxHealth = (float)Math.Round((float)maxHealth, 1);

      ITreeAttribute hungerTree = capi_.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
      if (hungerTree != null)
      {
          saturation = hungerTree.TryGetFloat("currentsaturation");
          maxSaturation = hungerTree.TryGetFloat("maxsaturation");
      }
      if (saturation != null) saturation = (int)saturation;
  }

  private void UpdateStats()
  {
      EntityPlayer entity = capi_.World.Player.Entity;
      GuiComposer compo = DlgComposersProp["playerstats"];
      if (compo == null || !IsOpened()) return;

      float? health;
      float? maxhealth;
      float? saturation;
      float? maxsaturation;
      getHealthSat(out health, out maxhealth, out saturation, out maxsaturation);

      float walkspeed = entity.Stats.GetBlended("walkspeed");
      float healingEffectivness = entity.Stats.GetBlended("healingeffectivness");
      float hungerRate = entity.Stats.GetBlended("hungerrate");
      float rangedWeaponAcc = entity.Stats.GetBlended("rangedWeaponsAcc");
      float rangedWeaponSpeed = entity.Stats.GetBlended("rangedWeaponsSpeed");


      if (health != null) compo.GetDynamicText("health").SetNewText((health + " / " + maxhealth));
      if (saturation != null) compo.GetDynamicText("satiety").SetNewText((int)saturation + " / " + (int)maxsaturation);

      compo.GetDynamicText("walkspeed").SetNewText((int)Math.Round(100 * walkspeed) + "%");
      compo.GetDynamicText("healeffectiveness").SetNewText((int)Math.Round(100 * healingEffectivness) + "%");
      compo.GetDynamicText("hungerrate")?.SetNewText((int)Math.Round(100 * hungerRate) + "%");
      compo.GetDynamicText("rangedweaponacc").SetNewText((int)Math.Round(100 * rangedWeaponAcc) + "%");
      compo.GetDynamicText("rangedweaponchargespeed").SetNewText((int)Math.Round(100 * rangedWeaponSpeed) + "%");

      ITreeAttribute tempTree = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
      compo.GetRichtext("bodytemp").SetNewText(getBodyTempText(tempTree), CairoFont.WhiteDetailText());
  }

  private void UpdateStatBars()
  {
      GuiComposer compo = DlgComposersProp["playerstats"];
      if (compo == null || !IsOpened()) return;

      ITreeAttribute hungerTree = capi_.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");

      if (hungerTree != null)
      {
          float saturation = hungerTree.GetFloat("currentsaturation");
          float maxSaturation = hungerTree.GetFloat("maxsaturation");
          float fruitLevel = hungerTree.GetFloat("fruitLevel");
          float vegetableLevel = hungerTree.GetFloat("vegetableLevel");
          float grainLevel = hungerTree.GetFloat("grainLevel");
          float proteinLevel = hungerTree.GetFloat("proteinLevel");
          float dairyLevel = hungerTree.GetFloat("dairyLevel");

          compo.GetDynamicText("satiety").SetNewText((int)saturation + " / " + maxSaturation);

          DlgComposersProp["playerstats"].GetStatbar("fruitBar").SetLineInterval(maxSaturation / 10);
          DlgComposersProp["playerstats"].GetStatbar("vegetableBar").SetLineInterval(maxSaturation / 10);
          DlgComposersProp["playerstats"].GetStatbar("grainBar").SetLineInterval(maxSaturation / 10);
          DlgComposersProp["playerstats"].GetStatbar("proteinBar").SetLineInterval(maxSaturation / 10);
          DlgComposersProp["playerstats"].GetStatbar("dairyBar").SetLineInterval(maxSaturation / 10);


          DlgComposersProp["playerstats"].GetStatbar("fruitBar").SetValues(fruitLevel, 0, maxSaturation);
          DlgComposersProp["playerstats"].GetStatbar("vegetableBar").SetValues(vegetableLevel, 0, maxSaturation);
          DlgComposersProp["playerstats"].GetStatbar("grainBar").SetValues(grainLevel, 0, maxSaturation);
          DlgComposersProp["playerstats"].GetStatbar("proteinBar").SetValues(proteinLevel, 0, maxSaturation);
          DlgComposersProp["playerstats"].GetStatbar("dairyBar").SetValues(dairyLevel, 0, maxSaturation);
      }


  }
}

}