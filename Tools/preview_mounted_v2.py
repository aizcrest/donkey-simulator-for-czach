"""Render the exact exported runtime geometry; asset preview, not Unity gameplay."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tools'))
from check_mounted_v2 import load,sample
bpy.ops.wm.read_factory_settings(use_empty=True)
for kind in ['Donkey','Knight']:
 d=load(ROOT/'Assets/Resources/MountedV2'/(kind+'.bytes'));p=sample(d,'idle',.08)
 verts=[(x,-z,y) for x,y,z in p];mesh=bpy.data.meshes.new(kind);mesh.from_pydata(verts,[],d['indices'].reshape(-1,3).tolist());mesh.update()
 obj=bpy.data.objects.new(kind,mesh);bpy.context.collection.objects.link(obj)
 attr=mesh.color_attributes.new(name='Coat',type='FLOAT_COLOR',domain='POINT')
 for i,c in enumerate(d['colors']):
  rgb=[v/255 for v in c[:3]];rgb=[v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb];attr.data[i].color=(*rgb,1)
 if kind=='Donkey':
  for poly in mesh.polygons:poly.use_smooth=True
 mat=bpy.data.materials.new(kind);mat.use_nodes=True;bsdf=mat.node_tree.nodes.get('Principled BSDF');col=mat.node_tree.nodes.new('ShaderNodeVertexColor');col.layer_name='Coat';mat.node_tree.links.new(col.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.8;mesh.materials.append(mat)
bpy.ops.mesh.primitive_plane_add(size=200);plane=bpy.context.object;plane.location.z=-.08
mat=bpy.data.materials.new('Backdrop');mat.diffuse_color=(.09,.14,.13,1);plane.data.materials.append(mat)
s=bpy.context.scene;s.world=bpy.data.worlds.new('World');s.world.color=(.25,)*3
bpy.ops.object.camera_add(location=(3.5,-5,3));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1.25))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.3;s.camera=cam
for loc,power,size in [((3,-4,6),900,5),((-3,1,4),700,4)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
s.render.engine='CYCLES';s.cycles.samples=48;s.cycles.use_denoising=False;s.render.resolution_x=900;s.render.resolution_y=900;s.render.resolution_percentage=100;s.render.filepath=str(ROOT/'Docs/v2-mounted-preview.png');bpy.ops.render.render(write_still=True)
