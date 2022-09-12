#!/bin/env python3

import numpy as np
import math
import sys, os
import matplotlib.pyplot as plt

# Just do a hyperbolic
def experience( relativeExp ):
  return relativeExp / ( relativeExp + 1 )


# determine what your relative level is
def determineLevel( relativeExp ):
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
  lvlStr      = "[{title}] {lvl:2.2f} {normExp:2.2f}% (actual : {relExp:2.2f}%)"

  # Hyperbolic experience function + fudge factor to get approx 1, use same function so we maintain shape
  normalizedExp = experience( relativeExp * 7 ) + ( experience( relativeExp * 10 ) * 1.0 / 8.0 )
  level         = normalizedExp / levelMax
  title         = levels[ int( np.floor( level ) ) ]
  # title         = lvlStr.format(
  #                               title=levels[ int( np.floor( level ) ) ],
  #                               relExp=relativeExp,
  #                               normExp=normalizedExp,
  #                               lvl=level
  #                               )

  return title, normalizedExp, level

class Skill( object ):
  def __init__( self ) :
    self.name_ = "Skill"
    self.min_ = None
    self.max_ = None

    self.avgcoef_ = 0
    self.varcoef_ = 0

    self.avg_     = 0
    self.var_     = 0

    self.dist_    = "normal"
  
  def calc( self, lvl, samples=1 ) :
    val = 0

    if self.dist_ == "normal" :
      val = np.random.normal( self.avg_ + lvl * self.avgcoef_, ( self.var_ + lvl * self.varcoef_ ) * np.sqrt( lvl ), samples )

    else :
      print( "Uknown distribution" )
      val = 0
    
    # Shift up for min
    if self.min_ is not None :
      val = np.maximum( self.min_, val )
      
    # Shift down for max
    if self.max_ is not None :
      val = np.minimum( self.max_, val )

    return val


# determine what your relative level is
def plotSkill( skill, samples, step ):

  skillSteps = np.arange( 0, 1, step )

  for lvl in skillSteps :

    xVal = np.full ( (samples), lvl )
    yVal = skill.calc( lvl, samples )
    plt.scatter( xVal, yVal, marker='o', s=0.1 ) # color=np.full( (samples), lvl * 255 )

  plt.show()

# determine what your relative level is
def plotSkillViaExp( skill, samples, step ):

  expStep = np.arange( 0, 1, step )
  normExp = np.zeros( expStep.shape[0] )
  titles  = [ "" ] * expStep.shape[0]

  for idx in range( expStep.shape[0] ) :
    relExp = expStep[idx]
    title, normalizedExp, level = determineLevel( relExp )

    normExp[idx] = normalizedExp
    titles [idx] = title

  region = [ True if t == "Novice" else False for t in titles ]
  region[ len( region ) - region[::-1].index( True ) ] = True
  # region.reverse()
  # idx    = len( region ) - region.index( True ) + 1
  # region[ idx ] = True
  # region.reverse()
  
  plt.fill_between( expStep, 0, skill.avg_ + skill.avgcoef_, where=region, alpha=0.1, facecolor="green" )

  region = [ True if t == "Apprentice" else False for t in titles ]
  region[ len( region ) - region[::-1].index( True ) ] = True
  # region.reverse()
  # idx    = len( region ) - region.index( True ) + 1
  # region[ idx ] = True
  # region.reverse()
  
  plt.fill_between( expStep, 0, skill.avg_ + skill.avgcoef_, where=region, alpha=0.1, facecolor="seagreen" )

  region = [ True if t == "Journeyperson" else False for t in titles ]
  region[ len( region ) - region[::-1].index( True ) ] = True
  # region.reverse()
  # idx    = len( region ) - region.index( True ) + 1
  # region[ idx ] = True
  # region.reverse()
  
  plt.fill_between( expStep, 0, skill.avg_ + skill.avgcoef_, where=region, alpha=0.1, facecolor="mediumturquoise" )

  region = [ True if t == "Professional" else False for t in titles ]
  region[ len( region ) - region[::-1].index( True ) ] = True
  # region.reverse()
  # idx    = len( region ) - region.index( True ) + 1
  # region[ idx ] = True
  # region.reverse()
  
  plt.fill_between( expStep, 0, skill.avg_ + skill.avgcoef_, where=region, alpha=0.1, facecolor="teal" )

  region = [ True if t == "Expert" else False for t in titles ]
  region[ len( region ) - region[::-1].index( True ) ] = True
  # region.reverse()
  # idx    = len( region ) - region.index( True ) + 1
  # region[ idx ] = True
  # region.reverse()
  
  plt.fill_between( expStep, 0, skill.avg_ + skill.avgcoef_, where=region, alpha=0.1, facecolor="darkturquoise" )

  region = [ True if t == "Master" else False for t in titles ]
  region[ len( region ) - region[::-1].index( True ) ] = True
  # region.reverse()
  # idx    = len( region ) - region.index( True ) + 1
  # region[ idx ] = True
  # region.reverse()
  
  plt.fill_between( expStep, 0, skill.avg_ + skill.avgcoef_, where=region, alpha=0.1, facecolor="steelblue" )

  region = [ True if t == "Legendary" else False for t in titles ]
  # region[ len( region ) - region[::-1].index( True ) ] = True
  # region.reverse()
  # idx    = len( region ) - region.index( True ) + 1
  # # region[ idx ] = True
  # region.reverse()
  
  plt.fill_between( expStep, 0, skill.avg_ + skill.avgcoef_, where=region, alpha=0.1, facecolor="royalblue" )

  for idx in range( expStep.shape[0] ) :
    relExp = expStep[idx]
    title, normalizedExp, level = determineLevel( relExp )

    xVal = np.full ( (samples), relExp )
    yVal = skill.calc( normalizedExp, samples )
    plt.scatter( xVal, yVal, marker='o', s=0.1 ) # color=np.full( (samples), lvl * 255 )
    plt.scatter( relExp, normalizedExp * ( skill.avg_ + skill.avgcoef_ ), marker='o', s=0.2, c="black" ) # color=np.full( (samples), lvl * 255 )

  
  plt.show()


def main():
  test = Skill()
  test.min_ = 0
  test.max_ = 20
  test.avg_ = 0
  test.var_ = 4
  test.avgcoef_ = 20
  test.varcoef_ = -3.9

  plotSkillViaExp( test, 250, 0.01 )
  # plotSkill( test, 250, 0.01 )

  # determineLevel( float( sys.argv[1] ), float( sys.argv[2] ) )

if __name__ == '__main__':
  main()