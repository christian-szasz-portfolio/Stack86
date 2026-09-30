import { IconProp } from '@fortawesome/fontawesome-svg-core';

export interface DropdownMenuItem {
  readonly value: string;
  readonly label: string;
  readonly icon?: IconProp;
  readonly image?: string;
  readonly disabled?: boolean;
}
