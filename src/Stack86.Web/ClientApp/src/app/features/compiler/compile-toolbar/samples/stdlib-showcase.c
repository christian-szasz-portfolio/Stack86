// Standard Library — malloc, string ops, ctype checks
// Demonstrates dynamic memory, string manipulation, and
// character classification using the C standard library.
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>

void print_char_info(char c) {
    putchar(c);
    printf(": ");
    if (isalpha(c))
        printf("alpha ");
    if (isdigit(c))
        printf("digit ");
    if (isupper(c))
        printf("upper ");
    if (islower(c))
        printf("lower ");
    if (ispunct(c))
        printf("punct ");
    if (isspace(c))
        printf("space ");
    printf("\n");
}

int main() {
    // --- Dynamic memory ---
    char* greeting = malloc(32);
    if (greeting == 0) {
        printf("malloc failed!\n");
        return 1;
    }

    // Build a string from parts
    strcpy(greeting, "Hello");
    strcat(greeting, ", ");
    strncat(greeting, "World!!!", 5);  // appends only "World"

    printf("%s\n", greeting);          // "Hello, World"
    printf("Length: %d\n", strlen(greeting));

    // --- Search within strings ---
    char* found = strchr(greeting, ',');
    if (found != 0)
        printf("Found comma at position %d\n", found - greeting);

    char* sub = strstr(greeting, "World");
    if (sub != 0)
        printf("Found 'World' at position %d\n", sub - greeting);

    // --- Character classification ---
    printf("\nCharacter info:\n");
    print_char_info('A');
    print_char_info('z');
    print_char_info('5');
    print_char_info('!');
    print_char_info(' ');

    // --- Case conversion ---
    printf("\nCase conversion:\n");
    printf("toupper('a') = %c\n", toupper('a'));
    printf("tolower('Z') = %c\n", tolower('Z'));

    // --- Integer conversion ---
    char buf[16];
    itoa(255, buf, 16);
    printf("\n255 in hex: %s\n", buf);

    int num = atoi("42");
    printf("atoi(\"42\") = %d\n", num);

    // --- String comparison ---
    printf("\nstrcmp(\"abc\", \"abc\") = %d\n", strcmp("abc", "abc"));
    printf("strcmp(\"abc\", \"abd\") = %d\n", strcmp("abc", "abd"));
    printf("strncmp(\"hello\", \"help\", 3) = %d\n", strncmp("hello", "help", 3));

    // --- Memory operations ---
    char* block = calloc(1, 8);
    memset(block, 'X', 4);
    printf("\ncalloc+memset: %c%c%c%c\n", block[0], block[1], block[2], block[3]);

    int cmp = memcmp(block, "XXXX", 4);
    printf("memcmp result: %d\n", cmp);

    // --- Cleanup ---
    free(greeting);
    free(block);

    printf("\nDone!\n");
    return 0;
}
