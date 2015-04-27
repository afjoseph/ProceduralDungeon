## Truly Random Method

The "Truly Random" method will dig out a "module" in the middle of the map (0,0,0), and then choose a random direction, and create yet another random "module". for the purposes of "DarkMaze", the modules are only rooms and corridors. Each room that the algorithm creates is a completely sealed module with no exits. Exits are created after the adjacent direction for the new module has been determined. I felt that this would bring a truly chaotic and random mazes because each module won't depend on one hardcoded exit.

The algorithm is split to two steps, creating the maze with CreateDungeon(), and building the maze with BuildDungeon(). CreateDungeon() simply maps out the tile pieces on the floor without physically creating anything. 
BuildDungeon() doesn't do any thinking. It simply takes each tile piece, and creates a wall, room, or corridor prefab based on the tile piece's data.



## Pros

- Truly random mazes that will never create the same maze twice (unless you ask really nicely :D )

- Mapping out each walkable space in the entire map with data, which can allow spawning enemies at random places, and even setting traps.



## Cons

- Everything is random. There is no way to make any scripted events happen without a lot of hassle and hardcoded codes. It would be easier to use the "Lego" method if the developer wants scripted rooms.