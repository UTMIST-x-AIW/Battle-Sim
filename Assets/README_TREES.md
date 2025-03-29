# Tree Spawning System

## Overview
This system allows trees to be spawned randomly across the map, with new trees automatically respawning after a delay when existing trees are chopped down.

## Setup Instructions

1. **Create a Tree Manager GameObject:**
   - Create an empty GameObject in your scene
   - Name it "TreeManager"
   - Add the `TreeManagerSetup` component to it
   - This will automatically add the `TreeManager` component

2. **Assign Tree Prefabs:**
   - In the TreeManager Inspector, assign:
     - `tree1Prefab`: Assign the slim tree prefab from Assets/Prefabs/tree1
     - `tree2Prefab`: Assign the wider tree prefab from Assets/Prefabs/tree2

3. **Configure Spawn Settings:**
   - `initialTreeCount`: Set the number of trees to spawn at the start (default: 10)
   - `respawnDelay`: Set the time in seconds before a new tree spawns after one is destroyed (default: 5)
   - `spawnArea`: Assign the floor's PolygonCollider2D for the spawn area (if left blank, it will try to find a GameObject with the "Floor" tag)

4. **Make Sure Trees Have the Right Tag:**
   - All tree prefabs should have the "Tree" tag
   - The system will automatically tag them if needed

## How It Works
- When the game starts, the TreeManager spawns the initial number of trees randomly within the spawn area
- When creatures chop down trees, the TreeManager is notified via the TreeHealth.OnTreeDestroyed event
- After the respawn delay, a new tree is spawned at a random location
- Trees are evenly spaced to avoid overlap (min 2 units apart)
- Both tree types (tree1 and tree2) are spawned with equal probability

## Important Components

### TreeManager
The main component that handles spawning trees and tracking their count.

### TreeHealth
Attached to each tree to manage health, damage, and destruction events.

### TreeManagerSetup
A simple helper component that just adds the TreeManager component when needed 