import { InstructionHandler } from './instruction.model';
import { ArithmeticHandler } from './handlers/arithmetic.handler';
import { LogicHandler } from './handlers/logic.handler';
import { FlowHandler } from './handlers/flow.handler';
import { DataHandler } from './handlers/data.handler';

export class InstructionRegistry {
  private readonly handlers = new Map<string, InstructionHandler>();

  public constructor() {
    this.registerDefaults();
  }

  public get(mnemonic: string): InstructionHandler | undefined {
    return this.handlers.get(mnemonic.toUpperCase());
  }

  public has(mnemonic: string): boolean {
    return this.handlers.has(mnemonic.toUpperCase());
  }

  public getMnemonics(): string[] {
    return Array.from(this.handlers.keys());
  }

  private register(mnemonic: string, handler: InstructionHandler): void {
    this.handlers.set(mnemonic.toUpperCase(), handler);
  }

  private registerDefaults(): void {
    // Data movement
    this.register('MOV', ArithmeticHandler.mov);
    this.register('PUSH', DataHandler.push);
    this.register('POP', DataHandler.pop);
    this.register('XCHG', DataHandler.xchg);
    this.register('LEA', DataHandler.lea);

    // Arithmetic
    this.register('ADD', ArithmeticHandler.add);
    this.register('SUB', ArithmeticHandler.sub);
    this.register('CMP', ArithmeticHandler.cmp);
    this.register('INC', ArithmeticHandler.inc);
    this.register('DEC', ArithmeticHandler.dec);
    this.register('MUL', ArithmeticHandler.mul);
    this.register('DIV', ArithmeticHandler.div);
    this.register('NEG', ArithmeticHandler.neg);

    // Logic
    this.register('AND', LogicHandler.and);
    this.register('OR', LogicHandler.or);
    this.register('XOR', LogicHandler.xor);
    this.register('NOT', LogicHandler.not);
    this.register('SHL', LogicHandler.shl);
    this.register('SAL', LogicHandler.shl);
    this.register('SHR', LogicHandler.shr);

    // Flow control
    this.register('JMP', FlowHandler.jmp);
    this.register('JE', FlowHandler.je);
    this.register('JNE', FlowHandler.jne);
    this.register('JG', FlowHandler.jg);
    this.register('JGE', FlowHandler.jge);
    this.register('JL', FlowHandler.jl);
    this.register('JLE', FlowHandler.jle);
    this.register('JA', FlowHandler.ja);
    this.register('JB', FlowHandler.jb);
    this.register('JC', FlowHandler.jc);
    this.register('JZ', FlowHandler.jz);
    this.register('JNZ', FlowHandler.jnz);
    this.register('CALL', FlowHandler.call);
    this.register('RET', FlowHandler.ret);
    this.register('LOOP', FlowHandler.loop);

    // Misc
    this.register('NOP', DataHandler.nop);
    this.register('HLT', DataHandler.hlt);
    this.register('INT', DataHandler.int);
  }
}
