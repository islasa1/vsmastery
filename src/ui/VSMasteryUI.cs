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
  DlgComposers DlgComposersProp => dlg_.Composers;
  GuiComposer  skillsComposer = null;

  ICoreClientAPI capi_;
  
  // Baseline default skill 
  Skill defaultSkill_ = new Skill();



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

    // How often we update the info
    api.Event.RegisterGameTickListener( On2sTick, 2000 );


  }

  private void Dlg_TabClicked( int tabIndex )
  {
    // I believe the DataInt from vssurvivalmod/System/Character/Character.cs:StartClientSide()
    // Trait Tab creation is what determines this... not sure why you wouldn't use the list index
    if ( tabIndex != 1 ) Dlg_OnClosed();
    if ( tabIndex == 1 ) Dlg_OnOpened();
  }

  private void On2sTick( float dt )
  {
    if ( IsOpened() )
    {
      // I believe the watched attributes should take care of this
      // UpdateStatBars();
    }
  }

  private void Dlg_OnClosed()
  {
    // We no longer need to keep updating our stats bar
    capi_.World.Player.Entity.WatchedAttributes.UnregisterListener( UpdateStatBars );
  }

  private void Dlg_OnOpened()
  {
    // If this is our first time we should create the skills gui
    if ( skillsComposer == null )
    {
      composeSkillsGui();
    }

    // Watch our stats grow/shrink
    capi_.World.Player.Entity.WatchedAttributes.RegisterModifiedListener( "skills",   UpdateStatBars );
  }

  private void composeSkillsGui( )
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
    DlgComposersProp["playerskills"] = capi_.Gui.CreateCompo( "playerskills", dialogBounds )
                                            .AddShadedDialogBG ( bgBounds, true )
                                            .AddDialogTitleBar ( Lang.Get("Skills"), () => dlg_.OnTitleBarClose() )
                                            .BeginChildElements( bgBounds );

    ITreeAttribute vsmastery = capi_.World.Player.Entity.WatchedAttributes.GetTreeAttribute("vsmasteryskills");
    if ( vsmastery != null )
    {
      // Because we flattened to not include defaults anymore we go straight to categories
      foreach ( KeyValuePair< string, IAttribute > category in vsmastery )
      {
        // Note that we are modifying the bounds as we shift it down with a copy
        DlgComposersProp["playerskills"]
            // Add our title with a larger font
            .AddStaticText( Lang.Get( "vsmastery-" + category.Key ), CairoFont.WhiteSmallText().WithWeight( Cairo.FontWeight.Bold ), leftColumnBounds.WithFixedWidth(200));

        TreeArrayAttribute skillsInCategory = category.Value as TreeArrayAttribute;
        bool firstBar = true;

        // Get all our skills
        foreach ( IAttribute skillValue in skillsInCategory.value )
        {
          
          ITreeAttribute skill = skillValue as ITreeAttribute;

          DlgComposersProp["playerskills"]
            // Now add our stats with a smaller font
            .AddStaticText( Lang.Get( "vsmastery-" + skill.GetString( "skillname"  ) ), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy().WithFixedWidth(90))
            // Now add the bar, the copy on the y offset is probably a hard-coded value to adjust lining up with the text lines
            .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0, firstBar ? 16 : 12 ), GuiStyle.FoodBarColor, skill.GetString( "skillname"  ) );

            firstBar = false;

          // Move the lower bounds just under where we've made everything
          leftColumnBoundsLower = leftColumnBoundsLower.FixedUnder( leftColumnBounds, -5 );
        }
      }
    }

    DlgComposersProp["playerskills"].EndChildElements().Compose();
    UpdateStatBars();

  }

  private void UpdateStatBars()
  {
    GuiComposer compo = DlgComposersProp["playerskills"];
    if ( compo == null || !IsOpened() ) return;

    ITreeAttribute vsmastery = capi_.World.Player.Entity.WatchedAttributes.GetTreeAttribute("vsmasteryskills");
    if ( vsmastery != null )
    {
      // Because we flattened to not include defaults anymore we go straight to categories
      foreach ( KeyValuePair< string, IAttribute > category in vsmastery )
      {

        TreeArrayAttribute skillsInCategory = category.Value as TreeArrayAttribute;

        // Get all our skills
        foreach ( IAttribute skillValue in skillsInCategory.value )
        {

          ITreeAttribute skill = skillValue as ITreeAttribute;

          DlgComposersProp["playerskills"].GetStatbar( skill.GetString( "skillname" ) ).SetLineInterval( skill.GetFloat( "max" ) / 10 );
          DlgComposersProp["playerskills"].GetStatbar( skill.GetString( "skillname" ) ).SetValues      ( skill.GetFloat( "exp" ), 0, skill.GetFloat( "max" ) );

        }
      }
    }


  }

}

}