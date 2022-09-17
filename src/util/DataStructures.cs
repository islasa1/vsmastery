using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;


namespace vsmastery
{

public class TreeAtributeExtractor
{

  public static Nullable< T > tryGetAsType< T >( ITreeAttribute tree, string key ) where T : struct
  {

    // See if the key exists
    if ( !tree.HasAttribute( key ) ) { return null; }

    // We know we have the key
    IAttribute value = tree[ key ];

    // Try to convert to type
    try
    {
      // No way around this
      return ( Nullable< T > )Convert.ChangeType( value.GetValue(), typeof( T ) );
    }
    catch ( System.InvalidCastException )
    {
      return null;
    }   

  }

}


}