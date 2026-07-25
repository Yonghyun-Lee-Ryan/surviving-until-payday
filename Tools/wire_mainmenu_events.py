# -*- coding: utf-8 -*-
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
events = []
for p in sorted((root / "Assets/Data/Events").glob("*.asset")):
    meta = Path(str(p) + ".meta")
    if not meta.exists():
        continue
    guid = re.search(r"guid: ([a-f0-9]+)", meta.read_text(encoding="utf-8")).group(1)
    text = p.read_text(encoding="utf-8")
    eid = re.search(r"^  id: (.+)$", text, re.M).group(1).strip()
    events.append((eid, guid))
events.sort(key=lambda x: x[0])

lines = ["  eventCatalog:"] + [
    f"  - {{fileID: 11400000, guid: {g}, type: 2}}" for _, g in events
]
block = "\n".join(lines) + "\n"

scene = root / "Assets/Scenes/MainMenu.unity"
text = scene.read_text(encoding="utf-8")
marker = "  totalEndingCount:"
idx = text.find(marker)
if idx < 0:
    raise SystemExit("totalEndingCount marker not found")
if "eventCatalog:" in text:
    text = re.sub(
        r"  eventCatalog:\n(?:  - \{fileID: 11400000, guid: [a-f0-9]+, type: 2\}\n)+",
        block,
        text,
        count=1,
    )
else:
    text = text[:idx] + block + text[idx:]
scene.write_text(text, encoding="utf-8")
print(f"wired {len(events)} events into MainMenu.eventCatalog")
