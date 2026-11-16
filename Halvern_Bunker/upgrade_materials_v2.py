import bpy, math, random, json
from mathutils import Vector
scene=bpy.context.scene
S=.28
random.seed(42)
def material(n,c,metal=0,rough=.8):
 m=bpy.data.materials.get(n) or bpy.data.materials.new(n);m.use_nodes=True;m.diffuse_color=(*c,1)
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 return m
def weather(n,c1,c2,scale,bump,metal=0,rough=.8):
 m=material(n,c1,metal,rough);nt=m.node_tree;bs=nt.nodes.get('Principled BSDF')
 tc=nt.nodes.new('ShaderNodeTexCoord');noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale;noise.inputs['Detail'].default_value=4
 nt.links.new(tc.outputs['Object'],noise.inputs['Vector'])
 ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.28;ramp.color_ramp.elements[0].color=(*c1,1);ramp.color_ramp.elements[1].position=.72;ramp.color_ramp.elements[1].color=(*c2,1)
 nt.links.new(noise.outputs['Fac'],ramp.inputs[0]);nt.links.new(ramp.outputs['Color'],bs.inputs['Base Color'])
 fine=nt.nodes.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=95;fine.inputs['Detail'].default_value=2;nt.links.new(tc.outputs['Object'],fine.inputs['Vector'])
 b=nt.nodes.new('ShaderNodeBump');b.inputs['Strength'].default_value=.38;b.inputs['Distance'].default_value=bump;nt.links.new(fine.outputs['Fac'],b.inputs['Height']);nt.links.new(b.outputs['Normal'],bs.inputs['Normal'])
 return m
weather('concrete',(.09,.12,.12),(.38,.40,.36),3,.025)
weather('steel',(.025,.065,.063),(.18,.27,.24),9,.008,.65,.55)
weather('rust',(.055,.025,.012),(.43,.16,.045),12,.016,.45,.82)
weather('floor',(.055,.09,.083),(.25,.29,.22),14,.006,0,.48)
weather('linen',(.12,.17,.14),(.42,.44,.34),24,.012)
weather('wood',(.06,.035,.014),(.28,.19,.08),6,.018)
weather('moss',(.018,.035,.012),(.15,.24,.06),16,.025)
weather('paper',(.35,.33,.22),(.7,.67,.48),8,.003)
weather('white',(.29,.35,.30),(.65,.68,.55),6,.005,.12)
weather('Paint_aged',(.045,.11,.10),(.24,.32,.24),5,.008,.05)
material('Water',(.018,.035,.035),.25,.1)
material('Gasket',(.012,.018,.017),0,.9)
material('BareMetal',(.23,.27,.25),.85,.3)
def part(n,p,d,m='steel',r=(0,0,0),cyl=False):
 if cyl:bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.5,depth=1)
 else:bpy.ops.mesh.primitive_cube_add(size=1)
 o=bpy.context.object;o.name=n;o.location=Vector(p)*S;o.dimensions=Vector(d)*S
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.rotation_euler=tuple(math.radians(v) for v in r);o.data.materials.append(bpy.data.materials[m])
 o['primitive']='CYLINDER' if cyl else 'BLOCK';o['size_studs']=list(d)
 return o
def label(body,p,size=.45,r=(90,0,0)):
 d=bpy.data.curves.new('Stencil','FONT');d.body=body;d.size=size*S;d.extrude=.0005
 o=bpy.data.objects.new('SIGN_'+body,d);scene.collection.objects.link(o);o.location=Vector(p)*S;o.rotation_euler=tuple(math.radians(v) for v in r);d.materials.append(bpy.data.materials['paper'])
# half height wall paint with openings inherited from wall blocks
for o in list(bpy.data.objects):
 if o.name.startswith('CorridorWall'):
  p=o.location/S;d=o.dimensions/S
  for side in [-1,1]:
   part('DadoPaint',(p.x,p.y+side*.51,2),(d.x,.025,4),'Paint_aged')
   part('DadoCap',(p.x,p.y+side*.535,4.05),(d.x,.06,.08),'rust')
