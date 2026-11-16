import bpy, math, random, json
from mathutils import Vector
S=.28
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1100
scene.render.resolution_y=800
scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('BunkerWorld')
scene.world.color=(.08,.08,.08)
M={}
for n,h in {'concrete':'626e70','steel':'394f50','rust':'784532','floor':'68746b','black':'171e22','yellow':'c5a54a','paper':'b1b29a','moss':'344b36','linen':'737e71','white':'b7c9be','red':'e43b28','glow':'a8e4de','wood':'645640'}.items():
 c=tuple(int(h[i:i+2],16)/255 for i in (0,2,4))
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Roughness'].default_value=.85
 if n in ['steel','rust']:bs.inputs['Metallic'].default_value=.55
 if n in ['glow','red']:bs.inputs['Emission Color'].default_value=(*c,1);bs.inputs['Emission Strength'].default_value=2
 M[n]=m
def part(n,p,d,m='steel',shape='BLOCK',r=(0,0,0)):
 if shape=='CYLINDER':bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.5,depth=1)
 else:bpy.ops.mesh.primitive_cube_add(size=1)
 o=bpy.context.object;o.name=n;o.location=Vector(p)*S;o.dimensions=Vector(d)*S
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.rotation_euler=tuple(math.radians(a) for a in r);o.data.materials.append(M[m])
 o['primitive']=shape;o['size_studs']=list(d);o['position_studs']=list(p);o['rotation_degrees']=list(r)
 return o
def cam(n,p,t,ortho=None):
 bpy.ops.object.camera_add(location=Vector(p)*S);o=bpy.context.object;o.name=n
 o.rotation_euler=(Vector(t)*S-o.location).to_track_quat('-Z','Y').to_euler()
 o.data.lens=23
 if ortho:o.data.type='ORTHO';o.data.ortho_scale=ortho*S
 return o
def light(n,p,c,power,typ='POINT'):
 d=bpy.data.lights.new(n,typ);d.color=c;d.energy=power
 if typ=='POINT':d.shadow_soft_size=.5
 o=bpy.data.objects.new(n,d);scene.collection.objects.link(o);o.location=Vector(p)*S
 return o
part('Foundation',(30,20,-.5),(62,42,1),'concrete')
part('Linoleum',(30,20,.04),(60,40,.08),'floor')
for y in [-.5,40.5]:part('Perimeter',(30,y,5),(62,1,10),'concrete')
part('Rear',(60.5,20,5),(1,40,10),'concrete')
for y,l in [(8.5,17),(31.5,17)]:part('FrontWall',(-.5,y,5),(1,l,10),'concrete')
part('DoorLintel',(-.5,20,9),(1,6,2),'concrete')
# corridor boundaries with real openings
for y,openings in [(16.5,[40,53]),(23.5,[20,37])]:
 start=0
 for x in openings:
  part('CorridorWall',((start+x-2)/2,y,5),(x-2-start,1,10),'concrete')
  part('DoorHeader',(x,y,9),(4,1,2),'concrete');start=x+2
 part('CorridorWall',((start+60)/2,y,5),(60-start,1,10),'concrete')
for x,y,d in [(29.5,32,(1,16,10)),(29.5,8,(1,16,10)),(46.5,8,(1,16,10))]:
 part('RoomDivider',(x,y,5),d,'concrete')
# airlock divider opening
for y in [17.5,22.5]:part('AirlockJamb',(10,y,4),(1,1,8),'steel')
part('AirlockHeader',(10,20,9),(1,6,2),'steel')
part('Roof',(30,20,10.5),(62,42,1),'concrete').hide_render=True
# exterior door, apron and entrance
part('Apron',(-6,20,-.3),(12,12,.6),'concrete')
for y in [15,25]:part('EntranceButtress',(-3,y,5),(6,2,10),'concrete')
part('EntranceCanopy',(-3,20,10),(7,12,2),'concrete')
part('BlastDoor',(-.7,20,4),(1,5.7,8),'steel')
for z in [1,7]:part('DoorBrace',(-1.28,20,z),(.18,5.4,.35),'rust')
for i in range(12):
 a=i*math.tau/12
 part('WheelRim',(-1.4,20+math.cos(a),4+math.sin(a)),(.18,.56,.18),'rust',r=(math.degrees(a)+90,0,0))
for a in [0,60,120]:part('WheelSpoke',(-1.45,20,4),(.16,2,.13),'steel',r=(a,0,0))
part('Vent',(-2,27,8),(1,1,8),'rust','CYLINDER')
part('BrokenFloodlight',(-4,25,8.3),(1.6,.9,.8),'black',r=(0,25,0))
def text(n,body,p,size,r):
 d=bpy.data.curves.new(n,'FONT');d.body=body;d.size=size*S;d.extrude=.001
 o=bpy.data.objects.new(n,d);scene.collection.objects.link(o);o.location=Vector(p)*S;o.rotation_euler=tuple(math.radians(a) for a in r);d.materials.append(M['paper'])
