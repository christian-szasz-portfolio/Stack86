import { Operand, OperandType, OperandSize, ParsedInstruction } from '../instruction/instruction.model';
import { InstructionRegistry } from '../instruction/instruction-registry';
import { ParseError } from './parse-error.model';
import { Tokenizer, TokenizedLine, Token, TokenType } from './tokenizer';

export enum MemoryModel {
  Small = 'SMALL',
  Tiny = 'TINY',
  Medium = 'MEDIUM',
  Large = 'LARGE',
}

export interface ProgramConfig {
  /** Whether a .MODEL directive was declared */
  hasModel: boolean;
  /** Memory model declared by .MODEL directive */
  model: MemoryModel;
  /** Stack size in bytes declared by .STACK directive (default 256 for SMALL) */
  stackSize: number;
}

export interface StructField {
  name: string;
  size: number;
}

export interface StructDefinition {
  name: string;
  fields: StructField[];
}

export interface ParseResult {
  instructions: ParsedInstruction[];
  labels: Map<string, number>;
  errors: ParseError[];
  config: ProgramConfig;
  structs: StructDefinition[];
}

export class Parser {
  private readonly tokenizer = new Tokenizer();
  private readonly registry = new InstructionRegistry();

  public parse(source: string): ParseResult {
    const tokenizedLines = this.tokenizer.tokenize(source);
    const errors: ParseError[] = [];
    const labels = new Map<string, number>();
    const config: ProgramConfig = { hasModel: false, model: MemoryModel.Small, stackSize: 256 };
    const structs: StructDefinition[] = [];

    // --- Pass 1: collect labels, extract directives, compute instruction addresses ---
    const instructionLines: { tokens: Token[]; line: number; source: string }[] = [];
    let address = 0;
    let currentStruct: StructDefinition | null = null;

    for (const tl of tokenizedLines) {
      const remaining = Parser.extractLabels(tl, labels, address, errors);
      if (remaining.length === 0) continue; // label-only line

      // STRUC/ENDS block handling
      if (currentStruct !== null) {
        if (Parser.isEndOfStruct(remaining, currentStruct.name)) {
          structs.push(currentStruct);
          currentStruct = null;
        } else {
          Parser.collectStructField(remaining, currentStruct);
        }
        continue;
      }

      // Check for STRUC definition start: <name> STRUC
      const strucDef = Parser.tryParseStrucStart(remaining);
      if (strucDef !== null) {
        currentStruct = strucDef;
        continue;
      }

      // Check for PROC/ENDP: <name> PROC | <name> ENDP
      const procResult = Parser.tryParseProcDirective(remaining, labels, address, errors);
      if (procResult !== null) {
        continue;
      }

      // Process assembler directives (.MODEL, .CODE, .STACK, .DATA)
      if (remaining[0].type === TokenType.Directive) {
        if (remaining[0].value.toUpperCase() === '.DATA') {
          labels.set('@DATA', address);
        }
        Parser.processDirective(remaining, config);
        continue;
      }

      const mnemonic = remaining[0].value.toUpperCase();
      // Check for data directives
      const size = Parser.estimateInstructionSize(mnemonic, remaining.slice(1));
      instructionLines.push({ tokens: remaining, line: tl.line, source: tl.source });
      address += size;
    }

    // --- Pass 2: parse instructions with resolved labels ---
    const instructions: ParsedInstruction[] = [];
    let currentAddress = 0;

    for (const il of instructionLines) {
      const mnemonic = il.tokens[0].value.toUpperCase();

      if (!this.registry.has(mnemonic) && mnemonic !== 'DB' && mnemonic !== 'DW') {
        errors.push(new ParseError(il.line, il.tokens[0].column, `Unknown instruction: ${mnemonic}`));
        continue;
      }

      const operandTokens = il.tokens.slice(1);
      const operands = Parser.parseOperands(operandTokens, labels, il.line, errors);
      const size = Parser.estimateInstructionSize(mnemonic, il.tokens.slice(1));

      instructions.push({
        mnemonic,
        operands,
        line: il.line,
        address: currentAddress,
        size,
        source: il.source,
      });

      currentAddress += size;
    }

    return { instructions, labels, errors, config, structs };
  }

  private static processDirective(tokens: Token[], config: ProgramConfig): void {
    const directive = tokens[0].value.toUpperCase();

    switch (directive) {
      case '.MODEL': {
        config.hasModel = true;
        const arg = tokens[1]?.value?.toUpperCase();
        if (arg && Object.values(MemoryModel).includes(arg as MemoryModel)) {
          config.model = arg as MemoryModel;
        }
        // Set default stack size based on model
        switch (config.model) {
          case MemoryModel.Tiny:
            config.stackSize = 128;
            break;
          case MemoryModel.Small:
            config.stackSize = 256;
            break;
          case MemoryModel.Medium:
          case MemoryModel.Large:
            config.stackSize = 512;
            break;
        }
        break;
      }
      case '.STACK': {
        const sizeToken = tokens[1];
        if (sizeToken) {
          const size = typeof sizeToken.value === 'string'
            ? parseInt(sizeToken.value, 10)
            : sizeToken.value;
          if (!isNaN(size as number) && (size as number) > 0) {
            config.stackSize = size as number;
          }
        }
        break;
      }
      // .CODE and other directives — no config effect, just skip
    }
  }

