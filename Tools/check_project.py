"""Lightweight repository sanity checks; Unity is required for compilation."""
import json
import re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
manifest=json.loads((root/'Packages/manifest.json').read_text())
assert 'com.unity.ugui' in manifest['dependencies']
assert '2022.3.62f1' in (root/'ProjectSettings/ProjectVersion.txt').read_text()
for path in (root/'Assets').rglob('*.cs'):
    source=path.read_text()
    # Strip comments and string literals before checking delimiter balance.
    clean=re.sub(r'//[^\n]*|/\*.*?\*/|\$?"(?:\\.|[^"\\])*"', '', source, flags=re.S)
    stack=[]
    pairs={'}':'{', ')':'(', ']':'['}
    for char in clean:
        if char in '{([': stack.append(char)
        elif char in '})]':
            assert stack and stack.pop()==pairs[char], path
    assert not stack, path
    assert 'namespace DonkeyForest' in source, path
for name in ['ForestGame.cs','ForestEnemy.cs']:
    assert (root/'Assets/Scripts'/name).is_file()
print('Project structure and package checks passed. Unity compilation still required.')
