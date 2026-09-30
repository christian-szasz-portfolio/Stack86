; Fibonacci sequence using registers
; Computes first 10 Fibonacci numbers
; Final result in BX = 55 (F10)

MOV CX, 9       ; iterations remaining (first is seeded)
MOV AX, 0       ; F(0) = 0
MOV BX, 1       ; F(1) = 1

fib_loop:
  MOV DX, BX    ; DX = current
  ADD BX, AX    ; BX = current + previous
  MOV AX, DX    ; AX = old current (now previous)
  DEC CX
  JNZ fib_loop

; AX = 34 (F9), BX = 55 (F10)
HLT
