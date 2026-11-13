"""Blender: bake licensed animated enemies into shared ordinary-mesh frames for Unity."""
import bpy,struct,json,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];out=root/'Assets/Resources/EnemyFrames';out.mkdir(exist_ok=True)
manifest=[]
for kind in ['Deer','Wolf','Fox','Zombie','Dragon']:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for blocks in [bpy.data.actions,bpy.data.meshes,bpy.data.armatures,bpy.data.materials,bpy.data.images]:
  for item in list(blocks):blocks.remove(item)
 bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Resources/Enemies'/(kind+'.fbx')))
 rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
 actions=list(bpy.data.actions)
 def choose(words,exclude=()):
  for word in words:
   for a in actions:
    n=a.name.lower().split('|')[-1]
    if word in n and not any(x in n for x in exclude):return a
 clips={}
 clips['idle']=choose(['idle'],['attack','hit','jump','flying']) or choose(['flying'])
 clips['move']=choose(['walk','run','gallop']) or clips['idle']
 clips['attack']=choose(['attack','punch']) or clips['idle']
 hit=choose(['hit']);fly=choose(['flying'],['attack'])
 if hit:clips['stun']=hit
 if fly:clips['fly']=fly
 def pose(action,frame):
  for rig in rigs:rig.animation_data_create();rig.animation_data.action=action
  bpy.context.scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
 def snapshot():
  graph=bpy.context.evaluated_depsgraph_get();result=[]
  for obj in objects:
   ev=obj.evaluated_get(graph);mesh=ev.to_mesh();mesh.calc_loop_triangles();result.append((ev,mesh))
  return result
 def release(items):
  for ev,mesh in items:ev.to_mesh_clear()
 pose(clips['idle'],clips['idle'].frame_range[0]);items=snapshot()
 refs=[];indices=[];lookup={};colors=[];pixels={}
 for oi,(ev,mesh) in enumerate(items):
  uv=mesh.uv_layers.active;attr=mesh.color_attributes.active_color or (mesh.color_attributes[0] if len(mesh.color_attributes) else None)
  for tri in mesh.loop_triangles:
   mat=mesh.materials[tri.material_index];base=(1,1,1,1);image=None
   if mat and mat.use_nodes:
    for node in mat.node_tree.nodes:
     if node.type=='BSDF_PRINCIPLED':base=tuple(node.inputs['Base Color'].default_value)
     if node.type=='TEX_IMAGE' and node.image:image=node.image
   for li in tri.loops:
    vi=mesh.loops[li].vertex_index;normal=mesh.corner_normals[li].vector
    tex=tuple(uv.data[li].uv) if uv else (0,0)
    color=tuple(attr.data[li if attr.domain=='CORNER' else vi].color) if attr else (1,1,1,1)
    if image:
     if image.name not in pixels:pixels[image.name]=list(image.pixels)
     w,h=image.size;assert w and h,(kind,image.name)
     x=min(w-1,max(0,int(tex[0]*w)));y=min(h-1,max(0,int(tex[1]*h)));offset=(y*w+x)*4
     sample=pixels[image.name][offset:offset+4];color=tuple(color[i]*sample[i] for i in range(4))
    linear=[max(0,min(1,color[i]*base[i])) for i in range(3)]
    color=tuple(round((12.92*v if v<=.0031308 else 1.055*v**(1/2.4)-.055)*255) for v in linear)+(255,)
    key=(oi,vi,tuple(round(v,5) for v in normal),color)
    if key not in lookup:lookup[key]=len(refs);refs.append((oi,li));colors.append(color)
    indices.append(lookup[key])
 points=[ev.matrix_world@v.co for ev,mesh in items for v in mesh.vertices]
 low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
 scale=1/(high.z-low.z);center=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z));release(items)
 info={'kind':kind,'vertices':len(refs),'triangles':len(indices)//3,'clips':{}}
 with (out/(kind+'.bytes')).open('wb') as file:
  file.write(b'FRM1');file.write(struct.pack('<iii',len(refs),len(indices),len(clips)))
  for color in colors:file.write(bytes(color))
  file.write(struct.pack('<'+'i'*len(indices),*indices))
  for state,action in clips.items():
   start,end=action.frame_range;duration=max(.1,(end-start)/bpy.context.scene.render.fps);count=8 if state in ['move','fly'] else 6
   name=state.encode();file.write(struct.pack('<i',len(name))+name+struct.pack('<fi',duration,count));info['clips'][state]=count
   for f in range(count):
    pose(action,start+(end-start)*f/count);items=snapshot();values=[]
    for oi,li in refs:
     ev,mesh=items[oi];v=mesh.vertices[mesh.loops[li].vertex_index];p=(ev.matrix_world@v.co-center)*scale
     n=(ev.matrix_world.to_3x3().inverted().transposed()@mesh.corner_normals[li].vector).normalized()
     values.extend((p.x,p.z,-p.y,n.x,n.z,-n.y))
    assert all(math.isfinite(v) for v in values),kind
    file.write(struct.pack('<'+'f'*len(values),*values));release(items)
 info['bytes']=(out/(kind+'.bytes')).stat().st_size;manifest.append(info);print('BAKED',info)
(root/'Tools/AssetSources/enemy_frames.json').write_text(json.dumps(manifest,indent=2)+'\n')