text('CompanyStencil','HALVERN RESEARCH',(-6.53,24.6,9.65),.65,(90,0,-90))
# exterior hillside masses stay outside interior
for y in [-5,45]:
 part('Hillside',(30,y,5),(72,10,12),'moss')
# camera rig
scene.camera=cam('CAM_Overview',(-35,-48,65),(27,20,0),96)
cam('CAM_Front',(-27,3,15),(-1,20,5))
cam('CAM_Side',(28,-60,23),(28,20,2),84)
cam('CAM_Interior',(12,20,5.3),(58,20,4.7))
key=light('LGT_Moon',(-20,-20,35),(.55,.72,1),2,'SUN');key.rotation_euler=(.4,-.5,-.4)
light('LGT_Fill',(-9,20,14),(.6,.78,.82),900)
result={'footprint_m':[16.8,11.2],'objects':len(bpy.data.objects)}


# practical ceiling fixtures
for i,(x,y) in enumerate([(5,20),(16,20),(29,20),(43,20),(55,20),(15,32),(44,32),(38,8),(53,8)]):
 part('FluorescentHousing',(x,y,9.65),(4,.7,.3),'steel')
 part('FluorescentTube',(x,y,9.43),(3.7,.28,.18),'glow')
 light('LGT_Fluorescent_%02d'%i,(x,y,9.1),(.66,.89,.87),85 if i%3 else 35)
for x in [10,32,58]:
 part('EmergencyLens',(x,16.0,7.5),(.7,.25,.7),'red')
 light('LGT_Emergency',(x,17,7.5),(1,.06,.025),22)
for y in [17.8,18.5]:
 part('CeilingPipe',(35,y,8.7),(.35,.35,48),'rust','CYLINDER',r=(0,90,0))
part('CableTray',(35,22,8.7),(48,.8,.15),'steel')
for x in range(12,59,3):
 part('TrayBracket',(x,22,9.1),(.15,1.2,.8),'steel')
for y in [21.8,22,22.2]:part('Cable',(35,y,8.9),(.08,.08,48),'black','CYLINDER',r=(0,90,0))
for i in range(10):part('HazardStripe',(1+i*.9,20,.11),(.45,5.8,.06),'yellow' if i%2 else 'black',r=(0,0,12))
for y in [18,22]:part('DeconNozzle',(5,y,7),(.4,.4,.5),'steel','CYLINDER',r=(90,0,0))
# control desks and CRTs
for x in [5,12,23]:
 part('DeskTop',(x,36,2.8),(5,2.5,.3),'steel')
 for dx in [-2,2]:
  for dy in [-.9,.9]:part('DeskLeg',(x+dx,36+dy,1.35),(.2,.2,2.7),'steel')
 part('CRTCase',(x,36.3,4),(2,1.5,1.8),'paper')
 part('CRTScreen',(x,35.53,4),(1.6,.05,1.2),'black')
 for z in [3.8,4,4.2]:part('CRTScanline',(x,35.49,z),(1.2,.025,.025),'glow')
 part('Keyboard',(x,35,3.05),(1.8,.6,.15),'black')
for x in [3,6]:
 part('FilingCabinet',(x,27,2.5),(2,2,5),'steel')
 for z in [1,2.2,3.4,4.6]:
  part('Drawer',(x,25.97,z),(1.85,.06,1),'floor')
  part('DrawerHandle',(x,25.85,z),(.6,.15,.12),'rust')
part('IslandMapBoard',(17,39.85,5),(9,.18,5),'paper')
for i in range(7):
 part('IslandMapLand',(17+math.sin(i)*1.4,39.73,3.5+i*.45),(2.4,.04,.7),'moss',r=(0,15*i,0))
text('MapTitle','HALVERN / ISLAND 04',(13.2,39.6,7),.4,(90,0,0))
# 4 bunk frames, eight mattresses
for bi,(x,y) in enumerate([(34,36),(42,36),(50,36),(57,36)]):
 for dx in [-1.4,1.4]:
  for dy in [-3,3]:part('Bunk%d_Post'%bi,(x+dx,y+dy,3),(.18,.18,6),'steel')
 for z in [1.5,4.6]:
  part('Bunk%d_Frame'%bi,(x,y,z),(3,6.2,.2),'steel')
  part('Bunk%d_Mattress'%bi,(x,y,z+.35),(2.8,6,.5),'linen')
  part('Bunk%d_Pillow'%bi,(x,y+2,z+.7),(2,.9,.3),'paper')
  part('BunkRail',(x+1.4,y,z+.8),(.12,6,.12),'steel')
 for z in [1,2,3,4,5]:part('LadderRung',(x,y-3.2,z),(1.6,.15,.15),'rust')
