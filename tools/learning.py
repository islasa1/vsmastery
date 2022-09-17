#!/bin/env python3

import numpy as np
import math
import sys, os


# Just do a hyperbolic
def experience( relativeHours ):
  return relativeHours / ( relativeHours + 1 )


# determine what your relative level is
def determineLevel( step, maxHours ):
  levels = [
              "Novice",
              "Apprentice",
              "Journeyperson",
              "Professional",
              "Expert",
              "Master",
              "Legendary"
              ]

  levelMax    = 1.0 / ( len( levels ) )
  lvlStr      = "{hours:4.4f} = {exp:4.4f} => {lvl:4.4f} [{title}]"


  steps = np.arange( 0, maxHours, step )
  for hours in steps :

    relativeExp = experience( hours / maxHours * 7 ) + hours / maxHours * 1.0 / 8.0
    level       = relativeExp / levelMax

    print( lvlStr.format( hours=hours, exp=relativeExp, lvl=level, title=levels[ int( np.floor( level ) ) ] ) )



def main():
  determineLevel( float( sys.argv[1] ), float( sys.argv[2] ) )

if __name__ == '__main__':
  main()