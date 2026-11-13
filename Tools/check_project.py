"""Lightweight repository sanity checks; Unity is required for compilation."""
import hashlib
import json
import re
import wave
import array
import math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
manifest=json.loads((root/'Packages/manifest.json').read_text())
assert manifest['dependencies']['com.unity.ugui']=='2.0.0'
assert manifest['dependencies']['com.unity.test-framework']=='1.6.0'
# Engine assembly names do not necessarily identify installable UPM packages.
# These two entries caused a confirmed package-resolution failure in Unity 6.4.
for unavailable in ('com.unity.modules.inputlegacy','com.unity.modules.textrendering'):
    assert unavailable not in manifest['dependencies'], f'Not a supported UPM package: {unavailable}'
version=(root/'ProjectSettings/ProjectVersion.txt').read_text()
assert 'm_EditorVersion: 6000.4.11f1\n' in version
assert 'm_EditorVersionWithRevision: 6000.4.11f1 (b0a1d6caadd2)' in version
input_settings=(root/'ProjectSettings/InputManager.asset').read_text()
assert set(re.findall(r'^    m_Name: (.+)$', input_settings, re.M))=={'Horizontal','Vertical','Submit','Cancel'}
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
mounted=json.loads((root/'Assets/Resources/MountedModels.json').read_text())['nodes']
joints=set();triangles=0;groups=set()
for node in mounted:
    assert not node['parent'] or node['parent'] in joints, node
    for field in ['position','rotation','scale','color']:
        assert len(node[field])==3 and all(math.isfinite(x) for x in node[field]), node
    assert all(x>0 for x in node['scale']), node
    if not node['shape']:
        assert node['name'] not in joints
        joints.add(node['name'])
    else:
        assert node['shape'] in ['round','box']
        assert all(0<=x<=1 for x in node['color'])
        triangles+=80 if node['shape']=='round' else 12
        groups.add((node['parent'],tuple(node['color'])))
assert {'Body','Neck','Tail','Head','Rider','RightArm','LeftArm','Sword','FL','FR','RL','RR','FLKnee','FRKnee','RLKnee','RRKnee'}<=joints
assert triangles<6000 and len(groups)<50, (triangles,len(groups))
print(f'Original mounted model hierarchy passed: {triangles} triangles, {len(groups)} material/joint groups.')
print('Project, FBX integrity, credits, mono audio and metadata checks passed. Unity compilation still required.')
