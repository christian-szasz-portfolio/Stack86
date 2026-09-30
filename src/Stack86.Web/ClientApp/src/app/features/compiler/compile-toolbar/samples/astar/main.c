// A* Pathfinding — multi-file example
// Finds the shortest path on a 6x8 grid with walls.
// Returns the path length (expected: 11).
#include <stdio.h>
#include "astar.h"

int main() {
    //  Grid layout (0 = open, 1 = wall):
    //
    //     0  1  2  3  4  5  6  7
    //  0 [S] .  .  #  .  .  .  .
    //  1  .  #  .  #  .  #  .  .
    //  2  .  #  .  .  .  #  .  .
    //  3  .  .  #  #  .  .  .  .
    //  4  .  .  .  #  .  #  #  .
    //  5  .  .  .  .  .  .  . [E]

    int grid[TOTAL];
    int i;

    // Clear grid
    for (i = 0; i < TOTAL; i = i + 1)
        grid[i] = 0;

    // Place walls
    grid[0 * COLS + 3] = WALL;
    grid[1 * COLS + 1] = WALL;
    grid[1 * COLS + 3] = WALL;
    grid[1 * COLS + 5] = WALL;
    grid[2 * COLS + 1] = WALL;
    grid[2 * COLS + 5] = WALL;
    grid[3 * COLS + 2] = WALL;
    grid[3 * COLS + 3] = WALL;
    grid[4 * COLS + 3] = WALL;
    grid[4 * COLS + 5] = WALL;
    grid[4 * COLS + 6] = WALL;

    // Find shortest path from (0,0) to (5,7)
    int path_len = astar(grid, 0, 0, 5, 7);

    printf("Path length: %d\n", path_len);
    return path_len;
}
