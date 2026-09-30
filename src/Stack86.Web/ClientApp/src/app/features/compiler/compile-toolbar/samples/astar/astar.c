// astar.c — A* pathfinding implementation
#include "astar.h"

int abs_val(int x) {
    if (x < 0)
        return 0 - x;
    return x;
}

int heuristic(int r1, int c1, int r2, int c2) {
    return abs_val(r1 - r2) + abs_val(c1 - c2);
}

// Find the open node with the lowest f score.
// Returns its index in the nodes array, or -1 if none.
int pick_best(struct Node* nodes) {
    int best = -1;
    int best_f = INF;
    int i;
    for (i = 0; i < TOTAL; i = i + 1) {
        if (nodes[i].open == 1 && nodes[i].f < best_f) {
            best_f = nodes[i].f;
            best = i;
        }
    }
    return best;
}

// Trace back from the goal to the start and return the path length.
int trace_path(struct Node* nodes, int ei) {
    int length = 0;
    int ci = ei;
    while (ci != -1) {
        length = length + 1;
        int pr = nodes[ci].parent_row;
        int pc = nodes[ci].parent_col;
        if (pr == -1)
            ci = -1;
        else
            ci = pr * COLS + pc;
    }
    return length;
}

int astar(int* grid, int sr, int sc, int er, int ec) {
    struct Node nodes[TOTAL];
    int i;

    // Initialise every node
    for (i = 0; i < TOTAL; i = i + 1) {
        nodes[i].row = i / COLS;
        nodes[i].col = i - (i / COLS) * COLS;
        nodes[i].g = INF;
        nodes[i].f = INF;
        nodes[i].parent_row = -1;
        nodes[i].parent_col = -1;
        nodes[i].open = 0;
        nodes[i].closed = 0;
    }

    // Seed the start node
    int si = sr * COLS + sc;
    nodes[si].g = 0;
    nodes[si].f = heuristic(sr, sc, er, ec);
    nodes[si].open = 1;

    // Direction offsets: up, down, left, right
    int dr[4];
    int dc[4];
    dr[0] = -1; dc[0] =  0;
    dr[1] =  1; dc[1] =  0;
    dr[2] =  0; dc[2] = -1;
    dr[3] =  0; dc[3] =  1;

    while (1) {
        int ci = pick_best(nodes);
        if (ci == -1)
            return -1;

        int cr = nodes[ci].row;
        int cc = nodes[ci].col;

        // Reached the goal — trace back and return length
        if (cr == er && cc == ec)
            return trace_path(nodes, ci);

        // Move current from open to closed
        nodes[ci].open = 0;
        nodes[ci].closed = 1;

        // Explore four neighbours
        int d;
        for (d = 0; d < 4; d = d + 1) {
            int nr = cr + dr[d];
            int nc = cc + dc[d];

            // Bounds check
            if (nr < 0 || nr >= ROWS || nc < 0 || nc >= COLS)
                continue;

            int ni = nr * COLS + nc;

            // Skip walls and already-closed nodes
            if (grid[ni] == WALL || nodes[ni].closed == 1)
                continue;

            int tentative_g = nodes[ci].g + 1;
            if (tentative_g < nodes[ni].g) {
                nodes[ni].g = tentative_g;
                nodes[ni].f = tentative_g + heuristic(nr, nc, er, ec);
                nodes[ni].parent_row = cr;
                nodes[ni].parent_col = cc;
                nodes[ni].open = 1;
            }
        }
    }

    return -1;
}
