; Count from 1 to 5
MOV CX, 5       ; loop counter
MOV AX, 0       ; accumulator

loop_start:
  INC AX
  DEC CX
  JNZ loop_start

; AX now contains 5
HLT
