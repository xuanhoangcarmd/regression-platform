/** Side-by-side line diff: aligns two texts into rows for the diff viewer. */

/** 'ignored' is assigned by the viewer (not by buildLineDiff) for lines under an ignore rule. */
export type DiffRowType = 'equal' | 'changed' | 'added' | 'removed' | 'ignored';

export interface IDiffRow {
  /** 1-based line number on the left (Run A), or null for a gap. */
  leftNo: number | null;
  /** 1-based line number on the right (Run B), or null for a gap. */
  rightNo: number | null;
  left: string | null;
  right: string | null;
  type: DiffRowType;
}

/**
 * Builds aligned side-by-side rows from two arrays of lines, using a Myers O(ND) diff and pairing
 * adjacent delete/insert runs into "changed" rows (GitHub-style). Handles large inputs and one-sided
 * edits correctly: lines that match stay aligned ("equal"), and a field/element present on only one
 * side shows as added/removed rather than shifting everything into "changed".
 */
export function buildLineDiff(a: string[], b: string[]): IDiffRow[] {
  if (a.length === 0 && b.length === 0) {
    return [];
  }
  return pairBlocks(myersOps(a, b));
}

interface Op {
  type: 'equal' | 'delete' | 'insert';
  line: string;
}

/**
 * Myers diff (O(ND)) → an ordered op list (equal/delete/insert). Records each round's frontier, then
 * backtracks from (n, m) to (0, 0). No size cap — memory is O(D·(n+m)), and D (the edit distance) is
 * small for typical near-identical payloads.
 */
function myersOps(a: string[], b: string[]): Op[] {
  const n = a.length;
  const m = b.length;
  const max = n + m;
  const offset = max;
  const trace: number[][] = [];
  const v = new Array<number>(2 * max + 1).fill(0);
  let found = 0;

  for (let d = 0; d <= max; d++) {
    trace.push(v.slice());
    let done = false;
    for (let k = -d; k <= d; k += 2) {
      // Move down (insert from b) or right (delete from a), whichever reached further.
      const x =
        k === -d || (k !== d && v[offset + k - 1] < v[offset + k + 1])
          ? v[offset + k + 1]
          : v[offset + k - 1] + 1;
      let y = x - k;
      let xx = x;
      while (xx < n && y < m && a[xx] === b[y]) {
        xx++;
        y++;
      }
      v[offset + k] = xx;
      if (xx >= n && y >= m) {
        found = d;
        done = true;
        break;
      }
    }
    if (done) {
      break;
    }
  }

  const ops: Op[] = [];
  let x = n;
  let y = m;
  for (let d = found; d > 0; d--) {
    const prev = trace[d];
    const k = x - y;
    const prevK =
      k === -d || (k !== d && prev[offset + k - 1] < prev[offset + k + 1]) ? k + 1 : k - 1;
    const prevX = prev[offset + prevK];
    const prevY = prevX - prevK;
    while (x > prevX && y > prevY) {
      ops.push({ type: 'equal', line: a[x - 1] });
      x--;
      y--;
    }
    if (x === prevX) {
      ops.push({ type: 'insert', line: b[y - 1] });
      y--;
    } else {
      ops.push({ type: 'delete', line: a[x - 1] });
      x--;
    }
  }
  // d === 0: the leading run of equal lines.
  while (x > 0 && y > 0) {
    ops.push({ type: 'equal', line: a[x - 1] });
    x--;
    y--;
  }
  ops.reverse();
  return ops;
}

/** Converts the op list into aligned rows, pairing delete+insert runs into "changed" rows. */
function pairBlocks(ops: Op[]): IDiffRow[] {
  const rows: IDiffRow[] = [];
  let leftNo = 1;
  let rightNo = 1;
  let i = 0;

  while (i < ops.length) {
    if (ops[i].type === 'equal') {
      rows.push({ leftNo: leftNo++, rightNo: rightNo++, left: ops[i].line, right: ops[i].line, type: 'equal' });
      i++;
      continue;
    }

    // Gather a contiguous change block, then pair removed lines with added lines.
    const dels: string[] = [];
    const ins: string[] = [];
    while (i < ops.length && ops[i].type !== 'equal') {
      if (ops[i].type === 'delete') {
        dels.push(ops[i].line);
      } else {
        ins.push(ops[i].line);
      }
      i++;
    }

    const pairs = Math.min(dels.length, ins.length);
    for (let k = 0; k < pairs; k++) {
      rows.push({ leftNo: leftNo++, rightNo: rightNo++, left: dels[k], right: ins[k], type: 'changed' });
    }
    for (let k = pairs; k < dels.length; k++) {
      rows.push({ leftNo: leftNo++, rightNo: null, left: dels[k], right: null, type: 'removed' });
    }
    for (let k = pairs; k < ins.length; k++) {
      rows.push({ leftNo: null, rightNo: rightNo++, left: null, right: ins[k], type: 'added' });
    }
  }

  return rows;
}