for x in [32,35,38]:
 part('Locker',(x,26,3),(2,2,6),'steel')
 part('LockerHandle',(x+.65,24.94,3),(.1,.1,.7),'rust')
part('KitchenCounter',(52,26,1.6),(12,3,3.2),'steel')
part('CounterSurface',(52,26,3.3),(12.2,3.1,.2),'paper')
part('Sink',(49,26,3.43),(2.5,1.8,.08),'black')
for x in [54,56]:
 for y in [25.5,26.5]:part('StoveRing',(x,y,3.48),(.7,.7,.1),'black','CYLINDER')
# shelving and crates
for y in [3,12]:
 for x in [33,43]:
  for dx in [-2,2]:
   for dy in [-1,1]:part('ShelfUpright',(x+dx,y+dy,3),(.15,.15,6),'steel')
  for z in [.3,2.5,4.7]:
   part('Shelf',(x,y,z),(4.5,2.3,.15),'rust')
   part('SupplyCrate',(x,y,z+.85),(2.5,1.8,1.5),'wood')
   for dx in [-1,1]:part('CrateBand',(x+dx,y,z+.85),(.12,1.85,1.55),'steel')
# med bay
for x in [50,54]:
 for y in [4,10]:part('MedBedLeg',(x,y,1),(.2,.2,2),'steel')
part('MedBedFrame',(52,7,2),(4.5,7,.3),'steel')
part('MedMattress',(52,7,2.5),(4.2,6.8,.7),'white')
part('MedPillow',(52,9.4,3),(3,1.3,.3),'paper')
part('IVPole',(56,9,3),(.12,.12,6),'steel','CYLINDER')
part('IVBase',(56,9,.2),(2,2,.2),'steel')
part('IVHook',(55.7,9,5.8),(.8,.1,.1),'steel')
part('IVBag',(55.4,9,5),(.6,.25,1.1),'white')
part('MedicineCabinet',(59,6,5),(1.5,4,3),'white')
part('CrossHorizontal',(58.2,6,5),(.04,1.4,.35),'red')
part('CrossVertical',(58.18,6,5),(.04,.35,1.4),'red')
# reserved utility area front south, deliberately sealed
text('AirlockLabel','01 / DECONTAMINATION',(2,23,7.7),.45,(90,0,0))
random.seed(17)
for i in range(40):
 x=random.uniform(11,58);y=random.choice([random.uniform(17,23),random.uniform(25,33)])
 part('ScatteredPaper',(x,y,.14),(random.uniform(.5,.9),1,.025),'paper',r=(0,0,random.uniform(0,180)))
for i in range(36):
 x=random.uniform(1,59);z=random.uniform(1,8)
 part('PeelingPaint',(x,16-.015,z),(random.uniform(.25,1.3),.025,random.uniform(.2,.9)),'paper')
 part('WaterStain',(x,24.015,z),(random.uniform(.15,.6),.025,random.uniform(.7,2)),'moss')
for i in range(18):
 y=random.choice([14,26])+random.uniform(-.5,.5);z=random.uniform(4,10)
 part('Vine',(-6.05,y,z),(.08,.13,random.uniform(.8,2)),'moss',r=(random.uniform(-20,20),0,0))
 part('MossPatch',(-6.08,y,z),(.05,.6,.3),'moss')
part('OverturnedChairSeat',(18,29,.8),(2,.2,2),'steel',r=(0,0,15))
part('OverturnedChairBack',(18,30,.8),(2,2,.2),'steel',r=(0,0,15))
for dx in [-.8,.8]:
 for z in [.2,1.5]:part('ChairLeg',(18+dx,28,z),(.12,2,.12),'rust')
# Roof remains available but hidden for cutaway inspection.
roof=bpy.data.objects['Roof'];roof.hide_set(True)
scene.camera=bpy.data.objects['CAM_Overview']
# Embedded rebuild schedules, including every primitive and lettering
rows=[]
for o in bpy.data.objects:
 if o.type in ['MESH','FONT']:
  rows.append({'name':o.name,'shape':o.get('primitive','TEXT'),'size_studs':list(o.get('size_studs',o.dimensions/.28)),'position_studs':list(o.location/.28),'rotation_degrees':[round(math.degrees(v),3) for v in o.rotation_euler],'material':o.active_material.name if o.active_material else '', 'color_rgb':list(o.active_material.diffuse_color[:3]) if o.active_material else []})
bpy.data.texts.new('HALVERN_PARTS.json').write(json.dumps(rows,indent=2))
result={'part_count':len(rows),'lights':len([o for o in bpy.data.objects if o.type=='LIGHT'])}

