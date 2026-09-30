// New & Delete — dynamic memory allocation
#include <iostream>
using namespace std;

class Node {
public:
    int value;
    Node* next;

    Node(int v) : value(v), next(0) {}
};

int main() {
    Node* head = new Node(10);
    head->next = new Node(20);
    head->next->next = new Node(30);

    Node* current = head;
    while (current != 0) {
        cout << current->value << " ";
        current = current->next;
    }
    cout << endl;

    // Cleanup
    current = head;
    while (current != 0) {
        Node* temp = current;
        current = current->next;
        delete temp;
    }

    return 0;
}
