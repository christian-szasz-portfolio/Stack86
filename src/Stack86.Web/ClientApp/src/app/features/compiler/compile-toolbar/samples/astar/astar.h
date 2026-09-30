// astar.h — A* pathfinding types and declarations

#define ROWS 6
#define COLS 8
#define TOTAL 48
#define WALL 1
#define INF 9999

struct Node {
    int row;
    int col;
    int g;
    int f;
    int parent_row;
    int parent_col;
    int open;
    int closed;
};

int abs_val(int x);
int heuristic(int r1, int c1, int r2, int c2);
int astar(int* grid, int sr, int sc, int er, int ec);
