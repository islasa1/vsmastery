using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;


[assembly: ModInfo( "vsmastery",
	Description = "TBD",
	Website     = "https://github.com/islasa1/vsmastery",
	Authors     = new []{ "islasa1" } )]

namespace vsmastery
{

/// <summary>
/// The core mod system for this mod
/// </summary>
public class VSMastery : Vintagestory.API.Common.ModSystem
{
  // Our client API
  Vintagestory.API.Client.ICoreClientAPI capi_;
  // Server API
  Vintagestory.API.Server.ICoreServerAPI sapi_;

  // The core API
  Vintagestory.API.Common.ICoreAPI       api_;

  // Start our core API
  public override void Start( ICoreAPI api )
  {
    api_ = api;

    api.RegisterEntityBehaviorClass( BehaviorSkills.BEHAVIOR, typeof( BehaviorSkills ) );

  }


  // For the client side
  public override void StartClientSide( ICoreClientAPI api )
  {
    capi_ = api;
  }

  // For the server side
  public override void StartServerSide( ICoreServerAPI api )
  {
    sapi_ = api;
  }


}


}