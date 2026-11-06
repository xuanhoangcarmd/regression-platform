import type { ComboOption } from '../components/combo-select';

export type { ComboOption };

/** Innova group byte options (used by the OEM DTC "Group" combo). */
export const INNOVA_GROUP_COMBO_OPTIONS: ComboOption[] = [
  { id: 0x00, name: 'All' },
  { id: 0x01, name: 'OBDII' },
  { id: 0x02, name: 'ABS' },
  { id: 0x04, name: 'SRS' },
  { id: 0x08, name: 'TCM' },
  { id: 0x10, name: 'PCM' },
  { id: 0x20, name: 'OBDI' },
  { id: 0x40, name: 'TPMS' },
  { id: 0x80, name: 'ADAS' },
  { id: 0xff, name: 'All (255)' },
];

/** OEM DTC status/type names (chips). Determines RepairStatus on the backend. */
export const BUFFER_NAME_OPTIONS = [
  // → Recommend Repair
  '1 Trip',
  '1t',
  'Active',
  'Confirmed',
  'Current',
  'Current/MIL',
  'Failsafe/MIL',
  'Indeterminate/MIL',
  'Intermittent/MIL',
  'Last Test Failed',
  'MIL',
  'Not Current/MIL',
  'Pending|Confirmed',
  'Permanent',
  'Present',
  'Static',
  'Test Failed',
  'This Ignition Cycle',
  // → Warning
  'DEVELOPING',
  'FSC',
  'Indeterminate',
  'Intermittent',
  'Not Confirmed',
  'Not Passed SC',
  'Pending',
  'Pending|Not Confirmed',
  'Temporary',
  'Unconfirmed',
  // → History
  'Historic',
  'History',
  'Not Active',
  'Not Current',
  'Not Present',
  'Past',
  'Storage',
  'Stored',
  // → Warning (BufferName enum — not classified by DtcParser)
  'CM',
  'Continuous',
  'DCheck',
  'Failed',
  'Invalid',
  'KOEO',
  'KOER',
  'Latched',
  'Memory',
  'Provisional',
  'Readiness',
] as const;

/** ECU/TCU DTC type options (per-DTC dropdown). */
export const ECU_DTC_TYPE_OPTIONS = [
  'Stored',
  'Pending',
  'Permanent',
  'Current',
  'History',
  'Past',
  'Active',
  'MIL',
  'FSC',
  'Memory',
] as const;
