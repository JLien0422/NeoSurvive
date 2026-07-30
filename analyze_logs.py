import re
from pathlib import Path

base = Path(r"c:\Users\a3435\Downloads")
for n in range(1, 9):
    p = base / f"{n} (1).txt"
    text = p.read_text(encoding="utf-8", errors="replace")
    m = re.search(r"RESULT (.+)", text)
    result = m.group(1).strip() if m else "?"
    turns = len(re.findall(r"^TURN \d+ RESULT", text, re.M))
    blocks = re.findall(r"COMMAND LEFT START\n(.*?)\nCOMMAND LEFT END", text, re.S)
    train = sum(1 for b in blocks if re.search(r"^TRAIN", b, re.M))
    upgrade = sum(len(re.findall(r"^UPGRADE (\d+)", b, re.M)) for b in blocks)
    move = sum(len(re.findall(r"^MOVE ", b, re.M)) for b in blocks)
    empty = sum(1 for b in blocks if not b.strip())
    first_empty = next((i + 1 for i, b in enumerate(blocks) if not b.strip()), None)
    # TRAIN streak at start
    start_train = 0
    for b in blocks:
        if re.search(r"^TRAIN", b, re.M) and not re.search(r"^MOVE|^UPGRADE", b, re.M):
            start_train += 1
        elif b.strip():
            break
    abort = re.search(r"ABORT (.+)", text)
    print(
        f"#{n}: {result} | turns={turns} | LEFT TRAIN={train} UPGRADE={upgrade} MOVE={move} "
        f"| EMPTY={empty} | first_EMPTY=T{first_empty} | early_TRAIN_only={start_train}"
        + (f" | ABORT={abort.group(1)}" if abort else "")
    )
