## Lego Method

The lego method will create a random maze based on pre-created "Modules". These modules can be rooms, corridors, stairs, whatever shape you want. Each Module has a "ModuleConnector" object attached to any exit or entrance of that module. 
So for example, a corridor can have two module connectors since it has an entrance and an exit.

What the algorithm does is simply get all the lego pieces prefabs, put them in a list, and randomize them. The result would be a unique maze that has pre-created shapes, but in different forms. Kind of like Scrabble where a bunch of letters could have different combinations that would create completely different letters. 

If you want a module to show up more than one time, then add it twice to the dungeon modules list in the ModularWorldGenerator script.



## Pros

- The ability to have pre-scripted rooms with trigger boxes that would activate certain events in a random maze.

- Having mutliple or single start and exit modules that can have as much customizability as possible.



## Cons

- If the number of pre-created modules was not big enough. The dungeon could create duplicate modules which would look a bit redundant to the player.

- The developer needs to have collisions in mind by creating different modules of different shapes and sizes.