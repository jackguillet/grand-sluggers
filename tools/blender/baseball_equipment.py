"""Original baseball equipment. Dimensions and mesh IDs precede the bake.

These meshes are used by extras.fbx and the motion evidence renders, so the
artist sees the same bat and gloves the player receives.
"""
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector
import hero_shared_blockout as body

SPEC = json.loads((Path(__file__).resolve().parents[2] / 'data/art/baseball-equipment.json').read_text())


def surface(name, vertices, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(material)
    for poly in mesh.polygons:
        poly.use_smooth = True
    return ob


def tube(name, points, radius, material):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    spline = curve.splines.new('POLY')
    spline.points.add(len(points)-1)
    for p, xyz in zip(spline.points, points):
        p.co = (*xyz, 1)
    ob = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(material)
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target='MESH')
    return bpy.context.object


def lathe(name, profile, material, segments=40):
    vertices = [(r*math.cos(i*2*math.pi/segments), r*math.sin(i*2*math.pi/segments), z)
                for z,r in profile for i in range(segments)]
    faces = []
    for j in range(len(profile)-1):
        for i in range(segments):
            a=j*segments+i; b=j*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    faces.extend([tuple(reversed(range(segments))),tuple((len(profile)-1)*segments+i for i in range(segments))])
    return surface(name,vertices,faces,material)


def bat_parts(m):
    spec=SPEC['bat']
    bat=lathe('BatBarrel',spec['profile'],m['wood'])
    bat.data.materials.append(m['grip'])
    for polygon in bat.data.polygons:
        if -1.001 < polygon.center.z < -.10001:
            polygon.material_index=1
    # Raised diagonal wrap reads from gameplay distance without altering the
    # bat's collision envelope or handle endpoint.
    points=[(.083*math.cos(i*.20),.083*math.sin(i*.20),-.98+.63*i/240) for i in range(241)]
    return [bat,tube('GripWrap',points,.008,m['cream'])]


def glove_parts(m, gold=False):
    spec=SPEC['glove']; w=spec['width']; length=spec['length']; depth=spec['pocketDepth']
    leather=m['gold'] if gold else m['wood']
    lining=m['grip']; lace=m['cream']
    # Open oval bowl: the center is recessed away from the incoming ball.
    # Rim rises toward the palm-facing -Y side, leaving a real concave pocket.
    rings=9; seg=40; verts=[]
    for j in range(rings):
        r=j/(rings-1)
        for i in range(seg):
            a=i*2*math.pi/seg
            verts.append((w*.46*r*math.cos(a), depth*(1-r*r), length*.40+length*.35*r*math.sin(a)))
    faces=[(j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i) for j in range(rings-1) for i in range(seg)]
    pocket=surface('GlovePocket',verts,faces,leather)
    solid=pocket.modifiers.new('Leather thickness','SOLIDIFY');solid.thickness=.025
    pieces=[pocket]
    rim=[(w*.46*math.cos(i*2*math.pi/seg),0,length*.40+length*.35*math.sin(i*2*math.pi/seg)) for i in range(seg+1)]
    pieces.append(tube('PocketWelt',rim,.022,lace))
    for i in range(spec['fingers']):
        x=-.09+i*.085; top=length*(.92-abs(i-1.3)*.065)
        pieces.append(tube('FingerStall'+str(i),[(x,.08,.32),(x*1.12,-.005,top-.05),(x*1.12,-.05,top)],.052,leather))
        pieces.append(tube('FingerSeam'+str(i),[(x,.023,.39),(x*1.12,-.055,top-.045)],.008,lace))
    pieces.append(tube('ThumbStall',[(-.10,.04,.15),(-.24,-.03,.29),(-.29,-.06,.46)],.075,leather))
    # Woven bridge between thumb and index; dark empty cells remain visible.
    for i in range(4):
        z=.34+i*.038
        pieces.append(tube('WebCross'+str(i),[(-.26,-.045,z),(-.09,-.035,z+.06)],.015,leather))
    for i in range(4):
        x=-.245+i*.042
        pieces.append(tube('WebUpright'+str(i),[(x,-.050,.34),(x+.025,-.04,.52)],.012,lace))
    # An annular cuff, open through the center. Never a sphere glued to an arm.
    for z in (.015,.085):
        pieces.append(tube('WristWelt',[(spec['wristRadius']*math.cos(i*2*math.pi/32),.04+spec['wristRadius']*.7*math.sin(i*2*math.pi/32),z) for i in range(33)],.025,lining))
    pieces.append(tube('Heel', [(-.11,.06,.09),(0,.12,.12),(.12,.06,.09)],.05,leather))
    return pieces


def reflected_mesh(source, name):
    ob=source.copy(); ob.data=source.data.copy(); ob.name=name
    bpy.context.collection.objects.link(ob)
    for v in ob.data.vertices:v.co.x=-v.co.x
    # Reflection reverses winding. Correct actual mesh normals before export.
    import bmesh
    bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
    return ob
