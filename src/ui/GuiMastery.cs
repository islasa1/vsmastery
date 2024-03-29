using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace vsmastery
{

public class GuiMastery : GuiDialog
{

  public override string ToggleKeyCombinationCode => VSMastery.MOD_ID;
  
  // Baseline default skill 
  Skill defaultSkill_ = new Skill();

  string  selectedSkill      = null;
  string SKILL_DESC          = "skilldesc";
  string SKILL_DESC_SUFFIX   = "-desc";


  public GuiMastery( ICoreClientAPI capi ) : base( capi )
  {
    this.OnClosed += onClosed;
    this.OnOpened += onOpened;
    
    composeSkillsGui();

    // How often we update the info
    // capi.Event.RegisterGameTickListener( On2sTick, 2000 );
  }

  
  // private void On2sTick( float dt )
  // {
  //   if ( IsOpened() )
  //   {
  //     // I believe the watched attributes should take care of this
  //     // UpdateStatBars();
  //   }
  // }

  private void onClosed()
  {
    // We no longer need to keep updating our stats bar
    capi.World.Player.Entity.WatchedAttributes.UnregisterListener( UpdateStatBars );
  }

  private void onOpened()
  {
    // Refresh our dynamic text
    clearSkillDesc();

    // Refresh our stats
    UpdateStatBars();

    // Watch our stats grow/shrink
    capi.World.Player.Entity.WatchedAttributes.RegisterModifiedListener( BehaviorSkills.BEHAVIOR, UpdateStatBars );
  }

  private void composeSkillsGui( )
  {

    ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment( EnumDialogArea.CenterMiddle );
    
    // Where our main text will reside
    ElementBounds leftSide  = ElementBounds.Fixed( 0, 15, 250, 400 )
                                           .WithAlignment( EnumDialogArea.LeftTop );

    // Where our skills will reside, make it slightly thinner
    ElementBounds rightSide = leftSide.RightCopy( 0, 0, -25, 0 ).WithFixedAlignmentOffset( 10, 0 );

    // Background boundaries
    // Construct the background, everything will reside within this box
    ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding( GuiStyle.ElementToDialogPadding );
    bgBounds.BothSizing    = ElementSizing.FitToChildren;
    bgBounds.WithChildren( leftSide, rightSide );

    // Where our info will reside
    ElementBounds leftColumnBounds  = ElementBounds.Fixed(   0, 25,  90, 20 );
    ElementBounds rightColumnBounds = ElementBounds.Fixed( 120, 30, 120,  8 );


    Composers[ "main" ] = capi.Gui.CreateCompo( VSMastery.MOD_ID, dialogBounds )
                              .AddShadedDialogBG( bgBounds )
                              .AddDialogTitleBar( Lang.Get( VSMastery.MOD_ID + ":main-title"   ), () => TryClose() )
                              .AddDynamicText   ( Lang.Get( VSMastery.MOD_ID + ":main-desc"    ), CairoFont.WhiteDetailText(), leftSide, SKILL_DESC )
                              .AddStaticText    ( Lang.Get( VSMastery.MOD_ID + ":skills-title" ), CairoFont.WhiteDetailText()
                                                                                                           .WithSlant( Cairo.FontSlant.Oblique )
                                                                                                           .WithWeight( Cairo.FontWeight.Bold )
                                                                                                           .WithLineHeightMultiplier( 1.05 ), rightSide );


    ////////////////////////////////////////////////////////////////////////////
    //
    // Start to get our values
    //
    ITreeAttribute vsmastery = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute( BehaviorSkills.BEHAVIOR );

    if ( vsmastery != null )
    {
      // Because we flattened to not include defaults anymore we go straight to categories
      foreach ( KeyValuePair< string, IAttribute > category in vsmastery )
      {
        // Note that we are modifying the bounds as we shift it down with a copy
        // Make a copy since we've added it as a child to the bg
        Composers["main"].BeginChildElements( rightSide.FlatCopy() )
            // Add our title with a larger font
            .AddStaticText( Lang.Get( VSMastery.MOD_ID + ":categ-" + category.Key ), 
                            CairoFont.WhiteDetailText().WithWeight( Cairo.FontWeight.Bold ), 
                            leftColumnBounds.WithFixedWidth(180)
                            );

        
        bool firstBar = true;

        // Get all our skills
        foreach ( KeyValuePair< string, IAttribute > skillValue in category.Value as ITreeAttribute )
        {
          
          ITreeAttribute skill = skillValue.Value as ITreeAttribute;
          string skillDesc = Lang.Get( VSMastery.MOD_ID + ":skill-" + skillValue.Key + SKILL_DESC_SUFFIX );

          Composers["main"]
            // Now add our stats with a smaller font
            .AddButton( 
                        Lang.Get( VSMastery.MOD_ID + ":skill-" + skillValue.Key ),
                        () => 
                          { 
                            if ( selectedSkill == null || selectedSkill != skillValue.Key )
                            { 
                              Composers["main"].GetDynamicText( SKILL_DESC ).SetNewText( skillDesc );
                              selectedSkill = skillValue.Key;
                            }
                            else
                            {
                              // Revert
                              clearSkillDesc();
                            }
                            return true; 
                          },
                        leftColumnBounds = leftColumnBounds.BelowCopy( 0, firstBar ? 4 : 0 ).WithFixedWidth(90), 
                        CairoFont.WhiteDetailText().WithLineHeightMultiplier( 0.9 ), EnumButtonStyle.MainMenu, 
                        EnumTextOrientation.Left, 
                        skillValue.Key + SKILL_DESC_SUFFIX
                        )
            // Now add the bar, the copy on the y offset is probably a hard-coded value to adjust lining up with the text lines
            .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0, firstBar ? 16 : 12 ), GuiStyle.FoodBarColor, skillValue.Key );
          
          // This isn't doing what I quite expect... It only does right and not right + Y level
          ElementBounds switchBounds = ElementBounds.Fixed( 0, rightColumnBounds.fixedY, 12, 12 ).FixedLeftOf( rightColumnBounds );

          // Now add our switch
          Composers["main"]
            .AddSwitch ( 
                        ( bool isOn ) => 
                          { 
                            if ( isOn )
                            { 
                              System.Console.WriteLine( VSMastery.MODLOG + "Activating skill " + skillValue.Key );
                            }
                            else
                            {
                              System.Console.WriteLine( VSMastery.MODLOG + "De-activating skill " + skillValue.Key );
                            }
                          }, 
                        switchBounds,
                        skillValue.Key + "-toggle", 10, 2
                      );

          // Turn on the skill - TBD replace with the previous state of the attribute
          Composers[ "main" ].GetSwitch( skillValue.Key + "-toggle" ).SetValue( true );

          firstBar = false;
        }      

        // Move down
        leftColumnBounds  = leftColumnBounds .BelowCopy( 0, 16 );
        rightColumnBounds = rightColumnBounds.BelowCopy( 0, 16 + 12 );
      }
    }

    Composers["main"].EndChildElements().Compose();
    UpdateStatBars();

  }

  private void UpdateStatBars()
  {
    GuiComposer compo = Composers["main"];
    if ( compo == null ) return;

    ITreeAttribute vsmastery = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute( BehaviorSkills.BEHAVIOR );
    if ( vsmastery != null )
    {
      // Because we flattened to not include defaults anymore we go straight to categories
      foreach ( KeyValuePair< string, IAttribute > category in vsmastery )
      {

        // Get all our skills
        foreach ( KeyValuePair< string, IAttribute > skillValue in category.Value as TreeAttribute )
        {

          ITreeAttribute skill = skillValue.Value as ITreeAttribute;
          float max = skill.GetFloat( "max" );
          float exp = skill.GetFloat( "exp" ) + skill.GetFloat( "expprimary" ) + skill.GetFloat( "expsecondary" ) + skill.GetFloat( "expmisc" );
          Composers["main"].GetStatbar( skillValue.Key ).SetLineInterval( max / 10 );
          Composers["main"].GetStatbar( skillValue.Key ).SetValues      ( 
                                                                          System.Math.Min(
                                                                                          max,
                                                                                          exp
                                                                                          ),
                                                                          0,
                                                                          max
                                                                          );
        }
      }
    }

  }

  private void clearSkillDesc()
  {
    selectedSkill = null;
    Composers["main"].GetDynamicText( SKILL_DESC ).SetNewText( Lang.Get( VSMastery.MOD_ID + ":main-desc" ) );
  }

}

}