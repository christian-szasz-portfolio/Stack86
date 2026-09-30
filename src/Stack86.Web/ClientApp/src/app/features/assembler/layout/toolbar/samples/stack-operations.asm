; Stack push/pop demo
MOV AX, 0x1234
MOV BX, 0x5678

PUSH AX         ; push 1234h
PUSH BX         ; push 5678h

POP CX          ; CX = 5678h (last pushed)
POP DX          ; DX = 1234h (first pushed)

; CX and DX now have swapped values
HLT