  /**
   * Checks if a line is `<name> PROC` and registers it as a label.
   * Returns a truthy result if consumed, null otherwise.
   */
  private static tryParseProcDirective(
    tokens: Token[],
    labels: Map<string, number>,
    address: number,
    errors: ParseError[],
  ): string | null {
    if (tokens.length < 2 || tokens[0].type !== TokenType.Identifier) return null;
    const keyword = tokens[1].value.toUpperCase();

    if (keyword === 'PROC') {
      const name = tokens[0].value;
      if (labels.has(name)) {
        errors.push(new ParseError(tokens[0].line, tokens[0].column, `Duplicate label: ${name}`));
      } else {
        labels.set(name, address);
      }
      return name;
    }

    if (keyword === 'ENDP') {
      // End of procedure — no effect on instruction stream
      return tokens[0].value;
    }

    return null;
  }

  /**
   * Checks if a line is `<name> STRUC` and starts a struct definition.
   */
  private static tryParseStrucStart(tokens: Token[]): StructDefinition | null {
    if (
      tokens.length >= 2 &&
      tokens[0].type === TokenType.Identifier &&
      tokens[1].type === TokenType.Identifier &&
      tokens[1].value.toUpperCase() === 'STRUC'
    ) {
      return { name: tokens[0].value, fields: [] };
    }
    return null;
  }

  /**
   * Checks if a line is `<name> ENDS` that closes the given struct.
   */
  private static isEndOfStruct(tokens: Token[], structName: string): boolean {
    return (
      tokens.length >= 2 &&
      tokens[0].type === TokenType.Identifier &&
      tokens[0].value === structName &&
      tokens[1].type === TokenType.Identifier &&
      tokens[1].value.toUpperCase() === 'ENDS'
    );
  }

  /**
   * Collects a field from a STRUC body line: `<fieldName> DB ?` or `<fieldName> DW ?`
   */
  private static collectStructField(tokens: Token[], struct: StructDefinition): void {
    if (tokens.length < 2 || tokens[0].type !== TokenType.Identifier) return;
    const fieldName = tokens[0].value;
    const directive = tokens[1].value.toUpperCase();
    const size = directive === 'DB' ? 1 : directive === 'DW' ? 2 : 0;
    if (size > 0) {
      struct.fields.push({ name: fieldName, size });
    }
  }

  private static extractLabels(
    tl: TokenizedLine,
    labels: Map<string, number>,
    address: number,
    errors: ParseError[],
  ): Token[] {
    const tokens = [...tl.tokens];
    const remaining: Token[] = [];
    let i = 0;

    while (i < tokens.length) {
      // pattern: Identifier followed by Colon → label
      if (
        tokens[i].type === TokenType.Identifier &&
        i + 1 < tokens.length &&
        tokens[i + 1].type === TokenType.Colon
      ) {
        const name = tokens[i].value;
        if (labels.has(name)) {
          errors.push(new ParseError(
            tl.line, tokens[i].column,
            `Duplicate label: ${name}`,
          ));
        } else {
          labels.set(name, address);
        }
        i += 2; // skip identifier + colon
        continue;
      }
      remaining.push(tokens[i]);
      i++;
    }

    return remaining;
  }

  private static parseOperands(
    tokens: Token[],
    labels: Map<string, number>,
    line: number,
    errors: ParseError[],
  ): Operand[] {
    const operands: Operand[] = [];
    const groups = Parser.splitByComma(tokens);

    for (const group of groups) {
      if (group.length === 0) continue;
      const op = Parser.parseOperandGroup(group, labels, line, errors);
      if (op) operands.push(op);
    }

    return operands;
  }

  private static splitByComma(tokens: Token[]): Token[][] {
    const groups: Token[][] = [];
    let current: Token[] = [];

    for (const t of tokens) {
      if (t.type === TokenType.Comma) {
        groups.push(current);
        current = [];
      } else {
        current.push(t);
      }
    }
    if (current.length > 0) groups.push(current);
    return groups;
  }

