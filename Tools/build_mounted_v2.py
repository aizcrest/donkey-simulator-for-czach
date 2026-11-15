"""Blender 4.3: build compact, explicitly bound mobile rigs from licensed GLBs.
Run: blender -b -t 2 --python Tools/build_mounted_v2.py
No Unity FBX/Avatar importer or postprocessor is involved.
"""
import bpy, bmesh, struct, json, math, hashlib
from pathlib import Path
from mathutils import Matrix, Vector, Quaternion, Euler
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Tools/AssetSources/MountedV2'; OUT=ROOT/'Assets/Resources/MountedV2'
C=Matrix(((1,0,0,0),(0,0,1,0),(0,-1,0,0),(0,0,0,1)))
CI=C.inverted(); manifest=[]

def build(kind):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath=str(SOURCE/(kind+'.glb')))
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 for t in rig.animation_data.nla_tracks:t.mute=True
 excluded={'1H_Sword_Offhand','Rectangle_Shield','Round_Shield','Spike_Shield','2H_Sword'}
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.name.startswith('Icosphere') and o.name not in excluded]
 for o in bpy.context.scene.objects:
  if o.type=='MESH' and o not in objects:o.hide_render=True
 bones=list(rig.data.bones); ids={b.name:i for i,b in enumerate(bones)}
 actions={a.name.rsplit('_',1)[0]:a for a in bpy.data.actions}
 fixed={}; scale=.65 if kind=='Knight' else 1.12
 rig.scale*=scale
 if kind=='Knight':
  rig.animation_data.action=actions['Sit_Chair_Pose'];bpy.context.scene.frame_set(0)
  for name in ['upperleg.l','upperleg.r']:
   b=rig.pose.bones[name];b.rotation_quaternion=Euler((-.5,0,-.65 if name.endswith('.l') else .65)).to_quaternion();b.scale.y=1.5
  for name in ['lowerleg.l','lowerleg.r']:
   rig.pose.bones[name].rotation_quaternion=Euler((.4,0,0)).to_quaternion();rig.pose.bones[name].scale.y=1.15
  for b in rig.pose.bones:
   if b.name in ['root','hips'] or any(w in b.name for w in ['leg','foot','toes','toe','heel','knee']):fixed[b.name]=b.matrix_basis.copy()
  bpy.context.view_layer.update()
  # Anchor the seated pelvis above the donkey's saddle, not at the source clip's chair position.
  rig.location=Vector((0,.12,1.15))-rig.matrix_world@rig.pose.bones['hips'].head
  specs=[('idle','Sit_Chair_Idle',True,12),('move','Sit_Chair_Idle',True,12),('charge','Sit_Chair_Idle',True,12),
   ('attack','1H_Melee_Attack_Slice_Diagonal',False,18),('chop','1H_Melee_Attack_Chop',False,18),
   ('guard','Blocking',True,12),('whirlwind','2H_Melee_Attack_Spinning',True,18),('kick','Sit_Chair_Idle',True,12)]
 else:
  specs=[('idle','Idle',True,18),('move','Walk',True,18),('charge','Run',True,18),('kick','Idle',True,18)]
  for o in objects:
   mesh=bmesh.new();mesh.from_mesh(o.data)
   bmesh.ops.remove_doubles(mesh,verts=list(mesh.verts),dist=.00001)
   mesh.to_mesh(o.data);mesh.free()
   for p in o.data.polygons:p.use_smooth=True
  # Original saddle and blue saddlecloth, bound to the spine along with the licensed donkey.
  for label,location,dimensions,color in [
   ('Saddlecloth',(0,.12,1.08),(.49,.68,.12),(.035,.12,.29,1)),
   ('Leather saddle',(0,.12,1.16),(.39,.48,.09),(.16,.065,.025,1))]:
   bpy.ops.mesh.primitive_cube_add(size=1,location=location);obj=bpy.context.object;obj.name=label
   obj.dimensions=dimensions;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
   bevel=obj.modifiers.new('Rounded edges','BEVEL');bevel.width=.04;bevel.segments=2
   bpy.ops.object.modifier_apply(modifier=bevel.name)
   mat=bpy.data.materials.new(label);mat.use_nodes=True;mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=color
   obj.data.materials.append(mat)
   group=obj.vertex_groups.new(name='Spine');group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
   mod=obj.modifiers.new('Saddle follows spine','ARMATURE');mod.object=rig;objects.append(obj)
 # Rest mesh and explicit bind matrices, including rigid helmet/weapon attachments.
 rig.animation_data.action=None;rig.data.pose_position='REST';bpy.context.view_layer.update()
 rest=[C@rig.matrix_world@b.matrix_local@CI for b in bones]
 inv=[m.inverted() for m in rest]
 graph=bpy.context.evaluated_depsgraph_get();refs=[];vertices=[];indices=[];lookup={};pixels={}
 for oi,obj in enumerate(objects):
  ev=obj.evaluated_get(graph);mesh=ev.to_mesh();mesh.calc_loop_triangles()
  uv=mesh.uv_layers.active;attr=mesh.color_attributes.active_color or (mesh.color_attributes[0] if len(mesh.color_attributes) else None)
  for tri in mesh.loop_triangles:
   mat=mesh.materials[tri.material_index];base=(1,1,1,1);image=None
   if mat and mat.use_nodes:
    for node in mat.node_tree.nodes:
     if node.type=='BSDF_PRINCIPLED':base=tuple(node.inputs['Base Color'].default_value)
     if node.type=='TEX_IMAGE' and node.image:image=node.image
   for li in tri.loops:
    vi=mesh.loops[li].vertex_index;v=mesh.vertices[vi]
    p=C@ev.matrix_world@v.co;n=(C.to_3x3()@ev.matrix_world.to_3x3().inverted().transposed()@mesh.corner_normals[li].vector).normalized()
    color=tuple(attr.data[li if attr.domain=='CORNER' else vi].color) if attr else (1,1,1,1)
    if image:
     if image.name not in pixels:pixels[image.name]=list(image.pixels)
     w,h=image.size;assert w and h
     tex=uv.data[li].uv;x=min(w-1,max(0,int(tex.x*w)));y=min(h-1,max(0,int(tex.y*h)))
     sample=pixels[image.name][(y*w+x)*4:(y*w+x)*4+4];color=tuple(color[i]*sample[i] for i in range(4))
    linear=[max(0,min(1,color[i]*base[i])) for i in range(3)]
    color=tuple(round((12.92*x if x<=.0031308 else 1.055*x**(1/2.4)-.055)*255) for x in linear)+(255,)
    weights=[]
    if obj.parent_type=='BONE':weights=[(ids[obj.parent_bone],1.)]
    else:
     for g in v.groups:
      name=obj.vertex_groups[g.group].name
      if name in ids and g.weight>0:weights.append((ids[name],g.weight))
    weights=sorted(weights,key=lambda x:-x[1])[:4];assert weights,(kind,obj.name,vi)
    total=sum(w for _,w in weights);weights=[(b,w/total) for b,w in weights]
    weights+= [(0,0.)]*(4-len(weights))
    key=(oi,vi,tuple(round(x,5) for x in n),color)
    if key not in lookup:
     lookup[key]=len(vertices);vertices.append((p,n,color,weights));refs.append((oi,vi))
    indices.append(lookup[key])
  ev.to_mesh_clear()
 rig.data.pose_position='POSE';clips=[];low=Vector((1e6,)*3);high=-low;max_error=0
 for state,action_name,loop,count in specs:
  action=actions[action_name];rig.animation_data.action=action
  start,end=action.frame_range;duration=max(.1,(end-start)/bpy.context.scene.render.fps)
  if state in ['move','charge','kick'] and kind=='Knight':duration={'move':1.15,'charge':.72,'kick':1.}[state]
  if state=='kick' and kind=='Donkey':duration=1.
  frames=[]
  for f in range(count):
   t=f/(count if loop else count-1);frame=start+(end-start)*t
   bpy.context.scene.frame_set(int(frame),subframe=frame-int(frame))
   for name,m in fixed.items():rig.pose.bones[name].matrix_basis=m
   phase=t*math.tau
   if kind=='Knight':
    # Small seated balance motions follow the donkey's stride; legs remain mounted during combat.
    if state in ['move','charge','kick']:
     amp=.045 if state=='charge' else .025
     rig.pose.bones['hips'].location.y+=math.sin(phase*2)*amp
     rig.pose.bones['chest'].rotation_quaternion @= Quaternion((1,0,0),math.sin(phase)*amp)
    if state=='charge':rig.pose.bones['chest'].rotation_quaternion @= Quaternion((1,0,0),-.16)
   elif state=='kick':
    pulse=math.sin(math.pi*t)**2
    for name in ['LeftHindUpLeg','RightHindUpLeg']:
     rig.pose.bones[name].rotation_quaternion @= Quaternion((1,0,0),-1.05*pulse)
    rig.pose.bones['Hips'].rotation_quaternion @= Quaternion((1,0,0),.09*pulse)
   bpy.context.view_layer.update()
   world=[C@rig.matrix_world@rig.pose.bones[b.name].matrix@CI for b in bones]
   local=[(world[ids[b.parent.name]].inverted()@world[i]) if b.parent else world[i] for i,b in enumerate(bones)]
   decomposed=[m.decompose() for m in local]
   for m,(p,q,s) in zip(local,decomposed):
    rebuilt=Matrix.LocRotScale(p,q,s)
    assert max(abs(m[row][col]-rebuilt[row][col]) for row in range(4) for col in range(4))<.0001,'Pose shear is not representable by Unity transforms'
   frames.append(decomposed)
   # Independently compare exported four-weight skinning against Blender's evaluated geometry.
   skin=[world[i]@inv[i] for i in range(len(bones))]
   graph=bpy.context.evaluated_depsgraph_get();evaluated=[]
   for o in objects:
    ev=o.evaluated_get(graph);evaluated.append((ev,ev.to_mesh()))
   for vi,(p,n,color,weights) in enumerate(vertices):
    result=sum((skin[b]@p*w for b,w in weights),Vector())
    for axis in range(3):low[axis]=min(low[axis],result[axis]);high[axis]=max(high[axis],result[axis])
    if vi%17==0:
     oi,idx=refs[vi];ev,mesh=evaluated[oi];expected=C@ev.matrix_world@mesh.vertices[idx].co
     error=(expected-result).length;max_error=max(max_error,error)
     assert error<.003,(kind,state,f,vi,error)
   for ev,mesh in evaluated:ev.to_mesh_clear()
  clips.append((state,duration,loop,frames))
 out=OUT/(kind+'.bytes')
 with out.open('wb') as file:
  def ints(*values):file.write(struct.pack('<'+'i'*len(values),*values))
  def floats(*values):
   assert all(math.isfinite(x) for x in values)
   file.write(struct.pack('<'+'f'*len(values),*values))
  def text(value):data=value.encode();ints(len(data));file.write(data)
  file.write(b'MNT2');ints(len(vertices),len(indices),len(bones),len(clips));floats(*low,*high)
  for i,b in enumerate(bones):
   text(b.name);ints(ids[b.parent.name] if b.parent else -1);floats(*(x for row in inv[i] for x in row))
  for p,n,color,weights in vertices:
   floats(*p,*n);file.write(bytes(color));file.write(struct.pack('<4H',*(b for b,w in weights)));floats(*(w for b,w in weights))
  ints(*indices)
  for name,duration,loop,frames in clips:
   text(name);floats(duration);ints(int(loop),len(frames))
   for frame in frames:
    for p,q,s in frame:floats(*p,q.x,q.y,q.z,q.w,*s)
 info={'kind':kind,'vertices':len(vertices),'triangles':len(indices)//3,'bones':len(bones),'clips':{c[0]:{'duration':c[1],'frames':len(c[3])} for c in clips},'bounds':[list(low),list(high)],'max_skin_error_m':max_error,'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()}
 manifest.append(info);print('EXPORTED',info)
 # Keep inspectable source scene with active idle pose for the preview renderer.
 rig.animation_data.action=actions[specs[0][1]];bpy.context.scene.frame_set(0)
 for name,m in fixed.items():rig.pose.bones[name].matrix_basis=m
 bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath='/tmp/mounted-v2-'+kind+'.blend')
for kind in ['Donkey','Knight']:build(kind)
(SOURCE/'generated.json').write_text(json.dumps(manifest,indent=2)+'\n')
