import argparse
import json
import os
import sqlite3
from datetime import datetime


def read_grid_rows(conn):
    cur = conn.cursor()
    cur.execute('SELECT row_index, row_text FROM grid_rows ORDER BY row_index')
    rows = [row_text for _, row_text in cur.fetchall()]
    return rows


def read_placements(conn):
    cur = conn.cursor()
    cur.execute('SELECT word, row_index, col_index, orientation FROM placements')
    return cur.fetchall()


def lookup_definitions(defs_conn):
    cur = defs_conn.cursor()
    cur.execute('SELECT word, definition FROM definitions')
    return {w.upper(): d for (w, d) in cur.fetchall() if w and d}


def build_cells(row, col, orientation, length):
    cells = []
    if orientation.lower().startswith('h'):
        for i in range(length):
            cells.append([row, col + i])
    else:
        for i in range(length):
            cells.append([row + i, col])
    return cells


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--result', default=os.path.join('..', 'cache', 'motcroise.result.sqlite'))
    ap.add_argument('--defs', default=os.path.join('..', 'cache', 'definitions.fr.sqlite'))
    ap.add_argument('--out', default=os.path.join('src', 'data', 'grids5x5-001.json'))
    ap.add_argument('--grid-size', type=int, default=5)
    ap.add_argument('--grid-id', default='001')
    args = ap.parse_args()

    result_path = os.path.abspath(args.result)
    defs_path = os.path.abspath(args.defs)
    out_path = os.path.abspath(args.out)

    if not os.path.exists(result_path):
        raise SystemExit(f'Result DB introuvable: {result_path}')

    res_conn = sqlite3.connect(result_path)
    rows = read_grid_rows(res_conn)
    if not rows:
        raise SystemExit('Aucune ligne dans grid_rows')

    size = len(rows)
    if size != args.grid_size:
        raise SystemExit(f'La grille dans SQLite est {size}x{size} (attendu {args.grid_size}x{args.grid_size}). Genere d\'abord une grille {args.grid_size}x{args.grid_size}.')

    placements = read_placements(res_conn)
    res_conn.close()

    defs_map = {}
    if os.path.exists(defs_path):
        defs_conn = sqlite3.connect(defs_path)
        defs_map = lookup_definitions(defs_conn)
        defs_conn.close()

    across = []
    down = []
    num_across = 1
    num_down = 1

    # Stable ordering
    placements_sorted = sorted(placements, key=lambda p: (p[3].lower(), p[1], p[2], p[0].upper()))

    for word, r, c, orient in placements_sorted:
        w = (word or '').upper().strip()
        if len(w) < 2:
            continue
        clue = defs_map.get(w, 'Definition indisponible')
        entry = {
            'id': None,
            'number': None,
            'clue': clue,
            'answer': w,
            'cells': build_cells(int(r), int(c), orient, len(w)),
        }
        if orient.lower().startswith('h'):
            entry['number'] = num_across
            entry['id'] = str(num_across).zfill(2)
            num_across += 1
            across.append(entry)
        else:
            entry['number'] = num_down
            entry['id'] = str(num_down).zfill(2)
            num_down += 1
            down.append(entry)

    payload = {
        'id': args.grid_id,
        'size': size,
        'difficulty': 'moyen',
        'source': {
            'result_db': os.path.basename(result_path),
            'defs_db': os.path.basename(defs_path),
            'generated_utc': datetime.utcnow().isoformat() + 'Z',
        },
        'grid': rows,
        'clues': {
            'across': across,
            'down': down,
        },
    }

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, 'w', encoding='utf-8') as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)

    print('OK:', out_path)
    print('grid size:', size)
    print('across:', len(across), 'down:', len(down))


if __name__ == '__main__':
    main()