  private static parseOperandGroup(
    tokens: Token[],
    labels: Map<string, number>,
    line: number,
    errors: ParseError[],
  ): Operand | null {
    let i = 0;
    let sizePrefix: OperandSize | undefined;

    // Check for size prefix: BYTE PTR / WORD PTR
    if (tokens[i]?.type === TokenType.SizePrefix) {
      sizePrefix = tokens[i].value === 'byte' ? OperandSize.Byte : OperandSize.Word;
      i++;
    }

    // Memory operand: [...]
    if (tokens[i]?.type === TokenType.OpenBracket) {
      i++; // skip [
      const inner: Token[] = [];
      while (i < tokens.length && tokens[i].type !== TokenType.CloseBracket) {
        inner.push(tokens[i]);
        i++;
      }
      // skip ]
      if (i < tokens.length) i++;

      if (inner.length === 0) {
        errors.push(new ParseError(line, tokens[0].column, 'Empty memory reference'));
        return null;
      }

      return Parser.parseMemoryExpression(inner, sizePrefix);
    }

    // Register operand
    if (tokens[i]?.type === TokenType.Register) {
      return { type: OperandType.Register, value: tokens[i].value, size: sizePrefix };
    }

    // Numeric immediate
    if (tokens[i]?.type === TokenType.Number) {
      return { type: OperandType.Immediate, value: Parser.parseNumber(tokens[i].value), size: sizePrefix };
    }

    // Identifier — could be a label reference
    if (tokens[i]?.type === TokenType.Identifier) {
      const name = tokens[i].value;
      if (labels.has(name)) {
        return { type: OperandType.Label, value: name, size: sizePrefix };
      }
      // Might be forward reference — treat as label
      return { type: OperandType.Label, value: name, size: sizePrefix };
    }

    // String literal (for DB directive)
    if (tokens[i]?.type === TokenType.String) {
      return { type: OperandType.Immediate, value: tokens[i].value, size: sizePrefix };
    }

    if (tokens[i]) {
      errors.push(new ParseError(line, tokens[i].column, `Unexpected token: ${tokens[i].value}`));
    }
    return null;
  }

  private static parseNumber(value: string): number {
    const lower = value.toLowerCase();
    // 0x prefix
    if (lower.startsWith('0x')) {
      return parseInt(lower, 16);
    }
    // h suffix
    if (lower.endsWith('h')) {
      return parseInt(lower.slice(0, -1), 16);
    }
    // b suffix — binary
    if (lower.endsWith('b')) {
      return parseInt(lower.slice(0, -1), 2);
    }
    // Plain decimal
    return parseInt(value, 10);
  }

  private static parseMemoryExpression(
    inner: Token[],
    sizePrefix: OperandSize | undefined,
  ): Operand {
    // Single token: [register] or [number] or [label]
    if (inner.length === 1) {
      const token = inner[0];
      if (token.type === TokenType.Register) {
        return { type: OperandType.Memory, value: token.value, size: sizePrefix };
      }
      if (token.type === TokenType.Number) {
        return { type: OperandType.Memory, value: Parser.parseNumber(token.value), size: sizePrefix };
      }
      return { type: OperandType.Memory, value: token.value, size: sizePrefix };
    }

    // Multi-token expression: [base +/- term ...]
    // e.g. [BP-2], [BX+4], [BX+SI+5]
    const base = inner[0];
    let baseValue: string | number;
    if (base.type === TokenType.Register) {
      baseValue = base.value;
    } else if (base.type === TokenType.Number) {
      baseValue = Parser.parseNumber(base.value);
    } else {
      baseValue = base.value;
    }

    let offset = 0;
    let j = 1;
    while (j < inner.length) {
      const op = inner[j];
      if (op.type !== TokenType.Plus && op.type !== TokenType.Minus) {
        j++;
        continue;
      }
      const sign = op.type === TokenType.Plus ? 1 : -1;
      j++;
      if (j >= inner.length) break;

      const term = inner[j];
      if (term.type === TokenType.Number) {
        offset += sign * Parser.parseNumber(term.value);
      }
      j++;
    }

    if (typeof baseValue === 'string') {
      return {
        type: OperandType.Memory,
        value: baseValue,
        size: sizePrefix,
        ...(offset !== 0 ? { offset } : {}),
      };
    }

    return { type: OperandType.Memory, value: (baseValue as number) + offset, size: sizePrefix };
  }

  private static estimateInstructionSize(mnemonic: string, operandTokens: Token[]): number {
    // For our emulator, each instruction occupies 1 "slot" for simplicity.
    // DB/DW directives occupy their data size.
    switch (mnemonic) {
      case 'DB': {
        let size = 0;
        for (const t of operandTokens) {
          if (t.type === TokenType.String) size += t.value.length;
          else if (t.type === TokenType.Number) size += 1;
          else if (t.type !== TokenType.Comma) size += 1;
        }
        return Math.max(size, 1);
      }
      case 'DW': {
        let count = 0;
        for (const t of operandTokens) {
          if (t.type === TokenType.Number || t.type === TokenType.Identifier) count++;
        }
        return Math.max(count * 2, 2);
      }
      default:
        return 1;
    }
  }
}
