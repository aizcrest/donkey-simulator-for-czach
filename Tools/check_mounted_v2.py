"""Validate explicit runtime rigs and sampled skinning, including interpolated poses.
Requires numpy. Also imported by the Blender preview script.
"""
import hashlib, json, math, struct
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
def load(path):
 raw=path.read_bytes();offset=0
 def take(fmt):
  nonlocal offset
  size=struct.calcsize('<'+fmt);values=struct.unpack_from('<'+fmt,raw,offset);offset+=size;return values
 def text():
  nonlocal offset
  length=take('i')[0];assert 0<length<81
  value=raw[offset:offset+length].decode();offset+=length;return value
 assert take('4s')[0]==b'MNT2'
 nv,ni,nb,nc=take('4i');assert 3<=nv<65000 and ni%3==0 and 0<nb<=128 and 0<nc<=16
 bounds=np.array(take('6f')).reshape(2,3);bones=[];bind=[]
 for i in range(nb):
  name=text();parent=take('i')[0];assert -1<=parent<i
  bones.append((name,parent));bind.append(np.array(take('16f')).reshape(4,4))
 p=[];n=[];colors=[];joints=[];weights=[]
 for i in range(nv):
  p.append(take('3f'));n.append(take('3f'));colors.append(take('4B'));joints.append(take('4H'));weights.append(take('4f'))
 indices=np.array(take(str(ni)+'i'));clips={}
 for i in range(nc):
  name=text();duration=take('f')[0];loop,count=take('2i');assert duration>0 and loop in [0,1] and 2<=count<=64
  poses=np.array(take(str(count*nb*10)+'f')).reshape(count,nb,10)
  assert np.isfinite(poses).all() and np.allclose(np.linalg.norm(poses[:,:,3:7],axis=2),1,atol=.0001)
  assert (poses[:,:,7:]>0).all()
  clips[name]={'duration':duration,'loop':bool(loop),'poses':poses}
 assert offset==len(raw)
 return dict(bones=bones,bind=np.array(bind),p=np.array(p),n=np.array(n),colors=np.array(colors),joints=np.array(joints),weights=np.array(weights),indices=indices,clips=clips,bounds=bounds)

def sample(data,name,t):
 clip=data['clips'][name];poses=clip['poses'];count=len(poses)
 frame=(t%1 if clip['loop'] else min(t,1))*(count if clip['loop'] else count-1)
 a=min(int(frame),count-1);b=(a+1)%count if clip['loop'] else min(a+1,count-1);f=frame-a
 pose=poses[a]*(1-f)+poses[b]*f
 # Spherical interpolation, matching Unity Quaternion.Slerp.
 q0=poses[a,:,3:7];q1=poses[b,:,3:7].copy();dot=np.sum(q0*q1,axis=1)
 q1[dot<0]*=-1;dot=abs(dot).clip(0,1);theta=np.arccos(dot);sin=np.sin(theta)
 wa=np.full(len(dot),1-f);wb=np.full(len(dot),f);mask=sin>.0001
 wa[mask]=np.sin((1-f)*theta[mask])/sin[mask];wb[mask]=np.sin(f*theta[mask])/sin[mask]
 q=q0*wa[:,None]+q1*wb[:,None];q/=np.linalg.norm(q,axis=1)[:,None]
 matrices=[]
 for i,(_,parent) in enumerate(data['bones']):
  x,y,z,w=q[i];m=np.eye(4)
  m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
   [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
   [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])*pose[i,7:][None,:]
  m[:3,3]=pose[i,:3]
  matrices.append(m if parent<0 else matrices[parent]@m)
 skin=np.array(matrices)@data['bind'];points=np.column_stack((data['p'],np.ones(len(data['p']))))
 transformed=np.einsum('vwij,vj->vwi',skin[data['joints']],points)
 return np.sum(transformed*data['weights'][:,:,None],axis=1)[:,:3]

def check():
 sources=ROOT/'Tools/AssetSources/MountedV2';manifest=json.loads((sources/'generated.json').read_text());total=0;checks=0
 for entry in manifest:
  path=ROOT/'Assets/Resources/MountedV2'/(entry['kind']+'.bytes');data=load(path)
  assert hashlib.sha256(path.read_bytes()).hexdigest()==entry['sha256']
  assert len(data['p'])==entry['vertices'] and len(data['indices'])==entry['triangles']*3
  assert (data['indices']>=0).all() and (data['indices']<len(data['p'])).all()
  assert (data['joints']<len(data['bones'])).all() and (data['weights']>=0).all()
  assert np.allclose(data['weights'].sum(axis=1),1,atol=.0001)
  assert np.isfinite(data['p']).all() and np.isfinite(data['n']).all()
  assert np.allclose(np.linalg.norm(data['n'],axis=1),1,atol=.001)
  assert {'idle','move','charge','kick'}<=data['clips'].keys()
  if entry['kind']=='Knight':assert {'attack','chop','guard','whirlwind'}<=data['clips'].keys()
  for name,clip in data['clips'].items():
   for t in np.linspace(0,1,len(clip['poses'])*2+1):
    points=sample(data,name,t);assert np.isfinite(points).all()
    assert (points>=data['bounds'][0]-.15).all() and (points<=data['bounds'][1]+.15).all(),(entry['kind'],name,t)
    assert np.ptp(points[:,1])>.5
    checks+=1
  total+=entry['triangles']
  print(entry['kind'],entry['triangles'],'triangles',len(data['clips']),'clips; skinning and interpolated bounds passed')
 assert total<10000
 for source in json.loads((sources/'sources.json').read_text()):
  assert hashlib.sha256((sources/source['file']).read_bytes()).hexdigest()==source['sha256']
 print(checks,'sampled poses passed;',total,'total triangles.')
if __name__=='__main__':check()
