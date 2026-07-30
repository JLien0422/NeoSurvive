import re
from pathlib import Path

base = Path(r"c:\Users\a3435\Downloads")
for n in range(1, 9):
    text = (base / f"{n} (1).txt").read_text(encoding="utf-8", errors="replace")
    blocks = re.findall(r"COMMAND LEFT START\r?\n(.*?)\r?\nCOMMAND LEFT END", text, re.S)
    train_only = 0
    empty = 0
    first_upg = None
    first_move = None
    for i, b in enumerate(blocks, 1):
        lines = [x for x in b.strip().splitlines() if x.strip()]
        if not lines:
            empty += 1
            continue
        kinds = set()
        for ln in lines:
            if ln.startswith("TRAIN"): kinds.add("T")
            elif ln.startswith("MOVE"): kinds.add("M")
            elif ln.startswith("UPGRADE"): kinds.add("U")
        if kinds == {"T"}:
            train_only += 1
        if "U" in kinds and first_upg is None:
            first_upg = i
        if "M" in kinds and first_move is None:
            first_move = i
    result = re.search(r"RESULT (.+)", text).group(1).strip()
    abort = re.search(r"ABORT (.+)", text)
    print(f"#{n} {result}")
    print(f"  turns={len(blocks)} train_only={train_only} empty={empty} first_MOVE=T{first_move} first_UPGRADE=T{first_upg}")
    if abort:
        print(f"  ABORT: {abort.group(1)}")
    # T1-T6 snapshot
    for i in range(min(6, len(blocks))):
        lines = [x for x in blocks[i].strip().splitlines() if x.strip()]
        tag = "EMPTY" if not lines else " | ".join(lines[:4])
        print(f"  T{i+1}: {tag}")
    print()