# floor tile seams, wall base trim
for x in range(0,61,3):part('FloorJoint',(x,20,.087),(.025,40,.008),'Gasket')
for y in range(0,41,3):part('FloorJoint',(30,y,.087),(60,.025,.008),'Gasket')
for o in list(bpy.data.objects):
 if o.name.startswith('CorridorWall'):
  p=o.location/S;d=o.dimensions/S
  for side in [-1,1]:part('Skirting',(p.x,p.y+side*.55,.25),(d.x,.1,.5),'steel')
# door frames, labels and jamb fasteners
for x,y,sign in [(20,23.5,'02 / CONTROL'),(37,23.5,'03 / HABITAT'),(40,16.5,'04 / STORES'),(53,16.5,'05 / MEDICAL')]:
 for dx in [-2,2]:
  part('DoorFrame',(x+dx,y,4),(.16,1.12,8),'rust')
  for z in [1,4,7]:part('FrameBolt',(x+dx,y+(-.6 if y>20 else .6),z),(.13,.08,.13),'BareMetal')
 part('FrameTop',(x,y,8),(4.2,1.12,.16),'steel')
 if y>20:label(sign,(x-1.7,y-.62,8.55),.37)
 else:label(sign,(x+1.7,y+.62,8.55),.37,(90,0,180))
# pipe flanges and fixture guards
for x in range(13,59,6):
 for y in [17.8,18.5]:
  part('PipeFlange',(x,y,8.7),(.62,.62,.18),'rust',r=(0,90,0),cyl=True)
for o in list(bpy.data.objects):
 if o.name.startswith('FluorescentHousing'):
  p=o.location/S
  for dx in [-1.8,-.6,.6,1.8]:part('LampGuard',(p.x+dx,p.y,9.27),(.06,.75,.06),'steel')
# vent grille and switch boxes
for x in [15,28,44,57]:
 part('ServicePanel',(x,22.86,5.6),(1.4,.15,1.8),'steel')
 for z in [5.2,5.4,5.6,5.8,6]:part('PanelLouvre',(x,22.75,z),(1,.06,.035),'Gasket')
 part('Switch',(x+.4,22.72,4.9),(.15,.1,.25),'red')
# front door fittings
for y in [17.4,22.6]:
 for z in [1,3,5,7]:
  part('BlastDoorRivet',(-1.26,y,z),(.12,.2,.2),'BareMetal',r=(0,90,0),cyl=True)
for z in [1.5,6.5]:part('BlastHinge',(-1.3,22.8,z),(.5,.45,1),'rust',cyl=True)
label('AUTHORIZED PERSONNEL ONLY',(-1.23,22.2,6.4),.25,(90,0,-90))
# organic irregular puddles
for x,y,s in [(15,20,1.2),(31,21,1.7),(49,19,1),(8,30,1.3),(52,12,.8)]:
 verts=[(x,y,.095)]+[(x+math.cos(i*math.tau/16)*s*random.uniform(.8,1.2),y+math.sin(i*math.tau/16)*s*.45*random.uniform(.8,1.2),.095) for i in range(16)]
 me=bpy.data.meshes.new('Puddle');me.from_pydata([Vector(v)*S for v in verts],[],[(0,i+1,(i+1)%16+1) for i in range(16)]);me.update()
 o=bpy.data.objects.new('DampPuddle',me);scene.collection.objects.link(o);me.materials.append(bpy.data.materials['Water'])
# add small physical labels, crate slats, medicine bottles and bedding seams
for o in list(bpy.data.objects):
 if o.name.startswith('SupplyCrate'):
  p=o.location/S
  for dx in [-.9,-.3,.3,.9]:part('CrateSlat',(p.x+dx,p.y-.92,p.z),(.035,.04,1.5),'Gasket')
 if o.name.startswith('Locker'):
  if o.name.startswith('LockerHandle'):continue
  p=o.location/S
  for z in [4.8,5,5.2]:part('LockerVent',(p.x,p.y-1.02,z),(1.3,.04,.055),'Gasket')
 if 'Mattress' in o.name:
  p=o.location/S;d=o.dimensions/S
  part('FoldedBlanket',(p.x,p.y-1,p.z+d.z/2+.07),(d.x*.96,2,.15),'Paint_aged')
