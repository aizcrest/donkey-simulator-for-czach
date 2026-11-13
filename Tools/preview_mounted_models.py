"""Blender asset preview of the exact mounted-model layout used by Unity; not gameplay."""
import bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector
root=Path(__file__).resolve().parents[1]
nodes=json.loads((root/'Assets/Resources/MountedModels.json').read_text())['nodes']
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
objects={};materials={}
c=Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
triangles=0
for node in nodes:
 if node['shape']:
  if node['shape']=='box':bpy.ops.mesh.primitive_cube_add(size=1)
  else:bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=6,radius=.5)
  obj=bpy.context.object
  color=tuple(node['color'])
  if color not in materials:
   mat=bpy.data.materials.new(str(color));mat.diffuse_color=(*color,1);materials[color]=mat
  obj.data.materials.append(materials[color]);triangles+=sum(len(p.vertices)-2 for p in obj.data.polygons)
 else:
  obj=bpy.data.objects.new(node['name'],None);bpy.context.collection.objects.link(obj);objects[node['name']]=obj
 obj.name=node['name']
 if node['parent']:obj.parent=objects[node['parent']]
 x,y,z=[math.radians(v) for v in node['rotation']]
 rot=Matrix.Rotation(y,4,'Y')@Matrix.Rotation(x,4,'X')@Matrix.Rotation(z,4,'Z')
 obj.matrix_basis=c@(Matrix.Translation(Vector(node['position']))@rot@Matrix.Diagonal((*node['scale'],1)))@c.inverted()
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(v) for o in bpy.context.scene.objects if o.type=='MESH' for v in o.bound_box]
low=[min(v[i] for v in points) for i in range(3)];high=[max(v[i] for v in points) for i in range(3)]
assert low[2]>-.01 and 2<high[2]<3.5,(low,high)
print('Mounted geometry bounds',low,high,'triangles',triangles)
bpy.ops.object.camera_add(location=(4,-6,3.3));camera=bpy.context.object;target=Vector((0,0,1.4));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=3.8
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH';scene.world.color=(.12,.16,.18);scene.display.shading.background_type='WORLD'
scene.render.resolution_x=900;scene.render.resolution_y=850;scene.render.resolution_percentage=100;scene.render.filepath=str(root/'Docs/replacement-mounted-models.png');bpy.ops.render.render(write_still=True)
