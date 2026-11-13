"""Validate every exported enemy pose without a Unity installation."""
import struct,math,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read_model(path):
 with path.open('rb') as f:
  def ints(n=1):return struct.unpack('<'+'i'*n,f.read(4*n))
  assert f.read(4)==b'FRM1',path
  v,n,c=ints(3);assert 3<=v<=65535 and n%3==0 and 1<=c<=8
  colors=f.read(v*4);assert len(colors)==v*4 and any(colors[i]>0 for i in range(len(colors)) if i%4<3)
  indices=ints(n);assert all(0<=i<v for i in indices)
  clips={}
  for _ in range(c):
   length=ints()[0];assert 1<=length<=32
   name=f.read(length).decode('ascii');duration,count=struct.unpack('<fi',f.read(8));assert math.isfinite(duration) and duration>0 and 1<=count<=32
   frames=[]
   for frame in range(count):
    values=struct.unpack('<'+'f'*(v*6),f.read(v*24));assert all(math.isfinite(x) for x in values),(path,name,frame)
    positions=[values[i:i+3] for i in range(0,len(values),6)];ys=[p[1] for p in positions]
    assert max(ys)-min(ys)>.05 and max(ys)<5 and min(ys)>-3,(path,name,frame,min(ys),max(ys))
    normals=[values[i+3:i+6] for i in range(0,len(values),6)]
    assert all(.8<sum(n*n for n in normal)<1.2 for normal in normals),(path,name,'normals')
    frames.append((positions,normals))
   clips[name]=(duration,frames)
  assert {'idle','move','attack'}<=clips.keys();assert not f.read(1)
  ys=[p[1] for p in clips['idle'][1][0][0]];assert abs(min(ys))<.005 and abs(max(ys)-1)<.005,path
  if path.stem=='Dragon':assert 'fly' in clips
  return colors,indices,clips
if __name__=='__main__':
 for entry in json.loads((root/'Tools/AssetSources/enemy_frames.json').read_text()):
  path=root/'Assets/Resources/EnemyFrames'/(entry['kind']+'.bytes');colors,indices,clips=read_model(path)
  assert path.stat().st_size==entry['bytes'] and len(colors)//4==entry['vertices']
  print('PASS',path.stem,len(colors)//4,'vertices,',sum(len(x[1]) for x in clips.values()),'checked poses')
