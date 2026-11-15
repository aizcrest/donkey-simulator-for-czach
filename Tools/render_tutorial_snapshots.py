"""Blender 4.3: small staged tutorial illustrations using the actual licensed runtime meshes.
These are instructional snapshots, not Unity gameplay captures.
"""
import bpy, math, sys, struct
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tools'))
from check_mounted_v2 import load,sample
OUT=ROOT/'Assets/Resources/Tutorial'

def material(name,color,glow=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=.85
 if glow:b.inputs['Emission Color'].default_value=(*color,1);b.inputs['Emission Strength'].default_value=1.2
 return m

def mesh_object(name,points,indices,colors,position=(0,0,0),scale=1,angle=0):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([(x,-z,y) for x,y,z in points],[],[tuple(indices[i:i+3]) for i in range(0,len(indices),3)]);mesh.update()
 obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);obj.location=position;obj.scale=(scale,)*3;obj.rotation_euler.z=angle
 attr=mesh.color_attributes.new(name='Coat',type='FLOAT_COLOR',domain='POINT')
 for i,c in enumerate(colors):
  rgb=[v/255 for v in c[:3]];attr.data[i].color=(*[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb],1)
 m=material(name,(1,1,1));node=m.node_tree.nodes.new('ShaderNodeVertexColor');node.layer_name='Coat';m.node_tree.links.new(node.outputs['Color'],m.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);mesh.materials.append(m)
 return obj

def mounted(position=(-1.4,0,0),state='idle'):
 for kind in ['Donkey','Knight']:
  d=load(ROOT/'Assets/Resources/MountedV2'/(kind+'.bytes'));pose=state if kind=='Knight' else 'idle'
  mesh_object(kind,sample(d,pose,.35),d['indices'].tolist(),d['colors'],position)

def enemy(kind,state,position,height,angle=0):
 data=(ROOT/'Assets/Resources/EnemyFrames'/(kind+'.bytes')).read_bytes();offset=4
 def read(fmt):
  nonlocal offset
  v=struct.unpack_from('<'+fmt,data,offset);offset+=struct.calcsize('<'+fmt);return v
 nv,ni,nc=read('3i');colors=[read('4B') for _ in range(nv)];indices=read(str(ni)+'i')
 for c in range(nc):
  n=read('i')[0];name=data[offset:offset+n].decode();offset+=n;duration,count=read('fi')
  if name==state:
   offset+=min(2,count-1)*nv*24;values=[read('6f') for _ in range(nv)]
   return mesh_object(kind,[v[:3] for v in values],indices,colors,position,height,angle)
  offset+=count*nv*24
 raise ValueError((kind,state))

def cylinder(position,radius,depth,mat):
 bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=depth,location=position);o=bpy.context.object;o.data.materials.append(mat);return o

def ring(position,radius,mat):
 bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=6,location=position,major_radius=radius,minor_radius=.035);bpy.context.object.data.materials.append(mat)

def sphere(position,radius,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=radius,location=position);bpy.context.object.data.materials.append(mat)

def setup():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 grass=material('Forest floor',(.10,.21,.13));bark=material('Bark',(.16,.075,.035));leaves=material('Needles',(.10,.29,.17))
 bpy.ops.mesh.primitive_plane_add(size=100);bpy.context.object.data.materials.append(grass);bpy.context.object.location.z=-.06
 for x,y in [(-4,1),(-2.8,3.5),(0,4),(2.2,3),(4.5,1),(5,4),(-5,-1)]:
  cylinder((x,y,.8),.12,1.6,bark)
  for z,r in [(1.7,.85),(2.4,.62)]:
   bpy.ops.mesh.primitive_cone_add(vertices=7,radius1=r,depth=1.8,location=(x,y,z));bpy.context.object.data.materials.append(leaves)
 s=bpy.context.scene;s.world=bpy.data.worlds.new('Forest light');s.world.color=(.3,)*3
 bpy.ops.object.camera_add(location=(4.5,-8,5));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1.3))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=9.4;s.camera=cam
 for loc,power,size in [((1,-4,7),1200,5),((-3,2,4),700,4)]:
  bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
 s.render.engine='CYCLES';s.cycles.samples=96;s.cycles.use_denoising=False;s.render.resolution_x=768;s.render.resolution_y=288;s.render.resolution_percentage=100
 return s

pages=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['ride','combat','orbs','dragon']
assert pages and set(pages)<={'ride','combat','orbs','dragon'}
for page in pages:
 s=setup();gold=material('Direction gold',(1,.65,.12),True);cyan=material('Orb cyan',(.05,.85,.7),True)
 if page=='ride':
  mounted((-.7,0,0))
  # A simple route arrow illustrates travel without resembling an interactive HUD button.
  for y in [.45,0,-.45]:
   bpy.ops.mesh.primitive_cone_add(vertices=3,radius1=.2,depth=.045,location=(1.25,y,.03));bpy.context.object.data.materials.append(gold)
 elif page=='combat':
  mounted((-1.4,0,0),'attack');enemy('Wolf','attack',(1.15,-.2,0),1.15,-math.pi/2)
  ring((.1,-.3,.03),.75,gold)
  for x,y in [(1.5,1),(1.8,.8),(2.1,1)]:sphere((x,y,.18),.12,gold)
 elif page=='orbs':
  mounted((-1.8,-.3,0));stone=material('Altar stone',(.25,.27,.30));cylinder((1.1,.2,.08),1.05,.16,stone);ring((1.1,.2,.19),1.12,cyan)
  for i in range(10):
   a=i*math.tau/10;bpy.ops.mesh.primitive_cube_add(size=.16,location=(1.1+math.cos(a)*.8,.2+math.sin(a)*.8,.33),rotation=(0,math.pi/4,math.pi/4));bpy.context.object.data.materials.append(cyan)
  sphere((-.1,-.85,.55),.27,cyan);ring((-.1,-.85,.03),.48,cyan)
 else:
  mounted((-1.9,-.55,0));enemy('Dragon','fly',(1.05,1.2,1.2),2.2,-.65)
  red=material('Dive warning',(1,.08,.025),True);fire=material('Fireball',(1,.35,.025),True)
  ring((.75,0,.035),.72,red);sphere((1,-.35,1.6),.22,fire)
  s.camera.data.ortho_scale=10.6
  s.camera.rotation_euler=(Vector((0,0,1.65))-s.camera.location).to_track_quat('-Z','Y').to_euler()
 s.render.filepath=str(OUT/(page+'.png'));bpy.ops.render.render(write_still=True)