for i in range(7):
 part('MedicineBottle',(58.1,4.8+i*.4,4.25),(.22,.22,.5),'white',cyl=True)
 part('BottleCap',(58.1,4.8+i*.4,4.53),(.23,.23,.08),'rust',cyl=True)
# thin crack lines on corridor wall, gravity runoff strips
for i in range(24):
 x=random.uniform(11,59);z=random.uniform(5,8)
 part('LeakStreak',(x,22.975,z),(.04,.025,random.uniform(.5,2)),'moss',r=(0,random.uniform(-6,6),0))
# replace cuboid terrain with sloping triangulated hillside strips
for o in list(bpy.data.objects):
 if o.name.startswith('Hillside'):bpy.data.objects.remove(o,do_unlink=True)
for side in [-1,1]:
 ys=[-12,-1] if side==-1 else [41,52]
 verts=[]
 for x in [-12,0,12,24,36,48,62,72]:
  for j,y in enumerate(ys):
   z=(1 if (side==-1 and j==0) or (side==1 and j==1) else 10.5)+random.uniform(-.8,.8)
   verts.append((x*S,y*S,z*S))
 faces=[]
 for i in range(7):faces.extend([(2*i,2*i+1,2*i+2),(2*i+1,2*i+3,2*i+2)])
 me=bpy.data.meshes.new('TerrainSlope');me.from_pydata(verts,[],faces);me.update()
 o=bpy.data.objects.new('TerrainSlope',me);scene.collection.objects.link(o);me.materials.append(bpy.data.materials['moss'])
# bevel physical manufactured parts; preserve low polygon count
for o in bpy.data.objects:
 if o.type=='MESH' and len(o.data.polygons)<=18 and min(o.dimensions)>.025 and not o.name.startswith(('Terrain','Damp')):
  b=o.modifiers.new('Soft worn edges','BEVEL');b.width=min(.012,min(o.dimensions)*.12);b.segments=2
# organized collection groups
for group in ['Architecture','Furniture','Wear_and_details','Lighting','Cameras','Terrain']:
 if not bpy.data.collections.get(group):scene.collection.children.link(bpy.data.collections.new(group))
for o in list(scene.objects):
 group='Lighting' if o.type=='LIGHT' else 'Cameras' if o.type=='CAMERA' else 'Terrain' if o.name.startswith('Terrain') else 'Architecture' if o.name.startswith(('Perimeter','CorridorWall','FrontWall','Rear','Roof','Foundation','Linoleum','RoomDivider','Dado','Skirting')) else 'Furniture' if o.name.startswith(('Bunk','Desk','CRT','Med','Locker','Shelf','Kitchen','Supply','Filing')) else 'Wear_and_details'
 for col in list(o.users_collection):col.objects.unlink(o)
 bpy.data.collections[group].objects.link(o)
roof=bpy.data.objects['Roof'];roof.hide_set(False);roof.hide_render=False
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.12,.19,.22,1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.12
for o in bpy.data.objects:
 if o.type=='LIGHT' and 'Fluorescent' in o.name:o.data.energy=65;o.data.shadow_soft_size=.32
 if o.type=='LIGHT' and 'Emergency' in o.name:o.data.energy=35
scene.render.engine='BLENDER_EEVEE'
scene.eevee.taa_render_samples=8
scene.eevee.use_raytracing=False
scene.render.resolution_x=960;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.view_settings.exposure=1
scene.camera=bpy.data.objects['CAM_Interior']
scene.camera.data.lens=22
# reproducible full updated schedule
rows=[]
for o in bpy.data.objects:
 if o.type in ['MESH','FONT']:
  rows.append({'name':o.name,'shape':o.get('primitive','MESH' if o.type=='MESH' else 'TEXT'),'size_studs':[round(v/S,4) for v in o.dimensions],'position_studs':[round(v/S,4) for v in o.location],'rotation_degrees':[round(math.degrees(v),3) for v in o.rotation_euler],'material':o.active_material.name if o.active_material else ''})
t=bpy.data.texts.get('HALVERN_PARTS_V2.json') or bpy.data.texts.new('HALVERN_PARTS_V2.json');t.clear();t.write(json.dumps(rows,indent=2))
result={'parts':len(rows),'materials':len(bpy.data.materials)}

