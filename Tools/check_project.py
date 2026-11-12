"""Lightweight repository sanity checks; Unity is required for compilation."""
import hashlib
import json
import re
import wave
import array
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
entries=json.loads((root/'Tools/AssetSources/models.json').read_text())
assert len(entries)==9
for entry in entries:
    model=root/'Assets/Resources/Enemies'/(entry['kind']+'.fbx')
    content=model.read_bytes()
    assert content.startswith(b'Kaydara FBX Binary'), model
    assert hashlib.sha256(content).hexdigest()==entry['fbx_sha256'], model
    assert entry['license'] in ['CC0 1.0','CC BY 3.0']
    assert entry['source'] in (root/'THIRD_PARTY.md').read_text()
for path in (root/'Assets').rglob('*'):
    if path.suffix!='.meta': assert Path(str(path)+'.meta').exists(), path
audio=list((root/'Assets/Resources/Audio').glob('*.wav'))
assert len(audio)==19
for path in audio:
    with wave.open(str(path),'rb') as sound:
        assert sound.getnchannels()==1 and sound.getsampwidth()==2 and sound.getframerate()==22050, path
        frames=sound.readframes(sound.getnframes());assert len(frames)>1000, path
        samples=array.array('h',frames);assert max(abs(x) for x in samples)>1000, path
        assert samples[0]==0 and samples[-1]==0, path
assert (root/'Assets/Scripts/MobilePerformance.cs').is_file()
print('Project, FBX integrity, credits, mono audio and metadata checks passed. Unity compilation still required.')
