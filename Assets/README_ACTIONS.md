# Battle Simulator: Energy and Action System

## Overview
This update adds an energy meter and two new actions to the creatures:
1. Tree chopping
2. Creature attacking

## Energy System
- Each creature has an energy meter that ranges from 0.0 to 1.0
- Energy regenerates over time (takes 3 seconds to fill completely)
- Energy is consumed when performing actions (chopping or attacking)
- The default energy cost for an action is 1.0

## New Actions
The neural network now outputs 4 values:
1. Move X (horizontal movement)
2. Move Y (vertical movement)
3. Chop (tree interaction)
4. Attack (opposing creature interaction)

### Chopping Trees
- When the chop output is positive, and higher than the attack output, the creature will attempt to chop the nearest tree in its field of view
- Each chop deals 1.0 damage to the tree
- Trees have 5.0 health by default
- When a tree's health reaches 0, it is destroyed

### Attacking Creatures
- When the attack output is positive, and higher than the chop output, the creature will attempt to attack the nearest opposing creature in its field of view
- Each attack deals 1.0 damage to the opposing creature
- Creatures have 3.0 health by default
- When a creature's health reaches 0, it dies

## Implementation Details
- The `CreatureObserver` component now detects trees and includes them in the observations
- The `TreeHealth` component tracks tree health and handles damage
- The `TreeSetup` component automatically adds the `TreeHealth` component to all trees in the scene
- The `Creature` component now includes energy management and action execution logic
- The neural network's genome structure has been updated to handle the new inputs and outputs

## Tags Used
- "Tree" - For all tree objects
- "Cherry" - For health pickups (unchanged)

## Setup Instructions
1. Make sure all trees in your scene have names containing "tree" (case insensitive)
2. Add the `TreeSetup` component to a GameObject in your scene
3. Press Play - the trees will be automatically set up with the correct tag and components
4. Creatures will now use their energy to chop trees and attack other creatures based on their neural network outputs

Creatures will show a red flash when taking damage, and trees will also flash red when chopped.